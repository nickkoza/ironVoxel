// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using ironVoxel.Domain;
using ironVoxel.Pool;

namespace ironVoxel.Service {

    /// <summary>
    /// Handles functions related to generating new chunks, and unloading old chunks.
    /// </summary>
    public sealed class GeneratorService : ServiceGateway<GeneratorServiceImplementation> {

        /// <summary>
        /// Scan for any chunks that no longer need to be loaded in memory, and unload them.
        /// </summary>
        public static void CleanupOldChunks()
        {
            Instance().CleanupOldChunks();
        }


        /// <summary>
        /// Queue a chunk for loading if it can find an empty spot in the world.
        /// </summary>
        /// <returns>A new chunk, if it needed to generate one.</returns>
        public static Chunk GenerateNewChunk()
        {
            Chunk chunk = Instance().GenerateNewChunk();
            return chunk;
        }


        /// <summary>
        /// Get the total number of chunks loaded.
        /// </summary>
        public static uint TotalChunkCount()
        {
            return Instance().TotalChunkCount();
        }


        /// <summary>
        /// Iterate evenly around the universal world position (the camera.)
        /// </summary>
        /// <param name="numberOfSpokes">
        /// How many "spokes" to use when iterating outward. This allows for even iterating on all sides.
        /// </param>
        public static IEnumerable IterateChunksAroundTheWorldPosition(int numberOfSpokes)
        {
            foreach (Chunk chunk in Instance().IterateChunksAroundTheWorldPosition(numberOfSpokes)) {
                yield return chunk;
            }
        }


        /// <summary>
        /// Return a chunk to the chunk pool, so that it's available for distribution again. Be sure not to reference
        /// it directly again after returning it!
        /// </summary>
        public static void ReturnChunk(Chunk chunk)
        {
            Instance().ReturnChunk(chunk);
        }


        /// <summary>
        /// Scan for and do a final unload step on all chunks that have finished unloading themselves as far as they 
        /// could by themselves.
        /// </summary>
        public static void UnloadDeadChunks()
        {
            Instance().UnloadDeadChunks();
        }


        /// <summary>
        /// Get how many chunks are waiting for a final unload step.
        /// </summary>
        public static int UnloadChunksListCount()
        {
            return Instance().UnloadChunksListCount();
        }
    }
    
    public sealed class GeneratorServiceImplementation : IService {
        int worldViewPositionX;
        int worldViewPositionZ;
        static bool[,,] loadedChunksCheckArray;
        static List<Chunk> unloadChunksList;
        static int chunkArrayHeight = CalculateChunkArrayHeight();
        static int chunkPoolSize = CalculateChunkPoolSize();
        static ChunkPool chunkPool;
    
        public GeneratorServiceImplementation ()
        {
            InitializeChunkMeshPool();
            InitializeChunkMeshClusterPool();
            InitializeChunkPool();
            
            loadedChunksCheckArray = new bool[Configuration.CHUNK_VIEW_DISTANCE * 2 + 1, chunkArrayHeight,
                Configuration.CHUNK_VIEW_DISTANCE * 2 + 1];
            
            unloadChunksList = new List<Chunk>((int)Mathf.Ceil(Configuration.CHUNK_VIEW_DISTANCE * 2.5f));
        }
        
        // TODO -- Split up this function to make it shorter and more manageable
        public void CleanupOldChunks()
        {
            // TODO -- This is a weird place to update the worldviewposition
            worldViewPositionX = (int)(Camera.main.transform.position.x / Chunk.SIZE);
            worldViewPositionZ = (int)(Camera.main.transform.position.z / Chunk.SIZE);
            
            for (int xIttr = 0; xIttr < Configuration.CHUNK_VIEW_DISTANCE * 2 + 1; xIttr++) {
                for (int yIttr = 0; yIttr < chunkArrayHeight; yIttr++) {
                    for (int zIttr = 0; zIttr < Configuration.CHUNK_VIEW_DISTANCE * 2 + 1; zIttr++) {
                        loadedChunksCheckArray[xIttr, yIttr, zIttr] = false;
                    }
                }
            }
            
            // Get rid of chunks that are too far from the camera
            for (int chunkIndex = 0; chunkIndex < ChunkRepository.NumberOfChunks(); chunkIndex++) {
                Chunk chunk = ChunkRepository.GetChunkAtIndex(chunkIndex);
                
                // If the chunk is out of range now, return it
                int xDiff = Mathf.Abs(chunk.WorldPosition().x - worldViewPositionX);
                int zDiff = Mathf.Abs(chunk.WorldPosition().z - worldViewPositionZ);
                
                if ((xDiff > Configuration.CHUNK_VIEW_DISTANCE) ||
                    (zDiff > Configuration.CHUNK_VIEW_DISTANCE)) {
                    ChunkSpacePosition location = chunk.WorldPosition();
                    ChunkRepository.SetChunkAtPosition(location, null);
                    ChunkRepository.Remove(chunk);

                    chunk.MarkForUnload();
                    unloadChunksList.Add(chunk);
                }
                else {
                    int markX = (chunk.WorldPosition().x - worldViewPositionX) + Configuration.CHUNK_VIEW_DISTANCE;
                    int markZ = (chunk.WorldPosition().z - worldViewPositionZ) + Configuration.CHUNK_VIEW_DISTANCE;
                    loadedChunksCheckArray[markX, chunk.WorldPosition().y, markZ] = true;
                }
            }
        }
        
        // TODO -- Split up this function to make it shorter, since it gets called to often
        public Chunk GenerateNewChunk()
        {
            for (int worldHeight = chunkArrayHeight - 1; worldHeight >= 0; worldHeight--) {
                if (SlotIsEmpty(worldViewPositionX, worldHeight, worldViewPositionZ)) {
                    return GenerateChunk(worldViewPositionX, worldHeight, worldViewPositionZ);
                }
            }

            int prevXOffset = int.MinValue;
            int prevZOffset = int.MinValue;
            for (int worldHeight = chunkArrayHeight - 1; worldHeight >= 0; worldHeight--) {
                for (int distance = 0; distance <= Configuration.CHUNK_VIEW_DISTANCE - Configuration.CHUNK_GENERATE_GAP; distance++) {
                    for (int angle = 0; angle < 360; angle += (Configuration.CHUNK_VIEW_DISTANCE - distance + 1) * 2) {
                        int xOffset = (int)(Mathf.Sin(Mathf.Deg2Rad * angle) * (distance + 0.5f));
                        int zOffset = (int)(Mathf.Cos(Mathf.Deg2Rad * angle) * (distance + 0.5f));
                        if (xOffset == prevXOffset && zOffset == prevZOffset) {
                            continue;
                        }

                        ChunkSpacePosition chunkPosition;
                        chunkPosition.x = worldViewPositionX + xOffset;
                        chunkPosition.y = worldHeight;
                        chunkPosition.z = worldViewPositionZ + zOffset;
                        
                        if (SlotIsEmpty(chunkPosition)) {
                            Chunk returnChunk = GenerateChunk(chunkPosition);
                            return returnChunk;
                        }
                    }
                }
            }

            return null;
        }
        
        public IEnumerable IterateChunksAroundTheWorldPosition(int numberOfSpokes)
        {
            ChunkSpacePosition lastPosition;
            lastPosition.x = 0;
            lastPosition.y = 0;
            lastPosition.z = 0;
            Chunk chunk = null;
            for (int distance = 0; distance <= Configuration.CHUNK_VIEW_DISTANCE; distance++) {
                for (int angle = 0; angle < 360 / numberOfSpokes; angle += 1) {
                    for (int worldHeight = chunkArrayHeight - 1; worldHeight >= 0; worldHeight--) {
                        for (int spokeNum = 0; spokeNum < numberOfSpokes; spokeNum++) {
                            float calculateAngle = angle + spokeNum * (360 / numberOfSpokes);
                            int xOffset = (int)(Mathf.Sin(Mathf.Deg2Rad * calculateAngle) * (distance + 0.5f));
                            int zOffset = (int)(Mathf.Cos(Mathf.Deg2Rad * calculateAngle) * (distance + 0.5f));
                            //if (frameStopwatch.ElapsedMilliseconds >= partialFrameLength) { break; }
                            
                            ChunkSpacePosition position;
                            position.x = worldViewPositionX + xOffset;
                            position.y = worldHeight;
                            position.z = worldViewPositionZ + zOffset;
                            
                            if (chunk == null || lastPosition.x != position.x || lastPosition.y != position.y ||
                                lastPosition.z != position.z) {
                                
                                chunk = ChunkRepository.GetChunkAtPosition(position);
                                if (chunk != null) {
                                    yield return chunk;
                                }
                                lastPosition = position;
                            }
                        }
                    }
                }
            }
        }

        public void ReturnChunk(Chunk chunk)
        {
            chunkPool.ReturnChunk(chunk);
        }

        public void UnloadDeadChunks()
        {
            for (int chunkIndex = 0; chunkIndex < unloadChunksList.Count; chunkIndex++) {
                Chunk chunk = unloadChunksList[chunkIndex];
                bool returned = chunk.AttemptToUnload();
                if (returned) {
                    unloadChunksList.Remove(chunk);
                }
            }
        }

        public int UnloadChunksListCount()
        {
            return unloadChunksList.Count;
        }
        
        // -------------------------------------------------------------------------------------------------------------
        private void InitializeChunkMeshPool()
        {
            int layers = Enum.GetNames(typeof(ironVoxel.Render.RendererType)).Length;
            ChunkMeshPool.Initialize(chunkPoolSize * layers);
        }
        
        private void InitializeChunkMeshClusterPool()
        {
            ChunkMeshClusterPool.Initialize(chunkPoolSize);
        }
        
        private void InitializeChunkPool()
        {
            chunkPool = new ChunkPool(chunkPoolSize);
        }
        
        // -------------------------------------------------------------------------------------------------------------
        private bool SlotIsEmpty(int worldX, int worldY, int worldZ)
        {
            ChunkSpacePosition position;
            position.x = worldX;
            position.y = worldY;
            position.z = worldZ;
            return SlotIsEmpty(position);
        }
        
        private bool SlotIsEmpty(ChunkSpacePosition position)
        {
            int arrayX = (position.x - worldViewPositionX) + Configuration.CHUNK_VIEW_DISTANCE;
            int arrayZ = (position.z - worldViewPositionZ) + Configuration.CHUNK_VIEW_DISTANCE;
            return loadedChunksCheckArray[arrayX, position.y, arrayZ] == false;
        }
        
        private Chunk GenerateChunk(int worldX, int worldY, int worldZ)
        {
            ChunkSpacePosition position;
            position.x = worldX;
            position.y = worldY;
            position.z = worldZ;
            return GenerateChunk(position);
        }
        
        private Chunk GenerateChunk(ChunkSpacePosition position)
        {
            int arrayX = (position.x - worldViewPositionX) + Configuration.CHUNK_VIEW_DISTANCE;
            int arrayZ = (position.z - worldViewPositionZ) + Configuration.CHUNK_VIEW_DISTANCE;
            
            Chunk newChunk = chunkPool.GetChunk(position);
            ChunkRepository.Add(newChunk);
            ChunkRepository.SetChunkAtPosition(position, newChunk);
            loadedChunksCheckArray[arrayX, position.y, arrayZ] = true;
            newChunk.LoadOrGenerate();
            return newChunk;
        }

        public uint TotalChunkCount()
        {
            return chunkPool.TotalChunkCount();
        }
        
        private static int CalculateChunkArrayHeight()
        {
            return (int)Mathf.Ceil(Configuration.HEIGHT / (float)Chunk.SIZE);
        }
        
        private static int CalculateChunkPoolSize()
        {
            int horizontal = (int)Mathf.Pow(Configuration.CHUNK_VIEW_DISTANCE * 2 + 1, 2);
            int vertical = CalculateChunkArrayHeight();
            return horizontal * vertical;
        }
        
    }
}

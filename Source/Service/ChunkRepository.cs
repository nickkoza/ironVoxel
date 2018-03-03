// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using ironVoxel;
using ironVoxel.Domain;
using ironVoxel.Pool;
using ironVoxel.Gameplay;
using ironVoxel.Service;

namespace ironVoxel.Service {

    /// <summary>
    /// Acts as a facade to interact with all the chunks as a whole.
    /// </summary>
    /// <remarks>
    /// The chunk repository keeps track of all the chunks currently loaded into the world. It also coordinates all the 
    /// interactions between the chunks.
    /// </remarks>
    public sealed class ChunkRepository : ServiceGateway<ChunkRepositoryImplementation> {

        /// <summary>
        /// Flush all the async modifications that have been made to the chunks.
        /// </summary>
        public static void FlushModifications()
        {
            Instance().FlushModifications();
        }


        /// <summary>
        /// Get the block at specific world position.
        /// </summary>
        public static Block GetBlockAtPosition(Vector3 position)
        {
            return Instance().GetBlockAtPosition(position);
        }


        /// <summary>
        /// Get the block at specific world position.
        /// </summary>
        /// <param name="position">Position</param>
        /// <param name="hintChunk">
        /// A hint for which chunk the block may be within. This can be used to cache some of the heavy work between 
        /// calls to this function.
        /// </param>
        public static ChunkBlockPair GetBlockAtPosition(Vector3 position, Chunk hintChunk)
        {
            return Instance().GetBlockAtPosition(position, hintChunk);
        }


        /// <summary>
        /// Get the block at a specific world position.
        /// </summary>
        public static Block GetBlockAtPosition(BlockSpacePosition position)
        {
            return Instance().GetBlockAtPosition(position).block;
        }


        /// <summary>
        /// Get the block at specific world position.
        /// </summary>
        /// <param name="position">Position</param>
        /// <param name="hintChunk">
        /// A hint for which chunk the block may be within. This can be used to cache some of the heavy work between 
        /// calls to this function.
        /// </param>
        public static ChunkBlockPair GetBlockAtPosition(BlockSpacePosition position, Chunk hintChunk)
        {
            return Instance().GetBlockAtPosition(position, hintChunk);
        }


        /// <summary>
        /// Sets the block at a specific world position.
        /// </summary>
        public static void SetBlockAtPosition(Vector3 position, BlockType type)
        {
            Instance().SetBlockAtPosition(position, type);
        }


        /// <summary>
        /// Set the block at a specific world position.
        /// </summary>
        /// <remarks>
        /// This function also allows to manually select whether to do a full surrounding lighjting update. Not 
        /// triggering the update will result in a faster function call, but surrounding blocks may have outdated 
        /// lighting information.
        /// </remarks>
        public static void SetBlockAtPosition(BlockSpacePosition position, BlockType type, bool triggerLightingUpdate)
        {
            Instance().SetBlockAtPosition(position, type, triggerLightingUpdate);
        }


        /// <summary>
        /// Remove (set to air) the block at a specific world position.
        /// </summary>
        public static void RemoveBlockAtPosition(Vector3 position, Chunk.RemoveBlockCallback callback)
        {
            Instance().RemoveBlockAtPosition(position, callback);
        }


        /// <summary>
        /// Remove (set to air) the block at a specific world position.
        /// </summary>
        public static void RemoveBlockAtPosition(BlockSpacePosition position, Chunk.RemoveBlockCallback callback)
        {
            Instance().RemoveBlockAtPosition(position, callback);
        }


        /// <summary>
        /// Remove (set to air) all the blocks within the radius of a specific world position.
        /// </summary>
        public static void RemoveBlocksWithinRadius(Vector3 origin, float radius, Chunk.RemoveBlockCallback callback)
        {
            Instance().RemoveBlocksWithinRadius(origin, radius, callback);
        }


        /// <summary>
        /// Get the number of chunks currently loaded into the world.
        /// </summary>
        public static int NumberOfChunks()
        {
            return Instance().NumberOfChunks();
        }


        /// <summary>
        /// Get a chunk from the list of chunks currently loaded into the world.
        /// </summary>
        public static Chunk GetChunkAtIndex(int index)
        {
            return Instance().GetChunkAtIndex(index);
        }


        /// <summary>
        /// Set the chunk at specific position within the chunk grid.
        /// </summary>
        public static void SetChunkAtPosition(ChunkSpacePosition position, Chunk chunk)
        {
            Instance().SetChunkAtPosition(position, chunk);
        }


        /// <summary>
        /// Get the chunk at a specific position within the chunk grid.
        /// </summary>
        public static Chunk GetChunkAtPosition(ChunkSpacePosition position)
        {
            return Instance().GetChunkAtPosition(position);
        }


        /// <summary>
        /// Get the chunk at a specific world position.
        /// </summary>
        public static Chunk GetChunkAtPosition(Vector3 position)
        {
            return Instance().GetChunkAtPosition(position);
        }


        /// <summary>
        /// Gets the chunk at a specific world position.
        /// </summary>
        public static Chunk GetChunkAtPosition(BlockSpacePosition position)
        {
            return Instance().GetChunkAtPosition(position);
        }


        /// <summary>
        /// Remove a specified chunk from the list of chunks loaded into the world.
        /// </summary>
        public static void Remove(Chunk chunk)
        {
            Instance().RemoveChunkFromList(chunk);
        }


        /// <summary>
        /// Add a specified chunk from the list of chunks loaded into the world.
        /// </summary>
        public static void Add(Chunk chunk)
        {
            Instance().AddChunkToList(chunk);
        }


        /// <summary>
        /// Save all the current loaded chunks to the current file repository.
        /// </summary>
        public static void SaveAllLoadedChunks()
        {
            Instance().SaveAllLoadedChunks();
        }


        /// <summary>
        /// Add the specified chunk to the list of chunks that need work done to them.
        /// </summary>
        public static void AddToProcessingChunkList(Chunk chunk)
        {
            Instance().AddToProcessingChunkList(chunk);
        }


        /// <summary>
        /// Remove the specified chunk from the list of chunks that need work done to them.
        /// </summary>
        public static void RemoveFromProcessingChunkList(Chunk chunk)
        {
            Instance().RemoveFromProcessingChunkList(chunk);
        }


        /// <summary>
        /// Flush all changes to the processing chunk list.
        /// </summary>
        public static void FlushProcessingChunkListModifications()
        {
            Instance().FlushProcessingChunkListModifications();
        }


        /// <summary>
        /// Get a specific chunk that needs processing.
        /// </summary>
        public static Chunk GetProcessingChunk(int index)
        {
            return Instance().GetProcessingChunk(index);
        }


        /// <summary>
        /// Get the size of the list of chunks that need processing.
        /// </summary>
        public static int GetProcessingChunkListSize()
        {
            return Instance().GetProcessingChunkListSize();
        }


        /// <summary>
        /// Reprioritize the list of chunks that need processing, so that the most urgent get processed first.
        /// </summary>
        public static void RePrioritizeSortProcessingChunkList()
        {
            Instance().RePrioritizeProcessingChunkList();
        }


        /// <summary>
        /// Iterate all the the chunks within a radius of a given point.
        /// </summary>
        public static IEnumerable IterateChunksWithinRadius(BlockSpacePosition position, int distance)
        {
            foreach (Chunk chunk in Instance().IterateChunksWithinRadius(position, distance)) {
                yield return chunk;
            }
        }


        /// <summary>
        /// Dump the list of chunks that need processing into the debug output.
        /// </summary>
        public static void DumpProcessingChunkListDebugData()
        {
            Instance().DumpProcessingChunkListDebugData();
        }
    }

    public sealed class ChunkRepositoryImplementation : IService {
        private class PriorityComparer : IComparer<int> {
            public int Compare(int x, int y)
            {
                return y.CompareTo(x);
            }
        }

        private List<Chunk> chunks;
        private Chunk[,,] chunkMap;
        private int[] chunkMapSize;
        private SortedList<int, Chunk> processingChunkList;
        private List<Chunk> addToProcessingChunkList;
        private int processingChunkListSortIndex = 0;
        int chunkArrayHeight;

        public ChunkRepositoryImplementation ()
        {
            chunks = new List<Chunk>();
            int loopPadding = 8;
            chunkMapSize = new int[3] {Configuration.CHUNK_VIEW_DISTANCE * 2 + 1 + loopPadding, 
                                       Configuration.HEIGHT / Chunk.SIZE,
                                       Configuration.CHUNK_VIEW_DISTANCE * 2 + 1 + loopPadding};
            chunkMap = new Chunk[chunkMapSize[0], chunkMapSize[1], chunkMapSize[2]];

            addToProcessingChunkList = new List<Chunk>(1000);
            processingChunkList = new SortedList<int, Chunk>(2000, new PriorityComparer());
        }
        
        public void FlushModifications()
        {
            int chunkCount = chunks.Count;
            for (int i = 0; i < chunkCount; i++) {
                chunks[i].FlushModifications();
            }
        }

#region ProcessingChunkList
        public void AddToProcessingChunkList(Chunk chunk)
        {
            if (!chunk.IsUnloading()) {
                chunk.SetInChunkProcessingListCacheFlag();
                lock (processingChunkList) {
                    if (processingChunkList.ContainsValue(chunk)) {
                        return;
                    }
                }

                lock (addToProcessingChunkList) {
                    if (addToProcessingChunkList.Contains(chunk)) {
                        return;
                    }
                    addToProcessingChunkList.Add(chunk);
                }
            }
        }
        
        public void RemoveFromProcessingChunkList(Chunk chunk)
        {
            lock (addToProcessingChunkList) {
                lock (processingChunkList) {
                    while (addToProcessingChunkList.Contains(chunk)) {
                        addToProcessingChunkList.Remove(chunk);
                    }

                    while (processingChunkList.ContainsValue(chunk)) {
                        processingChunkList.RemoveAt(processingChunkList.IndexOfValue(chunk));
                    }
                }
            }

            chunk.ClearInChunkProcessingListCacheFlag();
        }
        
        public void FlushProcessingChunkListModifications()
        {
            lock (addToProcessingChunkList) {
                lock (processingChunkList) {
                    for (int i = 0; i < addToProcessingChunkList.Count; i++) {
                        Chunk addChunk = addToProcessingChunkList[i];

                        Vector3 position;
                        position.x = addChunk.WorldPosition().x * Chunk.SIZE;
                        position.y = addChunk.WorldPosition().y * Chunk.SIZE;
                        position.z = addChunk.WorldPosition().z * Chunk.SIZE;
                        int priority = RenderService.PriorityRelativeToCamera(position);
                        while (processingChunkList.ContainsKey(priority)) {
                            priority++;
                        }
                        processingChunkList.Add(priority, addChunk);
                    }
                }

                addToProcessingChunkList.Clear();
            }
        }

        public Chunk GetProcessingChunk(int index)
        {
            Chunk returnChunk = null;
            lock (processingChunkList) {
                if (index < processingChunkList.Count) {
                    returnChunk = processingChunkList.Values[index];
                }
            }
            return returnChunk;
        }
        
        public int GetProcessingChunkListSize()
        {
            int returnCount = 0;
            lock (processingChunkList) {
                returnCount = processingChunkList.Count;
            }
            return returnCount;
        }

        public void RePrioritizeProcessingChunkList()
        {
            lock (processingChunkList) {
                if (processingChunkListSortIndex >= processingChunkList.Count) {
                    processingChunkListSortIndex = 0;
                }
                
                int startIndex = processingChunkListSortIndex;
                while (processingChunkListSortIndex < processingChunkList.Count &&
                      processingChunkListSortIndex < startIndex + Configuration.PERFORMANCE_MAX_PROCESSING_CHUNK_LIST_REPRIORITIZE_BATCH) {

                    Chunk chunk = processingChunkList.Values[processingChunkListSortIndex];

                    Vector3 position;
                    position.x = chunk.WorldPosition().x * Chunk.SIZE;
                    position.y = chunk.WorldPosition().y * Chunk.SIZE;
                    position.z = chunk.WorldPosition().z * Chunk.SIZE;
                    int priority = RenderService.PriorityRelativeToCamera(position);

                    while (processingChunkList.ContainsKey(priority)) {
                        priority++;
                    }
                    
                    processingChunkList.RemoveAt(processingChunkListSortIndex);
                    processingChunkList.Add(priority, chunk);
                    processingChunkListSortIndex++;
                }
            }
        }

        public void DumpProcessingChunkListDebugData()
        {
            lock (processingChunkList) {
                for (int i = 0; i < processingChunkList.Count; i++) {
                    Chunk processingChunk = processingChunkList.Values[i];

                    String position = String.Format("({0},{1},{2})", processingChunk.WorldPosition().x,
                                                    processingChunk.WorldPosition().y, processingChunk.WorldPosition().z);
                    bool isUnloading = processingChunk.IsUnloading();
                    bool inChunkList;
                    lock (chunks) {
                        inChunkList = chunks.Contains(processingChunk);
                    }

                    ChunkLoadState state = processingChunk.GetLoadState();

                    Debug.Log(String.Format("{0} - state: {1} - unloading: {2} - in chunk list: {3}", position, state, isUnloading, inChunkList));
                }
            }
        }

#endregion

#region BlockAccessors
        public ChunkBlockPair GetBlockAtPosition(Vector3 position, Chunk hintChunk)
        {
            BlockSpacePosition blockspacePosition = BlockSpacePosition.CreateFromVector3(position);
            return GetBlockAtPosition(blockspacePosition, hintChunk);
        }
        
        public Block GetBlockAtPosition(Vector3 position)
        {
            BlockSpacePosition blockspacePosition = BlockSpacePosition.CreateFromVector3(position);
            return GetBlockAtPosition(blockspacePosition).block;
        }
        
        public ChunkBlockPair GetBlockAtPosition(BlockSpacePosition position)
        {
            ChunkBlockPair returnPair;
            Chunk chunk = GetChunkAtPosition(position);
            if (chunk == null) {
                returnPair.block = Block.EmptyBlock();
                returnPair.chunk = null;
                return returnPair;
            }
            
            returnPair.block = chunk.GetBlockAtPosition(position);
            returnPair.chunk = chunk;
            return returnPair;
        }
        
        public ChunkBlockPair GetBlockAtPosition(BlockSpacePosition position, Chunk hintChunk)
        {
            if (hintChunk == null) {
                return GetBlockAtPosition(position);
            }

            ChunkSubspacePosition subspacePosition = position.GetChunkSubspacePosition(hintChunk);
            if (subspacePosition.x < 0 ||
                subspacePosition.y < 0 ||
                subspacePosition.z < 0 ||
                subspacePosition.x >= Chunk.SIZE ||
                subspacePosition.y >= Chunk.SIZE ||
                subspacePosition.z >= Chunk.SIZE) {

                return GetBlockAtPosition(position);
            }
            
            ChunkBlockPair returnPair;
            returnPair.block = hintChunk.GetBlock(subspacePosition);
            returnPair.chunk = hintChunk;
            return returnPair;
        }
        
        public void SetBlockAtPosition(Vector3 position, BlockType type)
        {
            BlockSpacePosition blockspacePosition = BlockSpacePosition.CreateFromVector3(position);
            SetBlockAtPosition(blockspacePosition, type, true);
        }
        
        public void SetBlockAtPosition(BlockSpacePosition position, BlockType type, bool triggerLightingUpdate)
        {
            Chunk chunk = GetChunkAtPosition(position);
            if (chunk == null) {
                return;
            }
            chunk.SetBlockAtPosition(position, type, triggerLightingUpdate);
            MarkForMeshUpdateWithinRadius(position, Configuration.MAX_LIGHT_RADIUS);
        }
        
        public void RemoveBlockAtPosition(Vector3 position, Chunk.RemoveBlockCallback callback)
        {
            BlockSpacePosition blockspacePosition = BlockSpacePosition.CreateFromVector3(position);
            RemoveBlockAtPosition(blockspacePosition, callback);
        }
        
        public void RemoveBlockAtPosition(BlockSpacePosition position, Chunk.RemoveBlockCallback callback)
        {
            Chunk chunk = GetChunkAtPosition(position);
            if (chunk == null) {
                return;
            }
            chunk.RemoveBlockAtPosition(position, callback);
            MarkForMeshUpdateWithinRadius(position, Configuration.MAX_LIGHT_RADIUS);
        }
        
        public void RemoveBlocksWithinRadius(Vector3 origin, float radius, Chunk.RemoveBlockCallback callback)
        {
            foreach (Vector3 blockPosition in IterateBlockPositionsWithinRadius(origin, radius)) {
                RemoveBlockAtPosition(blockPosition, callback);
            }
        }
#endregion

#region ChunkAccessors
        public int NumberOfChunks()
        {
            return chunks.Count;
        }

        public Chunk GetChunkAtIndex(int index)
        {
            return chunks[index];
        }

        public void SetChunkAtPosition(ChunkSpacePosition position, Chunk chunk)
        {
            int indexY = position.y;

            int indexX = position.x % chunkMapSize[0];
            while (indexX < 0) {
                indexX += chunkMapSize[0];
            }

            int indexZ = position.z % chunkMapSize[2];
            while (indexZ < 0) {
                indexZ += chunkMapSize[2];
            }

            chunkMap[indexX, indexY, indexZ] = chunk;
        }
        
        public Chunk GetChunkAtPosition(ChunkSpacePosition position)
        {
            if (position.y < 0 || position.y >= chunkMapSize[1]) {
                return null;
            }
            Chunk returnChunk = null;
            int indexY = position.y;

            int indexX = position.x % chunkMapSize[0];
            while (indexX < 0) {
                indexX += chunkMapSize[0];
            }

            int indexZ = position.z % chunkMapSize[2];
            while (indexZ < 0) {
                indexZ += chunkMapSize[2];
            }

            returnChunk = chunkMap[indexX, indexY, indexZ];
            return returnChunk;
        }
        
        public Chunk GetChunkAtPosition(Vector3 position)
        {
            BlockSpacePosition blockspacePosition = BlockSpacePosition.CreateFromVector3(position);
            return GetChunkAtPosition(blockspacePosition);
        }
        
        public Chunk GetChunkAtPosition(BlockSpacePosition position)
        {
            ChunkSpacePosition chunkspacePosition;
            chunkspacePosition.y = position.y / Chunk.SIZE;
            chunkspacePosition.x = (int)Math.Floor((double)position.x / (double)Chunk.SIZE);
            chunkspacePosition.z = (int)Math.Floor((double)position.z / (double)Chunk.SIZE);

            return GetChunkAtPosition(chunkspacePosition);
        }
        
        public void RemoveChunkFromList(Chunk chunk)
        {
            chunks.Remove(chunk);
        }
        
        public void AddChunkToList(Chunk chunk)
        {
            chunks.Add(chunk);
        }
        
        public IEnumerable IterateBlockPositionsWithinRadius(Vector3 origin, float radius)
        {
            Vector3 testPosition;
            float ittrX, ittrY, ittrZ;
            for (ittrX = origin.x - radius; ittrX < origin.x + radius; ittrX += 1) {
                for (ittrY = origin.y - radius; ittrY < origin.y + radius; ittrY += 1) {
                    for (ittrZ = origin.z - radius; ittrZ < origin.z + radius; ittrZ += 1) {
                        testPosition.x = ittrX;
                        testPosition.y = ittrY;
                        testPosition.z = ittrZ;
                        if (Vector3.Distance(testPosition, origin) <= radius) {
                            yield return testPosition;
                        }
                    }
                }
            }
        }
        
        public IEnumerable IterateChunksWithinRadius(BlockSpacePosition position, int distance)
        {
            ChunkSpacePosition checkPosition;
            int xIttr, yIttr, zIttr;
            for (xIttr = (int)((position.x - distance) / Chunk.SIZE);
                xIttr < (int)Math.Ceiling((position.x + distance) / (double)Chunk.SIZE);
                xIttr += 1) {
                checkPosition.x = xIttr;
                for (yIttr = (int)((position.y - distance) / Chunk.SIZE);
                    yIttr < (int)Math.Ceiling((position.y + distance) / (double)Chunk.SIZE);
                    yIttr += 1) {
                    checkPosition.y = yIttr;
                    for (zIttr = (int)((position.z - distance) / Chunk.SIZE);
                        zIttr < (int)Math.Ceiling((position.z + distance) / (double)Chunk.SIZE);
                        zIttr += 1) {
                        checkPosition.z = zIttr;
                        
                        Chunk additionalChunk = GetChunkAtPosition(checkPosition);
                        if (additionalChunk != null) {
                            yield return additionalChunk;
                        }
                    }
                }
            }
        }
#endregion

#region ChunkSaving
        public void SaveAllLoadedChunks()
        {
            for (int x = 0; x < chunkMapSize[0]; x++) {
                for (int y = 0; y < chunkMapSize[1]; y++) {
                    for (int z = 0; z < chunkMapSize[2]; z++) {
                        Chunk chunk = chunkMap[x, y, z];
                        if (chunk != null && chunk.HasLoadedInitialBlockData() && chunk.IsDirty()) {
                            chunk.FlushModifications();
                            chunk.Save();
                        }
                    }
                }
            }
        }
#endregion

#region PrivateApi
        private void MarkForMeshUpdateWithinRadius(BlockSpacePosition position, int distance)
        {
            foreach (Chunk updateChunk in IterateChunksWithinRadius(position, distance)) {
                updateChunk.MarkForMeshUpdate();
            }
        }
#endregion
    }
}

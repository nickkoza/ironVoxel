// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

//#define DEBUG
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;

using ironVoxel;
using ironVoxel.Render;
using ironVoxel.Gameplay;
using ironVoxel.Service;
using ironVoxel.Pool;
using ironVoxel.Asynchronous;

namespace ironVoxel.Domain {
    public enum ChunkLoadState {
        LoadingFromDisk,
        
        WaitingToGenerateBlocks,
        BlocksGenerating,
        BlockGenerationComplete,

        WaitingForMeshUpdate,
        MeshCalculating,
        MeshCalculationComplete,
        Done
    }
    
    public sealed class Chunk {
        private struct BlockModification {
            public ChunkSubspacePosition position;
            public BlockDefinition definition;
        }
        
        private struct AdjacentTransparencyModification {
            public ChunkSubspacePosition position;
            public int xOffset;
            public int yOffset;
            public int zOffset;
            public bool transparent;
        }

        // The chunk size should always be a power of 2
        public static readonly ushort SIZE = 16;
        private object padlock;
        private bool unload;
        private bool visible;
        public Texture texture;
        private ChunkSpacePosition worldPosition;
        volatile private Block[,,] blocks;
        volatile private ChunkLoadState loadState;
        volatile private bool needsMeshUpdate;
        private ChunkMeshCluster chunkMeshCluster;
        private BatchProcessor.WorkFunction generateBlocksWorkFunction;
        volatile private List<BlockLight> lights;
        volatile private Queue<BlockModification> modificationList;
        volatile private Queue<AdjacentTransparencyModification> adjacentTransparencyModificationList;
        private WorldGenerator worldGenerator;
        private bool[,] isShorelineCache;
        volatile private bool isInChunkProcessingList;
        private long processingStartTime;
        volatile private bool dirty;
        volatile private List<Model> models;
        volatile private object generatingModelsLock;
        
        public delegate void RemoveBlockCallback(BlockSpacePosition position, Block block);
        
        public Chunk (ChunkSpacePosition worldPosition)
        {
            padlock = new object();

            unload = false;
            visible = true;
            dirty = true;

            models = new List<Model>();
            generatingModelsLock = new object();

            worldGenerator = new WorldGenerator();
            isShorelineCache = new bool[SIZE, SIZE];

            generateBlocksWorkFunction = new BatchProcessor.WorkFunction(GenerateBlocksThread);

            chunkMeshCluster = null;

            SetLoadState(ChunkLoadState.LoadingFromDisk);
            needsMeshUpdate = false;

            lights = new List<BlockLight>();
            modificationList = new Queue<BlockModification>();
            adjacentTransparencyModificationList = new Queue<AdjacentTransparencyModification>(2000);
            
            // Get the instance variables ready
            blocks = new Block[SIZE, SIZE, SIZE];
            int xIttr, yIttr, zIttr;
            for (xIttr = 0; xIttr < SIZE; xIttr++) {
                for (yIttr = 0; yIttr < SIZE; yIttr++) {
                    for (zIttr = 0; zIttr < SIZE; zIttr++) {
                        blocks[xIttr, yIttr, zIttr] = new Block(BlockType.Air);
                    }
                }
            }
            this.worldPosition = worldPosition;

            isInChunkProcessingList = false;
        }

#region WORLD_INTERACTION
        private bool IsInChunkProcessingList()
        {
            return isInChunkProcessingList;
        }

        public long TimeSpentInProcessingList()
        {
            return (DateTime.Now.Ticks - processingStartTime) / 1000000000; // In seconds
        }

        private void PutInChunkProcessingList()
        {
            if (!isInChunkProcessingList) {
                ChunkRepository.AddToProcessingChunkList(this);
            }
        }

        public void SetInChunkProcessingListCacheFlag()
        {
            isInChunkProcessingList = true;
            processingStartTime = DateTime.Now.Ticks;
        }

        public void ClearInChunkProcessingListCacheFlag()
        {
            isInChunkProcessingList = false;
        }
#endregion

#region BLOCK_INTERACTION

        public Block GetBlockAtPosition(BlockSpacePosition position)
        {
            ChunkSubspacePosition subspacePosition = position.GetChunkSubspacePosition(this);
            return GetBlock(subspacePosition);
        }
        
        public Block GetBlock(ChunkSubspacePosition position)
        {
#if DEBUG
            if (position.x < 0) { UnityEngine.Debug.LogWarning(String.Format("Chunk.GetBlock - X is below 0: {0}", position.x)); }
            else if (position.x >= SIZE) { UnityEngine.Debug.LogWarning(String.Format("Chunk.GetBlock - X is above max: {0}", position.x)); }
            
            if (position.y < 0) { UnityEngine.Debug.LogWarning("Chunk.GetBlock - Y is below 0."); }
            else if (position.y >= SIZE) { UnityEngine.Debug.LogWarning("Chunk.GetBlock - Y is above max."); }
            
            if (position.z < 0) { UnityEngine.Debug.LogWarning(String.Format("Chunk.GetBlock - Z is below 0: {0}", position.z)); }
            else if (position.z >= SIZE) { UnityEngine.Debug.LogWarning(String.Format("Chunk.GetBlock - Z is above max: {0}", position.z)); }
#endif
            
            
            Block returnBlock;
            lock (padlock) {
                if (position.x < 0 || position.x >= SIZE || position.y < 0 || position.y >= SIZE ||
                    position.z < 0 || position.z >= SIZE) {
                    return Block.EmptyBlock();
                }
                
                returnBlock = blocks[position.x, position.y, position.z];
            }
            return returnBlock;
        }
        
        public Block GetBlock(int x, int y, int z)
        {
            ChunkSubspacePosition position;
            position.x = x;
            position.y = y;
            position.z = z;
            return GetBlock(position);
        }
        
        public BlockLight[] LightsArray()
        {
            BlockLight[] lightArray;
            lock (padlock) {
                lightArray = lights.ToArray();
            }
            return lightArray;
        }
        
        private void SetBlock(ChunkSubspacePosition position, BlockType type, bool triggerLightingUpdate)
        {
            SetBlock(position, BlockDefinition.DefinitionOfType(type), triggerLightingUpdate);
        }
        
        private void SetBlock(ChunkSubspacePosition position, BlockDefinition definition, bool triggerLightingUpdate)
        {
            lock (padlock) {
                if (position.x < 0 || position.x >= SIZE || position.y < 0 || position.y >= SIZE ||
                    position.z < 0 || position.z >= SIZE) {
                    return;
                }

                if (triggerLightingUpdate) {
                    BlockSpacePosition blockPosition = position.GetBlockSpacePosition(this);
                    RenderService.MarkChunksWithinMaxLightRadiusForMeshUpdate(blockPosition);
                }
                else {
                    PutInChunkProcessingList();
                }

                dirty = true;
                if (MeshGenerationIsInProgress()) {
                    BlockModification modification;
                    modification.position = position;
                    modification.definition = definition;
                    modificationList.Enqueue(modification);
                }
                else {
                    BlockDefinition prevDefinition;
                    prevDefinition = blocks[position.x, position.y, position.z].GetDefinition();
                    blocks[position.x, position.y, position.z].Set(definition);
                    if (definition.IsActive() == false) {
                        FlushBlockRemoval(position, definition, prevDefinition);
                    }
                    else {
                        FlushBlockSet(position, definition, prevDefinition);
                    }
                }
            }
        }
        
        public void FlushModifications()
        {
            if (Monitor.TryEnter(padlock)) {
                if (MeshGenerationIsInProgress() == false) {
                    while (modificationList.Count > 0 &&
                          AsyncService.FrameElapsedPercentageIsNotExceeded(Configuration.PERFORMANCE_FLUSH_MODIFICATIONS_DEADLINE)) {

                        BlockModification modification = modificationList.Dequeue();
                        BlockDefinition prevDefinition =
                            blocks[modification.position.x, modification.position.y, modification.position.z].GetDefinition();
                        blocks[modification.position.x, modification.position.y, modification.position.z]
                            .Set(modification.definition);
                        
                        if (modification.definition.IsActive() == false) {
                            FlushBlockRemoval(modification.position, modification.definition, prevDefinition);
                        }
                        else {
                            FlushBlockSet(modification.position, modification.definition, prevDefinition);
                        }
                    }

                    AdjacentTransparencyModification transparencyModification;
                    if (Monitor.TryEnter(adjacentTransparencyModificationList)) {
                        while (adjacentTransparencyModificationList.Count > 0 &&
                              AsyncService.FrameElapsedPercentageIsNotExceeded(Configuration.PERFORMANCE_FLUSH_MODIFICATIONS_DEADLINE)) {

                            transparencyModification = adjacentTransparencyModificationList.Dequeue();
                            SetAdjacentBlockTransparentFlag(transparencyModification.position,
                                transparencyModification.xOffset, transparencyModification.yOffset, transparencyModification.zOffset,
                                transparencyModification.transparent);
                        }
                        Monitor.Exit(adjacentTransparencyModificationList);
                    }
                }
                Monitor.Exit(padlock);
            }
        }
        
        public void RemoveBlockAtPosition(BlockSpacePosition position, RemoveBlockCallback callback)
        {
            ChunkSubspacePosition subspacePosition = position.GetChunkSubspacePosition(this);
            
            if (subspacePosition.x < 0 || subspacePosition.y < 0 || subspacePosition.z < 0 ||
                subspacePosition.x >= SIZE || subspacePosition.y >= SIZE || subspacePosition.z >= SIZE) {
                return;
            }
            
            Block block = GetBlock(subspacePosition);
            if (block.IsBedrock()) {
                // Bedrock is a special case - it can't be removed
                return;
            }
            
            if (callback != null) {
                callback(position, block);
            }
            
            SetBlock(subspacePosition, BlockType.Air, true);
        }
        
        private void FlushBlockRemoval(ChunkSubspacePosition position, BlockDefinition definition,
            BlockDefinition prevDefinition)
        {
            
            PushTransparencyCacheToAdjacentBlocks(position, definition.IsTransparent());
            
            if (prevDefinition.IsLightEmitter()) {
                BlockLight light;
                light.chunk = this;
                light.chunkPosition = position;
                light.blockDefinition = prevDefinition;
                lights.Remove(light);
            }
            
            needsMeshUpdate = true;
        }
        
        public void MarkForMeshUpdate()
        {
            needsMeshUpdate = true;
        }

        public bool NeedsMeshUpdate()
        {
            return needsMeshUpdate;
        }
        
        public void SetBlockAtPosition(BlockSpacePosition position, BlockType type, bool triggerLightingUpdate)
        {
            if (type == BlockType.Air) {
                throw new Exception("Can not use SetBlockAtPosition to set a block to air. Rather, use " +
                    "RemoveBlockAtPosition for this purpose.");
            }
            
            ChunkSubspacePosition subspacePosition = position.GetChunkSubspacePosition(this);
            SetBlock(subspacePosition, type, triggerLightingUpdate);
        }
        
        private void FlushBlockSet(ChunkSubspacePosition position, BlockDefinition definition,
            BlockDefinition prevDefinition)
        {
            
            if (prevDefinition.IsTransparent() != definition.IsTransparent()) {
                PushTransparencyCacheToAdjacentBlocks(position, definition.IsTransparent());
            }
            
            MarkForMeshUpdate();
            
            if (definition.IsLightEmitter()) {
                BlockLight light;
                light.chunk = this;
                light.chunkPosition = position;
                light.blockDefinition = definition;
                lights.Add(light);
            }
        }

        private Chunk GetAdjacentChunk(int xOffset, int yOffset, int zOffset)
        {
            ChunkSpacePosition position = worldPosition;
            position.x += xOffset;
            position.y += yOffset;
            position.z += zOffset;
            return ChunkRepository.GetChunkAtPosition(position);
        }

#endregion

#region TRANSPARENCY_CACHING
        private void LoadTransparencyCache()
        {
            ChunkSubspacePosition position;
            for (position.x = 0; position.x < SIZE; position.x++) {
                for (position.z = 0; position.z < SIZE; position.z++) {
                    for (position.y = 0; position.y < SIZE; position.y++) {
                        PullTransparencyCacheFromAdjacentBlocks(position);
                        
                        // If this is along an outside edge, push to any surrounding chunks
                        if (position.x == 0 || position.x == SIZE - 1 ||
                            position.y == 0 || position.y == SIZE - 1 ||
                            position.z == 0 || position.z == SIZE - 1) {
                            
                            PushTransparencyCacheToAdjacentBlocks(position, GetBlock(position).IsTransparent());
                        }
                    }
                }
            }
        }
        
        private void PushTransparencyCacheToAdjacentBlocks(ChunkSubspacePosition position, bool transparent)
        {
            PushTransparencyCacheToAdjacentBlock(position, -1, -1, -1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 0, -1, -1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 1, -1, -1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, -1, 0, -1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 0, 0, -1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 1, 0, -1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, -1, 1, -1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 0, 1, -1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 1, 1, -1, transparent);
            
            PushTransparencyCacheToAdjacentBlock(position, -1, -1, 0, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 0, -1, 0, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 1, -1, 0, transparent);
            PushTransparencyCacheToAdjacentBlock(position, -1, 0, 0, transparent);
            
            PushTransparencyCacheToAdjacentBlock(position, 1, 0, 0, transparent);
            PushTransparencyCacheToAdjacentBlock(position, -1, 1, 0, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 0, 1, 0, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 1, 1, 0, transparent);
            
            PushTransparencyCacheToAdjacentBlock(position, -1, -1, 1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 0, -1, 1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 1, -1, 1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, -1, 0, 1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 0, 0, 1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 1, 0, 1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, -1, 1, 1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 0, 1, 1, transparent);
            PushTransparencyCacheToAdjacentBlock(position, 1, 1, 1, transparent);
        }

        private void PushTransparencyCacheToAdjacentBlock(ChunkSubspacePosition position, int xOffset, int yOffset, int zOffset,
                                                          bool transparent)
        {
            
            position.x += xOffset;
            position.y += yOffset;
            position.z += zOffset;
            
            if (position.x >= 0 && position.x < SIZE &&
                position.y >= 0 && position.y < SIZE &&
                position.z >= 0 && position.z < SIZE) {
                
                SetAdjacentBlockTransparentFlag(position, -xOffset, -yOffset, -zOffset, transparent);
            }
            else {
                BlockSpacePosition blockPosition = position.GetBlockSpacePosition(this);
                Chunk otherChunk = ChunkRepository.GetChunkAtPosition(blockPosition);
                
                if (otherChunk != null) {
                    ChunkSubspacePosition otherChunkPosition = blockPosition.GetChunkSubspacePosition(otherChunk);
                    
                    otherChunk.SetAdjacentBlockTransparentFlag(otherChunkPosition,
                                                               -xOffset, -yOffset, -zOffset, transparent);
                }
            }
        }
        
        private void PullTransparencyCacheFromAdjacentBlocks(ChunkSubspacePosition position)
        {
            PullTransparencyCacheFromAdjacentBlock(position, -1, -1, -1);
            PullTransparencyCacheFromAdjacentBlock(position, 0, -1, -1);
            PullTransparencyCacheFromAdjacentBlock(position, 1, -1, -1);
            PullTransparencyCacheFromAdjacentBlock(position, -1, 0, -1);
            PullTransparencyCacheFromAdjacentBlock(position, 0, 0, -1);
            PullTransparencyCacheFromAdjacentBlock(position, 1, 0, -1);
            PullTransparencyCacheFromAdjacentBlock(position, -1, 1, -1);
            PullTransparencyCacheFromAdjacentBlock(position, 0, 1, -1);
            PullTransparencyCacheFromAdjacentBlock(position, 1, 1, -1);
            
            PullTransparencyCacheFromAdjacentBlock(position, -1, -1, 0);
            PullTransparencyCacheFromAdjacentBlock(position, 0, -1, 0);
            PullTransparencyCacheFromAdjacentBlock(position, 1, -1, 0);
            PullTransparencyCacheFromAdjacentBlock(position, -1, 0, 0);
            
            PullTransparencyCacheFromAdjacentBlock(position, 1, 0, 0);
            PullTransparencyCacheFromAdjacentBlock(position, -1, 1, 0);
            PullTransparencyCacheFromAdjacentBlock(position, 0, 1, 0);
            PullTransparencyCacheFromAdjacentBlock(position, 1, 1, 0);
            
            PullTransparencyCacheFromAdjacentBlock(position, -1, -1, 1);
            PullTransparencyCacheFromAdjacentBlock(position, 0, -1, 1);
            PullTransparencyCacheFromAdjacentBlock(position, 1, -1, 1);
            PullTransparencyCacheFromAdjacentBlock(position, -1, 0, 1);
            PullTransparencyCacheFromAdjacentBlock(position, 0, 0, 1);
            PullTransparencyCacheFromAdjacentBlock(position, 1, 0, 1);
            PullTransparencyCacheFromAdjacentBlock(position, -1, 1, 1);
            PullTransparencyCacheFromAdjacentBlock(position, 0, 1, 1);
            PullTransparencyCacheFromAdjacentBlock(position, 1, 1, 1);
        }
        
        private void PullTransparencyCacheFromAdjacentBlock(ChunkSubspacePosition position,
                                                            int xOffset, int yOffset, int zOffset)
        {
            ChunkSubspacePosition checkPosition;
            checkPosition = position;
            checkPosition.x += xOffset;
            checkPosition.y += yOffset;
            checkPosition.z += zOffset;
            
            if (checkPosition.x >= 0 && checkPosition.x < SIZE &&
                checkPosition.y >= 0 && checkPosition.y < SIZE &&
                checkPosition.z >= 0 && checkPosition.z < SIZE) {
                
                SetAdjacentBlockTransparentFlag(position, xOffset, yOffset, zOffset,
                                                GetBlock(checkPosition).IsTransparent());
            }
            else {
                BlockSpacePosition blockPosition = checkPosition.GetBlockSpacePosition(this);
                Chunk otherChunk = ChunkRepository.GetChunkAtPosition(blockPosition);
                
                if (otherChunk != null) {
                    ChunkSubspacePosition otherChunkPosition = blockPosition.GetChunkSubspacePosition(otherChunk);
                    
                    SetAdjacentBlockTransparentFlag(position, xOffset, yOffset, zOffset,
                                                    otherChunk.GetBlock(otherChunkPosition).IsTransparent());
                }
            }
        }
        
        public void QueueAdjacentBlockTransparencyModification(ChunkSubspacePosition position,
                                                               int xOffset, int yOffset, int zOffset, bool transparent)
        {
            
            AdjacentTransparencyModification modification;
            modification.position = position;
            modification.xOffset = xOffset;
            modification.yOffset = yOffset;
            modification.zOffset = zOffset;
            modification.transparent = transparent;
            lock (adjacentTransparencyModificationList) {
                adjacentTransparencyModificationList.Enqueue(modification);
            }
        }
        
        private void SetAdjacentBlockTransparentFlag(ChunkSubspacePosition position,
                                                     int xOffset, int yOffset, int zOffset, bool transparent)
        {
            if (position.x < 0 || position.x >= SIZE ||
                position.y < 0 || position.y >= SIZE ||
                position.z < 0 || position.z >= SIZE) {
                return;
            }

            PutInChunkProcessingList();
            
            if (Monitor.TryEnter(padlock)) {
                blocks[position.x, position.y, position.z]
                .SetAdjacentBlockTransparentFlag(xOffset, yOffset, zOffset, transparent);
                Monitor.Exit(padlock);
            }
            else {
                // Else, could not get the lock - queue to try again later
                QueueAdjacentBlockTransparencyModification(position, xOffset, yOffset, zOffset, transparent);
            }
        }
#endregion

#region LIGHTING

        private void GatherLights()
        {
            // This is an extremely slow way of doing this and should only be used in special cases, like when a chunk is
            // first loaded off the disk.
            lock (padlock) {
                lights.Clear();
                ChunkSubspacePosition position;
                for (position.x = 0; position.x < SIZE; position.x++) {
                    for (position.y = 0; position.y < SIZE; position.y++) {
                        for (position.z = 0; position.z < SIZE; position.z++) {
                            Block checkBlock = GetBlock(position);
                            if (checkBlock.IsLightEmitter()) {
                                BlockLight light;
                                light.chunkPosition = position;
                                light.chunk = this;
                                light.blockDefinition = checkBlock.GetDefinition();
                                lights.Add(light);
                            }
                        }
                    }
                }
            }
        }

#endregion

#region BLOCK_GENERATION
        public void GenerateBlocks(float seed)
        {
            if (WaitingToGenerateBlocks()) {
                SetLoadState(ChunkLoadState.BlocksGenerating);

                Vector3 position;
                position.x = worldPosition.x * SIZE;
                position.y = worldPosition.y * SIZE;
                position.z = worldPosition.z * SIZE;
                AsyncService.GetCPUMediator().EnqueueBatchForProcessing(generateBlocksWorkFunction,
                    (object)this, CPUMediator.LOW_PRIORITY, position);
            }
        }
        
        void GenerateBlocksThread(object chunkInstance)
        {
            Chunk chunk = chunkInstance as Chunk;

            ChunkSubspacePosition position;
            BlockSpacePosition checkPosition;
            for (position.x = 0; position.x < SIZE; position.x++) {
                checkPosition.x = worldPosition.x * SIZE + position.x;
                for (position.z = 0; position.z < SIZE; position.z++) {
                    checkPosition.z = worldPosition.z * SIZE + position.z;
                    isShorelineCache[position.x, position.z] = true;
                    
                    for (position.y = worldPosition.y * SIZE + SIZE; position.y < Configuration.HEIGHT; position.y++) {
                        checkPosition.y = position.y;
                        Block checkBlock = ChunkRepository.GetBlockAtPosition(checkPosition);
                        if (checkBlock.IsActive() && checkBlock.IsNotTransparent()) {
                            isShorelineCache[position.x, position.z] = false;
                            break;
                        }
                    }
                }
            }
            
            BlockType[,,] blockTypes = worldGenerator.GenerateBlocks(chunk);

            for (position.x = 0; position.x < SIZE; position.x++) {
                for (position.y = 0; position.y < SIZE; position.y++) {
                    for (position.z = 0; position.z < SIZE; position.z++) {
                        BlockDefinition blockDefinition = BlockDefinition.DefinitionOfType(blockTypes[position.x, position.y, position.z]);
                        chunk.SetBlock(position, blockDefinition, false);
                        
                        if (blockDefinition.IsActive() && blockDefinition.IsNotTransparent() &&
                            blockDefinition.GetBlockType() != BlockType.Sand) {
                            isShorelineCache[position.x, position.z] = false;
                        }
                        
                        if (blockDefinition.IsLightEmitter()) {
                            BlockLight light;
                            light.chunk = this;
                            light.chunkPosition = position;
                            light.blockDefinition = blockDefinition;
                            lock (chunk.padlock) {
                                chunk.lights.Add(light);
                            }
                        }
                    }
                }
            }

            // Generate and apply the models from this and all nearby chunks
            List<Model> generatedModels = worldGenerator.GenerateModels(chunk);

            lock (generatingModelsLock) {
                for (int i = 0; i < generatedModels.Count; i++) {
                    AddModel(generatedModels[i]);
                }
            }

            List<Chunk> lockedChunks = LockNearbyChunkModels();
            ApplyModels();
            UnlockChunkModels(lockedChunks);

            // Cleanup pass
            for (position.x = 0; position.x < SIZE; position.x++) {
                for (position.z = 0; position.z < SIZE; position.z++) {
                    for (position.y = SIZE - 1; position.y >= 0; position.y--) {
                        chunk.GetBlock(position);

                        // If the block is water, make sure it's surrounded on the bottom and sides
                        if (chunk.GetBlock(position).IsWater()) {
                            for (int i = 0; i < 5; i++) {
                                ChunkSubspacePosition solidCheckPosition = position;
                                if (i == 0) {
                                    solidCheckPosition.y -= 1;
                                }
                                else if (i == 1) {
                                    solidCheckPosition.x -= 1;
                                }
                                else if (i == 2) {
                                    solidCheckPosition.x += 1;
                                }
                                else if (i == 3) {
                                    solidCheckPosition.z -= 1;
                                }
                                else if (i == 4) {
                                    solidCheckPosition.z += 1;
                                }

                                if (solidCheckPosition.x >= 0 && solidCheckPosition.x < SIZE &&
                                    solidCheckPosition.y >= 0 && solidCheckPosition.y < SIZE &&
                                    solidCheckPosition.z >= 0 && solidCheckPosition.z < SIZE) {

                                    if (chunk.GetBlock(solidCheckPosition).IsNotActive()) {
                                        chunk.SetBlock(solidCheckPosition, BlockType.Stone, false);
                                    }
                                }
                                else {
                                    BlockSpacePosition checkWorldPosition = solidCheckPosition.GetBlockSpacePosition(chunk);
                                    if (ChunkRepository.GetBlockAtPosition(checkWorldPosition).IsNotActive()) {
                                        ChunkRepository.SetBlockAtPosition(checkWorldPosition, BlockType.Stone, false);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            LoadTransparencyCache();

            chunk.SetLoadState(ChunkLoadState.BlockGenerationComplete);
        }
        
        public bool WaitingToGenerateBlocks()
        {
            return GetLoadState() == ChunkLoadState.WaitingToGenerateBlocks;
        }
        
        public bool BlocksAreGenerating()
        {
            return GetLoadState() == ChunkLoadState.BlocksGenerating;
        }
        
        public bool BlockGenerationIsComplete()
        {
            return GetLoadState() == ChunkLoadState.BlockGenerationComplete;
        }

        public bool HasLoadedInitialBlockData()
        {
            return GetLoadState() != ChunkLoadState.LoadingFromDisk &&
                GetLoadState() != ChunkLoadState.WaitingToGenerateBlocks &&
                GetLoadState() != ChunkLoadState.BlocksGenerating;
        }
#endregion

#region MODELS
        public void AddModel(Model model)
        {
            lock (padlock) {
                models.Add(model);
            }
        }

        private void ApplyModels()
        {
            lock (padlock) {
                for (int i = 0; i < models.Count; i++) {
                    Model model = models[i];
                    model.template.ApplyToWorld(model.position);
                }

                ApplyNearbyModels();
            }
        }

        private List<Chunk> LockNearbyChunkModels() {
            List<Chunk> lockedChunks = new List<Chunk>();
            int maxOffset = (int)Mathf.Ceil(Configuration.MAX_MODEL_RADIUS / (float)SIZE);
            int maxXIttr = worldPosition.x + maxOffset;
            int maxYIttr = worldPosition.y + maxOffset;
            int maxZIttr = worldPosition.z + maxOffset;
            for (int xIttr = worldPosition.x - maxOffset; xIttr <= maxXIttr; xIttr++) {
                for (int yIttr = worldPosition.y - maxOffset; yIttr <= maxYIttr; yIttr++) {
                    for (int zIttr = worldPosition.z - maxOffset; zIttr <= maxZIttr; zIttr++) {
                        
                        ChunkSpacePosition position;
                        position.x = xIttr;
                        position.y = yIttr;
                        position.z = zIttr;
                        Chunk chunk = ChunkRepository.GetChunkAtPosition(position);
                        if (chunk != null) {
                            Monitor.Enter(chunk.generatingModelsLock);
                            lockedChunks.Add(chunk);
                        }
                    }
                }
            }
            
            return lockedChunks;
        }

        private void UnlockChunkModels(List<Chunk> lockedChunks) {
            for (int i = 0; i < lockedChunks.Count; i++) {
                Monitor.Exit(lockedChunks[i].generatingModelsLock);
            }
        }

        private void ApplyNearbyModels() {
            int maxOffset = (int)Mathf.Ceil(Configuration.MAX_MODEL_RADIUS / (float)SIZE);
            int maxXIttr = worldPosition.x + maxOffset;
            int maxYIttr = worldPosition.y + maxOffset;
            int maxZIttr = worldPosition.z + maxOffset;
            for (int xIttr = worldPosition.x - maxOffset; xIttr <= maxXIttr; xIttr++) {
                for (int yIttr = worldPosition.y - maxOffset; yIttr <= maxYIttr; yIttr++) {
                    for (int zIttr = worldPosition.z - maxOffset; zIttr <= maxZIttr; zIttr++) {

                        ChunkSpacePosition position;
                        position.x = xIttr;
                        position.y = yIttr;
                        position.z = zIttr;
                        Chunk chunk = ChunkRepository.GetChunkAtPosition(position);
                        if (chunk != null) {
                            foreach (Model model in chunk.IterateModels()) {
                                model.template.ApplyToChunk(model.position, this);
                            }
                        }
                    }
                }
            }
        }

        private Model[] GetModels() {
            Model[] modelsArray;
            lock (padlock) {
                modelsArray = models.ToArray();
            }
            return modelsArray;
        }

        private System.Collections.IEnumerable IterateModels() {
            lock (padlock) {
                for (int i = 0; i < models.Count; i++) {
                    yield return models[i];
                }
            }
        }

#endregion

#region MESH

        public void GenerateMesh(Chunk northChunk, Chunk southChunk,
                                Chunk westChunk, Chunk eastChunk,
                                Chunk aboveChunk, Chunk belowChunk)
        {   
            if (chunkMeshCluster == null) {
                chunkMeshCluster = ChunkMeshClusterPool.Instance().GetChunkMeshCluster();
            }

            SetLoadState(ChunkLoadState.MeshCalculating);
            needsMeshUpdate = false;
            chunkMeshCluster.Setup(this);
            chunkMeshCluster.Generate(northChunk, southChunk, westChunk, eastChunk, aboveChunk, belowChunk);
        }
        
        public void IterateOnFinishingMeshGeneration()
        {
            if (chunkMeshCluster.chunk == this) {
                chunkMeshCluster.IterateOnFinishingMeshGeneration();
            }
        }
        
        public bool MeshCalculationIsFinished()
        {
            return GetLoadState() == ChunkLoadState.MeshCalculationComplete;
        }
        
        public void ClearMeshObject()
        {
            ChunkMeshClusterPool.Instance().ReturnChunkMeshCluster(chunkMeshCluster);
            chunkMeshCluster = null;
        }

        public void Show()
        {
            if (chunkMeshCluster != null && !visible) {
                chunkMeshCluster.Show();
            }
            visible = true;
        }

        public void Hide()
        {
            if (chunkMeshCluster != null && visible) {
                chunkMeshCluster.Hide();
            }
            visible = false;
        }

        public bool IsVisible()
        {
            return visible;
        }

#endregion

#region STATUS

        public ChunkLoadState GetLoadState()
        {
            return loadState;
        }

        public void SetLoadState(ChunkLoadState state)
        {
            loadState = state;
        }
        
        public void ClearAll()
        {
            lock (padlock) {
                modificationList.Clear();
                lock (adjacentTransparencyModificationList) {
                    adjacentTransparencyModificationList.Clear();
                }
                SetLoadState(ChunkLoadState.LoadingFromDisk);
                ClearMeshObject();
                lights.Clear();
                models.Clear();
                ChunkRepository.RemoveFromProcessingChunkList(this);
                unload = false;
            }
        }
        
        public bool ManipulationIsInProgress()
        {
            return (GetLoadState() == ChunkLoadState.BlocksGenerating ||
                GetLoadState() == ChunkLoadState.MeshCalculating);
        }
        
        public bool MeshGenerationIsInProgress()
        {
            return GetLoadState() == ChunkLoadState.MeshCalculating;
        }
        
        public ChunkSpacePosition WorldPosition()
        {
            return worldPosition;
        }
        
        public void SetWorldPosition(ChunkSpacePosition position)
        {
            worldPosition = position;
        }

        public bool IsFinishedProcessing()
        {
            return GetLoadState() == ChunkLoadState.Done;
        }

        public bool HasModifications()
        {
            bool hasModifications = false;
            lock (padlock) {
                hasModifications = modificationList.Count > 0 ||
                    adjacentTransparencyModificationList.Count > 0 ||
                    needsMeshUpdate;
            }
            return hasModifications;
        }

#endregion

#region SAVING_AND_LOADING
        // Returns true if it was successful in returning it to the pool
        public bool AttemptToUnload()
        {
            if (unload) {
                if (GetLoadState() == ChunkLoadState.LoadingFromDisk) {
                    return false;
                }

                if (GetLoadState() == ChunkLoadState.BlocksGenerating ||
                    GetLoadState() == ChunkLoadState.MeshCalculating) {
                    bool workRevoked = AsyncService.GetCPUMediator().CancelProcessingRequest(this);
                    if (!workRevoked) {
                        return false;
                    }
                }

                if (GetLoadState() != ChunkLoadState.WaitingToGenerateBlocks) {
                    Save();
                }

                GeneratorService.ReturnChunk(this);
                return true;
            }

            return false;
        }

        public void MarkForUnload()
        {
            unload = true;
        }

        public bool IsUnloading()
        {
            return unload;
        }

        public bool IsDirty() {
            return dirty;
        }

        public String FileID()
        {
            return String.Format("{0}_{1}_{2}", worldPosition.x.ToString(), worldPosition.y.ToString(), worldPosition.z.ToString());
        }
        
        public uint SaveFileVersion()
        {
            return 0;
        }
        
        public void Save()
        {
            if (!dirty) {
                return;
            }

            // Create the struct
            using (MemoryStream stream = new MemoryStream()) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(SaveFileVersion());
                    writer.Write(Chunk.SIZE);
                    BlockType currentType = BlockType.Air;
                    ushort typeCount = 0;

                    ChunkSubspacePosition position;
                    for (position.x = 0; position.x < SIZE; position.x++) {
                        for (position.y = 0; position.y < SIZE; position.y++) {
                            for (position.z = 0; position.z < SIZE; position.z++) {
                                Block block = GetBlock(position);
                                if (currentType != block.GetBlockType()) {
                                    if (typeCount > 0) {
                                        writer.Write(typeCount);
                                        writer.Write((byte)currentType);
                                        typeCount = 0;
                                    }
                                    currentType = block.GetBlockType();
                                }

                                typeCount++;
                            }
                        }
                    }
                    
                    if (typeCount > 0) {
                        writer.Write(typeCount);
                        writer.Write((int)currentType);
                        typeCount = 0;
                    }
                
                
                    // Save it
                    QueuedFileSave.SaveFinishedCallback callback = new QueuedFileSave.SaveFinishedCallback(SaveCallback);
                    QueuedFileChange change = new QueuedFileSave(FileID(), stream, callback, this);
                    FileRepository.Push(change);
                }
            }

            dirty = false;
        }
        
        public void SaveCallback(object chunkInstance)
        {
        }

        public void LoadOrGenerate()
        {
            QueuedFileLoad.LoadFinishedCallback callback = new QueuedFileLoad.LoadFinishedCallback(LoadCallback);
            QueuedFileChange change = new QueuedFileLoad(FileID(), callback, this);
            FileRepository.Push(change);
        }

        public void LoadCallback(String filePath, MemoryStream stream, object chunkInstance)
        {
            (chunkInstance as Chunk).LoadCallback(filePath, stream);
        }
        
        public void LoadCallback(String filePath, MemoryStream stream)
        {
            if (filePath != FileID()) {
                UnityEngine.Debug.LogError("Load callback triggered with an unexpected file loaded. Received: " +
                    filePath + ". Expected: " + FileID() + ".");
                return;
            }

            if (GetLoadState() != ChunkLoadState.LoadingFromDisk) {
                UnityEngine.Debug.LogError("Load callback triggered while the state machine is in an " +
                    "inappropriate state. Current state: " + GetLoadState() + ".");
                return;
            }
            
            if (stream == null) {
                SetLoadState(ChunkLoadState.WaitingToGenerateBlocks);
                return;
            }

            stream.Seek(0, SeekOrigin.Begin);
            // Don't use 'using' here - we want the stream to be saved so rather just let it be garbage collected
            BinaryReader reader = new BinaryReader(stream);
            uint version = reader.ReadUInt32();
            if (version != 0) {
                UnityEngine.Debug.LogError("Unexpected version found in chunk file: " + version + ".");
            }
            ushort chunkSize = reader.ReadUInt16();
            if (chunkSize != Chunk.SIZE) {
                UnityEngine.Debug.LogError("Unexpected chunk size found in chunk file: " + chunkSize + ".");
            }
            
            int typeCount = 0;
            BlockType currentType = BlockType.Air;
            ChunkSubspacePosition readerPosition;
            for (readerPosition.x = 0; readerPosition.x < SIZE; readerPosition.x++) {
                for (readerPosition.y = 0; readerPosition.y < SIZE; readerPosition.y++) {
                    for (readerPosition.z = 0; readerPosition.z < SIZE; readerPosition.z++) {
                        if (typeCount <= 0) {
                            typeCount = reader.ReadUInt16();
                            currentType = (BlockType)reader.ReadByte();
                        }
                        
                        SetBlock(readerPosition, currentType, false);
                        typeCount--;
                    }
                }
            }

            LoadTransparencyCache();
            GatherLights();
            needsMeshUpdate = true;
            dirty = false;
            SetLoadState(ChunkLoadState.WaitingForMeshUpdate);
        }
#endregion
    }
}

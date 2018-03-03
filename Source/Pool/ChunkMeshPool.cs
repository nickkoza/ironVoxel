// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using ironVoxel.Render;
using ironVoxel.Domain;

namespace ironVoxel.Pool {
    public sealed class ChunkMeshPool {
        private object padlock;
        private Stack<ChunkMesh> availableChunkMeshes;
        private uint availableChunkMeshCount;
        private uint totalChunkMeshCount;
        
        // Static interface
        private static ChunkMeshPool instance = null;

        public static void Initialize(int capacity)
        {
            if (instance == null) {
                instance = new ChunkMeshPool(capacity);
            }
        }
        
        public static ChunkMeshPool Instance()
        {
            if (instance == null) {
                Debug.Log("Getting null singleton");
            }
            return instance;
        }
        
        // Instance interface
        public ChunkMeshPool (int capacity)
        {
            padlock = new object();
            availableChunkMeshes = new Stack<ChunkMesh>(capacity);
            availableChunkMeshCount = 0;
            totalChunkMeshCount = 0;
            
            for (int i = 0; i < capacity; i++) {
                ReturnChunkMesh(CreateNewChunkMesh(null, RendererType.Solid));
            }
        }
        
        public ChunkMesh GetChunkMesh(ChunkMeshCluster chunkMeshCluster, RendererType rendererType)
        {
            lock (padlock) {
                if (ChunkMeshIsAvailable()) {
                    return GetExistingChunkMesh(chunkMeshCluster, rendererType);
                }
                else {
                    return CreateNewChunkMesh(chunkMeshCluster, rendererType);
                }
            }
        }
        
        public void ReturnChunkMesh(ChunkMesh chunkMesh)
        {
            if (chunkMesh == null) {
                return;
            }
            chunkMesh.ClearName();
            chunkMesh.ClearMeshes();
            
            lock (padlock) {
                availableChunkMeshes.Push(chunkMesh);
                availableChunkMeshCount++;
            }
        }
        
        private bool ChunkMeshIsAvailable()
        {
            return (availableChunkMeshCount > 0);
        }
        
        private ChunkMesh GetExistingChunkMesh(ChunkMeshCluster chunkMeshCluster, RendererType rendererType)
        {
            availableChunkMeshCount--;
            ChunkMesh returnChunkMesh = availableChunkMeshes.Pop();
            returnChunkMesh.Setup(chunkMeshCluster, rendererType);
            return returnChunkMesh;
        }
        
        private ChunkMesh CreateNewChunkMesh(ChunkMeshCluster chunkMeshCluster, RendererType rendererType)
        {
            totalChunkMeshCount++;
            Vector3 position;
            if (chunkMeshCluster != null && chunkMeshCluster.chunk != null) {
                position.x = chunkMeshCluster.chunk.WorldPosition().x * Chunk.SIZE;
                position.y = chunkMeshCluster.chunk.WorldPosition().y * Chunk.SIZE;
                position.z = chunkMeshCluster.chunk.WorldPosition().z * Chunk.SIZE;
            }
            else {
                position = Vector3.zero;
            }
            GameObject chunkMeshPrefab = UnityEngine.Object.Instantiate(World.Instance().getChunkMeshPrefab(), position, Quaternion.identity) as GameObject;
            ChunkMesh returnChunkMesh = chunkMeshPrefab.GetComponent("ChunkMesh") as ChunkMesh;
            returnChunkMesh.Setup(chunkMeshCluster, rendererType);
            return returnChunkMesh;
        }
    }
}
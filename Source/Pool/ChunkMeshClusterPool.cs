// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using ironVoxel.Render;

namespace ironVoxel.Pool {
    public sealed class ChunkMeshClusterPool {
        private object padlock;
        private Stack<ChunkMeshCluster> availableChunkMeshClusters;
        private uint availableChunkMeshClusterCount;
        private uint totalChunkMeshClusterCount;
        
        // Static interface
        private static ChunkMeshClusterPool instance = null;

        public static void Initialize(int capacity)
        {
            if (instance == null) {
                instance = new ChunkMeshClusterPool(capacity);
            }
        }
        
        public static ChunkMeshClusterPool Instance()
        {
            if (instance == null) {
                Debug.Log("Getting null singleton");
            }
            return instance;
        }
        
        // Instance interface
        public ChunkMeshClusterPool (int capacity)
        {
            padlock = new object();
            availableChunkMeshClusters = new Stack<ChunkMeshCluster>(capacity);
            availableChunkMeshClusterCount = 0;
            totalChunkMeshClusterCount = 0;
            
            for (int i = 0; i < capacity; i++) {
                ReturnChunkMeshCluster(CreateNewChunkMeshCluster());
            }
        }
        
        public ChunkMeshCluster GetChunkMeshCluster()
        {
            lock (padlock) {
                if (ChunkMeshClusterIsAvailable()) {
                    return GetExistingChunkMeshCluster();
                }
                else {
                    return CreateNewChunkMeshCluster();
                }
            }
        }
        
        public void ReturnChunkMeshCluster(ChunkMeshCluster chunkMeshCluster)
        {
            if (chunkMeshCluster == null) {
                return;
            }
            chunkMeshCluster.Clear();
            
            lock (padlock) {
                availableChunkMeshClusters.Push(chunkMeshCluster);
                availableChunkMeshClusterCount++;
            }
        }
        
        private bool ChunkMeshClusterIsAvailable()
        {
            return (availableChunkMeshClusterCount > 0);
        }
        
        private ChunkMeshCluster GetExistingChunkMeshCluster()
        {
            availableChunkMeshClusterCount--;
            ChunkMeshCluster returnChunkMeshCluster = availableChunkMeshClusters.Pop();
            return returnChunkMeshCluster;
        }
        
        private ChunkMeshCluster CreateNewChunkMeshCluster()
        {
            totalChunkMeshClusterCount++;
            ChunkMeshCluster returnChunkMeshCluster = new ChunkMeshCluster();
            return returnChunkMeshCluster;
        }
    }
}
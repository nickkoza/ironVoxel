// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using ironVoxel.Domain;

namespace ironVoxel.Pool {
    public sealed class ChunkPool {
        private object padlock;
        private Stack<Chunk> availableChunks;
        private uint availableChunkCount;
        private uint totalChunkCount;
        
        public ChunkPool (int capacity)
        {
            padlock = new object();
            availableChunks = new Stack<Chunk>(capacity);

            // Warm up the pool
            for (int i = 0; i < capacity; i++) {
                ReturnChunk(CreateNewChunk());
            }
        }
        
        public Chunk GetChunk(ChunkSpacePosition position)
        {
            lock (padlock) {
                Chunk returnChunk = null;
                if (ChunkIsAvailable()) {
                    returnChunk = GetExistingChunk();
                }
                else {
                    returnChunk = CreateNewChunk();
                }
                lock (returnChunk) {
                    returnChunk.SetWorldPosition(position);
                }
                return returnChunk;
            }
        }
        
        public void ReturnChunk(Chunk chunk)
        {
            if (chunk == null) {
                return;
            }
            lock (padlock) {
                chunk.ClearAll();
                availableChunks.Push(chunk);
                availableChunkCount++;
            }
        }

        public uint TotalChunkCount()
        {
            return totalChunkCount;
        }
        
        private bool ChunkIsAvailable()
        {
            return (availableChunkCount > 0);
        }
        
        private Chunk GetExistingChunk()
        {
            availableChunkCount--;
            return availableChunks.Pop();
        }
        
        private Chunk CreateNewChunk()
        {
            totalChunkCount++;
            ChunkSpacePosition position;
            position.x = 0;
            position.y = 0;
            position.z = 0;
            return new Chunk(position);
        }
    }
}

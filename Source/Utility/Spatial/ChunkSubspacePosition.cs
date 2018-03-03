// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ironVoxel.Domain;

namespace ironVoxel {
    public struct ChunkSubspacePosition {
        public int x;
        public int y;
        public int z;
        
        public ChunkSubspacePosition (int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        
        public Vector3 GetVector3(Chunk currentChunk)
        {
            return GetBlockSpacePosition(currentChunk).GetVector3();
        }
        
        public Vector3 GetSubspaceVector3()
        {
            Vector3 newPosition;
            newPosition.x = (int)x;
            newPosition.y = (int)y;
            newPosition.z = (int)z;
            return newPosition;
        }
        
        public BlockSpacePosition GetBlockSpacePosition(Chunk currentChunk)
        {
            if (currentChunk == null) {
                UnityEngine.Debug.LogWarning("ChunkSubspacePosition::GetBlockSpacePosition provided with no chunk.");
                BlockSpacePosition returnPos;
                returnPos.x = 0;
                returnPos.y = 0;
                returnPos.z = 0;
                return returnPos;
            }
            
            BlockSpacePosition newPosition;
            newPosition.x = currentChunk.WorldPosition().x * Chunk.SIZE + x;
            newPosition.y = currentChunk.WorldPosition().y * Chunk.SIZE + y;
            newPosition.z = currentChunk.WorldPosition().z * Chunk.SIZE + z;
            return newPosition;
        }
    }
}
// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using ironVoxel.Domain;

namespace ironVoxel {
    public struct BlockSpacePosition {
        public int x;
        public int y;
        public int z;
        
        public BlockSpacePosition (int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        
        public Vector3 GetVector3()
        {
            Vector3 returnVector;
            returnVector.x = x;
            returnVector.y = y;
            returnVector.z = z;
            return returnVector;
        }

        public void FromVector3(Vector3 vector)
        {
            x = (int)vector.x;
            y = (int)vector.y;
            z = (int)vector.z;
        }
        
        public static BlockSpacePosition CreateFromVector3(Vector3 vector)
        {
            BlockSpacePosition position;
            position.x = (int)Mathf.Floor(vector.x);
            position.y = (int)Mathf.Floor(vector.y);
            position.z = (int)Mathf.Floor(vector.z);
            return position;
        }

        public ChunkSpacePosition GetChunkSpacePosition()
        {
            ChunkSpacePosition chunkPosition;
            chunkPosition.x = x / Chunk.SIZE;
            chunkPosition.y = y / Chunk.SIZE;
            chunkPosition.z = z / Chunk.SIZE;
            return chunkPosition;
        }
        
        public ChunkSubspacePosition GetChunkSubspacePosition(Chunk relativeToChunk)
        {
            if (relativeToChunk == null) {
                UnityEngine.Debug.LogWarning("BlockSpacePosition::GetChunkSubspacePosition provided with no chunk.");
                ChunkSubspacePosition returnPos;
                returnPos.x = 0;
                returnPos.y = 0;
                returnPos.z = 0;
                return returnPos;
            }

            ChunkSubspacePosition newPosition;
            newPosition.y = (int)(y - (relativeToChunk.WorldPosition().y * Chunk.SIZE));
            newPosition.x = (int)(x - (relativeToChunk.WorldPosition().x * Chunk.SIZE));
            newPosition.z = (int)(z - (relativeToChunk.WorldPosition().z * Chunk.SIZE));
            return newPosition;
        }
    }
}

// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using ironVoxel.Domain;

namespace ironVoxel {
    public struct ChunkBlockPair {
        public Chunk chunk;
        public Block block;
        
        public ChunkBlockPair (Chunk chunk, Block block)
        {
            this.chunk = chunk;
            this.block = block;
        }
    }
}
// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using System;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel {
    public class ChunkSpacePositionComparer : Comparer<ChunkSpacePosition> {
        public override int Compare(ChunkSpacePosition x, ChunkSpacePosition y)
        {
            ChunkSpacePosition a = (ChunkSpacePosition)x;
            ChunkSpacePosition b = (ChunkSpacePosition)y;
            if (a.x == b.x) {
                if (a.y == b.y) {
                    if (a.z == b.z) {
                        return 0;
                    }
                    else if (a.z > b.z) {
                            return 1;
                        }
                        else {
                            return -1;
                        }
                }
                else if (a.y > b.y) {
                        return 1;
                    }
                    else {
                        return -1;
                    }
            }
            else {
                if (a.x < b.x) {
                    return -1;
                }
                else {
                    return 1;
                }
            }
        }
    }

    public struct ChunkSpacePosition {
        public int x;
        public int y;
        public int z;
        
        public ChunkSpacePosition (int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
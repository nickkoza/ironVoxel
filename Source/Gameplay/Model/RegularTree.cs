// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

using ironVoxel.Gameplay;

namespace ironVoxel.Gameplay {
    public class RegularTree : ModelTemplate {
        // Use static variables to cache the model template
        private static readonly uint SIZE_X = 5;
        private static readonly uint SIZE_Y = 7;
        private static readonly uint SIZE_Z = 5;
        private static readonly int ORIGIN_X = 2;
        private static readonly int ORIGIN_Y = 0;
        private static readonly int ORIGIN_Z = 2;
        private static readonly bool SET_AIR = false;
        private static readonly BlockType[,,] blocks = LoadModel();

        protected override uint SizeX() { return SIZE_X; }
        protected override uint SizeY() { return SIZE_Y; }
        protected override uint SizeZ() { return SIZE_Z; }
        protected override int OriginX() { return ORIGIN_X; }
        protected override int OriginY() { return ORIGIN_Y; }
        protected override int OriginZ() { return ORIGIN_Z; }
        protected override bool SetAir() { return SET_AIR; }
        protected override BlockType[,,] Blocks() { return blocks; }

        private static BlockType[,,] LoadModel()
        {
            BlockType[,,] returnBlocks = new BlockType[SIZE_X, SIZE_Y, SIZE_Z];
            
            int xIttr, yIttr, zIttr;
            for (xIttr = 0; xIttr < SIZE_X; xIttr++) {
                for (yIttr = 0; yIttr < SIZE_Y; yIttr++) {
                    for (zIttr = 0; zIttr < SIZE_Z; zIttr++) {
                        returnBlocks[xIttr, yIttr, zIttr] = BlockType.Air;
                    }
                }
            }
            
            // 1st layer
            returnBlocks[2, 6, 2] = BlockType.Leaves;
            
            // 2nd layer
            returnBlocks[2, 5, 2] = BlockType.Leaves;
            returnBlocks[1, 5, 2] = BlockType.Leaves;
            returnBlocks[3, 5, 2] = BlockType.Leaves;
            returnBlocks[2, 5, 1] = BlockType.Leaves;
            returnBlocks[2, 5, 3] = BlockType.Leaves;
            
            // 3rd layer
            returnBlocks[1, 4, 1] = BlockType.Leaves;
            returnBlocks[2, 4, 1] = BlockType.Leaves;
            returnBlocks[3, 4, 1] = BlockType.Leaves;
            returnBlocks[1, 4, 3] = BlockType.Leaves;
            returnBlocks[2, 4, 3] = BlockType.Leaves;
            returnBlocks[3, 4, 3] = BlockType.Leaves;
            returnBlocks[3, 4, 2] = BlockType.Leaves;
            returnBlocks[1, 4, 2] = BlockType.Leaves;
            
            // 4th layer
            returnBlocks[1, 3, 1] = BlockType.Leaves;
            returnBlocks[2, 3, 1] = BlockType.Leaves;
            returnBlocks[3, 3, 1] = BlockType.Leaves;
            returnBlocks[1, 3, 3] = BlockType.Leaves;
            returnBlocks[2, 3, 3] = BlockType.Leaves;
            returnBlocks[3, 3, 3] = BlockType.Leaves;
            returnBlocks[3, 3, 2] = BlockType.Leaves;
            returnBlocks[1, 3, 2] = BlockType.Leaves;
            
            returnBlocks[0, 3, 2] = BlockType.Leaves;
            returnBlocks[2, 3, 4] = BlockType.Leaves;
            returnBlocks[2, 3, 0] = BlockType.Leaves;
            returnBlocks[4, 3, 2] = BlockType.Leaves;
            
            // 5th layer
            returnBlocks[1, 2, 1] = BlockType.Leaves;
            returnBlocks[2, 2, 1] = BlockType.Leaves;
            returnBlocks[3, 2, 1] = BlockType.Leaves;
            returnBlocks[1, 2, 3] = BlockType.Leaves;
            returnBlocks[2, 2, 3] = BlockType.Leaves;
            returnBlocks[3, 2, 3] = BlockType.Leaves;
            returnBlocks[3, 2, 2] = BlockType.Leaves;
            returnBlocks[1, 2, 2] = BlockType.Leaves;
            
            returnBlocks[0, 2, 1] = BlockType.Leaves;
            returnBlocks[0, 2, 2] = BlockType.Leaves;
            returnBlocks[0, 2, 3] = BlockType.Leaves;
            returnBlocks[4, 2, 1] = BlockType.Leaves;
            returnBlocks[4, 2, 2] = BlockType.Leaves;
            returnBlocks[4, 2, 3] = BlockType.Leaves;
            returnBlocks[1, 2, 0] = BlockType.Leaves;
            returnBlocks[2, 2, 0] = BlockType.Leaves;
            returnBlocks[3, 2, 0] = BlockType.Leaves;
            returnBlocks[1, 2, 4] = BlockType.Leaves;
            returnBlocks[2, 2, 4] = BlockType.Leaves;
            returnBlocks[3, 2, 4] = BlockType.Leaves;
            
            // Trunk
            returnBlocks[2, 4, 2] = BlockType.Wood;
            returnBlocks[2, 3, 2] = BlockType.Wood;
            returnBlocks[2, 2, 2] = BlockType.Wood;
            returnBlocks[2, 1, 2] = BlockType.Wood;
            returnBlocks[2, 0, 2] = BlockType.Wood;

            return returnBlocks;
        }
    }
}
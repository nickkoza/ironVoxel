// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

using ironVoxel.Gameplay;

namespace ironVoxel.Domain {
    public struct Block {
        private static Block emptyBlock = new Block(BlockType.Air);
        private BlockType type;
        private uint adjacentBlockTransparentBitField;
        
        public Block (BlockType type)
        {
            this.type = type;
            adjacentBlockTransparentBitField = 0xFFFFFFFF;
        }
        
        public bool AdjacentBlockIsTransparent(int x, int y, int z)
        {
            return (adjacentBlockTransparentBitField & OffsetsToBitMask(x, y, z)) == 0;
        }
        
        public bool AdjacentBlockIsNotTransparent(int x, int y, int z)
        {
            return !AdjacentBlockIsTransparent(x, y, z);
        }
        
        public void SetAdjacentBlockTransparentFlag(int x, int y, int z, bool transparent)
        {
            if (transparent) {
                SetAdjacentBlockTransparent(x, y, z);
            }
            else {
                SetAdjacentBlockNotTransparent(x, y, z);
            }
        }
        
        public void SetAdjacentBlockTransparent(int x, int y, int z)
        {
            adjacentBlockTransparentBitField |= OffsetsToBitMask(x, y, z);
        }
        
        public void SetAdjacentBlockNotTransparent(int x, int y, int z)
        {
            adjacentBlockTransparentBitField &= ~OffsetsToBitMask(x, y, z);
        }

        private uint OffsetsToBitMask(int x, int y, int z)
        {
            int shiftAmount = BitOffset(x, 1) + BitOffset(y, 3) + BitOffset(z, 9);
            return (uint)0x00000001 << shiftAmount;
        }
        
        private int BitOffset(int x, int multiplier)
        {
            if (x > 0) {
                return 2 * multiplier;
            }
            else if (x == 0) {
                    return 1 * multiplier;
                }
                else {
                    return 0;
                }
        }
        
        public Vector2 GetTextureCoordinates(CubeSide side, bool blockAbove, bool blockBelow, byte lightValue)
        {
            BlockTextureSlot slot = GetDefinition().GetTextureSlot(side, blockAbove, blockBelow, lightValue);
            Vector2 returnVector;
            returnVector.x = slot.x * 16;
            returnVector.y = slot.y * 16;
            return returnVector;
        }
        
        public Vector2 GetOverallTextureSize()
        {
            Vector2 returnVector;
            returnVector.x = 256;
            returnVector.y = 256;
            return returnVector;
        }
        
        public Vector2 GetIndividualTextureSize()
        {
            Vector2 returnVector;
            returnVector.x = 16;
            returnVector.y = 16;
            return returnVector;
        }
        
        public void Set(BlockType type)
        {
            this.type = type;
        }
        
        public void Set(BlockDefinition definition)
        {
            this.type = definition.GetBlockType();
        }
        
        public BlockType GetBlockType()
        {
            return type;
        }
        
        public bool IsActive()
        {
            return GetDefinition().IsActive();
        }
        
        public bool IsNotActive()
        {
            return !IsActive();
        }
        
        public bool IsSolidToTouch()
        {
            return GetDefinition().IsSolidToTouch();
        }
        
        public bool IsNotSolidToTouch()
        {
            return GetDefinition().IsNotSolidToTouch();
        }
        
        public bool IsTransparent()
        {
            return GetDefinition().IsTransparent();
        }
        
        public bool IsNotTransparent()
        {
            return GetDefinition().IsNotTransparent();
        }

        public byte FilterColorHue()
        {
            return GetDefinition().FilterColorHue();
        }

        public byte FilterColorSaturation()
        {
            return GetDefinition().FilterColorSaturation();
        }

        public byte FilterColorValue()
        {
            return GetDefinition().FilterColorValue();
        }
        
        public bool IsWater()
        {
            return GetDefinition().IsWater();
        }
        
        public bool IsBedrock()
        {
            return GetDefinition().IsBedrock();
        }
        
        public bool RemoveHiddenFaces()
        {
            return GetDefinition().RemoveHiddenFaces();
        }
        
        public BlockDefinition GetDefinition()
        {
            return BlockDefinition.DefinitionOfType(type);
        }
        
        public bool BlocksViewOf(Block otherBlock)
        {
            return (IsActive() && (
                IsNotTransparent() ||
                (IsTransparent() && otherBlock.IsTransparent() && RemoveHiddenFaces()) ||
                (IsWater() && otherBlock.IsWater())
                ));
        }
        
        public bool DoesNotBlockViewOf(Block otherBlock)
        {
            return !BlocksViewOf(otherBlock);
        }
        
        public bool IsLightEmitter()
        {
            return GetDefinition().IsLightEmitter();
        }
        
        public int LightEmitRadius()
        {
            return GetDefinition().LightEmitRadius();
        }

        public byte LightEmitHue()
        {
            return GetDefinition().LightEmitHue();
        }

        public byte LightEmitSaturation()
        {
            return GetDefinition().LightEmitSaturation();
        }

        public byte LightEmitValue()
        {
            return GetDefinition().LightEmitValue();
        }
        
        public static Block EmptyBlock()
        {
            return emptyBlock;
        }
    }
}

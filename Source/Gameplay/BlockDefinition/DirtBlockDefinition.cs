// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel.Gameplay {
    public class DirtBlockDefinition : BlockDefinition {
        private BlockTextureSlot grassTextureSideSlot;
        private BlockTextureSlot grassTextureTopSlot;
        
        public DirtBlockDefinition ()
        {
            type = BlockType.Dirt;
            transparent = false;
            solidToTouch = true;
            
            textureSlot.x = 2;
            textureSlot.y = 0;
            grassTextureTopSlot.x = 0;
            grassTextureTopSlot.y = 0;
            grassTextureSideSlot.x = 3;
            grassTextureSideSlot.y = 0;
        }
        
        public override BlockTextureSlot GetTextureSlot(CubeSide side, bool blockAbove, bool blockBelow, byte lightValue)
        {
            BlockTextureSlot slot;
            if (blockAbove == false && side == CubeSide.Top && lightValue == 255) {
                slot = grassTextureTopSlot;
            }
            else if (blockAbove == false && lightValue == 255 && side != CubeSide.Top && side != CubeSide.Bottom) {
                    slot = grassTextureSideSlot;
                }
                else {
                    slot = textureSlot;
                }
            return slot;
        }
    }
}
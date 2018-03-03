// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel.Gameplay {
    public class WoodBlockDefinition : BlockDefinition {
        private BlockTextureSlot topAndBottomTextureSlot;
        
        public WoodBlockDefinition ()
        {
            type = BlockType.Wood;
            transparent = false;
            solidToTouch = true;
            
            textureSlot.x = 4;
            textureSlot.y = 1;
            topAndBottomTextureSlot.x = 5;
            topAndBottomTextureSlot.y = 1;
            
        }
        
        public override BlockTextureSlot GetTextureSlot(CubeSide side, bool blockAbove, bool blockBelow, byte lightValue)
        {
            if (side == CubeSide.Top || side == CubeSide.Bottom) {
                return topAndBottomTextureSlot;
            }
            else {
                return textureSlot;
            }
        }
    }
}
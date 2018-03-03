// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel.Gameplay {
    public enum BlockType {
        Air,
        Dirt,
        Stone,
        WoodPlanks,
        Water,
        Sand,
        Cobble,
        Glass,
        RedGlass,
        BlueGlass,
        GreenGlass,
        YellowGlass,
        PurpleGlass,
        Bedrock,
        Wood,
        Leaves,
        Coal,
        Iron,

        Lava,

        Lamp,
        RedLamp,
        OrangeLamp,
        YellowLamp,
        GreenLamp,
        AquaLamp,
        BlueLamp,
        PurpleLamp,
        PinkLamp,

        NumberOfBlockTypes
    }

    // Using an enum as the key for a dictionary without a custom comparer causes boxing, which
    // massively slows down the game.
    class BlockTypeComparer : IEqualityComparer<BlockType> {
        private static readonly BlockTypeComparer instance = new BlockTypeComparer();

        public static BlockTypeComparer Instance()
        {
            return instance;
        }

        public bool Equals(BlockType type1, BlockType type2)
        {
            return (type1 == type2);
        }
        
        public int GetHashCode(BlockType type)
        {
            return (int)type;
        }
    }
    
    public class BlockDefinition {
        public static void InitializeAllTypes()
        {
            definitionsList = new List<BlockDefinition>();

            definitionsTable = new Dictionary<BlockType, BlockDefinition>(BlockTypeComparer.Instance());
            BlockDefinition definition;
            
            DefineTransparentBlock(BlockType.Air, 0, 0, false, true, 0, 0, byte.MaxValue);
            
            definition = new DirtBlockDefinition();
            AddDefinitionToLookupStructs(definition);
            
            definition = new WoodBlockDefinition();
            AddDefinitionToLookupStructs(definition);
            
            DefineBasicBlock(BlockType.Stone, 1, 0, true, true);
            DefineBasicBlock(BlockType.WoodPlanks, 4, 0, true, true);
            DefineBasicBlock(BlockType.Sand, 2, 1, true, true);
            DefineBasicBlock(BlockType.Cobble, 0, 1, true, true);
            DefineBasicBlock(BlockType.Bedrock, 1, 1, true, true);
            DefineBasicBlock(BlockType.Coal, 2, 2, true, true);
            DefineBasicBlock(BlockType.Iron, 1, 2, true, true);

            DefineTransparentBlock(BlockType.Leaves, 4, 3, true, false, 80, 3, 220);
            DefineTransparentBlock(BlockType.Water, 0, 15, false, true, 140, 20, 200);

            DefineTransparentBlock(BlockType.Glass, 1, 3, true, true, 120, 20, 200);
            DefineTransparentBlock(BlockType.RedGlass, 15, 0, true, true, 0, 255, 180);
            DefineTransparentBlock(BlockType.GreenGlass, 15, 1, true, true, 80, 255, 180);
            DefineTransparentBlock(BlockType.BlueGlass, 15, 2, true, true, 160, 255, 180);
            DefineTransparentBlock(BlockType.YellowGlass, 15, 3, true, true, 40, 255, 180);
            DefineTransparentBlock(BlockType.PurpleGlass, 15, 4, true, true, 180, 255, 180);

            DefineLightEmittingBlock(BlockType.Lava, 13, 14, true, true, 0, 255, 1, 8);

            DefineLightEmittingBlock(BlockType.Lamp, 9, 6, true, true, 0, 0, 254, 10);
            DefineLightEmittingBlock(BlockType.RedLamp, 9, 6, true, true, 0, 255, 254, 10);
            DefineLightEmittingBlock(BlockType.OrangeLamp, 9, 6, true, true, 25, 255, 254, 10);
            DefineLightEmittingBlock(BlockType.YellowLamp, 9, 6, true, true, 40, 255, 254, 10);
            DefineLightEmittingBlock(BlockType.GreenLamp, 9, 6, true, true, 75, 255, 254, 10);
            DefineLightEmittingBlock(BlockType.AquaLamp, 9, 6, true, true, 120, 255, 254, 10);
            DefineLightEmittingBlock(BlockType.BlueLamp, 9, 6, true, true, 165, 255, 254, 10);
            DefineLightEmittingBlock(BlockType.PurpleLamp, 9, 6, true, true, 185, 255, 254, 10);
            DefineLightEmittingBlock(BlockType.PinkLamp, 9, 6, true, true, 205, 255, 254, 10);
        }
        
        private static List<BlockDefinition> definitionsList;
        private static Dictionary<BlockType, BlockDefinition> definitionsTable;
        protected bool solidToTouch;
        protected bool removeHiddenFaces;
        protected bool transparent; // Or "translucent", if it acts as a filter
        protected readonly byte filterColorHue; // What color tint the light receives as it passes through a translucent material
        protected readonly byte filterColorSaturation; // How much the light gets tinted as it passes through a translucent material
        protected readonly byte filterColorValue; // How much of the light passes through the translucent material
        
        protected BlockTextureSlot textureSlot;
        protected readonly byte lightEmitHue;
        protected readonly byte lightEmitSaturation;
        protected readonly byte lightEmitValue;
        protected readonly int lightEmitRadius;
        protected BlockType type;

        public BlockDefinition (BlockType type,
                               int textureSlotX, int textureSlotY,
                               bool solidToTouch, bool removeHiddenFaces) :
        this(type, textureSlotX, textureSlotY, solidToTouch, removeHiddenFaces, false, 0, 0, 0, 0, 0, 0, 0)
        {
        }

        public BlockDefinition (BlockType type,
                        int textureSlotX, int textureSlotY,
                        bool solidToTouch, bool removeHiddenFaces,
                        bool transparent, byte filterColorHue, byte filterColorSaturation, byte filterColorValue) :
        this(type, textureSlotX, textureSlotY, solidToTouch, removeHiddenFaces, 
                transparent, filterColorHue, filterColorSaturation, filterColorValue,
                0, 0, 0, 0)
        {
        }

        public BlockDefinition (BlockType type,
                        int textureSlotX, int textureSlotY,
                        bool solidToTouch, bool removeHiddenFaces,
                        bool transparent, byte filterColorHue, byte filterColorSaturation, byte filterColorValue,
                        int lightEmitRadius, byte lightEmitHue, byte lightEmitSaturation, byte lightEmitValue)
        {
            this.type = type;
            this.textureSlot.x = textureSlotX;
            this.textureSlot.y = textureSlotY;
            this.transparent = transparent;
            this.filterColorHue = filterColorHue;
            this.filterColorSaturation = filterColorSaturation;
            this.filterColorValue = filterColorValue;
            this.solidToTouch = solidToTouch;
            this.removeHiddenFaces = removeHiddenFaces;
            this.lightEmitHue = lightEmitHue;
            this.lightEmitSaturation = lightEmitSaturation;

            if (lightEmitValue > 254) {
                Debug.LogWarning("Trying to initialize a light with sunlight power. This would cause texture problems " +
                    "due to it thinking there's sunlight exposure where there's not.");
                this.lightEmitValue = 254;
            }
            else {
                this.lightEmitValue = lightEmitValue;
            }

            if (lightEmitRadius > Configuration.MAX_LIGHT_RADIUS) {
                Debug.LogWarning("Trying to initialize a light with greater than max radius.");
                this.lightEmitRadius = Configuration.MAX_LIGHT_RADIUS;
            }
            else {
                this.lightEmitRadius = lightEmitRadius;
            }
        }
        
        public BlockDefinition ()
        {
            type = BlockType.Air;
            textureSlot.x = 0;
            textureSlot.y = 0;
            transparent = false;
            solidToTouch = false;
            removeHiddenFaces = true;
        }

        public virtual BlockTextureSlot GetTextureSlot(CubeSide side, bool blockAbove, bool blockBelow, byte lightValue)
        {
            BlockTextureSlot slot;
            slot.x = textureSlot.x;
            slot.y = textureSlot.y;
            return slot;
        }
        
        public BlockType GetBlockType()
        {
            return type;
        }

        public virtual bool IsLightEmitter()
        {
            return lightEmitValue > 0 && lightEmitRadius > 0;
        }

        public bool IsSolidToTouch()
        {
            return solidToTouch;
        }

        public bool IsNotSolidToTouch()
        {
            return !IsSolidToTouch();
        }

        public bool IsTransparent()
        {
            return transparent;
        }

        public bool IsNotTransparent()
        {
            return !IsTransparent();
        }

        public byte FilterColorHue()
        {
            return filterColorHue;
        }

        public byte FilterColorSaturation()
        {
            return filterColorSaturation;
        }

        public byte FilterColorValue()
        {
            return filterColorValue;
        }

        public int  LightEmitRadius()
        {
            return lightEmitRadius;
        }

        public byte LightEmitHue()
        {
            return lightEmitHue;
        }

        public byte LightEmitSaturation()
        {
            return lightEmitSaturation;
        }

        public byte LightEmitValue()
        {
            return lightEmitValue;
        }

        public bool IsActive()
        {
            return type != BlockType.Air;
        }

        public bool IsWater()
        {
            return type == BlockType.Water;
        }

        public bool IsShorelineType()
        {
            return type == BlockType.Sand;
        }

        public bool IsBedrock()
        {
            return type == BlockType.Bedrock;
        }

        public bool RemoveHiddenFaces()
        {
            return removeHiddenFaces;
        }

        public bool DoNotRemoveHiddenFaces()
        {
            return !removeHiddenFaces;
        }
        
        public static void DefineBasicBlock(BlockType type, int textureSlotX, int textureSlotY,
            bool solidToTouch, bool removeHiddenFaces)
        {
            BlockDefinition definition;
            definition = new BlockDefinition(type, textureSlotX, textureSlotY, solidToTouch, removeHiddenFaces);
            AddDefinitionToLookupStructs(definition);
        }

        public static void DefineTransparentBlock(BlockType type,
                int textureSlotX, int textureSlotY,
                bool solidToTouch, bool removeHiddenFaces,
                byte filterColorHue, byte filterColorSaturation, byte filterColorValue)
        {
            BlockDefinition definition;
            definition = new BlockDefinition(type, textureSlotX, textureSlotY,
                solidToTouch, removeHiddenFaces,
                true, filterColorHue, filterColorSaturation, filterColorValue);
            AddDefinitionToLookupStructs(definition);
        }
        
        public static void DefineLightEmittingBlock(BlockType type,
                                        int textureSlotX, int textureSlotY,
                                        bool solidToTouch, bool removeHiddenFaces,
                                        byte lightEmitHue, byte lightEmitSaturation, byte lightEmitValue,
                                        int lightEmitRadius)
        {
            if (lightEmitRadius > Configuration.MAX_LIGHT_RADIUS) {
                Debug.LogWarning("A light has been defined with a radius greater than the configured MAX_LIGHT_RADIUS. " +
                    "This may cause partial updates when using this light. Configured max radius: " +
                    Configuration.MAX_LIGHT_RADIUS + ". Actual radius: " + lightEmitRadius + ".");
            }

            BlockDefinition definition;
            definition = new BlockDefinition(type, textureSlotX, textureSlotY, solidToTouch, removeHiddenFaces,
                false, 0, 0, 0,
                lightEmitRadius, lightEmitHue, lightEmitSaturation, lightEmitValue);
            
            AddDefinitionToLookupStructs(definition);
        }
        
        public static void AddDefinitionToLookupStructs(BlockDefinition definition)
        {
            definitionsList.Add(definition);
            definitionsTable.Add(definition.type, definition);
        }
        
        public static BlockDefinition DefinitionOfType(BlockType type)
        {
            return definitionsTable[type];
        }
    }
    
    public struct BlockTextureSlot {
        public int x;
        public int y;
    }
}

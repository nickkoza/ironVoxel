// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ironVoxel.Domain;

namespace ironVoxel.Gameplay {
    public sealed class WorldGenerator {
        private struct Sample {
            public double overworld;
            public double cave;
        }
        
        private static readonly int WATER_LEVEL = 90;
        private static readonly int MIN_TREE_ALTITUDE = WATER_LEVEL + 7;
        private static readonly int MAX_TREE_ALTITUDE = MIN_TREE_ALTITUDE + 20;
        private static readonly int MAX_NUMBER_OF_TREES_PER_CHUNK = 5;
        private static readonly float CHANCE_OF_NO_TREES_IN_CHUNK = 0.5f;
        
        public WorldGenerator ()
        {
        }

        // TODO -- Split up this function to make it shorter and more manageable
        public BlockType[,,] GenerateBlocks(Chunk chunk)
        {
            // Generate the actual samples - spaced out to save generation time
            Sample sample;
            BlockSpacePosition blockPosition;
            int horizontalSampleSpacing = 4;
            int verticalSampleSpacing = 2;
            int horizontalSampleStructSize = Chunk.SIZE / horizontalSampleSpacing + 1;
            int verticalSampleStructSize = Chunk.SIZE / verticalSampleSpacing + 1;
            Sample[,,] samples = new Sample[horizontalSampleStructSize, verticalSampleStructSize, horizontalSampleStructSize];

            ChunkSubspacePosition position;
            int xIttr, yIttr, zIttr;
            for (xIttr = 0; xIttr < horizontalSampleStructSize; xIttr++) {
                for (yIttr = 0; yIttr < verticalSampleStructSize; yIttr++) {
                    for (zIttr = 0; zIttr < horizontalSampleStructSize; zIttr++) {
                        position.x = xIttr * horizontalSampleSpacing;
                        position.y = yIttr * verticalSampleSpacing;
                        position.z = zIttr * horizontalSampleSpacing;
                        blockPosition = position.GetBlockSpacePosition(chunk);
                        sample = GenerateDensitySample(blockPosition);
                        samples[xIttr, yIttr, zIttr] = sample;
                    }
                }
            }

            // Fill in the full block structure, filling in the blanks based on the samples we took
            double v000, v001, v010, v011, v100, v101, v110, v111;
            double x, y, z;
            bool xIsSampleAligned, yIsSampleAligned, zIsSampleAligned;
            int xLowerSample, xUpperSample, yLowerSample, yUpperSample, zLowerSample, zUpperSample;
            bool shoreline, sunlightBlocked;
            int shorelineDepth;
            
            BlockType[,,] blockTypes = new BlockType[Chunk.SIZE, Chunk.SIZE, Chunk.SIZE];
            for (position.x = 0; position.x < Chunk.SIZE; position.x++) {
                for (position.z = 0; position.z < Chunk.SIZE; position.z++) {
                    shorelineDepth = 0;

                    // Run a raytrace to see if we can see sunlight. Adjust the shoreline bool accordingly.
                    position.y = Chunk.SIZE - 1;
                    blockPosition = position.GetBlockSpacePosition(chunk);
                    BlockSpacePosition endPoint;
                    endPoint.x = blockPosition.x;
                    endPoint.y = Configuration.HEIGHT;
                    endPoint.z = blockPosition.z;
                    sunlightBlocked = ironVoxel.Service.CollisionService.RaytraceCollision(blockPosition, endPoint, CubeSide.Bottom, true);
                    shoreline = !sunlightBlocked;

                    for (position.y = Chunk.SIZE - 1; position.y >= 0; position.y--) {
                        xIsSampleAligned = (position.x % (double)horizontalSampleSpacing) == 0;
                        yIsSampleAligned = (position.y % (double)verticalSampleSpacing) == 0;
                        zIsSampleAligned = (position.z % (double)horizontalSampleSpacing) == 0;

                        // Derive the sample from surrounding samples
                        xLowerSample = position.x / horizontalSampleSpacing;
                        xUpperSample = xIsSampleAligned ? xLowerSample : xLowerSample + 1;
                        yLowerSample = position.y / verticalSampleSpacing;
                        yUpperSample = yIsSampleAligned ? yLowerSample : yLowerSample + 1;
                        zLowerSample = position.z / horizontalSampleSpacing;
                        zUpperSample = zIsSampleAligned ? zLowerSample : zLowerSample + 1;

                        v000 = samples[xLowerSample, yLowerSample, zLowerSample].overworld;
                        v001 = samples[xLowerSample, yLowerSample, zUpperSample].overworld;
                        v010 = samples[xLowerSample, yUpperSample, zLowerSample].overworld;
                        v011 = samples[xLowerSample, yUpperSample, zUpperSample].overworld;
                        v100 = samples[xUpperSample, yLowerSample, zLowerSample].overworld;
                        v101 = samples[xUpperSample, yLowerSample, zUpperSample].overworld;
                        v110 = samples[xUpperSample, yUpperSample, zLowerSample].overworld;
                        v111 = samples[xUpperSample, yUpperSample, zUpperSample].overworld;
                        x = (position.x % horizontalSampleSpacing) / (double)horizontalSampleSpacing;
                        y = (position.y % verticalSampleSpacing) / (double)verticalSampleSpacing;
                        z = (position.z % horizontalSampleSpacing) / (double)horizontalSampleSpacing;
                        sample.overworld = TrilinearInterpolate(v000, v001, v010, v011, v100, v101, v110, v111, x, y, z);

                        v000 = samples[xLowerSample, yLowerSample, zLowerSample].cave;
                        v001 = samples[xLowerSample, yLowerSample, zUpperSample].cave;
                        v010 = samples[xLowerSample, yUpperSample, zLowerSample].cave;
                        v011 = samples[xLowerSample, yUpperSample, zUpperSample].cave;
                        v100 = samples[xUpperSample, yLowerSample, zLowerSample].cave;
                        v101 = samples[xUpperSample, yLowerSample, zUpperSample].cave;
                        v110 = samples[xUpperSample, yUpperSample, zLowerSample].cave;
                        v111 = samples[xUpperSample, yUpperSample, zUpperSample].cave;
                        sample.cave = TrilinearInterpolate(v000, v001, v010, v011, v100, v101, v110, v111, x, y, z);

                        blockPosition = position.GetBlockSpacePosition(chunk);
                        BlockType type = GenerateBlockTypeFromSample(blockPosition, sample, shoreline);
                        blockTypes[position.x, position.y, position.z] = type;

                        BlockDefinition blockDefinition = BlockDefinition.DefinitionOfType(type);
                        if (blockDefinition.IsSolidToTouch() && !blockDefinition.IsWater()) {
                            if (blockDefinition.IsShorelineType()) {
                                shorelineDepth++;
                                if (shorelineDepth > 3) {
                                    shoreline = false;
                                }
                            }
                            else {
                                shoreline = false;
                            }
                            sunlightBlocked = true;
                        }
                        else if ((type == BlockType.Air || type == BlockType.Water) && !sunlightBlocked) {
                                shoreline = true;
                            }
                    }
                }
            }

            return blockTypes;
        }

        public List<Model> GenerateModels(Chunk chunk) {
            List<Model> models = new List<Model>();

            if (chunk == null) {
                return models;
            }

            // Generate trees
            int chunkSeed = chunk.WorldPosition().x + chunk.WorldPosition().y + chunk.WorldPosition().z + World.Instance().GetSeed();
            System.Random random = new System.Random(chunkSeed);

            int numberOfTrees = ZeroWeightedRandom(MAX_NUMBER_OF_TREES_PER_CHUNK, CHANCE_OF_NO_TREES_IN_CHUNK, random);
            for (int i = 0; i < numberOfTrees; i++) {
                int treePositionIndex = (int)random.Next(Chunk.SIZE * Chunk.SIZE);
                ChunkSubspacePosition subspacePosition;
                subspacePosition.x = treePositionIndex / Chunk.SIZE;
                subspacePosition.z = treePositionIndex % Chunk.SIZE;

                BlockType type = BlockType.Air;
                subspacePosition.y = Chunk.SIZE - 1;
                while (type == BlockType.Air && subspacePosition.y >= 0) {
                    BlockSpacePosition position = subspacePosition.GetBlockSpacePosition(chunk);
                    type = chunk.GetBlock(subspacePosition).GetBlockType();
                    
                    if (position.y >= MIN_TREE_ALTITUDE && position.y <= MAX_TREE_ALTITUDE && type == BlockType.Dirt) {
                        Model model;
                        model.position = position;
                        model.template = new RegularTree();
                        models.Add(model);
                    }
                    
                    subspacePosition.y--;
                }
            }

            return models;
        }

        private Sample GenerateDensitySample(BlockSpacePosition blockPosition)
        {
            Sample sample;
            sample.overworld = 0.0;
            sample.cave = 0.0;

            sample.overworld = GenerateOverworldSample(blockPosition);
            if (OverworldSampleIsSolid(sample.overworld)) {
                sample.cave = GenerateCaveSample(blockPosition);
            }
            return sample;
        }

        private BlockType GenerateSolidBlockType(BlockSpacePosition blockPosition)
        {
            if (GenerateCoalSample(blockPosition)) {
                return BlockType.Coal;
            }
            else if (GenerateIronSample(blockPosition)) {
                    return BlockType.Iron;
                }
                else if (GenerateRockSample(blockPosition)) {
                        return BlockType.Stone;
                    }
                    else {
                        return BlockType.Dirt;
                    }
        }

        private BlockType GenerateBlockTypeFromSample(BlockSpacePosition blockPosition, Sample sample, bool shoreline)
        {
            // Make the bottom most layer always solid bedrock
            if (blockPosition.y == 0) {
                return BlockType.Bedrock;
            }
            
            // Generate overworld
            bool isWater = false;
            bool overworldSolid = OverworldSampleIsSolid(sample.overworld);
            
            if (overworldSolid == false && blockPosition.y < WATER_LEVEL) {
                isWater = true;
                overworldSolid = true;
            }
            
            // Overworld block type
            BlockType blockType = BlockType.Air;
            if (overworldSolid) {
                if (isWater) {
                    blockType = BlockType.Water;
                }
                else {
                    if (shoreline) {
                        if (blockPosition.y > WATER_LEVEL - 20 && blockPosition.y < WATER_LEVEL + 2) {
                            blockType = BlockType.Sand;
                        }
                        else {
                            blockType = GenerateSolidBlockType(blockPosition);
                        }
                    }
                    else {
                        blockType = GenerateSolidBlockType(blockPosition);
                    }
                }
            }
            
            // Generate caves
            bool caveSolid;
            if (isWater) {
                caveSolid = true;
            }
            else {
                bool showOnlyCaves = false;
                caveSolid = CaveSampleIsSolid(sample.cave);
                
                if (showOnlyCaves) {
                    caveSolid = !caveSolid;
                    overworldSolid = true;
                }
            }
            
            bool solid = overworldSolid && caveSolid;
            if (solid) {
                return blockType;
            }
            else {
                // Fill natural space near the bottom of the world with lava
                if (blockPosition.y < 5) {
                    return BlockType.Lava;
                }
                else {
                    return BlockType.Air;
                }
            }
        }

        private bool OverworldSampleIsSolid(double overworldSample)
        {
            return overworldSample > 0.0;
        }

        private bool CaveSampleIsSolid(double caveSample)
        {
            return caveSample <= 0.7; // Smaller number = bigger caves
        }

        private double GenerateOverworldSample(BlockSpacePosition blockPosition)
        {
            Vector3Double calc;
            calc.x = blockPosition.x * 0.005;
            calc.y = blockPosition.y * 0.008;
            calc.z = blockPosition.z * 0.005;
            double sample = PerlinNoise.RidgedTurbulence(calc, 10);
            return 1.0 + sample - ((double)blockPosition.y / 64.0);
        }
        
        private double GenerateCaveSample(BlockSpacePosition blockPosition)
        {
            double calcX = blockPosition.x * 0.03;
            double calcY = blockPosition.y * 0.03;
            double calcZ = blockPosition.z * 0.03;
            
            double noiseSample1 = MultifractalNoise.GenerateRidged((float)calcX, (float)calcY, (float)calcZ, 2.0f, 0.0f, 1.0f, 1.0f, 2.0f);
            double noiseSample2 = MultifractalNoise.GenerateRidged((float)calcX + 100.0f, (float)calcZ, (float)calcY, 2.0f, 0.0f, 1.0f, 1.0f, 2.0f);
            return noiseSample1 * noiseSample2 - blockPosition.y * 0.002;
        }
        
        private bool GenerateRockSample(BlockSpacePosition blockPosition)
        {
            double calcX = blockPosition.x * 0.018;
            double calcY = blockPosition.y * 0.020;
            double calcZ = blockPosition.z * 0.018;
            
            double noiseSample = PerlinNoise.Generate(calcX, calcY + 0.01, calcZ);
            return (1.0 + noiseSample - ((double)blockPosition.y / 32.0)) > -1.45 ||
                noiseSample < -0.3;
        }

        private bool GenerateCoalSample(BlockSpacePosition blockPosition)
        {
            double calcX = blockPosition.x * 0.20;
            double calcY = blockPosition.y * 0.20 + 5.0;
            double calcZ = blockPosition.z * 0.20;
            
            double noiseSample = PerlinNoise.Generate(calcX, calcY + 0.01, calcZ);
            return noiseSample < -0.465;
        }

        private bool GenerateIronSample(BlockSpacePosition blockPosition)
        {
            double calcX = blockPosition.x * 0.1 + 20.0;
            double calcY = blockPosition.y * 0.05 - 20.0;
            double calcZ = blockPosition.z * 0.1;
            
            double noiseSample = PerlinNoise.Generate(calcX, calcY + 0.01, calcZ);
            return noiseSample < -7.5;
        }

        private int ZeroWeightedRandom(int maxValue, float chanceOfZero, System.Random randomInstance) {
            float randomRange = maxValue / chanceOfZero;
            return (int)Mathf.Max(randomInstance.Next((int)Mathf.Ceil(randomRange)) - randomRange * chanceOfZero, 0);
        }

        private double TrilinearInterpolate(double v000, double v001, double v010, double v011,
                                            double v100, double v101, double v110, double v111,
                                            double x, double y, double z)
        {
            double dx00 = Mathd.Lerp(v000, v100, x);
            double dx01 = Mathd.Lerp(v001, v101, x);
            double dx10 = Mathd.Lerp(v010, v110, x);
            double dx11 = Mathd.Lerp(v011, v111, x);
            
            double dxy0 = Mathd.Lerp(dx00, dx10, y);
            double dxy1 = Mathd.Lerp(dx01, dx11, y);
            
            double dxyz = Mathd.Lerp(dxy0, dxy1, z);
            return dxyz;
        }
    }
}

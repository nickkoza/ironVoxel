// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

namespace ironVoxel.Gameplay {
    public abstract class ModelTemplate {
        protected abstract uint SizeX();
        protected abstract uint SizeY();
        protected abstract uint SizeZ();
        protected abstract int OriginX();
        protected abstract int OriginY();
        protected abstract int OriginZ();
        protected abstract bool SetAir();
        protected abstract BlockType[,,] Blocks();

        public void ApplyToWorld(BlockSpacePosition origin)
        {
            BlockSpacePosition setPosition;
            BlockType setType;
            int xIttr, yIttr, zIttr;
            for (xIttr = 0; xIttr < SizeX(); xIttr++) {
                for (yIttr = 0; yIttr < SizeY(); yIttr++) {
                    for (zIttr = 0; zIttr < SizeZ(); zIttr++) {
                        setType = Blocks()[xIttr, yIttr, zIttr];
                        if (setType != BlockType.Air || SetAir()) {
                            setPosition.x = (int)(origin.x + xIttr - OriginX());
                            setPosition.y = (int)(origin.y + yIttr - OriginY() + 1);
                            setPosition.z = (int)(origin.z + zIttr - OriginZ());
                            ironVoxel.Service.ChunkRepository.SetBlockAtPosition(setPosition, setType, true);
                        }
                    }
                }
            }
        }

        public void ApplyToChunk(BlockSpacePosition origin, ironVoxel.Domain.Chunk chunk) {
            if (chunk == null) {
                return;
            }

            BlockSpacePosition setPosition;
            BlockType setType;
            int xIttr, yIttr, zIttr;
            for (xIttr = 0; xIttr < SizeX(); xIttr++) {
                for (yIttr = 0; yIttr < SizeY(); yIttr++) {
                    for (zIttr = 0; zIttr < SizeZ(); zIttr++) {
                        setType = Blocks()[xIttr, yIttr, zIttr];
                        if (setType != BlockType.Air || SetAir()) {
                            setPosition.x = (int)(origin.x + xIttr - OriginX());
                            setPosition.y = (int)(origin.y + yIttr - OriginY() + 1);
                            setPosition.z = (int)(origin.z + zIttr - OriginZ());
                            chunk.SetBlockAtPosition(setPosition, setType, true);
                        }
                    }
                }
            }
        }
    }
}
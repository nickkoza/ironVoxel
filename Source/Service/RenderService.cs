// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza
#define INEXPENSIVE_FAKE_GLOBAL_ILLUMINATION

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using ironVoxel.Domain;
using ironVoxel.Gameplay;

namespace ironVoxel.Service {

    /// <summary>
    /// Handles visual interactions with the voxel world.
    /// </summary>
    /// <remarks>
    /// The Render Service handles a wide range of functions related to making sure that the voxel world looks visually 
    /// appealing. Because ironVoxel is designed to support full lighting and shadows without Unity Pro, the lighting 
    /// system is completely custom. From the Render Service you can access lighting information, mark caches for 
    /// updates, and other useful functions related to rendering.
    /// </remarks>
    public sealed class RenderService : ServiceGateway<RenderServiceImplementation> {

        /// <summary>
        /// Sample the voxel lighting at a given point in the world.
        /// </summary>
        /// <remarks>
        /// This is a useful function for making additional, non-voxel items appear with the correct lighting 
        /// information in the voxel world. For example, you can sample the lighting surrounding an enemy entity and 
        /// adjust the appearance of the enemy accordingly. 
        /// 
        /// Be aware that this is a CPU-intensive function, and it should be used sparingly if possible.
        /// </remarks>
        /// <param name="samplePosition">The world position to take the sample at.</param>
        /// <param name="sampleSide">
        /// What direction to sample at (a version of the normal of the face you're lighting).
        /// </param>
        public static Color SampleLight(BlockSpacePosition samplePosition, CubeSide sampleSide)
        {
            return Instance().SampleLight(samplePosition, sampleSide);
        }


        /// <summary>
        /// Sample the voxel lighting at a given point in the world.
        /// </summary>
        /// <remarks>
        /// This is a useful function for making additional, non-voxel items appear with the correct lighting 
        /// information in the voxel world. For example, you can sample the lighting surrounding an enemy entity and 
        /// adjust the appearance of the enemy accordingly. 
        /// 
        /// Be aware that this is a CPU-intensive function, and it should be used sparingly if possible.
        /// </remarks>
        /// <param name="lightList">
        /// A list of lights that you know are nearby. Gathering the nearby lights can be CPU intensive, so this allows 
        /// you to cache that operation between calls.
        /// </param>
        /// <param name="samplePosition">The world position to take the sample at.</param>
        /// <param name="sampleSide">
        /// What direction to sample at (a version of the normal of the face you're lighting).
        /// </param>
        public static Color SampleLight(List<BlockLight> lightList, BlockSpacePosition samplePosition, CubeSide sampleSide)
        {
            return Instance().SampleLight(lightList, samplePosition, sampleSide);
        }


        /// <summary>
        /// Mark the chunk, along with the surrounding chunks, as needing a mesh update because something has changed.
        /// </summary>
        public static void MarkSurroundingChunksForMeshUpdate(Chunk chunk)
        {
            Instance().MarkSurroundingChunksForMeshUpdate(chunk);
        }


        /// <summary>
        /// Mark all chunks within the maximum radius of a light as needing a mesh update because something has changed.
        /// </summary>
        public static void MarkChunksWithinMaxLightRadiusForMeshUpdate(BlockSpacePosition position)
        {
            Instance().MarkChunksWithinMaxLightRadiusForMeshUpdate(position);
        }


        /// <summary>
        /// Trigger the start of a given chunk's mesh generation system, so that it will update it's visual appearance.
        /// </summary>
        public static void GenerateMeshes(Chunk chunk)
        {
            Instance().GenerateMeshes(chunk);
        }


        /// <summary>
        /// Finish a given chunk's mech generation system, causing the updated mesh to appear on the screen.
        /// </summary>
        public static void FinishMeshGeneration(Chunk chunk)
        {
            Instance().FinishMeshGeneration(chunk);
        }


        /// <summary>
        /// Gather all the lights that the given chunks contain. This is a CPU-intensive process, and should be cached 
        /// when possible.
        /// </summary>
        public static List<BlockLight> GetAllLightsFromChunks(List<Chunk> chunkList)
        {
            return Instance().GetAllLightsFromChunks(chunkList);
        }


        /// <summary>
        /// Gather all the lights within the max lighting radius of a given world position. This is a CPU-intensive 
        /// process, and should be cached when possible.
        /// </summary>
        public static List<BlockLight> GetAllLightsWithinMaxRange(BlockSpacePosition blockPosition)
        {
            return Instance().GetAllLightsWithinMaxRange(blockPosition);
        }


        /// <summary>
        /// Gather all the lights within the max lighting radius of a given chunk grid position. This is a 
        /// CPU-intensive process, and should be cached when possible.
        /// </summary>
        public static List<BlockLight> GetAllLightsWithinMaxRange(ChunkSpacePosition chunkPosition)
        {
            return Instance().GetAllLightsWithinMaxRange(chunkPosition);
        }


        /// <summary>
        /// Calculate how urgent processing work at a given position is, based on how close it is to the camera. The 
        /// closer it is to the camera the higher priority it receives.
        /// </summary>
        public static int PriorityRelativeToCamera(Vector3 position)
        {
            return Instance().PriorityRelativeToCamera(position);
        }


        /// <summary>
        /// Update the chunk culling so that we don't waste GPU cycles rendering chunks that aren't at all visible 
        /// currently.
        /// </summary>
        public static void CullChunks()
        {
            Instance().CullChunks();
        }
    }
    
    public sealed class RenderServiceImplementation : IService {

        private Plane[] cameraFrustrumPlanes;
        Bounds frustrumCheckBounds;

        public RenderServiceImplementation ()
        {
            frustrumCheckBounds = new Bounds();
        }

        public void MarkSurroundingChunksForMeshUpdate(Chunk chunk)
        {
            if (chunk.BlockGenerationIsComplete()) {
                chunk.SetLoadState(ChunkLoadState.WaitingForMeshUpdate);
                chunk.MarkForMeshUpdate();
                
                // Add the adjacent chunks as well, because that changes rendering
                ChunkSpacePosition position = chunk.WorldPosition();
                position.z -= 1;
                MarkChunkAsNeedingMeshUpdate(position);
                
                position = chunk.WorldPosition();
                position.z += 1;
                MarkChunkAsNeedingMeshUpdate(position);
                
                position = chunk.WorldPosition();
                position.x -= 1;
                MarkChunkAsNeedingMeshUpdate(position);
                
                position = chunk.WorldPosition();
                position.x += 1;
                MarkChunkAsNeedingMeshUpdate(position);
                
                position = chunk.WorldPosition();
                position.y -= 1;
                MarkChunkAsNeedingMeshUpdate(position);
                
                position = chunk.WorldPosition();
                position.y += 1;
                MarkChunkAsNeedingMeshUpdate(position);
            }
        }

        public void MarkChunksWithinMaxLightRadiusForMeshUpdate(BlockSpacePosition position)
        {
            int distance = Configuration.MAX_LIGHT_RADIUS + Chunk.SIZE / 2;
            foreach (Chunk chunk in ChunkRepository.IterateChunksWithinRadius(position, distance)) {
                chunk.MarkForMeshUpdate();
                ChunkRepository.AddToProcessingChunkList(chunk);
            }
        }
        
        // TODO -- Make this func shorter
        public void GenerateMeshes(Chunk chunk)
        {
            if (chunk.NeedsMeshUpdate() &&
                (chunk.GetLoadState() == ChunkLoadState.WaitingForMeshUpdate || 
                chunk.GetLoadState() == ChunkLoadState.Done)) {
                ChunkSpacePosition position = chunk.WorldPosition();
                position.z -= 1;
                Chunk northChunk = ChunkRepository.GetChunkAtPosition(position);
                
                position = chunk.WorldPosition();
                position.z += 1;
                Chunk southChunk = ChunkRepository.GetChunkAtPosition(position);
                
                position = chunk.WorldPosition();
                position.x -= 1;
                Chunk westChunk = ChunkRepository.GetChunkAtPosition(position);
                
                position = chunk.WorldPosition();
                position.x += 1;
                Chunk eastChunk = ChunkRepository.GetChunkAtPosition(position);
                
                position = chunk.WorldPosition();
                position.y += 1;
                Chunk aboveChunk = ChunkRepository.GetChunkAtPosition(position);
                
                position = chunk.WorldPosition();
                position.y -= 1;
                Chunk belowChunk = ChunkRepository.GetChunkAtPosition(position);
                
                if ((northChunk == null || northChunk.BlocksAreGenerating() == false) &&
                    (southChunk == null || southChunk.BlocksAreGenerating() == false) &&
                    (westChunk == null || westChunk.BlocksAreGenerating() == false) &&
                    (eastChunk == null || eastChunk.BlocksAreGenerating() == false) &&
                    (aboveChunk == null || aboveChunk.BlocksAreGenerating() == false) &&
                    (belowChunk == null || belowChunk.BlocksAreGenerating() == false)) {
                    chunk.GenerateMesh(northChunk, southChunk, westChunk, eastChunk, aboveChunk, belowChunk);
                }
            }
        }
        
        public void FinishMeshGeneration(Chunk chunk)
        {
            if (AsyncService.FrameElapsedPercentageIsExceeded(0.7)) {
                return;
            }
            
            if (chunk.MeshCalculationIsFinished()) {
                while (chunk.MeshCalculationIsFinished()) {
                    if (AsyncService.FrameElapsedPercentageIsExceeded(0.7)) {
                        break;
                    }
                    chunk.IterateOnFinishingMeshGeneration();
                }
            }
        }
        
        public List<BlockLight> GetAllLightsFromChunks(List<Chunk> chunkList)
        {
            if (chunkList == null) {
                Debug.LogWarning("GetAllLightsFromChunks provided with null chunkList.");
                return null;
            }
            
            List<BlockLight> lights = new List<BlockLight>();
            int chunkListCount = chunkList.Count;
            for (int chunkListIndex = 0; chunkListIndex < chunkListCount; chunkListIndex++) {
                Chunk chunk = chunkList[chunkListIndex];
                // TODO -- This isn't super effecient on memory, but required for thread safety. Try to figure out a
                // better way of doing this.
                BlockLight[] chunkLights = chunk.LightsArray();
                int chunkLightsLength = chunkLights.Length;
                for (int chunkLightsIndex = 0; chunkLightsIndex < chunkLightsLength; chunkLightsIndex++) {
                    BlockLight light = chunkLights[chunkLightsIndex];
                    BlockSpacePosition lightPosition = light.chunkPosition.GetBlockSpacePosition(chunk);
                    if (BlockIsNotHidden(lightPosition)) {
                        lights.Add(light);
                    }
                }
            }
            return lights;
        }

        public List<BlockLight> GetAllLightsWithinMaxRange(BlockSpacePosition blockPosition)
        {
            return GetAllLightsWithinMaxRange(blockPosition.GetChunkSpacePosition());
        }
        
        public List<BlockLight> GetAllLightsWithinMaxRange(ChunkSpacePosition chunkPosition)
        {
            int distance = Configuration.MAX_LIGHT_RADIUS + Chunk.SIZE / 2;
            BlockSpacePosition checkPosition;
            checkPosition.x = chunkPosition.x * Chunk.SIZE + Chunk.SIZE / 2;
            checkPosition.y = chunkPosition.y * Chunk.SIZE + Chunk.SIZE / 2;
            checkPosition.z = chunkPosition.z * Chunk.SIZE + Chunk.SIZE / 2;
            
            List<BlockLight> lights = new List<BlockLight>();
            foreach (Chunk chunk in ChunkRepository.IterateChunksWithinRadius(checkPosition, distance)) {
                // TODO -- This isn't super efficient on memory, but required for thread safety. Try to figure out a
                // better way of doing this.
                BlockLight[] chunkLights;
                lock (chunk) {
                    chunkLights = chunk.LightsArray();
                }

                int chunkLightsLength = chunkLights.Length;
                for (int chunkLightsIndex = 0; chunkLightsIndex < chunkLightsLength; chunkLightsIndex++) {
                    BlockLight light = chunkLights[chunkLightsIndex];
                    BlockSpacePosition lightPosition = light.chunkPosition.GetBlockSpacePosition(chunk);
                    if (BlockIsNotHidden(lightPosition)) {
                        lights.Add(light);
                    }
                }
            }
            return lights;
        }
        
        public int PriorityRelativeToCamera(Vector3 position)
        {
            float distance = Vector3.Distance(Camera.main.transform.position, position);
            int returnVal = (int)(50000.0f - distance * 100.0f);
            return returnVal;
        }

        public void CullChunks()
        {
            Vector3 checkPosition;
            Vector3 checkSize;
            checkSize.x = Chunk.SIZE;
            checkSize.y = Chunk.SIZE;
            checkSize.z = Chunk.SIZE;

            UpdateCameraFrustrumPlanes();

            int numberOfChunks = ChunkRepository.NumberOfChunks();
            for (int chunkIndex = 0; chunkIndex < numberOfChunks; chunkIndex++) {
                Chunk chunk = ChunkRepository.GetChunkAtIndex(chunkIndex);
                checkPosition.x = chunk.WorldPosition().x * Chunk.SIZE + Chunk.SIZE / 2;
                checkPosition.y = chunk.WorldPosition().y * Chunk.SIZE + Chunk.SIZE / 2;
                checkPosition.z = chunk.WorldPosition().z * Chunk.SIZE + Chunk.SIZE / 2;

                if (CubeIsWithinFrustrum(checkPosition, checkSize)) {
                    chunk.Show();
                }
                else {
                    chunk.Hide();
                }
            }
        }

        public Color SampleLight(BlockSpacePosition samplePosition, CubeSide sampleSide)
        {
            List<BlockLight> lightList = GetAllLightsWithinMaxRange(samplePosition);
            return SampleLight(lightList, samplePosition, sampleSide);
        }

        public Color SampleLight(List<BlockLight> lightList, BlockSpacePosition samplePosition, CubeSide sampleSide)
        {
            float ambientPercentage = (samplePosition.y - Configuration.AMBIENT_SUBTERRANEAN_FULL_HEIGHT) /
                Configuration.AMBIENT_SUBTERRANEAN_START_HEIGHT;
            ambientPercentage = Mathf.Clamp(ambientPercentage, 0.0f, 1.0f);
            
            float calcHue = Mathf.Lerp(Configuration.AMBIENT_LIGHT_HUE_SUBTERRANEAN, Configuration.AMBIENT_LIGHT_HUE, ambientPercentage);
            float calcSaturation = Mathf.Lerp(Configuration.AMBIENT_LIGHT_SATURATION_SUBTERRANEAN, Configuration.AMBIENT_LIGHT_SATURATION, ambientPercentage);
            float calcValue = Mathf.Lerp(Configuration.AMBIENT_LIGHT_VALUE_SUBTERRANEAN, Configuration.AMBIENT_LIGHT_VALUE, ambientPercentage);
            
            HSBColor hsbColor;
            hsbColor.a = 1.0f;
            hsbColor.h = calcHue / 255.0f;
            hsbColor.s = calcSaturation / 255.0f;
            hsbColor.b = calcValue / 255.0f;
            
            Color calcColor = hsbColor.ToColor();
            
            // Calculate sunlight
            Vector3 startPosition;
            startPosition.x = samplePosition.x;
            startPosition.y = samplePosition.y;
            startPosition.z = samplePosition.z;
            Vector3 sunPosition = startPosition + (Configuration.SUN_ANGLE * Configuration.HEIGHT);
            BlockSpacePosition blockSunPosition;
            blockSunPosition.x = (int)sunPosition.x;
            blockSunPosition.y = (int)sunPosition.y;
            blockSunPosition.z = (int)sunPosition.z;
            
            byte sunlightHue;
            byte sunlightSaturation;
            byte sunlightValue;
            bool sunlight =
                !RaytraceLightBeam(blockSunPosition, samplePosition, sampleSide,
                    Configuration.SUNLIGHT_HUE, Configuration.SUNLIGHT_SATURATION, Configuration.SUNLIGHT_VALUE,
                     out sunlightHue, out sunlightSaturation, out sunlightValue);

            if (sunlight && LightCanAffectFace(samplePosition, blockSunPosition, sampleSide)) {
                calcColor = AddLightSample(calcColor, sunlightHue, sunlightSaturation, sunlightValue);
            }
            
            // Calculate artificial light
            int lightListCount = lightList.Count;
            for (int i = 0; i < lightListCount; i++) {
                BlockLight light = lightList[i];
                BlockSpacePosition worldLightPosition = light.chunkPosition.GetBlockSpacePosition(light.chunk);
                
                if (worldLightPosition.x == samplePosition.x &&
                    worldLightPosition.y == samplePosition.y &&
                    worldLightPosition.z == samplePosition.z) {
                    // Emitter - Full light
                    calcValue = 255;
                }
                else {
                    bool lightFace = LightCanAffectFace(samplePosition, worldLightPosition, sampleSide);
                    float distance = Vector3.Distance(worldLightPosition.GetVector3(), samplePosition.GetVector3());
                    BlockDefinition lightBlock = light.blockDefinition;
                    if (distance < lightBlock.LightEmitRadius()) {
                        byte lightHue = lightBlock.LightEmitHue();
                        byte lightSaturation = lightBlock.LightEmitSaturation();
                        byte lightValue = lightBlock.LightEmitValue();
                        if (lightFace) {
                            lightFace = RaytraceLightBeam(worldLightPosition, samplePosition, sampleSide,
                                                          lightBlock.LightEmitHue(), lightBlock.LightEmitSaturation(), lightBlock.LightEmitValue(),
                                                          out lightHue, out lightSaturation, out lightValue)
                                == false;
                        }
                        
                        if (lightFace) {
                            byte effectiveLightValue = (byte)Mathf.Min(lightValue * (1.0f - distance / lightBlock.LightEmitRadius()));
                            calcColor = AddLightSample(calcColor, lightHue, lightSaturation, effectiveLightValue);
                        }
                        #if INEXPENSIVE_FAKE_GLOBAL_ILLUMINATION
                        else {
                            byte effectiveLightValue = (byte)Mathf.Min(calcValue + lightValue / 2.0f *
                                                                       ((1 - distance / lightBlock.LightEmitRadius()) * 0.4f), 254);
                            calcColor = AddLightSample(calcColor, lightHue, lightSaturation, effectiveLightValue);
                        }
                        #endif
                    }
                }
            }
            
            return calcColor;
        }
        
        // -------------------------------------------------------------------------------------------------------------

        private Color AddLightSample(Color baseLight, byte additionalHue, byte additionalSaturation, byte additionalValue)
        {
            HSBColor additionalLight;
            additionalLight.a = 1.0f;
            additionalLight.h = additionalHue / 255.0f;
            additionalLight.s = additionalSaturation / 255.0f;
            additionalLight.b = additionalValue / 255.0f;
            Color effectiveColor = additionalLight.ToColor();
            baseLight.a = 1.0f;
            baseLight.r = Mathf.Min(baseLight.r + effectiveColor.r, 1.0f);
            baseLight.g = Mathf.Min(baseLight.g + effectiveColor.g, 1.0f);
            baseLight.b = Mathf.Min(baseLight.b + effectiveColor.b, 1.0f);
            return baseLight;
        }

        private HSBColor FilterLightSample(byte baseLightHue, byte baseLightSaturation, byte baseLightValue,
                byte filterHue, byte filterSaturation, byte filterValue)
        {

            HSBColor filterColorHsb;
            filterColorHsb.a = 1.0f;
            filterColorHsb.h = filterHue / 255.0f;
            filterColorHsb.s = filterSaturation / 255.0f;
            filterColorHsb.b = filterValue / 255.0f;
            Color filterColor = filterColorHsb.ToColor();

            HSBColor baseLightHsb;
            baseLightHsb.a = 1.0f;
            baseLightHsb.h = baseLightHue / 255.0f;
            baseLightHsb.s = baseLightSaturation / 255.0f;
            baseLightHsb.b = baseLightValue / 255.0f;
            Color baseLight = baseLightHsb.ToColor();

            baseLight.a = 1.0f;
            baseLight.r = Mathf.Max(baseLight.r - (1.0f - filterColor.r), 0.0f);
            baseLight.g = Mathf.Max(baseLight.g - (1.0f - filterColor.g), 0.0f);
            baseLight.b = Mathf.Max(baseLight.b - (1.0f - filterColor.b), 0.0f);

            return HSBColor.FromColor(baseLight);
        }
        
        private bool RaytraceLightBeam(BlockSpacePosition lightPosition, BlockSpacePosition samplePosition,
                                       CubeSide sampleSide,
                                       byte inLightHue, byte inLightSaturation, byte inLightValue,
                                       out byte outLightHue, out byte outLightSaturation, out byte outLightValue)
        {
            // Setup
            outLightHue = inLightHue;
            outLightSaturation = inLightSaturation;
            outLightValue = inLightValue;
            
            Vector3 originVector;
            Vector3 endpointVector;
            originVector = lightPosition.GetVector3();
            originVector.x += 0.5f;
            originVector.y += 0.5f;
            originVector.z += 0.5f;
            endpointVector = samplePosition.GetVector3();
            endpointVector.x += 0.5f;
            endpointVector.y += 0.5f;
            endpointVector.z += 0.5f;
            
            if (sampleSide == CubeSide.West) {
                endpointVector.x -= 0.5f;
            }
            if (sampleSide == CubeSide.East) {
                endpointVector.x += 0.5f;
            }
            if (sampleSide == CubeSide.North) {
                endpointVector.z -= 0.5f;
            }
            if (sampleSide == CubeSide.South) {
                endpointVector.z += 0.5f;
            }
            if (sampleSide == CubeSide.Bottom) {
                endpointVector.y -= 0.5f;
            }
            if (sampleSide == CubeSide.Top) {
                endpointVector.y += 0.5f;
            }
            
            // Do the raytrace
            Vector3 direction = Vector3.Normalize(endpointVector - originVector);
            float maxDistance = Vector3.Distance(originVector, endpointVector) - 1.0f;
            
            Vector3 movementAddition = Vector3.Normalize(direction);
            float movementSpeed = 0.05f;
            movementAddition.x *= movementSpeed;
            movementAddition.y *= movementSpeed;
            movementAddition.z *= movementSpeed;
            
            Vector3 checkPosition = originVector;
            Vector3 prevCheckPosition = checkPosition;
            float checkDistance = 0.0f;
            Chunk chunkCache = null;
            while (checkDistance <= maxDistance) {
                bool validBlock = (int)checkPosition.x != (int)originVector.x ||
                    (int)checkPosition.y != (int)originVector.y ||
                    (int)checkPosition.z != (int)originVector.z;
                
                ChunkBlockPair chunkBlockPair = ChunkRepository.GetBlockAtPosition(checkPosition, chunkCache);
                chunkCache = chunkBlockPair.chunk;
                Block checkBlock = chunkBlockPair.block;
                
                if (checkBlock.IsActive() && checkBlock.IsTransparent()) {
                    HSBColor outLightColor = FilterLightSample(outLightHue, outLightSaturation, outLightValue,
                        checkBlock.FilterColorHue(), checkBlock.FilterColorSaturation(), checkBlock.FilterColorValue());

                    outLightHue = (byte)(outLightColor.h * byte.MaxValue);
                    outLightSaturation = (byte)(outLightColor.s * byte.MaxValue);
                    outLightValue = (byte)(outLightColor.b * byte.MaxValue);
                }
                
                if (checkBlock.IsSolidToTouch() && validBlock && checkBlock.IsNotTransparent()) {
                    return true;
                }
                else {
                    prevCheckPosition = checkPosition;
                    do {
                        checkPosition += movementAddition;
                        checkDistance += movementSpeed;
                    }
                    while((int)checkPosition.x == (int)prevCheckPosition.x &&
                          (int)checkPosition.y == (int)prevCheckPosition.y &&
                          (int)checkPosition.z == (int)prevCheckPosition.z);
                }
            }
            
            return false;
        }

        private bool LightCanAffectFace(BlockSpacePosition samplePosition, BlockSpacePosition lightPosition,
                                        CubeSide sampleSide)
        {
            if (sampleSide == CubeSide.Bottom) {
                return lightPosition.y < samplePosition.y;
            }
            else if (sampleSide == CubeSide.Top) {
                    return lightPosition.y > samplePosition.y;
                }
                else if (sampleSide == CubeSide.West) {
                        return lightPosition.x < samplePosition.x;
                    }
                    else if (sampleSide == CubeSide.East) {
                            return lightPosition.x > samplePosition.x;
                        }
                        else if (sampleSide == CubeSide.North) {
                                return lightPosition.z < samplePosition.z;
                            }
                            else if (sampleSide == CubeSide.South) {
                                    return lightPosition.z > samplePosition.z;
                                }
            return false;
        }
        
        private static void MarkChunkAsNeedingMeshUpdate(ChunkSpacePosition position)
        {
            Chunk checkChunk = ChunkRepository.GetChunkAtPosition(position);
            if (checkChunk != null) {
                checkChunk.MarkForMeshUpdate();
            }
        }
        
        private static bool BlockIsActive(int x, int y, int z)
        {
            BlockSpacePosition position;
            position.x = x;
            position.y = y;
            position.z = z;
            return BlockIsActive(position);
        }
        
        private static bool BlockIsActive(BlockSpacePosition position)
        {
            return ChunkRepository.GetBlockAtPosition(position).IsActive();
        }
        
        private static bool BlockIsNotHidden(BlockSpacePosition blockPosition)
        {
            return !BlockIsHidden(blockPosition);
        }
        
        private static bool BlockIsHidden(BlockSpacePosition blockPosition)
        {
            return  BlockIsActive(blockPosition.x - 1, blockPosition.y, blockPosition.z) &&
                BlockIsActive(blockPosition.x + 1, blockPosition.y, blockPosition.z) &&
                BlockIsActive(blockPosition.x, blockPosition.y - 1, blockPosition.z) &&
                BlockIsActive(blockPosition.x, blockPosition.y + 1, blockPosition.z) &&
                BlockIsActive(blockPosition.x, blockPosition.y, blockPosition.z - 1) &&
                BlockIsActive(blockPosition.x, blockPosition.y, blockPosition.z + 1);
        }

        private void UpdateCameraFrustrumPlanes()
        {
            float cameraFOV = Camera.main.fieldOfView;
            Camera.main.fieldOfView += 20; // Culling is a bit aggressive, so open the FOV a bit before running the calculation
            cameraFrustrumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            Camera.main.fieldOfView = cameraFOV;
        }

        private bool CubeIsWithinFrustrum(Vector3 center, Vector3 size)
        {
            frustrumCheckBounds.center = center;
            frustrumCheckBounds.size = size;
            return GeometryUtility.TestPlanesAABB(cameraFrustrumPlanes, frustrumCheckBounds);
        }
    }
}

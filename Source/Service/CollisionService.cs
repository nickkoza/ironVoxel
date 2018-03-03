// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System;
using System.Collections;

using ironVoxel.Domain;
using ironVoxel.Gameplay;

namespace ironVoxel.Service {

    /// <summary>
    /// Handles physical interactions with the voxel world.
    /// </summary>
    /// <remarks>
    /// The Collision Service handles all the physical interactions with the voxel world. Unity currently doesn't allow 
    /// for collision hulls to be loaded asynchronously, so our only option is to maintain our own ultra-light-weight 
    /// collision and physics system. This allows new chunks to be loaded without hitching the framerate.
    /// </remarks>
    public sealed class CollisionService : ServiceGateway<CollisionServiceImplementation> {


        /// <summary>
        /// Runs a raytrace for a collision.
        /// </summary>
        /// <returns><c>true</c> if there was a collision</returns>
        /// <param name="origin">Point for the raytrace to start at.</param>
        /// <param name="endpoint">Point for the raytrace to end at.</param>
        /// <param name="endpointSide">Which side of the endpoint to target.</param>
        /// <param name="ignoreStartAndEnd">
        /// Whether to ignore blocks that are actually within the start and end point. You can enable this if you 
        /// actually only care about what is between two blocks, but not the blocks themselves.
        /// </param>
        public static bool RaytraceCollision(BlockSpacePosition origin, BlockSpacePosition endpoint,
                                             CubeSide endpointSide,
                                             bool ignoreStartAndEnd)
        {
            return Instance().RaytraceCollision(origin, endpoint, endpointSide, ignoreStartAndEnd);
        }


        /// <summary>
        /// Runs a raytrace for a collision.
        /// </summary>
        /// <returns><c>true</c> if there was a collision</returns>
        /// <param name="origin">Point for the raytrace to start at.</param>
        /// <param name="endpoint">Point for the raytrace to end at.</param>
        /// <param name="endpointSide">Which side of the endpoint to target.</param>
        /// <param name="ignoreStartAndEnd">
        /// Whether to ignore blocks that are actually within the start and end point. You can enable this if you 
        /// actually only care about what is between two blocks, but not the blocks themselves.
        /// </param>
        /// <param name="reverseScan">If set to <c>true</c>, it will scan from the endpoint towards the origin.</param>
        public static bool RaytraceCollision(BlockSpacePosition origin, BlockSpacePosition endpoint,
                                             CubeSide endpointSide,
                                             bool ignoreStartAndEnd,
                                             bool reverseScan)
        {
            return Instance().RaytraceCollision(origin, endpoint, endpointSide, ignoreStartAndEnd, reverseScan);
        }


        /// <summary>
        /// Runs a raytrace for a collision.
        /// </summary>
        /// <returns><c>true</c> if there was a collision</returns>
        /// <param name="origin">Point for the raytrace to start at.</param>
        /// <param name="direction">Direction of the ray.</param>
        /// <param name="maxDistance">
        /// The maximum distance that the raytrace will travel before it considers it a non-collision.
        /// </param>
        /// <param name="ignoreStartAndEnd">
        /// Whether to ignore blocks that are actually within the start and end point. You can enable this if you 
        /// actually only care about what is between two blocks, but not the blocks themselves.
        /// </param>
        public static bool RaytraceCollision(Vector3 origin, Vector3 direction, float maxDistance,
                                             bool ignoreStartAndEnd)
        {
            return Instance().RaytraceCollision(origin, direction, maxDistance, ignoreStartAndEnd);
        }


        /// <summary>
        /// Runs a raytrace for a collision.
        /// </summary>
        /// <returns><c>true</c> if there was a collision</returns>
        /// <param name="origin">Point for the raytrace to start at.</param>
        /// <param name="direction">Direction of the ray.</param>
        /// <param name="maxDistance">
        /// The maximum distance that the raytrace will travel before it considers it a non-collision.
        /// </param>
        /// <param name="ignoreStartAndEnd">
        /// Whether to ignore blocks that are actually within the start and end point. You can enable this if you 
        /// actually only care about what is between two blocks, but not the blocks themselves.
        /// </param>
        /// <param name="drawDebug">If set to <c>true</c> it will draw collision debug data using UnityEngine.Debug.*.</param>
        /// <param name="hitBlock">The block that the ray collided with. Block.EmptyBlock() if no collision was found.</param>
        /// <param name="hitBlockLocation">The location of the block that the ray collided with. Undefined if no collision was found.</param>
        /// <param name="hitNormal">The normal of the face of the block that the ray collided with. Undefined if no collision was found.</param>
        public static bool RaytraceCollision(Vector3 origin, Vector3 direction, float maxDistance,
                                             bool ignoreStartAndEnd,
                                             bool drawDebug,
                                             out Block hitBlock, out Vector3 hitBlockLocation, out Vector3 hitNormal)
        {
            return Instance().RaytraceCollision(origin, direction, maxDistance, ignoreStartAndEnd, drawDebug, out hitBlock,
                                     out hitBlockLocation, out hitNormal);
        }


        /// <summary>
        /// Runs a raytrace for a collision.
        /// </summary>
        /// <returns><c>true</c> if there was a collision</returns>
        /// <param name="origin">Point for the raytrace to start at.</param>
        /// <param name="direction">Direction of the ray.</param>
        /// <param name="maxDistance">
        /// The maximum distance that the raytrace will travel before it considers it a non-collision.
        /// </param>
        /// <param name="ignoreStartAndEnd">
        /// Whether to ignore blocks that are actually within the start and end point. You can enable this if you 
        /// actually only care about what is between two blocks, but not the blocks themselves.
        /// </param>
        /// <param name="hitBlock">The block that the ray collided with. Block.EmptyBlock() if no collision was found.</param>
        /// <param name="hitBlockLocation">The location of the block that the ray collided with. Undefined if no collision was found.</param>
        /// <param name="hitNormal">The normal of the face of the block that the ray collided with. Undefined if no collision was found.</param>
        public static bool RaytraceCollision(Vector3 origin, Vector3 direction, float maxDistance,
                                             bool ignoreStartAndEnd,
                                            out Block hitBlock, out Vector3 hitBlockLocation, out Vector3 hitNormal)
        {
            return Instance().RaytraceCollision(origin, direction, maxDistance, ignoreStartAndEnd, out hitBlock,
                                                out hitBlockLocation, out hitNormal);
        }


        /// <summary>
        /// Check whether there's a collision in a direction that an entity is attempting to move.
        /// </summary>
        public static bool PotentialEntityCollision(BlockSpacePosition position)
        {
            return Instance().PotentialEntityCollision(position);
        }


        /// <summary>
        /// Check whether there's a solid object in a given region of the world.
        /// </summary>
        public static bool RegionIsSolid(Vector3 origin, Vector3 size)
        {
            return Instance().RegionIsSolid(origin, size);
        }
    }

    public sealed class CollisionServiceImplementation : IService {

        public bool RaytraceCollision(BlockSpacePosition origin, BlockSpacePosition endpoint, CubeSide endpointSide,
                                    bool ignoreStartAndEnd)
        {
            return RaytraceCollision(origin, endpoint, endpointSide, ignoreStartAndEnd, false);
        }
        
        public bool RaytraceCollision(BlockSpacePosition origin, BlockSpacePosition endpoint, CubeSide endpointSide,
            bool ignoreStartAndEnd, bool reverseScan)
        {
            Vector3 originVector;
            Vector3 endpointVector;
            originVector = origin.GetVector3();
            originVector.x += 0.5f;
            originVector.y += 0.5f;
            originVector.z += 0.5f;
            endpointVector = endpoint.GetVector3();
            endpointVector.x += 0.5f;
            endpointVector.y += 0.5f;
            endpointVector.z += 0.5f;
            
            if (endpointSide == CubeSide.West) {
                endpointVector.x -= 0.5f;
            }
            if (endpointSide == CubeSide.East) {
                endpointVector.x += 0.5f;
            }
            if (endpointSide == CubeSide.North) {
                endpointVector.z -= 0.5f;
            }
            if (endpointSide == CubeSide.South) {
                endpointVector.z += 0.5f;
            }
            if (endpointSide == CubeSide.Bottom) {
                endpointVector.y -= 0.5f;
            }
            if (endpointSide == CubeSide.Top) {
                endpointVector.y += 0.5f;
            }
            
            if (reverseScan) {
                Vector3 originalOriginVector = originVector;
                originVector = endpointVector;
                endpointVector = originalOriginVector;
            }
            
            return RaytraceCollision(originVector, Vector3.Normalize(endpointVector - originVector),
                Vector3.Distance(originVector, endpointVector) - 1.0f, ignoreStartAndEnd);
        }
        
        public bool RaytraceCollision(Vector3 origin, Vector3 direction, float maxDistance, bool ignoreStartAndEnd)
        {
            Block hitBlock;
            Vector3 hitBlockLocation;
            Vector3 hitNormal;
            return RaytraceCollision(origin, direction, maxDistance, ignoreStartAndEnd, out hitBlock, out hitBlockLocation,
                                    out hitNormal);
        }
        
        public bool RaytraceCollision(Vector3 origin, Vector3 direction, float maxDistance, bool ignoreStartAndEnd,
            out Block hitBlock, out Vector3 hitBlockLocation, out Vector3 hitNormal)
        {
            return RaytraceCollision(origin, direction, maxDistance, ignoreStartAndEnd, false, out hitBlock, out hitBlockLocation, out hitNormal);
        }

        [ThreadStatic]
        private static Ray
            ray = new Ray(Vector3.zero, Vector3.zero);
        [ThreadStatic]
        private static Plane
            plane = new Plane(Vector3.zero, Vector3.zero);
        private const float INSIDE_WALL_OFFSET = 0.0001f;

        public bool RaytraceCollision(Vector3 origin, Vector3 direction, float maxDistance, bool ignoreStartAndEnd, bool drawDebug,
            out Block hitBlock, out Vector3 hitBlockLocation, out Vector3 hitNormal)
        {
            hitBlock = Block.EmptyBlock();
            hitBlockLocation = Vector3.zero;
            hitNormal = Vector3.zero;
            bool hit = false;
            Vector3 movementAddition = Vector3.Normalize(direction);
            float movementSpeed = 0.05f;
            movementAddition.x *= movementSpeed;
            movementAddition.y *= movementSpeed;
            movementAddition.z *= movementSpeed;

            ray.direction = direction.normalized;
            
            Vector3 checkPosition = origin;
            Vector3 prevCheckPosition = checkPosition;
            float checkDistance = 0.0f;

            bool refreshXPlane = true;
            bool refreshYPlane = true;
            bool refreshZPlane = true;

            float xDistance = maxDistance;
            float yDistance = maxDistance;
            float zDistance = maxDistance;
            
            Chunk chunkCache = null;
            
            while (checkDistance <= maxDistance) {
                bool validBlock =
                    ignoreStartAndEnd == false ||
                    (
                        (
                            (int)checkPosition.x != (int)origin.x ||
                    (int)checkPosition.y != (int)origin.y ||
                    (int)checkPosition.z != (int)origin.z
                        )
                    );
                
                ChunkBlockPair chunkBlockPair = ChunkRepository.GetBlockAtPosition(checkPosition, chunkCache);
                chunkCache = chunkBlockPair.chunk;

                if (drawDebug && chunkBlockPair.chunk != null) {
                    Vector3 drawPosition;
                    drawPosition.x = chunkBlockPair.chunk.WorldPosition().x * Chunk.SIZE + Chunk.SIZE / 2;
                    drawPosition.y = chunkBlockPair.chunk.WorldPosition().y * Chunk.SIZE + Chunk.SIZE / 2;
                    drawPosition.z = chunkBlockPair.chunk.WorldPosition().z * Chunk.SIZE + Chunk.SIZE / 2;
                    DebugUtils.DrawCube(drawPosition, Vector3.one * Chunk.SIZE / 2, Color.magenta, 0, true);
                }

                Block checkBlock = chunkBlockPair.block;
                
                if (checkBlock.IsSolidToTouch() && validBlock) {
                    hitBlock = checkBlock;
                    hitBlockLocation = checkPosition;
                    hit = true;
                    if ((int)prevCheckPosition.y != (int)checkPosition.y) {
                        hitNormal.y = ((int)prevCheckPosition.y).CompareTo((int)checkPosition.y);
                    }
                    else if ((int)prevCheckPosition.x != (int)checkPosition.x) {
                            hitNormal.x = ((int)prevCheckPosition.x).CompareTo((int)checkPosition.x);
                        }
                        else if ((int)prevCheckPosition.z != (int)checkPosition.z) {
                                hitNormal.z = ((int)prevCheckPosition.z).CompareTo((int)checkPosition.z);
                            }
                    break;
                }
                else {
                    prevCheckPosition = checkPosition;
                    ray.origin = checkPosition;
                    
                    // ---- X ----
                    if (refreshXPlane) {
                        if (direction.x < 0.0f) {
                            Vector3 westPlanePosition;
                            if (checkPosition.x == Mathf.Floor(checkPosition.x)) {
                                westPlanePosition.x = Mathf.Floor(checkPosition.x) - 1.0f - INSIDE_WALL_OFFSET;
                            }
                            else {
                                westPlanePosition.x = Mathf.Floor(checkPosition.x) - INSIDE_WALL_OFFSET;
                            }
                            westPlanePosition.y = 0.0f;
                            westPlanePosition.z = 0.0f;

                            plane.SetNormalAndPosition(Vector3.right, westPlanePosition);
                            plane.Raycast(ray, out xDistance);
                        }
                        else if (direction.x > 0.0f) {
                                Vector3 eastPlanePosition;
                                eastPlanePosition.x = Mathf.Floor(checkPosition.x) + 1.0f;
                                eastPlanePosition.y = 0.0f;
                                eastPlanePosition.z = 0.0f;

                                plane.SetNormalAndPosition(Vector3.left, eastPlanePosition);
                                plane.Raycast(ray, out xDistance);
                            }
                    }

                    // ---- Y ----
                    if (refreshYPlane) {
                        if (direction.y < 0.0f) {
                            Vector3 downwardPlanePosition;
                            downwardPlanePosition.x = 0.0f;
                            if (checkPosition.y == Mathf.Floor(checkPosition.y)) {
                                downwardPlanePosition.y = Mathf.Floor(checkPosition.y) - 1.0f - INSIDE_WALL_OFFSET;
                            }
                            else {
                                downwardPlanePosition.y = Mathf.Floor(checkPosition.y) - INSIDE_WALL_OFFSET;
                            }
                            downwardPlanePosition.z = 0.0f;
                            
                            plane.SetNormalAndPosition(Vector3.down, downwardPlanePosition);
                            plane.Raycast(ray, out yDistance);
                        }
                        else if (direction.y > 0.0f) {
                                Vector3 upwardPlanePosition;
                                upwardPlanePosition.x = 0.0f;
                                upwardPlanePosition.y = Mathf.Floor(checkPosition.y) + 1.0f;
                                upwardPlanePosition.z = 0.0f;
                            
                                plane.SetNormalAndPosition(Vector3.up, upwardPlanePosition);
                                plane.Raycast(ray, out yDistance);
                            }
                    }

                    // ---- Z ----
                    if (refreshZPlane) {
                        if (direction.z < 0.0f) {
                            Vector3 southPlanePosition;
                            southPlanePosition.x = 0.0f;
                            southPlanePosition.y = 0.0f;
                            if (checkPosition.z == Mathf.Floor(checkPosition.z)) {
                                southPlanePosition.z = Mathf.Floor(checkPosition.z) - 1.0f - INSIDE_WALL_OFFSET;
                            }
                            else {
                                southPlanePosition.z = Mathf.Floor(checkPosition.z) - INSIDE_WALL_OFFSET;
                            }

                            plane.SetNormalAndPosition(Vector3.forward, southPlanePosition);
                            plane.Raycast(ray, out zDistance);
                        }
                        else if (direction.z > 0.0f) {
                                Vector3 northPlanePosition;
                                northPlanePosition.x = 0.0f;
                                northPlanePosition.y = 0.0f;
                                northPlanePosition.z = Mathf.Floor(checkPosition.z) + 1.0f;

                                plane.SetNormalAndPosition(Vector3.back, northPlanePosition);
                                plane.Raycast(ray, out zDistance);
                            }
                    }

                    float distance;
                    if (xDistance > 0.0f && (xDistance < zDistance || zDistance == 0.0f) && (xDistance < yDistance || yDistance == 0.0f)) {
                        distance = xDistance;
                        refreshXPlane = true;
                    }
                    else if (zDistance > 0.0f && (zDistance < yDistance || yDistance == 0.0f)) {
                            distance = zDistance;
                            refreshZPlane = true;
                            if (xDistance == zDistance) {
                                refreshXPlane = true;
                            }
                        }
                        else {
                            distance = yDistance;
                            refreshYPlane = true;
                            if (xDistance == yDistance) {
                                refreshXPlane = true;
                            }
                            if (zDistance == yDistance) {
                                refreshZPlane = true;
                            }
                        }

                    if (distance < 0.0f) {
                        Debug.LogWarning("Distance: " + distance);
                        throw new Exception("Distance: " + distance + ". direction: " + direction + ". x dist: " + xDistance + ". y dist: " + yDistance + ". z dist: " + zDistance);
                    }
                    
                    Vector3 drawPoint1 = checkPosition;
                    
                    if (drawDebug) {
                        DebugUtils.DrawMark(checkPosition, Color.red, 0);
                    
                        Vector3 drawPosition;
                        drawPosition.x = Mathf.Floor(checkPosition.x) + 0.5f;
                        drawPosition.y = Mathf.Floor(checkPosition.y) + 0.5f;
                        drawPosition.z = Mathf.Floor(checkPosition.z) + 0.5f;
                        DebugUtils.DrawCube(drawPosition, Vector3.one / 2.0f, Color.blue, 0, true);
                    }
                    
                    checkPosition = ray.GetPoint(distance);
                    checkDistance += distance;
                    
                    if (drawDebug) {
                        DebugUtils.DrawMark(checkPosition, Color.green, 0);
                        UnityEngine.Debug.DrawLine(drawPoint1, checkPosition, Color.white, 0.0f, true);
                    }

                    xDistance -= distance;
                    yDistance -= distance;
                    zDistance -= distance;
                }
            }
            
            return hit;
        }

        // TODO -- This needs to be updated to not directly reference the player
        public bool PotentialEntityCollision(BlockSpacePosition position)
        {
            Block block = ChunkRepository.GetBlockAtPosition(position);
            if (block.IsSolidToTouch()) {
                // No use checking for entities if we already know that this spot is taken
                return true;
            }
            VoxelCharacterController[] characters =
                UnityEngine.Component.FindObjectsOfType(typeof(VoxelCharacterController)) as VoxelCharacterController[];

            int charactersLength = characters.Length;
            for (int i = 0; i < charactersLength; i++) {
                VoxelCharacterController character = characters[i];
                if (character.transform.position.x - character.radius + 0.5f < position.x + 0.5f &&
                    character.transform.position.x + character.radius + 0.5f > position.x - 0.5f &&
                    character.transform.position.z - character.radius + 0.5f < position.z + 0.5f &&
                    character.transform.position.z + character.radius + 0.5f > position.z - 0.5f &&
                    character.transform.position.y - character.height / 2.0f + 0.5f < position.y + 0.5f &&
                    character.transform.position.y + character.height / 2.0f + 0.5f > position.y - 0.5f) {
                    return true;
                }
            }
            
            return false;
        }

        private const bool DRAW_DEBUG = false;

        public bool RegionIsSolid(Vector3 origin, Vector3 size)
        {
            Vector3 lowerPoint;
            Vector3 upperPoint;
            lowerPoint.x = Mathf.Ceil(origin.x - size.x / 2.0f);
            lowerPoint.y = Mathf.Ceil(origin.y - size.y / 2.0f);
            lowerPoint.z = Mathf.Ceil(origin.z - size.z / 2.0f);
            upperPoint.x = Mathf.Ceil(origin.x + size.x / 2.0f);
            upperPoint.y = Mathf.Ceil(origin.y + size.y / 2.0f);
            upperPoint.z = Mathf.Ceil(origin.z + size.z / 2.0f);
            
#if DEBUG
            DebugUtils.DrawCube(origin, size / 2.0f, Color.yellow, 0, true);
#endif
            
            Vector3 drawSize;
            drawSize.x = (upperPoint.x - lowerPoint.x + 1.0f) / 2.0f;
            drawSize.y = (upperPoint.y - lowerPoint.y + 1.0f) / 2.0f;
            drawSize.z = (upperPoint.z - lowerPoint.z + 1.0f) / 2.0f;
            
            Vector3 drawPoint;
            drawPoint.x = lowerPoint.x + drawSize.x - 1.0f;
            drawPoint.y = lowerPoint.y + drawSize.y - 1.0f;
            drawPoint.z = lowerPoint.z + drawSize.z - 1.0f;

#if DEBUG
            DebugUtils.DrawCube(drawPoint, drawSize, Color.green, 0, true);
            DebugUtils.DrawMark(lowerPoint, Color.red, 0);
            DebugUtils.DrawMark(upperPoint, Color.red, 0);
#endif
            
            bool solid = false;
            int checkX, checkY, checkZ;
            for (checkX = (int)lowerPoint.x; checkX <= upperPoint.x; checkX += 1) {
                for (checkY = (int)lowerPoint.y; checkY <= upperPoint.y; checkY += 1) {
                    for (checkZ = (int)lowerPoint.z; checkZ <= upperPoint.z; checkZ += 1) {
                        BlockSpacePosition checkPosition;
                        checkPosition.x = checkX;
                        checkPosition.y = checkY;
                        checkPosition.z = checkZ;
                        Block checkBlock = ChunkRepository.GetBlockAtPosition(checkPosition);
                        solid = checkBlock.IsSolidToTouch();
                        
                        if (solid) {
                            Vector3 markPoint;
                            markPoint.x = checkX;
                            markPoint.y = checkY;
                            markPoint.z = checkZ;
#if DEBUG
                            DebugUtils.DrawMark(markPoint, Color.blue, 0);
#endif
                        }
                        
                        if (solid) {
                            break;
                        }
                    }
                    if (solid) {
                        break;
                    }
                }
                if (solid) {
                    break;
                }
            }
            
            return solid;
        }
    }
}

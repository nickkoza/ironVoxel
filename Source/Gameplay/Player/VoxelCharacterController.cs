// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

using ironVoxel;
using ironVoxel.Domain;

namespace ironVoxel.Gameplay {
    public struct VoxelControllerColliderHit {
        public Vector3 normal;
        public Vector3 moveDirection;
        public float moveLength;
    }

    public class VoxelCharacterController : MonoBehaviour {
        
        public float stepOffset = 0.4f;
        public Vector3 center = Vector3.zero;
        public float radius = 0.4f;
        public float height = 2.0f;
        CollisionFlags collisionFlags;

        void Start()
        {
            collisionFlags = CollisionFlags.None;
        }
        
        void Update()
        {
        
        }
        
        public CollisionFlags Move(Vector3 motion)
        {
            Vector3 newPosition = transform.position + motion;
            Vector3 size;
            size.x = radius * 2.0f;
            size.z = size.x;
            size.y = height;
            
            DebugUtils.DrawCube(transform.position, size / 2.0f, Color.green, 0, true);
            
            collisionFlags = CollisionFlags.None;
            newPosition = transform.position;
            newPosition.y += motion.y;
            
            Vector3 checkPosition = newPosition;

            VoxelControllerColliderHit hit;
            hit.normal = Vector3.zero;
            
            if (ironVoxel.Service.CollisionService.RegionIsSolid(checkPosition, size)) {
                collisionFlags |= CollisionFlags.Below;
                
                if (newPosition.y < transform.position.y) {
                    // Put them on top of the block they just landed on
                    newPosition.y = Mathf.Floor(newPosition.y) + 1.001f;
                    hit.normal.y = 1.0f;
                }
                else {
                    // They hit their head
                    newPosition.y = Mathf.Floor(newPosition.y);
                    hit.normal.y = -1.0f;
                }
            }
            
            if (motion.x != 0.0f) {
                newPosition.x += motion.x;
                checkPosition = newPosition;
                if (ironVoxel.Service.CollisionService.RegionIsSolid(checkPosition, size)) {
                    collisionFlags |= CollisionFlags.Sides;
                    if (newPosition.x < transform.position.x) {
                        newPosition.x = Mathf.Floor(newPosition.x) + radius + 0.001f;
                        hit.normal.x = 1.0f;
                    }
                    else {
                        newPosition.x = Mathf.Floor(newPosition.x) + 1.0f - Mathf.Repeat(radius, 1.0f);
                        hit.normal.x = -1.0f;
                    }
                }
            }
            
            if (motion.z != 0.0f) {
                newPosition.z += motion.z;
                checkPosition = newPosition;
                if (ironVoxel.Service.CollisionService.RegionIsSolid(checkPosition, size)) {
                    collisionFlags |= CollisionFlags.Sides;
                    if (newPosition.z < transform.position.z) {
                        newPosition.z = Mathf.Floor(newPosition.z) + radius + 0.001f;
                        hit.normal.z = 1.0f;
                    }
                    else {
                        newPosition.z = Mathf.Floor(newPosition.z) + 1.0f - Mathf.Repeat(radius, 1.0f);
                        hit.normal.z = -1.0f;
                    }
                }
            }
            
            hit.moveLength = Vector3.Distance(transform.position, newPosition);
            hit.moveDirection = Vector3.Normalize(transform.position - newPosition);
            
            transform.position = newPosition;
            
            SendMessage("OnControllerColliderHit", hit);

            return collisionFlags;
        }
    }
}
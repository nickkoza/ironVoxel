// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

using ironVoxel.Service;

public class BallPhysics : MonoBehaviour {

    private static Vector3 gravity = new Vector3(0, -0.0098067f, 0.0f);
    private static Vector3 collisionCheckOffset = new Vector3(1.0f, 1.0f, 1.0f);
    public float restitution = 0.6f;
    public float airResistance = 0.01f;
    public float radius = 0.25f;
    private Vector3 velocity;
    
    void Start()
    {
    }

    void Update()
    {
        ironVoxel.Domain.Block hitBlock;
        Vector3 hitBlockLocation;
        Vector3 hitBlockNormal;
        bool hit;
        Vector3 checkPosition = transform.position + collisionCheckOffset;

        hit = CollisionService.RaytraceCollision(checkPosition, Vector3.down, gravity.magnitude + radius, false,
                                                      out hitBlock, out hitBlockLocation, out hitBlockNormal);
        if (!hit) {
            velocity += gravity;
        }
        else {
            Vector3 position;
            position.x = transform.position.x;
            position.z = transform.position.z;

            position.y = hitBlockLocation.y - collisionCheckOffset.y + radius;
            transform.position = position;
        }

        if (velocity.magnitude > 0.0001f) {

            hit = CollisionService.RaytraceCollision(checkPosition, velocity.normalized, velocity.magnitude + radius, false,
                                                          out hitBlock, out hitBlockLocation, out hitBlockNormal);
            
            if (hit) {
                velocity = Vector3.Reflect(velocity, hitBlockNormal) * restitution;
            }
        }

        transform.position = transform.position + velocity;
    }

    public void applyForce(Vector3 force)
    {
        velocity += force;
    }
}

// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel.Gameplay {
    [RequireComponent(typeof(VoxelCharacterController))]
    [AddComponentMenu("Character/Character Motor")]
    public class CharacterMotor : MonoBehaviour {
        bool canControl = true;
        bool useFixedUpdate = true;
        [System.NonSerialized]
        public Vector3
            inputMoveDirection = Vector3.zero;
        [System.NonSerialized]
        public bool
            inputJump = false;
        public bool applyGravity = true;

        [System.Serializable]
        public class CharacterMotorMovement {
            public float maxForwardSpeed = 3.0f;
            public float maxSidewaysSpeed = 2.0f;
            public float maxBackwardsSpeed = 2.0f;
            public float maxGroundAcceleration = 30.0f;
            public float maxAirAcceleration = 20.0f;
            public float gravity = 9.81f;
            public float maxFallSpeed = 20.0f;
            [System.NonSerialized]
            public CollisionFlags
                collisionFlags;
            [System.NonSerialized]
            public Vector3
                velocity;
            [System.NonSerialized]
            public Vector3
                frameVelocity = Vector3.zero;
            [System.NonSerialized]
            public Vector3
                hitPoint = Vector3.zero;
            [System.NonSerialized]
            public Vector3
                lastHitPoint = new Vector3(Mathf.Infinity, 0, 0);
        }

        public CharacterMotorMovement movement = new CharacterMotorMovement();

        public enum MovementTransferOnJump {
            None,
            InitTransfer,
            PermaTransfer,
            PermaLocked
        }

        [System.Serializable]
        public class CharacterMotorJumping {
            public bool enabled = true;
            public float baseHeight = 1.0f;
            public float extraHeight = 1.1f;
            public float perpAmount = 0.0f;
            public float steepPerpAmount = 0.5f;
            [System.NonSerialized]
            public bool
                jumping = false;
            [System.NonSerialized]
            public bool
                holdingJumpButton = false;
            [System.NonSerialized]
            public float
                lastStartTime = 0.0f;
            [System.NonSerialized]
            public float
                lastButtonDownTime = -100.0f;
            [System.NonSerialized]
            public Vector3
                jumpDir = Vector3.up;
        }

        public CharacterMotorJumping jumping = new CharacterMotorJumping();

        [System.Serializable]
        public class CharacterMotorMovingPlatform {
            public bool enabled = true;
            public MovementTransferOnJump movementTransfer = MovementTransferOnJump.PermaTransfer;
            [System.NonSerialized]
            public Transform
                hitPlatform;
            [System.NonSerialized]
            public Transform
                activePlatform;
            [System.NonSerialized]
            public Vector3
                activeLocalPoint;
            [System.NonSerialized]
            public Vector3
                activeGlobalPoint;
            [System.NonSerialized]
            public Quaternion
                activeLocalRotation;
            [System.NonSerialized]
            public Quaternion
                activeGlobalRotation;
            [System.NonSerialized]
            public Matrix4x4
                lastMatrix;
            [System.NonSerialized]
            public Vector3
                platformVelocity;
            [System.NonSerialized]
            public bool
                newPlatform;
        }

        [System.NonSerialized]
        public bool
            grounded = true;
        [System.NonSerialized]
        public Vector3
            groundNormal = Vector3.zero;
        private Transform tr;
        private VoxelCharacterController controller;

        void Awake()
        {
            controller = GetComponent<VoxelCharacterController>();
            tr = transform;
        }

        private Vector3 velocity;

        private void UpdateFunction()
        {
            velocity = movement.velocity;
            velocity = ApplyInputVelocityChange(velocity);

            if (applyGravity) {
                velocity = ApplyGravityAndJumping(velocity);
            }

            Vector3 lastPosition = tr.position;
            Vector3 currentMovementOffset = velocity * Time.deltaTime;

            float pushDownOffset = Mathf.Max(controller.stepOffset, new Vector3(currentMovementOffset.x, 0, currentMovementOffset.z).magnitude);
            if (grounded) {
                currentMovementOffset -= pushDownOffset * Vector3.up;
            }

            groundNormal = Vector3.zero;

            movement.collisionFlags = controller.Move(currentMovementOffset);

            movement.lastHitPoint = movement.hitPoint;

            Vector3 oldHVelocity = new Vector3(velocity.x, 0, velocity.z);
            movement.velocity = (tr.position - lastPosition) / Time.deltaTime;
            Vector3 newHVelocity = new Vector3(movement.velocity.x, 0, movement.velocity.z);

            if (oldHVelocity == Vector3.zero) {
                movement.velocity = new Vector3(0, movement.velocity.y, 0);
            }
            else {
                float projectedNewVelocity = Vector3.Dot(newHVelocity, oldHVelocity) / oldHVelocity.sqrMagnitude;
                movement.velocity = oldHVelocity * Mathf.Clamp01(projectedNewVelocity) + movement.velocity.y * Vector3.up;
            }

            if (movement.velocity.y < velocity.y - 0.001) {
                if (movement.velocity.y < 0) {
                    movement.velocity.y = velocity.y;
                }
                else {
                    jumping.holdingJumpButton = false;
                }
            }

            if (grounded && !IsGroundedTest()) {
                grounded = false;

                SendMessage("OnFall", SendMessageOptions.DontRequireReceiver);
                tr.position += pushDownOffset * Vector3.up;
            }
            else if (!grounded && IsGroundedTest()) {
                    grounded = true;
                    jumping.jumping = false;

                    SendMessage("OnLand", SendMessageOptions.DontRequireReceiver);
                }
        }

        void FixedUpdate()
        {
            if (useFixedUpdate) {
                UpdateFunction();
            }
        }

        void Update()
        {
            if (!useFixedUpdate) {
                UpdateFunction();
            }
        }

        private Vector3 ApplyInputVelocityChange(Vector3 velocity)
        {
            if (!canControl) {
                inputMoveDirection = Vector3.zero;
            }

            Vector3 desiredVelocity = GetDesiredHorizontalVelocity();

            float maxVelocityChange = GetMaxAcceleration(grounded) * Time.deltaTime;
            Vector3 velocityChangeVector = (desiredVelocity - velocity);
            if (velocityChangeVector.sqrMagnitude > maxVelocityChange * maxVelocityChange) {
                velocityChangeVector = velocityChangeVector.normalized * maxVelocityChange;
            }
            
            velocity += velocityChangeVector;

            if (!grounded) {
                velocity.y = 0;
            }
            return velocity;
        }

        private Vector3 ApplyGravityAndJumping(Vector3 velocity)
        {

            if (!inputJump || !canControl) {
                jumping.holdingJumpButton = false;
                jumping.lastButtonDownTime = -100;
            }

            if (inputJump && jumping.lastButtonDownTime < 0 && canControl) {
                jumping.lastButtonDownTime = Time.time;
            }

            if (grounded) {
                velocity.y = Mathf.Min(0, velocity.y) - movement.gravity * Time.deltaTime;
            }
            else {
                velocity.y = movement.velocity.y - movement.gravity * Time.deltaTime;

                if (jumping.jumping && !jumping.holdingJumpButton && velocity.y > 0.0f) {
                    velocity.y *= 0.75f;
                }

                velocity.y = Mathf.Max(velocity.y, -movement.maxFallSpeed);
            }

            if (grounded) {
                if (jumping.enabled && canControl && (Time.time - jumping.lastButtonDownTime < 0.2)) {
                    grounded = false;
                    jumping.jumping = true;
                    jumping.lastStartTime = Time.time;
                    jumping.lastButtonDownTime = -100;
                    jumping.holdingJumpButton = true;

                    jumping.jumpDir = Vector3.Slerp(Vector3.up, groundNormal, jumping.perpAmount);

                    velocity.y = 0;
                    velocity += jumping.jumpDir * CalculateJumpVerticalSpeed(jumping.baseHeight);

                    SendMessage("OnJump", SendMessageOptions.DontRequireReceiver);
                }
                else {
                    jumping.holdingJumpButton = false;
                }
            }

            return velocity;
        }

        void OnControllerColliderHit(VoxelControllerColliderHit hit)
        {
            if (hit.normal.y > 0) {
                groundNormal = hit.normal;
                movement.frameVelocity = Vector3.zero;
            }
        }

        private Vector3 GetDesiredHorizontalVelocity()
        {
            Vector3 desiredLocalDirection = tr.InverseTransformDirection(inputMoveDirection);
            float maxSpeed = MaxSpeedInDirection(desiredLocalDirection);
            return tr.TransformDirection(desiredLocalDirection * maxSpeed);
        }

        private Vector3 AdjustGroundVelocityToNormal(Vector3 hVelocity, Vector3 groundNormal)
        {
            Vector3 sideways = Vector3.Cross(Vector3.up, hVelocity);
            return Vector3.Cross(sideways, groundNormal).normalized * hVelocity.magnitude;
        }

        private bool IsGroundedTest()
        {
            return (groundNormal.y > 0.01);
        }

        float GetMaxAcceleration(bool grounded)
        {
            if (grounded) {
                return movement.maxGroundAcceleration;
            }
            else {
                return movement.maxAirAcceleration;
            }
        }

        float CalculateJumpVerticalSpeed(float targetJumpHeight)
        {
            return Mathf.Sqrt(2 * targetJumpHeight * movement.gravity);
        }

        bool IsJumping()
        {
            return jumping.jumping;
        }

        bool IsTouchingCeiling()
        {
            return (movement.collisionFlags & CollisionFlags.CollidedAbove) != 0;
        }

        bool IsGrounded()
        {
            return grounded;
        }

        Vector3 GetDirection()
        {
            return inputMoveDirection;
        }

        void SetControllable(bool controllable)
        {
            canControl = controllable;
        }

        float MaxSpeedInDirection(Vector3 desiredMovementDirection)
        {
            if (desiredMovementDirection == Vector3.zero) {
                return 0;
            }
            else {
                float zAxisEllipseMultiplier = (desiredMovementDirection.z > 0 ? movement.maxForwardSpeed : movement.maxBackwardsSpeed) / movement.maxSidewaysSpeed;
                Vector3 temp = new Vector3(desiredMovementDirection.x, 0, desiredMovementDirection.z / zAxisEllipseMultiplier).normalized;
                float length = new Vector3(temp.x, 0, temp.z * zAxisEllipseMultiplier).magnitude * movement.maxSidewaysSpeed;
                return length;
            }
        }

        void SetVelocity(Vector3 velocity)
        {
            grounded = false;
            movement.velocity = velocity;
            movement.frameVelocity = Vector3.zero;
            SendMessage("OnExternalVelocity");
        }
    }
}
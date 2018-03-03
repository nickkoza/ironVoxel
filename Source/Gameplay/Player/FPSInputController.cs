// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ironVoxel.Gameplay {
    [RequireComponent(typeof(CharacterMotor))]
    [AddComponentMenu("Character/FPS Input Controller")]
    public class FPSInputController : MonoBehaviour {
        private CharacterMotor motor;

        void Awake()
        {
            motor = GetComponent<CharacterMotor>();
        }

        void Update()
        {
            Vector3 directionVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

            if (directionVector != Vector3.zero) {
                float directionLength = directionVector.magnitude;
                directionVector = directionVector / directionLength;

                directionLength = Mathf.Min(1.0f, directionLength);

                directionLength = directionLength * directionLength;

                directionVector = directionVector * directionLength;
            }

            motor.inputMoveDirection = transform.rotation * directionVector;
            motor.inputJump = Input.GetButton("Jump");
        }
    }
}
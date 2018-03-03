// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

namespace ironVoxel.Gameplay {
    public class FlyingCamera : MonoBehaviour {
        float speed = 0.7f;

        // Use this for initialization
        void Start()
        {
        }
        
        // Update is called once per frame
        void Update()
        {
            Vector3 inputVector = GetInputVector();
            transform.Translate(inputVector * speed);
            
            if (Input.GetKey(KeyCode.Q) == true) {
                transform.Translate(Vector3.down * speed);
            }
            
            if (Input.GetKey(KeyCode.E) == true) {
                transform.Translate(Vector3.up * speed);
            }
        }
        
        private Vector3 GetInputVector()
        {
            Vector3 returnVector;
            returnVector.x = 0.0f;
            returnVector.y = 0.0f;
            returnVector.z = 0.0f;
            
            if (Input.GetKey(KeyCode.W)) {
                returnVector.z += 1.0f;
            }
            if (Input.GetKey(KeyCode.S)) {
                returnVector.z -= 1.0f;
            }
            if (Input.GetKey(KeyCode.A)) {
                returnVector.x -= 1.0f;
            }
            if (Input.GetKey(KeyCode.D)) {
                returnVector.x += 1.0f;
            }
            return returnVector;
        }
    }
}
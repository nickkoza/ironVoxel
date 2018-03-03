// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

namespace ironVoxel {
    public sealed class LogoDisplay : MonoBehaviour {
        
        public Texture logoTexture;
        private const float displayTime = 3.0f; // seconds
        private float startTime;
        private Rect position;
        
        // Use this for initialization
        void Start()
        {
            startTime = Time.time;
            position.x = Screen.width / 2.0f - logoTexture.width / 2.0f;
            position.y = Screen.height / 2.0f - logoTexture.height / 2.0f;
            position.width = logoTexture.width;
            position.height = logoTexture.height;
        }
        
        // Update is called once per frame
        void Update()
        {
            if (Time.time - startTime >= displayTime) {
                Destroy(gameObject);
            }
        }
        
        void OnGUI()
        {
            Color guiColor = GUI.color;
            guiColor.a = 1.0f - (Time.time - startTime) / displayTime;
            GUI.color = guiColor;
            GUI.DrawTexture(position, logoTexture);
        }
    }
}
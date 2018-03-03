// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

using ironVoxel.Domain;

namespace ironVoxel.Gameplay {
    public class Fog : MonoBehaviour {
        const float logSmall = -2.72124639905f; // Mathf.Log(0.0019f)
        const float distanceIncreaseSpeed = 0.005f;
        const float distanceDecreaseSpeed = 0.05f;
        const float colorChangeSpeed = 0.05f;
        Color startingColor;
        Color endingColor;
        Color undergroundColor;
        Color currentColor;
        float currentDistance;
        float targetDistance;
        int undergroundHeightLimit;
    
        void Start()
        {
            endingColor = Camera.main.backgroundColor;
            startingColor = endingColor;
            undergroundColor = new Color(0.0f, 0.0f, 0.0f);
            currentColor = startingColor;
            
            RenderSettings.fogColor = Camera.main.backgroundColor;
            currentDistance = 0.0f;
            targetDistance = 0.0f;
            undergroundHeightLimit = 45;
        }
        
        void Update()
        {
            if (currentDistance < targetDistance) {
                currentDistance = currentDistance + (targetDistance - currentDistance) * distanceIncreaseSpeed;
            }
            else if (currentDistance > targetDistance) {
                    currentDistance = currentDistance + (targetDistance - currentDistance) * distanceDecreaseSpeed;
                }
            
            AdjustToCurrentDistance();
            AdjustColor();
        }
        
        public void UpdateFogDistance(float distance)
        {
            targetDistance = distance;
        }
        
        private void AdjustColor()
        {
            float targetPercentage = currentDistance / (Configuration.CHUNK_VIEW_DISTANCE * Chunk.SIZE);
            targetPercentage = Mathf.Min(1.0f, targetPercentage * 1.25f); // Accelerate the color change
            Color targetColor = Color.Lerp(startingColor, endingColor, targetPercentage);
            
            float undergroundTint = Mathf.Min(1.0f - (Camera.main.transform.position.y / undergroundHeightLimit), 1.0f);
            targetColor = Color.Lerp(targetColor, undergroundColor, undergroundTint);
            
            currentColor = Color.Lerp(currentColor, targetColor, colorChangeSpeed);
            RenderSettings.fogColor = currentColor;
            Camera.main.backgroundColor = currentColor;
        }
        
        private void AdjustToCurrentDistance()
        {
            if (RenderSettings.fogMode == FogMode.Linear) {
                SetLinearFog();
            }
            else if (RenderSettings.fogMode == FogMode.Exponential) {
                    SetExpFog();
                }
                else if (RenderSettings.fogMode == FogMode.ExponentialSquared) {
                        SetExp2Fog();
                    }
        }
        
        private void SetLinearFog()
        {
            float startDistance = currentDistance - Chunk.SIZE * 6.0f;
            if (startDistance < 0) {
                startDistance = 0.0f;
            }
            RenderSettings.fogStartDistance = startDistance;
            RenderSettings.fogEndDistance = currentDistance;
        }
        
        private void SetExpFog()
        {
            RenderSettings.fogDensity = -(logSmall / currentDistance);
        }
        
        private void SetExp2Fog()
        {
            RenderSettings.fogDensity = Mathf.Sqrt(-(logSmall / Mathf.Pow(currentDistance, 2)));
        }
    }
}

// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

namespace ironVoxel {
    public sealed class DebugUtils {

        public static void DrawCube(Vector3 origin, Vector3 halfSize, Color color, float duration, bool depthTest)
        {
            Vector3 startPos, endPos;
            
            startPos = origin;
            startPos.x -= halfSize.x;
            startPos.y += halfSize.y;
            startPos.z -= halfSize.z;
            endPos = origin;
            endPos.x += halfSize.x;
            endPos.y += halfSize.y;
            endPos.z -= halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
            
            startPos = origin;
            startPos.x -= halfSize.x;
            startPos.y += halfSize.y;
            startPos.z -= halfSize.z;
            endPos = origin;
            endPos.x -= halfSize.x;
            endPos.y += halfSize.y;
            endPos.z += halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
            
            startPos = origin;
            startPos.x -= halfSize.x;
            startPos.y -= halfSize.y;
            startPos.z -= halfSize.z;
            endPos = origin;
            endPos.x -= halfSize.x;
            endPos.y += halfSize.y;
            endPos.z -= halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
            
            startPos = origin;
            startPos.x -= halfSize.x;
            startPos.y -= halfSize.y;
            startPos.z -= halfSize.z;
            endPos = origin;
            endPos.x += halfSize.x;
            endPos.y -= halfSize.y;
            endPos.z -= halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
            
            startPos = origin;
            startPos.x -= halfSize.x;
            startPos.y -= halfSize.y;
            startPos.z -= halfSize.z;
            endPos = origin;
            endPos.x -= halfSize.x;
            endPos.y -= halfSize.y;
            endPos.z += halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
            
            startPos = origin;
            startPos.x += halfSize.x;
            startPos.y -= halfSize.y;
            startPos.z -= halfSize.z;
            endPos = origin;
            endPos.x += halfSize.x;
            endPos.y += halfSize.y;
            endPos.z -= halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
            
            startPos = origin;
            startPos.x += halfSize.x;
            startPos.y -= halfSize.y;
            startPos.z += halfSize.z;
            endPos = origin;
            endPos.x += halfSize.x;
            endPos.y += halfSize.y;
            endPos.z += halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
            
            startPos = origin;
            startPos.x -= halfSize.x;
            startPos.y -= halfSize.y;
            startPos.z += halfSize.z;
            endPos = origin;
            endPos.x -= halfSize.x;
            endPos.y += halfSize.y;
            endPos.z += halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
            
            startPos = origin;
            startPos.x += halfSize.x;
            startPos.y -= halfSize.y;
            startPos.z += halfSize.z;
            endPos = origin;
            endPos.x += halfSize.x;
            endPos.y -= halfSize.y;
            endPos.z -= halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
            
            startPos = origin;
            startPos.x += halfSize.x;
            startPos.y -= halfSize.y;
            startPos.z += halfSize.z;
            endPos = origin;
            endPos.x -= halfSize.x;
            endPos.y -= halfSize.y;
            endPos.z += halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
            
            startPos = origin;
            startPos.x += halfSize.x;
            startPos.y += halfSize.y;
            startPos.z += halfSize.z;
            endPos = origin;
            endPos.x += halfSize.x;
            endPos.y += halfSize.y;
            endPos.z -= halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
            
            startPos = origin;
            startPos.x += halfSize.x;
            startPos.y += halfSize.y;
            startPos.z += halfSize.z;
            endPos = origin;
            endPos.x -= halfSize.x;
            endPos.y += halfSize.y;
            endPos.z += halfSize.z;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, depthTest);
        }
        
        public static void DrawMark(Vector3 point, Color color, float duration)
        {
            Vector3 startPos, endPos;
            
            startPos = point;
            startPos.x += 0.25f;
            endPos = point;
            endPos.x -= 0.25f;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, true);
            
            startPos = point;
            startPos.y += 0.25f;
            endPos = point;
            endPos.y -= 0.25f;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, true);
            
            startPos = point;
            startPos.z += 0.25f;
            endPos = point;
            endPos.z -= 0.25f;
            UnityEngine.Debug.DrawLine(startPos, endPos, color, duration, true);
        }
    }
}
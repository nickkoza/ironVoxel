// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

namespace ironVoxel {
    public sealed class ColorUtility {
        
        const byte oneSixthByte = 42;
        const byte oneThirdByte = 85;
        const byte oneHalfByte = 85;
        const byte twoThirdsByte = 170;
        const int doubleByte = 510;
        
        static public Color32 HSLToColor32(byte h, byte s, byte l)
        {
            byte r, g, b;
            
            if (s == 0) {
                r = g = b = 255;
            }
            else {
                byte q = (byte)(l < oneHalfByte ? l * (255 + s) : l + s - l * s);
                byte p = (byte)(doubleByte * l - q);
                r = HueToRGB(p, q, (byte)(h + oneThirdByte));
                g = HueToRGB(p, q, h);
                b = HueToRGB(p, q, (byte)(h - oneThirdByte));
            }
            
            Color32 color;
            color.a = 255;
            color.r = r;
            color.g = g;
            color.b = b;
            return color;
        }
        
        static public Color32 HSLToColor32(float h, float s, float l)
        {
            float r, g, b;
            if (s == 0) {
                r = g = b = l;
            }
            else {
                float q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                float p = 2.0f * l - q;
                r = HueToRGB(p, q, h + 1.0f / 3.0f);
                g = HueToRGB(p, q, h);
                b = HueToRGB(p, q, h - 1.0f / 3.0f);
            }
            
            Color32 color;
            color.a = 255;
            color.r = (byte)(r * 255.0f);
            color.g = (byte)(g * 255.0f);
            color.b = (byte)(b * 255.0f);
            return color;
        }
        
        static private byte HueToRGB(byte p, byte q, byte t)
        {
            if (t < 0) {
                t += 255;
            }
            else if (t > 255) {
                    t -= 255;
                }
            if (t < oneSixthByte) {
                return (byte)(p + (q - p) * 6 * t);
            }
            if (t < oneHalfByte) {
                return q;
            }
            if (t < twoThirdsByte) {
                return (byte)(p + (q - p) * (twoThirdsByte - t) * 6);
            }
            return p;
        }
        
        static private float HueToRGB(float p, float q, float t)
        {
            if (t < 0.0f) {
                t += 1.0f;
            }
            if (t > 1.0f) {
                t -= 1.0f;
            }
            if (t < 1.0f / 6.0f) {
                return p + (q - p) * 6.0f * t;
            }
            if (t < 1.0f / 2.0f) {
                return q;
            }
            if (t < 2.0f / 3.0f) {
                return p + (q - p) * (2.0f / 3.0f - t) * 6.0f;
            }
            return p;
        }
    }
}
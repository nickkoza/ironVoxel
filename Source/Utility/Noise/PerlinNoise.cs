// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza
// Based on the original Perlin Noise C source code by Ken Perlin.

using UnityEngine;
using System;

namespace ironVoxel {
    public sealed class PerlinNoise {
        private const int B = 0x100;
        private const int BM = 0xff;
        private const int N = 12;
        private const int NM = 0xfff;
        private static System.Random random = new System.Random();
        private static int[] p = new int[B + B + 2];
        private static Vector3Double[] g3 = new Vector3Double[B + B + 2];
        private static bool first = true;
        
        private static double SCurve(double t)
        {
            return t * t * (3.0 - 2.0 * t);
        }

        private static double Lerp(double t, double a, double b)
        {
            return a + t * (b - a);
        }

        private static double At3(Vector3Double q, double rx, double ry, double rz)
        {
            return rx * q.x + ry * q.y + rz * q.z;
        }
        
        public static double Bias(double a, double b)
        {
            return Math.Pow(a, Math.Log(b) / Math.Log(0.5));
        }
        
        public static double Gain(double a, double b)
        {
            double p = Math.Log(1.0 - b) / Math.Log(0.5);
            if (a < 0.001) {
                return 0.0;
            }
            else if (a > 0.999) {
                    return 1.0;
                }
            if (a < 0.5) {
                return Math.Pow(2 * a, p) / 2;
            }
            else {
                return 1.0 - Math.Pow(2 * (1.0 - a), p) / 2;
            }
        }
        
        public static double Turbulence(Vector3Double v, double freq)
        {
            double t;
            Vector3Double vec;
        
            for (t = 0.0f; freq >= 1.0f; freq /= 2) {
                vec.x = freq * v[0];
                vec.y = freq * v[1];
                vec.z = freq * v[2];
                t += Math.Abs(Generate(vec.x, vec.y, vec.z)) / freq;
            }
            return t;
        }
        
        public static double RidgedTurbulence(Vector3Double v, double freq)
        {
            return 0.8 - Math.Abs(Turbulence(v, freq));
        }
        
        public static void SetSeed(int seed)
        {
            random = new System.Random(seed);
            first = true;
        }
        
        public static double Generate(double x, double y, double z)
        {
            // The world generator is designed to operate between 0 and int.MaxValue, so offset into the center of the range:
            x += int.MaxValue * 0.5;
            y += int.MaxValue * 0.5;
            z += int.MaxValue * 0.5;

            int bx0, bx1, by0, by1, bz0, bz1, b00, b10, b01, b11;
            double rx0, rx1, ry0, ry1, rz0, rz1, sy, sz, a, b, c, d, t, u, v;
            Vector3Double q;
            int i, j;
            
            if (first) {
                first = false;
                int k;
                for (i = 0; i < B; i++) {
                    p[i] = i;
                    
                    for (j = 0; j < 3; j++) {
                        g3[i][j] = (double)((random.Next() % (B + B)) - B) / B;
                    }
                    
                    g3[i].Normalize();
                }
                
                while (--i != 0) {
                    k = p[i];
                    p[i] = p[j = random.Next() % B];
                    p[j] = k;
                }
                
                for (i = 0; i < B + 2; i++) {
                    p[B + i] = p[i];
                    
                    for (j = 0; j < 3; j++) {
                        g3[B + i][j] = g3[i][j];
                    }
                }
            }
            
            t = x + N;
            bx0 = ((int)t) & BM;
            bx1 = (bx0 + 1) & BM;
            rx0 = t - (int)t;
            rx1 = rx0 - 1.0;
            
            t = y + N;
            by0 = ((int)t) & BM;
            by1 = (by0 + 1) & BM;
            ry0 = t - (int)t;
            ry1 = ry0 - 1.0;
            
            t = z + N;
            bz0 = ((int)t) & BM;
            bz1 = (bz0 + 1) & BM;
            rz0 = t - (int)t;
            rz1 = rz0 - 1.0;
        
            i = p[bx0];
            j = p[bx1];
            
            b00 = p[i + by0];
            b10 = p[j + by0];
            b01 = p[i + by1];
            b11 = p[j + by1];
        
            t = SCurve(rx0);
            sy = SCurve(ry0);
            sz = SCurve(rz0);

            q = g3[b00 + bz0];
            u = At3(q, rx0, ry0, rz0);
            q = g3[b10 + bz0];
            v = At3(q, rx1, ry0, rz0);
            a = Lerp(t, u, v);
        
            q = g3[b01 + bz0];
            u = At3(q, rx0, ry1, rz0);
            q = g3[b11 + bz0];
            v = At3(q, rx1, ry1, rz0);
            b = Lerp(t, u, v);
        
            c = Lerp(sy, a, b);
        
            q = g3[b00 + bz1];
            u = At3(q, rx0, ry0, rz1);
            q = g3[b10 + bz1];
            v = At3(q, rx1, ry0, rz1);
            a = Lerp(t, u, v);
        
            q = g3[b01 + bz1];
            u = At3(q, rx0, ry1, rz1);
            q = g3[b11 + bz1];
            v = At3(q, rx1, ry1, rz1);
            b = Lerp(t, u, v);
        
            d = Lerp(sy, a, b);
        
            return Lerp(sz, c, d);
        }
    }
}
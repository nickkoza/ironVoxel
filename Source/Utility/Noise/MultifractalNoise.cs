// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza
// Based on the original Multifractal Noise C source code by, I believe, Ken Musgrave (correct if me I'm mistaken.)

using UnityEngine;
using System;

namespace ironVoxel {
    public sealed class MultifractalNoise {
        static bool first = true;
        static double[] exponentArray;

        public static double GenerateRidged(double x, double y, double z, double H, double lacunarity,
                            double octaves, double offset, double gain)
        {
            double result;
            double frequency;
            double signal;
            double weight;
            int i;
            
            // precompute and store spectral weights
            if (first) {
                // seize required memory for exponent_array
                exponentArray = new double[(int)octaves + 1];
                frequency = 1.0f;
                for (i = 0; i <= octaves; i++) {
                    // compute weight for each frequency
                    exponentArray[i] = Math.Pow(frequency, -H);
                    frequency *= lacunarity;
                }
                first = false;
            }
            
            // get first octave
            signal = PerlinNoise.Generate(x, y, z);
            // get absolute value of signal (this creates the ridges)
            if (signal < 0.0) {
                signal = -signal;
            }
            // invert and translate (note that "offset" should be ~= 1.0)
            signal = offset - signal;
            // square the signal, to increase "sharpness" of ridges
            signal *= signal;
            // assign initial values
            result = signal;
            weight = 1.0;
            
            for (i = 1; i < octaves; i++) {
                // increase the frequency
                x *= lacunarity;
                y *= lacunarity;
                z *= lacunarity;
                
                // weight successive contributions by previous signal
                weight = signal * gain;
                if (weight > 1.0) {
                    weight = 1.0;
                }
                if (weight < 0.0) {
                    weight = 0.0;
                }
                signal = PerlinNoise.Generate(x, y, z);
                if (signal < 0.0) {
                    signal = -signal;
                }
                signal = offset - signal;
                signal *= signal;
                // weight the contribution
                signal *= weight;
                result += signal * exponentArray[i];
            }
            return(result);
        }
    }
}

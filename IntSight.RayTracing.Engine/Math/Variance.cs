using System;
using System.Runtime.CompilerServices;

namespace IntSight.RayTracing.Engine
{
    /// <summary>Encapsulates a deviation test.</summary>
    [SkipLocalsInit]
    public struct VarianceTest
    {
        private Pixel sm, sm2;
        private readonly int minSamples;
        private readonly float[] thresholds;

        /// <summary>Initializes a deviation test.</summary>
        /// <param name="totalSamples">Total number of samples.</param>
        /// <param name="minDev">Acceptable deviation.</param>
        public VarianceTest(int totalSamples, double minDev)
        {
            sm = new();
            sm2 = new();
            Samples = 0;
            thresholds = new float[totalSamples];
            minSamples = totalSamples / 2;
            for (int i = 0; i < totalSamples; i++)
                thresholds[i] = (i + 1) *
                    (float)(Math.Max(0.0, (2.0 * i - totalSamples) / totalSamples) * minDev);
        }

        /// <summary>Gets the total number of sampled rays.</summary>
        public int Samples { get; private set; }

        /// <summary>Converts accumulated data into a color.</summary>
        /// <param name="alphaHits">Number of missing rays.</param>
        /// <returns>The resulting color, including the transparence channel.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TransPixel ToColor(int alphaHits)
        {
            float f = 255.0F / Samples;
            var (r, g, b) = sm * f;
            return new(unchecked((uint)(
                (byte)(255 - ((alphaHits << 8) - alphaHits) / Samples) << 24 |
                (byte)r << 16 | (byte)g << 8 | (byte)b)));
        }

        /// <summary>Clears all the accumulated information.</summary>
        public void Reset()
        {
            sm = new();
            sm2 = new();
            Samples = 0;
        }

        /// <summary>Adds a new oversampled pixel to this accumulator.</summary>
        /// <param name="p">New sampled pixel.</param>
        /// <returns>True, if we can stop oversampling; false, otherwise.</returns>
        public bool Test(in Pixel p)
        {
            sm += p; sm2 += p * p;
            if (++Samples >= minSamples)
            {
                float inv = 1F / Samples;
                var v = sm2 - sm * sm * inv;
                float min = thresholds[Samples - 1];
                if (v.Red < min && v.Green < min && v.Blue < min)
                    return true;
            }
            return false;
        }
    }
}

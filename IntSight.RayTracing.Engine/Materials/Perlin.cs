using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static System.Math;
using static System.Runtime.CompilerServices.Unsafe;

namespace IntSight.RayTracing.Engine
{
    /// <summary>Implements Perlin noise.</summary>
    public sealed class SolidNoise
    {
        private static readonly Vector[] grd =
        {
            new(+1.0, +1.0, 0.0), new(-1.0, +1.0, 0.0),
            new(+1.0, -1.0, 0.0), new(-1.0, -1.0, 0.0),
            new(+1.0, 0.0, +1.0), new(-1.0, 0.0, +1.0),
            new(+1.0, 0.0, -1.0), new(-1.0, 0.0, -1.0),
            new(0.0, +1.0, +1.0), new(0.0, -1.0, +1.0),
            new(0.0, +1.0, -1.0), new(0.0, -1.0, -1.0),
            new(+1.0, +1.0, 0.0), new(-1.0, +1.0, 0.0),
            new(0.0, -1.0, +1.0), new(0.0, -1.0, -1.0)
        };

        private readonly int[] phi = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

        private readonly Vector256<double> m6 = Vector256.Create(-6d);
        private readonly Vector256<double> p15 = Vector256.Create(15d);
        private readonly Vector256<double> m10 = Vector256.Create(-10d);
        private readonly Vector256<double> p1 = Vector256.Create(1d);

        /// <summary>Creates a solid noise generator.</summary>
        /// <param name="rnd">A random numbers generator.</param>
        private SolidNoise(Random rnd)
        {
            // Shuffle the phi array.
            for (int i = 0; i < phi.Length; i++)
            {
                int j = rnd.Next(phi.Length);
                int tmp = phi[i];
                phi[i] = phi[j];
                phi[j] = tmp;
            }
        }

        /// <summary>Creates a solid noise generator with a randomized seed.</summary>
        public SolidNoise() : this(new Random()) { }

        /// <summary>Creates a solid noise generator with a fixed seed.</summary>
        /// <param name="seed">Noise generation seed.</param>
        public SolidNoise(int seed) : this(new Random(seed)) { }

        /// <summary>Non-turbulent Perlin's solid noise function.</summary>
        /// <param name="x">X position.</param>
        /// <param name="y">Y position.</param>
        /// <param name="z">Z position.</param>
        /// <returns>A random value between 0 and 1.</returns>
        public double this[double x, double y, double z]
        {
            //[MethodImpl(MethodImplOptions.AggressiveOptimization)]
            get
            {
                int fi = (int)Floor(x), fj = (int)Floor(y), fk = (int)Floor(z);
                x -= fi; y -= fj; z -= fk;
                int fi1, fj1, fk1;
                if (fi < 0) { fi = -fi; fi1 = fi - 1; } else fi1 = fi + 1;
                if (fj < 0) { fj = -fj; fj1 = fj - 1; } else fj1 = fj + 1;
                ref int p = ref phi[0];
                if (fk < 0)
                {
                    fk = -fk;
                    fk1 = Add(ref p, (fk - 1) & 15);
                }
                else
                    fk1 = Add(ref p, (fk + 1) & 15);
                fk = Add(ref p, fk & 15);
                double ox0, oy0, oz0;
                if (Avx.IsSupported)
                {
                    var t = Vector256.Create(x, y, z, 0);
                    var t2 = Avx.Multiply(t, t);
                    var v = Avx.Add(Avx.Multiply(m6, t), p15);
                    v = Avx.Add(Avx.Multiply(v, t), m10);
                    t2 = Avx.Multiply(t2, t);
                    v = Avx.Add(Avx.Multiply(v, t2), p1);
                    ox0 = v.ToScalar();
                    oy0 = v.GetElement(1);
                    oz0 = v.GetElement(2);
                }
                else
                {
                    ox0 = FusedMultiplyAdd(FusedMultiplyAdd(-6.0, x, 15.0), x, -10.0) * x * x * x + 1.0;
                    oy0 = FusedMultiplyAdd(FusedMultiplyAdd(-6.0, y, 15.0), y, -10.0) * y * y * y + 1.0;
                    oz0 = FusedMultiplyAdd(FusedMultiplyAdd(-6.0, z, 15.0), z, -10.0) * z * z * z + 1.0;
                }
                double oy1 = 1.0 - oy0, oz1 = 1.0 - oz0;
                int phi1 = Add(ref p, (fj + fk) & 15);
                int phi2 = Add(ref p, (fj + fk1) & 15);
                int phi3 = Add(ref p, (fj1 + fk) & 15);
                int phi4 = Add(ref p, (fj1 + fk1) & 15);
                ref Vector g = ref grd[0];
                double result =
                    ox0 * (
                        oy0 * (
                            oz0 * Add(ref g, Add(ref p, (fi + phi1) & 15)).Dot(x, y, z) +
                            oz1 * Add(ref g, Add(ref p, (fi + phi2) & 15)).Dot(x, y, z - 1.0)) +
                        oy1 * (
                            oz0 * Add(ref g, Add(ref p, (fi + phi3) & 15)).Dot(x, y - 1.0, z) +
                            oz1 * Add(ref g, Add(ref p, (fi + phi4) & 15)).Dot(x, y - 1.0, z - 1.0)));
                x -= 1.0;
                return result +
                    (1 - ox0) * (
                        oy0 * (
                            oz0 * Add(ref g, Add(ref p, (fi1 + phi1) & 15)).Dot(x, y, z) +
                            oz1 * Add(ref g, Add(ref p, (fi1 + phi2) & 15)).Dot(x, y, z - 1.0)) +
                        oy1 * (
                            oz0 * Add(ref g, Add(ref p, (fi1 + phi3) & 15)).Dot(x, y - 1.0, z) +
                            oz1 * Add(ref g, Add(ref p, (fi1 + phi4) & 15)).Dot(x, y - 1.0, z - 1.0)));
            }
        }

        /// <summary>Turbulent Perlin's solid noise function.</summary>
        /// <param name="x">X position.</param>
        /// <param name="y">Y position.</param>
        /// <param name="z">Z position.</param>
        /// <param name="depth">Turbulence.</param>
        /// <returns>A random value between 0 and 1.</returns>
        public double this[double x, double y, double z, short depth]
        {
            get
            {
                // I'm unrolling the loop inline.
                double result = (this[x, y, z] + 1.0) * 0.5;
                if (--depth > 0)
                {
                    result += (this[x + x, y + y, z + z] + 1.0) * 0.25;
                    double w1 = 2.0, w2 = 4.0;
                    while (--depth > 0)
                    {
                        w1 = w2; w2 += w2;
                        result += (this[w1 * x, w1 * y, w1 * z] + 1.0) / w2;
                    }
                    return result * w1 / (w2 - 1.0);
                }
                else
                    return result;
            }
        }

        private static Pixel HueConversion(double value) => Pixel.FromHue((1.0 + value) * 180.0);

        public static PixelMap GenerateNoise(
            short turbulence, int seed,
            double scaleX, double scaleY, double offsetX, double offsetY,
            int width, int height)
        {
            PixelMap map = new(width, height);
            SolidNoise gen = new(seed);
            double w = scaleX / width, h = scaleY / height;
            int index = 0;
            var pxs = map.pixs;
            if (turbulence == 0)
                for (int row = 0; row < height; row++)
                {
                    double r = offsetY + row * h;
                    for (int col = width - 1; col >= 0; col--)
                        pxs[index++] = HueConversion(gen[offsetX + col * w, r, 0.0]);
                }
            else
                for (int row = 0; row < height; row++)
                {
                    double r = offsetY + row * h;
                    for (int col = width - 1; col >= 0; col--)
                        pxs[index++] = HueConversion(gen[offsetX + col * w, r, 0.0, turbulence]);
                }
            return map;
        }
    }
}

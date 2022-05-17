using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;

namespace IntSight.RayTracing.Engine
{
    /// <summary>A sampler simulating a focus distance.</summary>
    [XSight(Alias = "Focal")]
    public sealed class FocalSampler : BasicSampler
    {
        /// <summary>Square root of the total number of samples by pixel.</summary>
        private readonly int samples;
        /// <summary>Jitter for antialiasing and the thin lens effect.</summary>
        private readonly double[] jitter;
        /// <summary>Maximum allowed variance.</summary>
        private readonly double minDev;
        /// <summary>Must we watch the variance to stop supersampling?</summary>
        private readonly bool checkDev;

        /// <summary>Creates a focal sampler.</summary>
        /// <param name="bounces">Allowed number of bounces.</param>
        /// <param name="minWeight">Minimum ray weight</param>
        /// <param name="aperture">Lens width.</param>
        /// <param name="samples">Samples by pixel.</param>
        /// <param name="mindev">Maximum allowed variance for stopping samples.</param>
        public FocalSampler(
            [Proposed("10")] int bounces,
            [Proposed("0.001")] double minWeight,
            [Proposed("0.1")] double aperture,
            [Proposed("3")] int samples,
            [Proposed("0.00000001")] double mindev)
            : base(bounces, minWeight)
        {
            Aperture = aperture;
            this.samples = samples;
            oversampling = samples * samples;
            minDev = mindev;
            checkDev = mindev > 0.0;
            // Four random numbers are need for each sample:
            // two for the lens, two for the pixel area.
            jitter = new double[oversampling * 4];
            var seed = new Random(9125);
            for (int i = 0; i < jitter.Length; i++)
                jitter[i] = seed.NextDouble();
            int idx = 0;
            for (int x = 0; x < samples; x++)
                for (int y = 0; y < samples; y++)
                {
                    // Bake jitter items.
                    jitter[idx] = jitter[idx] / samples - 0.5;
                    idx++;
                    jitter[idx] = jitter[idx] / samples - 0.5;
                    idx++;
                    jitter[idx] = (-0.5 + (y + jitter[idx]) / samples) * Aperture;
                    idx++;
                    jitter[idx] = (-0.5 + (x + jitter[idx]) / samples) * Aperture;
                    idx++;
                }
        }

        /// <summary>Creates a focal sampler.</summary>
        /// <param name="bounces">Allowed number of bounces.</param>
        /// <param name="minWeight">Minimum ray weight</param>
        /// <param name="aperture">Lens width.</param>
        /// <param name="samples">Samples by pixel.</param>
        [Preferred]
        public FocalSampler(
            [Proposed("10")] int bounces,
            [Proposed("0.001")] double minWeight,
            [Proposed("0.1")] double aperture,
            [Proposed("3")] int samples)
            : this(bounces, minWeight, aperture, samples, 0.0) { }

        /// <summary>Creates a focal sampler with perfect focus.</summary>
        /// <param name="bounces">Allowed number of bounces.</param>
        /// <param name="minWeight">Minimum ray weight</param>
        /// <param name="samples">Samples by pixel.</param>
        public FocalSampler(
            [Proposed("10")] int bounces,
            [Proposed("0.001")] double minWeight,
            [Proposed("3")] int samples)
            : this(bounces, minWeight, 0.0, samples, 0.0) { }

        /// <summary>Create an independent copy of the sampler.</summary>
        /// <returns>The new sampler.</returns>
        public override ISampler Clone() =>
            new FocalSampler(bounces, minWeight, Aperture, samples, minDev);

        /// <summary>Renders a scene using focal blur.</summary>
        /// <param name="strip">Band of the pixel map to render.</param>
        [SkipLocalsInit]
        public override void Render(PixelStrip strip)
        {
            float pixelWeight = 255.0F / oversampling;
            double invSamples = 1.0 / samples;
            int w = camera.Width, superSamples = 0;
            // Create a random match between lens and pixel bins for each new row.
            Span<(double x, double y)> pairs = stackalloc (double x, double y)[oversampling];
            int index = 0;
            for (int x = 0; x < samples; x++)
                for (int y = 0; y < samples; y++)
                    pairs[index++] = (y * invSamples, x * invSamples);
            // Copy the pixel matrix pointer in local memory for faster access.
            ref TransPixel pxs = ref strip.FirstPixel;
            uint randSeed = 9125 * 9125;
            ref double jit = ref jitter[0];
            ref var p0 = ref pairs[0];
            int row;
            if (checkDev)
            {
                // Sampling stops when variance stalls.
                VarianceTest varTest = new(oversampling, minDev);
                while ((row = strip.NextRow()) >= 0)
                {
                    camera.FocusRow(row);
                    // Shuffle pairs.
                    for (int i = 0; i < 4; i++)
                    {
                        randSeed = randSeed * 0x08088405 + 1;
                        int i0 = (int)(((ulong)oversampling * randSeed) >> 32);
                        randSeed = randSeed * 0x08088405 + 1;
                        int i1 = (int)(((ulong)oversampling * randSeed) >> 32);
                        if (i0 != i1)
                        {
                            var tmp = Add(ref p0, i0);
                            Add(ref p0, i0) = Add(ref p0, i1);
                            Add(ref p0, i1) = tmp;
                        }
                    }
                    int col = w;
                    do
                    {
                        ref double jt = ref jit;
                        missingRays = 0;
                        camera.FocusColumn(--col);
                        varTest.Reset();
                        foreach (var (x, y) in pairs)
                        {
                            camera.GetRay(
                                dY: y + jt,
                                dX: x + Add(ref jt, 1),
                                odY: Add(ref jt, 2),
                                odX: Add(ref jt, 3));
                            jt = ref Add(ref jt, 4);
                            superSamples++;
                            if (varTest.Test(Trace()))
                                break;
                        }
                        pxs = varTest.ToColor(missingRays);
                        pxs = ref Add(ref pxs, 1);
                    }
                    while (col > 0);
                }
            }
            else
            {
                // The number of sampling rays is a constant.
                superSamples = strip.Area * oversampling;
                while ((row = strip.NextRow()) >= 0)
                {
                    camera.FocusRow(row);
                    // Shuffle pairs.
                    for (int i = 0; i < 4; i++)
                    {
                        randSeed = randSeed * 0x08088405 + 1;
                        int i0 = (int)(((ulong)oversampling * randSeed) >> 32);
                        randSeed = randSeed * 0x08088405 + 1;
                        int i1 = (int)(((ulong)oversampling * randSeed) >> 32);
                        if (i0 != i1)
                        {
                            var tmp = Add(ref p0, i0);
                            Add(ref p0, i0) = Add(ref p0, i1);
                            Add(ref p0, i1) = tmp;
                        }
                    }
                    int col = w;
                    do
                    {
                        ref double jt = ref jit;
                        camera.FocusColumn(--col);
                        Pixel sample = new();
                        missingRays = 0;
                        foreach ((double x, double y) in pairs)
                        {
                            camera.GetRay(
                                dY: y + jt,
                                dX: x + Add(ref jt, 1),
                                odY: Add(ref jt, 2),
                                odX: Add(ref jt, 3));
                            jt = ref Add(ref jt, 4);
                            sample += Trace();
                        }
                        pxs = sample.ToTransPixel(
                            255 - ((missingRays << 8) - missingRays) / oversampling, pixelWeight);
                        pxs = ref Add(ref pxs, 1);
                    }
                    while (col > 0);
                }
            }
            strip.MaxDepth = Math.Max(0, bounces - lowestBounce);
            strip.SuperSamples = superSamples;
        }
    }
}

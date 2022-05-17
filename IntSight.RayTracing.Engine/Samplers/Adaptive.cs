using System;
using static System.Runtime.CompilerServices.Unsafe;

namespace IntSight.RayTracing.Engine
{
    /// <summary>An adaptive antialias sampler based on edge detection.</summary>
    [XSight(Alias = "Adaptive")]
    public sealed class AdaptiveSampler : BasicSampler
    {
        private readonly int maxSamples;
        private readonly int minSamples;
        private readonly double[] jitter;
        private readonly double[] minJitter;
        private bool jittered;

        /// <summary>Creates an adaptive sampler.</summary>
        /// <param name="bounces">Allowed number of bounces.</param>
        /// <param name="minWeight">Minimum ray weight</param>
        /// <param name="minSamples">Number of rays by pixel for flat zones.</param>
        /// <param name="maxSamples">Number of rays by pixel for edge zones.</param>
        [Preferred]
        public AdaptiveSampler(
            [Proposed("10")] int bounces,
            [Proposed("0.001")] double minWeight,
            [Proposed("1")] int minSamples,
            [Proposed("4")] int maxSamples)
            : base(bounces, minWeight)
        {
            this.minSamples = minSamples;
            this.maxSamples = maxSamples;
            oversampling = minSamples * maxSamples;
            jitter = new double[maxSamples * maxSamples * 2];
            minJitter = new double[minSamples * minSamples * 2];
            // Generate random numbers for jittering
            int idx = 0;
            var seed = new Random(9125);
            foreach (double value in Grid(maxSamples, maxSamples, seed))
                jitter[idx++] = -0.5 + value;
            if (minSamples == 1)
                minJitter[0] = minJitter[1] = 0.0;
            else
            {
                idx = 0;
                foreach (double value in Grid(minSamples, minSamples, seed))
                    minJitter[idx++] = -0.5 + value;
            }
        }

        public AdaptiveSampler(
            [Proposed("10")] int bounces,
            [Proposed("0.001")] double minWeight,
            [Proposed("5")] int maxSamples)
            : this(bounces, minWeight, 3, maxSamples) { }

        public AdaptiveSampler(
            [Proposed("10")] int bounces,
            [Proposed("0.001")] double minWeight)
            : this(bounces, minWeight, 1, 4) { }

        /// <summary>Create an independent copy of the sampler.</summary>
        /// <returns>The new sampler.</returns>
        public override ISampler Clone() =>
            new AdaptiveSampler(bounces, minWeight, minSamples, maxSamples);

        /// <summary>Renders the scene using an edge map for antialiasing.</summary>
        /// <param name="strip">Band of the pixel map to render.</param>
        public override void Render(PixelStrip strip)
        {
            if (!jittered)
            {
                camera.InitJitter(jitter);
                camera.InitJitter(minJitter);
                jittered = true;
            }
            bool[] edgeMap = CreateEdgeMap(strip);
            ref TransPixel p = ref strip.FirstPixel;
            ref double j = ref jitter[0];
            int max2 = maxSamples * maxSamples, min2 = minSamples * minSamples;
            float pixelWeightMax = 255.0F / max2;
            float pixelWeightMin = 255.0F / min2;
            int w = camera.Width, superSamples = 0, edgeIdx = 0, row;
            while ((row = strip.NextRow()) >= 0)
            {
                camera.FocusRow(row);
                int col = w - 1;
                do
                {
                    camera.FocusColumn(col);
                    Pixel sample = new();
                    missingRays = 0;
                    if (edgeMap[edgeIdx++])
                    {
                        int idx = jitter.Length - 1;
                        do
                        {
                            camera.GetRay(Add(ref j, idx--), Add(ref j, idx--));
                            sample += Trace();
                        }
                        // Actually, it should be idx >= 0, but jitter.Length is even.
                        while (idx > 0);
                        p = sample.ToTransPixel(
                            255 - missingRays * 255 / max2, pixelWeightMax);
                        p = ref Add(ref p, 1);
                        superSamples += max2;
                    }
                    else
                    {
                        int idx = minJitter.Length - 1;
                        do
                        {
                            camera.GetRay(minJitter[idx--], minJitter[idx--]);
                            sample += Trace();
                        }
                        while (idx > 0);
                        p = sample.ToTransPixel(
                            255 - missingRays * 255 / min2, pixelWeightMin);
                        p = ref Add(ref p, 1);
                        superSamples += min2;
                    }
                }
                while (--col >= 0);
            }
            strip.MaxDepth = Math.Max(0, bounces - lowestBounce);
            strip.SuperSamples = superSamples;
        }

        /// <summary>Creates the edge map using a basic sampler with the scene.</summary>
        /// <param name="strip">Band of the pixel map to render.</param>
        private bool[] CreateEdgeMap(PixelStrip strip)
        {
            TransPixel[,] matrix = new TransPixel[strip.Height, strip.Width];
            float saveWeight = minWeight;
            int w = strip.Width, saveBounces = bounces;
            try
            {
                minWeight = Math.Max(minWeight, 1.0F / 255.0F);
                bounces = Math.Min(bounces, 4);
                int fromRow = strip.FromRow, toRow = strip.ToRow;
                for (int row = fromRow; row <= toRow; row++)
                {
                    int col = w;
                    do
                    {
                        camera.Focus(row, --col);
                        matrix[row - fromRow, col] = Trace();
                    }
                    while (col > 0);
                }
            }
            finally
            {
                minWeight = saveWeight;
                bounces = saveBounces;
            }
            strip.Preview(matrix);
            return FindEdges(matrix, strip.Height, strip.Width);
        }

        /// <summary>Applies a high-pass filter to a rendered scene to find borders.</summary>
        /// <param name="mx">The color matrix with the rendered scene.</param>
        /// <param name="height">Height in pixels of the rendered scene.</param>
        /// <param name="width">Width in pixels of the rendered scene.</param>
        /// <returns></returns>
        private static bool[] FindEdges(TransPixel[,] mx, int height, int width)
        {
            bool[,] edges = new bool[height, width];
            int h = height - 1, w = width - 1;
            for (int row = 0; row < height; row++)
            {
                edges[row, 0] = true;
                edges[row, w] = true;
            }
            for (int col = 1; col < w; col++)
            {
                edges[0, col] = true;
                edges[h, col] = true;
            }
            for (int r = 1; r < h; r++)
                for (int c = 1; c < w; c++)
                {
                    TransPixel c1 = mx[r - 1, c - 1], c2 = mx[r - 1, c], c3 = mx[r - 1, c + 1];
                    TransPixel c4 = mx[r, c - 1], c5 = mx[r, c], c6 = mx[r, c + 1];
                    TransPixel c7 = mx[r + 1, c - 1], c8 = mx[r + 1, c], c9 = mx[r + 1, c + 1];
                    if (8 * c5.R - c1.R - c2.R - c3.R - c4.R -
                            c6.R - c7.R - c8.R - c9.R > 11 ||
                        8 * c5.G - c1.G - c2.G - c3.G - c4.G -
                            c6.G - c7.G - c8.G - c9.G > 11 ||
                        8 * c5.B - c1.B - c2.B - c3.B - c4.B -
                            c6.B - c7.B - c8.B - c9.B > 11)
                        edges[r, c] = true;
                }
            bool[] result = new bool[height * width];
            int idx = 0;
            for (int r = 0; r < height; r++)
                for (int c = width - 1; c >= 0; c--)
                    result[idx++] = edges[r, c] ||
                        edges[r - 1, c] || edges[r + 1, c] ||
                        edges[r, c - 1] || edges[r, c + 1];
            return result;
        }
    }
}

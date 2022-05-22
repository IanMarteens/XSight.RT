using static System.Runtime.CompilerServices.Unsafe;

namespace IntSight.RayTracing.Engine;

/// <summary>
/// A sampler gathering a fixed number of jittered samples for each pixel.
/// </summary>
[XSight(Alias = "Antialias")]
public sealed class AntialiasSampler : BasicSampler
{
    /// <summary>Square root of the total number of samples by pixel.</summary>
    private readonly int samples;
    /// <summary>Jitter for antialiasing.</summary>
    private readonly double[] jitter;
    /// <summary>Maximum allowed variance.</summary>
    private readonly double minDev;
    /// <summary>Must we watch the variance to stop supersampling?</summary>
    private readonly bool checkDev;
    /// <summary>Has the jittered been modified according to the camera?</summary>
    private bool jittered;

    /// <summary>Creates an antialias sampler.</summary>
    /// <param name="bounces">Allowed number of bounces.</param>
    /// <param name="minWeight">Minimum ray weight</param>
    /// <param name="samples">Number of rays by pixel.</param>
    /// <param name="mindev">Maximum allowed variance for stopping samples.</param>
    public AntialiasSampler(
        [Proposed("10")] int bounces,
        [Proposed("0.001")] double minWeight,
        [Proposed("3")] int samples,
        [Proposed("0.00000001")] double mindev)
        : base(bounces, minWeight)
    {
        this.samples = samples;
        oversampling = samples * samples;
        minDev = mindev;
        checkDev = mindev > 0.0;
        jitter = new double[oversampling * 2];
        // Generate random numbers for jittering
        int idx = 0;
        foreach (double value in Grid(samples, samples, new Random(9125)))
            jitter[idx++] = -0.5 + value;
        // Scramble jitter pairs.
        var seed = new Random(9125);
        for (int i = 0; i < oversampling; i += 2)
        {
            // Both i and j must be even numbers.
            int j = seed.Next(oversampling) & 0x7ffffffe;
            if (j != i)
            {
                var t = jitter[i]; jitter[i] = jitter[j]; jitter[j] = t;
                t = jitter[i + 1]; jitter[i + 1] = jitter[j + 1]; jitter[j + 1] = t;
            }
        }
    }

    /// <summary>Creates an antialias sampler.</summary>
    /// <param name="bounces">Allowed number of bounces.</param>
    /// <param name="minWeight">Minimum ray weight</param>
    /// <param name="samples">Number of rays by pixel.</param>
    [Preferred]
    public AntialiasSampler(
        [Proposed("10")] int bounces,
        [Proposed("0.001")] double minWeight,
        [Proposed("3")] int samples)
        : this(bounces, minWeight, samples, 0.0) { }

    /// <summary>Creates an independent copy of the sampler.</summary>
    /// <returns>The new antialias sampler.</returns>
    public override ISampler Clone() =>
        new AntialiasSampler(bounces, minWeight, samples, minDev);

    /// <summary>Renders a scene using jittered rays.</summary>
    /// <param name="strip">Band of the pixel map to render.</param>
    public override void Render(PixelStrip strip)
    {
        // Scale jitter according to the needs of the camera.
        if (!jittered)
        {
            camera.InitJitter(jitter);
            jittered = true;
        }
        ref TransPixel p = ref strip.FirstPixel;
        int w = camera.Width, superSamples = 0;
        if (checkDev)
        {
            VarianceTest varTest = new(oversampling, minDev);
            ref double jit = ref jitter[0];
            int row;
            while ((row = strip.NextRow()) >= 0)
            {
                camera.FocusRow(row);
                int col = w;
                do
                {
                    varTest.Reset();
                    camera.FocusColumn(--col);
                    missingRays = 0;
                    int idx = jitter.Length - 1;
                    do
                    {
                        camera.GetRay(Add(ref jit, idx--), Add(ref jit, idx--));
                        if (varTest.Test(Trace()))
                            break;
                    }
                    // Actually, it should be idx >= 0, but jitter.Length is even.
                    while (idx > 0);
                    p = varTest.ToColor(missingRays);
                    p = ref Add(ref p, 1);
                    superSamples += varTest.Samples;
                }
                while (col > 0);
            }
        }
        else
        {
            superSamples = strip.Area * oversampling;
            float pixelWeight = 255.0F / oversampling;
            ref double jit = ref jitter[0];
            int row;
            while ((row = strip.NextRow()) >= 0)
            {
                camera.FocusRow(row);
                int col = w;
                do
                {
                    missingRays = 0;
                    camera.FocusColumn(--col);
                    Pixel sample = new();
                    int idx = jitter.Length - 1;
                    do
                    {
                        camera.GetRay(Add(ref jit, idx--), Add(ref jit, idx--));
                        sample += Trace();
                    }
                    // Actually, it should be idx >= 0, but jitter.Length is even.
                    while (idx > 0);
                    p = sample.ToTransPixel(
                        255 - missingRays * 255 / oversampling, pixelWeight);
                    p = ref Add(ref p, 1);
                }
                while (col > 0);
            }
        }
        strip.MaxDepth = Math.Max(0, bounces - lowestBounce);
        strip.SuperSamples = superSamples;
    }
}

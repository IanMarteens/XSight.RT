using static System.Runtime.CompilerServices.Unsafe;

namespace IntSight.RayTracing.Engine;

/// <summary>A constant ambient light.</summary>
[XSight]
public sealed class ConstantAmbient : IAmbient
{
    [Preferred]
    public ConstantAmbient([Proposed("0.10")] double intensity) =>
        Color = new Pixel(intensity);

    public ConstantAmbient([Proposed("0.10")] Pixel color) =>
        Color = color;

    public Pixel Color { get; }
    public double Intensity => Color.GrayLevel;

    #region IAmbient members

    /// <summary>Initializes an ambient light before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void IAmbient.Initialize(IScene scene) { }

    /// <summary>Creates an independent thread-safe copy of this ambient light.</summary>
    /// <returns>The same ambient light, since it's a stateless object.</returns>
    IAmbient IAmbient.Clone() => this;

    /// <summary>Gets the ambient light intensity at a given point.</summary>
    /// <param name="location">The point sampled.</param>
    /// <param name="normal">Normal vector at the hit location.</param>
    /// <returns>Ambient light contribution at the sampled point.</returns>
    Pixel IAmbient.this[in Vector location, in Vector normal] => Color;

    #endregion
}

/// <summary>Ambient source with squared distance intensity decay.</summary>
[XSight]
[method: Preferred]
public sealed class LocalAmbient(
    [Proposed("[0,0,0]")] Vector center,
    [Proposed("rgb 0.10")] Pixel color,
    [Proposed("10")] double fade) : IAmbient
{
    private readonly double inv = Tolerance.Zero(fade) ? 0.0 : 1.0 / (fade * fade);

    public LocalAmbient(double x0, double y0, double z0, Pixel color, double fade)
        : this(new Vector(x0, y0, z0), color, fade) { }

    #region IAmbient members

    /// <summary>Initializes an ambient light before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void IAmbient.Initialize(IScene scene) { }

    /// <summary>Creates an independent thread-safe copy of this ambient light.</summary>
    /// <returns>The same ambient light, since it's a stateless object.</returns>
    IAmbient IAmbient.Clone() => this;

    /// <summary>Gets the ambient light intensity at a given point.</summary>
    /// <param name="location">The point sampled.</param>
    /// <param name="normal">Normal vector at the hit location.</param>
    /// <returns>Ambient light contribution at the sampled point.</returns>
    Pixel IAmbient.this[in Vector location, in Vector normal] =>
        color * (1.0F / (1.0F + (float)((location - center).Squared * inv)));

    #endregion
}

[XSight]
[method: Preferred]
public sealed class LightSource(
    [Proposed("[0,0,0]")] Vector center,
    [Proposed("rgb 0.10")] Pixel color1,
    [Proposed("rgb 1.00")] Pixel color2,
    [Proposed("10")] double radius) : IAmbient
{
    private readonly double r2 = radius * radius;

    public LightSource(double x0, double y0, double z0,
        Pixel color1, Pixel color2, double radius)
        : this(new Vector(x0, y0, z0), color1, color2, radius) { }

    #region IAmbient members

    /// <summary>Initializes an ambient light before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void IAmbient.Initialize(IScene scene) { }

    /// <summary>Creates an independent thread-safe copy of this ambient light.</summary>
    /// <returns>The same ambient light, since it's a stateless object.</returns>
    IAmbient IAmbient.Clone() => new LightSource(center, color1, color2, radius);

    /// <summary>Gets the ambient light intensity at a given point.</summary>
    /// <param name="location">The point sampled.</param>
    /// <param name="normal">Normal vector at the hit location.</param>
    /// <returns>Ambient light contribution at the sampled point.</returns>
    Pixel IAmbient.this[in Vector location, in Vector normal] =>
        (location - center).Squared <= r2 ? color2 : color1;

    #endregion
}

/// <summary>Fill light with environment occlusion.</summary>
[XSight(Alias = "occluder")]
public sealed class AmbientOccluder(Pixel minColor, Pixel maxColor, int samples) : IAmbient
{
    private readonly Pixel delta = maxColor - minColor;
    private int cacheSize, idx;
    private float factor;
    private Vector[] r;
    private readonly Vector seed = new Vector(0.0072, 1.0000, 0.0034).Normalized();
    /// <summary>An auxiliary ray for occlusion tests.</summary>
    private readonly Ray testRay = new();
    /// <summary>The root of the scene tree.</summary>
    private IShape rootShape;
    /// <summary>Last occluder for this light.</summary>
    private IShape occluder;

    public AmbientOccluder(double minColor, double maxColor, int samples)
        : this(new Pixel(minColor), new Pixel(maxColor), samples) { }

    public AmbientOccluder(Pixel color, int samples)
        : this(new Pixel(), color, samples) { }

    [Preferred]
    public AmbientOccluder(
        [Proposed("0.25")] double color,
        [Proposed("16")] int samples)
        : this(new Pixel(), new Pixel(color), samples) { }

    #region IAmbient Members

    /// <summary>Initializes an ambient light before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void IAmbient.Initialize(IScene scene)
    {
        // Adjust samples to fit in a rectangular grid.
        int h = (int)Math.Sqrt(samples);
        int w = h * h < samples ? h + 1 : h;
        samples = h * w;
        factor = 1.0F / samples;
        cacheSize = samples * scene.Sampler.Oversampling;
        r = new Vector[cacheSize];
        int i = 0;
        Random seed = new(9125);
        for (int times = scene.Sampler.Oversampling; times-- > 0;)
            using (var it = SamplerBase.Grid(w, h, seed).GetEnumerator())
                while (it.MoveNext())
                {
                    double cosTheta = Math.Sqrt(1.0 - it.Current);
                    double sinTheta = Math.Sqrt(it.Current);
                    it.MoveNext();
                    double angle = 2 * Math.PI * it.Current;
                    r[i] = new(
                        Math.Cos(angle) * sinTheta, Math.Sin(angle) * sinTheta, cosTheta);
                    i++;
                }
        idx = 0;
        rootShape = scene.Root;
    }

    /// <summary>Gets the ambient light intensity at a given point.</summary>
    /// <param name="location">The point sampled.</param>
    /// <param name="normal">Normal vector at the hit location.</param>
    /// <returns>Ambient light contribution at the sampled point.</returns>
    Pixel IAmbient.this[in Vector location, in Vector normal]
    {
        get
        {
            Vector v = (normal ^ seed).Normalized();
            Vector u = v ^ normal;
            testRay.Origin = location;
            ref Vector r0 = ref r[0];
            int hits = 0;
            var saveOccluder = BaseLight.lastOccluder;
            for (int i = samples; i > 0; i--)
            {
                Vector sp = Add(ref r0, idx);
                testRay.Direction = new(
                    sp.X * u.X + sp.Y * v.X + sp.Z * normal.X,
                    sp.X * u.Y + sp.Y * v.Y + sp.Z * normal.Y,
                    sp.X * u.Z + sp.Y * v.Z + sp.Z * normal.Z);
                testRay.SquaredDir = testRay.Direction.Squared;
                if (occluder != null)
                {
                    if (occluder.ShadowTest(testRay))
                        goto NEXT;
                    occluder = null;
                }
                if (rootShape.ShadowTest(testRay))
                    occluder = BaseLight.lastOccluder;
                else
                    hits++;
                NEXT:
                if (++idx == cacheSize)
                    idx = 0;
            }
            BaseLight.lastOccluder = saveOccluder;
            return minColor.Lerp(delta, hits * factor);
        }
    }

    /// <summary>Creates an independent thread-safe copy of this ambient light.</summary>
    /// <returns>The same ambient light, since it's a stateless object.</returns>
    IAmbient IAmbient.Clone() =>
        new AmbientOccluder(minColor, maxColor, samples);

    #endregion
}

/// <summary>Represents the sum of several ambients.</summary>
public sealed class ComboAmbient(Pixel threshold, params IAmbient[] ambients) : IAmbient
{
    public ComboAmbient(params IAmbient[] ambients)
        : this(Pixel.White, ambients) { }

    #region IAmbient members

    /// <summary>Initializes an ambient light before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void IAmbient.Initialize(IScene scene)
    {
        foreach (IAmbient ambient in ambients)
            ambient.Initialize(scene);
    }

    /// <summary>Creates an independent thread-safe copy of this ambient light.</summary>
    /// <returns>The same ambient light, since it's a stateless object.</returns>
    IAmbient IAmbient.Clone()
    {
        IAmbient[] list = new IAmbient[ambients.Length];
        for (int i = 0; i < list.Length; i++)
            list[i] = ambients[i].Clone();
        return new ComboAmbient(threshold, list);
    }

    /// <summary>Gets the ambient light intensity at a given point.</summary>
    /// <param name="location">The point sampled.</param>
    /// <param name="normal">Normal vector at the hit location.</param>
    /// <returns>The sum of ambient lights' contributions at the sampled point.</returns>
    Pixel IAmbient.this[in Vector location, in Vector normal]
    {
        get
        {
            Pixel p = new();
            foreach (IAmbient amb in ambients)
                p += amb[location, normal];
            return p.Clip(threshold);
        }
    }

    #endregion
}

using static System.Runtime.CompilerServices.Unsafe;

namespace IntSight.RayTracing.Engine;

/// <summary>A common base class for all lights.</summary>
/// <remarks>Initializes a base light.</remarks>
/// <param name="location">Light source's location.</param>
/// <param name="color">Light's color.</param>
public abstract class BaseLight(Vector location, Pixel color)
{
    /// <summary>Last shape to succeed in an occlusion test.</summary>
    [ThreadStatic]
    internal static IShape lastOccluder;

    /// <summary>An auxiliary ray for occlusion tests.</summary>
    protected readonly Ray testRay = new();
    /// <summary>The root of the scene tree.</summary>
    protected IShape rootShape;
    /// <summary>The color of the light source.</summary>
    protected readonly Pixel color = color;
    /// <summary>Coordinates of the light source.</summary>
    protected Vector loc = location;
    /// <summary>Last occluder for this light.</summary>
    protected IShape occluder;
    /// <summary>Is this light located at the same point as the camera?</summary>
    /// <remarks>When the answer is yes, occlusion testing can be omitted.</remarks>
    protected bool useCameraLocation;

    /// <summary>Gets the location of the light source.</summary>
    public Vector Location => loc;

    /// <summary>Gets the light color.</summary>
    public Pixel Color => color;

    /// <summary>Initializes a light source before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    protected void Initialize(IScene scene, bool allowCameraLocation)
    {
        rootShape = scene.Root;
        ICamera cam = scene.Camera;
        if (useCameraLocation)
            // Copy the camera location.
            loc = cam.Location;
        else if (allowCameraLocation)
            useCameraLocation = cam.Location == Location;
    }

    /// <summary>Computes the direction the light comes from at a given location.</summary>
    /// <param name="hitPoint">Sampling point.</param>
    /// <returns>Light's direction.</returns>
    public Vector GetDirection(in Vector hitPoint) => (loc - hitPoint).Normalized();

    /// <summary>Gets a quick estimation of light's intensity.</summary>
    /// <param name="hitPoint">Location where intensity will be sampled.</param>
    /// <returns>0.0F for total obstruction, 1.0F for no obstruction.</returns>
    /// <remarks>Implements the <see cref="ILight.DraftIntensity"/> method.</remarks>
    public float DraftIntensity(in Vector hitPoint) =>
        !useCameraLocation &&
            rootShape.ShadowTest(testRay.FromTo(hitPoint, Location)) ? 0.0F : 1.0F;

    /// <summary>Optimizes the order of a shape list.</summary>
    /// <param name="shapeList">List of shapes to sort.</param>
    /// <returns>The same list, or a new one with a different order.</returns>
    /// <remarks>Implements the <see cref="ILight.Sort"/> method.</remarks>
    public IShape[] Sort(IShape[] shapeList)
    {
        if (useCameraLocation)
            return shapeList;
        var result = (IShape[])shapeList.Clone();
        Array.Sort(result, (s1, s2) => s1.CompareTo(s2, Location));
        return result;
    }
}

/// <summary>A point light source.</summary>
/// <remarks>Creates a point light source.</remarks>
/// <param name="location">Light's location.</param>
/// <param name="color">Light's color.</param>
[XSight(Alias = "Point")]
public sealed class PointLight(Vector location, Pixel color) : BaseLight(location, color), ILight
{

    /// <summary>Creates a white point light source.</summary>
    /// <param name="location">Light's location.</param>
    public PointLight(Vector location)
        : this(location, Pixel.White) { }

    public PointLight(double x, double y, double z, Pixel color)
        : this(new Vector(x, y, z), color) { }

    public PointLight(double x, double y, double z)
        : this(new Vector(x, y, z), Pixel.White) { }

    public PointLight(double x, double y, double z, double brightness)
        : this(new Vector(x, y, z), new Pixel(brightness)) { }

    /// <summary>Creates a point light located at the same position as the camera.</summary>
    /// <param name="color">Light's color.</param>
    public PointLight(Pixel color)
        : this(Vector.Null, color) => useCameraLocation = true;

    /// <summary>Creates a point light located at the same position as the camera.</summary>
    /// <param name="brightness">Light's intensity.</param>
    public PointLight(double brightness)
        : this(new Pixel(brightness)) { }

    /// <summary>Creates a point light located at the same position as the camera.</summary>
    [Preferred]
    public PointLight() : this(Pixel.White) { }

    #region ILight members.

    /// <summary>Creates an independent copy of this light source.</summary>
    /// <returns>A new point light source.</returns>
    ILight ILight.Clone() =>
        new PointLight(Location, Color) { useCameraLocation = useCameraLocation };

    /// <summary>Initializes a light source before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void ILight.Initialize(IScene scene) => Initialize(scene, true);

    /// <summary>Light intensity at a point.</summary>
    /// <param name="hitPoint">Location where intensity will be sampled.</param>
    /// <param name="isPrimary">Are we sampling an intersection from a primary ray?</param>
    /// <returns>0.0F for total obstruction, 1.0F for no obstruction.</returns>
    float ILight.Intensity(in Vector hitPoint, bool isPrimary)
    {
        if (!useCameraLocation || !isPrimary)
        {
            testRay.FromTo(hitPoint, Location);
            if (occluder != null)
            {
                if (occluder.ShadowTest(testRay))
                    return 0.0F;
                occluder = null;
            }
            lastOccluder = null;
            if (rootShape.ShadowTest(testRay))
            {
                occluder = lastOccluder;
                return 0.0F;
            }
        }
        return 1.0F;
    }

    #endregion
}

/// <summary>A point light emitting rays inside a cone.</summary>
[XSight(Alias = "Spot")]
public sealed class SpotLight : BaseLight, ILight
{
    private Vector target;
    private readonly double angle;
    private readonly double penumbra;
    private readonly double cosine0;
    private readonly double cosine1;
    private readonly double deltaCos;
    private double deltax, deltay, deltaz;
    private bool useCameraTarget;

    public SpotLight(
        [Proposed("[0,10,0]")] Vector location,
        [Proposed("[0,0,0]")] Vector target,
        [Proposed("60")] double angle,
        [Proposed("75")] double penumbra,
        [Proposed("White")] Pixel color)
        : base(location, color)
    {
        this.target = target;
        if (penumbra < angle)
            penumbra = angle;
        this.angle = angle;
        this.penumbra = penumbra;
        // The cone angle is halved for computing the cosine threshold.
        cosine0 = Math.Cos(Math.PI * angle / 360.0);
        cosine1 = Math.Cos(Math.PI * penumbra / 360.0);
        deltaCos = cosine0 - cosine1;
        if (Tolerance.Zero(deltaCos))
            deltaCos = 0.0;
    }

    [Preferred]
    public SpotLight(
        [Proposed("[0,10,0]")] Vector location,
        [Proposed("[0,0,0]")] Vector target,
        [Proposed("60")] double angle,
        [Proposed("White")] Pixel color)
        : this(location, target, angle, angle, color) { }

    public SpotLight(
        [Proposed("[0,0,0]")] Vector target,
        [Proposed("60")] double angle,
        [Proposed("White")] Pixel color)
        : this(Vector.Null, target, angle, angle, color) => useCameraLocation = true;

    public SpotLight(
        [Proposed("60")] double angle,
        [Proposed("White")] Pixel color)
        : this(Vector.Null, Vector.Null, angle, angle, color) =>
        useCameraLocation = useCameraTarget = true;

    #region ILight members.

    /// <summary>Creates an independent copy of this light source.</summary>
    /// <returns>The new light source.</returns>
    ILight ILight.Clone() =>
        new SpotLight(Location, target, angle, penumbra, Color)
        {
            useCameraLocation = useCameraLocation,
            useCameraTarget = useCameraTarget
        };

    /// <summary>Initializes a light source before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void ILight.Initialize(IScene scene)
    {
        Initialize(scene, true);
        ICamera cam = scene.Camera;
        if (useCameraTarget)
            target = cam.Target;
        else
            useCameraTarget = cam.Target == target;
        (deltax, deltay, deltaz) = target.Difference(Location);
    }

    /// <summary>Light intensity at a point.</summary>
    /// <param name="hitPoint">Location where intensity will be sampled.</param>
    /// <param name="isPrimary">Are we sampling an intersection from a primary ray?</param>
    /// <returns>0.0F for total obstruction, 1.0F for no obstruction.</returns>
    float ILight.Intensity(in Vector hitPoint, bool isPrimary)
    {
        var (dx, dy, dz) = loc - hitPoint;
        double r2 = dx * dx + dy * dy + dz * dz;
        double len = Math.Sqrt(r2);
        if (len < Tolerance.Epsilon ||
            (len = -(deltax * dx + deltay * dy + deltaz * dz) / len - cosine1) <= 0.0)
            return 0.0F;
        if (!(useCameraLocation && isPrimary))
        {
            testRay.Origin = hitPoint;
            testRay.Direction = new(dx, dy, dz);
            testRay.SquaredDir = r2;
            if (occluder != null && occluder.ShadowTest(testRay))
                return 0.0F;
            lastOccluder = null;
            if (rootShape.ShadowTest(testRay))
            {
                occluder = lastOccluder;
                return 0.0F;
            }
            occluder = null;
        }
        return len >= deltaCos ? 1.0F : (float)(len / deltaCos);
    }

    #endregion
}

/// <summary>A light source with rays starting at random points in a sphere.</summary>
[XSight(Alias = "Spheric")]
public sealed class SphericLight : BaseLight, ILight
{
    private readonly double radius;
    private float factor;
    private readonly float factor2;
    private int samples;
    private readonly int samples2;
    private int cacheSize;
    private int idx;
    private double[] r;

    /// <summary>Creates a spheric light source.</summary>
    /// <param name="location">The center of the sphere.</param>
    /// <param name="radius">Radius of the sphere.</param>
    /// <param name="color">Color of the light source.</param>
    /// <param name="samples">Number of sample for the occlusion test.</param>
    public SphericLight(
        [Proposed("[0,10,0]")] Vector location,
        [Proposed("1.0")] double radius,
        [Proposed("White")] Pixel color,
        [Proposed("16")] int samples)
        : base(location, color)
    {
        this.radius = radius;
        this.samples = samples;
        factor = 1.0F / samples;
        if (samples > 5)
        {
            samples2 = samples * 3 / 5;
            factor2 = 1.0F / samples2;
        }
        else
        {
            samples2 = samples;
            factor2 = factor;
        }
    }

    public SphericLight(
        [Proposed("1.0")] double radius,
        [Proposed("White")] Pixel color,
        [Proposed("16")] int samples)
        : this(Vector.Null, radius, color, samples) => useCameraLocation = true;

    public SphericLight(
        [Proposed("[0,10,0]")] Vector location,
        [Proposed("1.0")] double radius,
        [Proposed("16")] int samples)
        : this(location, radius, Pixel.White, samples) { }

    [Preferred]
    public SphericLight(
        [Proposed("1.0")] double radius,
        [Proposed("16")] int samples)
        : this(Vector.Null, radius, Pixel.White, samples) => useCameraLocation = true;

    public SphericLight(double x, double y, double z, double radius, int samples)
        : this(new Vector(x, y, z), radius, Pixel.White, samples) { }

    public SphericLight(double x, double y, double z, double radius,
        double brightness, int samples)
        : this(new Vector(x, y, z), radius, new Pixel(brightness), samples) { }

    #region ILight members.

    /// <summary>Creates an independent copy of this light source.</summary>
    /// <returns>The new light source.</returns>
    ILight ILight.Clone() =>
        new SphericLight(Location, radius, Color, samples)
        {
            useCameraLocation = useCameraLocation
        };

    /// <summary>Initializes a light source before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void ILight.Initialize(IScene scene)
    {
        Initialize(scene, true);
        // Adjust samples to fit in a rectangular grid.
        int h = (int)Math.Sqrt(samples);
        int w = h * h < samples ? h + 1 : h;
        samples = h * w;
        factor = 1.0F / samples;
        cacheSize = (scene.Sampler.Oversampling << 1) * samples;
        r = new double[cacheSize];
        int i = 0;
        Random seed = new(9125);
        for (int times = scene.Sampler.Oversampling; times-- > 0;)
            using (var it = SamplerBase.Grid(w, h, seed).GetEnumerator())
                while (it.MoveNext())
                {
                    double rad = radius * Math.Sqrt(it.Current);
                    it.MoveNext();
                    double angle = 2 * Math.PI * it.Current;
                    r[i++] = rad * Math.Cos(angle);
                    r[i++] = rad * Math.Sin(angle);
                }
        idx = 0;
    }

    /// <summary>
    /// Light intensity at a point, considering partial and total obstruction.
    /// </summary>
    /// <param name="hitPoint">Location where intensity will be sampled.</param>
    /// <param name="isPrimary">Are we sampling an intersection from a primary ray?</param>
    /// <returns>0.0F for total obstruction, 1.0F for no obstruction.</returns>
    float ILight.Intensity(in Vector hitPoint, bool isPrimary)
    {
        var (dx, dy, dz) = loc - hitPoint;
        double ux, uy, vx, vy, vz;
        double len2 = dx * dx + dy * dy;
        if (len2 >= Tolerance.Epsilon)
        {
            double len = 1.0 / Math.Sqrt((dz * dz + len2) * len2);
            vx = dx * dz * len;
            vy = dy * dz * len;
            vz = -len2 * len;
            ux = -dy / (len2 = Math.Sqrt(len2));
            uy = dx / len2;
        }
        else
        {
            // Actually, "u" should be: [delta.Z, 0.0, -delta.X],
            // but dX and dY are 0.0, so dZ must be 1.0.
            ux = +1.0; uy = 0.0;
            vx = 0.0; vy = -1.0; vz = 0.0;
        }
        ref double rf = ref r[0];
        int i = isPrimary ? samples : samples2, hits = 0;
        while (--i >= 0)
        {
            double f1 = Add(ref rf, idx++), f2 = Add(ref rf, idx++);
            testRay.Direction = new(
                f1 * ux + f2 * vx + dx,
                f1 * uy + f2 * vy + dy,
                // "uz" is always zero.
                Math.FusedMultiplyAdd(f2, vz, dz));
            testRay.SquaredDir = testRay.Direction.Squared;
            testRay.Origin = hitPoint;
            if (occluder != null)
            {
                if (occluder.ShadowTest(testRay))
                    goto NEXT;
                occluder = null;
            }
            if (rootShape.ShadowTest(testRay))
                occluder = lastOccluder;
            else
                hits++;
            NEXT:
            if (idx == cacheSize)
                idx = 0;
        }
        return (isPrimary ? factor : factor2) * hits;
    }

    #endregion
}

/// <summary>A light source with parallel light rays.</summary>
[XSight(Alias = "Parallel")]
public sealed class ParallelLight : BaseLight, ILight
{
    private Vector normal, direction;
    private bool useCameraTarget;

    [Preferred]
    public ParallelLight(
        [Proposed("1000^Y")] Vector location,
        [Proposed("^0")] Vector target,
        [Proposed("White")] Pixel color)
        : base(location, color)
    {
        direction = location - target;
        normal = direction.Normalized();
    }

    public ParallelLight(
        [Proposed("White")] Pixel color,
        [Proposed("1000^Y")] Vector location,
        [Proposed("^0")] Vector target)
        : this(location, target, color) { }

    public ParallelLight(
        [Proposed("1000^Y")] Vector location,
        [Proposed("^0")] Vector target)
        : this(location, target, Pixel.White) { }

    public ParallelLight(
        [Proposed("1000^Y")] Vector location)
        : this(location, Vector.Null, Pixel.White) => useCameraTarget = true;

    #region ILight members.

    /// <summary>Creates an independent copy of the light source.</summary>
    /// <returns>The new light object.</returns>
    ILight ILight.Clone() =>
        new ParallelLight(Location, Location - direction, Color)
        {
            useCameraTarget = useCameraTarget
        };

    /// <summary>Initializes a light source before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    void ILight.Initialize(IScene scene)
    {
        Initialize(scene, false);
        if (useCameraTarget)
        {
            direction = Location - scene.Camera.Target;
            normal = direction.Norm();
        }
        // ShadowTest doesn't destroy the values from the ray.
        // So, we fix the ray's direction here, only once.
        testRay.Direction = direction;
        testRay.SquaredDir = direction.Squared;
    }

    /// <summary>Computes the direction the light comes from at a given location.</summary>
    /// <param name="hitPoint">Sampling point.</param>
    /// <returns>Light's direction.</returns>
    Vector ILight.GetDirection(in Vector hitPoint) => normal;

    /// <summary>Light intensity at a point.</summary>
    /// <param name="hitPoint">Location where intensity will be sampled.</param>
    /// <param name="isPrimary">Are we sampling an intersection from a primary ray?</param>
    /// <returns>0.0F for total obstruction, 1.0F for no obstruction.</returns>
    float ILight.Intensity(in Vector hitPoint, bool isPrimary)
    {
        testRay.Origin = hitPoint;
        if (occluder != null)
        {
            if (occluder.ShadowTest(testRay))
                return 0.0F;
            occluder = null;
        }
        lastOccluder = null;
        if (rootShape.ShadowTest(testRay))
        {
            occluder = lastOccluder;
            return 0.0F;
        }
        return 1.0F;
    }

    /// <summary>Gets a quick estimation of light's intensity.</summary>
    /// <param name="hitPoint">Location where intensity will be sampled.</param>
    /// <returns>0.0F for total obstruction, 1.0F for no obstruction.</returns>
    float ILight.DraftIntensity(in Vector hitPoint)
    {
        testRay.Origin = hitPoint;
        return rootShape.ShadowTest(testRay) ? 0.0F : 1.0F;
    }

    #endregion
}

/// <summary>Photons are included using a fake <see cref="ILight"/> implementation.</summary>
[XSight]
public sealed class Photons : ILight
{
    /// <summary>Default number of traced photons.</summary>
    public const int TOTAL_PHOTONS = 1_000_000;

    /// <summary>Creates a photon source with the default properties.</summary>
    public Photons() : this(TOTAL_PHOTONS) { }

    /// <summary>Creates a photon source.</summary>
    /// <param name="totalPhotons">Number of emitted photons.</param>
    public Photons(int totalPhotons) :
        this(totalPhotons, (int)Math.Sqrt(totalPhotons))
    { }

    /// <summary>Creates a photon source.</summary>
    /// <param name="totalPhotons">Number of emitted photons.</param>
    /// <param name="gatherCount">Number of gathered photons.</param>
    public Photons(int totalPhotons, int gatherCount) =>
        (TotalPhotons, GatherCount) = (totalPhotons, gatherCount);

    /// <summary>Total number of photons to be emitted.</summary>
    public int TotalPhotons { get; }

    /// <summary>Number of photons for the irradiance estimate.</summary>
    public int GatherCount { get; }

    /// <summary>Gets the light color.</summary>
    public Pixel Color => Pixel.White;

    /// <summary>Gets the light's location.</summary>
    public Vector Location => Vector.Null;

    /// <summary>Creates an independent copy of the light source.</summary>
    public ILight Clone() => this;
    /// <summary>Gets a quick estimation of light's intensity.</summary>
    public float DraftIntensity(in Vector hitPoint) => default;
    /// <summary>Computes the direction the light comes from at a given location.</summary>
    public Vector GetDirection(in Vector hitPoint) => Vector.YRay;
    /// <summary>Initializes a light source before rendering.</summary>
    public void Initialize(IScene scene) { }
    /// <summary>Light intensity at a point, with partial and total occlusion.</summary>
    public float Intensity(in Vector hitPoint, bool isPrimary) => default;
    /// <summary>Optimizes the order of a shape list.</summary>
    public IShape[] Sort(IShape[] shapeList) => shapeList;
}

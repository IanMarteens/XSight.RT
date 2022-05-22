using System.Diagnostics.CodeAnalysis;

namespace IntSight.RayTracing.Engine;

/// <summary>The common base for all camera classes.</summary>
public abstract class BasicCamera
{
    /// <summary>The common origin for all camera rays.</summary>
    protected readonly Vector loc;
    /// <summary>Vector from location to target.</summary>
    protected readonly Vector delta;
    protected Vector up;
    protected readonly int width, height;
    /// <summary>Distance to target.</summary>
    protected readonly double distance;
    /// <summary>Delta.Z squared.</summary>
    protected readonly double dz2;
    /// <summary>The primary ray provided by the camera.</summary>
    protected readonly Ray ray = new();

    /// <summary>Initializes the basic camera parameters.</summary>
    /// <param name="location">Camera's location.</param>
    /// <param name="target">Point the camera is looking at.</param>
    /// <param name="up">A pointer to the sky.</param>
    /// <param name="width">Desired image width, in pixels.</param>
    /// <param name="height">Desired image height, in pixels.</param>
    protected BasicCamera(in Vector location, in Vector target, Vector up,
        int width, int height)
    {
        Target = target;
        Vector diff = target - location;
        if ((up ^ diff).Length < Tolerance.Epsilon)
            up = (Vector.ZRay ^ diff).Length < Tolerance.Epsilon ?
                Vector.YRay : Vector.ZRay;
        loc = location;
        this.up = up;
        this.width = width;
        this.height = height;
        // Distance to target.
        delta = target - location;
        distance = delta.Length;
        dz2 = delta.Z * delta.Z;
    }

    /// <summary>Gets the primary ray used by the camera.</summary>
    public Ray PrimaryRay => ray;

    /// <summary>Gets the common origin for all camera rays.</summary>
    public Vector Location => loc;
    /// <summary>Gets the point the camera looks at.</summary>
    public Vector Target { get; }
    /// <summary>Gets the sky reference, for tilting the camera.</summary>
    public Vector Up => up;

    /// <summary>Gets the sampling height in pixels.</summary>
    public int Height => height;
    /// <summary>Gets the sampling width in pixels.</summary>
    public int Width => width;

    /// <summary>Computes the rotated location around the sky direction.</summary>
    /// <param name="rotationAngle">Rotation angle, in degrees.</param>
    /// <returns>The rotated location.</returns>
    protected Vector RotateLocation(double rotationAngle, bool keepCameraHeight)
    {
        Matrix transf = new(Location, Target, Up);
        Vector loc = transf.Transpose() * (Location - Target);
        Vector result = transf * Matrix.Rotation(0, rotationAngle, 0) * loc + Target;
        if (keepCameraHeight)
            result += (Location - result) * Up * Up;
        return result;
    }

    protected static IShape UncheckUnion(IUnion union)
    {
        if (union.IsCheckModifiable)
            union.IsChecking = false;
        else if (union is SUnion2 su2)
            return su2.ToUncheckedUnion();
        return union as IShape;
    }

    /// <summary>Selects the best camera for a given scene.</summary>
    /// <param name="sampler">Sampler to be used.</param>
    /// <returns>The own camera, by default.</returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
    public ICamera Simplify(ISampler sampler) => (ICamera)this;
}

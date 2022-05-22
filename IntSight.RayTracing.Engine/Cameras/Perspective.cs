using static System.Math;

namespace IntSight.RayTracing.Engine;

/// <summary>Common base class for all perspective cameras.</summary>
public abstract class BasePerspectiveCamera : BasicCamera
{
    protected readonly double angle;
    protected readonly double a, bRow, bCol;
    protected double focusRow, focusCol;

    /// <summary>Initializes the basic camera parameters.</summary>
    /// <param name="location">Camera's location.</param>
    /// <param name="target">Point the camera is looking at.</param>
    /// <param name="up">A pointer to the sky.</param>
    /// <param name="width">Desired image width, in pixels.</param>
    /// <param name="height">Desired image height, in pixels.</param>
    protected BasePerspectiveCamera(
        in Vector location, in Vector target, in Vector up,
        double angle, int width, int height)
        : base(location, target, up, width, height)
    {
        this.angle = angle;
        // Radius at target.
        double pixSz = distance * Tan(angle * PI / 360) /
            (width >= height ? width : height);
        a = pixSz + pixSz;
        bRow = pixSz * (1 - height);
        bCol = pixSz * (1 - width);
    }

    /// <summary>Creates a copy of the camera with a rotated location.</summary>
    /// <param name="rotationAngle">Rotation angle, in degrees.</param>
    /// <returns>The new camera.</returns>
    public ICamera Rotate(double rotationAngle, bool keepCameraHeight) =>
        new PerspectiveCamera(
            RotateLocation(rotationAngle, keepCameraHeight),
            Target, Up, angle, width, height);

    /// <summary>Scales jitter according to the pixel size in each dimension.</summary>
    /// <param name="jitter">Array with jitter data.</param>
    /// <returns>Echoes the jitter array.</returns>
    public double[] InitJitter(double[] jitter)
    {
        for (int i = 0; i < jitter.Length; i++)
            jitter[i] *= a;
        return jitter;
    }

    /// <summary>Projects the bounds of a root shape into the view plane.</summary>
    /// <param name="root">The root shape of a scene.</param>
    /// <returns>Percentage of the view area covered by objects in the scene.</returns>
    private double GetCoverage(IShape root)
    {
        Bounds b = root.Bounds;
        // If our camera is located inside the root's bounds, coverage is total.
        if (b.Contains(Location))
            return 1.0;
        ((ICamera)this).GetRayCoordinates(
            ray.FromTo(Location, b.From), out int r0, out int c0);
        ((ICamera)this).GetRayCoordinates(
            ray.FromTo(Location, b.To), out int r1, out int c1);
        return (double)((r1 - r0) * (c1 - c0)) / (width * height);
    }

    /// <summary>Removes bounds from the root shape, when no needed.</summary>
    /// <param name="root">The root shape in the scene.</param>
    /// <returns>The new root shape.</returns>
    public IShape CheckRoot(IShape root) =>
        root is IUnion union && union.IsChecking && GetCoverage(root) >= 0.85
            ? UncheckUnion(union) : root;

    /// <summary>Sets the row for the camera ray.</summary>
    /// <param name="row">Zero based row number.</param>
    public void FocusRow(int row) => focusRow = a * row + bRow;

    /// <summary>Sets the column for the camera ray.</summary>
    /// <param name="row">Zero based column number.</param>
    public void FocusColumn(int column) => focusCol = a * column + bCol;
}

/// <summary>Represents cameras using the perspective projection.</summary>
[XSight(Alias = "Perspective")]
public sealed class PerspectiveCamera : BasePerspectiveCamera, ICamera
{
    private readonly Matrix transform;
    private readonly Matrix inverse;
    private readonly Vector dist;

    public PerspectiveCamera(
        [Proposed("[0,0,-10]")] Vector location,
        [Proposed("^0")] Vector target,
        [Proposed("^Y")] Vector up,
        [Proposed("60")] double angle,
        [Proposed("320")] int width,
        [Proposed("240")] int height)
        : base(location, target, up, angle, width, height)
    {
        // Camera rotation
        transform = new(location, target, up);
        inverse = transform.Transpose();
        dist = new Vector(transform.A13, transform.A23, transform.A33) * distance;
    }

    public PerspectiveCamera(
        [Proposed("[0,0,-10]")] Vector location,
        [Proposed("[0,0,0]")] Vector target,
        [Proposed("60")] double angle,
        [Proposed("200")] int width,
        [Proposed("200")] int height)
        : this(location, target, Vector.YRay, angle, width, height) { }

    public PerspectiveCamera(
        [Proposed("[0,0,-10]")] Vector location,
        [Proposed("[0,0,0]")] Vector target,
        [Proposed("200")] int width,
        [Proposed("200")] int height)
        : this(location, target, Vector.YRay, 60, width, height) { }

    #region ICamera members.

    /// <summary>Creates an independent copy of the camera.</summary>
    /// <returns>The new camera.</returns>
    ICamera ICamera.Clone() =>
        new PerspectiveCamera(Location, Target, Up, angle, width, height);

    /// <summary>Initializes the camera ray for a given row and column.</summary>
    /// <param name="row">Row for the camera ray.</param>
    /// <param name="column">Column for the camera ray.</param>
    void ICamera.Focus(int row, int column)
    {
        ray.Origin = Location;
        ray.Direction = transform.RotateNorm(a * column + bCol, a * row + bRow, distance);
    }

    /// <summary>Initializes the camera ray given a small target deviation.</summary>
    /// <param name="dY">Vertical target deviation.</param>
    /// <param name="dX">Horizontal target deviation.</param>
    void ICamera.GetRay(double dY, double dX)
    {
        dY += focusRow;
        dX += focusCol;
        double x = transform.A11 * dX + transform.A12 * dY + dist.X;
        double y = transform.A21 * dX + transform.A22 * dY + dist.Y;
        double z = transform.A31 * dX + transform.A32 * dY + dist.Z;
        double len = 1.0 / Sqrt(x * x + y * y + z * z);
        ray.Direction = new(x * len, y * len, z * len);
        ray.Origin = Location;
    }

    /// <summary>Initializes the camera ray given a target and an origin deviation.</summary>
    /// <param name="dY">Vertical target deviation.</param>
    /// <param name="dX">Horizontal target deviation.</param>
    /// <param name="odY">Vertical origin deviation.</param>
    /// <param name="odX">Horizontal origin deviation.</param>
    /// <remarks>Used by the focal sampler.</remarks>
    void ICamera.GetRay(double dY, double dX, double odY, double odX)
    {
        double vx = FusedMultiplyAdd(a, dX, focusCol);
        double vy = FusedMultiplyAdd(a, dY, focusRow);
        double a13 = delta.X - odX, a23 = delta.Y - odY, a33 = delta.Z;
        double d0 = 1.0 / Sqrt(a13 * a13 + a23 * a23 + dz2);
        a13 *= d0; a23 *= d0; a33 *= d0;
        double a11 = up.Y * a33 - up.Z * a23;
        double a21 = up.Z * a13 - up.X * a33;
        double a31 = up.X * a23 - up.Y * a13;
        d0 = 1.0 / Sqrt(a11 * a11 + a21 * a21 + a31 * a31);
        double x = ((a23 * a31 - a33 * a21) * vy + a11 * vx) * d0 + a13 * distance;
        double y = ((a33 * a11 - a13 * a31) * vy + a21 * vx) * d0 + a23 * distance;
        double z = ((a13 * a21 - a23 * a11) * vy + a31 * vx) * d0 + a33 * distance;
        double len = 1.0 / Sqrt(x * x + y * y + z * z);
        ray.Direction = new(x * len, y * len, z * len);
        ray.Origin = loc.Add2D(odX, odY);
    }

    /// <summary>Given a ray, find the pixel it intersects.</summary>
    /// <param name="ray">A camera ray.</param>
    /// <param name="row">Row where the pixel belongs.</param>
    /// <param name="column">Column where the pixel belongs.</param>
    void ICamera.GetRayCoordinates(Ray ray, out int row, out int column)
    {
        Vector v = inverse * ray.Direction;
        double factor = distance / v.Z;
        row = (int)((v.Y * factor - bRow) / a);
        column = (int)((v.X * factor - bCol) / a);
    }

    /// <summary>Selects the best camera for a given scene.</summary>
    /// <param name="sampler">Sampler to be used.</param>
    /// <returns>An optimized camera, according to the camera orientation.</returns>
    ICamera ICamera.Simplify(ISampler sampler) =>
        transform.IsIdentity && sampler is not FocalSampler
            ? new AlignedPerspectiveCamera(Location, Target, Up, angle, width, height)
            : (Up - Vector.YRay).Length < Tolerance.Epsilon
            ? new VerticalAlignedCamera(Location, Target, angle, width, height) :
            this;

    #endregion
}

/// <summary>Represents axis-aligned perspective cameras.</summary>
internal sealed class AlignedPerspectiveCamera : BasePerspectiveCamera, ICamera
{
    private readonly double d2;

    public AlignedPerspectiveCamera(
        Vector location, Vector target, Vector up, double angle, int width, int height)
        : base(location, target, up, angle, width, height) => d2 = distance * distance;

    #region ICamera members.

    /// <summary>Creates an independent copy of the camera.</summary>
    /// <returns>The new camera.</returns>
    ICamera ICamera.Clone() =>
        new AlignedPerspectiveCamera(Location, Target, Up, angle, width, height);

    /// <summary>Initializes the camera ray for a given row and column.</summary>
    /// <param name="row">Row for the camera ray.</param>
    /// <param name="column">Column for the camera ray.</param>
    void ICamera.Focus(int row, int column)
    {
        double x = a * column + bCol;
        double y = a * row + bRow;
        double len = 1.0 / Sqrt(x * x + y * y + d2);
        ray.Direction = new(x * len, y * len, distance * len);
        ray.Origin = Location;
    }

    /// <summary>Initializes the camera ray given a small target deviation.</summary>
    /// <param name="dY">Vertical target deviation.</param>
    /// <param name="dX">Horizontal target deviation.</param>
    void ICamera.GetRay(double dY, double dX)
    {
        dY += focusRow;
        dX += focusCol;
        double len = 1.0 / Sqrt(dX * dX + dY * dY + d2);
        ray.Direction = new(dX * len, dY * len, distance * len);
        ray.Origin = Location;
    }

    /// <summary>Initializes the camera ray given a target and an origin deviation.</summary>
    /// <param name="dY">Vertical target deviation.</param>
    /// <param name="dX">Horizontal target deviation.</param>
    /// <param name="odY">Vertical origin deviation.</param>
    /// <param name="odX">Horizontal origin deviation.</param>
    /// <remarks>Used by the focal sampler.</remarks>
    void ICamera.GetRay(double dY, double dX, double odY, double odX)
    {
        double vx = a * dX + focusCol, vy = a * dY + focusRow;
        double a13 = delta.X - odX, a23 = delta.Y - odY, a33 = delta.Z;
        double d0 = 1.0 / Sqrt(a13 * a13 + a23 * a23 + dz2);
        a13 *= d0; a23 *= d0; a33 *= d0;
        double a11 = up.Y * a33 - up.Z * a23;
        double a21 = up.Z * a13 - up.X * a33;
        double a31 = up.X * a23 - up.Y * a13;
        d0 = 1.0 / Sqrt(a11 * a11 + a21 * a21 + a31 * a31);
        double x = ((a23 * a31 - a33 * a21) * vy + a11 * vx) * d0 + a13 * distance;
        double y = ((a33 * a11 - a13 * a31) * vy + a21 * vx) * d0 + a23 * distance;
        double z = ((a13 * a21 - a23 * a11) * vy + a31 * vx) * d0 + a33 * distance;
        double len = 1.0 / Sqrt(x * x + y * y + z * z);
        ray.Direction = new(x * len, y * len, z * len);
        ray.Origin = loc.Add2D(odX, odY);
    }

    /// <summary>Given a ray, find the pixel it intersects.</summary>
    /// <param name="ray">A camera ray.</param>
    /// <param name="row">Row where the pixel belongs.</param>
    /// <param name="column">Column where the pixel belongs.</param>
    void ICamera.GetRayCoordinates(Ray ray, out int row, out int column)
    {
        double factor = distance * ray.InvDir.Z;
        row = (int)((ray.Direction.Y * factor - bRow) / a);
        column = (int)((ray.Direction.X * factor - bCol) / a);
    }

    #endregion
}

/// <summary>Represents axis-aligned perspective cameras.</summary>
public sealed class VerticalAlignedCamera : BasePerspectiveCamera, ICamera
{
    private readonly Matrix transform;
    private readonly Matrix inverse;
    private readonly Vector dist;

    public VerticalAlignedCamera(
        Vector location, Vector target, double angle, int width, int height)
        : base(location, target, Vector.YRay, angle, width, height)
    {
        // Camera rotation
        transform = new(location, target, Vector.YRay);
        inverse = transform.Transpose();
        dist = new Vector(transform.A13, transform.A23, transform.A33) * distance;
    }

    #region ICamera members.

    /// <summary>Creates an independent copy of the camera.</summary>
    /// <returns>The new camera.</returns>
    ICamera ICamera.Clone() =>
        new VerticalAlignedCamera(Location, Target, angle, width, height);

    /// <summary>Initializes the camera ray for a given row and column.</summary>
    /// <param name="row">Row for the camera ray.</param>
    /// <param name="column">Column for the camera ray.</param>
    void ICamera.Focus(int row, int column)
    {
        ray.Origin = Location;
        ray.Direction = transform.RotateNorm(a * column + bCol, a * row + bRow, distance);
    }

    /// <summary>Initializes the camera ray given a small target deviation.</summary>
    /// <param name="dY">Vertical target deviation.</param>
    /// <param name="dX">Horizontal target deviation.</param>
    void ICamera.GetRay(double dY, double dX)
    {
        dY += focusRow;
        dX += focusCol;
        double x = transform.A11 * dX + transform.A12 * dY + dist.X;
        // When the sky vector is the Y ray, transform.A21 is zero.
        double y = FusedMultiplyAdd(transform.A22, dY, dist.Y);
        double z = transform.A31 * dX + transform.A32 * dY + dist.Z;
        double len = 1.0 / Sqrt(x * x + y * y + z * z);
        ray.Direction = new(x * len, y * len, z * len);
        ray.Origin = Location;
    }

    /// <summary>Initializes the camera ray given a target and an origin deviation.</summary>
    /// <param name="dY">Vertical target deviation.</param>
    /// <param name="dX">Horizontal target deviation.</param>
    /// <param name="odY">Vertical origin deviation.</param>
    /// <param name="odX">Horizontal origin deviation.</param>
    /// <remarks>Used by the focal sampler.</remarks>
    void ICamera.GetRay(double dY, double dX, double odY, double odX)
    {
        double a13 = delta.X - odX, a23 = delta.Y - odY, a33 = delta.Z;
        double d0 = 1.0 / Sqrt(a13 * a13 + a23 * a23 + dz2);
        a13 *= d0; a23 *= d0; a33 *= d0;
        d0 = Sqrt(a13 * a13 + a33 * a33);
        double vx = FusedMultiplyAdd(a, dX, focusCol);
        double vy = FusedMultiplyAdd(a, dY, focusRow);
        double y = a23 * distance + vy * d0;
        vy *= a23;
        double x = (a33 * vx - a13 * vy) / d0 + a13 * distance;
        double z = a33 * distance - (a33 * vy + a13 * vx) / d0;
        d0 = 1.0 / Sqrt(x * x + y * y + z * z);
        ray.Direction = new(x * d0, y * d0, z * d0);
        ray.Origin = loc.Add2D(odX, odY);
    }

    /// <summary>Given a ray, find the pixel it intersects.</summary>
    /// <param name="ray">A camera ray.</param>
    /// <param name="row">Row where the pixel belongs.</param>
    /// <param name="column">Column where the pixel belongs.</param>
    void ICamera.GetRayCoordinates(Ray ray, out int row, out int column)
    {
        Vector v = inverse * ray.Direction;
        double factor = distance / v.Z;
        row = (int)((v.Y * factor - bRow) / a);
        column = (int)((v.X * factor - bCol) / a);
    }

    #endregion
}

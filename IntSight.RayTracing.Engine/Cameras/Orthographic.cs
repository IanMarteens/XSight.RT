namespace IntSight.RayTracing.Engine;

/// <summary>An ortographic camera, casting parallel rays.</summary>
[XSight(Alias = "Orthographic")]
public sealed class OrthographicCamera : BasicCamera, ICamera
{
    private readonly double scale;
    private readonly Vector front;
    private readonly Vector right;
    private double focusRow, focusCol;

    public OrthographicCamera(
        [Proposed("1.0")] double scale,
        [Proposed("[0,0,-10]")] Vector location,
        [Proposed("[0, 0, 1]")] Vector target,
        [Proposed("[0, 1, 0]")] Vector up,
        [Proposed("200")] int width,
        [Proposed("200")] int height)
        : base(location, target, up, width, height)
    {
        this.scale = scale;
        front = target.Difference(location);
        right = this.up ^ front;
        double len = right.Length;
        if (len > 0.0)
            right /= len;
        this.up = front ^ right;
    }

    public OrthographicCamera(
        [Proposed("[0,0,-10]")] Vector location,
        [Proposed("[0, 0, 1]")] Vector target,
        [Proposed("[0, 1, 0]")] Vector up,
        [Proposed("60")] double angle,
        [Proposed("200")] int width,
        [Proposed("200")] int height)
        : base(location, target, up, width, height)
    {
        front = target.Difference(location);
        scale = 2.0 * target.Distance(location) *
            Math.Tan(Math.PI * angle / 360.0) / Math.Max(width, height);
        right = this.up ^ front;
        double len = right.Length;
        if (len > 0.0)
            right /= len;
        this.up = front ^ right;
    }

    public OrthographicCamera(
        [Proposed("1.0")] double scale,
        [Proposed("200")] int width,
        [Proposed("200")] int height)
        : this(scale, new Vector(0, 0, -10), Vector.ZRay, Vector.YRay,
            width, height)
    { }

    public OrthographicCamera(
        [Proposed("200")] int width,
        [Proposed("200")] int height)
        : this(1.0, width, height) { }

    #region ICamera members.

    /// <summary>Creates an independent copy of the camera.</summary>
    /// <returns>The new camera.</returns>
    ICamera ICamera.Clone() =>
        new OrthographicCamera(scale, Location, Target, Up, width, height);

    /// <summary>Creates a copy of the camera with a rotated location.</summary>
    /// <param name="rotationAngle">Rotation angle, in degrees.</param>
    /// <returns>The new camera.</returns>
    ICamera ICamera.Rotate(double rotationAngle, bool keepCameraHeight) =>
        new OrthographicCamera(scale,
            RotateLocation(rotationAngle, keepCameraHeight),
            Target, Up, width, height);

    /// <summary>Initializes the camera ray for a given row and column.</summary>
    /// <param name="row">Row for the camera ray.</param>
    /// <param name="column">Column for the camera ray.</param>
    void ICamera.Focus(int row, int column)
    {
        double f = (column - width / 2.0) * scale;
        double g = (row - height / 2.0) * scale;
        ray.Origin = loc + f * right + g * up;
        ray.Direction = front;
    }

    /// <summary>Sets the row for the camera ray.</summary>
    /// <param name="row">Zero based row number.</param>
    void ICamera.FocusRow(int row) => focusRow = row;

    /// <summary>Sets the column for the camera ray.</summary>
    /// <param name="row">Zero based column number.</param>
    void ICamera.FocusColumn(int column) => focusCol = column;

    /// <summary>Scales jitter according to the pixel size in each dimension.</summary>
    /// <param name="jitter">Array with jitter data.</param>
    /// <returns>Echoes the jitter array.</returns>
    double[] ICamera.InitJitter(double[] jitter) => jitter;

    /// <summary>Initializes the camera ray given a small target deviation.</summary>
    /// <param name="dY">Vertical target deviation.</param>
    /// <param name="dX">Horizontal target deviation.</param>
    void ICamera.GetRay(double dY, double dX)
    {
        double f = (focusCol + dX - width / 2.0) * scale;
        double g = (focusRow + dY - height / 2.0) * scale;
        ray.Origin = loc + f * right + g * up;
        ray.Direction = front;
    }

    /// <summary>Initializes the camera ray given a target and an origin deviation.</summary>
    /// <param name="dY">Vertical target deviation.</param>
    /// <param name="dX">Horizontal target deviation.</param>
    /// <param name="odY">Vertical origin deviation.</param>
    /// <param name="odX">Horizontal origin deviation.</param>
    void ICamera.GetRay(double dY, double dX, double odY, double odX)
    {
        double f = (focusCol + dX - width / 2.0) * scale;
        double g = (focusRow + dY - height / 2.0) * scale;
        ray.Origin = loc + f * right + g * up;
        ray.Direction = front;
    }

    /// <summary>Given a ray, find the pixel it intersects.</summary>
    /// <param name="ray">A camera ray.</param>
    /// <param name="row">Row where the pixel belongs.</param>
    /// <param name="column">Column where the pixel belongs.</param>
    void ICamera.GetRayCoordinates(Ray ray, out int row, out int column) => row = column = 0;

    /// <summary>Removes bounds from the root shape, when no needed.</summary>
    /// <param name="root">The root shape in the scene.</param>
    /// <returns>The new root shape.</returns>
    /// <remarks>Root bounds are superfluous when the camera is inside.</remarks>
    IShape ICamera.CheckRoot(IShape root) =>
        root is IUnion union && union.IsChecking && root.Bounds.Contains(Location) ?
            UncheckUnion(union) : root;

    #endregion
}

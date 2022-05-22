using static System.Math;

namespace IntSight.RayTracing.Engine;

/// <summary>Represents a ray in the affine 3D space.</summary>
public sealed class Ray
{
    /// <summary>The direction of the ray.</summary>
    private Vector dir;

    /// <summary>Gets or sets the origin of the ray.</summary>
    public Vector Origin;
    /// <summary>Gets or sets the direction of the ray.</summary>
    public Vector Direction
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => dir;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => InvDir = (dir = value).Invert();
    }
    /// <summary>Gets the inverted direction of the ray.</summary>
    public Vector InvDir;
    /// <summary>Squared length of the <see cref="Direction"/> vector.</summary>
    public double SquaredDir;

    /// <summary>Resets the ray given two points.</summary>
    /// <param name="from">The origin of the ray.</param>
    /// <param name="to">Direction of the ray.</param>
    /// <returns>Echoes the ray.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ray FromTo(in Vector from, in Vector to)
    {
        Origin = from;
        dir = to - from;
        InvDir = dir.Invert();
        SquaredDir = Direction.Squared;
        return this;
    }

    /// <summary>Clones the direction fields from another ray.</summary>
    /// <param name="other">Ray to be cloned.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDirection(Ray other) =>
        (dir, InvDir) = (other.dir, other.InvDir);

    /// <summary>Evaluates coordinates at a given time.</summary>
    public Vector this[double time]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(
            FusedMultiplyAdd(time, dir.X, Origin.X),
            FusedMultiplyAdd(time, dir.Y, Origin.Y),
            FusedMultiplyAdd(time, dir.Z, Origin.Z));
    }

    /// <summary>Gets a textual representation of this ray.</summary>
    /// <returns>Ray's origin and direction formatted with the invariant culture.</returns>
    public override string ToString() =>
        string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "<{0,6:F3}, {1,6:F3}, {2,6:F3}><{3,6:F3}, {4,6:F3}, {5,6:F3}>",
            Origin.X, Origin.Y, Origin.Z,
            dir.X, dir.Y, dir.Z);
}

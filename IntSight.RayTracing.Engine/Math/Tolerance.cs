namespace IntSight.RayTracing.Engine;

/// <summary>Tolerance settings for the ray tracing algorithm.</summary>
public static class Tolerance
{
    /// <summary>Precision for hit and shadow tests.</summary>
    public const double Epsilon = 0.00001;

    /// <summary>Checks whether two real values are close enough to each other.</summary>
    /// <param name="x">First value to test.</param>
    /// <param name="x0">Second value to test.</param>
    /// <returns>True if the absolute distance is lesser than <see cref="Epsilon"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Near(double x, double x0) => Math.Abs(x - x0) < Epsilon;

    /// <summary>Checks whether a value is close enough to zero.</summary>
    /// <param name="x">Value to test.</param>
    /// <returns>True if the absolute value is lesser than <see cref="Epsilon"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Zero(double x) => -Epsilon < x && x < Epsilon;

    /// <summary>Are the three parameters smaller than the precision constant?</summary>
    /// <param name="x">First value to test.</param>
    /// <param name="y">Second value to test.</param>
    /// <param name="z">Third value to test.</param>
    /// <returns>True, when the three components are small enough.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsZero(double x, double y, double z) =>
        Math.Abs(x) < Epsilon && Math.Abs(y) < Epsilon && Math.Abs(z) <= Epsilon;

    /// <summary>Checks whether a vector is close enough to zero.</summary>
    /// <param name="v">Vector to test.</param>
    /// <returns>True if the Manhattan distance is lesser than <see cref="Epsilon"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsZero(this in Vector v) =>
        Math.Abs(v.X) <= Epsilon && Math.Abs(v.Y) <= Epsilon && Math.Abs(v.Z) <= Epsilon;
}

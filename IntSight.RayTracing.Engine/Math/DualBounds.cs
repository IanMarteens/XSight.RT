#if USE_SSE

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#endif

namespace IntSight.RayTracing.Engine;

#if USE_SSE

/// <summary>Represents two finite rectangular bounding boxes.</summary>
public readonly struct DualBounds
{
    /// <summary>X coordinates of the two bounds.</summary>
    private readonly Vector256<double> xs;
    /// <summary>Y coordinates of the two bounds.</summary>
    private readonly Vector256<double> ys;
    /// <summary>Z coordinates of the two bounds.</summary>
    private readonly Vector256<double> zs;

    /// <summary>Create instance from two finite bounds.</summary>
    /// <param name="b1">First bounding box.</param>
    /// <param name="b2">Second bounding box.</param>
    public DualBounds(in Bounds b1, in Bounds b2)
    {
        xs = Vector256.Create(b1.From.X, b2.From.X, b1.To.X, b2.To.X);
        ys = Vector256.Create(b1.From.Y, b2.From.Y, b1.To.Y, b2.To.Y);
        zs = Vector256.Create(b1.From.Z, b2.From.Z, b1.To.Z, b2.To.Z);
    }

    /// <summary>Combined intersection test between a ray and two bounding boxes.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="maxt">Maximum time allowed.</param>
    /// <returns>Which bounding boxes were pierced by the ray.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Intersects(Ray ray, double maxt)
    {
        var min = Vector128<double>.Zero;
        var max = Vector128.Create(maxt);
        var org = Vector256.Create(ray.Origin.X);
        var dir = Vector256.Create(ray.InvDir.X);
        var ts = Avx.Multiply(Avx.Subtract(xs, org), dir);
        if (ray.Direction.X >= 0)
        {
            min = Sse2.Max(min, ts.GetLower());
            max = Sse2.Min(max, ts.GetUpper());
        }
        else
        {
            min = Sse2.Max(min, ts.GetUpper());
            max = Sse2.Min(max, ts.GetLower());
        }
        org = Vector256.Create(ray.Origin.Y);
        dir = Vector256.Create(ray.InvDir.Y);
        ts = Avx.Multiply(Avx.Subtract(ys, org), dir);
        if (ray.Direction.Y >= 0)
        {
            min = Sse2.Max(min, ts.GetLower());
            max = Sse2.Min(max, ts.GetUpper());
        }
        else
        {
            min = Sse2.Max(min, ts.GetUpper());
            max = Sse2.Min(max, ts.GetLower());
        }
        org = Vector256.Create(ray.Origin.Z);
        dir = Vector256.Create(ray.InvDir.Z);
        ts = Avx.Multiply(Avx.Subtract(zs, org), dir);
        if (ray.Direction.Z >= 0)
        {
            min = Sse2.Max(min, ts.GetLower());
            max = Sse2.Min(max, ts.GetUpper());
        }
        else
        {
            min = Sse2.Max(min, ts.GetUpper());
            max = Sse2.Min(max, ts.GetLower());
        }
        double tt0 = min.ToScalar(), tt1 = max.ToScalar();
        double uu0 = min.GetElement(1), uu1 = max.GetElement(1);
        return tt0 <= tt1 ? (uu0 <= uu1 ? (byte)3 : (byte)1) : (uu0 <= uu1 ? (byte)2 : (byte)0);
    }
}

/// <summary>Represents four finite rectangular bounding boxes.</summary>
public readonly struct FourBounds
{
    /// <summary>Low X coordinates.</summary>
    private readonly Vector256<double> loxs;
    /// <summary>Low Y coordinates.</summary>
    private readonly Vector256<double> loys;
    /// <summary>Low Z coordinates.</summary>
    private readonly Vector256<double> lozs;
    /// <summary>High X coordinates.</summary>
    private readonly Vector256<double> hixs;
    /// <summary>High Y coordinates.</summary>
    private readonly Vector256<double> hiys;
    /// <summary>High Z coordinates.</summary>
    private readonly Vector256<double> hizs;

    /// <summary>Create instance from four finite bounds.</summary>
    /// <param name="b1">First bounding box.</param>
    /// <param name="b2">Second bounding box.</param>
    /// <param name="b3">Third bounding box.</param>
    /// <param name="b4">Fourth bounding box.</param>
    public FourBounds(in Bounds b1, in Bounds b2, in Bounds b3, in Bounds b4)
    {
        loxs = Vector256.Create(b1.From.X, b2.From.X, b3.From.X, b4.From.X);
        loys = Vector256.Create(b1.From.Y, b2.From.Y, b3.From.Y, b4.From.Y);
        lozs = Vector256.Create(b1.From.Z, b2.From.Z, b3.From.Z, b4.From.Z);
        hixs = Vector256.Create(b1.To.X, b2.To.X, b3.To.X, b4.To.X);
        hiys = Vector256.Create(b1.To.Y, b2.To.Y, b3.To.Y, b4.To.Y);
        hizs = Vector256.Create(b1.To.Z, b2.To.Z, b3.To.Z, b4.To.Z);
    }

    /// <summary>Combined intersection test between a ray and four bounding boxes.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="maxt">Maximum time allowed.</param>
    /// <returns>Which bounding boxes were pierced by the ray.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Intersects(Ray ray, double maxt)
    {
        var min = Vector256<double>.Zero;
        var max = Vector256.Create(maxt);
        var org = Vector256.Create(ray.Origin.X);
        var dir = Vector256.Create(ray.InvDir.X);
        var t0 = Avx.Multiply(Avx.Subtract(loxs, org), dir);
        var t1 = Avx.Multiply(Avx.Subtract(hixs, org), dir);
        if (ray.Direction.X >= 0)
        {
            min = Avx.Max(min, t0);
            max = Avx.Min(max, t1);
        }
        else
        {
            min = Avx.Max(min, t1);
            max = Avx.Min(max, t0);
        }
        org = Vector256.Create(ray.Origin.Y);
        dir = Vector256.Create(ray.InvDir.Y);
        t0 = Avx.Multiply(Avx.Subtract(loys, org), dir);
        t1 = Avx.Multiply(Avx.Subtract(hiys, org), dir);
        if (ray.Direction.Y >= 0)
        {
            min = Avx.Max(min, t0);
            max = Avx.Min(max, t1);
        }
        else
        {
            min = Avx.Max(min, t1);
            max = Avx.Min(max, t0);
        }
        org = Vector256.Create(ray.Origin.Z);
        dir = Vector256.Create(ray.InvDir.Z);
        t0 = Avx.Multiply(Avx.Subtract(lozs, org), dir);
        t1 = Avx.Multiply(Avx.Subtract(hizs, org), dir);
        if (ray.Direction.Z >= 0)
        {
            min = Avx.Max(min, t0);
            max = Avx.Min(max, t1);
        }
        else
        {
            min = Avx.Max(min, t1);
            max = Avx.Min(max, t0);
        }
        byte result = 0;
        if (min.ToScalar() <= max.ToScalar())
            result = 0b0001;
        if (min.GetElement(1) <= max.GetElement(1))
            result |= 0b0010;
        if (min.GetElement(2) <= max.GetElement(2))
            result |= 0b0100;
        if (min.GetElement(3) <= max.GetElement(3))
            result |= 0b1000;
        return result;
    }
}

#else

/// <summary>Represents two finite rectangular bounding boxes.</summary>
public readonly struct DualBounds
{
    /// <summary>Origin of first bounding box.</summary>
    private readonly Vector lo1;
    /// <summary>End of first bounding box.</summary>
    private readonly Vector hi1;
    /// <summary>Origin of second bounding box.</summary>
    private readonly Vector lo2;
    /// <summary>End of second bounding box.</summary>
    private readonly Vector hi2;

    /// <summary>Create instance from two finite bounds.</summary>
    /// <param name="b1">First bounding box.</param>
    /// <param name="b2">Second bounding box.</param>
    public DualBounds(in Bounds b1, in Bounds b2) =>
        (lo1, hi1, lo2, hi2) = (b1.From, b1.To, b2.From, b2.To);

    /// <summary>Combined intersection test between a ray and two bounding boxes.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="maxt">Maximum time allowed.</param>
    /// <returns>Which bounding boxes were pierced by the ray.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Intersects(Ray ray, double maxt)
    {
        double tt0 = 0;
        double uu0 = 0, uu1 = maxt;
        double org = ray.Origin.X, d = ray.InvDir.X;
        double t0 = (lo1.X - org) * d, t1 = (hi1.X - org) * d;
        double u0 = (lo2.X - org) * d, u1 = (hi2.X - org) * d;
        if (d >= 0)
        {
            if (t0 > 0) tt0 = t0;
            if (t1 < maxt) maxt = t1;
            if (u0 > 0) uu0 = u0;
            if (u1 < uu1) uu1 = u1;
        }
        else
        {
            if (t1 > 0) tt0 = t1;
            if (t0 < maxt) maxt = t0;
            if (u1 > 0) uu0 = u1;
            if (u0 < uu1) uu1 = u0;
        }
        org = ray.Origin.Y; d = ray.InvDir.Y;
        t0 = (lo1.Y - org) * d; t1 = (hi1.Y - org) * d;
        u0 = (lo2.Y - org) * d; u1 = (hi2.Y - org) * d;
        if (d >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < maxt) maxt = t1;
            if (u0 > uu0) uu0 = u0;
            if (u1 < uu1) uu1 = u1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < maxt) maxt = t0;
            if (u1 > uu0) uu0 = u1;
            if (u0 < uu1) uu1 = u0;
        }
        org = ray.Origin.Z; d = ray.InvDir.Z;
        t0 = (lo1.Z - org) * d; t1 = (hi1.Z - org) * d;
        u0 = (lo2.Z - org) * d; u1 = (hi2.Z - org) * d;
        if (d >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < maxt) maxt = t1;
            if (u0 > uu0) uu0 = u0;
            if (u1 < uu1) uu1 = u1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < maxt) maxt = t0;
            if (u1 > uu0) uu0 = u1;
            if (u0 < uu1) uu1 = u0;
        }
        return tt0 <= maxt ? (uu0 <= uu1 ? 3 : 1) : (uu0 <= uu1 ? 2 : 0);
    }
}

#endif

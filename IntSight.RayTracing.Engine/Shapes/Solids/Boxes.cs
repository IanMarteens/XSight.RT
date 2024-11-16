using System.Diagnostics.CodeAnalysis;
using static System.Math;

namespace IntSight.RayTracing.Engine;

/// <summary>Common base class for all boxes.</summary>
public abstract class BoxBase : MaterialShape
{
    protected double x0, y0, z0, x1, y1, z1;
    protected double one, minusOne;
    protected bool negated;

    /// <summary>Initializes the box ancestor.</summary>
    /// <param name="from">First corner.</param>
    /// <param name="to">Second corner.</param>
    /// <param name="material">Material the box is made of.</param>
    protected BoxBase(Vector from, Vector to, IMaterial material)
        : base(material)
    {
        bounds = new(from, to);
        (x0, y0, z0) = bounds.From;
        (x1, y1, z1) = bounds.To;
        one = 1.0;
        minusOne = -1.0;
    }

    /// <summary>Gets the point at the leftmost lower closer corner.</summary>
    public Vector From => new(x0, y0, z0);

    /// <summary>Gets the point at the rightmost upper farthest corner.</summary>
    public Vector To => new(x1, y1, z1);

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public int MaxHits => 2;

    /// <summary>Estimated complexity.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public ShapeCost Cost => ShapeCost.EasyPeasy;

    /// <summary>Invert normals for the right operand in a difference.</summary>
    public void Negate()
    {
        negated = !negated;
        one = -one;
        minusOne = -minusOne;
    }
}

/// <summary>
/// This is the most general box: it can be both scaled and rotated.
/// </summary>
[XSight]
[Properties(nameof(squaredRadius), nameof(negated), nameof(From), nameof(To), nameof(material))]
public sealed class Box : BoxBase, IShape
{
    private Matrix transform, inverse;
    private Vector centroid;
    private double squaredRadius;

    private Box(Vector from, Vector to,
        Matrix transform, Matrix inverse, IMaterial material)
        : base(from, to, material)
    {
        centroid = bounds.Center;
        squaredRadius = bounds.SquaredRadius;
        this.transform = transform;
        this.inverse = inverse;
    }

    public Box(
        [Proposed("[-1,-1,-1]")] Vector from,
        [Proposed("[+1,+1,+1]")] Vector to,
        IMaterial material)
        : this(from, to, Matrix.Identity, Matrix.Identity, material) { }

    public Box(
        [Proposed("-1")] double x0, [Proposed("-1")] double y0,
        [Proposed("-1")] double z0, [Proposed("+1")] double x1,
        [Proposed("+1")] double y1, [Proposed("+1")] double z1,
        IMaterial material)
        : this(
            new(x0, y0, z0), new(x1, y1, z1),
            Matrix.Identity, Matrix.Identity, material)
    { }

    #region IShape members.

    /// <summary>The center of the bounding sphere.</summary>
    public override Vector Centroid => centroid;

    /// <summary>The square radius of the bounding sphere.</summary>
    public override double SquaredRadius => squaredRadius;

    /// <summary>Checks whether a ray pierces this box.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        Vector org = inverse * ray.Origin;
        Vector invdir = inverse.RotateInvert(ray.Direction);
        double tt0 = Tolerance.Epsilon, tt1 = 1.0;
        double t0 = (x0 - org.X) * invdir.X, t1 = (x1 - org.X) * invdir.X;
        if (invdir.X >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < 1.0) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < 1.0) tt1 = t0;
        }
        if (tt0 > tt1)
            return false;
        t0 = (y0 - org.Y) * invdir.Y;
        t1 = (y1 - org.Y) * invdir.Y;
        if (invdir.Y >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        t0 = (z0 - org.Z) * invdir.Z;
        t1 = (z1 - org.Z) * invdir.Z;
        if (invdir.Z >= 0)
            return t0 > tt0 ?
                (t1 < tt1 ? t0 <= t1 : t0 <= tt1) :
                (t1 < tt1 ? tt0 <= t1 : tt0 <= tt1);
        else
            return t1 > tt0 ?
                (t0 < tt1 ? t1 <= t0 : t1 <= tt1) :
                (t0 < tt1 ? tt0 <= t0 : tt0 <= tt1);
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        Vector org = inverse * ray.Origin;
        Vector dir = inverse.RotateNorm(ray.Direction, out double len);
        double temp = 1.0 / dir.X;
        double tt0 = (x0 - org.X) * temp;
        double tt1 = (x1 - org.X) * temp;
        if (temp < 0)
        {
            temp = tt0; tt0 = tt1; tt1 = temp;
        }
        double t0 = (y0 - org.Y) * (temp = 1.0 / dir.Y);
        double t1 = (y1 - org.Y) * temp;
        if (temp >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        t0 = (z0 - org.Z) * (temp = 1.0 / dir.Z);
        t1 = (z1 - org.Z) * temp;
        if (temp >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        if (tt0 > tt1)
            return false;
        if (tt0 < Tolerance.Epsilon)
            if ((tt0 = tt1) < Tolerance.Epsilon)
                return false;
        // Time scale is readjusted here.
        if ((tt1 = tt0 * len) > maxt)
            return false;

        // Now, tt1 is the global time, and tt0 is the local time.
        info.Time = tt1;
        info.HitPoint = new(
            FusedMultiplyAdd(tt0, dir.X, org.X),
            FusedMultiplyAdd(tt0, dir.Y, org.Y),
            FusedMultiplyAdd(tt0, dir.Z, org.Z));
        if (Abs(info.HitPoint.X - x0) < Tolerance.Epsilon)
            info.Normal = transform.RotateNorm(-1.0, 0.0, 0.0);
        else if (Abs(info.HitPoint.X - x1) < Tolerance.Epsilon)
            info.Normal = transform.RotateNorm(+1.0, 0.0, 0.0);
        else if (Abs(info.HitPoint.Y - y0) < Tolerance.Epsilon)
            info.Normal = transform.RotateNorm(0.0, -1.0, 0.0);
        else if (Abs(info.HitPoint.Y - y1) < Tolerance.Epsilon)
            info.Normal = transform.RotateNorm(0.0, +1.0, 0.0);
        else if (Abs(info.HitPoint.Z - z0) < Tolerance.Epsilon)
            info.Normal = transform.RotateNorm(0.0, 0.0, -1.0);
        else
            info.Normal = transform.RotateNorm(0.0, 0.0, +1.0);
        info.Material = material;
        return true;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        Vector lct = inverse * location;
        if (Abs(lct.X - x0) < Tolerance.Epsilon)
            return transform.RotateNorm(minusOne, 0.0, 0.0);
        else if (Abs(lct.X - x1) < Tolerance.Epsilon)
            return transform.RotateNorm(one, 0.0, 0.0);
        else if (Abs(lct.Y - y0) < Tolerance.Epsilon)
            return transform.RotateNorm(0.0, minusOne, 0.0);
        else if (Abs(lct.Y - y1) < Tolerance.Epsilon)
            return transform.RotateNorm(0.0, one, 0.0);
        else if (Abs(lct.Z - z0) < Tolerance.Epsilon)
            return transform.RotateNorm(0.0, 0.0, minusOne);
        else
            return transform.RotateNorm(0.0, 0.0, one);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        Vector org = inverse * ray.Origin;
        double len = inverse.RotateInvertNorm(ray.Direction, out Vector invdir);
        double tt0, tt1;
        if (invdir.X >= 0)
        {
            tt0 = (x0 - org.X) * invdir.X;
            tt1 = (x1 - org.X) * invdir.X;
        }
        else
        {
            tt0 = (x1 - org.X) * invdir.X;
            tt1 = (x0 - org.X) * invdir.X;
        }
        double t0 = (y0 - org.Y) * invdir.Y;
        double t1 = (y1 - org.Y) * invdir.Y;
        if (invdir.Y >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        if (tt0 > tt1)
            return 0;
        t0 = (z0 - org.Z) * invdir.Z;
        t1 = (z1 - org.Z) * invdir.Z;
        if (invdir.Z >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        if (tt0 <= tt1)
        {
            hits[1].Time = tt1 / len;
            hits[0].Time = tt0 / len;
            hits[0].Shape = hits[1].Shape = this;
            return 2;
        }
        else
            return 0;
    }

    #endregion

    #region ITransformable members.

    /// <summary>Second optimization pass, where special shapes are introduced.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Substitute()
    {
        IShape b;
        if (Tolerance.Zero(transform.A12) && Tolerance.Zero(transform.A13) &&
            Tolerance.Zero(transform.A21) && Tolerance.Zero(transform.A23) &&
            Tolerance.Zero(transform.A31) && Tolerance.Zero(transform.A32))
        {
            Vector factor = new(
                1.0 / transform.A11, 1.0 / transform.A22, 1.0 / transform.A33);
            b = new AlignedBox(From.Scale(factor), To.Scale(factor), material);
        }
        else if (!Tolerance.Near(transform.Determinant, 1.0))
            return this;
        else if (Tolerance.Near(new Bounds(From, To).Volume, bounds.Volume))
            b = new AlignedBox(bounds.From, bounds.To, material);
        else
            b = new OrthoBox(From, To, transform, material);
        if (negated)
            b.Negate();
        return b;
    }

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        IMaterial m = material.Clone(force);
        if (force || m != material)
        {
            Box b = new(From, To, transform, inverse, m)
            {
                bounds = bounds,
                centroid = centroid,
                squaredRadius = squaredRadius
            };
            if (negated)
                b.Negate();
            return b;
        }
        else
            return this;
    }

    /// <summary>Is this either a diagonal or an orthogonal matrix?</summary>
    /// <param name="m">Matrix to check.</param>
    /// <returns>True if succeeds.</returns>
    private bool IsCanonical(in Matrix m)
    {
        if (Tolerance.Zero(m.A12) && Tolerance.Zero(m.A13) && Tolerance.Zero(m.A21) &&
            Tolerance.Zero(m.A23) && Tolerance.Zero(m.A31) && Tolerance.Zero(m.A32))
            return true;
        Bounds b = new(From, To);
        return Tolerance.Near(b.Volume, b.Rotate(m).Volume);
    }

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation) =>
        (IsCanonical(transform) && !IsCanonical(rotation * transform)) ?
            TransformationCost.Depends : TransformationCost.Ok;

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor) => TransformationCost.Ok;

    /// <summary>Translates this box.</summary>
    /// <param name="translation">Translation amount.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        Vector v = inverse * translation;
        x0 += v.X; x1 += v.X;
        y0 += v.Y; y1 += v.Y;
        z0 += v.Z; z1 += v.Z;
        bounds += translation;
        centroid += translation;
        material = material.Translate(translation);
    }

    /// <summary>Rotates this box.</summary>
    /// <param name="rotation">Rotation matrix.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        transform = rotation * transform;
        inverse *= rotation.Transpose();
        bounds = bounds.Rotate(rotation);
        centroid = rotation * centroid;
    }

    /// <summary>Scales this box.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        if (transform.IsIdentity ||
            factor.X == factor.Y && factor.Y == factor.Z &&
            Tolerance.Near(transform.Determinant, 1.0))
        {
            x0 *= factor.X; y0 *= factor.Y; z0 *= factor.Z;
            x1 *= factor.X; y1 *= factor.Y; z1 *= factor.Z;
        }
        else
        {
            Matrix m = new(1.0 / factor.X, 1.0 / factor.Y, 1.0 / factor.Z);
            transform = m * transform;
            inverse *= m;
        }
        bounds = bounds.Scale(factor);
        centroid = centroid.Scale(factor);
        double f = Max(Max(factor.X, factor.Y), factor.Z);
        squaredRadius = squaredRadius * f * f;
        f = bounds.SquaredRadius;
        if (squaredRadius > f)
        {
            squaredRadius = f;
            centroid = bounds.Center;
        }
    }

    #endregion
}

/// <summary>A box with an orthogonal transformation.</summary>
[Properties(nameof(negated), nameof(From), nameof(To), nameof(material))]
internal sealed class OrthoBox : BoxBase, IShape
{
    private readonly Matrix transform;
    private readonly Matrix inverse;

    /// <summary>Squared radius of the bounding sphere.</summary>
    private readonly double squaredRadius;

    /// <summary>Creates a rotated box.</summary>
    /// <param name="from">First corner.</param>
    /// <param name="to">Second corner.</param>
    /// <param name="transform">An orthogonal rotation matrix.</param>
    /// <param name="material">Material the box is made of.</param>
    public OrthoBox(Vector from, Vector to, Matrix transform, IMaterial material)
        : base(from, to, material)
    {
        this.transform = transform;
        inverse = transform.Transpose();
        squaredRadius = bounds.SquaredRadius;
        bounds = bounds.Rotate(transform);
    }

    #region IShape members.

    /// <summary>Gets the squared radius of the bounding sphere.</summary>
    public override double SquaredRadius => squaredRadius;

    /// <summary>Intersection test with a light or shadow ray.</summary>
    /// <param name="ray">Ray to check.</param>
    /// <returns>True if succeeds.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        Vector org = inverse * ray.Origin;
        Vector invdir = inverse.RotateInvert(ray.Direction);
        double tt0 = Tolerance.Epsilon, tt1 = 1.0;
        double t0 = (x0 - org.X) * invdir.X, t1 = (x1 - org.X) * invdir.X;
        if (invdir.X >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < 1.0) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < 1.0) tt1 = t0;
        }
        if (tt0 > tt1)
            return false;
        t0 = (y0 - org.Y) * invdir.Y;
        t1 = (y1 - org.Y) * invdir.Y;
        if (invdir.Y >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        t0 = (z0 - org.Z) * invdir.Z;
        t1 = (z1 - org.Z) * invdir.Z;
        if (invdir.Z >= 0)
            return t0 > tt0 ?
                (t1 < tt1 ? t0 <= t1 : t0 <= tt1) :
                (t1 < tt1 ? tt0 <= t1 : tt0 <= tt1);
        else
            return t1 > tt0 ?
                (t0 < tt1 ? t1 <= t0 : t1 <= tt1) :
                (t0 < tt1 ? tt0 <= t0 : tt0 <= tt1);
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        Vector org = inverse * ray.Origin;
        Vector dir = inverse * ray.Direction;
        double temp = 1.0 / dir.X;
        double tt0 = (x0 - org.X) * temp;
        double tt1 = (x1 - org.X) * temp;
        if (temp < 0)
        {
            temp = tt0; tt0 = tt1; tt1 = temp;
        }
        double t0 = (y0 - org.Y) * (temp = 1.0 / dir.Y);
        double t1 = (y1 - org.Y) * temp;
        if (temp >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        t0 = (z0 - org.Z) * (temp = 1.0 / dir.Z);
        t1 = (z1 - org.Z) * temp;
        if (temp >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        if (tt0 > tt1)
            return false;
        if (tt0 < Tolerance.Epsilon)
            if ((tt0 = tt1) < Tolerance.Epsilon)
                return false;
        if (tt0 > maxt)
            return false;

        info.Time = tt0;
        info.HitPoint = new(
            FusedMultiplyAdd(tt0, dir.X, org.X),
            FusedMultiplyAdd(tt0, dir.Y, org.Y),
            FusedMultiplyAdd(tt0, dir.Z, org.Z));
        if (Abs(info.HitPoint.X - x0) < Tolerance.Epsilon)
            info.Normal = new(-transform.A11, -transform.A21, -transform.A31);
        else if (Abs(info.HitPoint.X - x1) < Tolerance.Epsilon)
            info.Normal = new(transform.A11, transform.A21, transform.A31);
        else if (Abs(info.HitPoint.Y - y0) < Tolerance.Epsilon)
            info.Normal = new(-transform.A12, -transform.A22, -transform.A32);
        else if (Abs(info.HitPoint.Y - y1) < Tolerance.Epsilon)
            info.Normal = new(transform.A12, transform.A22, transform.A32);
        else if (Abs(info.HitPoint.Z - z0) < Tolerance.Epsilon)
            info.Normal = new(-transform.A13, -transform.A23, -transform.A33);
        else
            info.Normal = new(transform.A13, transform.A23, transform.A33);
        info.Material = material;
        return true;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        double x = 0, y = 0, z = 0;
        Vector lct = inverse * location;
        if (Abs(lct.X - x0) < Tolerance.Epsilon) x = minusOne;
        else if (Abs(lct.X - x1) < Tolerance.Epsilon) x = one;
        else if (Abs(lct.Y - y0) < Tolerance.Epsilon) y = minusOne;
        else if (Abs(lct.Y - y1) < Tolerance.Epsilon) y = one;
        else if (Abs(lct.Z - z0) < Tolerance.Epsilon) z = minusOne;
        else z = one;
        return transform.Rotate(x, y, z);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        Vector org = inverse * ray.Origin;
        Vector invdir = inverse.RotateInvert(ray.Direction);
        double tt0, tt1;
        if (invdir.X >= 0)
        {
            tt0 = (x0 - org.X) * invdir.X;
            tt1 = (x1 - org.X) * invdir.X;
        }
        else
        {
            tt0 = (x1 - org.X) * invdir.X;
            tt1 = (x0 - org.X) * invdir.X;
        }
        double t0 = (y0 - org.Y) * invdir.Y;
        double t1 = (y1 - org.Y) * invdir.Y;
        if (invdir.Y >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        if (tt0 > tt1)
            return 0;
        t0 = (z0 - org.Z) * invdir.Z;
        t1 = (z1 - org.Z) * invdir.Z;
        if (invdir.Z >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        if (tt0 <= tt1)
        {
            hits[1].Time = tt1;
            hits[0].Time = tt0;
            hits[0].Shape = hits[1].Shape = this;
            return 2;
        }
        else
            return 0;
    }

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        IMaterial m = material.Clone(force);
        if (force || m != material)
        {
            OrthoBox b = new(From, To, transform, m);
            if (negated)
                b.Negate();
            return b;
        }
        else
            return this;
    }

    #endregion
}

/// <summary>
/// Axis aligned box: it can't be rotated, but can be arbitrarily scaled.
/// </summary>
/// <remarks>Creates an axis-aligned box.</remarks>
/// <param name="from">First corner in a diagonal.</param>
/// <param name="to">Second corner in a diagonal.</param>
/// <param name="material">Box's material.</param>
[Properties(nameof(negated), nameof(From), nameof(To), nameof(material))]
internal sealed class AlignedBox(Vector from, Vector to, IMaterial material)
    : BoxBase(from, to, material), IShape
{
    #region IShape members.

    /// <summary>Intersection test with a light or shadow ray.</summary>
    /// <param name="ray">Ray to check.</param>
    /// <returns>True if succeeds.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        double tt0 = Tolerance.Epsilon, tt1 = 1.0;
        double org = ray.Origin.X, temp = ray.InvDir.X;
        double t0 = (x0 - org) * temp, t1 = (x1 - org) * temp;
        if (temp >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < 1.0) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < 1.0) tt1 = t0;
        }
        if (tt0 > tt1)
            return false;
        t0 = (y0 - (org = ray.Origin.Y)) * (temp = ray.InvDir.Y);
        t1 = (y1 - org) * temp;
        if (temp >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        t0 = (z0 - (org = ray.Origin.Z)) * (temp = ray.InvDir.Z);
        t1 = (z1 - org) * temp;
        return temp >= 0
            ? t0 > tt0 ?
                (t1 < tt1 ? t0 <= t1 : t0 <= tt1) :
                (t1 < tt1 ? tt0 <= t1 : tt0 <= tt1)
            : t1 > tt0 ?
                (t0 < tt1 ? t1 <= t0 : t1 <= tt1) :
                (t0 < tt1 ? tt0 <= t0 : tt0 <= tt1);
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        double org = ray.Origin.X, temp;
        double tt0 = (x0 - org) * (temp = ray.InvDir.X);
        double tt1 = (x1 - org) * temp;
        if (temp < 0)
        {
            temp = tt0; tt0 = tt1; tt1 = temp;
        }
        org = ray.Origin.Y;
        double t0 = (y0 - org) * (temp = ray.InvDir.Y);
        double t1 = (y1 - org) * temp;
        if (temp >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        org = ray.Origin.Z;
        t0 = (z0 - org) * (temp = ray.InvDir.Z);
        t1 = (z1 - org) * temp;
        if (temp >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        if (tt0 > tt1)
            return false;
        if (tt0 < Tolerance.Epsilon)
            if ((tt0 = tt1) < Tolerance.Epsilon)
                return false;
        if (tt0 > maxt)
            return false;

        info.Time = tt0;
        info.HitPoint = ray[tt0];
        if (Abs(info.HitPoint.X - x0) < Tolerance.Epsilon)
            info.Normal = Vector.XRayM;
        else if (Abs(info.HitPoint.X - x1) < Tolerance.Epsilon)
            info.Normal = Vector.XRay;
        else if (Abs(info.HitPoint.Y - y0) < Tolerance.Epsilon)
            info.Normal = Vector.YRayM;
        else if (Abs(info.HitPoint.Y - y1) < Tolerance.Epsilon)
            info.Normal = Vector.YRay;
        else if (Abs(info.HitPoint.Z - z0) < Tolerance.Epsilon)
            info.Normal = Vector.ZRayM;
        else
            info.Normal = Vector.ZRay;
        info.Material = material;
        return true;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        double x = 0, y = 0, z = 0;
        if (Abs(location.X - x0) < Tolerance.Epsilon) x = minusOne;
        else if (Abs(location.X - x1) < Tolerance.Epsilon) x = one;
        else if (Abs(location.Y - y0) < Tolerance.Epsilon) y = minusOne;
        else if (Abs(location.Y - y1) < Tolerance.Epsilon) y = one;
        else if (Abs(location.Z - z0) < Tolerance.Epsilon) z = minusOne;
        else z = one;
        return new(x, y, z);
    }

    /// <summary>Intersects the box with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        double tt0, tt1, t0, t1, temp, org;
        org = ray.Origin.X;
        tt0 = (x0 - org) * (temp = ray.InvDir.X);
        tt1 = (x1 - org) * temp;
        if (temp < 0)
        {
            temp = tt0; tt0 = tt1; tt1 = temp;
        }
        org = ray.Origin.Y;
        t0 = (y0 - org) * (temp = ray.InvDir.Y);
        t1 = (y1 - org) * temp;
        if (temp >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        org = ray.Origin.Z;
        t0 = (z0 - org) * (temp = ray.InvDir.Z);
        t1 = (z1 - org) * temp;
        if (temp >= 0)
        {
            if (t0 > tt0) tt0 = t0;
            if (t1 < tt1) tt1 = t1;
        }
        else
        {
            if (t1 > tt0) tt0 = t1;
            if (t0 < tt1) tt1 = t0;
        }
        if (tt0 <= tt1)
        {
            hits[1].Time = tt1;
            hits[0].Time = tt0;
            hits[0].Shape = hits[1].Shape = this;
            return 2;
        }
        else
            return 0;
    }

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        IMaterial m = material.Clone(force);
        if (force || m != material)
        {
            AlignedBox b = new(From, To, m);
            if (negated)
                b.Negate();
            return b;
        }
        else
            return this;
    }

    #endregion
}
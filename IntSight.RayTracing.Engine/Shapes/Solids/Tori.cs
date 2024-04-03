using System.Diagnostics.CodeAnalysis;
using static System.Math;

namespace IntSight.RayTracing.Engine;

/// <summary>Common base for tori.</summary>
public abstract class TorusBase : MaterialShape, IBoundsChecker
{
    /// <summary>Expanded coordinates for the center.</summary>
    protected double cx, cy, cz;
    protected double r0, r1, r0sqr, r1sqr, r1inv, bigRsqr, r01sqr, r0r1inv;
    protected Solver.Roots roots;
    protected bool negated, checkBounds;

    /// <summary>Initializes a torus.</summary>
    /// <param name="center">Torus' center.</param>
    /// <param name="r0">Greater radius.</param>
    /// <param name="r1">Lesser radius.</param>
    /// <param name="material">Torus' material.</param>
    protected TorusBase(Vector center, double r0, double r1, IMaterial material)
        : base(material)
    {
        (cx, cy, cz) = center;
        (this.r0, this.r1) = (r0, r1);
        checkBounds = true;
    }

    /// <summary>Updates bounds and coefficients that depends on dimensions.</summary>
    protected virtual void RecomputeBounds()
    {
        r1inv = 1.0 / r1;
        r0sqr = r0 * r0;
        r1sqr = r1 * r1;
        bigRsqr = (r0 + r1) * (r0 + r1);
        r01sqr = r0sqr + r1sqr;
        r0r1inv = r0 * r1inv;
        r0sqr *= 4;
    }

    /// <summary>The center of the bounding sphere.</summary>
    public override Vector Centroid => new(cx, cy, cz);

    /// <summary>The square radius of the bounding sphere.</summary>
    public override double SquaredRadius => bigRsqr;


    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public int MaxHits => 4;

    /// <summary>Estimated complexity.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public ShapeCost Cost => ShapeCost.NoPainNoGain;

    /// <summary>
    /// Inverts normals for difference's parameters starting from the second one.
    /// </summary>
    public void Negate() => negated = !negated;

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public sealed override TransformationCost CanScale(in Vector factor) =>
        factor.X == factor.Y && factor.Y == factor.Z ?
            TransformationCost.Ok : TransformationCost.Nope;

    /// <summary>Translate this shape.</summary>
    /// <param name="translation">Translation amount.</param>
    public sealed override void ApplyTranslation(in Vector translation)
    {
        cx += translation.X; cy += translation.Y; cz += translation.Z;
        bounds += translation;
        material = material.Translate(translation);
    }

    /// <summary>Scale this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    public sealed override void ApplyScale(in Vector factor)
    {
        cx *= factor.X; cy *= factor.Y; cz *= factor.Z;
        r0 *= factor.X;
        r1 *= factor.X;
        RecomputeBounds();
    }

    /// <summary>Notifies the shape that its container is checking spheric bounds.</summary>
    /// <param name="centroid">Centroid of parent's spheric bounds.</param>
    /// <param name="squaredRadius">Square radius of parent's spheric bounds.</param>
    public override void NotifySphericBounds(in Vector centroid, double squaredRadius)
    {
        // If spheric bounds are the same as ours, we can skip bound checking.
        if (bigRsqr > 0.9 * squaredRadius)
            checkBounds = false;
    }

    bool IBoundsChecker.IsCheckModifiable => true;

    bool IBoundsChecker.IsChecking
    {
        get => checkBounds;
        set => checkBounds = value;
    }
}

/// <summary>Rotated tori.</summary>
[XSight, Properties(nameof(r0), nameof(r1), nameof(Centroid), nameof(material))]
public sealed class Torus : TorusBase, IShape
{
    private Matrix transform, inverse;

    private Torus(Vector center, double r0, double r1,
        Matrix transform, IMaterial material)
        : base(center, r0, r1, material)
    {
        this.transform = transform;
        RecomputeBounds();
    }

    public Torus(
        [Proposed("[0,0,0]")] Vector center,
        [Proposed("1.0")] double r0,
        [Proposed("0.5")] double r1,
        IMaterial material)
        : this(center, r0, r1, Matrix.Identity, material) { }

    public Torus(
        [Proposed("0")] double x, [Proposed("0")] double y, [Proposed("0")] double z,
        [Proposed("1.0")] double r0,
        [Proposed("0.5")] double r1,
        IMaterial material)
        : this(new(x, y, z), r0, r1, Matrix.Identity, material) { }

    /// <summary>Updates bounds and coefficients that depends on dimensions.</summary>
    protected override void RecomputeBounds()
    {
        base.RecomputeBounds();
        inverse = transform.Transpose();
        double sum = r0 + r1;
        bounds = Bounds.Create(-sum, -r1, -sum, sum, r1, sum).Transform(transform, Centroid);
    }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        // Apply the inverse transformation to the incoming ray
        Vector org = inverse.Rotate(ray.Origin.X - cx, ray.Origin.Y - cy, ray.Origin.Z - cz);
        Vector dir = inverse * ray.Direction;
        double y2 = org.Y * org.Y;
        double ray2 = ray.SquaredDir;
        double beta = org * dir / ray2;
        double k1 = org.X * org.X + org.Z * org.Z + y2;
        double discr = beta * beta - (k1 - bigRsqr) / ray2;
        if (discr < 0)
            return false;
        double t = -beta - (discr = Sqrt(discr));
        if (t < 0 && (t = discr - beta) < 0 || t > 1.0)
            return false;
        // -------------------------
        double dy = dir.Y / (ray2 = Sqrt(ray2));
        k1 -= r01sqr;
        beta *= ray2;
        t = beta + beta; t += t;
        Solver.Solve(
            t,
            t * beta + r0sqr * dy * dy + k1 + k1,
            t * k1 + (r0sqr + r0sqr) * org.Y * dy,
            r0sqr * (y2 - r1sqr) + k1 * k1,
            ref roots);
        if (roots.Count == 0)
            return false;
        if (roots.Count == 2)
            return Tolerance.Epsilon <= roots.R0 ?
                roots.R0 < ray2 :
                Tolerance.Epsilon <= roots.R1 && roots.R1 <= ray2;
        return roots.Count == 1 ?
            Tolerance.Epsilon <= roots.R0 && roots.R0 <= ray2 :
            Tolerance.Epsilon <= roots.R0 && roots.R0 <= ray2 ||
            Tolerance.Epsilon <= roots.R1 && roots.R1 <= ray2 ||
            Tolerance.Epsilon <= roots.R2 && roots.R2 <= ray2 ||
            roots.Count == 4 && Tolerance.Epsilon <= roots.R3 && roots.R3 <= ray2;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        // Apply the inverse transformation to the incoming ray
        Vector org = inverse.Rotate(ray.Origin.X - cx, ray.Origin.Y - cy, ray.Origin.Z - cz);
        Vector dir = inverse * ray.Direction;
        double PDy2 = org.Y * dir.Y, y2 = org.Y * org.Y;
        double beta = org.X * dir.X + PDy2 + org.Z * dir.Z, beta2 = beta * beta;
        double k1 = org.X * org.X + org.Z * org.Z + y2;
        double discr = beta2 - (k1 - bigRsqr);
        if (discr < 0)
            return false;
        double t = -beta - (discr = Sqrt(discr));
        if (t < Tolerance.Epsilon && (t = discr - beta) < Tolerance.Epsilon || t > maxt)
            return false;
        // -------------------------------------
        k1 -= r01sqr;
        beta += beta; beta += beta;
        beta2 += beta2 + k1;
        Solver.Solve(
            a1: beta,
            a2: (dir.Y * dir.Y) * r0sqr + beta2 + beta2,
            a3: (PDy2 + PDy2) * r0sqr + beta * k1,
            a4: (y2 - r1sqr) * r0sqr + k1 * k1,
            roots: ref roots);
        if (roots.Count == 0)
            return false;
        if (roots.Count == 2)
            if (roots.R0 >= Tolerance.Epsilon)
                if (roots.R0 <= maxt)
                    t = roots.R0;
                else
                    return false;
            else if (Tolerance.Epsilon <= roots.R1 && roots.R1 <= maxt)
                t = roots.R1;
            else
                return false;
        else if ((t = roots.HitTest(maxt)) < 0.0)
            return false;
        info.Time = t;
        double x = org.X + t * dir.X;
        double y = org.Y + t * dir.Y;
        double z = org.Z + t * dir.Z;
        info.HitPoint = new(x + cx, y + cy, z + cz);
        k1 = r1inv - r0r1inv / Sqrt(x * x + z * z);
        info.Normal = transform.Rotate(k1 * x, r1inv * y, k1 * z);
        info.Material = material;
        return true;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        Vector lct = inverse.Rotate(location.X - cx, location.Y - cy, location.Z - cz);
        double k = r1inv - r0r1inv / Sqrt(lct.X * lct.X + lct.Z * lct.Z);
        return negated
            ? transform.Rotate(-k * lct.X, -r1inv * lct.Y, -k * lct.Z)
            : transform.Rotate(k * lct.X, r1inv * lct.Y, k * lct.Z);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        // Apply the inverse transformation to the incoming ray
        Vector org = inverse.Rotate(ray.Origin.X - cx, ray.Origin.Y - cy, ray.Origin.Z - cz);
        Vector dir = inverse * ray.Direction;
        double len2 = 1.0 / (dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
        double len4 = len2 * len2;
        double y2 = org.Y * org.Y, PDy2 = org.Y * dir.Y;
        double k1 = (org.X * org.X + y2 + org.Z * org.Z - r01sqr) * len2;
        double k2 = (org.X * dir.X + PDy2 + org.Z * dir.Z) * len2;
        Solver.Solve(
            4 * k2,
            4 * k2 * k2 + r0sqr * dir.Y * dir.Y * len4 + k1 + k1,
            4 * k2 * k1 + 2 * r0sqr * PDy2 * len4,
            r0sqr * (y2 - r1sqr) * len4 + k1 * k1,
            ref roots);
        switch (roots.Count)
        {
            case 0:
                return 0;
            case 1:
                // A tangent ray.
                hits[1].Time = roots.R0;
                hits[0].Time = roots.R0;
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            case 2:
                // There's no need to sort roots: the solver already does it.
                hits[1].Time = roots.R1;
                hits[0].Time = roots.R0;
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            case 3:
                // The middle point is useless.
                if (roots.R0 <= roots.R1)
                    if (roots.R0 <= roots.R2)
                    {
                        hits[1].Time = roots.R2 > roots.R1 ? roots.R2 : roots.R1;
                        hits[0].Time = roots.R0;
                    }
                    else
                    {
                        hits[1].Time = roots.R1;
                        hits[0].Time = roots.R2;
                    }
                else if (roots.R1 <= roots.R2)
                {
                    hits[1].Time = roots.R2 > roots.R0 ? roots.R2 : roots.R0;
                    hits[0].Time = roots.R1;
                }
                else
                {
                    hits[1].Time = roots.R0;
                    hits[0].Time = roots.R2;
                }
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            default:
                roots.GetHits4Roots(hits);
                hits[0].Shape = hits[1].Shape =
                    hits[2].Shape = hits[3].Shape = this;
                return 4;
        }
    }

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        Torus t = new(Centroid, r0, r1, transform, material.Clone(force));
        if (negated)
            t.Negate();
        return t;
    }

    /// <summary>Second optimization pass, where special shapes are introduced.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Substitute()
    {
        IShape s;
        if (Tolerance.Zero(transform.A22) && Tolerance.Zero(transform.A32))
            s = new XTorus(Centroid, r0, r1, material);
        else if (transform.IsIdentity || transform.IsDiagonal(1.0, -1.0, 1.0))
            s = new YTorus(Centroid, r0, r1, material);
        else if (Tolerance.Zero(transform.A12) && Tolerance.Zero(transform.A22))
            s = new ZTorus(Centroid, r0, r1, material);
        else
            return this;
        if (negated)
            s.Negate();
        return s;
    }

    private static bool IsCanonical(in Matrix m) =>
        m.IsIdentity || m.IsDiagonal(1.0, -1.0, 1.0) ||
            Tolerance.Zero(m.A22) && Tolerance.Zero(m.A32) ||
            Tolerance.Zero(m.A12) && Tolerance.Zero(m.A22);

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation) =>
        IsCanonical(transform) && !IsCanonical(rotation * transform) ?
        TransformationCost.Depends : TransformationCost.Ok;

    /// <summary>Rotate this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        transform = rotation * transform;
        (cx, cy, cz) = rotation * Centroid;
        RecomputeBounds();
    }

    #endregion
}

[Properties(nameof(r0), nameof(r1), nameof(checkBounds), nameof(Centroid), nameof(material))]
internal sealed class XTorus : TorusBase, IShape
{
    public XTorus(Vector center, double r0, double r1, IMaterial material)
        : base(center, r0, r1, material) => RecomputeBounds();

    /// <summary>Updates bounds and coefficients that depends on dimensions.</summary>
    protected override void RecomputeBounds()
    {
        base.RecomputeBounds();
        double sum = r0 + r1;
        bounds = Bounds.Create(-r1, -sum, -sum, r1, sum, sum) + Centroid;
    }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        double x = ray.Origin.X - cx, dx = ray.Direction.X;
        double y = ray.Origin.Y - cy, dy = ray.Direction.Y;
        double z = ray.Origin.Z - cz, dz = ray.Direction.Z;
        double ray2 = ray.SquaredDir;
        double beta = (x * dx + y * dy + z * dz) / ray2;
        double x2 = x * x, k1 = y * y + z * z + x2;
        if (checkBounds)
        {
            double discr = (bigRsqr - k1) / ray2 + beta * beta;
            if (discr < 0)
                return false;
            z = -beta - (discr = Sqrt(discr));
            if (z <= 0 && (z = discr - beta) <= 0 || z > 1.0)
                return false;
        }
        // -------------------------
        ray2 = Sqrt(ray2);
        dx /= ray2;
        k1 -= r01sqr;
        beta *= ray2;
        z = beta + beta; z += z;
        Solver.Solve(
            z,
            z * beta + r0sqr * dx * dx + k1 + k1,
            r0sqr * (x + x) * dx + z * k1,
            (x2 - r1sqr) * r0sqr + k1 * k1,
            ref roots);
        if (roots.Count == 0)
            return false;
        if (roots.Count == 2)
            return Tolerance.Epsilon <= roots.R0 ?
                roots.R0 < ray2 :
                Tolerance.Epsilon <= roots.R1 && roots.R1 <= ray2;
        return roots.Count == 1 ?
            Tolerance.Epsilon <= roots.R0 && roots.R0 <= ray2 :
            Tolerance.Epsilon <= roots.R0 && roots.R0 <= ray2 ||
            Tolerance.Epsilon <= roots.R1 && roots.R1 <= ray2 ||
            Tolerance.Epsilon <= roots.R2 && roots.R2 <= ray2 ||
            roots.Count == 4 && Tolerance.Epsilon <= roots.R3 && roots.R3 <= ray2;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        double x = ray.Origin.X - cx;
        double y = ray.Origin.Y - cy;
        double z = ray.Origin.Z - cz;
        double Dx = ray.Direction.X, Dy = ray.Direction.Y, Dz = ray.Direction.Z;
        double PDx2 = x * Dx, x2 = x * x;
        double beta = PDx2 + y * Dy + z * Dz, beta2 = beta * beta;
        double k1 = y * y + z * z + x2;
        if (checkBounds)
        {
            double discr = beta2 - k1 + bigRsqr;
            if (discr < 0)
                return false;
            double t = -beta - (discr = Sqrt(discr));
            if (t < 0 && (t = discr - beta) < 0 || t > maxt)
                return false;
        }
        // -------------------------------------
        k1 -= r01sqr;
        beta += beta; beta += beta;
        beta2 += beta2 + k1;
        Solver.Solve(
            a1: beta,
            a2: (Dx * Dx) * r0sqr + beta2 + beta2,
            a3: (PDx2 + PDx2) * r0sqr + beta * k1,
            a4: (x2 - r1sqr) * r0sqr + k1 * k1,
            roots: ref roots);
        if (roots.Count == 0)
            return false;
        if (roots.Count == 2)
            if (roots.R0 >= Tolerance.Epsilon)
                if (roots.R0 <= maxt)
                    beta = roots.R0;
                else
                    return false;
            else if (Tolerance.Epsilon <= roots.R1 && roots.R1 <= maxt)
                beta = roots.R1;
            else
                return false;
        else if ((beta = roots.HitTest(maxt)) < 0.0)
            return false;
        info.Time = beta;
        info.HitPoint = new(
            (x += beta * Dx) + cx,
            (y += beta * Dy) + cy,
            (z += beta * Dz) + cz);
        k1 = r1inv - r0r1inv / Sqrt(y * y + z * z);
        info.Normal = new(r1inv * x, k1 * y, k1 * z);
        info.Material = material;
        return true;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        double y = location.Y - cy;
        double z = location.Z - cz;
        double k = r1inv - r0r1inv / Sqrt(y * y + z * z);
        return negated
            ? new Vector((cx - location.X) * r1inv, -k * y, -k * z)
            : new Vector((location.X - cx) * r1inv, k * y, k * z);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        double Dx = ray.Direction.X, Dy = ray.Direction.Y, Dz = ray.Direction.Z;
        double len2 = 1.0 / (Dx * Dx + Dy * Dy + Dz * Dz), len4 = len2 * len2;
        double x = ray.Origin.X - cx, x2 = x * x;
        double y = ray.Origin.Y - cy;
        double z = ray.Origin.Z - cz;
        double PDx2 = x * Dx;
        double k1 = (y * y + z * z + x2 - r01sqr) * len2;
        double k2 = (y * Dy + z * Dz + PDx2) * len2;
        Solver.Solve(
            k2 * 4,
            k1 + k1 + 4 * k2 * k2 + r0sqr * Dx * Dx * len4,
            4 * k2 * k1 + 2 * r0sqr * PDx2 * len4,
            k1 * k1 + r0sqr * (x2 - r1sqr) * len4,
            ref roots);
        switch (roots.Count)
        {
            case 0:
                return 0;
            case 1:
                hits[1].Time = roots.R0;
                hits[0].Time = roots.R0;
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            case 2:
                // There's no need to sort roots.
                hits[1].Time = roots.R1;
                hits[0].Time = roots.R0;
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            case 3:
                if (roots.R0 > roots.R1)
                    (roots.R1, roots.R0) = (roots.R0, roots.R1);
                if (roots.R0 > roots.R2)
                    (roots.R2, roots.R0) = (roots.R0, roots.R2);
                hits[1].Time = roots.R2 > roots.R1 ? roots.R2 : roots.R1;
                hits[0].Time = roots.R0;
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            default:
                roots.GetHits4Roots(hits);
                hits[0].Shape = hits[1].Shape =
                    hits[2].Shape = hits[3].Shape = this;
                return 4;
        }
    }

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        XTorus t = new(Centroid, r0, r1, material.Clone(force));
        if (negated)
            t.Negate();
        return t;
    }

    #endregion
}

[Properties(nameof(r0), nameof(r1), nameof(checkBounds), nameof(Centroid), nameof(material))]
internal sealed class YTorus : TorusBase, IShape
{
    public YTorus(Vector center, double r0, double r1, IMaterial material)
        : base(center, r0, r1, material) => RecomputeBounds();

    /// <summary>Updates bounds and coefficients that depends on dimensions.</summary>
    protected override void RecomputeBounds()
    {
        base.RecomputeBounds();
        double sum = r0 + r1;
        bounds = Bounds.Create(-sum, -r1, -sum, sum, r1, sum) + Centroid;
    }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        double x = ray.Origin.X - cx, dx = ray.Direction.X;
        double y = ray.Origin.Y - cy, dy = ray.Direction.Y;
        double z = ray.Origin.Z - cz, dz = ray.Direction.Z;
        double ray2 = ray.SquaredDir;
        double beta = (x * dx + y * dy + z * dz) / ray2;
        double y2 = y * y, k1 = x * x + z * z + y2;
        if (checkBounds)
        {
            double discr = (bigRsqr - k1) / ray2 + beta * beta;
            if (discr < 0)
                return false;
            x = -beta - (discr = Sqrt(discr));
            if (x < Tolerance.Epsilon && (x = discr - beta) < Tolerance.Epsilon || x > 1.0)
                return false;
        }
        // -------------------------
        ray2 = Sqrt(ray2);
        dy /= ray2;
        k1 -= r01sqr;
        beta *= ray2;
        x = beta + beta; x += x;
        Solver.Solve(
            x,
            x * beta + r0sqr * dy * dy + k1 + k1,
            r0sqr * (y + y) * dy + x * k1,
            (y2 - r1sqr) * r0sqr + k1 * k1,
            ref roots);
        if (roots.Count == 0)
            return false;
        if (roots.Count == 2)
            return Tolerance.Epsilon <= roots.R0 ?
                roots.R0 < ray2 :
                Tolerance.Epsilon <= roots.R1 && roots.R1 <= ray2;
        return roots.Count == 1 ?
            Tolerance.Epsilon <= roots.R0 && roots.R0 <= ray2 :
            Tolerance.Epsilon <= roots.R0 && roots.R0 <= ray2 ||
            Tolerance.Epsilon <= roots.R1 && roots.R1 <= ray2 ||
            Tolerance.Epsilon <= roots.R2 && roots.R2 <= ray2 ||
            roots.Count == 4 && Tolerance.Epsilon <= roots.R3 && roots.R3 <= ray2;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        double x = ray.Origin.X - cx;
        double y = ray.Origin.Y - cy;
        double z = ray.Origin.Z - cz;
        double Dx = ray.Direction.X, Dy = ray.Direction.Y, Dz = ray.Direction.Z;
        double y2 = y * y, k1 = x * x + z * z + y2;
        double PDy2 = y * Dy, beta = x * Dx + PDy2 + z * Dz, beta2 = beta * beta;
        if (checkBounds)
        {
            double discr = beta2 - k1 + bigRsqr;
            if (discr < 0)
                return false;
            double t = -beta - (discr = Sqrt(discr));
            if (t <= 0 && (t = discr - beta) <= 0 || t > maxt)
                return false;
        }
        // -------------------------------------
        k1 -= r01sqr;
        beta += beta; beta += beta;
        beta2 += beta2 + k1;
        Solver.Solve(
            a1: beta,
            a2: (Dy * Dy) * r0sqr + beta2 + beta2,
            a3: (PDy2 + PDy2) * r0sqr + beta * k1,
            a4: (y2 - r1sqr) * r0sqr + k1 * k1,
            roots: ref roots);
        if (roots.Count == 0)
            return false;
        if (roots.Count == 2)
            if (roots.R0 >= Tolerance.Epsilon)
                if (roots.R0 <= maxt)
                    beta = roots.R0;
                else
                    return false;
            else if (Tolerance.Epsilon <= roots.R1 && roots.R1 <= maxt)
                beta = roots.R1;
            else
                return false;
        else if ((beta = roots.HitTest(maxt)) < 0.0)
            return false;
        info.Time = beta;
        info.HitPoint = new(
            (x += beta * Dx) + cx,
            (y += beta * Dy) + cy,
            (z += beta * Dz) + cz);
        k1 = r1inv - r0r1inv / Sqrt(x * x + z * z);
        info.Normal = new(k1 * x, r1inv * y, k1 * z);
        info.Material = material;
        return true;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        double x = location.X - cx;
        double z = location.Z - cz;
        double k = r1inv - r0r1inv / Sqrt(x * x + z * z);
        return negated
            ? new Vector(-k * x, (cy - location.Y) * r1inv, -k * z)
            : new Vector(k * x, (location.Y - cy) * r1inv, k * z);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        double Dx = ray.Direction.X, Dy = ray.Direction.Y, Dz = ray.Direction.Z;
        double len2 = 1.0 / (Dx * Dx + Dy * Dy + Dz * Dz), len4 = len2 * len2;
        double x = ray.Origin.X - cx;
        double y = ray.Origin.Y - cy, y2 = y * y;
        double z = ray.Origin.Z - cz;
        double PDy2 = y * Dy;
        double k1 = (x * x + z * z + y2 - r01sqr) * len2;
        double k2 = (x * Dx + z * Dz + PDy2) * len2;
        Solver.Solve(
            k2 * 4,
            4 * k2 * k2 + r0sqr * Dy * Dy * len4 + k1 + k1,
            4 * k2 * k1 + 2 * r0sqr * PDy2 * len4,
            k1 * k1 + r0sqr * (y2 - r1sqr) * len4,
            ref roots);
        switch (roots.Count)
        {
            case 0:
                return 0;
            case 1:
                // A tangent ray.
                hits[1].Time = roots.R0;
                hits[0].Time = roots.R0;
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            case 2:
                // There's no need to sort roots: the solver already does it.
                hits[1].Time = roots.R1;
                hits[0].Time = roots.R0;
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            case 3:
                // The middle point is useless.
                if (roots.R0 > roots.R1)
                    (roots.R1, roots.R0) = (roots.R0, roots.R1);
                if (roots.R0 > roots.R2)
                    (roots.R2, roots.R0) = (roots.R0, roots.R2);
                hits[1].Time = roots.R2 > roots.R1 ? roots.R2 : roots.R1;
                hits[0].Time = roots.R0;
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            default:
                roots.GetHits4Roots(hits);
                hits[0].Shape = hits[1].Shape =
                    hits[2].Shape = hits[3].Shape = this;
                return 4;
        }
    }

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        YTorus t = new(Centroid, r0, r1, material.Clone(force));
        if (negated)
            t.Negate();
        return t;
    }

    #endregion
}

[Properties(nameof(r0), nameof(r1), nameof(checkBounds), nameof(Centroid), nameof(material))]
internal sealed class ZTorus : TorusBase, IShape
{
    public ZTorus(Vector center, double r0, double r1, IMaterial material)
        : base(center, r0, r1, material) => RecomputeBounds();

    /// <summary>Updates bounds and coefficients that depends on dimensions.</summary>
    protected override void RecomputeBounds()
    {
        base.RecomputeBounds();
        double sum = r0 + r1;
        bounds = Bounds.Create(-sum, -sum, -r1, sum, sum, r1) + Centroid;
    }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        double x = ray.Origin.X - cx;
        double y = ray.Origin.Y - cy;
        double z = ray.Origin.Z - cz;
        var (dx, dy, dz) = ray.Direction;
        double ray2 = ray.SquaredDir;
        double beta = (x * dx + y * dy + z * dz) / ray2;
        double z2 = z * z, k1 = x * x + y * y + z2;
        if (checkBounds)
        {
            double discr = (bigRsqr - k1) / ray2 + beta * beta;
            if (discr < 0)
                return false;
            x = -beta - (discr = Sqrt(discr));
            if (x <= 0 && (x = discr - beta) <= 0 || x > 1.0)
                return false;
        }
        // -------------------------
        ray2 = Sqrt(ray2);
        dz /= ray2;
        k1 -= r01sqr;
        beta *= ray2;
        x = beta + beta; x += x;
        Solver.Solve(
            x,
            r0sqr * dz * dz + x * beta + k1 + k1,
            r0sqr * (z + z) * dz + x * k1,
            (z2 - r1sqr) * r0sqr + k1 * k1,
            ref roots);
        if (roots.Count == 0)
            return false;
        if (roots.Count == 2)
            return Tolerance.Epsilon <= roots.R0 ?
                roots.R0 < ray2 :
                Tolerance.Epsilon <= roots.R1 && roots.R1 <= ray2;
        return roots.Count == 1 ?
            Tolerance.Epsilon <= roots.R0 && roots.R0 <= ray2 :
            Tolerance.Epsilon <= roots.R0 && roots.R0 <= ray2 ||
            Tolerance.Epsilon <= roots.R1 && roots.R1 <= ray2 ||
            Tolerance.Epsilon <= roots.R2 && roots.R2 <= ray2 ||
            roots.Count == 4 && Tolerance.Epsilon <= roots.R3 && roots.R3 <= ray2;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        double x = ray.Origin.X - cx;
        double y = ray.Origin.Y - cy;
        double z = ray.Origin.Z - cz;
        var (Dx, Dy, Dz) = ray.Direction;
        double z2 = z * z, k1 = x * x + y * y + z2;
        double PDz2 = z * Dz, beta = x * Dx + y * Dy + PDz2, beta2 = beta * beta;
        if (checkBounds)
        {
            double discr = beta2 - k1 + bigRsqr;
            if (discr < 0)
                return false;
            double t = -beta - (discr = Sqrt(discr));
            if (t <= 0 && (t = discr - beta) <= 0 || t > maxt)
                return false;
        }
        // -------------------------------------
        k1 -= r01sqr;
        beta += beta; beta += beta;
        beta2 += beta2 + k1;
        Solver.Solve(
            a1: beta,
            a2: (Dz * Dz) * r0sqr + beta2 + beta2,
            a3: (PDz2 + PDz2) * r0sqr + beta * k1,
            a4: (z2 - r1sqr) * r0sqr + k1 * k1,
            roots: ref roots);
        if (roots.Count == 0)
            return false;
        if (roots.Count == 2)
            if (roots.R0 >= Tolerance.Epsilon)
                if (roots.R0 <= maxt)
                    beta = roots.R0;
                else
                    return false;
            else if (Tolerance.Epsilon <= roots.R1 && roots.R1 <= maxt)
                beta = roots.R1;
            else
                return false;
        else if ((beta = roots.HitTest(maxt)) < 0.0)
            return false;
        info.Time = beta;
        info.HitPoint = new(
            (x += beta * Dx) + cx,
            (y += beta * Dy) + cy,
            (z += beta * Dz) + cz);
        k1 = r1inv - r0r1inv / Sqrt(x * x + y * y);
        info.Normal = new(k1 * x, k1 * y, r1inv * z);
        info.Material = material;
        return true;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        double x = location.X - cx;
        double y = location.Y - cy;
        double k = r1inv - r0r1inv / Sqrt(x * x + y * y);
        return negated
            ? new Vector(-k * x, -k * y, (cz - location.Z) * r1inv)
            : new Vector(k * x, k * y, (location.Z - cz) * r1inv);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        var (Dx, Dy, Dz) = ray.Direction;
        double len2 = 1.0 / (Dx * Dx + Dy * Dy + Dz * Dz), len4 = len2 * len2;
        double x = ray.Origin.X - cx;
        double y = ray.Origin.Y - cy;
        double z = ray.Origin.Z - cz, z2 = z * z;
        double PDz2 = z * Dz;
        double k1 = (x * x + y * y + z2 - r01sqr) * len2;
        double k2 = (x * Dx + y * Dy + PDz2) * len2;
        Solver.Solve(
            k2 * 4,
            4 * k2 * k2 + r0sqr * Dz * Dz * len4 + k1 + k1,
            4 * k2 * k1 + 2 * r0sqr * PDz2 * len4,
            k1 * k1 + r0sqr * (z2 - r1sqr) * len4,
            ref roots);
        switch (roots.Count)
        {
            case 0:
                return 0;
            case 1:
                hits[1].Time = roots.R0;
                hits[0].Time = roots.R0;
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            case 2:
                // There's no need to sort roots.
                hits[1].Time = roots.R1;
                hits[0].Time = roots.R0;
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            case 3:
                if (roots.R0 > roots.R1)
                    (roots.R1, roots.R0) = (roots.R0, roots.R1);
                if (roots.R0 > roots.R2)
                    (roots.R2, roots.R0) = (roots.R0, roots.R2);
                hits[1].Time = roots.R2 > roots.R1 ? roots.R2 : roots.R1;
                hits[0].Time = roots.R0;
                hits[0].Shape = hits[1].Shape = this;
                return 2;
            default:
                roots.GetHits4Roots(hits);
                hits[0].Shape = hits[1].Shape =
                    hits[2].Shape = hits[3].Shape = this;
                return 4;
        }
    }

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        ZTorus t = new(Centroid, r0, r1, material.Clone(force));
        if (negated)
            t.Negate();
        return t;
    }

    #endregion
}
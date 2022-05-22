using System.Diagnostics.CodeAnalysis;
using static System.Math;

namespace IntSight.RayTracing.Engine;

/// <summary>Common base for cylinders and cones.</summary>
public abstract class LinearSoR : MaterialShape
{
    protected Vector bottom, top, bottomNormal, topNormal;
    protected double radius, height, r2, rinv;
    protected Matrix transform, inverse;
    protected bool negated;

    protected LinearSoR(Vector bottom, Vector top, double radius, IMaterial material)
        : base(material)
    {
        this.bottom = bottom;
        this.top = top;
        this.radius = radius;
        RecomputeBounds();
    }

    /// <summary>Recalculated the binding box and the binding sphere.</summary>
    protected virtual void RecomputeBounds()
    {
        r2 = radius * radius;
        rinv = 1.0 / radius;
        height = top.Distance(bottom);
        Vector v = top.Difference(bottom);
        double proj = Sqrt(1.0 - v.Y * v.Y);
        if (!Tolerance.Zero(proj))
            transform = new(
                v.Z / proj, v.X, v.X * v.Y / proj,
                0.0, v.Y, -proj,
                -v.X / proj, v.Z, v.Y * v.Z / proj);
        else if (v.Y > 0.0)
            transform = Matrix.Identity;
        else
            transform = new(1.0, -1.0, 1.0);
        inverse = transform.Transpose();
        bounds = Bounds.Create(-radius, 0.0, -radius, radius, height, radius).
            Transform(transform, bottom);
        bottomNormal = new(-transform.A12, -transform.A22, -transform.A32);
        topNormal = new(transform.A12, transform.A22, transform.A32);
    }

    #region IShape precursors.

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public int MaxHits => 2;

    /// <summary>Estimated complexity.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public ShapeCost Cost => ShapeCost.NoPainNoGain;

    /// <summary>
    /// Called at object creation time, to invert normals for difference's parameters
    /// starting with the second one.
    /// </summary>
    public void Negate() => negated = !negated;

    #endregion

    #region ITransformable members.

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation) => TransformationCost.Ok;

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor) =>
        // Only true for a non deforming scale change or else,
        // when the deformation takes place along the axis of simmetry.
        factor.X == factor.Y && factor.Y == factor.Z ||
            factor.Y == factor.Z && Tolerance.IsZero(bottomNormal ^ Vector.XRay) ||
            factor.Z == factor.X && Tolerance.IsZero(bottomNormal ^ Vector.YRay) ||
            factor.X == factor.Y && Tolerance.IsZero(bottomNormal ^ Vector.ZRay) ?
            TransformationCost.Ok : TransformationCost.Nope;

    /// <summary>Translate this shape.</summary>
    /// <param name="translation">Translation amount.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        bottom += translation;
        top += translation;
        material = material.Translate(translation);
        bounds = Bounds.Create(-radius, 0.0, -radius, radius, height, radius).
            Transform(transform, bottom);
    }

    /// <summary>Rotate this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        bottom = rotation * bottom;
        top = rotation * top;
        RecomputeBounds();
    }

    /// <summary>Scale this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        bottom = bottom.Scale(factor);
        top = top.Scale(factor);
        radius *= factor.X;
        RecomputeBounds();
    }

    #endregion
}

[XSight]
[Properties(nameof(radius), nameof(negated), nameof(bottom), nameof(top), nameof(material))]
public sealed class Cylinder : LinearSoR, IShape
{
    public Cylinder(
        [Proposed("[0,0,0]")] Vector bottom,
        [Proposed("[0,1,0]")] Vector top,
        [Proposed("1")] double radius,
        IMaterial material)
        : base(bottom, top, radius, material) { }

    public Cylinder(
        [Proposed("[0,0,0]")] Vector bottom,
        [Proposed("1")] double height,
        [Proposed("1")] double radius,
        IMaterial material)
        : this(bottom, bottom + height * Vector.YRay, radius, material) { }

    public Cylinder(
        [Proposed("0")] double x, [Proposed("0")] double y, [Proposed("0")] double z,
        [Proposed("1")] double height,
        [Proposed("1")] double radius,
        IMaterial material)
        : this(new(x, y, z), new Vector(x, y + height, z), radius, material) { }

    public Cylinder(
        [Proposed("0")] double x0, [Proposed("0")] double y0, [Proposed("0")] double z0,
        [Proposed("0")] double x1, [Proposed("1")] double y1, [Proposed("0")] double z1,
        [Proposed("1")] double radius,
        IMaterial material)
        : this(new(x0, y0, z0), new Vector(x1, y1, z1), radius, material) { }

    #region IShape members.

    /// <summary>The center of the bounding sphere.</summary>
    Vector IBounded.Centroid => 0.5 * (bottom + top);

    /// <summary>The square radius of the bounding sphere.</summary>
    double IBounded.SquaredRadius => 0.25 * height * height + r2;

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        // Apply the inverse transformation to the incoming ray
        Vector org = inverse.Rotate(
            ray.Origin.X - bottom.X, ray.Origin.Y - bottom.Y, ray.Origin.Z - bottom.Z);
        Vector dir = inverse * ray.Direction;
        double tt0 = Tolerance.Epsilon, tt1 = 1.0, temp;
        double t0 = -org.Y * (temp = 1.0 / dir.Y);
        double t1 = (height - org.Y) * temp;
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
        temp = 1.0 / (dir.X * dir.X + dir.Z * dir.Z);
        double beta = -(dir.X * org.X + dir.Z * org.Z) * temp;
        temp = (r2 - org.X * org.X - org.Z * org.Z) * temp + beta * beta;
        if (temp < 0)
            return false;
        t0 = beta - (temp = Sqrt(temp));
        t1 = beta + temp;
        if (t0 > tt0) tt0 = t0;
        if (t1 < tt1) tt1 = t1;
        return tt0 <= tt1;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        // Apply the inverse transformation to the incoming ray
        Vector org = inverse.Rotate(
            ray.Origin.X - bottom.X, ray.Origin.Y - bottom.Y, ray.Origin.Z - bottom.Z);
        Vector dir = inverse * ray.Direction;
        // Compute time bounds for the Y axis
        double tt0, tt1, temp;
        if ((temp = 1.0 / dir.Y) >= 0)
        {
            tt0 = -org.Y * temp;
            tt1 = (height - org.Y) * temp;
        }
        else
        {
            tt1 = -org.Y * temp;
            tt0 = (height - org.Y) * temp;
        }
        // Compute time bounds for the XZ plane
        temp = 1.0 / (1.0 - dir.Y * dir.Y);
        double beta = -(dir.X * org.X + dir.Z * org.Z) * temp;
        temp = (r2 - org.X * org.X - org.Z * org.Z) * temp + beta * beta;
        if (temp < 0)
            return false;
        double t0 = beta - (temp = Sqrt(temp)), t1 = beta + temp;
        if (t0 > tt0) tt0 = t0;
        if (t1 < tt1) tt1 = t1;
        if (tt0 > tt1)
            return false;
        // Check whether any of the intersections satisfy the initial time constraints.
        if (tt0 < Tolerance.Epsilon && (tt0 = tt1) < Tolerance.Epsilon)
            return false;
        if (tt0 > maxt)
            return false;
        // Fill the hit information record
        info.Time = tt0;
        info.Normal = (beta = FusedMultiplyAdd(tt0, dir.Y, org.Y)) < Tolerance.Epsilon
            ? bottomNormal
            : height - beta < Tolerance.Epsilon
            ? topNormal
            : transform.RotateXZ(
                FusedMultiplyAdd(tt0, dir.X, org.X) * rinv,
                FusedMultiplyAdd(tt0, dir.Z, org.Z) * rinv);
        info.HitPoint = ray[tt0];
        info.Material = material;
        return true;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        Vector lct = inverse.Rotate(
            location.X - bottom.X, location.Y - bottom.Y, location.Z - bottom.Z);
        return lct.Y < Tolerance.Epsilon
            ? negated ? topNormal : bottomNormal
            : height - lct.Y < Tolerance.Epsilon
            ? negated ? bottomNormal : topNormal
            : negated ? transform.RotateXZ(-lct.X * rinv, -lct.Z * rinv)
            : transform.RotateXZ(lct.X / rinv, lct.Z / rinv);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        // Apply the inverse transformation to the incoming ray
        Vector org = inverse.Rotate(
            ray.Origin.X - bottom.X, ray.Origin.Y - bottom.Y, ray.Origin.Z - bottom.Z);
        Vector dir = inverse * ray.Direction;
        // Compute time bounds for the Y axis
        double tt0, tt1, temp;
        if ((temp = 1 / dir.Y) >= 0)
        {
            tt0 = -org.Y * temp;
            tt1 = (height - org.Y) * temp;
        }
        else
        {
            tt1 = -org.Y * temp;
            tt0 = (height - org.Y) * temp;
        }
        // Compute time bounds for the XZ plane
        temp = 1 / (dir.X * dir.X + dir.Z * dir.Z);
        double beta = -(dir.X * org.X + dir.Z * org.Z) * temp;
        temp = beta * beta - (org.X * org.X + org.Z * org.Z - r2) * temp;
        if (temp < 0)
            return 0;
        temp = Sqrt(temp);
        double t0 = beta - temp, t1 = beta + temp;
        if (t0 > tt0) tt0 = t0;
        if (t1 < tt1) tt1 = t1;
        if (tt0 > tt1)
            return 0;
        hits[1].Time = tt1;
        hits[0].Time = tt0;
        hits[0].Shape = hits[1].Shape = this;
        return 2;
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
            IShape c = new Cylinder(bottom, top, radius, m);
            if (negated)
                c.Negate();
            return c;
        }
        else
            return this;
    }

    /// <summary>Second optimization pass, where special shapes are introduced.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Substitute()
    {
        IShape s;
        if (transform.IsIdentity)
            s = new YCylinder(bottom, top, radius, material);
        else if (transform.IsDiagonal(1.0, -1.0, 1.0))
            s = new YCylinder(top, bottom, radius, material);
        else if (bottomNormal.X == -1.0 &&
            Tolerance.Zero(bottomNormal.Y) && Tolerance.Zero(bottomNormal.Z))
            s = new XCylinder(bottom, top, radius, material);
        else if (bottomNormal.X == +1.0 &&
            Tolerance.Zero(bottomNormal.Y) && Tolerance.Zero(bottomNormal.Z))
            s = new XCylinder(top, bottom, radius, material);
        else if (bottomNormal.Z == -1.0 &&
            Tolerance.Zero(bottomNormal.X) && Tolerance.Zero(bottomNormal.Y))
            s = new ZCylinder(bottom, top, radius, material);
        else if (bottomNormal.Z == +1.0 &&
            Tolerance.Zero(bottomNormal.X) && Tolerance.Zero(bottomNormal.Y))
            s = new ZCylinder(top, bottom, radius, material);
        else
            return this;
        if (negated)
            s.Negate();
        return s;
    }

    /// <summary>Is the vector parameter one of the main axes?</summary>
    /// <param name="n">Vector to test.</param>
    /// <returns>True if succeeded.</returns>
    private static bool IsCanonical(Vector n) =>
        Tolerance.IsZero(n - Vector.XRay) || Tolerance.IsZero(n - Vector.XRayM) ||
        Tolerance.IsZero(n - Vector.YRay) || Tolerance.IsZero(n - Vector.YRayM) ||
        Tolerance.IsZero(n - Vector.ZRay) || Tolerance.IsZero(n - Vector.ZRayM);

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation) =>
        (IsCanonical(bottomNormal) && !IsCanonical(rotation * bottomNormal)) ?
            TransformationCost.Depends :
            TransformationCost.Ok;

    #endregion
}

/// <summary>Common base for axis-aligned cylinders.</summary>
internal abstract class CylinderBase : MaterialShape
{
    protected Vector bottom, top;
    protected double radius, height, r2, rinv, rInvNg;
    protected bool negated;

    /// <summary>Initializes a cylinder.</summary>
    /// <param name="bottom">Point at the center of the bottom face.</param>
    /// <param name="top">Poit at the center of the upper face.</param>
    /// <param name="radius">Radius of the cylinder.</param>
    /// <param name="material">Material the cylinder is made of.</param>
    protected CylinderBase(Vector bottom, Vector top, double radius, IMaterial material)
        : base(material)
    {
        this.bottom = bottom;
        this.top = top;
        this.radius = radius;
        r2 = radius * radius;
        rinv = 1.0 / radius;
        rInvNg = 1.0 / radius;
        height = top.Distance(bottom);
    }

    /// <summary>The center of the bounding sphere.</summary>
    public override Vector Centroid => 0.5 * (bottom + top);

    /// <summary>The square radius of the bounding sphere.</summary>
    public override double SquaredRadius => 0.25 * height * height + r2;

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public int MaxHits => 2;

    /// <summary>Estimated complexity.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public ShapeCost Cost => ShapeCost.NoPainNoGain;

    /// <summary>
    /// Called at object creation time, to invert normals for difference's parameters
    /// starting with the second one.
    /// </summary>
    public void Negate() => (negated, rInvNg) = (!negated, -rInvNg);
}

/// <summary>A cylinder aligned to the X axis.</summary>
[Properties(nameof(radius), nameof(negated), nameof(bottom), nameof(top), nameof(material))]
internal sealed class XCylinder : CylinderBase, IShape
{
    public XCylinder(Vector bottom, Vector top, double radius, IMaterial material)
        : base(bottom, top, radius, material) =>
        bounds = Bounds.Create(0, -radius, -radius, height, radius, radius) + bottom;

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        // Apply the inverse transformation to the incoming ray
        double orgX = bottom.X - ray.Origin.X;
        double tt0 = Tolerance.Epsilon, tt1 = 1.0, temp = ray.InvDir.X;
        double t0 = orgX * temp, t1 = (height + orgX) * temp;
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
        var (dirX, dirY, dirZ) = ray.Direction;
        double orgY = bottom.Y - ray.Origin.Y, orgZ = bottom.Z - ray.Origin.Z;
        double beta = (dirY * orgY + dirZ * orgZ) *
            (temp = 1.0 / (ray.SquaredDir - dirX * dirX));
        if ((temp = beta * beta - (orgY * orgY + orgZ * orgZ - r2) * temp) < 0)
            return false;
        t0 = beta - (temp = Sqrt(temp));
        t1 = beta + temp;
        return t0 > tt0 ?
            (t1 < tt1 ? t0 <= t1 : t0 <= tt1) :
            (t1 < tt1 ? tt0 <= t1 : tt0 <= tt1);
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        Vector dir = ray.Direction;
        // Compute time bounds for the X axis
        double tt0, tt1, temp, orgX = bottom.X - ray.Origin.X;
        if ((temp = ray.InvDir.X) >= 0)
        {
            tt0 = orgX * temp;
            tt1 = (height + orgX) * temp;
        }
        else
        {
            tt1 = orgX * temp;
            tt0 = (height + orgX) * temp;
        }
        // Compute time bounds for the YZ plane
        temp = 1.0 / (1.0 - dir.X * dir.X);
        double orgY = bottom.Y - ray.Origin.Y, orgZ = bottom.Z - ray.Origin.Z;
        double beta = (dir.Y * orgY + dir.Z * orgZ) * temp;
        temp = beta * beta + (r2 - orgY * orgY - orgZ * orgZ) * temp;
        if (temp < 0)
            return false;
        temp = Sqrt(temp);
        double t0 = beta - temp, t1 = beta + temp;
        if (t0 > tt0) tt0 = t0;
        if (t1 < tt1) tt1 = t1;
        if (tt0 > tt1)
            return false;
        // Check whether the first intersection satifies the initial time constraints
        if (tt0 < Tolerance.Epsilon)
            if ((tt0 = tt1) < Tolerance.Epsilon)
                return false;
        if (tt0 > maxt)
            return false;
        // Fill the hit information record
        info.Time = tt0;
        info.HitPoint = ray[tt0];
        if ((beta = info.HitPoint.X - bottom.X) < Tolerance.Epsilon)
            info.Normal = Vector.XRayM;
        else if (height - beta < Tolerance.Epsilon)
            info.Normal = Vector.XRay;
        else
            info.Normal = new(
                0.0,
                (info.HitPoint.Y - bottom.Y) * rinv,
                (info.HitPoint.Z - bottom.Z) * rinv);
        info.Material = material;
        return true;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        double x = location.X - bottom.X;
        return x < Tolerance.Epsilon
            ? negated ? Vector.XRay : Vector.XRayM
            : height - x < Tolerance.Epsilon
            ? negated ? Vector.XRayM : Vector.XRay
            : new Vector(
                0.0, (location.Y - bottom.Y) * rInvNg, (location.Z - bottom.Z) * rInvNg);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        Vector dir = ray.Direction;
        // Compute time bounds for the Y axis
        double tt0, tt1, temp, orgX = bottom.X - ray.Origin.X;
        if ((temp = ray.InvDir.X) >= 0)
        {
            tt0 = orgX * temp;
            tt1 = (height + orgX) * temp;
        }
        else
        {
            tt1 = orgX * temp;
            tt0 = (height + orgX) * temp;
        }
        // Compute time bounds for the YZ plane
        temp = 1 / (dir.Y * dir.Y + dir.Z * dir.Z);
        double orgY = bottom.Y - ray.Origin.Y, orgZ = bottom.Z - ray.Origin.Z;
        double beta = (dir.Y * orgY + dir.Z * orgZ) * temp;
        temp = beta * beta - (orgY * orgY + orgZ * orgZ - r2) * temp;
        if (temp < 0)
            return 0;
        temp = Sqrt(temp);
        double t0 = beta - temp, t1 = beta + temp;
        if (t0 > tt0) tt0 = t0;
        if (t1 < tt1) tt1 = t1;
        if (tt0 > tt1)
            return 0;
        hits[1].Time = tt1;
        hits[0].Time = tt0;
        hits[0].Shape = hits[1].Shape = this;
        return 2;
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
            IShape c = new XCylinder(bottom, top, radius, m);
            if (negated)
                c.Negate();
            return c;
        }
        else
            return this;
    }

    #endregion
}

/// <summary>A cylinder aligned to the Y axis.</summary>
[Properties(nameof(radius), nameof(negated), nameof(bottom), nameof(top), nameof(material))]
internal sealed class YCylinder : CylinderBase, IShape
{
    public YCylinder(Vector bottom, Vector top, double radius, IMaterial material)
        : base(bottom, top, radius, material) =>
        bounds = Bounds.Create(-radius, 0, -radius, radius, height, radius) + bottom;

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        // Compute time bounds for the Y axis
        double orgY = bottom.Y - ray.Origin.Y;
        double tt0 = Tolerance.Epsilon, tt1 = 1.0, temp = ray.InvDir.Y;
        double t0 = orgY * temp, t1 = (height + orgY) * temp;
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
        // Compute time bounds for the XZ plane
        var (dirX, dirY, dirZ) = ray.Direction;
        double orgX = bottom.X - ray.Origin.X, orgZ = bottom.Z - ray.Origin.Z;
        double beta = (dirX * orgX + dirZ * orgZ) *
            (temp = 1.0 / (ray.SquaredDir - dirY * dirY));
        if ((temp = beta * beta - (orgX * orgX + orgZ * orgZ - r2) * temp) < 0)
            return false;
        t0 = beta - (temp = Sqrt(temp));
        t1 = beta + temp;
        return t0 > tt0 ?
            (t1 < tt1 ? t0 <= t1 : t0 <= tt1) :
            (t1 < tt1 ? tt0 <= t1 : tt0 <= tt1);
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        Vector dir = ray.Direction;
        // Compute time bounds for the Y axis
        double tt0, tt1, temp, orgY = bottom.Y - ray.Origin.Y;
        if ((temp = ray.InvDir.Y) >= 0)
        {
            tt0 = orgY * temp;
            tt1 = (height + orgY) * temp;
        }
        else
        {
            tt1 = orgY * temp;
            tt0 = (height + orgY) * temp;
        }
        // Compute time bounds for the XZ plane
        temp = 1.0 / (1.0 - dir.Y * dir.Y);
        double orgX = bottom.X - ray.Origin.X, orgZ = bottom.Z - ray.Origin.Z;
        double beta = (dir.X * orgX + dir.Z * orgZ) * temp;
        temp = (r2 - orgX * orgX - orgZ * orgZ) * temp + beta * beta;
        if (temp < 0)
            return false;
        temp = Sqrt(temp);
        double t0 = beta - temp, t1 = beta + temp;
        if (t0 > tt0) tt0 = t0;
        if (t1 < tt1) tt1 = t1;
        if (tt0 > tt1)
            return false;
        // Check whether the first intersection satifies the initial time constraints
        if (tt0 < Tolerance.Epsilon)
            if ((tt0 = tt1) < Tolerance.Epsilon)
                return false;
        if (tt0 > maxt)
            return false;
        // Fill the hit information record
        info.Time = tt0;
        info.HitPoint = ray[tt0];
        if ((beta = info.HitPoint.Y - bottom.Y) < Tolerance.Epsilon)
            info.Normal = Vector.YRayM;
        else if (height - beta < Tolerance.Epsilon)
            info.Normal = Vector.YRay;
        else
            info.Normal = new(
                (info.HitPoint.X - bottom.X) * rinv,
                0.0,
                (info.HitPoint.Z - bottom.Z) * rinv);
        info.Material = material;
        return true;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        double y = location.Y - bottom.Y;
        return y < Tolerance.Epsilon
            ? negated ? Vector.YRay : Vector.YRayM
            : height - y < Tolerance.Epsilon
            ? negated ? Vector.YRayM : Vector.YRay
            : new Vector(
                (location.X - bottom.X) * rInvNg, 0.0, (location.Z - bottom.Z) * rInvNg);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        double dirX = ray.Direction.X, dirZ = ray.Direction.Z;
        // Compute time bounds for the Y axis
        double tt0, tt1, temp, orgY = bottom.Y - ray.Origin.Y;
        if ((temp = ray.InvDir.Y) >= 0)
        {
            tt0 = orgY * temp;
            tt1 = (height + orgY) * temp;
        }
        else
        {
            tt1 = orgY * temp;
            tt0 = (height + orgY) * temp;
        }
        // Compute time bounds for the XZ plane
        temp = 1 / (dirX * dirX + dirZ * dirZ);
        double orgX = bottom.X - ray.Origin.X, orgZ = bottom.Z - ray.Origin.Z;
        double beta = (dirX * orgX + dirZ * orgZ) * temp;
        temp = beta * beta - (orgX * orgX + orgZ * orgZ - r2) * temp;
        if (temp < 0)
            return 0;
        temp = Sqrt(temp);
        double t0 = beta - temp, t1 = beta + temp;
        if (t0 > tt0) tt0 = t0;
        if (t1 < tt1) tt1 = t1;
        if (tt0 > tt1)
            return 0;
        hits[1].Time = tt1;
        hits[0].Time = tt0;
        hits[0].Shape = hits[1].Shape = this;
        return 2;
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
            IShape c = new YCylinder(bottom, top, radius, m);
            if (negated)
                c.Negate();
            return c;
        }
        else
            return this;
    }

    #endregion
}

/// <summary>A cylinder aligned to the Z axis.</summary>
[Properties(nameof(radius), nameof(negated), nameof(bottom), nameof(top), nameof(material))]
internal sealed class ZCylinder : CylinderBase, IShape
{
    public ZCylinder(Vector bottom, Vector top, double radius, IMaterial material)
        : base(bottom, top, radius, material) =>
        bounds = Bounds.Create(-radius, -radius, 0, radius, radius, height) + bottom;

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        // Compute time bounds for the XY plane
        var (dirX, dirY, dirZ) = ray.Direction;
        double temp = 1.0 / (ray.SquaredDir - dirZ * dirZ);
        double orgX = bottom.X - ray.Origin.X, orgY = bottom.Y - ray.Origin.Y;
        double beta = (dirX * orgX + dirY * orgY) * temp;
        if ((temp = beta * beta - (orgX * orgX + orgY * orgY - r2) * temp) < 0)
            return false;
        double t0 = beta - (temp = Sqrt(temp));
        double t1 = beta + temp;
        double tt0 = t0 > Tolerance.Epsilon ? t0 : Tolerance.Epsilon;
        double tt1 = t1 < 1.0 ? t1 : 1.0;
        if (tt0 > tt1)
            return false;
        // Compute time bounds for the Z axis
        beta = bottom.Z - ray.Origin.Z;
        t0 = beta * (temp = ray.InvDir.Z);
        t1 = (height + beta) * temp;
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
        return tt0 <= tt1;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        Vector dir = ray.Direction;
        // Compute time bounds for the XY plane
        double temp = 1.0 / (1.0 - dir.Z * dir.Z);
        double orgX = bottom.X - ray.Origin.X, orgY = bottom.Y - ray.Origin.Y;
        double beta = (dir.X * orgX + dir.Y * orgY) * temp;
        if ((temp = beta * beta + (r2 - orgX * orgX - orgY * orgY) * temp) < 0)
            return false;
        double tt0 = beta - (temp = Sqrt(temp)), tt1 = beta + temp;
        // Compute time bounds for the Z axis
        double t0, t1, orgZ = bottom.Z - ray.Origin.Z;
        if ((temp = ray.InvDir.Z) >= 0)
        {
            t0 = orgZ * temp;
            t1 = (height + orgZ) * temp;
        }
        else
        {
            t1 = orgZ * temp;
            t0 = (height + orgZ) * temp;
        }
        if (t0 > tt0) tt0 = t0;
        if (t1 < tt1) tt1 = t1;
        if (tt0 > tt1)
            return false;
        // Check whether the first intersection satifies the initial time constraints
        if (tt0 < Tolerance.Epsilon)
            if ((tt0 = tt1) < Tolerance.Epsilon)
                return false;
        if (tt0 > maxt)
            return false;
        // Fill the hit information record
        info.Time = tt0;
        info.HitPoint = ray[tt0];
        if ((beta = info.HitPoint.Z - bottom.Z) < Tolerance.Epsilon)
            info.Normal = Vector.ZRayM;
        else if (height - beta < Tolerance.Epsilon)
            info.Normal = Vector.ZRay;
        else
            info.Normal = new(
                (info.HitPoint.X - bottom.X) * rinv,
                (info.HitPoint.Y - bottom.Y) * rinv,
                0.0);
        info.Material = material;
        return true;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        double z = location.Z - bottom.Z;
        return z < Tolerance.Epsilon
            ? negated ? Vector.ZRay : Vector.ZRayM
            : height - z < Tolerance.Epsilon
            ? negated ? Vector.ZRayM : Vector.ZRay
            : new Vector(
                (location.X - bottom.X) * rInvNg, (location.Y - bottom.Y) * rInvNg, 0.0);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        Vector dir = ray.Direction;
        // Compute time bounds for the Z axis
        double tt0, tt1, temp, orgZ = bottom.Z - ray.Origin.Z;
        if ((temp = ray.InvDir.Z) >= 0)
        {
            tt0 = orgZ * temp;
            tt1 = (height + orgZ) * temp;
        }
        else
        {
            tt1 = orgZ * temp;
            tt0 = (height + orgZ) * temp;
        }
        // Compute time bounds for the XY plane
        temp = 1 / (dir.X * dir.X + dir.Y * dir.Y);
        double orgX = bottom.X - ray.Origin.X, orgY = bottom.Y - ray.Origin.Y;
        double beta = (dir.X * orgX + dir.Y * orgY) * temp;
        temp = beta * beta - (orgX * orgX + orgY * orgY - r2) * temp;
        if (temp < 0)
            return 0;
        temp = Sqrt(temp);
        double t0 = beta - temp, t1 = beta + temp;
        if (t0 > tt0) tt0 = t0;
        if (t1 < tt1) tt1 = t1;
        if (tt0 > tt1)
            return 0;
        hits[1].Time = tt1;
        hits[0].Time = tt0;
        hits[0].Shape = hits[1].Shape = this;
        return 2;
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
            IShape c = new ZCylinder(bottom, top, radius, m);
            if (negated)
                c.Negate();
            return c;
        }
        else
            return this;
    }

    #endregion
}
using System.Diagnostics.CodeAnalysis;
using static System.Math;

namespace IntSight.RayTracing.Engine;

/// <summary>A plane with an arbitrary orientation.</summary>
[XSight, Properties(nameof(offset), nameof(normal), nameof(material))]
public sealed class Plane : MaterialShape, IShape
{
    private Vector normal, unitNormal, negatedNormal;
    private double offset, offsetLength;
    private bool negated;

    /// <summary>Creates a plane.</summary>
    /// <param name="normal">The normal vector.</param>
    /// <param name="offset">Offset along the normal vector.</param>
    /// <param name="material">Plane's material.</param>
    public Plane(
        [Proposed("[0,1,0]")] Vector normal,
        [Proposed("0")] double offset,
        IMaterial material)
        : base(material)
    {
        this.normal = normal;
        unitNormal = normal.Normalized();
        negatedNormal = unitNormal;
        this.offset = offset;
        offsetLength = offset * normal.Squared;
        bounds = new(normal, offset);
    }

    public Plane(
        [Proposed("0")] double dx, [Proposed("1")] double dy, [Proposed("0")] double dz,
        [Proposed("0")] double offset,
        IMaterial material)
        : this(new Vector(dx, dy, dz), offset, material) { }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        double t = (offsetLength - normal * ray.Origin) / (normal * ray.Direction);
        return Tolerance.Epsilon <= t && t <= 1.0;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        double time = (offsetLength - normal * ray.Origin) / (normal * ray.Direction);
        if (Tolerance.Epsilon <= time && time <= maxt)
        {
            info.Time = time;
            info.HitPoint = ray[time];
            info.Normal = unitNormal;
            info.Material = material;
            return true;
        }
        else
            return false;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location) => negatedNormal;

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        double cosine = normal * ray.Direction;
        if (cosine < 0.0)
        {
            hits[1].Time = double.MaxValue;
            hits[0].Time = (offsetLength - normal * ray.Origin) / cosine;
        }
        else
        {
            hits[1].Time = (offsetLength - normal * ray.Origin) / cosine;
            hits[0].Time = double.MinValue;
        }
        hits[1].Shape = hits[0].Shape = this;
        return 2;
    }

    #endregion

    #region ITransformable members.

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    int ITransformable.MaxHits => 2;

    /// <summary>Gets the estimated cost.</summary>
    ShapeCost ITransformable.Cost => ShapeCost.EasyPeasy;

    /// <summary>Invert normals for the right operand in a difference.</summary>
    void ITransformable.Negate() => (negated, negatedNormal) = (!negated, -negatedNormal);

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        IMaterial m = material.Clone(force);
        if (force || m != material)
        {
            IShape p = new Plane(normal, offset, m);
            if (negated) p.Negate();
            return p;
        }
        else
            return this;
    }

    /// <summary>Second optimization pass, where special shapes are introduced.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Substitute()
    {
        IShape s;
        if (Tolerance.IsZero(normal - Vector.XRay))
            s = new XPlane(offset, material);
        else if (Tolerance.IsZero(normal - Vector.YRay))
            s = new YPlane(offset, material);
        else if (Tolerance.IsZero(normal - Vector.ZRay))
            s = new ZPlane(offset, material);
        else
            return this;
        if (negated)
            s.Negate();
        return s;
    }

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation) => TransformationCost.Ok;

    /// <summary>Finds out how expensive would be statically scaling this plane.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor) => TransformationCost.Ok;

    /// <summary>Translate this plane.</summary>
    /// <param name="translation">Translation amount.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        offset += translation * unitNormal;
        offsetLength = offset * normal.Squared;
        material = material.Translate(translation);
        bounds = new(normal, offset);
    }

    /// <summary>Rotate this plane.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        normal = rotation * normal;
        unitNormal = normal.Normalized();
        negatedNormal = negated ? -unitNormal : unitNormal;
        bounds = new(normal, offset);
    }

    /// <summary>Scale this plane.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        normal = normal.Scale(factor.Invert());
        unitNormal = normal.Normalized();
        negatedNormal = negated ? -unitNormal : unitNormal;
        offset = offsetLength / normal.Squared;
        bounds = new(normal, offset);
    }

    #endregion
}

/// <summary>A common base class for all axis-aligned planes.</summary>
internal abstract class PlaneBase : MaterialShape
{
    protected readonly Vector unitNormal;
    protected readonly double offset;
    protected bool negated;

    protected PlaneBase(Vector unitNormal, double offset, IMaterial material)
        : base(material)
    {
        this.unitNormal = unitNormal;
        this.offset = offset;
        bounds = new(unitNormal, offset);
    }

    /// <summary>The center of the bounding sphere.</summary>
    public override Vector Centroid => offset * unitNormal;

    #region ITransformable precursors.

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public int MaxHits => 2;

    /// <summary>Estimated cost.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public ShapeCost Cost => ShapeCost.EasyPeasy;

    /// <summary>Invert normals for the right operand in a difference.</summary>
    public void Negate() => negated = !negated;

    #endregion
}

[Properties(nameof(offset), nameof(material))]
internal sealed class XPlane : PlaneBase, IShape
{
    public XPlane(double offset, IMaterial material)
        : base(Vector.XRay, offset, material) { }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        double t = (offset - ray.Origin.X) * ray.InvDir.X;
        return Tolerance.Epsilon <= t && t <= 1.0;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        double time = (offset - ray.Origin.X) * ray.InvDir.X;
        if (Tolerance.Epsilon <= time && time <= maxt)
        {
            info.Time = time;
            info.HitPoint = new(
                offset,
                FusedMultiplyAdd(time, ray.Direction.Y, ray.Origin.Y),
                FusedMultiplyAdd(time, ray.Direction.Z, ray.Origin.Z));
            info.Normal = Vector.XRay;
            info.Material = material;
            return true;
        }
        else
            return false;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location) => negated ? Vector.XRayM : Vector.XRay;

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        if (ray.Direction.X < 0.0)
        {
            hits[1].Time = double.MaxValue;
            hits[0].Time = (offset - ray.Origin.X) * ray.InvDir.X;
        }
        else
        {
            hits[1].Time = (offset - ray.Origin.X) * ray.InvDir.X;
            hits[0].Time = double.MinValue;
        }
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
            IShape p = new XPlane(offset, m);
            if (negated) p.Negate();
            return p;
        }
        else
            return this;
    }

    #endregion
}

[Properties(nameof(offset), nameof(material))]
internal sealed class YPlane : PlaneBase, IShape
{
    public YPlane(double offset, IMaterial material)
        : base(Vector.YRay, offset, material) { }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        double t = (offset - ray.Origin.Y) * ray.InvDir.Y;
        return Tolerance.Epsilon <= t && t <= 1.0;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        double time = (offset - ray.Origin.Y) * ray.InvDir.Y;
        if (Tolerance.Epsilon <= time && time <= maxt)
        {
            info.Time = time;
            info.HitPoint = new(
                FusedMultiplyAdd(time, ray.Direction.X, ray.Origin.X),
                offset,
                FusedMultiplyAdd(time, ray.Direction.Z, ray.Origin.Z));
            info.Normal = Vector.YRay;
            info.Material = material;
            return true;
        }
        else
            return false;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location) => negated ? Vector.YRayM : Vector.YRay;

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        if (ray.Direction.Y < 0.0)
        {
            hits[1].Time = double.MaxValue;
            hits[0].Time = (offset - ray.Origin.Y) * ray.InvDir.Y;
        }
        else
        {
            hits[1].Time = (offset - ray.Origin.Y) * ray.InvDir.Y;
            hits[0].Time = double.MinValue;
        }
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
            IShape p = new YPlane(offset, m);
            if (negated) p.Negate();
            return p;
        }
        else
            return this;
    }

    #endregion
}

[Properties(nameof(offset), nameof(material))]
internal sealed class ZPlane : PlaneBase, IShape
{
    public ZPlane(double offset, IMaterial material)
        : base(Vector.ZRay, offset, material) { }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        double t = (offset - ray.Origin.Z) * ray.InvDir.Z;
        return Tolerance.Epsilon <= t && t <= 1.0;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        double time = (offset - ray.Origin.Z) * ray.InvDir.Z;
        if (Tolerance.Epsilon <= time && time <= maxt)
        {
            info.Time = time;
            info.HitPoint = new(
                FusedMultiplyAdd(time, ray.Direction.X, ray.Origin.X),
                FusedMultiplyAdd(time, ray.Direction.Y, ray.Origin.Y),
                offset);
            info.Normal = Vector.ZRay;
            info.Material = material;
            return true;
        }
        else
            return false;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <param name="normal">Normal vector at that point.</param>
    Vector IShape.GetNormal(in Vector location) => negated ? Vector.ZRayM : Vector.ZRay;

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        if (ray.Direction.Z < 0.0)
        {
            hits[1].Time = double.MaxValue;
            hits[0].Time = (offset - ray.Origin.Z) * ray.InvDir.Z;
        }
        else
        {
            hits[1].Time = (offset - ray.Origin.Z) * ray.InvDir.Z;
            hits[0].Time = double.MinValue;
        }
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
            IShape p = new ZPlane(offset, m);
            if (negated) p.Negate();
            return p;
        }
        else
            return this;
    }

    #endregion
}
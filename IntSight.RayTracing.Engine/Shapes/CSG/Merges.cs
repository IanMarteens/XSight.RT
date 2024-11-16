using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Rsc = IntSight.RayTracing.Engine.Properties.Resources;

namespace IntSight.RayTracing.Engine;

/// <summary>Binary unions with inner surfaces removed.</summary>
[XSight, Children(nameof(shape0), nameof(shape1))]
public sealed class Merge : MaterialShape, IShape
{
    private IShape shape0, shape1;
    private Vector centroid;
    private double squaredRadius;
    private readonly Hit[] hits0;
    private readonly Hit[] hits1;

    public Merge(IShape shape0, IShape shape1, IMaterial material)
        : base(material)
    {
        this.shape0 = shape0;
        this.shape1 = shape1;
        hits0 = new Hit[shape0.MaxHits];
        hits1 = new Hit[shape1.MaxHits];
        RecomputeBounds();
    }

    /// <summary>Updates the bounding box and the bounding sphere around the union.</summary>
    private void RecomputeBounds()
    {
        bounds = shape0.Bounds + shape1.Bounds;
        (centroid, squaredRadius) = Merge(
            shape0.Centroid, shape0.SquaredRadius, shape1);
        if (!bounds.IsUniverse)
            bounds *= Bounds.FromSphere(centroid, Math.Sqrt(squaredRadius));
    }

    #region IShape members.

    /// <summary>The center of the bounding sphere.</summary>
    public override Vector Centroid => centroid;

    /// <summary>The square radius of the bounding sphere.</summary>
    public override double SquaredRadius => squaredRadius;

    /// <summary>Finds the first hit with positive time.</summary>
    /// <param name="ray">Ray to trace.</param>
    /// <param name="shape">The hiting shape.</param>
    /// <returns>Intersection time/distance.</returns>
    private double FindRoot(Ray ray, ref IShape shape)
    {
        int total0 = shape0.GetHits(ray, hits0), i0 = 0;
        int total1 = shape1.GetHits(ray, hits1), i1 = 0;
        if (total0 == 0)
            if (total1 == 0)
                return -1.0;
            else
                goto SCAN_1;
        else if (total1 == 0)
            goto SCAN_0;

        bool inside0 = false, inside1 = false, inside = false;
    LOOP:
        if (hits0[i0].Time <= hits1[i1].Time)
        {
            bool newInside = (inside0 = !inside0) | inside1;
            if (inside != newInside)
            {
                double t = hits0[i0].Time;
                if (t > Tolerance.Epsilon)
                {
                    shape = hits0[i0].Shape;
                    return t;
                }
                inside = newInside;
            }
            if (++i0 < total0) goto LOOP;
            goto SCAN_1;
        }
        else
        {
            bool newInside = inside0 | (inside1 = !inside1);
            if (inside != newInside)
            {
                double t = hits1[i1].Time;
                if (t > Tolerance.Epsilon)
                {
                    shape = hits1[i1].Shape;
                    return t;
                }
                inside = newInside;
            }
            if (++i1 < total1) goto LOOP;
        }

    SCAN_0:
        do
        {
            double t = hits0[i0].Time;
            if (t > Tolerance.Epsilon)
            {
                shape = hits0[i0].Shape;
                return t;
            }
        }
        while (++i0 < total0);
        return -1.0;

    SCAN_1:
        do
        {
            double t = hits1[i1].Time;
            if (t > Tolerance.Epsilon)
            {
                shape = hits1[i1].Shape;
                return t;
            }
        }
        while (++i1 < total1);
        return -1.0;
    }

    /// <summary>Find an intersection between union items and a ray.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        IShape shape = null;
        double t = FindRoot(ray, ref shape);
        return 0.0 <= t && t <= 1.0;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        IShape shape = null;
        double t = FindRoot(ray, ref shape);
        if (0.0 <= t && t <= maxt)
        {
            info.Time = t;
            info.HitPoint = ray[t];
            info.Normal = shape.GetNormal(info.HitPoint);
            info.Material = material;
            return true;
        }
        else
            return false;
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        int total0 = shape0.GetHits(ray, hits0);
        if (total0 == 0)
            return shape1.GetHits(ray, hits);
        int total1 = shape1.GetHits(ray, hits1);
        if (total1 == 0)
        {
            Array.Copy(hits0, hits, total0);
            return total0;
        }
        int i0 = 0, i1 = 0, total = 0;
        bool inside0 = false, inside1 = false, inside = false;
        do
            if (i0 < total0 && (i1 >= total1 || hits0[i0].Time <= hits1[i1].Time))
            {
                bool newInside = (inside0 = !inside0) | inside1;
                if (inside != newInside)
                {
                    hits[total++] = hits0[i0];
                    inside = newInside;
                }
                i0++;
            }
            else
            {
                bool newInside = inside0 | (inside1 = !inside1);
                if (inside != newInside)
                {
                    hits[total++] = hits1[i1];
                    inside = newInside;
                }
                i1++;
            }
        while (i0 < total0 || i1 < total1);
        return total;
    }

    /// <summary>Gets the normal vector given a point in the surface.</summary>
    /// <param name="location">Hit point.</param>
    /// <returns>Normal vector.</returns>
    public Vector GetNormal(in Vector location) =>
        throw new RenderException(Rsc.NotImplemented, "Merge.GetNormal");

    #endregion

    #region ITransformable members.

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    public int MaxHits => shape0.MaxHits + shape1.MaxHits;

    /// <summary>Estimated complexity.</summary>
    ShapeCost ITransformable.Cost => ShapeCost.PainInTheNeck;

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force) =>
        new Merge(shape0.Clone(force), shape1.Clone(force), material.Clone(force));

    /// <summary>Invert normals for the right operand in a difference.</summary>
    public void Negate()
    {
        shape0.Negate();
        shape1.Negate();
    }

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape.</returns>
    public override IShape Simplify()
    {
        shape0 = shape0.Simplify();
        shape1 = shape1.Simplify();
        return this;
    }

    /// <summary>Second optimization pass.</summary>
    /// <returns>This shape.</returns>
    public override IShape Substitute()
    {
        shape0 = shape0.Substitute();
        shape1 = shape1.Substitute();
        return this;
    }

    /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
    /// <param name="scene">The scene this shape is included within.</param>
    /// <param name="inCsg">Is this shape included in a CSG operation?</param>
    /// <param name="inTransform">Is this shape nested inside a transform?</param>
    public override void Initialize(IScene scene, bool inCsg, bool inTransform)
    {
        shape0.Initialize(scene, true, inTransform);
        shape1.Initialize(scene, true, inTransform);
    }

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation)
    {
        int problems = 0;
        return !shape0.CheckRotation(rotation, ref problems) ||
            !shape1.CheckRotation(rotation, ref problems)
            ? TransformationCost.Nope
            : problems <= 1 ? TransformationCost.Ok : TransformationCost.Nope;
    }

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor)
    {
        int problems = 0;
        return
            !shape0.CheckScale(factor, ref problems) ||
            !shape1.CheckScale(factor, ref problems)
            ? TransformationCost.Nope
            : problems <= 1 ? TransformationCost.Ok : TransformationCost.Nope;
    }

    /// <summary>Translates this shape.</summary>
    /// <param name="translation">Translation distance.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        shape0.ApplyTranslation(translation);
        shape1.ApplyTranslation(translation);
        RecomputeBounds();
    }

    /// <summary>Rotates this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        DoRotation(ref shape0, rotation);
        DoRotation(ref shape1, rotation);
        RecomputeBounds();
    }

    /// <summary>Scales this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        DoScale(ref shape0, factor);
        DoScale(ref shape1, factor);
        RecomputeBounds();
    }

    #endregion
}

[XSight]
[Children(nameof(shapes))]
public sealed class UnionMerge : MaterialShape, IShape
{
    private readonly IShape[] shapes;
    private readonly Hit[][] hits;
    private Vector centroid;
    private double squaredRadius;

    public UnionMerge(IShape[] shapes, IMaterial material)
        : base(material)
    {
        this.shapes = shapes;
        hits = new Hit[shapes.Length][];
        for (int i = 0; i < shapes.Length; i++)
            hits[i] = new Hit[shapes[i].MaxHits];
        RecomputeBounds();
    }

    public UnionMerge(IShape s1, IShape s2, IShape s3, IMaterial material)
        : this([s1, s2, s3], material) { }

    public UnionMerge(IShape s1, IShape s2, IShape s3, IShape s4, IMaterial material)
        : this([s1, s2, s3, s4], material) { }

    /// <summary>Updates the bounding box and the bounding sphere around the union.</summary>
    private void RecomputeBounds()
    {
        bounds = Bounds.Void; centroid = new(); squaredRadius = -1;
        foreach (IShape shape in shapes)
        {
            bounds += shape.Bounds;
            (centroid, squaredRadius) = Merge(Centroid, SquaredRadius, shape);
        }
        if (!bounds.IsUniverse)
            bounds *= Bounds.FromSphere(centroid, Math.Sqrt(squaredRadius));
    }

    #region IShape members.

    /// <summary>The center of the bounding sphere.</summary>
    public override Vector Centroid => centroid;

    /// <summary>The square radius of the bounding sphere.</summary>
    public override double SquaredRadius => squaredRadius;

    /// <summary>Finds the first hit with positive time.</summary>
    /// <param name="ray">Ray to trace.</param>
    /// <param name="shape">The hiting shape.</param>
    /// <returns>Intersection time/distance.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
    private double FindRoot(Ray ray, ref IShape shape) => -1.0;

    /// <summary>Find an intersection between union items and a ray.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        IShape shape = null;
        double t = FindRoot(ray, ref shape);
        return 0.0 <= t && t <= 1.0;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        IShape shape = null;
        double t = FindRoot(ray, ref shape);
        if (0.0 <= t && t <= maxt)
        {
            info.Time = t;
            info.HitPoint = ray[t];
            info.Normal = shape.GetNormal(info.HitPoint);
            info.Material = material;
            return true;
        }
        else
            return false;
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits) => 0;

    /// <summary>Gets the normal vector given a point in the surface.</summary>
    /// <param name="location">Hit point.</param>
    /// <returns>Normal vector.</returns>
    public Vector GetNormal(in Vector location) =>
        throw new RenderException(Rsc.NotImplemented, "UnionMerge.GetNormal");

    #endregion

    #region ITransformable members.

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    public int MaxHits => shapes.Select(s => s.MaxHits).Sum();

    /// <summary>Estimated complexity.</summary>
    ShapeCost ITransformable.Cost => ShapeCost.PainInTheNeck;

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        for (int i = 0; i < shapes.Length; i++)
        {
            IShape s = shapes[i].Clone(force);
            if (force || s != shapes[i])
            {
                IShape[] newShapes = new IShape[shapes.Length];
                for (int j = 0; j < i; j++)
                    newShapes[j] = shapes[j];
                newShapes[i] = s;
                for (int j = i + 1; j < newShapes.Length; j++)
                    newShapes[j] = shapes[j].Clone(force);
                return new UnionMerge(newShapes, material);
            }
        }
        return this;
    }

    /// <summary>Invert normals for the right operand in a difference.</summary>
    public void Negate()
    {
        foreach (IShape s in shapes)
            s.Negate();
    }

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape.</returns>
    public override IShape Simplify()
    {
        for (int i = 0; i < shapes.Length; i++)
            shapes[i] = shapes[i].Simplify();
        return this;
    }

    /// <summary>Second optimization pass.</summary>
    /// <returns>This shape.</returns>
    public override IShape Substitute()
    {
        for (int i = 0; i < shapes.Length; i++)
            shapes[i] = shapes[i].Substitute();
        return this;
    }

    /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
    /// <param name="scene">The scene this shape is included within.</param>
    /// <param name="inCsg">Is this shape included in a CSG operation?</param>
    /// <param name="inTransform">Is this shape nested inside a transform?</param>
    public override void Initialize(IScene scene, bool inCsg, bool inTransform)
    {
        foreach (IShape s in shapes)
            s.Initialize(scene, true, inTransform);
    }

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation)
    {
        int problems = 0;
        foreach (IShape s in shapes)
            if (!s.CheckRotation(rotation, ref problems))
                return TransformationCost.Nope;
        return problems <= 1 ? TransformationCost.Ok : TransformationCost.Nope;
    }

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor)
    {
        int problems = 0;
        foreach (IShape s in shapes)
            if (!s.CheckScale(factor, ref problems))
                return TransformationCost.Nope;
        return problems <= 1 ? TransformationCost.Ok : TransformationCost.Nope;
    }

    /// <summary>Translates this shape.</summary>
    /// <param name="translation">Translation distance.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        foreach (IShape s in shapes)
            s.ApplyTranslation(translation);
        RecomputeBounds();
    }

    /// <summary>Rotates this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        for (int i = 0; i < shapes.Length; i++)
            DoRotation(ref shapes[i], rotation);
        RecomputeBounds();
    }

    /// <summary>Scales this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        for (int i = 0; i < shapes.Length; i++)
            DoScale(ref shapes[i], factor);
        RecomputeBounds();
    }

    #endregion
}
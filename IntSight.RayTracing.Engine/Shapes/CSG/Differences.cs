using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Rsc = IntSight.RayTracing.Engine.Properties.Resources;

namespace IntSight.RayTracing.Engine;

/// <summary>General purpose difference.</summary>
[XSight(Alias = "Diff"), Children(nameof(shape0), nameof(shapes))]
public sealed class Difference : MaterialShape, IShape
{
    /// <summary>Asymetrical shapes: all other shapes are subtracted from this one.</summary>
    private IShape shape0;
    private IShape[] shapes;
    private Hit[] hits0;
    private readonly Hit[] hits1;
    private Hit[] accum;

    /// <summary>Creates a difference from a list of shapes.</summary>
    /// <param name="shapes">A list of shapes.</param>
    /// <param name="material">Material this shape is made of.</param>
    private Difference(IShape[] shapes, IMaterial material, bool negate)
        : base(material)
    {
        shape0 = shapes[0];
        this.shapes = new IShape[shapes.Length - 1];
        Array.Copy(shapes, 1, this.shapes, 0, this.shapes.Length);
        bounds = shape0.Bounds;
        int maxHits = 0, sumHits = shape0.MaxHits;
        foreach (IShape s in shapes)
        {
            int count = s.MaxHits;
            if (count > maxHits) maxHits = count;
            sumHits += count;
            if (negate && s != shape0)
                s.Negate();
        }
        hits0 = new Hit[sumHits];
        hits1 = new Hit[maxHits];
        accum = new Hit[sumHits];
    }

    /// <summary>Creates a difference from a list of shapes.</summary>
    /// <param name="shapes">A list of shapes.</param>
    /// <param name="material">Material this shape is made of.</param>
    public Difference(IShape[] shapes, IMaterial material) :
        this(shapes, material, true) { }

    public Difference(IShape shape0, IShape shape1, IMaterial material)
        : this([shape0, shape1], material) { }

    public Difference(IShape shape0, IShape shape1)
        : this([shape0, shape1], ((MaterialShape)shape0).Material) { }

    /// <summary>Finds the first positive intersection with a ray.</summary>
    /// <param name="ray">Ray to check.</param>
    /// <param name="obj">Shape hit by the ray.</param>
    /// <returns>The intersection time, or a negative number, if fails.</returns>
    private double FindRoot(Ray ray, ref IShape obj)
    {
        Hit[] h0 = hits0, h1 = hits1, acc = accum;
        int total0 = shape0.GetHits(ray, h0);
        if (total0 == 0)
            return -1.0;
        foreach (IShape shape1 in shapes)
        {
            int total1 = shape1.GetHits(ray, h1);
            if (total1 == 0)
                continue;
            bool inside0 = false, inside1 = false;
            int i0 = 0, i1 = 0, accTotal = 0;
        LOOP:
            if (h0[i0].Time <= h1[i1].Time)
            {
                inside0 = !inside0;
                if (!inside1)
                    acc[accTotal++] = h0[i0];
                if (++i0 < total0) goto LOOP;
                if (accTotal == 0)
                    return -1.0;
            }
            else
            {
                inside1 = !inside1;
                if (inside0)
                    acc[accTotal++] = h1[i1];
                if (++i1 < total1) goto LOOP;
                do
                    acc[accTotal++] = h0[i0];
                while (++i0 < total0);
            }
            total0 = accTotal;
            (h0, acc) = (acc, h0);
        }
        for (int i = 0; i < total0; i++)
        {
            double t = h0[i].Time;
            if (t > Tolerance.Epsilon)
            {
                obj = h0[i].Shape;
                return t;
            }
        }
        return -1.0;
    }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        IShape obj = null;
        double t = FindRoot(ray, ref obj);
        return 0.0 < t && t <= 1.0;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        IShape obj = null;
        double t = FindRoot(ray, ref obj);
        if (0.0 < t && t <= maxt)
        {
            info.Time = t;
            info.HitPoint = ray[t];
            info.Normal = obj.GetNormal(info.HitPoint);
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
            return 0;
        foreach (IShape shape1 in shapes)
        {
            int total1 = shape1.GetHits(ray, hits1);
            if (total1 == 0)
                continue;
            bool inside0 = false, inside1 = false;
            int i0 = 0, i1 = 0, accTotal = 0;
        LOOP:
            if (hits0[i0].Time <= hits1[i1].Time)
            {
                inside0 = !inside0;
                if (!inside1)
                    accum[accTotal++] = hits0[i0];
                if (++i0 < total0) goto LOOP;
                if (accTotal == 0)
                    return 0;
            }
            else
            {
                inside1 = !inside1;
                if (inside0)
                    accum[accTotal++] = hits1[i1];
                if (++i1 < total1) goto LOOP;
                do
                    accum[accTotal++] = hits0[i0];
                while (++i0 < total0);
            }
            total0 = accTotal;
            (hits0, accum) = (accum, hits0);
        }
        Array.Copy(hits0, hits, total0);
        return total0;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location) =>
        throw new RenderException(Rsc.NotImplemented, "Difference.GetNormal");

    #endregion

    #region ITransformable members.

    /// <summary>Gets the estimated complexity.</summary>
    ShapeCost ITransformable.Cost => ShapeCost.PainInTheNeck;

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    int ITransformable.MaxHits => shape0.MaxHits + shapes.Sum(s => s.MaxHits);

    /// <summary>Invert normals for the right operand in a difference.</summary>
    void ITransformable.Negate()
    {
        shape0.Negate();
        foreach (IShape s in shapes)
            s.Negate();
    }

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        IShape[] newShapes = new IShape[shapes.Length + 1];
        newShapes[0] = shape0.Clone(force);
        for (int i = 0; i < shapes.Length; i++)
            newShapes[i + 1] = shapes[i].Clone(force);
        return new Difference(newShapes, material.Clone(force), false);
    }

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Simplify()
    {
        shape0 = shape0.Simplify();
        if (shapes.Length == 0)
            return shape0;
        Bounds b = shape0.Bounds;
        int total = 0;
        for (int i = 0; i < shapes.Length; i++)
        {
            IShape s = shapes[i].Simplify();
            shapes[i] = s;
            if (!(b * s.Bounds).IsEmpty)
                shapes[total++] = s;
        }
        switch (total)
        {
            case 0:
                return shape0;
            case 1:
                {
                    IShape s = shapes[0];
                    return shape0.MaxHits == 2 && s.MaxHits == 2
                        ? new Diff2Convex(shape0, s, material)
                        : new Diff2(shape0, s, material);
                }
            default:
                if (total < shapes.Length)
                    Array.Resize(ref shapes, total);
                return this;
        }
    }

    /// <summary>Second optimization pass, where special shapes are introduced.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Substitute()
    {
        shape0 = shape0.Substitute();
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
        shape0.Initialize(scene, true, inTransform);
        foreach (IShape s in shapes)
            s.Initialize(scene, true, inTransform);
    }

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation)
    {
        int problems = 0;
        if (!shape0.CheckRotation(rotation, ref problems))
            return TransformationCost.Nope;
        foreach (IShape shape in shapes)
            if (!shape.CheckRotation(rotation, ref problems))
                return TransformationCost.Nope;
        return problems <= 1 ? TransformationCost.Ok : TransformationCost.Nope;
    }

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor)
    {
        int problems = 0;
        if (!shape0.CheckScale(factor, ref problems))
            return TransformationCost.Nope;
        foreach (IShape shape in shapes)
            if (!shape.CheckScale(factor, ref problems))
                return TransformationCost.Nope;
        return problems <= 1 ? TransformationCost.Ok : TransformationCost.Nope;
    }

    /// <summary>Translates this difference.</summary>
    /// <param name="translation">Translation distance.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        shape0.ApplyTranslation(translation);
        foreach (IShape s in shapes)
            s.ApplyTranslation(translation);
        bounds = shape0.Bounds;
    }

    /// <summary>Rotates this difference.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        DoRotation(ref shape0, rotation);
        for (int i = 0; i < shapes.Length; i++)
            DoRotation(ref shapes[i], rotation);
        bounds = shape0.Bounds;
    }

    /// <summary>Scales this difference.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        DoScale(ref shape0, factor);
        for (int i = 0; i < shapes.Length; i++)
            DoScale(ref shapes[i], factor);
        bounds = shape0.Bounds;
    }

    #endregion
}

/// <summary>Common base class for binary difference operations.</summary>
internal abstract class BinaryDiff : MaterialShape
{
    protected IShape shape0, shape1;

    /// <summary>Initializes a binary difference.</summary>
    /// <param name="shape0">First shape in difference.</param>
    /// <param name="shape1">Shape to be subtracted.</param>
    /// <param name="material">Material the difference is made of.</param>
    protected BinaryDiff(IShape shape0, IShape shape1, IMaterial material)
        : base(material)
    {
        this.shape0 = shape0;
        this.shape1 = shape1;
        bounds = shape0.Bounds;
    }

    #region IShape members

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <param name="normal">Normal vector at that point.</param>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
    public Vector GetNormal(in Vector location) =>
        throw new RenderException(Rsc.NotImplemented, "Difference.GetNormal");

    #endregion

    #region ITransformable members.

    public int MaxHits => shape0.MaxHits + shape1.MaxHits;

    /// <summary>Gets the estimated complexity.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public ShapeCost Cost => ShapeCost.PainInTheNeck;

    /// <summary>Invert normals for the right operand in a difference.</summary>
    public void Negate() { shape0.Negate(); shape1.Negate(); }

    /// <summary>Second optimization pass, where special shapes are introduced.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Substitute()
    {
        shape0 = shape0.Substitute();
        shape1 = shape1.Substitute();
        return (IShape)this;
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
        if (shape0.CheckRotation(rotation, ref problems) &&
            shape1.CheckRotation(rotation, ref problems))
            if (problems <= 1)
                return TransformationCost.Ok;
        return TransformationCost.Nope;
    }

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor)
    {
        int problems = 0;
        if (shape0.CheckScale(factor, ref problems) &&
            shape1.CheckScale(factor, ref problems))
            if (problems <= 1)
                return TransformationCost.Ok;
        return TransformationCost.Nope;
    }

    /// <summary>Translates this difference.</summary>
    /// <param name="translation">Translation distance.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        shape0.ApplyTranslation(translation);
        shape1.ApplyTranslation(translation);
        bounds = shape0.Bounds;
    }

    /// <summary>Rotates this difference.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        DoRotation(ref shape0, rotation);
        DoRotation(ref shape1, rotation);
        bounds = shape0.Bounds;
    }

    /// <summary>Scales this difference.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        DoScale(ref shape0, factor);
        DoScale(ref shape1, factor);
        bounds = shape0.Bounds;
    }

    #endregion
}

/// <summary>Specialized difference between two arbitrary shapes.</summary>
[Children(nameof(shape0), nameof(shape1))]
internal sealed class Diff2(IShape shape0, IShape shape1, IMaterial material)
    : BinaryDiff(shape0, shape1, material), IShape
{
    private readonly Hit[] hits0 = new Hit[shape0.MaxHits];
    private readonly Hit[] hits1 = new Hit[shape1.MaxHits];

    public Diff2(IShape shape0, IShape shape1)
        : this(shape0, shape1, ((MaterialShape)shape0).Material) { }

    private double FindRoot(Ray ray, ref IShape obj)
    {
        int total0 = shape0.GetHits(ray, hits0);
        if (total0 == 0)
            return -1.0;
        int total1 = shape1.GetHits(ray, hits1);
        if (total1 == 0)
        {
            for (int i = 0; i < total0; i++)
                if (hits0[i].Time > Tolerance.Epsilon)
                {
                    obj = hits0[i].Shape;
                    return hits0[i].Time;
                }
            return -1.0;
        }
        bool inside0 = false, inside1 = false;
        int i0 = 0, i1 = 0;
    LOOP:
        if (hits0[i0].Time <= hits1[i1].Time)
        {
            if (!inside1)
            {
                Hit hit = hits0[i0];
                if (hit.Time > Tolerance.Epsilon)
                {
                    obj = hit.Shape;
                    return hit.Time;
                }
            }
            inside0 = !inside0;
            if (++i0 < total0) goto LOOP;
        }
        else
        {
            if (inside0)
            {
                Hit hit = hits1[i1];
                if (hit.Time > Tolerance.Epsilon)
                {
                    obj = hit.Shape;
                    return hit.Time;
                }
            }
            inside1 = !inside1;
            if (++i1 < total1) goto LOOP;
            do
            {
                Hit hit = hits0[i0];
                if (hit.Time > Tolerance.Epsilon)
                {
                    obj = hit.Shape;
                    return hit.Time;
                }
            }
            while (++i0 < total0);
        }
        return -1.0;
    }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        IShape obj = null;
        double t = FindRoot(ray, ref obj);
        return 0.0 < t && t <= 1.0;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        IShape obj = null;
        double t = FindRoot(ray, ref obj);
        if (0.0 < t && t <= maxt)
        {
            info.Time = t;
            info.HitPoint = ray[t];
            info.Normal = obj.GetNormal(info.HitPoint);
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
            return 0;
        int total1 = shape1.GetHits(ray, hits1);
        if (total1 == 0)
        {
            Array.Copy(hits0, hits, total0);
            return total0;
        }
        bool inside0 = false, inside1 = false;
        int i0 = 0, i1 = 0, total = 0;
    LOOP:
        if (hits0[i0].Time <= hits1[i1].Time)
        {
            if (!inside1)
                hits[total++] = hits0[i0];
            inside0 = !inside0;
            if (++i0 < total0) goto LOOP;
        }
        else
        {
            if (inside0)
                hits[total++] = hits1[i1];
            inside1 = !inside1;
            if (++i1 < total1) goto LOOP;
            do
                hits[total++] = hits0[i0];
            while (++i0 < total0);
        }
        return total;
    }

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force) => 
        new Diff2(shape0.Clone(force), shape1.Clone(force), material.Clone(force));

    #endregion
}

/// <summary>Specialized difference between two convex shapes.</summary>
[Children(nameof(shape0), nameof(shape1))]
internal sealed class Diff2Convex(IShape shape0, IShape shape1, IMaterial material)
    : BinaryDiff(shape0, shape1, material), IShape
{
    private readonly Hit[] hits = new Hit[2];

    public Diff2Convex(IShape shape0, IShape shape1)
        : this(shape0, shape1, ((MaterialShape)shape0).Material) { }

    /// <summary>Finds the first positive intersection with a ray.</summary>
    /// <param name="ray">Ray to check.</param>
    /// <param name="obj">Shape hit by the ray.</param>
    /// <returns>The intersection time, or a negative number, if fails.</returns>
    private double FindRoot(Ray ray, ref IShape shape)
    {
        Hit h11 = hits[1], h10 = hits[0];
        if (shape1.GetHits(ray, hits) > 0)
        {
            Hit h21 = hits[1], h20 = hits[0];
            if (h20.Time <= h10.Time)
            {
                if (h21.Time >= h11.Time)
                    return -1.0;
                if (h21.Time >= h10.Time)
                    h10 = h21;
            }
            else if (h20.Time <= h11.Time)
                if (h21.Time >= h11.Time)
                    h11 = h20;
                else
                {
                    // Order: h10, h20, h21, h11
                    if (h10.Time >= Tolerance.Epsilon)
                    {
                        shape = h10.Shape;
                        return h10.Time;
                    }
                    if (h20.Time >= Tolerance.Epsilon)
                    {
                        shape = h20.Shape;
                        return h20.Time;
                    }
                    if (h21.Time >= Tolerance.Epsilon)
                    {
                        shape = h21.Shape;
                        return h21.Time;
                    }
                    if (h11.Time >= Tolerance.Epsilon)
                    {
                        shape = h11.Shape;
                        return h11.Time;
                    }
                    return -1.0;
                }
        }
        if (h10.Time >= Tolerance.Epsilon)
        {
            shape = h10.Shape;
            return h10.Time;
        }
        if (h11.Time >= Tolerance.Epsilon)
        {
            shape = h11.Shape;
            return h11.Time;
        }
        return -1.0;
    }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        IShape obj = null;
        if (shape0.GetHits(ray, hits) == 0)
            return false;
        double t = FindRoot(ray, ref obj);
        return 0 <= t && t <= 1.0;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        IShape obj = null;
        if (shape0.GetHits(ray, hits) == 0)
            return false;
        double t = FindRoot(ray, ref obj);
        if (0.0 <= t && t <= maxt)
        {
            info.Time = t;
            info.HitPoint = ray[t];
            info.Normal = obj.GetNormal(info.HitPoint);
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
        if (shape0.GetHits(ray, hits) == 0)
            return 0;
        Hit h11 = hits[1], h10 = hits[0];
        if (shape1.GetHits(ray, hits) > 0)
        {
            Hit h21 = hits[1], h20 = hits[0];
            if (h20.Time <= h10.Time)
            {
                if (h21.Time >= h11.Time)
                    return 0;
                if (h21.Time >= h10.Time)
                    h10 = h21;
            }
            else if (h20.Time <= h11.Time)
                if (h21.Time >= h11.Time)
                    h11 = h20;
                else
                {
                    hits[3] = h11;
                    hits[2] = h21;
                    hits[1] = h20;
                    hits[0] = h10;
                    return 4;
                }
        }
        hits[1] = h11;
        hits[0] = h10;
        return 2;
    }

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force) => new Diff2Convex(
        shape0.Clone(force), shape1.Clone(force), material.Clone(force));

    #endregion
}

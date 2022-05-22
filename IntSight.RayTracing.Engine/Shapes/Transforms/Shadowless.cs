namespace IntSight.RayTracing.Engine;

/// <summary>Groups objects that should not cast any shadows.</summary>
[XSight, Children(nameof(original))]
public sealed class Shadowless : Shape, IShape
{
    /// <summary>Shape to render without shadows.</summary>
    private IShape original;

    /// <summary>Creates a shadowless group.</summary>
    /// <param name="original">Root shape of the group.</param>
    public Shadowless(IShape original)
    {
        this.original = original;
        bounds = original.Bounds;
    }

    #region IShape members

    /// <summary>Computes an intersection between the shape and the ray.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>Always returns false.</returns>
    bool IShape.ShadowTest(Ray ray) => false;

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>The answer to the test, delegated to the original shape.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info) =>
        original.HitTest(ray, maxt, ref info);

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location) => original.GetNormal(location);

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits) => original.GetHits(ray, hits);

    #endregion

    #region ITransformable members

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    int ITransformable.MaxHits => original.MaxHits;

    /// <summary>Estimated complexity.</summary>
    ShapeCost ITransformable.Cost => ShapeCost.EasyPeasy;

    /// <summary>Invert normals for the right operand in a difference.</summary>
    void ITransformable.Negate() => original.Negate();

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        IShape s = original.Clone(force);
        return force || s != original ? new Shadowless(s) : (this);
    }

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Simplify()
    {
        original = original.Simplify();
        return this;
    }

    /// <summary>Second optimization pass, where special shapes are introduced.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Substitute()
    {
        original = original.Substitute();
        return this;
    }

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation) => original.CanRotate(rotation);

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor) => original.CanScale(factor);

    /// <summary>Translates this shape.</summary>
    /// <param name="translation">Translation amount.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        original.ApplyTranslation(translation);
        bounds = original.Bounds;
    }

    /// <summary>Rotates this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        original.ApplyRotation(rotation);
        bounds = original.Bounds;
    }

    /// <summary>Scales this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        original.ApplyScale(factor);
        bounds = original.Bounds;
    }

    #endregion
}
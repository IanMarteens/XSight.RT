using System.Diagnostics.CodeAnalysis;

namespace IntSight.RayTracing.Engine;

/// <summary>Common base class for all transformations.</summary>
public abstract class TransformBase : Shape, ITransform
{
    /// <summary>The transformed ray.</summary>
    protected readonly Ray testRay = new();
    /// <summary>Shape to be transformed.</summary>
    protected IShape original;

    /// <summary>Initializes the transformation.</summary>
    /// <param name="original">Original shape.</param>
    protected TransformBase(IShape original) => this.original = original;

    /// <summary>Gets the original shape.</summary>
    public IShape Original => original;

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    public virtual int MaxHits => original.MaxHits;

    /// <summary>Changes the material this shape is made of.</summary>
    /// <param name="newMaterial">The new material definition.</param>
    public override void ChangeDress(IMaterial newMaterial) => original.ChangeDress(newMaterial);

    /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
    /// <param name="scene">The scene this shape is included within.</param>
    /// <param name="inCsg">Is this shape included in a CSG operation?</param>
    /// <param name="inTransform">Is this shape nested inside a transform?</param>
    public override void Initialize(IScene scene, bool inCsg, bool inTransform) =>
        // Nested shapes are affected by this outer transform.
        // They cannot be used, for instance, to pick a global light ocludder.
        original.Initialize(scene, inCsg, true);

    /// <summary>Estimated cost.</summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public ShapeCost Cost => ShapeCost.PainInTheNeck;

    /// <summary>Invert normals for the right operand in a difference.</summary>
    public void Negate() => original.Negate();

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Simplify()
    {
        original = original.Simplify();
        return (IShape)this;
    }

    /// <summary>Second optimization pass, where special shapes are introduced.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Substitute()
    {
        original = original.Substitute();
        return (IShape)this;
    }
}

/// <summary>Euclidean translations.</summary>
/// <remarks>Translations are always removed by the scene optimizer.</remarks>
[XSight, Properties(nameof(translation)), Children(nameof(original))]
public sealed class Translate : TransformBase, IShape
{
    /// <summary>The translation distance.</summary>
    private Vector translation;

    public Translate(Vector translation, IShape original)
        : base(original)
    {
        this.translation = translation;
        bounds = original.Bounds + translation;
    }

    public Translate(IShape original, Vector translation)
        : this(translation, original) { }

    public Translate(double x, double y, double z, IShape original)
        : this(new Vector(x, y, z), original) { }

    public Translate(IShape original, double x, double y, double z)
        : this(new Vector(x, y, z), original) { }

    #region IShape members.

    /// <summary>
    /// Checks whether there's an intersection between the shape and the ray.
    /// </summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        testRay.Origin = ray.Origin - translation;
        testRay.SetDirection(ray);
        testRay.SquaredDir = ray.SquaredDir;
        return original.ShadowTest(testRay);
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        testRay.Origin = ray.Origin - translation;
        testRay.SetDirection(ray);
        return original.HitTest(testRay, maxt, ref info);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        testRay.Origin = ray.Origin - translation;
        testRay.SetDirection(ray);
        int result = original.GetHits(testRay, hits);
        for (int i = 0; i < result; i++)
            hits[i].Shape = this;
        return result;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location) =>
        original.GetNormal(location - translation);

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force) => new Translate(original.Clone(force), translation);

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Simplify()
    {
        original = original.Simplify();
        original.ApplyTranslation(translation);
        return original;
    }

    /// <summary>Combines two translations.</summary>
    /// <param name="translation">Distance of the new translation.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        this.translation += translation;
        bounds += translation;
    }

    #endregion
}

/// <summary>Euclidean rotations.</summary>
[XSight, Properties(nameof(Rotation), nameof(Angles)), Children(nameof(original))]
public sealed class Rotate : TransformBase, IShape
{
    /// <summary>The inverse of the <see cref="Rotation"/> matrix.</summary>
    private Matrix inverse;

    public Rotate(IShape original, Matrix rotation)
        : base(original)
    {
        Rotation = rotation;
        inverse = rotation.Transpose();
        RecomputeBounds();
    }

    public Rotate(IShape original, Vector angles)
        : this(original, Matrix.Rotation(angles.X, angles.Y, angles.Z)) { }

    public Rotate(Vector angles, IShape original)
        : this(original, Matrix.Rotation(angles.X, angles.Y, angles.Z)) { }

    public Rotate(IShape original, double x, double y, double z)
        : this(original, Matrix.Rotation(x, y, z)) { }

    public Rotate(double x, double y, double z, IShape original)
        : this(original, Matrix.Rotation(x, y, z)) { }

    public Matrix Rotation { get; private set; }

    public Vector Angles => Rotation.GetRotations() * (180.0 / Math.PI);

    /// <summary>Updates the bounding box and the bounding sphere around the shape.</summary>
    private void RecomputeBounds() =>
        // We have two techniques for computing bounds for a rotation:
        // rotate the original bounds and rotating the bounding sphere.
        // We apply both and find the intersection!
        bounds =
            original.Bounds.Rotate(Rotation) *
            Bounds.FromSphere(
                Rotation * original.Centroid,
                Math.Sqrt(original.SquaredRadius));

    #region IShape members.

    /// <summary>The center of the bounding sphere.</summary>
    public override Vector Centroid => Rotation * original.Centroid;

    /// <summary>The square radius of the bounding sphere.</summary>
    public override double SquaredRadius => original.SquaredRadius;

    /// <summary>
    /// Checks whether there's an intersection between the shape and the ray.
    /// </summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        testRay.Origin = inverse * ray.Origin;
        testRay.Direction = inverse * ray.Direction;
        testRay.SquaredDir = ray.SquaredDir;
        return original.ShadowTest(testRay);
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        testRay.Origin = inverse * ray.Origin;
        testRay.Direction = inverse * ray.Direction;
        if (original.HitTest(testRay, maxt, ref info))
        {
            info.Normal = Rotation * info.Normal;
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
        testRay.Origin = inverse * ray.Origin;
        testRay.Direction = inverse * ray.Direction;
        int result = original.GetHits(testRay, hits);
        for (int i = 0; i < result; i++)
            hits[i].Shape = this;
        return result;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location) =>
        Rotation * original.GetNormal(inverse * location);

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force) => new Rotate(original.Clone(force), Rotation);

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Simplify()
    {
        original = original.Simplify();
        if (Rotation.IsIdentity)
            return original;
        else if (original.CanRotate(Rotation) != TransformationCost.Nope)
        {
            original.ApplyRotation(Rotation);
            return original;
        }
        else
            return original is Scale s ?
                new Transform(s.Original, s.Factor, Rotation) : (IShape)this;
    }

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation) => TransformationCost.Ok;

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor)
    {
        TransformationCost cost = original.CanScale(factor);
        return cost != TransformationCost.Nope && Rotation.CanScale(factor) ?
            cost : TransformationCost.Nope;
    }

    /// <summary>Translates this shape.</summary>
    /// <param name="translation">Translation amount.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        original.ApplyTranslation(inverse * translation);
        RecomputeBounds();
    }

    /// <summary>Rotates this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        Rotation = rotation * Rotation;
        inverse = Rotation.Transpose();
        RecomputeBounds();
    }

    /// <summary>Scales this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        original.ApplyScale(factor);
        RecomputeBounds();
    }

    #endregion
}

/// <summary>Scale changes.</summary>
[XSight, Properties(nameof(Factor)), Children(nameof(original))]
public sealed class Scale : TransformBase, IShape
{
    /// <summary>The reciprocal of the scale factor.</summary>
    private Vector invScale;

    /// <summary>Creates a scale change.</summary>
    /// <param name="scale">Scale factor.</param>
    /// <param name="original">The original shape.</param>
    public Scale(Vector scale, IShape original)
        : base(original)
    {
        Factor = scale;
        bounds = original.Bounds.Scale(scale);
        invScale = scale.Invert();
    }

    /// <summary>Creates a scale change.</summary>
    /// <param name="original">The original shape.</param>
    /// <param name="scale">Scale factor.</param>
    public Scale(IShape original, Vector scale)
        : this(scale, original) { }

    public Scale(double x, double y, double z, IShape original)
        : this(new Vector(x, y, z), original) { }

    public Scale(IShape original, double x, double y, double z)
        : this(new Vector(x, y, z), original) { }

    /// <summary>Creates a scale change.</summary>
    /// <param name="scale">Scale factor.</param>
    /// <param name="original">The original shape.</param>
    public Scale(double factor, IShape original)
        : this(new Vector(factor), original) { }

    /// <summary>Creates a scale change.</summary>
    /// <param name="original">The original shape.</param>
    /// <param name="scale">Scale factor.</param>
    public Scale(IShape original, double factor)
        : this(new Vector(factor), original) { }

    /// <summary>Gets the scale factor.</summary>
    public Vector Factor { get; private set; }

    #region IShape members.

    /// <summary>The center of the bounding sphere.</summary>
    public override Vector Centroid => original.Centroid.Scale(Factor);

    /// <summary>The square radius of the bounding sphere.</summary>
    public override double SquaredRadius
    {
        get
        {
            double max = Math.Max(Math.Max(Factor.X, Factor.Y), Factor.Z);
            return original.SquaredRadius * max * max;
        }
    }

    /// <summary>
    /// Checks whether there's an intersection between the shape and the ray.
    /// </summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        testRay.Origin = ray.Origin.Scale(invScale);
        testRay.Direction = ray.Direction.Scale(invScale);
        testRay.SquaredDir = testRay.Direction.Squared;
        return original.ShadowTest(testRay);
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        testRay.Origin = ray.Origin.Scale(invScale);
        testRay.Direction = ray.Direction.ScaleNormal(invScale, out double len);
        if (original.HitTest(testRay, maxt / len, ref info))
        {
            info.Time *= len;
            info.Normal = info.Normal.ScaleNormal(invScale);
            return true;
        }
        else
            return false;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location) =>
        original.GetNormal(location.Scale(invScale)).ScaleNormal(invScale);

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        testRay.Origin = ray.Origin.Scale(invScale);
        testRay.Direction = ray.Direction.ScaleNormal(invScale, out double len);
        int result = original.GetHits(testRay, hits);
        for (int i = 0; i < result; i++)
        {
            hits[i].Time *= len;
            hits[i].Shape = this;
        }
        return result;
    }

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force) => new Scale(original.Clone(force), Factor);

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Simplify()
    {
        original = original.Simplify();
        if (Factor.Distance(new Vector(1.0)) <= Tolerance.Epsilon)
            return original;
        else if (original.CanScale(Factor) != TransformationCost.Nope)
        {
            original.ApplyScale(Factor);
            return original;
        }
        else
            return original is Rotate r ?
                new Transform(r.Original, r.Rotation, Factor) : (IShape)this;
    }

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation)
    {
        TransformationCost cost = original.CanRotate(rotation);
        return cost != TransformationCost.Nope && rotation.CanScale(Factor) ?
            cost : TransformationCost.Nope;
    }

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor) => TransformationCost.Ok;

    /// <summary>Translates this shape.</summary>
    /// <param name="translation">Translation amount.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        original.ApplyTranslation(translation.Scale(invScale));
        bounds = original.Bounds.Scale(Factor);
    }

    /// <summary>Rotates this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        original.ApplyRotation(rotation);
        bounds = original.Bounds.Scale(Factor);
    }

    /// <summary>Scales this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        Factor = Factor.Scale(factor);
        bounds = original.Bounds.Scale(Factor);
        invScale = Factor.Invert();
    }

    #endregion
}

/// <summary>Combines a scale change with a rotation, and viceversa.</summary>
[Properties(nameof(Steps)), Children(nameof(original))]
internal sealed class Transform : TransformBase, IShape
{
    private Matrix inverseTransform, normalTransform;
    private Vector centroid;
    private double squaredRadius;

    /// <summary>Creates a general transformation.</summary>
    /// <param name="original">Original shape.</param>
    /// <param name="rotation">Rotation matrix.</param>
    /// <param name="scale">Scale factor.</param>
    public Transform(IShape original, Matrix rotation, Vector scale)
        : base(original)
    {
        bounds = original.Bounds;
        centroid = original.Centroid;
        squaredRadius = original.SquaredRadius;
        Steps = string.Empty;
        inverseTransform = Matrix.Identity;
        normalTransform = Matrix.Identity;
        ApplyRotation(rotation);
        ApplyScale(scale);
    }

    /// <summary>Creates a general transformation.</summary>
    /// <param name="original">Original shape.</param>
    /// <param name="scale">Scale factor.</param>
    /// <param name="rotation">Rotation matrix.</param>
    public Transform(IShape original, Vector scale, Matrix rotation)
        : base(original)
    {
        bounds = original.Bounds;
        centroid = original.Centroid;
        squaredRadius = original.SquaredRadius;
        Steps = string.Empty;
        inverseTransform = Matrix.Identity;
        normalTransform = Matrix.Identity;
        ApplyScale(scale);
        ApplyRotation(rotation);
    }

    /// <summary>Creates a transformation by cloning an existing one.</summary>
    /// <param name="other">Transformation to clone.</param>
    /// <param name="force">Do we need an independent original shape?</param>
    private Transform(Transform other, bool force)
        : base(other.original.Clone(force))
    {
        bounds = other.bounds;
        centroid = other.centroid;
        squaredRadius = other.squaredRadius;
        Steps = other.Steps;
        inverseTransform = other.inverseTransform;
        normalTransform = other.normalTransform;
    }

    /// <summary>Gets the transformation sequence.</summary>
    private string Steps { get; set; }

    #region IShape members

    /// <summary>The center of the bounding sphere.</summary>
    public override Vector Centroid => centroid;

    /// <summary>The square radius of the bounding sphere.</summary>
    public override double SquaredRadius => squaredRadius;

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        testRay.Origin = inverseTransform * ray.Origin;
        testRay.Direction = inverseTransform * ray.Direction;
        testRay.SquaredDir = testRay.Direction.Squared;
        return original.ShadowTest(testRay);
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        testRay.Origin = inverseTransform * ray.Origin;
        testRay.Direction = inverseTransform.RotateNorm(ray.Direction, out double len);
        if (original.HitTest(testRay, maxt / len, ref info))
        {
            info.Time *= len;
            info.Normal = normalTransform.RotateNorm(
                info.Normal.X, info.Normal.Y, info.Normal.Z);
            return true;
        }
        else
            return false;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        Vector normal = original.GetNormal(inverseTransform * location);
        return normalTransform.RotateNorm(normal.X, normal.Y, normal.Z);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        testRay.Origin = inverseTransform * ray.Origin;
        testRay.Direction = inverseTransform.RotateNorm(ray.Direction, out double len);
        int result = original.GetHits(testRay, hits);
        for (int i = 0; i < result; i++)
        {
            hits[i].Time *= len;
            hits[i].Shape = this;
        }
        return result;
    }

    #endregion

    #region ITransformable members

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force) => new Transform(this, force);

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation) => TransformationCost.Ok;

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor) => TransformationCost.Ok;

    /// <summary>Translates this shape.</summary>
    /// <param name="translation">Translation amount.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        original.ApplyTranslation(inverseTransform * translation);
        bounds += translation;
        centroid += centroid;
    }

    /// <summary>Rotates this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        inverseTransform *= rotation.Transpose();
        normalTransform = rotation * normalTransform;
        bounds = bounds.Rotate(rotation);
        centroid = rotation * centroid;
        bounds *= Bounds.FromSphere(centroid, Math.Sqrt(squaredRadius));
        Steps += "R";
    }

    /// <summary>Scales this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        Matrix scale = new Matrix(1.0 / factor.X, 1.0 / factor.Y, 1.0 / factor.Z);
        inverseTransform *= scale;
        normalTransform = scale * normalTransform;
        bounds = bounds.Scale(factor);
        centroid = centroid.Scale(factor);
        double f = Math.Max(factor.X, Math.Max(factor.Y, factor.Z));
        squaredRadius *= f * f;
        Steps += "S";
    }

    #endregion
}
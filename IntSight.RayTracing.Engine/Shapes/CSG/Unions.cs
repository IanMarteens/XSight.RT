using System.Linq;
using System.Threading.Tasks;
using Rsc = IntSight.RayTracing.Engine.Properties.Resources;

namespace IntSight.RayTracing.Engine;

/// <summary>Common base class for all unions.</summary>
public abstract class UnionBase : Shape, IUnion
{
    /// <summary>State of the bounds checking algorithm.</summary>
    [Flags]
    protected enum UnionCheckState : byte
    {
        /// <summary>Neutral value required by the implementation of flags.</summary>
        None = 0,
        /// <summary>Bounds checking can be turned on and off.</summary>
        CheckModifiable = 1,
        /// <summary>Bounds checking has been turned off explicitly.</summary>
        ForcedCheckingOff = 2,
        /// <summary>Has the children nodes being already simplified?</summary>
        ChildrenSimplified = 4
    }

    /// <summary>List of shapes in the union.</summary>
    protected IShape[] shapes;
    /// <summary>Location of the centroid for a bounding sphere.</summary>
    protected Vector centroid;
    /// <summary>Squared radius of the bounding sphere.</summary>
    protected double squaredRadius;
    protected double effectivity;
    /// <summary>Are we checking bounds for this union?</summary>
    protected bool checkBounds;
    /// <summary>Is the union nested inside an outer transform?</summary>
    /// <remarks>This field is assigned by <see cref="Initialize"/>.</remarks>
    protected bool inTransform;
    /// <summary>Bounds checking state.</summary>
    protected UnionCheckState checkState;

    /// <summary>Initializes a union given its list of shapes.</summary>
    /// <param name="shapes">List of shapes in the union.</param>
    /// <param name="reorder">True if items can be reordered.</param>
    /// <param name="isCheckModifiable">True if bounds checking can be turned off.</param>
    protected UnionBase(IShape[] shapes, bool reorder, bool isCheckModifiable)
    {
        this.shapes = shapes;
        if (isCheckModifiable)
            checkState |= UnionCheckState.CheckModifiable;
        if (reorder)
            for (int i = 0; i < shapes.Length; i++)
            {
                IShape s = shapes[i];
                if (s.Bounds.IsInfinite && reorder)
                {
                    int j = i + 1;
                    while (j < shapes.Length &&
                        shapes[j].Bounds.IsInfinite) j++;
                    if (j >= shapes.Length)
                        reorder = false;
                    else
                    {
                        shapes[i] = shapes[j];
                        shapes[j] = s;
                    }
                }
            }
        RecomputeBounds();
    }

    /// <summary>Updates direct references to shapes in the shape list.</summary>
    /// <remarks>
    /// Specialized unions with a fixed number of operands have direct references
    /// to shapes in the <see cref="shapes"/> array. This methods updates those 
    /// references and it must be called after any change in the shapes list.
    /// </remarks>
    protected virtual void Rebind() { }

    /// <summary>Updates the bounding box and sphere around the union.</summary>
    protected virtual void RecomputeBounds()
    {
        bounds = Bounds.Void;
        Vector center = new(), cd = new();
        squaredRadius = -1.0;
        double sr = -1.0;
        for (int i = 0; i < shapes.Length; i++)
        {
            IShape s = shapes[i];
            bounds += s.Bounds;
            (center, squaredRadius) = Merge(center, squaredRadius, s);
            (cd, sr) = Merge(cd, sr, shapes[shapes.Length - i - 1]);
        }
        if (sr < squaredRadius - Tolerance.Epsilon)
        {
            centroid = cd;
            squaredRadius = sr;
        }
        else
            centroid = center;
        // Try to enhance bounds intersecting with the bounding sphere.
        if (!bounds.IsUniverse)
            bounds *= Bounds.FromSphere(Centroid, Math.Sqrt(squaredRadius));
        effectivity = Math.Pow(squaredRadius, 1.5) / bounds.Volume;
        Rebind();
    }

    #region IUnion members.

    /// <summary>Is this union checking its bounds?</summary>
    bool IBoundsChecker.IsChecking
    {
        get => checkBounds;
        set
        {
            if ((checkState & UnionCheckState.CheckModifiable) != 0)
            {
                checkBounds = value;
                if (!value)
                    checkState |= UnionCheckState.ForcedCheckingOff;
            }
        }
    }

    /// <summary>Can we turn off bounds checking for this union?</summary>
    bool IBoundsChecker.IsCheckModifiable =>
        (checkState & UnionCheckState.CheckModifiable) != 0;

    /// <summary>Gets the list of shapes grouped inside this union.</summary>
    public IShape[] Shapes => shapes;

    #endregion

    /// <summary>Gets the center of the bounding sphere.</summary>
    public override Vector Centroid => centroid;

    /// <summary>Gets the squared radius of the bounding sphere.</summary>
    public override double SquaredRadius => squaredRadius;

    /// <summary>Gets the estimated cost of rendering the union.</summary>
    public virtual ShapeCost Cost => ShapeCost.PainInTheNeck;

    /// <summary>Invert normals for the right operand in a difference.</summary>
    public void Negate()
    {
        foreach (IShape s in shapes)
            s.Negate();
    }

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    public virtual int MaxHits => shapes.Sum(s => s.MaxHits);

    /// <summary>Gets the normal vector given a point in the surface.</summary>
    /// <param name="location">Hit point.</param>
    /// <returns>Normal vector.</returns>
    /// <remarks>
    /// As a rule, CSG operations do not implement IShape.GetNormal, since their
    /// IShape.GetHits implementations honors the original shape the hit point belongs.
    /// </remarks>
    public virtual Vector GetNormal(in Vector location) =>
        throw new RenderException(Rsc.NotImplemented, "Union.GetNormal");

    /// <summary>Second optimization pass, where special shapes are introduced.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Substitute()
    {
        for (int i = 0; i < shapes.Length; i++)
            shapes[i] = shapes[i].Substitute();
        RecomputeBounds();
        return (IShape)this;
    }

    /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
    /// <param name="scene">The scene this shape is included within.</param>
    /// <param name="inCsg">Is this shape included in a CSG operation?</param>
    /// <param name="inTransform">Is this shape nested inside a transform?</param>
    public override void Initialize(IScene scene, bool inCsg, bool inTransform)
    {
        this.inTransform = inTransform;
        foreach (IShape s in shapes)
            s.Initialize(scene, inCsg, inTransform);
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

    /// <summary>Translate this shape.</summary>
    /// <param name="translation">Translation amount.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        foreach (IShape s in shapes)
            s.ApplyTranslation(translation);
        bounds += translation;
        centroid += translation;
    }

    /// <summary>Rotate this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        for (int i = 0; i < shapes.Length; i++)
            DoRotation(ref shapes[i], rotation);
        RecomputeBounds();
    }

    /// <summary>Scale this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        for (int i = 0; i < shapes.Length; i++)
            DoScale(ref shapes[i], factor);
        RecomputeBounds();
    }

    /// <summary>Changes the material this shape is made of.</summary>
    /// <param name="newMaterial">The new material definition.</param>
    public override void ChangeDress(IMaterial newMaterial)
    {
        foreach (IShape s in shapes)
            s.ChangeDress(newMaterial);
    }
}

/// <summary>The most general kind of union.</summary>
[XSight]
[Properties(nameof(checkBounds), nameof(squaredRadius), nameof(Centroid))]
[Children(nameof(shapes))]
public sealed class Union : UnionBase, IShape
{
    /// <summary>The list of shapes with an alternative order, for shadow testing.</summary>
    private IShape[] lightBuffer;

    /// <summary>Creates a union.</summary>
    /// <param name="shapes">The list of shapes grouped by the union.</param>
    public Union(params IShape[] shapes)
        : base(shapes, false, true) { }

    /// <summary>Updates the bounding box and the bounding sphere around the union.</summary>
    protected override void RecomputeBounds()
    {
        base.RecomputeBounds();
        if (bounds.IsInfinite)
            checkBounds = false;
        else if (shapes.Length >= Properties.Settings.Default.UnionThreshold)
        {
            if ((checkState & UnionCheckState.ForcedCheckingOff) == 0)
                checkBounds = true;
        }
        else
        {
            checkBounds = false;
            // We won't enforce bounds checking if it was explicitly turned off.
            if ((checkState & UnionCheckState.ForcedCheckingOff) == 0)
                foreach (IShape shape in shapes)
                    if (shape.Cost > ShapeCost.EasyPeasy)
                        checkBounds = true;
        }
    }

    #region IShape members.

    /// <summary>Find an intersection between union items and a ray.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        if (!checkBounds || bounds.Intersects(ray, 1))
            foreach (IShape shape in lightBuffer)
                if (shape.ShadowTest(ray))
                {
                    if (!inTransform)
                        BaseLight.lastOccluder ??= shape;
                    return true;
                }
        return false;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        if (!checkBounds || bounds.Intersects(ray, maxt))
            for (int i = 0; i < shapes.Length; i++)
                if (shapes[i].HitTest(ray, maxt, ref info))
                {
                    while (++i < shapes.Length)
                        shapes[i].HitTest(ray, info.Time, ref info);
                    return true;
                }
        return false;
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits) =>
        throw new RenderException(Rsc.ErrorInvalidShapeInCsg, "Unions");

    #endregion

    #region ITransformable members.

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
                return new Union(newShapes) { checkBounds = checkBounds };
            }
        }
        return this;
    }

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Simplify()
    {
        if (shapes.Length == 0)
            return this;
        if (shapes.Length == 1)
            return shapes[0].Simplify();
        int infiniteCount = 0, lastFiniteIndex = -1, realMessy = 0;
        if ((checkState & UnionCheckState.ChildrenSimplified) != 0)
        {
            // When a union contains infinite surfaces, it constructs a finite union
            // and call Simplify again on it. The above check avoids unneeded recursion.
            foreach (IShape shape in shapes)
                if (shape.Cost == ShapeCost.PainInTheNeck)
                    realMessy++;
        }
        else
        {
            Parallel.For(0, shapes.Length,
                i => shapes[i] = shapes[i].Simplify());
            for (int i = 0; i < shapes.Length; i++)
            {
                IShape shape = shapes[i];
                if (shape.Bounds.IsInfinite)
                {
                    infiniteCount++;
                    lastFiniteIndex = i;
                }
                else if (shape.Cost == ShapeCost.PainInTheNeck)
                    realMessy++;
            }
            checkState |= UnionCheckState.ChildrenSimplified;
        }
        // Infinite surfaces should be checked after finite solids.
        // When several finite solids are grouped together with infinite surfaces
        // it pays to group the finite elements in a different union.
        // The whole union can then be tested with a single bound check.
        if (infiniteCount > 0 && infiniteCount < shapes.Length && lastFiniteIndex >= 0)
            if (infiniteCount == shapes.Length - 1)
            {
                // There's only one finite solid, and we move it to the pole position.
                IShape temp = shapes[lastFiniteIndex];
                shapes[lastFiniteIndex] = shapes[0];
                shapes[0] = temp;
            }
            else
            {
                IShape[] infiniteGroup = new IShape[infiniteCount + 1];
                IShape[] finiteGroup = new IShape[shapes.Length - infiniteCount];
                int infiniteIdx = 1, finiteIdx = 0;
                foreach (IShape shape in shapes)
                    if (shape.Bounds.IsInfinite)
                        infiniteGroup[infiniteIdx++] = shape;
                    else
                        finiteGroup[finiteIdx++] = shape;
                infiniteGroup[0] = new Union(finiteGroup).Simplify();
                shapes = infiniteGroup;
            }
#if USE_SSE
    AGAIN:
#endif
        switch (shapes.Length)
        {
            case 2: return Simplify2();
            case 3: return Simplify3();
#if USE_SSE
            case 4:
                {
                    var pairs = shapes.OfType<Union2F>().Take(2).ToList();
                    if (pairs.Count == 2)
                    {
                        var u4 = new Union4F(pairs[0].Shapes.Concat(pairs[1].Shapes).ToArray());
                        // Create the new list of shapes.
                        var newShapes = shapes.Where(s => !pairs.Contains(s)).ToList();
                        newShapes.Add(u4);
                        shapes = newShapes.ToArray();
                        goto AGAIN;
                    }
                    return Simplify4();
                }
#endif
            default:
#if USE_SSE
                {
                    var candidates = shapes.Where(
                      s => !s.Bounds.IsInfinite && s.Cost > ShapeCost.EasyPeasy &&
                      (s is not IBoundsChecker cm || cm.IsCheckModifiable)).Take(4).ToList();
                    if (candidates.Count == 4)
                    {
                        // Create the new Union4F
                        foreach (var s in candidates.OfType<IBoundsChecker>())
                            s.IsChecking = false;
                        var u4 = new Union4F(candidates.ToArray());
                        // Create the new list of shapes.
                        var newShapes = shapes.Where(s => !candidates.Contains(s)).ToList();
                        newShapes.Add(u4);
                        shapes = newShapes.ToArray();
                        goto AGAIN;
                    }
                    if (candidates.Count == 2)
                    {
                        foreach (var s in candidates.OfType<IBoundsChecker>())
                            s.IsChecking = false;
                        var u2 = new Union2F(candidates.ToArray());
                        var newShapes = shapes.Where(s => !candidates.Contains(s)).ToList();
                        newShapes.Add(u2);
                        shapes = newShapes.ToArray();
                        goto AGAIN;
                    }
                }
                {
                    var pairs = shapes.OfType<Union2F>().Take(2).ToList();
                    if (pairs.Count == 2)
                    {
                        var u4 = new Union4F(pairs[0].Shapes.Concat(pairs[1].Shapes).ToArray());
                        // Create the new list of shapes.
                        var newShapes = shapes.Where(s => !pairs.Contains(s)).ToList();
                        newShapes.Add(u4);
                        shapes = newShapes.ToArray();
                        goto AGAIN;
                    }
                }
#endif
                if (shapes.Length < 16 && CanBindWithSphere())
                    return new SUnion(shapes);
                else if (infiniteCount == 0 &&
                    (shapes.Length >= 12 || shapes.Length >= 10 && realMessy >= 5))
                    return Split(Axis.Unknown);
                else
                    return this;
        }
    }

    private IShape Simplify(Axis sortedBy)
    {
        switch (shapes.Length)
        {
            case 0: return this;
            case 1: return shapes[0];
            case 2: return Simplify2();
            case 3: return Simplify3();
#if USE_SSE
            case 4: return Simplify4();
#endif
            case 9:
            case 10:
            case 11:
                if (CanBindWithSphere())
                    return new SUnion(shapes);
                else
                {
                    int realMessy = 0;
                    foreach (IShape shape in shapes)
                        if (shape.Cost == ShapeCost.PainInTheNeck)
                            realMessy++;
                    return realMessy >= 5 ? Split(sortedBy) : this;
                }
            default:
                if (shapes.Length < 16 && CanBindWithSphere())
                    return new SUnion(shapes);
                else
                    return shapes.Length >= 12 ? Split(sortedBy) : this;
        }
    }

    private IShape Simplify2()
    {
        IShape s1 = shapes[1], s0 = shapes[0];
        if (!s0.Bounds.IsInfinite && !s1.Bounds.IsInfinite &&
            s0.Cost > ShapeCost.EasyPeasy && s1.Cost > ShapeCost.EasyPeasy)
        {
            IBoundsChecker u0 = s0 as IBoundsChecker;
            IBoundsChecker u1 = s1 as IBoundsChecker;
            if ((u0 == null || u0.IsCheckModifiable) &&
                (u1 == null || u1.IsCheckModifiable))
            {
                if (u0 != null) u0.IsChecking = false;
                if (u1 != null) u1.IsChecking = false;
                return new Union2F(shapes);
            }
        }
        return CanBindWithSphere()
            ? new SUnion2(shapes) : new Union2(shapes);
    }

    private IShape Simplify3() =>
        CanBindWithSphere()
            ? new SUnion(shapes)
            : new Union3(shapes[0], shapes[1], shapes[2]);

#if USE_SSE

    private IShape Simplify4()
    {
        if (shapes.Any(
            s => s.Bounds.IsInfinite ||
                s.Cost == ShapeCost.EasyPeasy ||
                s is IBoundsChecker u && !u.IsCheckModifiable))
        {
            var candidates = shapes.Where(
              s => !s.Bounds.IsInfinite && s.Cost > ShapeCost.EasyPeasy &&
              (s is not IBoundsChecker cm || cm.IsCheckModifiable)).Take(2).ToList();
            if (candidates.Count == 2)
            {
                foreach (var s in candidates.OfType<IBoundsChecker>())
                    s.IsChecking = false;
                var u2 = new Union2F(candidates.ToArray());
                var newShapes = shapes.Where(s => !candidates.Contains(s)).ToList();
                newShapes.Add(u2);
                shapes = newShapes.ToArray();
            }
            return CanBindWithSphere() ? new SUnion(shapes) : this;
        }
        foreach (var s in shapes.OfType<IBoundsChecker>())
            s.IsChecking = false;
        return new Union4F(shapes);
    }

#endif

    /// <summary>Sorts shapes and divides the union using the surface heuristics.</summary>
    /// <param name="sortedBy">Axis used for splitting.</param>
    /// <returns>The new optimized union, or the original one, if not splitted.</returns>
    private IShape Split(Axis sortedBy)
    {
        Axis newSort = bounds.DominantAxis;
        if (newSort != sortedBy)
            switch (newSort)
            {
                case Axis.X:
                    Array.Sort(shapes, (x, y) => x.Bounds.CompareX(y.Bounds));
                    break;
                case Axis.Y:
                    Array.Sort(shapes, (x, y) => x.Bounds.CompareY(y.Bounds));
                    break;
                case Axis.Z:
                    Array.Sort(shapes, (x, y) => x.Bounds.CompareZ(y.Bounds));
                    break;
            }
        double[] areaRight = new double[shapes.Length];
        Bounds b = Bounds.Void;
        for (int i = shapes.Length - 1; i >= 0; i--)
        {
            b += shapes[i].Bounds;
            areaRight[i] = b.HalfArea;
        }
        int bestIdx = -1;
        double bestValue = double.MaxValue;
        b = shapes[0].Bounds;
        for (int i = 1; i < shapes.Length - 2; i++)
        {
            b += shapes[i].Bounds;
            double newValue = b.HalfArea * (i + 1) +
                areaRight[i + 1] * (shapes.Length - i - 1);
            if (newValue < bestValue)
            {
                bestValue = newValue;
                bestIdx = i;
            }
        }
        areaRight = null;
        if (bestIdx != -1)
        {
            IShape[] lshapes = new IShape[bestIdx + 1];
            Array.Copy(shapes, lshapes, bestIdx + 1);
            IShape[] rshapes = new IShape[shapes.Length - bestIdx - 1];
            Array.Copy(shapes, bestIdx + 1, rshapes, 0, shapes.Length - bestIdx - 1);
            return new Union(
                new Union(lshapes).Simplify(newSort),
                new Union(rshapes).Simplify(newSort)).Simplify(Axis.Unknown);
        }
        return this;
    }

    /// <summary>Can we enclose this union inside a bounding sphere?</summary>
    /// <returns>True if a bounding sphere makes any sense.</returns>
    private bool CanBindWithSphere()
    {
        double r2 = -1.0;
        Vector c = new();
        foreach (IShape s in shapes)
            (c, r2) = Merge(c, r2, s);
        double r = Math.Sqrt(r2);
        // Yes, no 4/3 PI involved! Heuristics, ya' know...
        return r2 * r < Properties.Settings.Default.BoundingSphereThreshold * bounds.Volume;
    }

    /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
    /// <param name="scene">The scene this shape is included within.</param>
    /// <param name="inCsg">Is this shape included in a CSG operation?</param>
    /// <param name="inTransform">Is this shape nested inside a transform?</param>
    /// <remarks>Children are sorted regarding their distance to the camera.</remarks>
    public override void Initialize(IScene scene, bool inCsg, bool inTransform)
    {
        Array.Sort(shapes, (s1, s2) => s1.CompareTo(s2, scene.Camera.Location));
        lightBuffer = scene.Lights.Length == 1 ? scene.Lights[0].Sort(shapes) : shapes;
        base.Initialize(scene, inCsg, inTransform);
    }

#endregion
}

/// <summary>Two shapes enclosed by an axis-aligned bounding box.</summary>
[Properties(nameof(checkBounds)), Children(nameof(shape0), nameof(shape1))]
internal sealed class Union2 : UnionBase, IShape
{
    private IShape shape0, shape1;
    private Union2 tail;
    private Hit[] hits0, hits1;

    /// <summary>Creates a binary union.</summary>
    /// <param name="shapes">The two involved shapes.</param>
    public Union2(IShape[] shapes) : base(shapes, true, true) { }

    /// <summary>Updates direct references to shapes in the shape list.</summary>
    protected override void Rebind() => (shape1, shape0) = (shapes[1], shapes[0]);

    /// <summary>Updates the bounding box and the bounding sphere around the union.</summary>
    protected override void RecomputeBounds()
    {
        base.RecomputeBounds();
        // We won't enforce bounds checking if it was explicitly turned off.
        checkBounds =
            (checkState & UnionCheckState.ForcedCheckingOff) == 0 &&
            !bounds.IsInfinite &&
            shape0.Cost > ShapeCost.EasyPeasy &&
            shape1.Cost > ShapeCost.EasyPeasy;
    }

#region IShape members.

    /// <summary>Find an intersection between union items and a ray.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        Union2 self = this;
    AGAIN:
        if (!self.checkBounds || self.bounds.Intersects(ray, 1))
            if (self.shape0.ShadowTest(ray))
            {
                if (!inTransform)
                    BaseLight.lastOccluder ??= self.shape0;
                return true;
            }
            else if (self.tail != null)
            {
                self = self.tail;
                goto AGAIN;
            }
            else if (self.shape1.ShadowTest(ray))
            {
                if (!inTransform)
                    BaseLight.lastOccluder ??= self.shape1;
                return true;
            }
        return false;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        Union2 self = this;
    AGAIN:
        if (self.checkBounds && !self.bounds.Intersects(ray, maxt))
            return false;
        if (self.shape0.HitTest(ray, maxt, ref info))
        {
            self.shape1.HitTest(ray, info.Time, ref info);
            return true;
        }
        if (self.tail != null)
        {
            self = self.tail;
            goto AGAIN;
        }
        return self.shape1.HitTest(ray, maxt, ref info);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        int total1 = shape0.GetHits(ray, hits0);
        if (total1 == 0)
            return shape1.GetHits(ray, hits);
        int total2 = shape1.GetHits(ray, hits1);
        if (total2 == 0)
        {
            Array.Copy(hits0, hits, total1);
            return total1;
        }
        int i1 = 0, i2 = 0, total = 0;
        bool inside1 = false, inside2 = false, inside = false;
        do
            if (i1 < total1 && (i2 >= total2 || hits0[i1].Time <= hits1[i2].Time))
            {
                bool newInside = (inside1 = !inside1) | inside2;
                if (inside != newInside)
                {
                    hits[total++] = hits0[i1];
                    inside = newInside;
                }
                i1++;
            }
            else
            {
                bool newInside = inside1 | (inside2 = !inside2);
                if (inside != newInside)
                {
                    hits[total++] = hits1[i2];
                    inside = newInside;
                }
                i2++;
            }
        while (i1 < total1 || i2 < total2);
        return total;
    }

#endregion

#region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        IShape s0 = shape0.Clone(force), s1 = shape1.Clone(force);
        return force || s0 != shape0 || s1 != shape1 || hits0 != null
            ? new Union2(new[] { s0, s1 }) { checkBounds = checkBounds }
            : this;
    }

    /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
    /// <param name="scene">The scene this shape is included within.</param>
    /// <param name="inCsg">Is this shape included in a CSG operation?</param>
    /// <param name="inTransform">Is this shape nested inside a transform?</param>
    /// <remarks>Children are sorted regarding their distance to the camera.</remarks>
    public override void Initialize(IScene scene, bool inCsg, bool inTransform)
    {
        Array.Sort(shapes, (s1, s2) => s1.CompareTo(s2, scene.Camera.Location));
        Rebind();
        if (inCsg)
            (hits0, hits1) = (new Hit[shape0.MaxHits], new Hit[shape1.MaxHits]);

        base.Initialize(scene, inCsg, inTransform);
        tail = shape1 as Union2;
    }

#endregion
}

/// <summary>A union containing three subitems.</summary>
[Properties(nameof(checkBounds))]
[Children(nameof(shape0), nameof(shape1), nameof(shape2))]
internal sealed class Union3 : UnionBase, IShape
{
    private IShape shape0, shape1, shape2;
    private Hit[] hits0, hits1;

    /// <summary>Creates a union with three shapes.</summary>
    /// <param name="shape0">First shape.</param>
    /// <param name="shape1">Second shape.</param>
    /// <param name="shape2">Third shape.</param>
    public Union3(IShape shape0, IShape shape1, IShape shape2)
        : base(new[] { shape0, shape1, shape2 }, true, true) { }

    /// <summary>Updates direct references to shapes in the shape list.</summary>
    protected override void Rebind() =>
        (shape2, shape1, shape0) = (shapes[2], shapes[1], shapes[0]);

    /// <summary>Updates the bounding box and the bounding sphere around this union.</summary>
    protected override void RecomputeBounds()
    {
        base.RecomputeBounds();
        // We won't enforce bounds checking if it was explicitly turned off.
        checkBounds =
            (checkState & UnionCheckState.ForcedCheckingOff) == 0 &&
            !bounds.IsInfinite;
    }

#region IShape members.

    /// <summary>Find an intersection between union items and a ray.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        if (!checkBounds || bounds.Intersects(ray, 1))
            if (shape0.ShadowTest(ray))
            {
                if (!inTransform)
                    BaseLight.lastOccluder ??= shape0;
                return true;
            }
            else if (shape1.ShadowTest(ray))
            {
                if (!inTransform)
                    BaseLight.lastOccluder ??= shape1;
                return true;
            }
            else if (shape2.ShadowTest(ray))
            {
                if (!inTransform)
                    BaseLight.lastOccluder ??= shape2;
                return true;
            }
        return false;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        if (checkBounds && !bounds.Intersects(ray, maxt))
            return false;
        if (shape0.HitTest(ray, maxt, ref info))
        {
            shape1.HitTest(ray, info.Time, ref info);
            shape2.HitTest(ray, info.Time, ref info);
            return true;
        }
        if (shape1.HitTest(ray, maxt, ref info))
        {
            shape2.HitTest(ray, info.Time, ref info);
            return true;
        }
        return shape2.HitTest(ray, maxt, ref info);
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        int t1 = shape0.GetHits(ray, hits);
        int t2 = shape1.GetHits(ray, hits1);
        if (t1 + t2 > 0)
            if (t1 == 0)
            {
                Array.Copy(hits1, hits0, t2);
                t1 = t2;
            }
            else if (t2 == 0)
                Array.Copy(hits, hits0, t1);
            else
            {
                int i1 = 0, i2 = 0, t = 0;
                bool inside1 = false, inside2 = false, inside = false;
                do
                    if (i1 < t1 && (i2 >= t2 || hits[i1].Time <= hits1[i2].Time))
                    {
                        if (inside != ((inside1 = !inside1) | inside2))
                        {
                            hits0[t++] = hits[i1];
                            inside = !inside;
                        }
                        i1++;
                    }
                    else
                    {
                        if (inside != (inside1 | (inside2 = !inside2)))
                        {
                            hits0[t++] = hits1[i2];
                            inside = !inside;
                        }
                        i2++;
                    }
                while (i1 < t1 || i2 < t2);
                t1 = t;
            }
        t2 = shape2.GetHits(ray, hits1);
        if (t1 == 0)
        {
            if (t2 == 0)
                return 0;
            Array.Copy(hits1, hits, t2);
            return t2;
        }
        if (t2 == 0)
        {
            Array.Copy(hits0, hits, t1);
            return t1;
        }
        int j1 = 0, j2 = 0, total = 0;
        bool in1 = false, in2 = false, intotal = false;
        do
            if (j1 < t1 && (j2 >= t2 || hits0[j1].Time <= hits1[j2].Time))
            {
                if (intotal != ((in1 = !in1) | in2))
                {
                    hits[total++] = hits0[j1];
                    intotal = !intotal;
                }
                j1++;
            }
            else
            {
                if (intotal != (in1 | (in2 = !in2)))
                {
                    hits[total++] = hits1[j2];
                    intotal = !intotal;
                }
                j2++;
            }
        while (j1 < t1 || j2 < t2);
        return total;
    }

#endregion

#region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        IShape s0 = shape0.Clone(force), s1 = shape1.Clone(force), s2 = shape2.Clone(force);
        return force || s0 != shape0 || s1 != shape1 || s2 != shape2 || hits0 != null
            ? new Union3(s0, s1, s2) { checkBounds = checkBounds } : this;
    }

    /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
    /// <param name="scene">The scene this shape is included within.</param>
    /// <param name="inCsg">Is this shape included in a CSG operation?</param>
    /// <param name="inTransform">Is this shape nested inside a transform?</param>
    /// <remarks>Children are sorted regarding their distance to the camera.</remarks>
    public override void Initialize(IScene scene, bool inCsg, bool inTransform)
    {
        Array.Sort(shapes, (s1, s2) => s1.CompareTo(s2, scene.Camera.Location));
        Rebind();
        if (inCsg)
        {
            hits0 = new Hit[shape0.MaxHits + shape1.MaxHits];
            hits1 = new Hit[Math.Max(shape1.MaxHits, shape2.MaxHits)];
        }
        base.Initialize(scene, inCsg, inTransform);
    }

#endregion
}
using Resources = IntSight.RayTracing.Engine.Properties.Resources;

namespace IntSight.RayTracing.Engine;

/// <summary>Creates virtual equispaced clones of another shape.</summary>
[XSight(Alias = "Repeat")]
[Properties(nameof(separable), nameof(count), nameof(delta))]
[Children(nameof(original))]
public sealed class Repeater : TransformBase, IShape, IBoundsChecker
{
    private readonly int count;
    /// <summary>The repeating distance.</summary>
    private Vector delta;
    /// <summary>Inverse repeating distances.</summary>
    private double idX, idY, idZ;
    private Vector w0, w1;
    private bool separable;

    public Repeater(int count, Vector delta, IShape original)
        : base(original)
    {
        this.count = count;
        this.delta = delta;
        (idX, idY, idZ) = delta.Invert();
        RecomputeBounds();
    }

    public Repeater(Vector delta, int count, IShape original)
        : this(count, delta, original) { }

    public Repeater(int count, double x, double y, double z, IShape original)
        : this(count, new Vector(x, y, z), original) { }

    /// <summary>Updates the bounding box and the bounding sphere around the shape.</summary>
    private void RecomputeBounds()
    {
        Bounds b = original.Bounds;
        bounds = b;
        int i = count;
        while (--i > 0)
        {
            b += delta;
            bounds += b;
        }
        b = original.Bounds;
        Vector w = b.To - b.From;
        w0 = bounds.From + w;
        w1 = bounds.To - w;
        b *= (b + delta);
        separable = b.IsEmpty || b.Volume == 0.0;
    }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        double tt0 = Tolerance.Epsilon, tt1 = 1.0;
        double dx = ray.Direction.X, org, temp;
        double t0 = (bounds.x0 - (org = ray.Origin.X)) * (temp = ray.InvDir.X);
        double t1 = (bounds.x1 - org) * temp;
        if (dx >= 0)
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
        double dy = ray.Direction.Y;
        t0 = (bounds.y0 - (org = ray.Origin.Y)) * (temp = ray.InvDir.Y);
        t1 = (bounds.y1 - org) * temp;
        if (dy >= 0)
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
        double dz = ray.Direction.Z;
        t0 = (bounds.z0 - (org = ray.Origin.Z)) * (temp = ray.InvDir.Z);
        t1 = (bounds.z1 - org) * temp;
        if (dz >= 0)
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
        int lo = 0, hi = count - 1, n;
        if (delta.X > 0.0)
            if (dx >= 0.0)
            {
                if ((t0 = ray.Origin.X + dx * tt0 - w0.X) > 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idX)) > lo) lo = n;
                n = (int)Math.Floor((ray.Origin.X + dx * tt1 - bounds.x0) * idX);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = ray.Origin.X + dx * tt1 - w0.X) > 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idX)) > lo) lo = n;
                n = (int)Math.Floor((ray.Origin.X + dx * tt0 - bounds.x0) * idX);
                if (n < hi) hi = n;
            }
        else if (delta.X < 0.0)
            if (dx <= 0)
            {
                if ((t0 = ray.Origin.X + dx * tt0 - w1.X) < 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idX)) > lo) lo = n;
                n = (int)Math.Floor((ray.Origin.X + dx * tt1 - bounds.x1) * idX);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = ray.Origin.X + dx * tt1 - w1.X) < 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idX)) > lo) lo = n;
                n = (int)Math.Floor((ray.Origin.X + dx * tt0 - bounds.x1) * idX);
                if (n < hi) hi = n;
            }
        if (delta.Y > 0.0)
            if (dy >= 0.0)
            {
                if ((t0 = ray.Origin.Y + dy * tt0 - w0.Y) > 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idY)) > lo) lo = n;
                n = (int)Math.Floor((ray.Origin.Y + dy * tt1 - bounds.y0) * idY);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = ray.Origin.Y + dy * tt1 - w0.Y) > 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idY)) > lo) lo = n;
                n = (int)Math.Floor((ray.Origin.Y + dy * tt0 - bounds.y0) * idY);
                if (n < hi) hi = n;
            }
        else if (delta.Y < 0.0)
            if (dy <= 0)
            {
                if ((t0 = ray.Origin.Y + dy * tt0 - w1.Y) < 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idY)) > lo) lo = n;
                n = (int)Math.Floor((ray.Origin.Y + dy * tt1 - bounds.y1) * idY);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = ray.Origin.Y + dy * tt1 - w1.Y) < 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idY)) > lo) lo = n;
                n = (int)Math.Floor((ray.Origin.Y + dy * tt0 - bounds.y1) * idY);
                if (n < hi) hi = n;
            }
        if (delta.Z > 0.0)
            if (dz >= 0.0)
            {
                if ((t0 = org + dz * tt0 - w0.Z) > 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idZ)) > lo) lo = n;
                n = (int)Math.Floor((org + dz * tt1 - bounds.z0) * idZ);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = org + dz * tt1 - w0.Z) > 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idZ)) > lo) lo = n;
                n = (int)Math.Floor((org + dz * tt0 - bounds.z0) * idZ);
                if (n < hi) hi = n;
            }
        else if (delta.Z < 0.0)
            if (dz <= 0.0)
            {
                if ((t0 = org + dz * tt0 - w1.Z) < 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idZ)) > lo) lo = n;
                n = (int)Math.Floor((ray.Origin.Z + dz * tt1 - bounds.z1) * idZ);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = org + dz * tt1 - w1.Z) < 0.0)
                    if ((n = 1 + (int)Math.Floor(t0 * idZ)) > lo) lo = n;
                n = (int)Math.Floor((ray.Origin.Z + dz * tt0 - bounds.z1) * idZ);
                if (n < hi) hi = n;
            }
        if (lo > hi)
            return false;
        testRay.SetDirection(ray);
        testRay.SquaredDir = ray.SquaredDir;
        testRay.Origin = ray.Origin - lo * delta;
        do
        {
            if (original.ShadowTest(testRay))
                return true;
            if (++lo > hi)
                return false;
            testRay.Origin -= delta;
        }
        while (true);
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        double tt0 = Tolerance.Epsilon, tt1 = maxt;
        double dx = ray.Direction.X, org, temp;
        double t0 = (bounds.x0 - (org = ray.Origin.X)) * (temp = ray.InvDir.X);
        double t1 = (bounds.x1 - org) * temp;
        if (dx >= 0)
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
        double dy = ray.Direction.Y;
        t0 = (bounds.y0 - (org = ray.Origin.Y)) * (temp = ray.InvDir.Y);
        t1 = (bounds.y1 - org) * temp;
        if (dy >= 0)
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
        double dz = ray.Direction.Z;
        t0 = (bounds.z0 - (org = ray.Origin.Z)) * (temp = ray.InvDir.Z);
        t1 = (bounds.z1 - org) * temp;
        if (dz >= 0)
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
        int lo = 0, hi = count - 1, n;
        if (delta.X > 0.0)
            if (dx >= 0.0)
            {
                if ((t0 = dx * tt0 + ray.Origin.X - w0.X) > 0.0)
                    if ((n = (int)Math.Floor(t0 * idX) + 1) > lo) lo = n;
                n = (int)Math.Floor((dx * tt1 + ray.Origin.X - bounds.x0) * idX);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = dx * tt1 + ray.Origin.X - w0.X) > 0.0)
                    if ((n = (int)Math.Floor(t0 * idX) + 1) > lo) lo = n;
                n = (int)Math.Floor((dx * tt0 + ray.Origin.X - bounds.x0) * idX);
                if (n < hi) hi = n;
            }
        else if (delta.X < 0.0)
            if (dx <= 0)
            {
                if ((t0 = dx * tt0 + ray.Origin.X - w1.X) < 0.0)
                    if ((n = (int)Math.Floor(t0 * idX) + 1) > lo) lo = n;
                n = (int)Math.Floor((dx * tt1 + ray.Origin.X - bounds.x1) * idX);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = dx * tt1 + ray.Origin.X - w1.X) < 0.0)
                    if ((n = (int)Math.Floor(t0 * idX) + 1) > lo) lo = n;
                n = (int)Math.Floor((dx * tt0 + ray.Origin.X - bounds.x1) * idX);
                if (n < hi) hi = n;
            }
        if (delta.Y > 0.0)
            if (dy >= 0.0)
            {
                if ((t0 = dy * tt0 + ray.Origin.Y - w0.Y) > 0.0)
                    if ((n = (int)Math.Floor(t0 * idY) + 1) > lo) lo = n;
                n = (int)Math.Floor((dy * tt1 + ray.Origin.Y - bounds.y0) * idY);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = dy * tt1 + ray.Origin.Y - w0.Y) > 0.0)
                    if ((n = (int)Math.Floor(t0 * idY) + 1) > lo) lo = n;
                n = (int)Math.Floor((dy * tt0 + ray.Origin.Y - bounds.y0) * idY);
                if (n < hi) hi = n;
            }
        else if (delta.Y < 0.0)
            if (dy <= 0)
            {
                if ((t0 = dy * tt0 + ray.Origin.Y - w1.Y) < 0.0)
                    if ((n = (int)Math.Floor(t0 * idY) + 1) > lo) lo = n;
                n = (int)Math.Floor((dy * tt1 + ray.Origin.Y - bounds.y1) * idY);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = dy * tt1 + ray.Origin.Y - w1.Y) < 0.0)
                    if ((n = (int)Math.Floor(t0 * idY) + 1) > lo) lo = n;
                n = (int)Math.Floor((dy * tt0 + ray.Origin.Y - bounds.y1) * idY);
                if (n < hi) hi = n;
            }
        if (delta.Z > 0.0)
            if (dz >= 0.0)
            {
                if ((t0 = dz * tt0 + org - w0.Z) > 0.0)
                    if ((n = (int)Math.Floor(t0 * idZ) + 1) > lo) lo = n;
                n = (int)Math.Floor((dz * tt1 + org - bounds.z0) * idZ);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = dz * tt1 + org - w0.Z) > 0.0)
                    if ((n = (int)Math.Floor(t0 * idZ) + 1) > lo) lo = n;
                n = (int)Math.Floor((dz * tt0 + org - bounds.z0) * idZ);
                if (n < hi) hi = n;
            }
        else if (delta.Z < 0.0)
            if (dz <= 0.0)
            {
                if ((t0 = dz * tt0 + org - w1.Z) < 0.0)
                    if ((n = (int)Math.Floor(t0 * idZ) + 1) > lo) lo = n;
                n = (int)Math.Floor((dz * tt1 + org - bounds.z1) * idZ);
                if (n < hi) hi = n;
            }
            else
            {
                if ((t0 = dz * tt1 + org - w1.Z) < 0.0)
                    if ((n = (int)Math.Floor(t0 * idZ) + 1) > lo) lo = n;
                n = (int)Math.Floor((dz * tt0 + org - bounds.z1) * idZ);
                if (n < hi) hi = n;
            }
        if (lo > hi)
            return false;
        testRay.Origin = ray.Origin - lo * delta;
        testRay.SetDirection(ray);
        do
        {
            if (original.HitTest(testRay, maxt, ref info))
            {
                if (separable && ray.Direction * delta > 0.1)
                    return true;
                while (++lo <= hi)
                {
                    testRay.Origin -= delta;
                    original.HitTest(testRay, info.Time, ref info);
                }
                return true;
            }
            if (++lo > hi)
                return false;
            testRay.Origin -= delta;
        }
        while (true);
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location) =>
        throw new RenderException(Resources.NotImplemented, "Repeater.GetNormal");

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits) =>
        throw new RenderException(Resources.ErrorInvalidShapeInCsg, "Repeaters");

    public override int MaxHits => count * original.MaxHits;

    bool IBoundsChecker.IsChecking { get => true; set { } }

    bool IBoundsChecker.IsCheckModifiable => false;

    #endregion IShape members.

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force) =>
        new Repeater(count, delta, original.Clone(force));

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Simplify()
    {
        original = original.Simplify();
        if (count == 1)
            return original;
        if (count <= Properties.Settings.Default.LoopThreshold)
        {
            IShape[] list = new IShape[count];
            IShape lastAdded = original;
            for (int idx = 0; idx < list.Length; idx++)
            {
                lastAdded = lastAdded.Clone(true);
                if (idx > 0)
                    lastAdded.ApplyTranslation(delta);
                list[idx] = lastAdded;
            }
            return new Union(list).Simplify();
        }
        return this;
    }

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation) =>
        original.CanRotate(rotation);

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor) =>
        original.CanScale(factor);

    public override void ApplyTranslation(in Vector translation)
    {
        original.ApplyTranslation(translation);
        RecomputeBounds();
    }

    public override void ApplyRotation(in Matrix rotation)
    {
        delta = rotation * delta;
        (idX, idY, idZ) = delta.Invert();
        original.ApplyRotation(rotation);
        RecomputeBounds();
    }

    public override void ApplyScale(in Vector factor)
    {
        delta = delta.Scale(factor);
        (idX, idY, idZ) = delta.Invert();
        original.ApplyScale(factor);
        RecomputeBounds();
    }

    #endregion
}

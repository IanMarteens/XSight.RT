using Rsc = IntSight.RayTracing.Engine.Properties.Resources;

namespace IntSight.RayTracing.Engine;

/// <summary>A union with two finite bound checked shapes.</summary>
[Children(nameof(shape0), nameof(shape1))]
internal sealed class Union2F : UnionBase, IShape
{
    private IShape shape0, shape1;
    private readonly Hit[] hits0;
    private readonly Hit[] hits1;
    private DualBounds db;

    /// <summary>Creates a binary union with combined bounds checking.</summary>
    /// <param name="shapes">The two involved shapes.</param>
    public Union2F(IShape[] shapes)
        : base(shapes, false, false)
    {
        checkBounds = true;
        hits1 = new Hit[this.shapes[1].MaxHits];
        hits0 = new Hit[this.shapes[0].MaxHits];
    }

    /// <summary>Updates direct references to shapes in the shape list.</summary>
    protected override void Rebind() => (shape1, shape0) = (shapes[1], shapes[0]);

    /// <summary>Updates the bounding box and the bounding sphere around the union.</summary>
    protected override void RecomputeBounds()
    {
        base.RecomputeBounds();
        db = new DualBounds(shape0.Bounds * bounds, shape1.Bounds * bounds);
    }

    #region IShape members.

    /// <summary>Find an intersection between union items and a ray.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        byte check = db.Intersects(ray, 1.0);
        if ((check & 1) != 0 && shape0.ShadowTest(ray))
        {
            if (!inTransform)
                BaseLight.lastOccluder ??= shape0;
            return true;
        }
        else if (check > 1 && shape1.ShadowTest(ray))
        {
            if (!inTransform)
                BaseLight.lastOccluder ??= shape1;
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
        byte check = db.Intersects(ray, maxt);
        if ((check & 1) != 0 && shape0.HitTest(ray, maxt, ref info))
        {
            if (check > 1)
                shape1.HitTest(ray, info.Time, ref info);
            return true;
        }
        else
            return check > 1 && shape1.HitTest(ray, maxt, ref info);
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
            if (i1 < total1 && (i2 >= total2 && hits0[i1].Time <= hits1[i2].Time))
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
    IShape ITransformable.Clone(bool force) =>
        new Union2F([shape0.Clone(force), shape1.Clone(force)]);

    #endregion
}

#if USE_SSE

/// <summary>A union with four finite bound checked shapes.</summary>
[Children(nameof(shape0), nameof(shape1), nameof(shape2), nameof(shape3))]
internal sealed class Union4F : UnionBase, IShape
{
    private IShape shape0, shape1, shape2, shape3;
    private FourBounds fb;

    /// <summary>Creates a binary union with combined bounds checking.</summary>
    /// <param name="shapes">The two involved shapes.</param>
    public Union4F(IShape[] shapes)
        : base(shapes, false, false) => checkBounds = true;

    /// <summary>Updates direct references to shapes in the shape list.</summary>
    protected override void Rebind() =>
        (shape3, shape2, shape1, shape0) = (shapes[3], shapes[2], shapes[1], shapes[0]);

    /// <summary>Updates the bounding box and the bounding sphere around the union.</summary>
    protected override void RecomputeBounds()
    {
        base.RecomputeBounds();
        fb = new(
            shape0.Bounds * bounds,
            shape1.Bounds * bounds,
            shape2.Bounds * bounds,
            shape3.Bounds * bounds);
    }

    #region IShape members.

    /// <summary>Find an intersection between union items and a ray.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        byte check = fb.Intersects(ray, 1.0);
        if ((check & 0b0001) != 0 && shape0.ShadowTest(ray))
        {
            if (!inTransform)
                BaseLight.lastOccluder ??= shape0;
            return true;
        }
        else if ((check & 0b0010) != 0 && shape1.ShadowTest(ray))
        {
            if (!inTransform)
                BaseLight.lastOccluder ??= shape1;
            return true;
        }
        else if ((check & 0b0100) != 0 && shape2.ShadowTest(ray))
        {
            if (!inTransform)
                BaseLight.lastOccluder ??= shape2;
            return true;
        }
        else if ((check & 0b1000) != 0 && shape3.ShadowTest(ray))
        {
            if (!inTransform)
                BaseLight.lastOccluder ??= shape3;
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
        byte check = fb.Intersects(ray, maxt);
        if ((check & 0b0001) != 0 && shape0.HitTest(ray, maxt, ref info))
        {
            if ((check & 0b0010) != 0)
                shape1.HitTest(ray, info.Time, ref info);
            if ((check & 0b0100) != 0)
                shape2.HitTest(ray, info.Time, ref info);
            if ((check & 0b1000) != 0)
                shape3.HitTest(ray, info.Time, ref info);
            return true;
        }
        if ((check & 0b0010) != 0 && shape1.HitTest(ray, maxt, ref info))
        {
            if ((check & 0b0100) != 0)
                shape2.HitTest(ray, info.Time, ref info);
            if ((check & 0b1000) != 0)
                shape3.HitTest(ray, info.Time, ref info);
            return true;
        }
        if ((check & 0b0100) != 0 && shape2.HitTest(ray, maxt, ref info))
        {
            if ((check & 0b1000) != 0)
                shape3.HitTest(ray, info.Time, ref info);
            return true;
        }
        return (check & 0b1000) != 0 && shape3.HitTest(ray, maxt, ref info);
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
    IShape ITransformable.Clone(bool force) =>
        new Union4F([ 
            shape0.Clone(force), shape1.Clone(force),
            shape2.Clone(force), shape3.Clone(force)
        ]);

    #endregion
}

#endif

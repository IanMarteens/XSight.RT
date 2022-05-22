namespace IntSight.RayTracing.Engine;

/// <summary>One-sheeted vertical hyperboloids.</summary>
[XSight(Alias = "Hyper")]
[Properties(nameof(Radius), nameof(axes), nameof(center), nameof(centroid), nameof(material))]
public sealed class Hyperboloid : MaterialShape, IShape
{
    private Vector center, axes, centroid;
    private double top, bottom, sqradius;
    private double r1x, r1y, r1z, r2x, r2y, r2z;
    private double r2nx, r2ny, r2nz;
    private bool negated;

    /// <summary>Creates a capped vertical-aligned one-sheeted hyperboloid.</summary>
    /// <param name="center">The center of the hyperboloid.</param>
    /// <param name="axes">The three axes of the shape.</param>
    /// <param name="top">Higher cap position.</param>
    /// <param name="bottom">Lower cap position.</param>
    /// <param name="material">Material this shape is made of.</param>
    public Hyperboloid(
        [Proposed("^0")] Vector center,
        [Proposed("^1")] Vector axes,
        double bottom, double top, IMaterial material)
        : base(material)
    {
        this.center = center;
        this.axes = axes;
        this.bottom = Math.Min(bottom, top);
        this.top = Math.Max(bottom, top);
        RecomputeBounds();
    }

    public Hyperboloid(
        [Proposed("^0")] Vector center,
        [Proposed("1")] double axis,
        double bottom, double top,
        IMaterial material)
        : this(center, new Vector(axis), bottom, top, material) { }

    private void RecomputeBounds()
    {
        r1x = 1.0 / axes.X; r1y = 1.0 / axes.Y; r1z = 1.0 / axes.Z;
        r2x = r1x * r1x; r2y = r1y * r1y; r2z = r1z * r1z;
        if (negated)
        {
            r2nx = -r2x; r2ny = -r2y; r2nz = -r2z;
        }
        else
        {
            r2nx = r2x; r2ny = r2y; r2nz = r2z;
        }
        double y0 = bottom - center.Y;
        double y1 = top - center.Y;
        double h = Math.Max(Math.Abs(y0), Math.Abs(y1));
        h = Math.Sqrt(1 + h * h * r2y);
        double x0 = axes.X * h;
        double z0 = axes.Z * h;
        bounds = new Bounds(-x0, y0, -z0, x0, y1, z0) + center;
        double major = Math.Max(axes.X, axes.Z);
        double dy = y1 - y0;
        double dTop = (major * major * r2y * (y0 * y0 - y1 * y1) + dy * dy) / (2 * dy);
        centroid = new Vector(0, y1 - dTop, 0) + center;
        sqradius = dTop * dTop + major * major * (1 + y1 * y1 * r2y);
    }

    public override Vector Centroid => centroid;

    public override double SquaredRadius => sqradius;

    public double Radius => Math.Sqrt(sqradius);

    #region IShape Members

    /// <summary>Checks whether a given ray intersects the hyperboloid.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        // Compute time bounds for the Y axis
        double tt0, tt1, a;
        if ((a = ray.InvDir.Y) >= 0)
        {
            tt0 = (bottom - ray.Origin.Y) * a;
            tt1 = (top - ray.Origin.Y) * a;
        }
        else
        {
            tt0 = (top - ray.Origin.Y) * a;
            tt1 = (bottom - ray.Origin.Y) * a;
        }
        if (tt0 > 1.0)
            return false;
        if (tt0 >= Tolerance.Epsilon)
        {
            if (IsInside(tt0, ray))
                return true;
            if (tt1 <= 1.0 && IsInside(tt1, ray))
                return true;
        }
        else if (tt1 >= Tolerance.Epsilon && tt1 <= 1.0 && IsInside(tt1, ray))
            return true;

        double gx = (ray.Origin.X - center.X) * r1x, dx = ray.Direction.X * r1x;
        double gy = (ray.Origin.Y - center.Y) * r1y, dy = ray.Direction.Y * r1y;
        double gz = (ray.Origin.Z - center.Z) * r1z, dz = ray.Direction.Z * r1z;
        a = 1.0 / ((dx + dy) * (dx - dy) + dz * dz);
        // "DZ" is the linear coefficient of the quadric and "GZ" is the discriminant.
        dz = (dy * gy - dx * gx - dz * gz) * a;
        gz = Math.FusedMultiplyAdd(gy * gy - gx * gx - gz * gz + 1, a, dz * dz);
        if (gz < 0.0)
            return false;
        a = dz - (gz = Math.Sqrt(gz));
        if (a >= tt0 && a <= tt1 && a >= Tolerance.Epsilon && a <= 1.0)
            return true;
        a = dz + gz;
        return a >= tt0 && a <= tt1 && a >= Tolerance.Epsilon && a <= 1.0;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        // There can be up to four roots, but we'll store only three of them.
        int count = 0;
        double r0 = 0, r1 = 0, r2 = 0;
        // Compute time bounds for the Y axis
        double tt0, tt1, a;
        if ((a = ray.InvDir.Y) >= 0)
        {
            tt0 = (bottom - ray.Origin.Y) * a;
            tt1 = top - ray.Origin.Y;
        }
        else
        {
            tt0 = (top - ray.Origin.Y) * a;
            tt1 = bottom - ray.Origin.Y;
        }
        if (tt0 > maxt)
            return false;
        if (Tolerance.Epsilon <= tt0 && IsInside(tt0, ray))
        {
            // We have hit the first cap: there's no need for further testing.
            goto FOUND;
        }
        tt1 *= a;
        if (IsInside(tt1, ray))
        {
            count = 1;
            r0 = tt1;
        }

        // Compute intersections with the hyperbolic sheet
        double gx = (ray.Origin.X - center.X) * r1x, dx = ray.Direction.X * r1x;
        double gy = (ray.Origin.Y - center.Y) * r1y, dy = ray.Direction.Y * r1y;
        double gz = (ray.Origin.Z - center.Z) * r1z, dz = ray.Direction.Z * r1z;
        a = 1.0 / ((dx + dy) * (dx - dy) + dz * dz);
        // "DZ" is the linear coefficient of the quadric and "GZ" is the discriminant.
        dz = (dy * gy - dx * gx - dz * gz) * a;
        gz = Math.FusedMultiplyAdd(gy * gy - gx * gx - gz * gz + 1, a, dz * dz);
        if (gz >= 0.0)
        {
            a = dz - (gz = Math.Sqrt(gz));
            if (a > tt0 && a < tt1)
            {
                count++;
                if (count == 1)
                    r0 = a;
                else
                    r1 = a;
            }
            a = dz + gz;
            if (a > tt0 && a < tt1)
            {
                count++;
                if (count == 1)
                    r0 = a;
                else if (count == 2)
                    r1 = a;
                else
                    r2 = a;
            }
        }
        // Find the first intersection
        if (count == 0)
            return false;
        tt0 = -1.0;
        if (count == 2)
        {
            if (Tolerance.Epsilon <= r0 && r0 <= maxt)
                tt0 = maxt = r0;
            if (Tolerance.Epsilon <= r1 && r1 <= maxt)
                tt0 = r1;
        }
        else if (count == 1)
        {
            if (Tolerance.Epsilon <= r0 && r0 <= maxt)
                tt0 = r0;
        }
        else // count == 3 && r1 <= r2
        {
            if (Tolerance.Epsilon <= r0 && r0 <= maxt)
                tt0 = maxt = r0;
            if (Tolerance.Epsilon <= r1 && r1 <= maxt)
                tt0 = r1;
            else if (Tolerance.Epsilon <= r2 && r2 <= maxt)
                tt0 = r2;
        }
        if (tt0 <= 0.0)
            return false;
        FOUND:
        info.Time = tt0;
        info.HitPoint = ray[tt0];
        if (Tolerance.Near(info.HitPoint.Y, top))
            info.Normal = Vector.YRay;
        else if (Tolerance.Near(info.HitPoint.Y, bottom))
            info.Normal = Vector.YRayM;
        else
        {
            gx = (info.HitPoint.X - center.X) * r2x;
            gy = (center.Y - info.HitPoint.Y) * r2y;
            gz = (info.HitPoint.Z - center.Z) * r2z;
            a = 1.0 / Math.Sqrt(gx * gx + gy * gy + gz * gz);
            info.Normal = new(a * gx, a * gy, a * gz);
        }
        info.Material = material;
        return true;
    }

    private bool IsInside(double time, Ray r)
    {
        double x = (r.Origin.X + r.Direction.X * time - center.X) * r1x;
        double y = (r.Origin.Y + r.Direction.Y * time - center.Y) * r1y;
        double z = (r.Origin.Z + r.Direction.Z * time - center.Z) * r1z;
        return (y + x) * (y - x) + 1 >= z * z;
    }

    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        int total = 0;
        bool cap0 = false, cap1 = false;
        // Compute time bounds for the Y axis
        double tt0, tt1, a;
        if ((a = ray.InvDir.Y) >= 0)
        {
            tt0 = (bottom - ray.Origin.Y) * a;
            tt1 = (top - ray.Origin.Y) * a;
        }
        else
        {
            tt0 = (top - ray.Origin.Y) * a;
            tt1 = (bottom - ray.Origin.Y) * a;
        }
        if (IsInside(tt0, ray))
        {
            hits[0].Time = tt0;
            hits[0].Shape = this;
            total = 1;
            cap0 = true;
        }
        if (IsInside(tt1, ray))
        {
            hits[total].Time = tt1;
            hits[total].Shape = this;
            total++;
            cap1 = true;
        }
        // Compute intersections with the hyperbolic sheet
        double gx = (ray.Origin.X - center.X) * r1x, dx = ray.Direction.X * r1x;
        double gy = (ray.Origin.Y - center.Y) * r1y, dy = ray.Direction.Y * r1y;
        double gz = (ray.Origin.Z - center.Z) * r1z, dz = ray.Direction.Z * r1z;
        a = 1.0 / ((dx + dy) * (dx - dy) + dz * dz);
        // "DZ" is the linear coefficient of the quadric and "GZ" is the discriminant.
        dz = (dy * gy - dx * gx - dz * gz) * a;
        gz = (gy * gy - gx * gx - gz * gz + 1) * a + dz * dz;
        if (gz >= 0.0)
        {
            a = dz - (gz = Math.Sqrt(gz));
            if (a > tt0 && a < tt1)
            {
                hits[total].Time = a;
                hits[total].Shape = this;
                total++;
            }
            a = dz + gz;
            if (a > tt0 && a < tt1)
            {
                hits[total].Time = a;
                hits[total].Shape = this;
                total++;
            }
        }
        // Sort roots.
        if (cap0)
        {
            if (cap1 && total > 2)
            {
                var time = hits[1].Time;
                hits[1].Time = hits[2].Time;
                hits[2].Time = hits[3].Time;
                hits[total - 1].Time = time;
            }
        }
        else if (cap1 && total > 1)
        {
            var time = hits[0].Time;
            hits[0].Time = hits[1].Time;
            hits[1].Time = hits[2].Time;
            hits[total - 1].Time = time;
        }
        return total;
    }

    /// <summary>Computes the hyperboloid normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        if (Tolerance.Near(location.Y, top))
            return negated ? Vector.YRayM : Vector.YRay;
        else if (Tolerance.Near(location.Y, bottom))
            return negated ? Vector.YRay : Vector.YRayM;
        else
        {
            double dx = (location.X - center.X) * r2nx;
            double dy = (center.Y - location.Y) * r2ny;
            double dz = (location.Z - center.Z) * r2nz;
            double len = 1.0 / Math.Sqrt(dx * dx + dy * dy + dz * dz);
            return new(dx * len, dy * len, dz * len);
        }
    }

    #endregion

    #region ITransformable Members

    int ITransformable.MaxHits => 4;

    ShapeCost ITransformable.Cost => ShapeCost.NoPainNoGain;

    IShape ITransformable.Clone(bool force)
    {
        IMaterial m = material.Clone(force);
        if (force || m != material)
        {
            IShape s = new Hyperboloid(center, axes, bottom, top, m);
            if (negated)
                s.Negate();
            return s;
        }
        else
            return this;
    }

    void ITransformable.Negate()
    {
        negated = !negated;
        r2nx = -r2nx; r2ny = -r2ny; r2nz = -r2nz;
    }

    public override TransformationCost CanRotate(in Matrix rotation) =>
        axes.X == axes.Z &&
            Tolerance.IsZero(rotation * Vector.YRay - Vector.YRay)
            ? TransformationCost.Ok
            : TransformationCost.Nope;

    /// <summary>Tells the optimizer this shape can be scaled offline.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor) => TransformationCost.Ok;

    public override void ApplyTranslation(in Vector translation)
    {
        center += translation;
        top += translation.Y;
        bottom += translation.Y;
        bounds += translation;
        centroid += translation;
        material = material.Translate(translation);
    }

    public override void ApplyRotation(in Matrix rotation)
    {
        center = rotation * center;
        RecomputeBounds();
    }

    public override void ApplyScale(in Vector factor)
    {
        center = center.Scale(factor);
        axes = axes.Scale(factor);
        top *= factor.Y;
        bottom *= factor.Y;
        material = material.Scale(factor);
        RecomputeBounds();
    }

    #endregion
}
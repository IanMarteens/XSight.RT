using static System.Math;

namespace IntSight.RayTracing.Engine;

/// <summary>Closed cones with arbitrary alineations.</summary>
[XSight]
[Properties(nameof(radius), nameof(SquaredRadius), nameof(bottom), nameof(top),
    nameof(Centroid), nameof(material))]
public sealed class Cone(
    [Proposed("[0,0,0]")] Vector bottom,
    [Proposed("[0,1,0]")] Vector top,
    [Proposed("1")] double radius,
    IMaterial material) : LinearSoR(bottom, top, radius, material), IShape
{
    private double h2, rh, rh2;

    public Cone(
        [Proposed("[0,0,0]")] Vector bottom,
        [Proposed("1")] double height,
        [Proposed("1")] double radius,
        IMaterial material)
        : this(bottom, bottom + height * Vector.YRay, radius, material) { }

    public Cone(
        [Proposed("0")] double x, [Proposed("0")] double y, [Proposed("0")] double z,
        [Proposed("1")] double height,
        [Proposed("1")] double radius,
        IMaterial material)
        : this(new(x, y, z), new Vector(x, y + height, z), radius, material) { }

    protected override void RecomputeBounds()
    {
        base.RecomputeBounds();
        h2 = height * height;
        rh = radius / height;
        rh2 = rh * rh;
    }

    #region IShape members.

    /// <summary>The center of the bounding sphere.</summary>
    public override Vector Centroid =>
        height <= radius ? bottom : bottom - (0.5 * (h2 - r2) / height) * bottomNormal;

    /// <summary>The square radius of the bounding sphere.</summary>
    public override double SquaredRadius
    {
        get
        {
            if (height <= radius)
                return r2;
            else
            {
                double ro = (h2 + r2) / height;
                return 0.25 * ro * ro;
            }
        }
    }

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        // Apply the inverse transformation to the incoming ray
        Vector org = inverse.Rotate(
            ray.Origin.X - bottom.X, ray.Origin.Y - bottom.Y, ray.Origin.Z - bottom.Z);
        Vector dir = inverse * ray.Direction;
        double root0 = Tolerance.Epsilon, root1 = 1.0, temp = 1.0 / dir.Y;
        double t0 = -org.Y * temp, t1 = (height - org.Y) * temp;
        if (temp > 0)
        {
            if (t0 > Tolerance.Epsilon)
                if ((root0 = t0) < 1.0)
                {
                    double x = FusedMultiplyAdd(t0, dir.X, org.X);
                    double z = FusedMultiplyAdd(t0, dir.Z, org.Z);
                    if (x * x + z * z <= r2)
                        return true;
                }
            if (t1 < 1.0) root1 = t1;
        }
        else
        {
            if (t1 > root0) root0 = t1;
            if (t0 < 1.0) root1 = t0;
        }
        if (root0 > root1)
            return false;
        double a = dir.X * dir.X + dir.Z * dir.Z - rh2 * dir.Y * dir.Y;
        double b = ((rh2 * org.Y - radius * rh) * dir.Y -
            org.X * dir.X - org.Z * dir.Z) / a;
        if ((temp = b * b - (org.X * org.X + org.Z * org.Z +
            (2 * radius * rh - rh2 * org.Y) * org.Y - r2) / a) < 0.0)
            return false;
        t0 = b - (temp = Sqrt(temp));
        if (t0 < root0 || t0 > root1)
            if ((t0 = b + temp) < root0 || t0 > root1)
                return false;
        return true;
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
        double root0 = Tolerance.Epsilon, root1 = maxt, temp = 1 / dir.Y;
        double t0 = -org.Y * temp, t1 = (height - org.Y) * temp;
        if (temp > 0)
        {
            if (t0 > Tolerance.Epsilon)
                if ((root0 = t0) < maxt)
                {
                    double x1 = FusedMultiplyAdd(t0, dir.X, org.X);
                    double z1 = FusedMultiplyAdd(t0, dir.Z, org.Z);
                    if (x1 * x1 + z1 * z1 <= r2)
                        goto OK;
                }
            if (t1 < root1) root1 = t1;
        }
        else
        {
            if (t1 > root0) root0 = t1;
            if (t0 < root1) root1 = t0;
        }
        if (root0 > root1)
            return false;
        double a = dir.X * dir.X + dir.Z * dir.Z - rh2 * dir.Y * dir.Y;
        double b = ((rh2 * org.Y - radius * rh) * dir.Y -
            org.X * dir.X - org.Z * dir.Z) / a;
        if ((temp = b * b - (org.X * org.X + org.Z * org.Z +
            (2 * radius * rh - rh2 * org.Y) * org.Y - r2) / a) < 0.0)
            return false;
        t0 = b - (temp = Sqrt(temp));
        if ((t0 < root0 || t0 > root1) && ((t0 = b + temp) < root0 || t0 > root1))
            return false;
        root0 = t0;
    OK:
        double x = root0 * dir.X + org.X, z = root0 * dir.Z + org.Z;
        temp = Sqrt(x * x + z * z);
        info.Normal = temp < Tolerance.Epsilon
            ? topNormal
            : root0 * dir.Y + org.Y < Tolerance.Epsilon 
            ? bottomNormal
            : transform.RotateNorm(x, rh * temp, z);
        info.Time = root0;
        info.HitPoint = ray[root0];
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
        double len = Sqrt(lct.X * lct.X + lct.Z * lct.Z);
        return len < Tolerance.Epsilon
            ? negated ? bottomNormal : topNormal
            : lct.Y < Tolerance.Epsilon
            ? negated ? topNormal : bottomNormal
            : negated ? transform.RotateNorm(-lct.X, -rh * len, -lct.Z)
            : transform.RotateNorm(lct.X, rh * len, lct.Z);
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
        double temp = 1 / dir.Y, root0, root1;
        if (temp > 0)
        {
            root0 = -org.Y * temp;
            root1 = (height - org.Y) * temp;
        }
        else
        {
            root0 = (height - org.Y) * temp;
            root1 = -org.Y * temp;
        }
        double a = 1 / (dir.X * dir.X + dir.Z * dir.Z - rh2 * dir.Y * dir.Y);
        double b = ((rh2 * org.Y - radius * rh) * dir.Y -
            org.X * dir.X - org.Z * dir.Z) * a;
        double discr = b * b - (org.X * org.X + org.Z * org.Z +
            ((radius + radius) * rh - rh2 * org.Y) * org.Y - r2) * a;
        if (discr < 0.0)
            return 0;
        double t0 = b - (discr = Sqrt(discr)), t1 = b + discr;
        int count = root0 <= t0 && t0 <= root1 ? 0x1 : 0x0;
        if (root0 <= t1 && t1 <= root1)
            count |= 0x2;
        if (count == 0)
            return 0;
        if (count == 3)
        {
            hits[1].Time = t1;
            hits[0].Time = t0;
        }
        else
        {
            if (count == 2)
                t0 = t1;
            t1 = temp > 0 ? root0 : root1;
            double x = FusedMultiplyAdd(t1, dir.X, org.X);
            double z = FusedMultiplyAdd(t1, dir.Z, org.Z);
            if (x * x + z * z > r2)
                return 0;
            if (t0 <= t1)
            {
                hits[1].Time = t1;
                hits[0].Time = t0;
            }
            else
            {
                hits[1].Time = t0;
                hits[0].Time = t1;
            }
        }
        hits[1].Shape = hits[0].Shape = this;
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
            Cone c = new(bottom, top, radius, m);
            if (negated)
                c.Negate();
            return c;
        }
        else
            return this;
    }

    #endregion
}

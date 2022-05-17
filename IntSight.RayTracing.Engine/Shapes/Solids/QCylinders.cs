using System;

namespace IntSight.RayTracing.Engine
{
    /// <summary>Quartic cylinders.</summary>
    /// <remarks>
    /// Surface definition: (x^2 + y^2)^2 + z^4 = 1
    /// </remarks>
    [XSight(Alias = "QCyl")]
    [Properties(nameof(radius), nameof(center), nameof(material))]
    public sealed class QCylinder : MaterialShape, IShape
    {
        private Vector center, radius;
        private Vector invRadius, invNegRadius;
        private Solver.Roots roots;
        private bool negated;

        /// <summary>Creates a quartic cylinder given its center and its three axes.</summary>
        /// <param name="center">The center of the quartic cylinder.</param>
        /// <param name="radius">The three axes of the quartic cylinder.</param>
        /// <param name="material">Material this shape is made from.</param>
        public QCylinder(
            [Proposed("^0")] Vector center,
            [Proposed("[1,1,1]")] Vector radius,
            IMaterial material)
            : base(material)
        {
            this.center = center;
            this.radius = radius;
            RecomputeBounds();
        }

        public QCylinder(
            [Proposed("^0")] Vector center,
            [Proposed("1")] double radius,
            [Proposed("1")] double height,
            IMaterial material)
            : this(center, new Vector(radius, radius, height / 2), material) { }

        public QCylinder(
            [Proposed("0")] double x0, [Proposed("0")] double y0, [Proposed("0")] double z0,
            [Proposed("1")] double rx, [Proposed("1")] double ry, [Proposed("1")] double rz,
             IMaterial material)
            : this(new Vector(x0, y0, z0), new Vector(rx, ry, rz), material) { }

        /// <summary>Update the bounding box around this shape.</summary>
        private void RecomputeBounds()
        {
            invRadius = new(1 / radius.X, 1 / radius.Y, 1 / radius.Z);
            invNegRadius = negated ? -invRadius : invRadius;
            bounds = Bounds.Create(
                center.X - radius.X, center.Y - radius.Y, center.Z - radius.Z,
                center.X + radius.X, center.Y + radius.Y, center.Z + radius.Z);
        }

        #region IShape members.

        /// <summary>The center of the bounding sphere.</summary>
        Vector IBounded.Centroid => center;

        /// <summary>Checks whether a given ray intersects the shape.</summary>
        /// <param name="ray">Ray emitted by a light source.</param>
        /// <returns>True, when such an intersection exists.</returns>
        bool IShape.ShadowTest(Ray ray)
        {
            double gx = (ray.Origin.X - center.X) * invRadius.X, gx2 = gx * gx;
            double gy = (ray.Origin.Y - center.Y) * invRadius.Y, gy2 = gy * gy;
            double gz = (ray.Origin.Z - center.Z) * invRadius.Z, gz2 = gz * gz;
            double dx = ray.Direction.X * invRadius.X, dx2 = dx * dx;
            double dy = ray.Direction.Y * invRadius.Y, dy2 = dy * dy;
            double dz = ray.Direction.Z * invRadius.Z, dz2 = dz * dz;
            double dxy2 = dx2 + dy2, gxy2 = gx2 + gy2;
            double gdxy = gx * dx + gy * dy;
            double f = 1 / (dxy2 * dxy2 + dz2 * dz2);
            Solver.Solve(
                4.0 * (dxy2 * gdxy + gz * dz * dz2) * f,
                (4.0 * gdxy * gdxy + (dxy2 + dxy2) * gxy2 + 6.0 * gz2 * dz2) * f,
                4.0 * (gdxy * gxy2 + gz * dz * gz2) * f,
                (gxy2 * gxy2 + gz2 * gz2 - 1.0) * f,
                ref roots);
            if (roots.Count == 0)
                return false;
            else if (roots.Count == 1)
                return Tolerance.Epsilon <= roots.R0 && roots.R0 <= Math.Sqrt(dx2 + dy2 + dz2);
            else
            {
                f = Math.Sqrt(dx2 + dy2 + dz2);
                return Tolerance.Epsilon <= roots.R0 ?
                    roots.R0 < f :
                    Tolerance.Epsilon <= roots.R1 && roots.R1 <= f;
            }
        }

        /// <summary>Test intersection with a given ray.</summary>
        /// <param name="ray">Ray to be tested (direction is always normalized).</param>
        /// <param name="maxt">Upper bound for the intersection time.</param>
        /// <param name="info">Hit information, when an intersection is found.</param>
        /// <returns>True when an intersection is found.</returns>
        bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
        {
            double gx = (ray.Origin.X - center.X) * invRadius.X, gx2 = gx * gx;
            double gy = (ray.Origin.Y - center.Y) * invRadius.Y, gy2 = gy * gy;
            double gz = (ray.Origin.Z - center.Z) * invRadius.Z, gz2 = gz * gz;
            double dx = ray.Direction.X * invRadius.X, dx2 = dx * dx;
            double dy = ray.Direction.Y * invRadius.Y, dy2 = dy * dy;
            double dz = ray.Direction.Z * invRadius.Z, dz2 = dz * dz;
            double dxy2 = dx2 + dy2, gxy2 = gx2 + gy2;
            double gdxy = gx * dx + gy * dy;
            double t = 1 / (dxy2 * dxy2 + dz2 * dz2);
            Solver.Solve(
                4.0 * (dxy2 * gdxy + gz * dz * dz2) * t,
                (4.0 * gdxy * gdxy + (dxy2 + dxy2) * gxy2 + 6.0 * gz2 * dz2) * t,
                4.0 * (gdxy * gxy2 + gz * dz * gz2) * t,
                (gxy2 * gxy2 + gz2 * gz2 - 1.0) * t,
                ref roots);
            if (roots.Count == 0)
                return false;
            else if (roots.Count == 1)
            {
                t = roots.R0;
                if (Tolerance.Epsilon > t || t > maxt)
                    return false;
            }
            else
            {
                if (roots.R0 >= Tolerance.Epsilon)
                    if (roots.R0 <= maxt)
                        t = roots.R0;
                    else
                        return false;
                else if (Tolerance.Epsilon <= roots.R1 && roots.R1 <= maxt)
                    t = roots.R1;
                else
                    return false;
            }
            info.Time = t;
            info.HitPoint = ray[t];
            // Compute the normal at the intersection point.
            {
                gx = (info.HitPoint.X - center.X) * invRadius.X;
                gy = (info.HitPoint.Y - center.Y) * invRadius.Y;
                gz = (info.HitPoint.Z - center.Z) * invRadius.Z;
                t = gx * gx + gy * gy;
                info.Normal = new Vector(gx * t, gy * t, gz * gz * gz).
                    ScaleNormal(invRadius);
            }
            info.Material = material;
            return true;
        }

        /// <summary>Computes the surface normal at a given location.</summary>
        /// <param name="location">Point to be tested.</param>
        /// <returns>Normal vector at that point.</returns>
        Vector IShape.GetNormal(in Vector location)
        {
            double x = (location.X - center.X) * invRadius.X;
            double y = (location.Y - center.Y) * invRadius.Y;
            double z = (location.Z - center.Z) * invRadius.Z;
            double r = x * x + y * y;
            return new Vector(x * r, y * r, z * z * z).ScaleNormal(invNegRadius);
        }

        /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
        /// <param name="ray">Ray to be tested.</param>
        /// <param name="hits">Preallocated array of intersections.</param>
        /// <returns>Number of found intersections.</returns>
        int IShape.GetHits(Ray ray, Hit[] hits)
        {
            double gx = (ray.Origin.X - center.X) * invRadius.X, gx2 = gx * gx;
            double gy = (ray.Origin.Y - center.Y) * invRadius.Y, gy2 = gy * gy;
            double gz = (ray.Origin.Z - center.Z) * invRadius.Z, gz2 = gz * gz;
            double dx = ray.Direction.X * invRadius.X, dx2 = dx * dx;
            double dy = ray.Direction.Y * invRadius.Y, dy2 = dy * dy;
            double dz = ray.Direction.Z * invRadius.Z, dz2 = dz * dz;
            double dxy2 = dx2 + dy2, gxy2 = gx2 + gy2;
            double gdxy = gx * dx + gy * dy;
            double f = dxy2 * dxy2 + dz2 * dz2;
            Solver.Solve(
                4.0 * (dxy2 * gdxy + gz * dz * dz2) / f,
                (4.0 * gdxy * gdxy + 2.0 * dxy2 * gxy2 + 6.0 * gz2 * dz2) / f,
                4.0 * (gdxy * gxy2 + gz * dz * gz2) / f,
                (gxy2 * gxy2 + gz2 * gz2 - 1.0) / f,
                ref roots);
            switch (roots.Count)
            {
                case 0:
                    return 0;
                case 1:
                    // A tangent ray.
                    hits[0].Time = hits[1].Time = roots.R0;
                    hits[0].Shape = hits[1].Shape = this;
                    return 2;
                default:
                    // There's no need to sort roots: the solver already does it.
                    hits[1].Time = roots.R1;
                    hits[0].Time = roots.R0;
                    hits[0].Shape = hits[1].Shape = this;
                    return 2;
            }
        }

        #endregion

        #region ITransformable members.

        /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
        int ITransformable.MaxHits => 2;

        /// <summary>Estimated complexity.</summary>
        ShapeCost ITransformable.Cost => ShapeCost.EasyPeasy;

        /// <summary>Invert normals for the right operand in a difference.</summary>
        void ITransformable.Negate() { negated = !negated; invNegRadius = -invNegRadius; }

        /// <summary>Creates a new independent copy of the whole shape.</summary>
        /// <param name="force">True when a new copy is needed.</param>
        /// <returns>The new shape.</returns>
        IShape ITransformable.Clone(bool force)
        {
            IShape q = new QCylinder(center, radius, material.Clone(force));
            if (negated)
                q.Negate();
            return q;
        }

        /// <summary>Translate this shape.</summary>
        /// <param name="translation">Translation amount.</param>
        public override void ApplyTranslation(in Vector translation)
        {
            center += translation;
            bounds += translation;
            material = material.Translate(translation);
        }

        /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The cost of the transformation.</returns>
        public override TransformationCost CanScale(in Vector factor) => TransformationCost.Ok;

        /// <summary>Scale this shape.</summary>
        /// <param name="factor">Scale factor.</param>
        public override void ApplyScale(in Vector factor)
        {
            center = center.Scale(factor);
            radius = radius.Scale(factor);
            material.Scale(factor);
            RecomputeBounds();
        }

        #endregion
    }
}
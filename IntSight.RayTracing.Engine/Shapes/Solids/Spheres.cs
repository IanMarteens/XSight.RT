using System.Runtime.CompilerServices;
using static System.Math;

namespace IntSight.RayTracing.Engine
{
    /// <summary>Axis-aligned ellipsoids.</summary>
    [XSight]
    [Properties(nameof(axes), nameof(center), nameof(material))]
    public sealed class Sphere : MaterialShape, IShape
    {
        /// <summary>Center of the ellipsoid.</summary>
        private Vector center;
        /// <summary>Axes of the ellipsoid.</summary>
        private Vector axes;
        /// <summary>Inverted axes.</summary>
        private Vector rinv;
        /// <summary>Inverted squared axes.</summary>
        private Vector r2inv, r2neg;
        /// <summary>The square radius of the bounding sphere.</summary>
        private double r2;
        private bool negated;

        /// <summary>Creates a ellipsoid given its center and axes.</summary>
        /// <param name="center">The center of the ellipsoid.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="material">The material this ellipsoid is made of.</param>
        public Sphere(
            [Proposed("^0")] Vector center,
            [Proposed("^1")] Vector axes,
            IMaterial material)
            : base(material)
        {
            this.center = center;
            this.axes = axes;
            RecomputeBounds();
        }

        /// <summary>Creates a spheric ellipsoide given its center and radius.</summary>
        /// <param name="center">The center of the ellipsoid.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="material">The material this ellipsoid is made of.</param>
        public Sphere(
            [Proposed("^0")] Vector center,
            [Proposed("1")] double radius,
            IMaterial material)
            : this(center, new Vector(radius), material) { }

        /// <summary>Creates an ellipsoid with equal axes.</summary>
        /// <param name="x0">X coordinate of the center.</param>
        /// <param name="y0">Y coordinate of the center.</param>
        /// <param name="z0">Z coordinate of the center.</param>
        /// <param name="radius">Radius of the ellipsoid.</param>
        /// <param name="material">Material this ellipsoid is made of.</param>
        public Sphere(
            [Proposed("0")] double x0, [Proposed("0")] double y0, [Proposed("0")] double z0,
            [Proposed("1")] double radius,
            IMaterial material)
            : this(new(x0, y0, z0), new Vector(radius), material) { }

        /// <summary>Computes the bounding box and the bounding sphere.</summary>
        private void RecomputeBounds()
        {
            // Compute the squared radius of the bounding sphere.
            r2 = Max(axes.X, Max(axes.Y, axes.Z));
            r2 *= r2;
            rinv = axes.Invert();
            r2inv = rinv.Scale(rinv);
            r2neg = negated ? -r2inv : r2inv;
            bounds = new(center - axes, center + axes);
        }

        #region IShape members.

        /// <summary>The center of the bounding sphere.</summary>
        public override Vector Centroid => center;

        /// <summary>The square radius of the bounding sphere.</summary>
        public override double SquaredRadius => r2;

        /// <summary>Checks whether a given ray intersects the shape.</summary>
        /// <param name="ray">Ray emitted by a light source.</param>
        /// <returns>True, when such an intersection exists.</returns>
        bool IShape.ShadowTest(Ray ray)
        {
            Vector d = ray.Direction.Scale(rinv);
            Vector g = (center - ray.Origin).Scale(rinv);
            double a = d.Squared;
            double b = d * g;
            double discr = (1.0 - g.Squared) * a + b * b;
            if (discr < 0)
                return false;
            discr = Sqrt(discr);
            double t;
            if ((t = b - discr) < Tolerance.Epsilon &&
                (t = b + discr) < Tolerance.Epsilon)
                return false;
            return t <= a;
        }

        /// <summary>Test intersection with a given ray.</summary>
        /// <param name="ray">Ray to be tested (direction is always normalized).</param>
        /// <param name="maxt">Upper bound for the intersection time.</param>
        /// <param name="info">Hit information, when an intersection is found.</param>
        /// <returns>True when an intersection is found.</returns>
        bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
        {
            Vector d = ray.Direction.Scale(rinv);
            Vector g = (center - ray.Origin).Scale(rinv);
            double a = 1 / d.Squared;
            double b = d * g * a;
            double t, discr = (1.0 - g.Squared) * a + b * b;
            if (discr < 0)
                return false;
            discr = Sqrt(discr);
            if ((t = b - discr) < Tolerance.Epsilon)
                if ((t = b + discr) < Tolerance.Epsilon)
                    return false;
            if (t > maxt)
                return false;
            info.Time = t;
            info.HitPoint = ray[t];
            info.Normal = (info.HitPoint - center).ScaleNormal(r2inv);
            info.Material = material;
            return true;
        }

        /// <summary>Computes the surface normal at a given location.</summary>
        /// <param name="location">Point to be tested.</param>
        /// <returns>Normal vector at that point.</returns>
        Vector IShape.GetNormal(in Vector location) =>
            (location - center).ScaleNormal(r2neg);

        /// <summary>Intersects the sphere with a ray and returns all intersections.</summary>
        /// <param name="ray">Ray to be tested.</param>
        /// <param name="hits">Preallocated array of intersections.</param>
        /// <returns>Number of found intersections.</returns>
        int IShape.GetHits(Ray ray, Hit[] hits)
        {
            Vector d = ray.Direction.Scale(rinv);
            Vector g = (center - ray.Origin).Scale(rinv);
            double a = 1 / d.Squared;
            double b = d * g * a;
            double discr = b * b - (g.Squared - 1.0) * a;
            if (discr < 0)
                return 0;
            hits[1].Time = b + (discr = Sqrt(discr));
            hits[0].Time = b - discr;
            hits[0].Shape = hits[1].Shape = this;
            return 2;
        }

        #endregion

        #region ITransformable members.

        /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
        int ITransformable.MaxHits => 2;

        /// <summary>Gets the estimated complexity.</summary>
        ShapeCost ITransformable.Cost => ShapeCost.EasyPeasy;

        /// <summary>Invert normals for the right operand in a difference.</summary>
        void ITransformable.Negate()
        {
            negated = !negated;
            r2neg = -r2neg;
        }

        /// <summary>Creates a new independent copy of the whole shape.</summary>
        /// <param name="force">True when a new copy is needed.</param>
        /// <returns>The new shape.</returns>
        IShape ITransformable.Clone(bool force)
        {
            IMaterial m = material.Clone(force);
            if (force || m != material)
            {
                IShape s = new Sphere(center, axes, m);
                if (negated)
                    s.Negate();
                return s;
            }
            else
                return this;
        }

        /// <summary>Second optimization pass, where special shapes are introduced.</summary>
        /// <returns>This shape, or a new and more efficient equivalent.</returns>
        public override IShape Substitute()
        {
            if (Tolerance.Near(axes.X, axes.Y) && Tolerance.Near(axes.X, axes.Z))
            {
                IShape s = new PSphere(center, axes.X, material);
                if (negated) s.Negate();
                return s;
            }
            else
                return this;
        }

        /// <summary>Translates this shape.</summary>
        /// <param name="translation">Translation amount.</param>
        public override void ApplyTranslation(in Vector translation)
        {
            center += translation;
            bounds += translation;
            material = material.Translate(translation);
        }

        /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The cost of the transformation.</returns>
        public override TransformationCost CanRotate(in Matrix rotation) =>
            Tolerance.Near(axes.X, axes.Y) && Tolerance.Near(axes.X, axes.Z)
                ? TransformationCost.Ok
                : rotation.HasRightAngles ? TransformationCost.Ok : TransformationCost.Nope;

        /// <summary>Rotate this shape.</summary>
        /// <param name="rotation">Rotation amount.</param>
        public override void ApplyRotation(in Matrix rotation)
        {
            if (!Tolerance.IsZero(center))
                center = rotation * center;
            if (!Tolerance.Near(axes.X, axes.Y) || !Tolerance.Near(axes.X, axes.Z))
                axes = rotation * axes;
            material.Rotate(rotation);
            RecomputeBounds();
        }

        /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The cost of the transformation.</returns>
        public override TransformationCost CanScale(in Vector factor) => TransformationCost.Ok;

        /// <summary>Scales this shape.</summary>
        /// <param name="factor">Scale factor.</param>
        public override void ApplyScale(in Vector factor)
        {
            center = center.Scale(factor);
            axes = axes.Scale(factor);
            material.Scale(factor);
            RecomputeBounds();
        }

        #endregion
    }

    /// <summary>Perfect spheres.</summary>
    [Properties(nameof(radius), nameof(center), nameof(material))]
    [SkipLocalsInit]
    internal sealed class PSphere : MaterialShape, IShape
    {
        /// <summary>The center of the sphere.</summary>
        private readonly Vector center;
        /// <summary>The radius of the sphere.</summary>
        private readonly double radius;
        /// <summary>The squared radius of the sphere.</summary>
        private readonly double r2;
        /// <summary>The reciprocal of the radius.</summary>
        private readonly double rinv;
        private double rInvNeg;
        private bool negated;

        /// <summary>Creates a perfect sphere given its center and its radius.</summary>
        /// <param name="center">The center of the sphere.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="material">The material this sphere is made of.</param>
        public PSphere(
            [Proposed("^0")] Vector center,
            [Proposed("1")] double radius,
            IMaterial material)
            : base(material)
        {
            this.center = center;
            this.radius = radius;
            rinv = 1.0 / radius;
            rInvNeg = rinv;
            r2 = radius * radius;
            bounds = Bounds.FromSphere(center, radius);
        }

        /// <summary>Creates a perfect sphere.</summary>
        /// <param name="x0">X coordinate of the center.</param>
        /// <param name="y0">Y coordinate of the center.</param>
        /// <param name="z0">Z coordinate of the center.</param>
        /// <param name="radius">Radius of the sphere.</param>
        /// <param name="material">Material this sphere is made of.</param>
        public PSphere(
            [Proposed("0")] double x0, [Proposed("0")] double y0, [Proposed("0")] double z0,
            [Proposed("1")] double radius,
            IMaterial material)
            : this(new(x0, y0, z0), radius, material) { }

        #region IShape members.

        /// <summary>The center of the bounding sphere.</summary>
        public override Vector Centroid => center;

        /// <summary>The square radius of the bounding sphere.</summary>
        public override double SquaredRadius => r2;

        /// <summary>Checks whether a given ray intersects the shape.</summary>
        /// <param name="ray">Ray emitted by a light source.</param>
        /// <returns>True, when such an intersection exists.</returns>
        public bool ShadowTest(Ray ray)
        {
            Vector c = center - ray.Origin;
            double b = c * ray.Direction;
            double ray2 = ray.SquaredDir;
            double d = b * b + (r2 - c.Squared) * ray2;
            if (d < 0)
                return false;
            d = Sqrt(d);
            double t = b - d;
            return t <= ray2
                && (t >= Tolerance.Epsilon || (t = b + d) >= Tolerance.Epsilon && t <= ray2);
        }

        /// <summary>Test intersection with a given ray.</summary>
        /// <param name="ray">Ray to be tested (direction is always normalized).</param>
        /// <param name="maxt">Upper bound for the intersection time.</param>
        /// <param name="info">Hit information, when an intersection is found.</param>
        /// <returns>True when an intersection is found.</returns>
        public bool HitTest(Ray ray, double maxt, ref HitInfo info)
        {
            Vector c = center - ray.Origin;
            double beta = ray.Direction * c;
            double t, d = (beta + c.X) * (beta - c.X) - c.Y * c.Y - c.Z * c.Z + r2;
            if (d < 0 || (t = beta - (d = Sqrt(d))) > maxt ||
                t < Tolerance.Epsilon && ((t = beta + d) < Tolerance.Epsilon || t > maxt))
                return false;
            info.Time = t;
            info.HitPoint = ray[t];
            info.Normal = (info.HitPoint - center) * rinv;
            info.Material = material;
            return true;
        }

        /// <summary>Computes the surface normal at a given location.</summary>
        /// <param name="location">Point to be tested.</param>
        /// <returns>Normal vector at that point.</returns>
        Vector IShape.GetNormal(in Vector location) => (location - center) * rInvNeg;

        /// <summary>Intersects the sphere with a ray and returns all intersections.</summary>
        /// <param name="ray">Ray to be tested.</param>
        /// <param name="hits">Preallocated array of intersections.</param>
        /// <returns>Number of found intersections.</returns>
        int IShape.GetHits(Ray ray, Hit[] hits)
        {
            double d = 1.0 / ray.Direction.Squared;
            Vector c = center - ray.Origin;
            double b = c * ray.Direction * d;
            if ((d = b * b + (r2 - c.Squared) * d) < 0)
                return 0;
            hits[1].Time = b + (d = Sqrt(d));
            hits[0].Time = b - d;
            hits[0].Shape = hits[1].Shape = this;
            return 2;
        }

        #endregion

        #region ITransformable members.

        /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
        int ITransformable.MaxHits => 2;

        /// <summary>Gets the estimated complexity.</summary>
        ShapeCost ITransformable.Cost => ShapeCost.EasyPeasy;

        /// <summary>Invert normals for the right operand in a difference.</summary>
        void ITransformable.Negate() { negated = !negated; rInvNeg = -rInvNeg; }

        /// <summary>Creates a new independent copy of the whole shape.</summary>
        /// <param name="force">True when a new copy is needed.</param>
        /// <returns>The new shape.</returns>
        IShape ITransformable.Clone(bool force)
        {
            IMaterial m = material.Clone(force);
            if (force || m != material)
            {
                IShape s = new PSphere(center, radius, m);
                if (negated)
                    s.Negate();
                return s;
            }
            else
                return this;
        }

        #endregion
    }
}

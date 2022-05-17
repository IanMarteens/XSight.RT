using System.Collections.Generic;
using static System.Math;

namespace IntSight.RayTracing.Engine
{
    /// <summary>Common base class for all blob items.</summary>
    public abstract class BlobItem
    {
        protected double c0, c1, c2, c3, c4;

        /// <summary>Adds this item's contribution to the total potential field.</summary>
        /// <param name="coefficients">Fourth-degree equation's coefficients.</param>
        public void AddCoefficients(ref QuarticCoefficients coefficients)
        {
            coefficients.c0 += c0;
            coefficients.c1 += c1;
            coefficients.c2 += c2;
            coefficients.c3 += c3;
            coefficients.c4 += c4;
        }

        /// <summary>Removes this item's contribution from the total potential field.</summary>
        /// <param name="coefficients">Fourth-degree equation's coefficients.</param>
        public void RemoveCoefficients(ref QuarticCoefficients coefficients)
        {
            coefficients.c0 -= c0;
            coefficients.c1 -= c1;
            coefficients.c2 -= c2;
            coefficients.c3 -= c3;
            coefficients.c4 -= c4;
        }

        /// <summary>Substitutes this blob item by a more efficient one.</summary>
        /// <returns>An equivalent blob item.</returns>
        public IBlobItem Substitute() => (IBlobItem)this;
    }

    /// <summary>A spheric field source for a blob.</summary>
    [XSight, Properties("radius", "strength", "center")]
    public sealed class Ball : BlobItem, IBlobItem
    {
        private readonly Vector axes;
        private Vector center;
        private double radius;
        private readonly double strength;
        private double r2, r4, r2inv2, r4inv, r4inv4, r2inv2s, r2inv2sa;

        /// <summary>Creates an isotropic field source.</summary>
        /// <param name="center">Field's center.</param>
        /// <param name="radius">Radius of the area of influence.</param>
        /// <param name="strength">Field strength.</param>
        public Ball(Vector center, double radius, double strength)
        {
            this.center = center;
            this.strength = strength;
            axes = new(radius);
            this.radius = radius;
            RecomputeBounds();
        }

        /// <summary>Creates an anisotropic field source.</summary>
        /// <param name="center">Field's center.</param>
        /// <param name="radius">Field intensity along axes.</param>
        /// <param name="strength">Field strength.</param>
        public Ball(Vector center, Vector axes, double strength)
        {
            this.center = center;
            this.strength = strength;
            this.axes = axes;
            radius = Sqrt(axes.Squared / 3);
            RecomputeBounds();
        }

        public Ball(Vector center, double radius)
            : this(center, radius, 1.0) { }

        public Ball(Vector center, Vector axes)
            : this(center, axes, 1.0) { }

        private void RecomputeBounds()
        {
            r2 = radius * radius;
            r2inv2 = 2.0 / (radius * radius);
            r2inv2s = r2inv2 * strength;
            r2inv2sa = Abs(r2inv2s);
            r4 = r2 * r2;
            r4inv = 1.0 / r4;
            r4inv4 = 4.0 * r4inv * strength;
            c0 = strength * r4inv;
        }

        #region IBlobItem members.

        public Bounds Bounds => Bounds.FromSphere(center, radius);

        Vector IBounded.Centroid => center;
        double IBounded.SquaredRadius => r2;
        double IBlobItem.CentroidX => center.X;
        double IBlobItem.CentroidY => center.Y;
        double IBlobItem.CentroidZ => center.Z;

        /// <summary>Creates an independent copy of this blob item.</summary>
        /// <returns>A copy of this ball.</returns>
        IBlobItem IBlobItem.Clone() => new Ball(center, radius, strength);

        /// <summary>Computes the intersection interval with the area of influence.</summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="t1">Time for entering the AOI.</param>
        /// <param name="t2">Time for leaving the AOI.</param>
        /// <returns>True, if the ray ever enters the AOI.</returns>
        bool IBlobItem.Hits(Ray ray, ref double t1, ref double t2)
        {
            double x = ray.Origin.X - center.X;
            double y = ray.Origin.Y - center.Y;
            double g2 = ray.Origin.Z - center.Z;
            double gd = x * ray.Direction.X + y * ray.Direction.Y + g2 * ray.Direction.Z;
            g2 = x * x + y * y + g2 * g2;
            if ((y = gd * gd + r2 - g2) <= 0.0)
                // When the discriminant is zero, the ray is tangent to the sphere.
                // This case can be safely discarded.
                return false;
            else
            {
                t1 = -gd - (y = Sqrt(y));
                t2 = y - gd;
                // Compute coefficients ("c0" is always 1.0 / r^4 * strength)
                c1 = gd * r4inv4;
                c2 = (gd * gd + 0.5 * g2) * r4inv4 - r2inv2s;
                c3 = (g2 - r2) * gd * r4inv4;
                c4 = ((g2 * r4inv - r2inv2) * g2 + 1.0) * strength;
                return true;
            }
        }

        /// <summary>Gets this item's contribution to the field's gradient.</summary>
        /// <param name="location">Point where the gradient will be evaluated.</param>
        /// <param name="normal">An accumulator for the total gradient.</param>
        void IBlobItem.GetNormal(in Vector location, ref Blob.Normal normal)
        {
            double dx = location.X - center.X;
            double dy = location.Y - center.Y;
            double dz = location.Z - center.Z;
            double len = dx * dx + dy * dy + dz * dz;
            if (len <= r2)
            {
                normal.x += (len = (2.0 - len * r2inv2) * r2inv2sa) * dx;
                normal.y += len * dy;
                normal.z += len * dz;
            }
        }

        /// <summary>Adds this ball, or an ellipsoid, to the parent's item list.</summary>
        /// <param name="accumulator">The item list of a blob shape.</param>
        void IBlobItem.Simplify(ICollection<IBlobItem> accumulator)
        {
            if (axes.X == axes.Y && axes.Y == axes.Z)
                accumulator.Add(this);
            else
                accumulator.Add(new Ellipse(center, axes, strength));
        }

        /// <summary>Can we rotate this ball?</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>Always true.</returns>
        bool IBlobItem.CanRotate(in Matrix rotation) => true;

        /// <summary>Can we scale this ball?</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>Always true.</returns>
        bool IBlobItem.CanScale(in Vector factor) => true;

        /// <summary>Translates this ball.</summary>
        /// <param name="translation">Translation distance.</param>
        /// <returns>The translated ball.</returns>
        IBlobItem IBlobItem.ApplyTranslation(in Vector translation)
        {
            center += translation;
            return this;
        }

        /// <summary>Rotates this ball.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The rotated ball.</returns>
        IBlobItem IBlobItem.ApplyRotation(in Matrix rotation)
        {
            center = rotation * center;
            return this;
        }

        /// <summary>Scales this ball.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>This ball, if isotropic, or an ellipsoid, if anisotropic.</returns>
        IBlobItem IBlobItem.ApplyScale(in Vector factor)
        {
            if (factor.X == factor.Y && factor.Y == factor.Z)
            {
                center = center.Scale(factor);
                radius *= factor.X;
                RecomputeBounds();
                return this;
            }
            else
                return new Ellipse(center.Scale(factor), radius * factor, strength);
        }

        #endregion
    }

    /// <summary>A hemispheric field source for a blob shape.</summary>
    [XSight, Properties("radius", "strength", "center", "top")]
    public sealed class Cap : BlobItem, IBlobItem
    {
        private Vector center, top;
        private readonly double radius;
        private readonly double strength;
        private double r2;
        private double r2inv;
        private double r4inv;
        private double r2inv2s;
        private double r2inv2sa;
        private double r4inv4s;
        private Matrix rotation, inverse;

        public Cap(Vector center, Vector top, double radius, double strength)
        {
            this.center = center;
            this.top = top.Norm();
            this.radius = radius;
            this.strength = strength;
            RecomputeBounds();
        }

        public Cap(Vector center, Vector top, double radius)
            : this(center, top, radius, 1.0) { }

        private void RecomputeBounds()
        {
            r2 = radius * radius;
            r2inv = 1.0 / r2;
            r4inv = 1.0 / (r2 * r2);
            c0 = strength / (r2 * r2);
            r2inv2s = 2.0 * strength / r2;
            r2inv2sa = Abs(r2inv2s);
            r4inv4s = 4.0 * strength / (r2 * r2);
            double proj = Sqrt(1.0 - top.Y * top.Y);
            if (!Tolerance.Zero(proj))
                rotation = new Matrix(
                    top.Z / proj, top.X, top.X * top.Y / proj,
                    0.0, top.Y, -proj,
                    -top.X / proj, top.Z, top.Y * top.Z / proj);
            else if (top.Y > 0.0)
                rotation = Matrix.Identity;
            else
                rotation = new Matrix(1.0, -1.0, 1.0);
            inverse = rotation.Transpose();
            Bounds = Bounds.Create(
                -radius, 0.0, -radius, radius, radius, radius).Transform(rotation, center);
        }

        #region IBlobItem members.

        public Bounds Bounds { get; private set; }
        Vector IBounded.Centroid => center;
        double IBounded.SquaredRadius => r2;
        double IBlobItem.CentroidX => Bounds.Center.X;
        double IBlobItem.CentroidY => Bounds.Center.Y;
        double IBlobItem.CentroidZ => Bounds.Center.Z;

        IBlobItem IBlobItem.Clone() => new Cap(center, top, radius, strength);

        void IBlobItem.GetNormal(in Vector location, ref Blob.Normal normal)
        {
            Vector loc = inverse.Rotate(
                location.X - center.X, location.Y - center.Y, location.Z - center.Z);
            if (loc.Y >= 0 && loc.Y <= radius)
            {
                double len = loc.Squared;
                if (len <= r2)
                {
                    len = (1.0 - len * r2inv) * r2inv2sa * 2.0;
                    normal.x += (rotation.A11 * loc.X +
                        rotation.A12 * loc.Y + rotation.A13 * loc.Z) * len;
                    normal.y += (rotation.A21 * loc.X +
                        rotation.A22 * loc.Y + rotation.A23 * loc.Z) * len;
                    normal.z += (rotation.A31 * loc.X +
                        rotation.A32 * loc.Y + rotation.A33 * loc.Z) * len;
                }
            }
        }

        bool IBlobItem.Hits(Ray ray, ref double t1, ref double t2)
        {
            Vector org = inverse.Rotate(
                ray.Origin.X - center.X, ray.Origin.Y - center.Y, ray.Origin.Z - center.Z);
            Vector dir = inverse * ray.Direction;
            double gd = org.X * dir.X + org.Y * dir.Y + org.Z * dir.Z;
            double g2 = org.X * org.X + org.Y * org.Y + org.Z * org.Z;
            double discr = gd * gd + r2 - g2;
            if (discr <= 0.0)
                // When the discriminant is zero, the ray is tangent to the sphere.
                // This case can be safely discarded.
                return false;
            else
            {
                t1 = -gd - (discr = Sqrt(discr));
                t2 = discr - gd;
                if (org.Y + dir.Y * t1 <= 0.0)
                    if (org.Y + dir.Y * t2 <= 0.0)
                        return false;
                    else
                        t1 = -org.Y / dir.Y;
                else if (org.Y + dir.Y * t2 <= 0.0)
                    t2 = -org.Y / dir.Y;
                // Compute coefficients.
                c1 = gd * r4inv4s;
                c2 = (gd * gd + 0.5 * g2) * r4inv4s - r2inv2s;
                c3 = (g2 - r2) * gd * r4inv4s;
                c4 = (1.0 + g2 * (g2 * r4inv - r2inv - r2inv)) * strength;
                return true;
            }
        }

        bool IBlobItem.CanRotate(in Matrix rotation) => true;

        /// <summary>Can we scale this blob item?</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>True iff scale change is isotropic.</returns>
        bool IBlobItem.CanScale(in Vector factor) => factor.X == factor.Y && factor.Y == factor.Z;

        /// <summary>Translates this blob item.</summary>
        /// <param name="translation">Translation distance.</param>
        /// <returns>This blob item.</returns>
        IBlobItem IBlobItem.ApplyTranslation(in Vector translation)
        {
            center += translation;
            RecomputeBounds();
            return this;
        }

        IBlobItem IBlobItem.ApplyRotation(in Matrix rotation)
        {
            center = rotation * center;
            top = rotation * top;
            RecomputeBounds();
            return this;
        }

        IBlobItem IBlobItem.ApplyScale(in Vector factor)
        {
            center = center.Scale(factor);
            top = top.Scale(factor);
            RecomputeBounds();
            return this;
        }

        void IBlobItem.Simplify(ICollection<IBlobItem> accumulator) => accumulator.Add(this);

        #endregion
    }

    [XSight, Properties(nameof(radius), nameof(strength), nameof(bottom), nameof(top))]
    public sealed class Pipe : BlobItem, IBlobItem
    {
        private Bounds bounds;
        private Vector bottom, top;
        private double radius;
        private readonly double strength;
        private double height;
        private double r2;
        private double r2inv;
        private double abss;
        private Matrix rotation, inverse;
        private bool active;

        public Pipe(Vector bottom, Vector top, double radius, double strength)
        {
            this.bottom = bottom;
            this.top = top;
            this.radius = radius;
            this.strength = strength;
            RecomputeBounds();
        }

        public Pipe(Vector bottom, Vector top, double radius)
            : this(bottom, top, radius, 1.0) { }

        private void RecomputeBounds()
        {
            r2 = radius * radius;
            r2inv = 1.0 / r2;
            abss = 4.0 * r2inv * Abs(strength);
            height = top.Distance(bottom);
            Vector v = top.Difference(bottom);
            double proj = Sqrt(1.0 - v.Y * v.Y);
            rotation = !Tolerance.Zero(proj)
                ? new Matrix(
                    v.Z / proj, v.X, v.X * v.Y / proj,
                    0.0, v.Y, -proj,
                    -v.X / proj, v.Z, v.Y * v.Z / proj)
                : v.Y > 0.0
                ? Matrix.Identity : new Matrix(1.0, -1.0, 1.0);
            inverse = rotation.Transpose();
            bounds = Bounds.Create(
                -radius, 0.0, -radius, radius, height, radius).Transform(rotation, bottom);
        }

        #region IBlobItem members.

        Bounds IBounded.Bounds => bounds;
        Vector IBounded.Centroid => bounds.Center;
        double IBounded.SquaredRadius => 0.25 * height * height + r2;
        double IBlobItem.CentroidX => bounds.Center.X;
        double IBlobItem.CentroidY => bounds.Center.Y;
        double IBlobItem.CentroidZ => bounds.Center.Z;

        /// <summary>Creates an independent copy of this blob item.</summary>
        /// <returns>A copy of the pipe.</returns>
        IBlobItem IBlobItem.Clone() => new Pipe(bottom, top, radius, strength);

        /// <summary>Gets this item's contribution to the field's gradient.</summary>
        /// <param name="location">Point where the gradient will be evaluated.</param>
        /// <param name="normal">An accumulator for the total gradient.</param>
        void IBlobItem.GetNormal(in Vector location, ref Blob.Normal normal)
        {
            if (active)
            {
                Vector loc = inverse.Rotate(
                    location.X - bottom.X, location.Y - bottom.Y, location.Z - bottom.Z);
                if (loc.Y >= 0.0 && loc.Y <= height)
                {
                    double len = loc.X * loc.X + loc.Z * loc.Z;
                    if (len <= r2)
                    {
                        normal.x += (len = (1.0 - len * r2inv) * abss) *
                            (rotation.A11 * loc.X + rotation.A13 * loc.Z);
                        normal.y += (rotation.A21 * loc.X + rotation.A23 * loc.Z) * len;
                        normal.z += (rotation.A31 * loc.X + rotation.A33 * loc.Z) * len;
                    }
                }
            }
        }

        /// <summary>Computes the intersection interval with the area of influence.</summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="t1">Time for entering the AOI.</param>
        /// <param name="t2">Time for leaving the AOI.</param>
        /// <returns>True, if the ray ever enters the AOI.</returns>
        bool IBlobItem.Hits(Ray ray, ref double t1, ref double t2)
        {
            // Apply the inverse transformation to the incoming ray
            Vector org = inverse.Rotate(
                ray.Origin.X - bottom.X, ray.Origin.Y - bottom.Y, ray.Origin.Z - bottom.Z);
            Vector dir = inverse * ray.Direction;
            // Compute time bounds for the XZ plane
            double temp = 1.0 / (1.0 - dir.Y * dir.Y);
            double beta = -(dir.X * org.X + dir.Z * org.Z) * temp;
            if ((temp = beta * beta - (org.X * org.X + org.Z * org.Z - r2) * temp) >= 0.0)
            {
                t1 = beta - (temp = Sqrt(temp));
                t2 = beta + temp;
                // Compute time bounds for the Y axis
                if (dir.Y >= 0)
                {
                    double t = -org.Y * (temp = 1.0 / dir.Y);
                    if (t > t1) t1 = t;
                    if ((t = (height - org.Y) * temp) < t2) t2 = t;
                }
                else
                {
                    double t = (height - org.Y) * (temp = 1.0 / dir.Y);
                    if (t > t1) t1 = t;
                    if ((t = -org.Y * temp) < t2) t2 = t;
                }
                if (t1 <= t2)
                {
                    // Compute coefficients.
                    double d2 = (1.0 - dir.Y * dir.Y) * r2inv;
                    double g2 = (org.X * org.X + org.Z * org.Z) * r2inv - 1.0;
                    double gd = (org.X * dir.X + org.Z * dir.Z) * r2inv;
                    c0 = d2 * (d2 * strength);
                    c1 = 4.0 * gd * (d2 * strength);
                    c2 = ((d2 + d2) * g2 + 4.0 * gd * gd) * strength;
                    c3 = 4.0 * gd * (g2 * strength);
                    c4 = g2 * (g2 * strength);
                    active = true;
                    return true;
                }
            }
            active = false;
            return false;
        }

        /// <summary>Can we rotate this blob item?</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>Always true.</returns>
        bool IBlobItem.CanRotate(in Matrix rotation) => true;

        /// <summary>Can we scale this blob item?</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>True iff scale change is isotropic.</returns>
        bool IBlobItem.CanScale(in Vector factor) => factor.X == factor.Y && factor.Y == factor.Z;

        /// <summary>Translates this blob item.</summary>
        /// <param name="translation">Translation distance.</param>
        /// <returns>This translated pipe.</returns>
        IBlobItem IBlobItem.ApplyTranslation(in Vector translation)
        {
            bottom += translation;
            top += translation;
            RecomputeBounds();
            return this;
        }

        /// <summary>Rotates this blob item.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The rotated pipe.</returns>
        IBlobItem IBlobItem.ApplyRotation(in Matrix rotation)
        {
            bottom = rotation * bottom;
            top = rotation * top;
            RecomputeBounds();
            return this;
        }

        /// <summary>Scales this blob item.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The original pipe.</returns>
        IBlobItem IBlobItem.ApplyScale(in Vector factor)
        {
            bottom = bottom.Scale(factor);
            top = top.Scale(factor);
            radius += factor.X;
            RecomputeBounds();
            return this;
        }

        void IBlobItem.Simplify(ICollection<IBlobItem> accumulator) => accumulator.Add(this);

        /// <summary>Substitutes this blob item by a more efficient one.</summary>
        /// <returns>An equivalent blob item.</returns>
        IBlobItem IBlobItem.Substitute()
        {
            if (rotation.IsIdentity)
                return bottom.Y < top.Y
                    ? new YPipe(bottom, top, radius, strength)
                    : new YPipe(top, bottom, radius, strength);
            else if (Tolerance.Near(bottom.Y, top.Y) && Tolerance.Near(bottom.Z, top.Z))
                return bottom.X < top.X
                    ? new XPipe(bottom, top, radius, strength)
                    : new XPipe(top, bottom, radius, strength);
            else if (Tolerance.Near(bottom.X, top.X) && Tolerance.Near(bottom.Y, top.Y))
                return bottom.Z < top.Z
                    ? new ZPipe(bottom, top, radius, strength)
                    : new ZPipe(top, bottom, radius, strength);
            else
                return this;
        }

        #endregion
    }

    [XSight(Alias = "BlobRep"), Properties("times", "delta", "rotation")]
    public sealed class BlobRepeater : IBlobItem
    {
        private Bounds bounds;
        private readonly IBlobItem original;
        private readonly int times;
        private readonly Vector delta;
        private readonly Vector rotation;
        private readonly Matrix matrix;

        public BlobRepeater(IBlobItem original, int times, Vector delta, Vector rotation)
        {
            this.original = original;
            this.times = times;
            this.delta = delta;
            this.rotation = rotation;
            matrix = Matrix.Rotation(rotation.X, rotation.Y, rotation.Z);
            RecomputeBounds();
        }

        private void RecomputeBounds()
        {
            bounds = original.Bounds;
            Bounds b = bounds;
            for (int i = 1; i < times; i++)
            {
                b = b.Transform(matrix, delta);
                bounds += b;
            }
        }

        #region IBlobItem members.

        Bounds IBounded.Bounds => bounds;
        Vector IBounded.Centroid => bounds.Center;
        double IBlobItem.CentroidX => bounds.Center.X;
        double IBlobItem.CentroidY => bounds.Center.Y;
        double IBlobItem.CentroidZ => bounds.Center.Z;
        double IBounded.SquaredRadius => bounds.SquaredRadius;

        IBlobItem IBlobItem.Clone() =>
            new BlobRepeater(original.Clone(), times, delta, rotation);

        void IBlobItem.GetNormal(in Vector location, ref Blob.Normal normal) { }

        bool IBlobItem.Hits(Ray ray, ref double t1, ref double t2) => false;

        bool IBlobItem.CanRotate(in Matrix rotation) => original.CanRotate(rotation);

        bool IBlobItem.CanScale(in Vector factor) => original.CanScale(factor);

        /// <summary>Translates this blob item.</summary>
        /// <param name="translation">Translation distance.</param>
        /// <returns>This blob item.</returns>
        IBlobItem IBlobItem.ApplyTranslation(in Vector translation)
        {
            original.ApplyTranslation(translation);
            RecomputeBounds();
            return this;
        }

        /// <summary>Rotates this blob item.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The rotated blob item.</returns>
        IBlobItem IBlobItem.ApplyRotation(in Matrix rotation)
        {
            original.ApplyRotation(rotation);
            RecomputeBounds();
            return this;
        }

        /// <summary>Scales this blob item.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The original repeater.</returns>
        IBlobItem IBlobItem.ApplyScale(in Vector factor)
        {
            original.ApplyScale(factor);
            RecomputeBounds();
            return this;
        }

        /// <summary>Adds this item's contribution to the total potential field.</summary>
        /// <param name="coefficients">Fourth-degree equation's coefficients.</param>
        void IBlobItem.AddCoefficients(ref QuarticCoefficients coefficients) { }
        /// <summary>Removes this item's contribution from the total potential field.</summary>
        /// <param name="coefficients">Fourth-degree equation's coefficients.</param>
        void IBlobItem.RemoveCoefficients(ref QuarticCoefficients coefficients) { }

        /// <summary>Adds the repeated items to the parent's item list.</summary>
        /// <param name="accumulator">The item list of a blob shape.</param>
        void IBlobItem.Simplify(ICollection<IBlobItem> accumulator)
        {
            IBlobItem item = original;
            item.Simplify(accumulator);
            for (int i = 1; i < times; i++)
            {
                item = item.Clone().ApplyRotation(matrix).ApplyTranslation(delta);
                item.Simplify(accumulator);
            }
        }

        /// <summary>Substitutes this blob item by a more efficient one.</summary>
        /// <returns>An equivalent blob item.</returns>
        IBlobItem IBlobItem.Substitute() => this;

        #endregion
    }

    [Properties("radius", "strength", "bottom", "top")]
    internal sealed class XPipe : BlobItem, IBlobItem
    {
        private Vector bottom, top;
        private readonly double radius;
        private readonly double strength;
        private double height;
        private double r2;
        private double r2inv;
        private double abss;

        public XPipe(Vector bottom, Vector top, double radius, double strength)
        {
            this.bottom = bottom;
            this.top = top;
            this.radius = radius;
            this.strength = strength;
            RecomputeBounds();
        }

        private void RecomputeBounds()
        {
            r2 = radius * radius;
            r2inv = 1.0 / r2;
            abss = 4.0 * r2inv * Abs(strength);
            height = top.Distance(bottom);
        }

        #region IBlobItem members.

        public Bounds Bounds =>
            Bounds.Create(0.0, -radius, -radius, height, radius, radius) + bottom;

        Vector IBounded.Centroid =>
            new Vector(0.5 * (bottom.X + top.X), bottom.Y, bottom.Z);

        double IBlobItem.CentroidX => 0.5 * (bottom.X + top.X);
        double IBlobItem.CentroidY => bottom.Y;
        double IBlobItem.CentroidZ => bottom.Z;
        double IBounded.SquaredRadius => 0.25 * height * height + r2;

        IBlobItem IBlobItem.Clone() => new XPipe(bottom, top, radius, strength);

        void IBlobItem.GetNormal(in Vector location, ref Blob.Normal normal)
        {
            if (location.X >= bottom.X && location.X <= top.X)
            {
                double y = location.Y - bottom.Y, z = location.Z - bottom.Z;
                double len = y * y + z * z;
                if (len <= r2)
                {
                    normal.y += (len = (1.0 - len * r2inv) * abss) * y;
                    normal.z += z * len;
                }
            }
        }

        bool IBlobItem.Hits(Ray ray, ref double t1, ref double t2)
        {
            // Apply the inverse transformation to the incoming ray
            double orgY = ray.Origin.Y - bottom.Y;
            double orgZ = ray.Origin.Z - bottom.Z;
            Vector dir = ray.Direction;
            // Compute time bounds for the YZ plane
            double temp = 1.0 / (1.0 - dir.X * dir.X);
            double beta = -(dir.Y * orgY + dir.Z * orgZ) * temp;
            if ((temp = beta * beta - (orgY * orgY + orgZ * orgZ - r2) * temp) < 0.0)
                return false;
            t1 = beta - (temp = Sqrt(temp));
            t2 = beta + temp;
            // Compute time bounds for the X axis
            if (dir.X >= 0)
            {
                double orgX = bottom.X - ray.Origin.X;
                double t = orgX * (temp = ray.InvDir.X);
                if (t > t1) t1 = t;
                if ((t = (height + orgX) * temp) < t2) t2 = t;
            }
            else
            {
                double orgX = bottom.X - ray.Origin.X;
                double t = (height + orgX) * (temp = ray.InvDir.X);
                if (t > t1) t1 = t;
                if ((t = orgX * temp) < t2) t2 = t;
            }
            if (t1 > t2)
                return false;
            // Compute coefficients.
            double d2 = (1.0 - dir.X * dir.X) * r2inv;
            double g2 = (orgY * orgY + orgZ * orgZ) * r2inv - 1.0;
            double gd = (orgY * dir.Y + orgZ * dir.Z) * r2inv;
            c0 = d2 * (d2 * strength);
            c1 = 4.0 * gd * (d2 * strength);
            c2 = ((d2 + d2) * g2 + 4.0 * gd * gd) * strength;
            c3 = 4.0 * gd * (g2 * strength);
            c4 = g2 * (g2 * strength);
            return true;
        }

        bool IBlobItem.CanRotate(in Matrix rotation) => false;

        bool IBlobItem.CanScale(in Vector factor) => false;

        /// <summary>Translates this blob item.</summary>
        /// <param name="translation">Translation distance.</param>
        /// <returns>This blob item.</returns>
        IBlobItem IBlobItem.ApplyTranslation(in Vector translation)
        {
            bottom += translation;
            top += translation;
            RecomputeBounds();
            return this;
        }

        /// <summary>Rotates this blob item.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The rotated blob item.</returns>
        IBlobItem IBlobItem.ApplyRotation(in Matrix rotation) => this;

        /// <summary>Scales this blob item.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The original XPipe.</returns>
        IBlobItem IBlobItem.ApplyScale(in Vector factor) => this;

        void IBlobItem.Simplify(ICollection<IBlobItem> accumulator) => accumulator.Add(this);

        #endregion
    }

    [Properties("radius", "strength", "bottom", "top")]
    internal sealed class YPipe : BlobItem, IBlobItem
    {
        private Vector bottom, top;
        private readonly double radius;
        private readonly double strength;
        private double height;
        private double r2;
        private double r2inv;
        private double abss;

        public YPipe(Vector bottom, Vector top, double radius, double strength)
        {
            this.bottom = bottom;
            this.top = top;
            this.radius = radius;
            this.strength = strength;
            RecomputeBounds();
        }

        private void RecomputeBounds()
        {
            r2 = radius * radius;
            r2inv = 1.0 / r2;
            abss = 4.0 * r2inv * Abs(strength);
            height = top.Distance(bottom);
        }

        #region IBlobItem members.

        public Bounds Bounds =>
            Bounds.Create(-radius, 0.0, -radius, radius, height, radius) + bottom;

        Vector IBounded.Centroid => new Vector(bottom.X, 0.5 * (bottom.Y + top.Y), bottom.Z);

        double IBlobItem.CentroidX => bottom.X;
        double IBlobItem.CentroidY => 0.5 * (bottom.Y + top.Y);
        double IBlobItem.CentroidZ => bottom.Z;
        double IBounded.SquaredRadius => 0.25 * height * height + r2;

        IBlobItem IBlobItem.Clone() => new YPipe(bottom, top, radius, strength);

        void IBlobItem.GetNormal(in Vector location, ref Blob.Normal normal)
        {
            if (location.Y >= bottom.Y && location.Y <= top.Y)
            {
                double x = location.X - bottom.X, z = location.Z - bottom.Z;
                double len = x * x + z * z;
                if (len <= r2)
                {
                    normal.x += (len = (1.0 - len * r2inv) * abss) * x;
                    normal.z += z * len;
                }
            }
        }

        bool IBlobItem.Hits(Ray ray, ref double t1, ref double t2)
        {
            // Apply the inverse transformation to the incoming ray
            double orgX = ray.Origin.X - bottom.X;
            double orgZ = ray.Origin.Z - bottom.Z;
            Vector dir = ray.Direction;
            // Compute time bounds for the XZ plane
            double temp = 1.0 / (1.0 - dir.Y * dir.Y);
            double beta = -(dir.X * orgX + dir.Z * orgZ) * temp;
            if ((temp = beta * beta - (orgX * orgX + orgZ * orgZ - r2) * temp) < 0.0)
                return false;
            t1 = beta - (temp = Sqrt(temp));
            t2 = beta + temp;
            // Compute time bounds for the Y axis
            if (dir.Y >= 0)
            {
                double orgY = bottom.Y - ray.Origin.Y;
                double t = orgY * (temp = ray.InvDir.Y);
                if (t > t1) t1 = t;
                if ((t = (height + orgY) * temp) < t2) t2 = t;
            }
            else
            {
                double orgY = bottom.Y - ray.Origin.Y;
                double t = (height + orgY) * (temp = ray.InvDir.Y);
                if (t > t1) t1 = t;
                if ((t = orgY * temp) < t2) t2 = t;
            }
            if (t1 > t2)
                return false;
            // Compute coefficients.
            double d2 = (1.0 - dir.Y * dir.Y) * r2inv;
            double g2 = (orgX * orgX + orgZ * orgZ) * r2inv - 1.0;
            double gd = (orgX * dir.X + orgZ * dir.Z) * r2inv;
            c0 = d2 * (d2 * strength);
            c1 = 4.0 * gd * (d2 * strength);
            c2 = ((d2 + d2) * g2 + 4.0 * gd * gd) * strength;
            c3 = 4.0 * gd * (g2 * strength);
            c4 = g2 * (g2 * strength);
            return true;
        }

        bool IBlobItem.CanRotate(in Matrix rotation) => false;

        bool IBlobItem.CanScale(in Vector factor) => false;

        /// <summary>Translates this blob item.</summary>
        /// <param name="translation">Translation distance.</param>
        /// <returns>This blob item.</returns>
        IBlobItem IBlobItem.ApplyTranslation(in Vector translation)
        {
            bottom += translation;
            top += translation;
            RecomputeBounds();
            return this;
        }

        /// <summary>Rotates this blob item.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The rotated blob item.</returns>
        IBlobItem IBlobItem.ApplyRotation(in Matrix rotation) => this;

        /// <summary>Scales this blob item.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The original YPipe.</returns>
        IBlobItem IBlobItem.ApplyScale(in Vector factor) => this;

        void IBlobItem.Simplify(ICollection<IBlobItem> accumulator) => accumulator.Add(this);

        #endregion
    }

    [Properties("radius", "strength", "bottom", "top")]
    internal sealed class ZPipe : BlobItem, IBlobItem
    {
        private Vector bottom, top;
        private readonly double radius;
        private readonly double strength;
        private double height;
        private double r2;
        private double r2inv;
        private double abss;

        public ZPipe(Vector bottom, Vector top, double radius, double strength)
        {
            this.bottom = bottom;
            this.top = top;
            this.radius = radius;
            this.strength = strength;
            RecomputeBounds();
        }

        private void RecomputeBounds()
        {
            r2 = radius * radius;
            r2inv = 1.0 / r2;
            abss = 4.0 * r2inv * Abs(strength);
            height = top.Distance(bottom);
        }

        #region IBlobItem members.

        public Bounds Bounds =>
            Bounds.Create(-radius, -radius, 0.0, radius, radius, height) + bottom;

        Vector IBounded.Centroid => new Vector(0, 0, height / 2) + bottom;
        double IBlobItem.CentroidX => bottom.X;
        double IBlobItem.CentroidY => bottom.Y;
        double IBlobItem.CentroidZ => 0.5 * (bottom.Z + top.Z);
        double IBounded.SquaredRadius => 0.25 * height * height + r2;

        IBlobItem IBlobItem.Clone() => new ZPipe(bottom, top, radius, strength);

        void IBlobItem.GetNormal(in Vector location, ref Blob.Normal normal)
        {
            if (location.Z >= bottom.Z && location.Z <= top.Z)
            {
                double x = location.X - bottom.X, y = location.Y - bottom.Y;
                double len = x * x + y * y;
                if (len <= r2)
                {
                    normal.x += (len = (1.0 - len * r2inv) * abss) * x;
                    normal.y += y * len;
                }
            }
        }

        bool IBlobItem.Hits(Ray ray, ref double t1, ref double t2)
        {
            // Apply the inverse transformation to the incoming ray
            double orgX = ray.Origin.X - bottom.X;
            double orgY = ray.Origin.Y - bottom.Y;
            Vector dir = ray.Direction;
            // Compute time bounds for the XY plane
            double temp = 1.0 / (1.0 - dir.Z * dir.Z);
            double beta = -(dir.Y * orgY + dir.X * orgX) * temp;
            if ((temp = beta * beta - (orgY * orgY + orgX * orgX - r2) * temp) < 0.0)
                return false;
            t1 = beta - (temp = Sqrt(temp));
            t2 = beta + temp;
            // Compute time bounds for the Z axis
            if (dir.Z >= 0)
            {
                double orgZ = bottom.Z - ray.Origin.Z;
                double t = orgZ * (temp = ray.InvDir.Z);
                if (t > t1) t1 = t;
                if ((t = (height + orgZ) * temp) < t2) t2 = t;
            }
            else
            {
                double orgZ = bottom.Z - ray.Origin.Z;
                double t = (height + orgZ) * (temp = ray.InvDir.Z);
                if (t > t1) t1 = t;
                if ((t = orgZ * temp) < t2) t2 = t;
            }
            if (t1 > t2)
                return false;
            // Compute coefficients.
            double d2 = (1.0 - dir.Z * dir.Z) * r2inv;
            double g2 = (orgY * orgY + orgX * orgX) * r2inv - 1.0;
            double gd = (orgY * dir.Y + orgX * dir.X) * r2inv;
            c0 = d2 * (d2 * strength);
            c1 = 4.0 * gd * (d2 * strength);
            c2 = ((d2 + d2) * g2 + 4.0 * gd * gd) * strength;
            c3 = 4.0 * gd * (g2 * strength);
            c4 = g2 * (g2 * strength);
            return true;
        }

        bool IBlobItem.CanRotate(in Matrix rotation) => false;

        bool IBlobItem.CanScale(in Vector factor) => false;

        /// <summary>Translates this blob item.</summary>
        /// <param name="translation">Translation distance.</param>
        /// <returns>This blob item.</returns>
        IBlobItem IBlobItem.ApplyTranslation(in Vector translation)
        {
            bottom += translation;
            top += translation;
            RecomputeBounds();
            return this;
        }

        /// <summary>Rotates this blob item.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The rotated blob item.</returns>
        IBlobItem IBlobItem.ApplyRotation(in Matrix rotation) => this;

        /// <summary>Scales this blob item.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The original ZPipe.</returns>
        IBlobItem IBlobItem.ApplyScale(in Vector factor) => this;

        void IBlobItem.Simplify(ICollection<IBlobItem> accumulator) => accumulator.Add(this);

        #endregion
    }

    /// <summary>An ellipsoidal, anisotropic, field source for a blob.</summary>
    [Properties("strength", "center", "axes")]
    internal sealed class Ellipse : BlobItem, IBlobItem
    {
        private Vector center;
        private Vector axes;
        private readonly Vector inverse;
        private readonly Vector abss;
        private readonly double strength;
        private double r2;

        /// <summary>Creates an anisotropic field source.</summary>
        /// <param name="center">Center of the field.</param>
        /// <param name="axes">Field intensity along axes..</param>
        /// <param name="strength">Field strength.</param>
        public Ellipse(Vector center, Vector axes, double strength)
        {
            this.center = center;
            this.axes = axes;
            inverse = axes.Invert();
            this.strength = strength;
            abss = 4.0 * Abs(strength) * inverse.Scale(inverse);
            double r = Max(Max(axes.X, axes.Y), axes.Z);
            r2 = r * r;
        }

        #region IBlobItem members.

        public Bounds Bounds => new Bounds(-axes, axes) + center;
        Vector IBounded.Centroid => center;
        double IBlobItem.CentroidX => center.X;
        double IBlobItem.CentroidY => center.Y;
        double IBlobItem.CentroidZ => center.Z;
        double IBounded.SquaredRadius => r2;

        /// <summary>Creates an independent copy of this ellipsoid.</summary>
        /// <returns>A copy of the ellipsoid.</returns>
        IBlobItem IBlobItem.Clone() => new Ellipse(center, axes, strength);

        /// <summary>Gets this item's contribution to the field's gradient.</summary>
        /// <param name="location">Point where the gradient will be evaluated.</param>
        /// <param name="normal">An accumulator for the total gradient.</param>
        void IBlobItem.GetNormal(in Vector location, ref Blob.Normal normal)
        {
            double dx = (location.X - center.X) * inverse.X;
            double dy = (location.Y - center.Y) * inverse.Y;
            double dz = (location.Z - center.Z) * inverse.Z;
            double len = dx * dx + dy * dy + dz * dz;
            if (len <= 1.0)
            {
                normal.x += (1.0 - len) * abss.X * dx;
                normal.y += (1.0 - len) * abss.Y * dy;
                normal.z += (1.0 - len) * abss.Z * dz;
            }
        }

        /// <summary>Computes the intersection interval with the area of influence.</summary>
        /// <param name="ray">Ray to test.</param>
        /// <param name="t1">Time for entering the AOI.</param>
        /// <param name="t2">Time for leaving the AOI.</param>
        /// <returns>True, if the ray ever enters the AOI.</returns>
        bool IBlobItem.Hits(Ray ray, ref double t1, ref double t2)
        {
            double x = (ray.Origin.X - center.X) * inverse.X;
            double y = (ray.Origin.Y - center.Y) * inverse.Y;
            double g2 = (ray.Origin.Z - center.Z) * inverse.Z;
            double dx = ray.Direction.X * inverse.X;
            double dy = ray.Direction.Y * inverse.Y;
            double gd = ray.Direction.Z * inverse.Z;
            double d2 = dx * dx + dy * dy + gd * gd;
            gd = gd * g2 + x * dx + y * dy;
            y = gd * gd - d2 * (g2 = x * x + y * y + g2 * g2 - 1.0);
            if (y <= 0.0)
                // When "discr" is zero, the ray is tangent to the ellipsoid.
                // This case can be safely discarded.
                return false;
            else
            {
                // Compute coefficients.
                c0 = d2 * d2 * strength;
                c1 = 4 * gd * d2 * strength;
                c2 = (2 * g2 * d2 + 4 * gd * gd) * strength;
                c3 = 4 * g2 * gd * strength;
                c4 = g2 * g2 * strength;
                // Find intersection times
                t1 = (-gd - (y = Sqrt(y))) * (d2 = 1.0 / d2);
                t2 = (y - gd) * d2;
                return true;
            }
        }

        /// <summary>Adds this ellipsoid item to the parent's item list.</summary>
        /// <param name="accumulator">The ellipsoid.</param>
        void IBlobItem.Simplify(ICollection<IBlobItem> accumulator) => accumulator.Add(this);

        /// <summary>Can we rotate this blob item?</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>Always false, since ellipsoids cannot be rotated.</returns>
        bool IBlobItem.CanRotate(in Matrix rotation) => false;

        /// <summary>Can we scale this blob item?</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>Always true, since ellipsoids can always be scaled.</returns>
        bool IBlobItem.CanScale(in Vector factor) => true;

        /// <summary>Translates this blob item.</summary>
        /// <param name="translation">Translation distance.</param>
        /// <returns>This blob item.</returns>
        IBlobItem IBlobItem.ApplyTranslation(in Vector translation)
        {
            center += translation;
            return this;
        }

        /// <summary>Rotates this blob item.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The rotated blob item.</returns>
        IBlobItem IBlobItem.ApplyRotation(in Matrix rotation)
        {
            center = rotation * center;
            return this;
        }

        /// <summary>Scales this blob item.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The scaled blob item.</returns>
        IBlobItem IBlobItem.ApplyScale(in Vector factor)
        {
            center = center.Scale(factor);
            axes = axes.Scale(factor);
            double r = Max(Max(axes.X, axes.Y), axes.Z);
            r2 = r * r;
            return this;
        }

        #endregion
    }
}

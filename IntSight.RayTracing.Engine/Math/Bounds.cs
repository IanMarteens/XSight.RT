using System;
using System.Runtime.CompilerServices;
using System.Xml;

namespace IntSight.RayTracing.Engine
{
    /// <summary>An axis-aligned parallelepiped.</summary>
    [SkipLocalsInit]
    public readonly struct Bounds
    {
        /// <summary>The empty bounding box.</summary>
        public static Bounds Void { get; } = new Bounds(Kind.Void);

        /// <summary>Internal classification of bounding boxes.</summary>
        private enum Kind : byte
        {
            /// <summary>A regular bounding box.</summary>
            Normal,
            /// <summary>An empty bounding box.</summary>
            Void,
            /// <summary>An infinite bounding box.</summary>
            Universe
        };

        internal readonly double x0, y0, z0;
        internal readonly double x1, y1, z1;
        private readonly Kind kind;

        /// <summary>Creates a void or an infinite bounding box.</summary>
        /// <param name="kind">Kind of special bounding box to create.</param>
        private Bounds(Kind kind)
        {
            x0 = y0 = z0 = x1 = y1 = z1 = 0.0;
            this.kind = kind;
        }

        /// <summary>Creates a finite axis-aligned bounding box.</summary>
        /// <remarks>This constructor checks bounds for inverted ends.</remarks>
        public Bounds(double x0, double y0, double z0, double x1, double y1, double z1)
        {
            if (x0 <= x1) { this.x0 = x0; this.x1 = x1; }
            else { this.x0 = x1; this.x1 = x0; }
            if (y0 <= y1) { this.y0 = y0; this.y1 = y1; }
            else { this.y0 = y1; this.y1 = y0; }
            if (z0 <= z1) { this.z0 = z0; this.z1 = z1; }
            else { this.z0 = z1; this.z1 = z0; }
            kind = Kind.Normal;
        }

        /// <summary>Creates a finite axis-aligned bounding box.</summary>
        /// <remarks>No parameter checking.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Bounds(
            double x0, double y0, double z0, 
            double x1, double y1, double z1, Kind kind) =>
            (this.x0, this.y0, this.z0, this.x1, this.y1, this.z1, this.kind) = 
                (x0, y0, z0, x1, y1, z1, kind);

        /// <summary>Creates a finite axis-aligned bounding box.</summary>
        /// <remarks>Faster than the constructor, since doesn't checks parameters.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Bounds Create(
            double x0, double y0, double z0,
            double x1, double y1, double z1) =>
            new(x0, y0, z0, x1, y1, z1, Kind.Normal);

        /// <summary>Creates a finite axis-aligned bounding box.</summary>
        public Bounds(in Vector v0, in Vector v1)
            : this(v0.X, v0.Y, v0.Z, v1.X, v1.Y, v1.Z) { }

        /// <summary>Initializes a bounding box from a plane.</summary>
        /// <param name="normal">Normal vector.</param>
        /// <param name="offset">Offset from origin of coordinates.</param>
        public Bounds(in Vector normal, double offset)
        {
            x0 = y0 = z0 = double.NegativeInfinity;
            x1 = y1 = z1 = double.PositiveInfinity;
            kind = Kind.Normal;
            if (Tolerance.Zero(normal.Y) && Tolerance.Zero(normal.Z))
                if (normal.X < 0) x0 = offset; else x1 = offset;
            else if (Tolerance.Zero(normal.Z) && Tolerance.Zero(normal.X))
                if (normal.Y < 0) y0 = offset; else y1 = offset;
            else if (Tolerance.Zero(normal.X) && Tolerance.Zero(normal.Y))
                if (normal.Z < 0) z0 = offset; else z1 = offset;
            else
                kind = Kind.Universe;
        }

        /// <summary>Initializes a bounding box from a sphere.</summary>
        /// <param name="center">Sphere's center.</param>
        /// <param name="radius">Sphere's radius.</param>
        /// <returns>The enclosing bounding box.</returns>
        public static Bounds FromSphere(in Vector center, double radius) =>
            Create(
                center.X - radius, center.Y - radius, center.Z - radius,
                center.X + radius, center.Y + radius, center.Z + radius);

        /// <summary>Gets the point at the leftmost lower closer corner.</summary>
        public Vector From => new(x0, y0, z0);

        /// <summary>Gets the point at the rightmost upper farthest corner.</summary>
        public Vector To => new(x1, y1, z1);

        /// <summary>Is this a void bounding box?</summary>
        public bool IsEmpty => kind == Kind.Void;

        /// <summary>Is this a Universe bounding box?</summary>
        public bool IsUniverse => kind == Kind.Universe;

        /// <summary>Is this an infinite bounding box?</summary>
        public bool IsInfinite =>
            kind == Kind.Universe ||
            double.IsInfinity(x0) || double.IsInfinity(y0) ||
            double.IsInfinity(z0) || double.IsInfinity(x1) ||
            double.IsInfinity(y1) || double.IsInfinity(z1);

        /// <summary>Gets the volume enclosed by the bounding box.</summary>
        public double Volume => (x1 - x0) * (y1 - y0) * (z1 - z0);

        /// <summary>Returns the halved area of the bounding box.</summary>
        public double HalfArea
        {
            get
            {
                double dx = x1 - x0, dy = y1 - y0;
                return (dx + dy) * (z1 - z0) + dx * dy;
            }
        }

        /// <summary>Returns the radius of the sphere enclosing these bounds.</summary>
        public double SquaredRadius
        {
            get
            {
                if (kind == Kind.Normal)
                {
                    double dx = x1 - x0, dy = y1 - y0, dz = z1 - z0;
                    return (dx * dx + dy * dy + dz * dz) * 0.25;
                }
                else
                    return kind == Kind.Void ? 0.0 : double.MaxValue;
            }
        }

        /// <summary>Returns the center of the sphere enclosing these bounds.</summary>
        public Vector Center
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0.5 * (x0 + x1), 0.5 * (y0 + y1), 0.5 * (z0 + z1));
        }

        /// <summary>Ray/box intersection test.</summary>
        /// <param name="ray">Ray to intersect.</param>
        /// <param name="maxt">Maximum time allowed.</param>
        /// <returns>True when the ray intersects the box.</returns>
        /// <remarks>Call by union's hit and shadow tests.</remarks>
        public bool Intersects(Ray ray, double maxt)
        {
            double tt0 = 0, t0, t1;
            double org = ray.Origin.X;
            double temp = ray.InvDir.X;
            if (temp >= 0)
            {
                if ((t0 = x0 - org) > 0)
                {
                    if ((tt0 = t0 * temp) > maxt)
                        return false;
                    t1 = (x1 - org) * temp;
                    if (t1 < maxt) maxt = t1;
                }
                else
                {
                    t1 = (x1 - org) * temp;
                    if (t1 < maxt)
                    {
                        if (0 > t1)
                            return false;
                        maxt = t1;
                    }
                }
            }
            else if ((t1 = x1 - org) < 0)
            {
                if ((tt0 = t1 * temp) > maxt)
                    return false;
                t0 = (x0 - org) * temp;
                if (t0 < maxt) maxt = t0;
            }
            else
            {
                t0 = (x0 - org) * temp;
                if (t0 < maxt)
                {
                    if (0 > t0)
                        return false;
                    maxt = t0;
                }
            }
            // We'll check bounds only when bounds are changed, for a premature exit.
            org = ray.Origin.Y;
            temp = ray.InvDir.Y;
            if (temp >= 0)
            {
                if ((t0 = (y0 - org) * temp) > tt0)
                {
                    if ((tt0 = t0) > maxt)
                        return false;
                    if ((t1 = (y1 - org) * temp) < maxt) maxt = t1;
                }
                else if ((t1 = (y1 - org) * temp) < maxt)
                {
                    if (tt0 > t1)
                        return false;
                    maxt = t1;
                }
            }
            else if ((t1 = (y1 - org) * temp) > tt0)
            {
                if ((tt0 = t1) > maxt)
                    return false;
                if ((t0 = (y0 - org) * temp) < maxt) maxt = t0;
            }
            else if ((t0 = (y0 - org) * temp) < maxt)
            {
                if (tt0 > t0)
                    return false;
                maxt = t0;
            }
            // This time, we avoid unnecessary memory writes.
            t0 = (z0 - (org = ray.Origin.Z)) * (temp = ray.InvDir.Z);
            t1 = (z1 - org) * temp;
            return temp >= 0
                ? t0 > tt0 ?
                    (t1 < maxt ? t0 <= t1 : t0 <= maxt) :
                    (t1 < maxt ? tt0 <= t1 : tt0 <= maxt)
                : t1 > tt0 ?
                    (t0 < maxt ? t1 <= t0 : t1 <= maxt) :
                    (t0 < maxt ? tt0 <= t0 : tt0 <= maxt);
        }

        /// <summary>Creates a rotated and translated copy of these bounds.</summary>
        /// <param name="rotation">Rotation matrix.</param>
        /// <param name="translation">Translation distance.</param>
        /// <returns>The transformed bounds.</returns>
        public Bounds Transform(in Matrix rotation, in Vector translation)
        {
            if (kind != Kind.Normal)
                return this;
            if (rotation.Equals(Matrix.Identity))
                return new(From + translation, To + translation);
            ReadOnlySpan<Vector> points = stackalloc[] {
                new(x0, y0, z0),
                new(x0, y0, z1),
                new(x0, y1, z0),
                new(x0, y1, z1),
                new(x1, y0, z0),
                new(x1, y0, z1),
                new(x1, y1, z0),
                new Vector(x1, y1, z1)
            };
            double xx0 = double.PositiveInfinity, yy0 = double.PositiveInfinity,
                zz0 = double.PositiveInfinity, xx1 = double.NegativeInfinity,
                yy1 = double.NegativeInfinity, zz1 = double.NegativeInfinity;
            foreach (Vector p in points)
            {
                Vector v = rotation * p + translation;
                if (v.X < xx0) xx0 = v.X; if (v.X > xx1) xx1 = v.X;
                if (v.Y < yy0) yy0 = v.Y; if (v.Y > yy1) yy1 = v.Y;
                if (v.Z < zz0) zz0 = v.Z; if (v.Z > zz1) zz1 = v.Z;
            }
            return new(xx0, yy0, zz0, xx1, yy1, zz1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareX(in Bounds other) =>
            (x0 + x1).CompareTo(other.x0 + other.x1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareY(in Bounds other) =>
            (y0 + y1).CompareTo(other.y0 + other.y1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareZ(in Bounds other) =>
            (z0 + z1).CompareTo(other.z0 + other.z1);

        /// <summary>Creates a rotated copy of these bounds.</summary>
        /// <param name="rotation">Rotation matrix.</param>
        /// <returns>The transformed bounds.</returns>
        public Bounds Rotate(in Matrix rotation) => Transform(rotation, Vector.Null);

        /// <summary>Creates a scaled version of these bounds.</summary>
        /// <param name="scale">Scale factor.</param>
        /// <returns>The transformed bounds.</returns>
        public Bounds Scale(in Vector scale) =>
            kind != Kind.Normal ? this :
                new Bounds(From.Scale(scale), To.Scale(scale));

        /// <summary>Translates a bounding box.</summary>
        /// <param name="b">The bounding box.</param>
        /// <param name="translation">Translation distance.</param>
        /// <returns>The translated bounds.</returns>
        public static Bounds operator +(in Bounds b, in Vector translation) =>
            b.kind != Kind.Normal ? b :
                Create(
                    b.x0 + translation.X, b.y0 + translation.Y, b.z0 + translation.Z,
                    b.x1 + translation.X, b.y1 + translation.Y, b.z1 + translation.Z);

        /// <summary>Merges two bounding boxes.</summary>
        /// <param name="b1">First box to merge.</param>
        /// <param name="b2">Second box to merge.</param>
        /// <returns>A box enclosing both operands.</returns>
        public static Bounds operator +(in Bounds b1, in Bounds b2) =>
            b1.kind switch
            {
                Kind.Normal => b2.kind switch
                {
                    Kind.Normal => Create(
                        b1.x0 <= b2.x0 ? b1.x0 : b2.x0,
                        b1.y0 <= b2.y0 ? b1.y0 : b2.y0,
                        b1.z0 <= b2.z0 ? b1.z0 : b2.z0,
                        b1.x1 >= b2.x1 ? b1.x1 : b2.x1,
                        b1.y1 >= b2.y1 ? b1.y1 : b2.y1,
                        b1.z1 >= b2.z1 ? b1.z1 : b2.z1),
                    Kind.Universe => b2,
                    _ => b1
                },
                Kind.Universe => b1,
                _ => b2
            };

        /// <summary>Commutative bounding box intersection.</summary>
        /// <param name="b1">First operand.</param>
        /// <param name="b2">Second operand.</param>
        /// <returns>The volume common to both bounds.</returns>
        public static Bounds operator *(in Bounds b1, in Bounds b2)
        {
            switch (b1.kind)
            {
                case Kind.Normal:
                    switch (b2.kind)
                    {
                        case Kind.Normal:
                            {
                                double x0 = b1.x0 <= b2.x0 ? b2.x0 : b1.x0;
                                double y0 = b1.y0 <= b2.y0 ? b2.y0 : b1.y0;
                                double z0 = b1.z0 <= b2.z0 ? b2.z0 : b1.z0;
                                double x1 = b1.x1 >= b2.x1 ? b2.x1 : b1.x1;
                                double y1 = b1.y1 >= b2.y1 ? b2.y1 : b1.y1;
                                double z1 = b1.z1 >= b2.z1 ? b2.z1 : b1.z1;
                                return x0 > x1 || y0 > y1 || z0 > z1
                                    ? new(Kind.Void)
                                    : Create(x0, y0, z0, x1, y1, z1);
                            }
                        case Kind.Universe:
                            return b1;
                        default:
                            return b2;
                    }
                case Kind.Universe:
                    return b2;
                default:
                    return b1;
            }
        }

        /// <summary>Writes the coordinates of the bounding box.</summary>
        /// <param name="writer">XML writer.</param>
        public void WriteXmlAttribute(XmlWriter writer)
        {
            writer.WriteStartAttribute("bounds");
            writer.WriteValue(ToString());
            writer.WriteEndAttribute();
        }

        /// <summary>A textual representation of the bounding box.</summary>
        /// <returns>A string containing the six bounding coordinates.</returns>
        public override string ToString() =>
            kind == Kind.Universe ? "U" :
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0:F3},{1:F3},{2:F3},{3:F3},{4:F3},{5:F3}",
                    x0, y0, z0, x1, y1, z1);

        /// <summary>Is the given point located inside the bounding box?</summary>
        /// <param name="point">Point to check.</param>
        /// <returns>True if the point is inside the box.</returns>
        public bool Contains(in Vector point) =>
            x0 <= point.X && point.X <= x1 &&
            y0 <= point.Y && point.Y <= y1 &&
            z0 <= point.Z && point.Z <= z1;

        /// <summary>Gets the longest edge of the bounding box.</summary>
        public Axis DominantAxis
        {
            get
            {
                double w = x1 - x0;
                double w1 = y1 - y0;
                Axis result;
                if (w1 > w) { result = Axis.Y; w = w1; } else result = Axis.X;
                return (z1 - z0 > w) ? Axis.Z : result;
            }
        }
    }
}

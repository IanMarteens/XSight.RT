namespace IntSight.RayTracing.Engine
{
    /// <summary>Represents a flat triangle.</summary>
    [XSight]
    [Properties(nameof(a), nameof(b), nameof(c), nameof(material))]
    public sealed class Triangle : MaterialShape, IShape
    {
        private Vector a, b, c;
        private Vector normal, negatedNormal;
        private double squaredRadius;
        private bool negated;

        public Triangle(Vector a, Vector b, Vector c, IMaterial material)
            : base(material)
        {
            this.a = a;
            this.b = a - b;
            this.c = a - c;
            RecomputeBounds();
        }

        public Triangle(
            double ax, double ay, double az,
            double bx, double by, double bz,
            double cx, double cy, double cz,
            IMaterial material)
            : this(new(ax, ay, az), new(bx, by, bz), new(cx, cy, cz), material) { }

        private void RecomputeBounds()
        {
            normal = (b ^ c).Norm();
            negatedNormal = -normal;
            bounds = new Bounds(a, a - b) + new Bounds(a, a - c);
            // Now precalculate the circumsphere.
            // There may be a more efficient way, but this code is not critical.
            double da = (b - c).Length, db = c.Length, dc = b.Length;
            double r = da * db * dc;
            squaredRadius = r /
                ((da + db + dc) * (db + dc - da) * (dc + da - db) * (da + db - dc)) * r;
        }

        public override double SquaredRadius => squaredRadius;

        #region IShape members

        /// <summary>Checks whether a given ray intersects the shape.</summary>
        /// <param name="ray">Ray emitted by a light source.</param>
        /// <returns>True, when such an intersection exists.</returns>
        bool IShape.ShadowTest(Ray ray)
        {
            Vector aOrg = a - ray.Origin;
            Vector eihf = c ^ ray.Direction;
            double denm = 1.0 / (eihf * b);
            double beta = (eihf * aOrg) * denm;
            if (beta <= 0.0 || beta >= 1.0)
                return false;

            Vector blkc = b ^ aOrg;
            double gamma = (blkc * ray.Direction) * denm;
            if (gamma <= 0.0 || beta + gamma >= 1.0)
                return false;

            double t = -(blkc * c) * denm;
            return t >= Tolerance.Epsilon && t <= 1.0;
        }

        /// <summary>Test intersection with a given ray.</summary>
        /// <param name="ray">Ray to be tested (direction is always normalized).</param>
        /// <param name="maxt">Upper bound for the intersection time.</param>
        /// <param name="info">Hit information, when an intersection is found.</param>
        /// <returns>True when an intersection is found.</returns>
        bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
        {
            Vector aOrg = a - ray.Origin;
            Vector eihf = c ^ ray.Direction;
            double denm = 1.0 / (eihf * b);
            double beta = (eihf * aOrg) * denm;
            if (beta <= 0.0 || beta >= 1.0)
                return false;

            Vector blkc = b ^ aOrg;
            double gamma = (blkc * ray.Direction) * denm;
            if (gamma <= 0.0 || beta + gamma >= 1.0)
                return false;

            double t = -(blkc * c) * denm;
            if (t >= Tolerance.Epsilon && t <= maxt)
            {
                info.Time = t;
                info.HitPoint = ray[t];
                info.Normal = ray.Direction * normal < 0.0 ? normal : negatedNormal;
                info.Material = material;
                return true;
            }
            return false;
        }

        /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
        /// <param name="ray">Ray to be tested.</param>
        /// <param name="hits">Preallocated array of intersections.</param>
        /// <returns>Number of found intersections.</returns>
        int IShape.GetHits(Ray ray, Hit[] hits) => 0;

        /// <summary>Computes the surface normal at a given location.</summary>
        /// <param name="location">Point to be tested.</param>
        /// <returns>Normal vector at that point.</returns>
        Vector IShape.GetNormal(in Vector location) => negated ? negatedNormal : normal;

#endregion

#region ITransformable members

        /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
        int ITransformable.MaxHits => 2;

        /// <summary>Gets the estimated cost.</summary>
        ShapeCost ITransformable.Cost => ShapeCost.EasyPeasy;

        /// <summary>Invert normals for the right operand in a difference.</summary>
        void ITransformable.Negate() => negated = !negated;

        /// <summary>Creates a new independent copy of the whole shape.</summary>
        /// <param name="force">True when a new copy is needed.</param>
        /// <returns>The new shape.</returns>
        IShape ITransformable.Clone(bool force)
        {
            IMaterial m = material.Clone(force);
            if (force || m != material)
            {
                IShape t = new Triangle(a, b, c, m);
                if (negated) t.Negate();
                return t;
            }
            else
                return this;
        }

        /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The cost of the transformation.</returns>
        public override TransformationCost CanRotate(in Matrix rotation) => TransformationCost.Ok;

        /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The cost of the transformation.</returns>
        public override TransformationCost CanScale(in Vector factor) => TransformationCost.Ok;

        /// <summary>Translate this shape.</summary>
        /// <param name="translation">Translation amount.</param>
        public override void ApplyTranslation(in Vector translation)
        {
            a += translation;
            bounds += translation;
            material = material.Translate(translation);
        }

        /// <summary>Rotate this shape.</summary>
        /// <param name="rotation">Rotation amount.</param>
        public override void ApplyRotation(in Matrix rotation)
        {
            Vector a1 = rotation * a;
            b = a1 - rotation * (a - b);
            c = a1 - rotation * (a - c);
            a = a1;
            RecomputeBounds();
        }

        /// <summary>Scale this shape.</summary>
        /// <param name="factor">Scale factor.</param>
        public override void ApplyScale(in Vector factor)
        {
            Vector a1 = a.Scale(factor);
            b = a1 - (a - b).Scale(factor);
            c = a1 - (a - c).Scale(factor);
            a = a1;
            RecomputeBounds();
        }

#endregion
    }
}
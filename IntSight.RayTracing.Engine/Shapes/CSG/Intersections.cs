using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Rsc = IntSight.RayTracing.Engine.Properties.Resources;

namespace IntSight.RayTracing.Engine
{
    /// <summary>Common base class for all intersections.</summary>
    public abstract class IntersectionBase : MaterialShape
    {
        protected IShape shape0;
        protected IShape[] shapes;
        protected Vector centroid;
        protected double squaredRadius;

        /// <summary>Initializes an intersection.</summary>
        /// <param name="shape">First shape to intersect.</param>
        /// <param name="shapes">Other shapes in intersection.</param>
        /// <param name="material">Material the intersection is made of.</param>
        protected IntersectionBase(IShape shape, IShape[] shapes, IMaterial material)
            : base(material)
        {
            shape0 = shape;
            this.shapes = shapes;
            RecomputeBounds();
        }

        /// <summary>Initializes an intersection.</summary>
        /// <param name="shapes">Shapes in intersection.</param>
        /// <param name="material">Material the intersection is made of.</param>
        protected IntersectionBase(IShape[] shapes, IMaterial material)
            : base(material)
        {
            shape0 = shapes[0];
            this.shapes = new IShape[shapes.Length - 1];
            Array.Copy(shapes, 1, this.shapes, 0, this.shapes.Length);
            RecomputeBounds();
        }

        /// <summary>Updates the bounding box and the bounding sphere around the union.</summary>
        protected virtual void RecomputeBounds()
        {
            bounds = shape0.Bounds;
            centroid = shape0.Centroid;
            squaredRadius = shape0.SquaredRadius;
            foreach (IShape s in shapes)
            {
                bounds *= s.Bounds;
                if (!s.Bounds.IsInfinite)
                    Intersect(ref centroid, ref squaredRadius, s);
            }
            bounds *= Bounds.FromSphere(centroid, Math.Sqrt(squaredRadius));
            if (bounds.SquaredRadius < squaredRadius)
            {
                centroid = bounds.Center;
                squaredRadius = bounds.SquaredRadius;
            }
            Rebind();
        }

        /// <summary>Resets direct references to shapes after changes.</summary>
        /// <remarks>Overriden by binary intersections.</remarks>
        protected virtual void Rebind() { }

        /// <summary>The center of the bounding sphere.</summary>
        public override Vector Centroid => centroid;

        /// <summary>The square radius of the bounding sphere.</summary>
        public override double SquaredRadius => squaredRadius;

        /// <summary>Estimated complexity.</summary>
        public virtual ShapeCost Cost => ShapeCost.PainInTheNeck;

        /// <summary>Invert normals for the right operand in a difference.</summary>
        public void Negate()
        {
            shape0.Negate();
            foreach (IShape s in shapes)
                s.Negate();
        }

        /// <summary>Gets the normal vector given a point in the surface.</summary>
        /// <param name="location">Hit point.</param>
        /// <returns>Normal vector.</returns>
        /// <remarks>
        /// As a rule, CSG operations do not implement IShape.GetNormal, since their
        /// IShape.GetHits implementations honors the original shape the hit point belongs.
        /// </remarks>
        public virtual Vector GetNormal(in Vector location) =>
            throw new RenderException(Rsc.NotImplemented, "Intersection.GetNormal");

        /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
        public int MaxHits => Math.Max(shape0.MaxHits, shapes.Max(s => s.MaxHits));

        /// <summary>Second optimization pass, where special shapes are introduced.</summary>
        /// <returns>This shape, or a new and more efficient equivalent.</returns>
        public override IShape Substitute()
        {
            shape0 = shape0.Substitute();
            for (int i = 0; i < shapes.Length; i++)
                shapes[i] = shapes[i].Substitute();
            if (shapes.Length > 1 || !shapes[0].Bounds.IsInfinite)
                RecomputeBounds();
            return (IShape)this;
        }

        /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
        /// <param name="scene">The scene this intersection is included within.</param>
        /// <param name="inCsg">Is this shape included in a CSG operation?</param>
        /// <param name="inTransform">Is this shape nested inside a transform?</param>
        public override void Initialize(IScene scene, bool inCsg, bool inTransform)
        {
            shape0.Initialize(scene, true, inTransform);
            foreach (IShape s in shapes)
                s.Initialize(scene, true, inTransform);
        }

        /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The cost of the transformation.</returns>
        public override TransformationCost CanRotate(in Matrix rotation)
        {
            int problems = 0;
            if (!shape0.CheckRotation(rotation, ref problems))
                return TransformationCost.Nope;
            foreach (IShape shape in shapes)
                if (!shape.CheckRotation(rotation, ref problems))
                    return TransformationCost.Nope;
            return problems <= 1 ? TransformationCost.Ok : TransformationCost.Nope;
        }

        /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The cost of the transformation.</returns>
        public override TransformationCost CanScale(in Vector factor)
        {
            int problems = 0;
            if (!shape0.CheckScale(factor, ref problems))
                return TransformationCost.Nope;
            foreach (IShape shape in shapes)
                if (!shape.CheckScale(factor, ref problems))
                    return TransformationCost.Nope;
            return problems <= 1 ? TransformationCost.Ok : TransformationCost.Nope;
        }

        /// <summary>Translates this intersection.</summary>
        /// <param name="translation">Translation distance.</param>
        public override void ApplyTranslation(in Vector translation)
        {
            shape0.ApplyTranslation(translation);
            foreach (IShape shape in shapes)
                shape.ApplyTranslation(translation);
            bounds += translation;
            centroid += translation;
        }

        /// <summary>Rotates this intersection.</summary>
        /// <param name="rotation">Rotation amount.</param>
        public override void ApplyRotation(in Matrix rotation)
        {
            DoRotation(ref shape0, rotation);
            for (int i = 0; i < shapes.Length; i++)
                DoRotation(ref shapes[i], rotation);
            RecomputeBounds();
        }

        /// <summary>Scales this intersection.</summary>
        /// <param name="factor">Scale factor.</param>
        public override void ApplyScale(in Vector factor)
        {
            DoScale(ref shape0, factor);
            for (int i = 0; i < shapes.Length; i++)
                DoScale(ref shapes[i], factor);
            RecomputeBounds();
        }
    }

    /// <summary>General purpose intersection.</summary>
    [XSight(Alias = "inter"), Properties(nameof(squaredRadius))]
    [Children(nameof(shape0), nameof(shapes))]
    public sealed class Intersection : IntersectionBase, IShape
    {
        private Hit[] hits0, hits1, accum;

        /// <summary>Creates an intersection from two shapes.</summary>
        /// <param name="shape0">First shape to intersect.</param>
        /// <param name="shape1">Second shape to intersect.</param>
        /// <param name="material">Material this intersection is made of.</param>
        public Intersection(IShape shape0, IShape shape1, IMaterial material)
            : base(shape0, new IShape[] { shape1 }, material)
        {
            int maxHits = MaxHits;
            hits0 = new Hit[maxHits];
            hits1 = new Hit[maxHits];
            accum = new Hit[maxHits];
        }

        /// <summary>Creates an intersection from two shapes.</summary>
        /// <param name="shape0">First shape to intersect.</param>
        /// <param name="shape1">Second shape to intersect.</param>
        public Intersection(IShape shape0, IShape shape1)
            : this(shape0, shape1, ((MaterialShape)shape0).Material) { }

        /// <summary>Creates an intersection.</summary>
        /// <param name="shapes">Shapes to intersect.</param>
        /// <param name="material">Material this intersection is made of.</param>
        public Intersection(IShape[] shapes, IMaterial material)
            : base(shapes, material)
        {
            int maxHits = MaxHits;
            hits0 = new Hit[maxHits];
            hits1 = new Hit[maxHits];
            accum = new Hit[maxHits];
        }

        private double FindRoot(Ray ray, ref IShape shape)
        {
            int total0 = shape0.GetHits(ray, hits0);
            if (total0 == 0)
                return -1.0;
            foreach (IShape s in shapes)
            {
                int total1 = s.GetHits(ray, hits1);
                if (total1 == 0)
                    return -1.0;
                int i0 = 0, i1 = 0, t = 0;
                bool inside0 = false, inside1 = false;
            LOOP:
                if (hits0[i0].Time <= hits1[i1].Time)
                {
                    inside0 = !inside0;
                    if (inside1)
                        accum[t++] = hits0[i0];
                    if (++i0 < total0) goto LOOP;
                }
                else
                {
                    inside1 = !inside1;
                    if (inside0)
                        accum[t++] = hits1[i1];
                    if (++i1 < total1) goto LOOP;
                }
                if (t == 0)
                    return -1.0;
                total0 = t;
                Hit[] temp = accum; accum = hits0; hits0 = temp;
            }
            for (int i = 0; i < total0; i++)
            {
                double t = hits0[i].Time;
                if (t > Tolerance.Epsilon)
                {
                    shape = hits0[i].Shape;
                    return t;
                }
            }
            return -1.0;
        }

        #region IShape members.

        /// <summary>Checks whether a given ray intersects the shape.</summary>
        /// <param name="ray">Ray emitted by a light source.</param>
        /// <returns>True, when such an intersection exists.</returns>
        bool IShape.ShadowTest(Ray ray)
        {
            IShape shape = null;
            double t = FindRoot(ray, ref shape);
            return 0.0 <= t && t <= 1.0;
        }

        /// <summary>Test intersection with a given ray.</summary>
        /// <param name="ray">Ray to be tested (direction is always normalized).</param>
        /// <param name="maxt">Upper bound for the intersection time.</param>
        /// <param name="info">Hit information, when an intersection is found.</param>
        /// <returns>True when an intersection is found.</returns>
        bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
        {
            IShape shape = null;
            double t = FindRoot(ray, ref shape);
            if (0.0 <= t && t <= maxt)
            {
                info.Time = t;
                info.HitPoint = ray[t];
                info.Normal = shape.GetNormal(info.HitPoint);
                info.Material = material;
                return true;
            }
            else
                return false;
        }

        /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
        /// <param name="ray">Ray to be tested.</param>
        /// <param name="hits">Preallocated array of intersections.</param>
        /// <returns>Number of found intersections.</returns>
        int IShape.GetHits(Ray ray, Hit[] hits)
        {
            int total0 = shape0.GetHits(ray, hits0);
            if (total0 == 0)
                return 0;
            foreach (IShape s in shapes)
            {
                int total1 = s.GetHits(ray, hits1);
                if (total1 == 0)
                    return 0;
                int i0 = 0, i1 = 0, t = 0;
                bool inside0 = false, inside1 = false;
            LOOP:
                if (hits0[i0].Time <= hits1[i1].Time)
                {
                    inside0 = !inside0;
                    if (inside1)
                        accum[t++] = hits0[i0];
                    if (++i0 < total0) goto LOOP;
                }
                else
                {
                    inside1 = !inside1;
                    if (inside0)
                        accum[t++] = hits1[i1];
                    if (++i1 < total1) goto LOOP;
                }
                if (t == 0)
                    return 0;
                total0 = t;
                Hit[] temp = accum; accum = hits0; hits0 = temp;
            }
            Array.Copy(hits0, hits, total0);
            return total0;
        }

        #endregion

        #region ITransformable members.

        /// <summary>Creates a new independent copy of the whole shape.</summary>
        /// <param name="force">True when a new copy is needed.</param>
        /// <returns>The new shape.</returns>
        IShape ITransformable.Clone(bool force)
        {
            IShape[] newShapes = new IShape[shapes.Length + 1];
            newShapes[0] = shape0.Clone(force);
            for (int i = 0; i < shapes.Length; i++)
                newShapes[i + 1] = shapes[i].Clone(force);
            return new Intersection(newShapes, material.Clone(force));
        }

        /// <summary>First optimization pass.</summary>
        /// <returns>This shape, or a new and more efficient equivalent.</returns>
        public override IShape Simplify()
        {
            int extraItems = 0;
            shape0 = shape0.Simplify();
            if (shape0 is Intersection intersection)
                extraItems = intersection.shapes.Length;
            for (int i = 0; i < shapes.Length; i++)
            {
                IShape s = shapes[i].Simplify();
                shapes[i] = s;
                if (s is Intersection intersect)
                    extraItems += intersect.shapes.Length;
            }
            if (extraItems > 0)
            {
                IShape[] newItems = new IShape[shapes.Length + extraItems];
                int i = 0;
                if (shape0 is Intersection intersection1)
                {
                    foreach (IShape s in intersection1.shapes)
                        newItems[i++] = s;
                    shape0 = intersection1.shape0;
                }
                foreach (IShape s1 in shapes)
                {
                    if (s1 is not Intersection intersect)
                        newItems[i++] = s1;
                    else
                    {
                        newItems[i++] = intersect.shape0;
                        foreach (IShape s2 in intersect.shapes)
                            newItems[i++] = s2;
                    }
                }
                shapes = newItems;
            }
            // Intersections with two convex operands can be optimized with a special class.
            if (shapes.Length == 1)
                return MaxHits == 2
                    ? new Inter2Convex(shape0, shapes, material)
                    : (IShape)new Inter2(shape0, shapes, material);
            else
            {
                int maxHits = MaxHits;
                if (maxHits == 2)
                    return new InterConvex(shape0, shapes, material);
                if (maxHits != hits0.Length)
                {
                    hits0 = new Hit[maxHits];
                    hits1 = new Hit[maxHits];
                    accum = new Hit[maxHits];
                }
                RecomputeBounds();
                return this;
            }
        }

        #endregion
    }

    /// <summary>Specialized intersection for arbitrary convex operands.</summary>
    [Properties(nameof(squaredRadius))]
    [Children(nameof(shape0), nameof(shapes))]
    internal sealed class InterConvex : IntersectionBase, IShape
    {
        private readonly Hit[] hits0;

        public InterConvex(IShape shape0, IShape[] shapes, IMaterial material)
            : base(shape0, shapes, material) => hits0 = new Hit[2];

        #region IShape members.

        /// <summary>Checks whether a given ray intersects the shape.</summary>
        /// <param name="ray">Ray emitted by a light source.</param>
        /// <returns>True, when such an intersection exists.</returns>
        bool IShape.ShadowTest(Ray ray)
        {
            if (shape0.GetHits(ray, hits0) == 0)
                return false;
            double h1 = hits0[1].Time, h0 = hits0[0].Time;
            foreach (IShape s in shapes)
            {
                if (s.GetHits(ray, hits0) == 0)
                    return false;
                if (hits0[1].Time < h1)
                {
                    h1 = hits0[1].Time;
                    if (hits0[0].Time > h0)
                        h0 = hits0[0].Time;
                    if (h0 > h1)
                        return false;
                }
                else if (hits0[0].Time > h0)
                {
                    h0 = hits0[0].Time;
                    if (h0 > h1)
                        return false;
                }
            }
            if (h0 >= Tolerance.Epsilon)
                return h0 <= 1.0;
            if (h1 >= Tolerance.Epsilon)
                return h1 <= 1.0;
            return false;
        }

        /// <summary>Test intersection with a given ray.</summary>
        /// <param name="ray">Ray to be tested (direction is always normalized).</param>
        /// <param name="maxt">Upper bound for the intersection time.</param>
        /// <param name="info">Hit information, when an intersection is found.</param>
        /// <returns>True when an intersection is found.</returns>
        bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
        {
            if (shape0.GetHits(ray, hits0) == 0)
                return false;
            Hit h1 = hits0[1], h0 = hits0[0];
            foreach (IShape s in shapes)
            {
                if (s.GetHits(ray, hits0) == 0)
                    return false;
                if (hits0[1].Time < h1.Time)
                {
                    h1 = hits0[1];
                    if (hits0[0].Time > h0.Time)
                        h0 = hits0[0];
                    if (h0.Time > h1.Time)
                        return false;
                }
                else if (hits0[0].Time > h0.Time)
                {
                    h0 = hits0[0];
                    if (h0.Time > h1.Time)
                        return false;
                }
            }
            if (h0.Time >= Tolerance.Epsilon)
            {
                double t = h0.Time;
                if (0.0 <= t && t <= maxt)
                {
                    info.Time = t;
                    info.HitPoint = ray[t];
                    info.Normal = h0.Shape.GetNormal(info.HitPoint);
                    info.Material = material;
                    return true;
                }
            }
            else if (h1.Time >= Tolerance.Epsilon)
            {
                double t = h1.Time;
                if (0.0 <= t && t <= maxt)
                {
                    info.Time = t;
                    info.HitPoint = ray[t];
                    info.Normal = h1.Shape.GetNormal(info.HitPoint);
                    info.Material = material;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
        /// <param name="ray">Ray to be tested.</param>
        /// <param name="hits">Preallocated array of intersections.</param>
        /// <returns>Number of found intersections.</returns>
        int IShape.GetHits(Ray ray, Hit[] hits)
        {
            if (shape0.GetHits(ray, hits0) == 0)
                return 0;
            Hit h1 = hits0[1], h0 = hits0[0];
            foreach (IShape s in shapes)
            {
                if (s.GetHits(ray, hits0) == 0)
                    return 0;
                if (hits0[1].Time < h1.Time)
                    h1 = hits0[1];
                if (hits0[0].Time > h0.Time)
                    h0 = hits0[0];
                if (h0.Time > h1.Time)
                    return 0;
            }
            hits[1] = h1;
            hits[0] = h0;
            return 2;
        }

        #endregion

        #region ITransformable members.

        /// <summary>Creates a new independent copy of the whole shape.</summary>
        /// <param name="force">True when a new copy is needed.</param>
        /// <returns>The new shape.</returns>
        IShape ITransformable.Clone(bool force)
        {
            IShape[] newShapes = new IShape[shapes.Length];
            for (int i = 0; i < newShapes.Length; i++)
                newShapes[i] = shapes[i].Clone(force);
            return new InterConvex(shape0.Clone(force), newShapes, material.Clone(force));
        }

        #endregion
    }

    /// <summary>Specialized intersection for two convex or concave operands.</summary>
    [Properties(nameof(squaredRadius)), Children(nameof(shape0), nameof(shape1))]
    internal sealed class Inter2 : IntersectionBase, IShape
    {
        private IShape shape1;
        private readonly Hit[] hits0;
        private readonly Hit[] hits1;

        /// <summary>Creates a binary intersection with two arbitrary operands.</summary>
        /// <param name="shape0">First shape.</param>
        /// <param name="shapes">A single-item array with the second shape.</param>
        /// <param name="material">The material this shape is made of.</param>
        public Inter2(IShape shape0, IShape[] shapes, IMaterial material)
            : base(shape0, shapes, material)
        {
            int maxHits = MaxHits;
            hits0 = new Hit[maxHits];
            hits1 = new Hit[maxHits];
        }

        /// <summary>Resets direct references to shapes after changes.</summary>
        protected override void Rebind() => shape1 = shapes[0];

        #region IShape members.

        /// <summary>Checks whether a given ray intersects the shape.</summary>
        /// <param name="ray">Ray emitted by a light source.</param>
        /// <returns>True, when such an intersection exists.</returns>
        bool IShape.ShadowTest(Ray ray)
        {
            int total0 = shape0.GetHits(ray, hits0);
            if (total0 == 0)
                return false;
            int total1 = shape1.GetHits(ray, hits1);
            if (total1 == 0)
                return false;
            int i0 = 0, i1 = 0;
            bool inside0 = false, inside1 = false;
        LOOP:
            if (hits0[i0].Time <= hits1[i1].Time)
            {
                inside0 = !inside0;
                if (inside1)
                {
                    double t = hits0[i0].Time;
                    if (t >= Tolerance.Epsilon)
                        return t <= 1.0;
                }
                if (++i0 < total0)
                    goto LOOP;
            }
            else
            {
                inside1 = !inside1;
                if (inside0)
                {
                    double t = hits1[i1].Time;
                    if (t >= Tolerance.Epsilon)
                        return t <= 1.0;
                }
                if (++i1 < total1)
                    goto LOOP;
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
            int total0 = shape0.GetHits(ray, hits0);
            if (total0 == 0)
                return false;
            int total1 = shape1.GetHits(ray, hits1);
            if (total1 == 0)
                return false;
            int i0 = 0, i1 = 0;
            bool inside0 = false, inside1 = false;
        LOOP:
            if (hits0[i0].Time <= hits1[i1].Time)
            {
                inside0 = !inside0;
                if (inside1)
                {
                    Hit h = hits0[i0];
                    if (h.Time >= Tolerance.Epsilon)
                        if (h.Time > maxt)
                            return false;
                        else
                        {
                            info.Time = h.Time;
                            info.HitPoint = ray[h.Time];
                            info.Normal = h.Shape.GetNormal(info.HitPoint);
                            info.Material = material;
                            return true;
                        }
                }
                if (++i0 < total0)
                    goto LOOP;
            }
            else
            {
                inside1 = !inside1;
                if (inside0)
                {
                    Hit h = hits1[i1];
                    if (h.Time >= Tolerance.Epsilon)
                        if (h.Time > maxt)
                            return false;
                        else
                        {
                            info.Time = h.Time;
                            info.HitPoint = ray[h.Time];
                            info.Normal = h.Shape.GetNormal(info.HitPoint);
                            info.Material = material;
                            return true;
                        }
                }
                if (++i1 < total1)
                    goto LOOP;
            }
            return false;
        }

        /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
        /// <param name="ray">Ray to be tested.</param>
        /// <param name="hits">Preallocated array of intersections.</param>
        /// <returns>Number of found intersections.</returns>
        int IShape.GetHits(Ray ray, Hit[] hits)
        {
            int total0 = shape0.GetHits(ray, hits0);
            if (total0 == 0)
                return 0;
            int total1 = shape1.GetHits(ray, hits1);
            if (total1 == 0)
                return 0;
            int i0 = 0, i1 = 0, total = 0;
            bool inside0 = false, inside1 = false;
        LOOP:
            if (hits0[i0].Time <= hits1[i1].Time)
            {
                inside0 = !inside0;
                if (inside1)
                    hits[total++] = hits0[i0];
                if (++i0 < total0) goto LOOP;
            }
            else
            {
                inside1 = !inside1;
                if (inside0)
                    hits[total++] = hits1[i1];
                if (++i1 < total1) goto LOOP;
            }
            return total;
        }

        #endregion

        #region ITransformable members.

        /// <summary>Creates a new independent copy of the whole shape.</summary>
        /// <param name="force">True when a new copy is needed.</param>
        /// <returns>The new shape.</returns>
        IShape ITransformable.Clone(bool force) =>
            new Inter2(
                shape0.Clone(force),
                new IShape[] { shape1.Clone(force) },
                material.Clone(force));

        #endregion
    }

    /// <summary>Specialized intersection for two convex operands.</summary>
    [Properties(nameof(squaredRadius))]
    [Children(nameof(shape0), nameof(shape1))]
    internal sealed class Inter2Convex : IntersectionBase, IShape
    {
        private IShape shape1;
        private readonly Hit[] hits;

        /// <summary>Creates a binary intersection with two convex operands.</summary>
        /// <param name="shape0">First convex shape.</param>
        /// <param name="shapes">A single-item array with the second convex shape.</param>
        /// <param name="material">The material this shape is made of.</param>
        public Inter2Convex(IShape shape0, IShape[] shapes, IMaterial material)
            : base(shape0, shapes, material) => hits = new Hit[2];

        /// <summary>Resets direct references to shapes after changes.</summary>
        protected override void Rebind() => shape1 = shapes[0];

        #region IShape members.

        /// <summary>Checks whether a given ray intersects the shape.</summary>
        /// <param name="ray">Ray emitted by a light source.</param>
        /// <returns>True, when such an intersection exists.</returns>
        bool IShape.ShadowTest(Ray ray)
        {
            if (shape0.GetHits(ray, hits) > 0)
            {
                double t0 = hits[0].Time, t1 = hits[1].Time;
                if (shape1.GetHits(ray, hits) > 0)
                {
                    if (t1 > hits[1].Time)
                        t1 = hits[1].Time;
                    if (t0 < hits[0].Time)
                        t0 = hits[0].Time;
                    if (t0 <= t1)
                    {
                        if (t0 >= Tolerance.Epsilon)
                            return t0 <= 1.0;
                        if (t1 >= Tolerance.Epsilon)
                            return t1 <= 1.0;
                    }
                }
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
            if (shape0.GetHits(ray, hits) == 0)
                return false;
            Hit h1 = hits[1], h0 = hits[0];
            if (shape1.GetHits(ray, hits) == 0)
                return false;
            if (h1.Time > hits[1].Time)
                h1 = hits[1];
            if (h0.Time < hits[0].Time)
                h0 = hits[0];
            if (h0.Time <= h1.Time)
                if (h0.Time >= Tolerance.Epsilon)
                {
                    if (h0.Time <= maxt)
                    {
                        info.Time = h0.Time;
                        info.HitPoint = ray[h0.Time];
                        info.Normal = h0.Shape.GetNormal(info.HitPoint);
                        info.Material = material;
                        return true;
                    }
                }
                else if (h1.Time >= Tolerance.Epsilon)
                {
                    if (h1.Time <= maxt)
                    {
                        info.Time = h1.Time;
                        info.HitPoint = ray[h1.Time];
                        info.Normal = h1.Shape.GetNormal(info.HitPoint);
                        info.Material = material;
                        return true;
                    }
                }
            return false;
        }

        /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
        /// <param name="ray">Ray to be tested.</param>
        /// <param name="hits">Preallocated array of intersections.</param>
        /// <returns>Number of found intersections.</returns>
        int IShape.GetHits(Ray ray, Hit[] hits)
        {
            if (shape0.GetHits(ray, hits) == 0 ||
                shape1.GetHits(ray, this.hits) == 0)
                return 0;
            else
            {
                if (hits[1].Time > this.hits[1].Time)
                    hits[1] = this.hits[1];
                if (hits[0].Time < this.hits[0].Time)
                    hits[0] = this.hits[0];
                return hits[1].Time < hits[0].Time ? 0 : 2;
            }
        }

        #endregion

        #region ITransformable members.

        /// <summary>Creates a new independent copy of the whole shape.</summary>
        /// <param name="force">True when a new copy is needed.</param>
        /// <returns>The new shape.</returns>
        IShape ITransformable.Clone(bool force) =>
            new Inter2Convex(
                shape0.Clone(force),
                new IShape[] { shape1.Clone(force) },
                material.Clone(force));

        #endregion
    }
}
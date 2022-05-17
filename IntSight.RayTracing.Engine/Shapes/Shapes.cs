using System;

namespace IntSight.RayTracing.Engine
{
    /// <summary>Common base class for all shapes.</summary>
    public abstract class Shape
    {
        /// <summary>Merges two bounding spheres.</summary>
        /// <param name="c1">The center of the first bounding sphere.</param>
        /// <param name="r1">The radius of the first bounding sphere.</param>
        /// <param name="bounded">A bounding sphere.</param>
        /// <returns>The radius and center of the resulting bounding sphere.</returns>
        protected internal static (Vector center, double radius2) Merge(
            in Vector c1,
            double r1,
            IBounded bounded)
        {
            if (r1 < 0.0)
                return (bounded.Centroid, bounded.SquaredRadius);
            double dist = c1.Distance(bounded.Centroid);
            double r1r = Math.Sqrt(r1), r2r = Math.Sqrt(bounded.SquaredRadius);
            if (dist + r1r <= r2r)
                return (bounded.Centroid, bounded.SquaredRadius);
            if (dist + r2r <= r1r)
                return (c1, r1);
            double k1 = ((r1r - r2r) / dist + 1.0) * 0.5;
            double k2 = ((r2r - r1r) / dist + 1.0) * 0.5;
            dist = (dist + r1r + r2r) * 0.5;
            return (k1 * c1 + k2 * bounded.Centroid, dist * dist);
        }

        /// <summary>Finds the combined radius of two bounding spheres.</summary>
        /// <param name="b1">First bounding sphere.</param>
        /// <param name="b2">Second bounding sphere.</param>
        /// <returns>The radius of the resulting bounding sphere.</returns>
        protected internal static double Merge(IBounded b1, IBounded b2)
        {
            if (b1.SquaredRadius < 0.0)
                return b2.SquaredRadius;
            double dist = b1.Centroid.Distance(b2.Centroid);
            double r1r = Math.Sqrt(b1.SquaredRadius);
            double r2r = Math.Sqrt(b2.SquaredRadius);
            if (dist + r1r <= r2r)
                return b2.SquaredRadius;
            if (dist + r2r <= r1r)
                return b1.SquaredRadius;
            dist = (dist + r1r + r2r) * 0.5;
            return dist * dist;
        }

        /// <summary>Intersects two bounding spheres.</summary>
        /// <param name="c1">The center of the first bounding sphere.</param>
        /// <param name="sr1">The radius of the first bounding sphere.</param>
        /// <param name="b2">Second bounding sphere.</param>
        /// <returns>True when the intersection is not empty; false, otherwise.</returns>
        protected static bool Intersect(ref Vector c1, ref double sr1, IBounded b2)
        {
            double r1 = Math.Sqrt(sr1), r2 = Math.Sqrt(b2.SquaredRadius);
            double distance = c1.Distance(b2.Centroid);
            // Check if they are too separated.
            if (r1 + r2 - distance < Tolerance.Epsilon)
                return false;
            // Check for containment.
            if (r1 >= r2 && distance <= r1 - r2)
            {
                c1 = b2.Centroid;
                sr1 = b2.SquaredRadius;
                return true;
            }
            if (r2 >= r1 && distance <= r2 - r1)
                return true;
            // This is a "proper" intersection.
            double fromR1 = (distance + (sr1 - b2.SquaredRadius) / distance) * 0.5;
            c1 += (b2.Centroid - c1) * (fromR1 / distance);
            sr1 -= fromR1 * fromR1;
            return true;
        }

        /// <summary>Merges a rotation and a shape that may be a scale change.</summary>
        /// <param name="s">The shape to be rotated.</param>
        /// <param name="rotation">The rotation matrix.</param>
        protected static void DoRotation(ref IShape s, in Matrix rotation)
        {
            if (s is not Scale scale)
                s.ApplyRotation(rotation);
            else
                s = new Transform(scale.Original, scale.Factor, rotation);
        }

        /// <summary>Merges a scale change and a shape that may be a rotation.</summary>
        /// <param name="s">The shape to be scaled.</param>
        /// <param name="factor">The scale factor.</param>
        protected static void DoScale(ref IShape s, in Vector factor)
        {
            if (s is not Rotate rotate)
                s.ApplyScale(factor);
            else
                s = new Transform(rotate.Original, rotate.Rotation, factor);
        }

        #region Precursors for IShape methods.

        /// <summary>Axis-aligned bounding box surrounding the shape.</summary>
        protected Bounds bounds;

        /// <summary>Gets the box that encloses this shape.</summary>
        public virtual Bounds Bounds => bounds;

        /// <summary>Gets the center of the bounding sphere.</summary>
        public virtual Vector Centroid => bounds.Center;

        /// <summary>The square radius of the bounding sphere.</summary>
        public virtual double SquaredRadius => bounds.SquaredRadius;

        #endregion

        #region Precursors for ITransformable methods.

        /// <summary>First optimization pass.</summary>
        /// <returns>This shape, or a new and more efficient equivalent.</returns>
        public virtual IShape Simplify() => (IShape)this;

        /// <summary>Second optimization pass, where special shapes are introduced.</summary>
        /// <returns>This shape, or a new and more efficient equivalent.</returns>
        public virtual IShape Substitute() => (IShape)this;

        /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The cost of the transformation.</returns>
        public virtual TransformationCost CanRotate(in Matrix rotation) => TransformationCost.Nope;

        /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The cost of the transformation.</returns>
        public virtual TransformationCost CanScale(in Vector factor) => TransformationCost.Nope;

        /// <summary>Translate this shape.</summary>
        /// <param name="translation">Translation amount.</param>
        public virtual void ApplyTranslation(in Vector translation) { }

        /// <summary>Rotate this shape.</summary>
        /// <param name="rotation">Rotation amount.</param>
        public virtual void ApplyRotation(in Matrix rotation) { }

        /// <summary>Scale this shape.</summary>
        /// <param name="factor">Scale factor.</param>
        public virtual void ApplyScale(in Vector factor) { }

        /// <summary>Change the material this shape is made of.</summary>
        /// <param name="newMaterial">The new material definition.</param>
        public virtual void ChangeDress(IMaterial newMaterial) { }

        /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
        /// <param name="scene">The scene this shape is included within.</param>
        /// <param name="inCsg">Is this shape included in a CSG operation?</param>
        /// <param name="inTransform">Is this shape nested inside a transform?</param>
        public virtual void Initialize(IScene scene, bool inCsg, bool inTransform) { }

        /// <summary>Notifies the shape that its container is checking spheric bounds.</summary>
        /// <param name="centroid">Centroid of parent's spheric bounds.</param>
        /// <param name="squaredRadius">Square radius of parent's spheric bounds.</param>
        public virtual void NotifySphericBounds(in Vector centroid, double squaredRadius) { }

        #endregion
    }

    /// <summary>Common base class for all material shapes.</summary>
    /// <remarks>
    /// Not all shapes are "material" shapes.
    /// Unions and Euclidean transforms, for instances, have no associated material.
    /// </remarks>
    public abstract class MaterialShape : Shape
    {
        /// <summary>Shape's material.</summary>
        protected IMaterial material;

        /// <summary>Initializes a material shape.</summary>
        /// <param name="material">Shape's material.</param>
        protected MaterialShape(IMaterial material) => this.material = material;

        /// <summary>Gets the material this shape is made of.</summary>
        public IMaterial Material => material;

        /// <summary>Change the material this shape is made of.</summary>
        /// <param name="newMaterial">The new material definition.</param>
        public override void ChangeDress(IMaterial newMaterial) => material = newMaterial;
    }
}
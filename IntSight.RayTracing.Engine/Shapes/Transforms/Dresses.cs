namespace IntSight.RayTracing.Engine
{
    /// <summary>Wraps another shape with a new material.</summary>
    [XSight, Properties(nameof(material)), Children(nameof(original))]
    public sealed class Dress : Shape, IShape
    {
        /// <summary>Shape whose material will be changed.</summary>
        private IShape original;
        /// <summary>New material for the transformed shape.</summary>
        private IMaterial material;

        /// <summary>Creates a material change operator for an arbitrary shape.</summary>
        /// <param name="material">New material for the shape.</param>
        /// <param name="original">Shape whose material will be changed.</param>
        public Dress(IMaterial material, IShape original)
        {
            this.original = original;
            this.material = material;
            bounds = original.Bounds;
        }

        /// <summary>Creates a material change operator for an arbitrary shape.</summary>
        /// <param name="original">Shape whose material will be changed.</param>
        /// <param name="material">New material for the shape.</param>
        public Dress(IShape original, IMaterial material) : this(material, original) { }

        #region IShape members.

        /// <summary>Computes the intersection between the shape and the ray.</summary>
        /// <param name="ray">Ray emitted by a light source.</param>
        /// <returns>True, when such an intersection exists.</returns>
        bool IShape.ShadowTest(Ray ray) => original.ShadowTest(ray);

        /// <summary>Test intersection with a given ray.</summary>
        /// <param name="ray">Ray to be tested (direction is always normalized).</param>
        /// <param name="maxt">Upper bound for the intersection time.</param>
        /// <param name="info">Hit information, when an intersection is found.</param>
        /// <returns>True when an intersection is found.</returns>
        bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
        {
            if (original.HitTest(ray, maxt, ref info))
            {
                info.Material = material;
                return true;
            }
            else
                return false;
        }

        /// <summary>Computes the surface normal at a given location.</summary>
        /// <param name="location">Point to be tested.</param>
        /// <returns>Normal vector at that point.</returns>
        Vector IShape.GetNormal(in Vector location) => original.GetNormal(location);

        /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
        /// <param name="ray">Ray to be tested.</param>
        /// <param name="hits">Preallocated array of intersections.</param>
        /// <returns>Number of found intersections.</returns>
        int IShape.GetHits(Ray ray, Hit[] hits) => original.GetHits(ray, hits);

        #endregion

        #region ITransformable members.

        /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
        int ITransformable.MaxHits => original.MaxHits;

        /// <summary>Estimated complexity.</summary>
        ShapeCost ITransformable.Cost => ShapeCost.EasyPeasy;

        /// <summary>Invert normals for the right operand in a difference.</summary>
        void ITransformable.Negate() => original.Negate();

        /// <summary>Creates a new independent copy of the whole shape.</summary>
        /// <param name="force">True when a new copy is needed.</param>
        /// <returns>The new shape.</returns>
        IShape ITransformable.Clone(bool force)
        {
            IShape s = original.Clone(force);
            IMaterial m = material.Clone(force);
            return force || s != original || m != material ? new Dress(s, m) : (this);
        }

        /// <summary>First optimization pass.</summary>
        /// <returns>This shape, or a new and more efficient equivalent.</returns>
        public override IShape Simplify()
        {
            original = original.Simplify();
            original.ChangeDress(material);
            return original;
        }

        /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
        /// <param name="scene">The scene this shape is included within.</param>
        /// <param name="inCsg">Is this shape included in a CSG operation?</param>
        /// <param name="inTransform">Is this shape nested inside a transform?</param>
        public override void Initialize(IScene scene, bool inCsg, bool inTransform) =>
            original.Initialize(scene, inCsg, true);

        /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
        /// <param name="rotation">Rotation amount.</param>
        /// <returns>The cost of the transformation.</returns>
        public override TransformationCost CanRotate(in Matrix rotation) =>
            original.CanRotate(rotation);

        /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>The cost of the transformation.</returns>
        public override TransformationCost CanScale(in Vector factor) =>
            original.CanScale(factor);

        /// <summary>Translates this shape.</summary>
        /// <param name="translation">Translation amount.</param>
        public override void ApplyTranslation(in Vector translation)
        {
            original.ApplyTranslation(translation);
            bounds = original.Bounds;
        }

        /// <summary>Rotates this shape.</summary>
        /// <param name="rotation">Rotation amount.</param>
        public override void ApplyRotation(in Matrix rotation)
        {
            original.ApplyRotation(rotation);
            bounds = original.Bounds;
        }

        /// <summary>Scales this shape.</summary>
        /// <param name="factor">Scale factor.</param>
        public override void ApplyScale(in Vector factor)
        {
            original.ApplyScale(factor);
            bounds = original.Bounds;
        }

        /// <summary>Changes the material this shape is made of.</summary>
        /// <param name="newMaterial">The new material definition.</param>
        public override void ChangeDress(IMaterial newMaterial)
        {
            original.ChangeDress(newMaterial);
            material = newMaterial;
        }

        #endregion
    }
}
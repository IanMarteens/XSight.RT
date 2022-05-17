using System;
using static System.Runtime.CompilerServices.Unsafe;

namespace IntSight.RayTracing.Engine
{
    /// <summary>A simple parser with no antialiasing.</summary>
    /// <remarks>
    /// All public samplers inherit this class. The tracing algorithm is implemented here.
    /// </remarks>
    [XSight(Alias = "Basic")]
    public class BasicSampler : SamplerBase, ISampler
    {
        /// <summary>Maximum number of allowed bounces.</summary>
        protected int bounces;
        /// <summary>Lowest bounce counter reached.</summary>
        protected int lowestBounce;
        /// <summary>
        /// Minimum level for tracing an additional ray with diffuse reflections.
        /// </summary>
        protected readonly int diffuseBounces;
        /// <summary>Number of rays hitting the background for a pixel.</summary>
        protected int missingRays;
        /// <summary>Minimum weight allowed for a light ray.</summary>
        protected float minWeight;
        /// <summary>Intersection information for tracing primary rays.</summary>
        private HitInfo info;
        /// <summary>A cache for media container nodes.</summary>
        private MediaLink containerCache = new();

        /// <summary>Creates a basic sampler with no antialiasing.</summary>
        /// <param name="bounces">Maximum number of bounces allowed.</param>
        /// <param name="minWeight">Minumum ray intensity allowed.</param>
        [Preferred]
        public BasicSampler(
            [Proposed("10")] int bounces,
            [Proposed("0.001")] double minWeight)
        {
            this.bounces = bounces;
            lowestBounce = bounces;
            diffuseBounces = bounces - Properties.Settings.Default.DiffusionLevels;
            this.minWeight = (float)minWeight;
        }

        /// <summary>Creates a basic sampler with no antialiasing.</summary>
        /// <param name="bounces">Maximum number of bounces allowed.</param>
        public BasicSampler([Proposed("10")] int bounces) : this(bounces, 0.001) { }

        /// <summary>Creates a basic sampler with no antialiasing.</summary>
        public BasicSampler() : this(10, 0.001) { }

        /// <summary>Traces a primary ray.</summary>
        /// <returns>Ray's computed color.</returns>
        protected Pixel Trace()
        {
            int bouncesToLive = bounces;
            float weight = 1.0F;
            Pixel filter = Pixel.White, result = default;
            bool isPrimary = true;
            Ray r = cameraRay;
            while (shapes.HitTest(r, double.PositiveInfinity, ref info))
            {
                IMaterial material = info.Material;
                Vector location = r[info.Time];
                if (material.GetColor(out Pixel surface, info.HitPoint))
                    material.Bump(ref info.Normal, location);
                double cosine = r.Direction * info.Normal;
                Vector reflection = (r.Direction - (cosine + cosine) * info.Normal).Normalized();

                Pixel color = surface * ambient[location, info.Normal];
                foreach (ILight light in lights)
                {
                    // Intensity is an obstruction test, with support for partial occlusion.
                    float factor = light.Intensity(location, isPrimary);
                    if (factor > 0F)
                        color = color.Lerp(material.Shade(
                            location, info.Normal, reflection, light, surface), factor);
                }
                media?.Modify(r, info.Time, ref color);
                result = result.Lerp(color, filter, weight);
                if (--bouncesToLive <= 0)
                    goto CLIP_N_QUIT;
                float reflectionCoeff = material.Reflection(
                    cosine, out Pixel lightFilter, out float refractionCoeff);
                if (refractionCoeff > 0F && (refractionCoeff *= weight) >= minWeight)
                {
                    // Compute the direction for the refracted ray.
                    double ior = material.IndexOfRefraction;
                    double n = 1.0 / ior, nc = n * cosine, f = (nc + n) * (nc - n) + 1.0;
                    if (f >= 0.0)
                    {
                        r.Origin = location;
                        f = Math.Sqrt(f) + nc;
                        r.Direction = new(
                            n * r.Direction.X - f * info.Normal.X,
                            n * r.Direction.Y - f * info.Normal.Y,
                            n * r.Direction.Z - f * info.Normal.Z);
                        // Create a list of containers with a single node.
                        // The cache has been preallocated with one item.
                        MediaLink containers = containerCache;
                        containerCache = containers.Next;
                        containers.Next = null;
                        containers.Material = material;
                        // Trace the refracted ray.
                        result += Trace(bouncesToLive, ior, refractionCoeff, filter, containers);
                        // Return the media node to the cache.
                        containers.Next = containerCache;
                        containerCache = containers;
                    }
                    else
                        reflectionCoeff = 0.999F;
                }
                if ((weight *= reflectionCoeff) < minWeight)
                    goto CLIP_N_QUIT;
                filter *= lightFilter;
                r.Origin = location;
                if (material.HasRoughness)
                    if (bouncesToLive >= diffuseBounces)
                    {
                        // We'll cast two rays for primary and reflected primary rays.
                        float w1 = material.Perturbate(
                            reflection, info.Normal, r, out reflection);
                        result += Trace(bouncesToLive, 1.0, weight * w1, filter, null);
                        r.Origin = location;
                        r.Direction = reflection;
                        weight -= weight * w1;
                    }
                    else
                        r.Direction = material.Perturbate(reflection, info.Normal, true);
                else
                    r.Direction = reflection;
                isPrimary = false;
            }
            if (isPrimary)
                missingRays++;
            result = result.Add(media?.Modify(r, background[r]) ?? background[r], filter, weight);
            if (bouncesToLive < lowestBounce) lowestBounce = bouncesToLive;
            return result;
        CLIP_N_QUIT:
            if (bouncesToLive < lowestBounce) lowestBounce = bouncesToLive;
            return result.Clip();
        }

        /// <summary>Traces a refracted or perturbated secondary ray.</summary>
        /// <param name="bounces">Allowed bounces.</param>
        /// <param name="ior">Index of refraction of the new media.</param>
        /// <param name="weight">Current ray weight.</param>
        /// <param name="filter">Current light filter.</param>
        /// <param name="containers">Current list of nested media containers.</param>
        /// <returns>Ray's computed color.</returns>
        private Pixel Trace(int bounces, double ior, float weight, Pixel filter,
            MediaLink containers)
        {
            HitInfo info = new();
            Pixel result = new();
            bool attenuation = containers != null && containers.Material.HasAttenuation;
            Pixel attFilter = attenuation ? containers.Material.AttenuationFactor : new();
            Ray r = cameraRay;
            while (shapes.HitTest(r, double.PositiveInfinity, ref info))
            {
                IMaterial material = info.Material;
                Vector location = r[info.Time];
                if (material.GetColor(out Pixel surface, info.HitPoint))
                    material.Bump(ref info.Normal, location);
                double cosine = r.Direction * info.Normal;
                Vector reflection = (r.Direction - (cosine + cosine) * info.Normal).Normalized();

                Pixel color = surface * ambient[location, info.Normal];
                foreach (ILight light in lights)
                {
                    // Intensity is an obstruction test, with support for partial occlusion.
                    float factor = light.Intensity(location, false);
                    if (factor > 0.0F)
                        color = color.Lerp(material.Shade(
                            location, info.Normal, reflection, light, surface), factor);
                }
                if (attenuation)
                    // Exponential attenuation inside transparent media (Beer's law).
                    filter = filter.Attenuate(attFilter, (float)info.Time);
                else if (containers == null)
                    media?.Modify(r, info.Time, ref color);
                result = result.Lerp(color, filter, weight);
                if (--bounces <= 0)
                    goto CLIP_N_QUIT;
                float reflectionCoeff = material.Reflection(
                    cosine, out Pixel lightFilter, out float refractionCoeff);
                if (refractionCoeff > 0.0F && (refractionCoeff *= weight) >= minWeight)
                {
                    double newIor, n;
                    if (cosine < 0)
                    {
                        n = ior / (newIor = material.IndexOfRefraction);
                        goto COMPUTE_ANGLE;
                    }
                    if (containers == null)
                        goto IOR_IS_1;
                    if (containers.Material != material)
                    {
                        n = ior / (newIor = containers.Material.IndexOfRefraction);
                        goto COMPUTE_ANGLE;
                    }
                    if (containers.Next == null)
                        goto IOR_IS_1;
                    n = ior / (newIor = containers.Next.Material.IndexOfRefraction);
                    goto COMPUTE_ANGLE;
                IOR_IS_1:
                    newIor = 1.0; n = ior;
                COMPUTE_ANGLE:
                    // Compute the direction for the refracted ray.
                    double nc = n * cosine, f = (nc + n) * (nc - n) + 1.0;
                    if (f >= 0.0)
                    {
                        MediaLink newContainers, recycle;
                        if (cosine < 0)
                        {
                            f = Math.Sqrt(f) + nc;
                            // We are entering a new container.
                            if (containerCache == null)
                                newContainers = new();
                            else
                            {
                                newContainers = containerCache;
                                containerCache = newContainers.Next;
                            }
                            newContainers.Material = material;
                            newContainers.Next = containers;
                            recycle = newContainers;
                        }
                        else
                        {
                            f = nc - Math.Sqrt(f);
                            recycle = null;
                            if (containers == null)
                                newContainers = null;
                            else if (containers.Material == material)
                                // We are leaving the outer container.
                                newContainers = containers.Next;
                            else
                                // We are leaving an inner container: a non-physical situation!
                                newContainers = RemoveAndClone(containers, ref recycle, material);
                        }
                        r.Origin = location;
                        r.Direction = new(
                            n * r.Direction.X - f * info.Normal.X,
                            n * r.Direction.Y - f * info.Normal.Y,
                            n * r.Direction.Z - f * info.Normal.Z);
                        result += Trace(bounces, newIor, refractionCoeff, filter, newContainers);
                        if (recycle != null)
                        {
                            recycle.Next = containerCache;
                            containerCache = newContainers;
                        }
                    }
                    else
                        reflectionCoeff = 0.99F;
                }
                if ((weight *= reflectionCoeff) < minWeight)
                    goto CLIP_N_QUIT;
                filter *= lightFilter;
                r.Origin = location;
                if (material.HasRoughness)
                    // Perturbate the direction to simulate glossy reflections.
                    r.Direction = material.Perturbate(reflection, info.Normal, cosine <= 0.0);
                else
                    r.Direction = reflection;
            }
            result = result.Add(media?.Modify(r, background[r]) ?? background[r], filter, weight);
            if (bounces < lowestBounce) lowestBounce = bounces;
            return result;
        CLIP_N_QUIT:
            if (bounces < lowestBounce) lowestBounce = bounces;
            return result.Clip();
        }

        /// <summary>Create an independent copy of the sampler.</summary>
        /// <returns>The new sampler.</returns>
        public override ISampler Clone() => new BasicSampler(bounces, minWeight);

        /// <summary>Renders a scene using only one ray by pixel.</summary>
        /// <param name="strip">Band of the pixel map to render.</param>
        public virtual void Render(PixelStrip strip)
        {
            ref TransPixel p = ref strip.FirstPixel;
            int w = camera.Width, row;
            while ((row = strip.NextRow()) >= 0)
            {
                int col = w;
                do
                {
                    camera.Focus(row, --col);
                    missingRays = 0;
                    p = Trace().ToTransPixel(missingRays != 0 ? 0 : 255);
                    p = ref Add(ref p, 1);
                }
                while (col > 0);
            }
            strip.MaxDepth = Math.Max(0, bounces - lowestBounce);
            strip.SuperSamples = strip.Area;
        }

        /// <summary>Duplicates a linked list, removing a node with a given material.</summary>
        /// <param name="original">Linked list of media nodes to clone.</param>
        /// <param name="recycle">Last node to recycle, if any.</param>
        /// <param name="removeMaterial">Material to remove.</param>
        /// <returns>The new list, or the original one, if material was not present.</returns>
        private MediaLink RemoveAndClone(MediaLink original, ref MediaLink recycle,
            IMaterial removeMaterial)
        {
            // We have reach the material to remove, and we return the tail.
            if (original.Material == removeMaterial)
                return original.Next;
            // We haven't found the material, and this is the last node.
            else if (original.Next == null)
                return original;
            else
            {
                // Try removing the material from the current node's tail.
                MediaLink newNext = RemoveAndClone(original.Next, ref recycle, removeMaterial);
                // If the cloned link is the same, we haven't found the material.
                if (newNext == original.Next)
                    return original;
                else
                {
                    // We have to clone the current node.
                    MediaLink result;
                    if (containerCache == null)
                        result = new();
                    else
                    {
                        result = containerCache;
                        containerCache = result.Next;
                    }
                    result.Material = original.Material;
                    result.Next = newNext;
                    recycle ??= result;
                    return result;
                }
            }
        }

        /// <summary>Linked node holding a material reference.</summary>
        private sealed class MediaLink
        {
            /// <summary>Curent volume's material.</summary>
            public IMaterial Material;
            /// <summary>Link to the container surrounding current volume.</summary>
            public MediaLink Next;
        }
    }
}

using System;

namespace IntSight.RayTracing.Engine
{
    /// <summary>Implements a container for traced photons.</summary>
    public sealed partial class PhotonMap
    {
        /// <summary>Implements photon emission and tracing.</summary>
        private sealed class PhotonTracer
        {
            private const double PI2 = 2 * Math.PI;
            private readonly PhotonMap map;
            private readonly int bounces;
            private readonly Random rnd = new(9125);
            private readonly Ray ray = new();
            private IShape root;
            private readonly IMaterial[] materialStack;
            private Matrix transform = Matrix.Identity;

            public PhotonTracer(PhotonMap map)
            {
                this.map = map;
                bounces = map.Bounces;
                materialStack = new IMaterial[bounces + 1];
            }

            /// <summary>Creates a photon map for the given scene.</summary>
            /// <param name="scene">Scene to be light traced.</param>
            public PhotonMap Trace(IScene scene)
            {
                root = scene.Root;
                int photonsBySource = map.Capacity / scene.Lights.Length;
                int emit = map.Capacity - photonsBySource * scene.Lights.Length + photonsBySource;
                foreach (ILight light in scene.Lights)
                {
                    int emitted = emit;
                    Pixel lightColor = light.Color;
                    Vector org = light.Location;
                    // Find a finite bounding sphere by discarding infinite planes.
                    IShape target = null;
                    if (!root.Bounds.IsInfinite)
                        target = root;
                    else if (root is IUnion union && union.Shapes.Length == 2)
                        if (!union.Shapes[0].Bounds.IsInfinite)
                            target = union.Shapes[0];
                        else if (!union.Shapes[1].Bounds.IsInfinite)
                            target = union.Shapes[1];
                    // Check if we're not inside the possible bounding sphere.
                    if (target != null && (org - target.Centroid).Squared <= target.SquaredRadius)
                        target = null;
                    // We can restrict photon shooting to a bounding sphere.
                    if (target != null)
                    {
                        Vector up = Vector.YRay;
                        if ((up ^ (target.Centroid - org)).Length <= Tolerance.Epsilon)
                            up = Vector.XRay;
                        transform = new(org, target.Centroid, up);
                        double theta = Math.Asin(Math.Sqrt(
                            target.SquaredRadius / (target.Centroid - org).Squared));
                        while (emit-- > 0)
                        {
                            // In that case, sample the lower hemisphere around the light source.
                            ray.Origin = org;
                            ray.Direction = TargetRandomDirection(theta);
                            TraceRay(lightColor);
                        }
                        map.ScalePhotonPower((float)(1 - Math.Cos(theta)) / (emitted + emitted));
                    }
                    // If no bounding sphere, check if the light is above the scene.
                    else if (org.Y > root.Bounds.y1)
                    {
                        while (emit-- > 0)
                        {
                            // In that case, sample the lower hemisphere around the light source.
                            ray.Origin = org;
                            ray.Direction = HalfRandomDirection();
                            TraceRay(lightColor);
                        }
                        map.ScalePhotonPower(0.5F / emitted);
                    }
                    // Bad luck: wait for the ricochet!
                    else
                    {
                        while (emit-- > 0)
                        {
                            // Sample the whole sphere.
                            ray.Origin = org;
                            ray.Direction = RandomDirection();
                            TraceRay(lightColor);
                        }
                        map.ScalePhotonPower(1.0F / emitted);
                    }
                    // Set the size of the next photon block to emit.
                    emit = photonsBySource;
                }
                return map;
            }

            /// <summary>Computes a random unit vector on a sphere.</summary>
            /// <returns>Computed direction.</returns>
            private Vector RandomDirection()
            {
                double phi = rnd.NextDouble() * PI2;
                double theta = 2 * rnd.NextDouble() - 1;
                double y;
                if (theta >= 0)
                {
                    y = 1 - theta;
                    theta = Math.Sqrt((2.0 - theta) * theta);
                }
                else
                {
                    y = -1 - theta;
                    theta = Math.Sqrt((-2.0 - theta) * theta);
                }
                return new(Math.Cos(phi) * theta, y, Math.Sin(phi) * theta);
            }

            /// <summary>Computes a random unit vector on the lower hemisphere.</summary>
            /// <returns>Computed direction.</returns>
            private Vector HalfRandomDirection()
            {
                double phi = rnd.NextDouble() * PI2;
                double theta = rnd.NextDouble();
                double y = theta - 1;
                return new(
                    Math.Cos(phi) * (theta = Math.Sqrt((2.0 - theta) * theta)),
                    y,
                    Math.Sin(phi) * theta);
            }

            /// <summary>Computes a random unit vector inside a light cone.</summary>
            /// <param name="theta">Half-deflection angle.</param>
            /// <returns>The computed direction.</returns>
            private Vector TargetRandomDirection(double theta)
            {
                double phi = rnd.NextDouble() * PI2;
                double z = Math.Cos((2 * rnd.NextDouble() - 1) * theta);
                double r = Math.Sqrt(1 - z * z);
                return transform.Rotate(Math.Cos(phi) * r, Math.Sin(phi) * r, z);
            }

            /// <summary>Traces a single light ray accross the scene.</summary>
            /// <param name="photonColor">Initial photon power.</param>
            private void TraceRay(in Pixel photonColor)
            {
                HitInfo info = new();
                int allowedBounces = bounces;
                float ior = 1.0F;
                int materialTop = 0;
                while (root.HitTest(ray, double.MaxValue, ref info))
                {
                    Vector location = ray[info.Time];
                    double cosine = ray.Direction * info.Normal;
                    float decision = (float)rnd.NextDouble();
                    float reflectionCoeff = info.Material.Reflection(
                        cosine, out _, out float refractionCoeff);
                    float diffusion = 1.0F - reflectionCoeff - refractionCoeff;
                    if (diffusion > 0.0F && (decision -= diffusion) <= 0.0F)
                    {
                        // Store photon, but only after the first bounce.
                        if (allowedBounces < bounces)
                            map.Store(location, photonColor, ray.Direction);
                        break;
                    }
                    if (--allowedBounces <= 0)
                        break;
                    // Compute the reflection vector.
                    Vector direction = (ray.Direction - (cosine + cosine) * info.Normal).Normalized();
                    if ((decision -= reflectionCoeff) <= 0.0F)
                    {
                        ray.Origin = location;
                        ray.Direction = direction;
                        continue;
                    }
                    double n;
                    if (cosine < 0)
                    {
                        n = ior / info.Material.IndexOfRefraction;
                        goto COMPUTE_ANGLE;
                    }
                    if (materialTop == 0)
                        goto IOR_IS_1;
                    if (materialStack[materialTop - 1] != info.Material)
                    {
                        n = ior / materialStack[materialTop - 1].IndexOfRefraction;
                        goto COMPUTE_ANGLE;
                    }
                    if (materialTop == 1)
                        goto IOR_IS_1;
                    n = ior / materialStack[materialTop - 2].IndexOfRefraction;
                    goto COMPUTE_ANGLE;
                IOR_IS_1:
                    n = ior;
                COMPUTE_ANGLE:
                    // Compute the direction for the refracted ray.
                    double nc = n * cosine, f = nc * nc - n * n + 1.0;
                    if (f < 0.0F)
                    {
                        ray.Origin = location;
                        ray.Direction = direction;
                        continue;
                    }
                    if (cosine < 0)
                    {
                        f = Math.Sqrt(f) + nc;
                        // We are entering a new container.
                        materialStack[materialTop++] = info.Material;
                    }
                    else
                    {
                        f = nc - Math.Sqrt(f);
                        // We are leaving the outer container.
                        if (materialTop > 0 && materialStack[materialTop - 1] == info.Material)
                            materialTop--;
                    }
                    // Refract the ray.
                    ray.Origin = location;
                    ray.Direction = new(
                        n * ray.Direction.X - f * info.Normal.X,
                        n * ray.Direction.Y - f * info.Normal.Y,
                        n * ray.Direction.Z - f * info.Normal.Z);
                }
            }
        }
    }
}
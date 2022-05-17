using System;

namespace IntSight.RayTracing.Engine
{
    /// <summary>Implements a container for traced photons.</summary>
    public sealed partial class PhotonMap
    {
        /// <summary>Implements photon gathering for the irradiance estimator.</summary>
        public sealed class PhotonSet
        {
            private readonly Photon[] photons;
            private readonly int storedCount;
            private readonly int maxCount;
            private readonly float maxDistance;
            private int foundCount;
            private float x, y, z;
            private float nx, ny, nz;
            private float maxDistance2;
            public Photon[] found;
            public float[] dist2;

            public PhotonSet(Photon[] photons, int storedCount, int maxCount, float maxDistance)
            {
                this.photons = photons;
                this.storedCount = storedCount;
                this.maxCount = maxCount;
                this.maxDistance = maxDistance;
                found = new Photon[maxCount];
                dist2 = new float[maxCount];
                maxDistance2 = maxDistance * maxDistance;
            }

            public void Locate(in Vector hitPoint, in Vector normal)
            {
                foundCount = 0;
                x = (float)hitPoint.X;
                y = (float)hitPoint.Y;
                z = (float)hitPoint.Z;
                nx = (float)normal.X;
                ny = (float)normal.Y;
                nz = (float)normal.Z;
                maxDistance2 = maxDistance * maxDistance;
                Locate(0, storedCount - 1);
            }

            private void Locate(int start, int end)
            {
                int middle = (start + end) / 2;
                Photon p = photons[middle];
                float dx = p.x - x, dy = p.y - y, dz = p.z - z;
                float dx2 = dx * dx, dy2 = dy * dy, dz2 = dx * dx;
                if (p.axis == Axis.X && dx2 <= maxDistance2 ||
                    p.axis == Axis.Y && dy2 <= maxDistance2 ||
                    p.axis == Axis.Z && dz2 <= maxDistance2)
                {
                    float d2 = (dx2 + dy2 + dz2) *
                        (1 + 16 * Math.Abs(dx * nx + dy * ny + dz * nz));
                    if (d2 < maxDistance2)
                        if (foundCount < maxCount)
                        {
                            int k = ++foundCount;
                            while (k > 1)
                            {
                                int half = k / 2;
                                float kd2 = dist2[half - 1];
                                if (d2 < kd2)
                                    break;
                                dist2[k - 1] = kd2;
                                found[k - 1] = found[half - 1];
                                k = half;
                            }
                            dist2[k - 1] = d2;
                            found[k - 1] = p;
                        }
                        else
                        {
                            int k = 1, k2 = 2;
                            for (; k2 < foundCount; k = k2, k2 += k)
                            {
                                float d_k2_m1 = dist2[k2 - 1];
                                float d_k2 = dist2[k2];
                                if (d_k2 > d_k2_m1)
                                {
                                    d_k2_m1 = d_k2;
                                    ++k2;
                                }
                                if (!(d_k2_m1 > d2))
                                    break;
                                dist2[k - 1] = d_k2_m1;
                                found[k - 1] = found[k2 - 1];
                            }
                            if (k2 == foundCount)
                            {
                                float d_k2_m1 = dist2[k2 - 1];
                                if (d_k2_m1 > d2)
                                {
                                    dist2[k - 1] = d_k2_m1;
                                    found[k - 1] = found[k2 - 1];
                                    k = k2;
                                }
                            }
                            dist2[k - 1] = d2;
                            found[k - 1] = p;
                            maxDistance2 = dist2[0];
                        }
                }
                // Recursive photon gathering.
                switch (p.axis)
                {
                    case Axis.X:
                        if (x < p.x)
                        {
                            if (x - maxDistance < p.x && middle - 1 >= start)
                                Locate(start, middle - 1);
                            if (x + maxDistance > p.x && end >= middle + 1)
                                Locate(middle + 1, end);
                        }
                        else
                        {
                            if (x + maxDistance > p.x && end >= middle + 1)
                                Locate(middle + 1, end);
                            if (x - maxDistance < p.x && middle - 1 >= start)
                                Locate(start, middle - 1);
                        }
                        break;
                    case Axis.Y:
                        if (y < p.y)
                        {
                            if (y - maxDistance < p.y && middle - 1 >= start)
                                Locate(start, middle - 1);
                            if (y + maxDistance > p.y && end >= middle + 1)
                                Locate(middle + 1, end);
                        }
                        else
                        {
                            if (y + maxDistance > p.y && end >= middle + 1)
                                Locate(middle + 1, end);
                            if (y - maxDistance < p.y && middle - 1 >= start)
                                Locate(start, middle - 1);
                        }
                        break;
                    default:
                        if (z < p.z)
                        {
                            if (z - maxDistance < p.z && middle - 1 >= start)
                                Locate(start, middle - 1);
                            if (z + maxDistance > p.z && end >= middle + 1)
                                Locate(middle + 1, end);
                        }
                        else
                        {
                            if (z + maxDistance > p.z && end >= middle + 1)
                                Locate(middle + 1, end);
                            if (z - maxDistance < p.z && middle - 1 >= start)
                                Locate(start, middle - 1);
                        }
                        break;
                }
            }
        }
    }
}

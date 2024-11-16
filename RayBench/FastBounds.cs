using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace IntSight.RayTracing.Engine
{
    /// <summary>An axis-aligned parallelepiped.</summary>
    [SkipLocalsInit]
    public readonly struct FastBounds
    {
        /// <summary>Lower limits.</summary>
        private readonly Vector256<double> lo;
        /// <summary>Upper limits.</summary>
        private readonly Vector256<double> hi;

        /// <summary>Creates bounds given the lower and upper limits.</summary>
        /// <remarks>No checks are performed on the limits.</remarks>
        /// <param name="lo">A vector with the lower limits.</param>
        /// <param name="hi">A vector with the upper limits.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FastBounds(Vector256<double> lo, Vector256<double> hi) =>
            (this.lo, this.hi) = (lo, hi);

        /// <summary>Creates a finite axis-aligned bounding box.</summary>
        public FastBounds(in Bounds b)
            : this(Vector256.Create(b.From.X, b.From.Y, b.From.Z, 0), 
                  Vector256.Create(b.To.X, b.To.Y, b.To.Z, 0)) { }

        /// <summary>Creates a finite axis-aligned bounding box.</summary>
        /// <remarks>Faster than the constructor, since doesn't checks parameters.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FastBounds Create(
            double x0, double y0, double z0,
            double x1, double y1, double z1) =>
            new(Vector256.Create(x0, y0, z0, 0), Vector256.Create(x1, y1, z1, 0));

        /// <summary>Initializes a bounding box from a sphere.</summary>
        /// <param name="center">Sphere's center.</param>
        /// <param name="radius">Sphere's radius.</param>
        /// <returns>The enclosing bounding box.</returns>
        public static FastBounds FromSphere(in Vector center, double radius) =>
            Create(
                center.X - radius, center.Y - radius, center.Z - radius,
                center.X + radius, center.Y + radius, center.Z + radius);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Intersects(Ray ray, double maxt)
        {
            var d = Vector256.Create(ray.InvDir.X, ray.InvDir.Y, ray.InvDir.Z, double.NaN);
            var org = Vector256.Create(ray.Origin.X, ray.Origin.Y, ray.Origin.Z, 0);
            var t0 = Avx.Multiply(Avx.Subtract(lo, org), d);
            var t1 = Avx.Multiply(Avx.Subtract(hi, org), d);

            var min = Avx.Min(t0, t1);
            var t2 = Avx.Max(min, Avx.Permute2x128(min, min, 1));
            double time0 = Avx.Max(Avx.Permute(t2, 5), t2).ToScalar();
            if (time0 > maxt)
                return false;

            var max = Avx.Max(t0, t1);
            t2 = Avx.Min(max, Avx.Permute2x128(max, max, 1));
            double time1 = Avx.Min(Avx.Permute(t2, 5), t2).GetElement(2);
            return time1 >= 0 && time0 <= time1;
        }
    }
}

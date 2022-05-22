namespace IntSight.RayTracing.Engine;

/// <summary>Common base class for unions of blob items.</summary>
internal abstract class BlobUnion : IBounded
{
    protected BlobUnion(Bounds bounds, Vector centroid, double radius2) =>
        (Bounds, Centroid, SquaredRadius) = (bounds, centroid, radius2);

    protected BlobUnion(Bounds bounds) => Bounds = bounds;

    public static BlobUnion Build(IBlobItem[] items) =>
        UnionBuilder.Build(items, 0, items.Length - 1, Axis.Unknown);

    protected double Effectivity => Math.Pow(SquaredRadius, 1.5) / Bounds.Volume;

    public abstract int FindHits(Ray ray, Blob.ItemHit[] hits, int count);

    public Bounds Bounds { get; }
    public Vector Centroid { get; protected set; }
    public double SquaredRadius { get; protected set; }

    /// <summary>A fork inside a bounding sphere.</summary>
    [Properties(nameof(Effectivity)), Children(nameof(n1), nameof(n2))]
    private sealed class SFork : BlobUnion
    {
        private readonly BlobUnion n1;
        private readonly BlobUnion n2;

        public SFork(BlobUnion n1, BlobUnion n2)
            : base(n1.Bounds + n2.Bounds)
        {
            this.n1 = n1;
            this.n2 = n2;
            (Centroid, SquaredRadius) = Shape.Merge(
                n1.Centroid, n1.SquaredRadius, n2);
        }

        public override int FindHits(Ray ray, Blob.ItemHit[] hits, int count)
        {
            double x = ray.Origin.X - Centroid.X;
            double y = ray.Origin.Y - Centroid.Y;
            double z = ray.Origin.Z - Centroid.Z;
            double b = x * ray.Direction.X + y * ray.Direction.Y + z * ray.Direction.Z;
            return SquaredRadius + (b + x) * (b - x) < y * y + z * z
                ? count
                : n2.FindHits(ray, hits, n1.FindHits(ray, hits, count));
        }
    }

    /// <summary>A fork inside an axis-aligned bounding box.</summary>
    [Properties(nameof(Effectivity)), Children(nameof(n1), nameof(n2))]
    private sealed class BFork : BlobUnion
    {
        private readonly BlobUnion n1;
        private readonly BlobUnion n2;

        public BFork(BlobUnion n1, BlobUnion n2)
            : base(n1.Bounds + n2.Bounds)
        {
            this.n1 = n1;
            this.n2 = n2;
            (Centroid, SquaredRadius) = Shape.Merge(
                n1.Centroid, n1.SquaredRadius, n2);
        }

        public override int FindHits(Ray ray, Blob.ItemHit[] hits, int count) =>
            !Bounds.Intersects(ray, double.MaxValue)
                ? count
                : n2.FindHits(ray, hits, n1.FindHits(ray, hits, count));
    }

    /// <summary>Leaf surrounded by a bounding sphere.</summary>
    [Properties(nameof(Effectivity)), Children(nameof(items))]
    private sealed class SLeaf : BlobUnion
    {
        private readonly IBlobItem[] items;

        public SLeaf(IBlobItem[] items, int lo, int count,
            Bounds bounds, Vector centroid, double radius2)
            : base(bounds, centroid, radius2)
        {
            this.items = new IBlobItem[count];
            Array.Copy(items, lo, this.items, 0, count);
        }

        public override int FindHits(Ray ray, Blob.ItemHit[] hits, int count)
        {
            double x = ray.Origin.X - Centroid.X;
            double y = ray.Origin.Y - Centroid.Y;
            double z = ray.Origin.Z - Centroid.Z;
            double b = x * ray.Direction.X + y * ray.Direction.Y + z * ray.Direction.Z;
            if (SquaredRadius + (b + x) * (b - x) >= y * y + z * z)
            {
                Blob.ItemHit hit = new();
                foreach (IBlobItem item in items)
                    if (item.Hits(ray, ref x, ref y))
                    {
                        hit.item = item;
                        hit.time = x;
                        hit.enter = true;
                        hits[count++] = hit;
                        hit.time = y;
                        hit.enter = false;
                        hits[count++] = hit;
                    }
            }
            return count;
        }
    }

    /// <summary>Leaf with an axis-aligned bounding box.</summary>
    [Properties("effectivity"), Children("items")]
    private sealed class BLeaf : BlobUnion
    {
        private readonly IBlobItem[] items;

        public BLeaf(IBlobItem[] items, int lo, int count,
            Bounds bounds, Vector centroid, double radius2)
            : base(bounds, centroid, radius2)
        {
            this.items = new IBlobItem[count];
            Array.Copy(items, lo, this.items, 0, count);
        }

        public override int FindHits(Ray ray, Blob.ItemHit[] hits, int count)
        {
            if (Bounds.Intersects(ray, double.MaxValue))
            {
                double t1 = 0.0, t2 = 0.0;
                Blob.ItemHit hit = new();
                foreach (IBlobItem item in items)
                    if (item.Hits(ray, ref t1, ref t2))
                    {
                        hit.item = item;
                        hit.time = t1;
                        hit.enter = true;
                        hits[count++] = hit;
                        hit.time = t2;
                        hit.enter = false;
                        hits[count++] = hit;
                    }
            }
            return count;
        }
    }

    /// <summary>Leaf nodes with exactly two items.</summary>
    [Properties(nameof(Effectivity)), Children(nameof(item0), nameof(item1))]
    private sealed class ULeaf : BlobUnion
    {
        private readonly IBlobItem item0;
        private readonly IBlobItem item1;

        public ULeaf(IBlobItem[] items, int lo, Bounds bounds,
            Vector centroid, double radius2)
            : base(bounds, centroid, radius2) =>
            (item0, item1) = (items[lo], items[lo + 1]);

        public override int FindHits(Ray ray, Blob.ItemHit[] hits, int count)
        {
            double t1 = 0.0, t2 = 0.0;
            Blob.ItemHit hit = new();
            if (item0.Hits(ray, ref t1, ref t2))
            {
                hit.item = item0;
                hit.time = t1;
                hit.enter = true;
                hits[count++] = hit;
                hit.time = t2;
                hit.enter = false;
                hits[count++] = hit;
            }
            if (item1.Hits(ray, ref t1, ref t2))
            {
                hit.item = item1;
                hit.time = t1;
                hit.enter = true;
                hits[count++] = hit;
                hit.time = t2;
                hit.enter = false;
                hits[count++] = hit;
            }
            return count;
        }
    }

    private static class UnionBuilder
    {
        private sealed class ComparerX : IComparer<IBlobItem>
        {
            int IComparer<IBlobItem>.Compare(IBlobItem x, IBlobItem y) =>
                x.CentroidX.CompareTo(y.CentroidX);
        }

        private sealed class ComparerY : IComparer<IBlobItem>
        {
            int IComparer<IBlobItem>.Compare(IBlobItem x, IBlobItem y) =>
                x.CentroidY.CompareTo(y.CentroidY);
        }

        private sealed class ComparerZ : IComparer<IBlobItem>
        {
            int IComparer<IBlobItem>.Compare(IBlobItem x, IBlobItem y) =>
                x.CentroidZ.CompareTo(y.CentroidZ);
        }

        private static readonly ComparerX compX = new();
        private static readonly ComparerY compY = new();
        private static readonly ComparerZ compZ = new();

        public static BlobUnion Build(
            IBlobItem[] items, int lo, int hi, Axis oldAxis)
        {
            int count = hi - lo + 1;
            if (count < 10)
                return BuildLeaf(items, lo, hi);
            // Select an axis.
            Bounds b = Bounds.Void;
            for (int i = lo; i <= hi; i++)
                b += items[i].Bounds;
            Axis axis = b.DominantAxis;
            // Sort if not already sorted.
            if (axis != oldAxis)
                switch (axis)
                {
                    case Axis.X: Array.Sort(items, lo, count, compX); break;
                    case Axis.Y: Array.Sort(items, lo, count, compY); break;
                    case Axis.Z: Array.Sort(items, lo, count, compZ); break;
                }
            // Precompute areas.
            double[] rarea = new double[count];
            b = Bounds.Void;
            for (int i = hi; i >= lo; i--)
            {
                b += items[i].Bounds;
                rarea[i - lo] = b.HalfArea;
            }
            // Split this range.
            int split = (lo + hi) / 2;
            double best = double.MaxValue;
            b = items[lo].Bounds;
            for (int i = lo + 1; i < hi; i++)
            {
                b += items[i].Bounds;
                double v = b.HalfArea * (i - lo + 1) + rarea[i - lo + 1] * (hi - i);
                if (v < best)
                {
                    best = v;
                    split = i;
                }
            }
            return BuildFork(
                Build(items, lo, split, axis),
                Build(items, split + 1, hi, axis));
        }

        private static BlobUnion BuildLeaf(IBlobItem[] items, int lo, int hi)
        {
            Bounds b = Bounds.Void;
            Vector c = new();
            double r2 = -1.0;
            for (int i = lo; i <= hi; i++)
            {
                IBlobItem item = items[i];
                b += item.Bounds;
                (c, r2) = Shape.Merge(c, r2, item);
            }
            if (hi == lo + 1)
                return new ULeaf(items, lo, b, c, r2);
            else if (Math.Pow(r2, 1.5) / b.Volume <
                Properties.Settings.Default.BoundingSphereThreshold)
                return new SLeaf(items, lo, hi - lo + 1, b, c, r2);
            else
                return new BLeaf(items, lo, hi - lo + 1, b, c, r2);
        }

        private static BlobUnion BuildFork(BlobUnion u1, BlobUnion u2)
        {
            double r2 = Shape.Merge(u1, u2);
            return Math.Pow(r2, 1.5) < (u1.Bounds + u2.Bounds).Volume *
                Properties.Settings.Default.BoundingSphereThreshold
                ? new SFork(u1, u2)
                : new BFork(u1, u2);
        }
    }
}
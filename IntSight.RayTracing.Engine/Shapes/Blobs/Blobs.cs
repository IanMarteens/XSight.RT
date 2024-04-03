namespace IntSight.RayTracing.Engine;

/// <summary>Organic-shaped solids defined by isosurfaces.</summary>
[XSight, Properties(nameof(threshold), nameof(material)), Children(nameof(blobs))]
public sealed class Blob : MaterialShape, IShape
{
    /// <summary>Accumulator for finding blob's normals.</summary>
    public struct Normal
    {
        public double x, y, z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector ToVector()
        {
            double len = 1.0 / Math.Sqrt(x * x + y * y + z * z);
            return new(x * len, y * len, z * len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector ToNegatedVector()
        {
            double len = -1.0 / Math.Sqrt(x * x + y * y + z * z);
            return new(x * len, y * len, z * len);
        }
    }

    internal struct ItemHit
    {
        public double time;
        public IBlobItem item;
        public bool enter;
    }

    private class ItemHitComparer : IComparer<ItemHit>
    {
        int IComparer<ItemHit>.Compare(ItemHit x, ItemHit y) => x.time.CompareTo(y.time);
    }

    private class HitComparer : IComparer<Hit>
    {
        int IComparer<Hit>.Compare(Hit x, Hit y) => x.Time.CompareTo(y.Time);
    }

    private static readonly ItemHitComparer comparer = new();
    private static readonly HitComparer hitComparer = new();

    private IBlobItem[] items;
    private BlobUnion blobs;
    private ItemHit[] itemHits;
    private readonly double threshold;
    private readonly Ray testRay = new();
    private bool negated;

    public Blob(IBlobItem[] items, double threshold, IMaterial material)
        : base(material)
    {
        this.items = items;
        itemHits = new ItemHit[2 * items.Length];
        this.threshold = threshold;
        RecomputeBounds();
    }

    private void RecomputeBounds()
    {
        bounds = Bounds.Void;
        foreach (IBlobItem item in items)
            bounds += item.Bounds;
    }

    #region IShape members.

    /// <summary>Checks whether a given ray intersects the blob.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        double len = Math.Sqrt(ray.SquaredDir);
        testRay.Origin = ray.Origin;
        testRay.Direction = ray.Direction / len;

        int total = blobs.FindHits(testRay, itemHits, 0) - 1;
        switch (total)
        {
            case -1:
                return false;
            case 1:
                break;
            case 3:
                if (itemHits[2].time >= itemHits[0].time)
                {
                    if (itemHits[1].time > itemHits[2].time)
                        if (itemHits[3].time < itemHits[1].time)
                        {
                            var t = itemHits[1]; itemHits[1] = itemHits[2];
                            itemHits[2] = itemHits[3]; itemHits[3] = t;
                        }
                        else
                        {
                            (itemHits[2], itemHits[1]) = (itemHits[1], itemHits[2]);
                        }
                }
                else if (itemHits[3].time <= itemHits[0].time)
                {
                    (itemHits[2], itemHits[0]) = (itemHits[0], itemHits[2]);
                    (itemHits[3], itemHits[1]) = (itemHits[1], itemHits[3]);
                }
                else if (itemHits[3].time > itemHits[1].time)
                {
                    var t = itemHits[0]; itemHits[0] = itemHits[2];
                    itemHits[2] = itemHits[1]; itemHits[1] = t;
                }
                else
                {
                    var t = itemHits[0]; itemHits[0] = itemHits[2];
                    itemHits[2] = itemHits[3]; itemHits[3] = itemHits[1];
                    itemHits[1] = t;
                }
                break;
            default:
                Array.Sort(itemHits, 0, total + 1, comparer);
                break;
        }

        QuarticCoefficients coeffs = new()
        {
            c4 = -threshold
        };
        int active = 0;
        Solver.Roots roots = new();
        for (int i = 0; i < total; i++)
        {
            ItemHit hit = itemHits[i];
            double nextTime = itemHits[i + 1].time;
            if (hit.enter)
            {
                hit.item.AddCoefficients(ref coeffs);
                active++;
            }
            else
            {
                active--;
                hit.item.RemoveCoefficients(ref coeffs);
                if (--active == 0)
                    continue;
            }
            double d = 1.0 / coeffs.c0;
            Solver.Solve(coeffs.c1 * d, coeffs.c2 * d,
                coeffs.c3 * d, coeffs.c4 * d, ref roots);
            if (roots.HitTest(hit.time, nextTime, len) > 0.0)
                return true;
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
        int total = blobs.FindHits(ray, itemHits, 0) - 1;
        switch (total)
        {
            case -1:
                return false;
            case 1:
                break;
            case 3:
                if (itemHits[2].time >= itemHits[0].time)
                {
                    if (itemHits[1].time > itemHits[2].time)
                        if (itemHits[3].time < itemHits[1].time)
                        {
                            var t = itemHits[1]; itemHits[1] = itemHits[2];
                            itemHits[2] = itemHits[3]; itemHits[3] = t;
                        }
                        else
                        {
                            (itemHits[2], itemHits[1]) = (itemHits[1], itemHits[2]);
                        }
                }
                else if (itemHits[3].time <= itemHits[0].time)
                {
                    (itemHits[2], itemHits[0]) = (itemHits[0], itemHits[2]);
                    (itemHits[3], itemHits[1]) = (itemHits[1], itemHits[3]);
                }
                else if (itemHits[3].time > itemHits[1].time)
                {
                    var t = itemHits[0]; itemHits[0] = itemHits[2];
                    itemHits[2] = itemHits[1]; itemHits[1] = t;
                }
                else
                {
                    var t = itemHits[0]; itemHits[0] = itemHits[2];
                    itemHits[2] = itemHits[3]; itemHits[3] = itemHits[1];
                    itemHits[1] = t;
                }
                break;
            default:
                Array.Sort(itemHits, 0, total + 1, comparer);
                break;
        }

        QuarticCoefficients coeffs = new()
        {
            c4 = -threshold
        };
        int active = 0;
        Solver.Roots roots = new();
        for (int i = 0; i < total; i++)
        {
            ItemHit hit = itemHits[i];
            double nextTime = itemHits[i + 1].time;
            if (hit.enter)
            {
                hit.item.AddCoefficients(ref coeffs);
                active++;
            }
            else
            {
                hit.item.RemoveCoefficients(ref coeffs);
                if (--active == 0)
                    continue;
            }
            double d = 1.0 / coeffs.c0;
            Solver.Solve(
                coeffs.c1 * d, coeffs.c2 * d, coeffs.c3 * d, coeffs.c4 * d,
                ref roots);
            double t = roots.HitTest(hit.time, nextTime, maxt);
            if (t > 0.0)
            {
                info.Time = t;
                info.HitPoint = ray[t];
                Normal n = new();
                do
                    if (itemHits[i].enter)
                        itemHits[i].item.GetNormal(info.HitPoint, ref n);
                while (i-- > 0);
                info.Normal = n.ToVector();
                info.Material = material;
                return true;
            }
        }
        return false;
    }

    /// <summary>Intersects the blob with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        testRay.Origin = ray.Origin;
        testRay.Direction = ray.Direction.Normalized();
        int total = blobs.FindHits(testRay, itemHits, 0) - 1;
        if (total < 0)
            return 0;
        if (total > 1)
            Array.Sort(itemHits, 0, total + 1, comparer);

        QuarticCoefficients coeffs = new()
        {
            c4 = -threshold
        };
        int active = 0, count = 0;
        bool mustSort = false;
        Solver.Roots roots = new();
        for (int i = 0; i < total; i++)
        {
            ItemHit hit = itemHits[i];
            double nextTime = itemHits[i + 1].time;
            if (hit.enter)
            {
                active++;
                hit.item.AddCoefficients(ref coeffs);
            }
            else
            {
                active--;
                hit.item.RemoveCoefficients(ref coeffs);
            }
            if (active > 0)
            {
                Solver.Solve(coeffs.c1 / coeffs.c0, coeffs.c2 / coeffs.c0,
                    coeffs.c3 / coeffs.c0, coeffs.c4 / coeffs.c0, ref roots);
                switch (roots.Count)
                {
                    case 1:
                        if (hit.time <= roots.R0 && roots.R0 <= nextTime)
                        {
                            hits[count].Time = roots.R0;
                            hits[count++].Shape = this;
                            hits[count].Time = roots.R0;
                            hits[count++].Shape = this;
                        }
                        break;
                    case 2:
                        if (hit.time <= roots.R0 && roots.R0 <= nextTime)
                        {
                            hits[count].Time = roots.R0;
                            hits[count++].Shape = this;
                        }
                        if (hit.time <= roots.R1 && roots.R1 <= nextTime)
                        {
                            hits[count].Time = roots.R1;
                            hits[count++].Shape = this;
                        }
                        break;
                    case 3:
                        // The middle point is useless and must be discarded.
                        if (roots.R0 > roots.R1)
                        {
                            (roots.R1, roots.R0) = (roots.R0, roots.R1);
                        }
                        if (roots.R0 > roots.R2)
                        {
                            (roots.R2, roots.R0) = (roots.R0, roots.R2);
                        }
                        if (roots.R1 > roots.R2)
                            roots.R2 = roots.R1;
                        if (hit.time <= roots.R0 && roots.R0 <= nextTime)
                        {
                            hits[count].Time = roots.R0;
                            hits[count++].Shape = this;
                            mustSort = true;
                        }
                        if (hit.time <= roots.R2 && roots.R2 <= nextTime)
                        {
                            hits[count].Time = roots.R2;
                            hits[count++].Shape = this;
                            mustSort = true;
                        }
                        break;
                    case 4:
                        int added = 0;
                        if (hit.time <= roots.R0 && roots.R0 <= nextTime)
                        {
                            hits[count].Time = roots.R0;
                            hits[count++].Shape = this;
                            added = 1;
                        }
                        if (hit.time <= roots.R1 && roots.R1 <= nextTime)
                        {
                            hits[count].Time = roots.R1;
                            hits[count++].Shape = this;
                            added++;
                        }
                        if (hit.time <= roots.R2 && roots.R2 <= nextTime)
                        {
                            hits[count].Time = roots.R2;
                            hits[count++].Shape = this;
                            added++;
                        }
                        if (hit.time <= roots.R3 && roots.R3 <= nextTime)
                        {
                            hits[count].Time = roots.R3;
                            hits[count++].Shape = this;
                            added++;
                        }
                        if (added > 1)
                            mustSort = true;
                        break;
                }
            }
        }
        if (count <= 1)
            return 0;
        if (mustSort)
            Array.Sort(hits, 0, count, hitComparer);
        double len = 1.0 / ray.Direction.Length;
        if (len != 1.0)
            for (int i = 0; i < count; i++)
                hits[i].Time *= len;
        return count;
    }

    /// <summary>Computes the blob normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <param name="normal">Normal vector at that point.</param>
    Vector IShape.GetNormal(in Vector location)
    {
        Normal n = new();
        foreach (IBlobItem item in items)
            item.GetNormal(location, ref n);
        return negated ? n.ToNegatedVector() : n.ToVector();
    }

    #endregion

    #region ITransformable members.

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    int ITransformable.MaxHits => items.Length * 2;

    /// <summary>Estimated complexity.</summary>
    ShapeCost ITransformable.Cost => ShapeCost.PainInTheNeck;

    /// <summary>Invert normals for the right operand in a difference.</summary>
    void ITransformable.Negate() => negated = !negated;

    /// <summary>Creates a new independent copy of the whole blob.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        IBlobItem[] newItems = new IBlobItem[items.Length];
        for (int i = 0; i < newItems.Length; i++)
            newItems[i] = items[i].Clone();
        IShape b = new Blob(newItems, threshold, material.Clone(force));
        if (negated)
            b.Negate();
        ((Blob)b).blobs = BlobUnion.Build(((Blob)b).items);
        return b;
    }

    /// <summary>First optimization pass.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Simplify()
    {
        List<IBlobItem> newItems = [];
        foreach (IBlobItem item in items)
            item.Simplify(newItems);
        items = [.. newItems];
        itemHits = new ItemHit[2 * items.Length];
        return this;
    }

    /// <summary>Second optimization pass, where special shapes are introduced.</summary>
    /// <returns>This shape, or a new and more efficient equivalent.</returns>
    public override IShape Substitute()
    {
        for (int i = 0; i < items.Length; i++)
            items[i] = items[i].Substitute();
        blobs = BlobUnion.Build(items);
        return this;
    }

    /// <summary>Finds out how expensive would be statically rotating this blob.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation)
    {
        foreach (IBlobItem item in items)
            if (!item.CanRotate(rotation))
                return TransformationCost.Nope;
        return TransformationCost.Ok;
    }

    /// <summary>Finds out how expensive would be statically scaling this blob.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor)
    {
        foreach (IBlobItem item in items)
            if (!item.CanScale(factor))
                return TransformationCost.Nope;
        return TransformationCost.Ok;
    }

    /// <summary>Translate this blob.</summary>
    /// <param name="translation">Translation amount.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        for (int i = 0; i < items.Length; i++)
            items[i] = items[i].ApplyTranslation(translation);
        material = material.Translate(translation);
        RecomputeBounds();
    }

    /// <summary>Rotate this blob.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        for (int i = 0; i < items.Length; i++)
            items[i] = items[i].ApplyRotation(rotation);
        RecomputeBounds();
    }

    /// <summary>Scale this blob.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        for (int i = 0; i < items.Length; i++)
            items[i] = items[i].ApplyScale(factor);
        RecomputeBounds();
    }

    #endregion
}

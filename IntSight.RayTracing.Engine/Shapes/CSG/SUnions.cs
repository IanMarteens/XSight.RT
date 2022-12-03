using System.Linq;
using static System.Math;
using Rsc = IntSight.RayTracing.Engine.Properties.Resources;

namespace IntSight.RayTracing.Engine;

/// <summary>A spheric union with an arbitrary number of items.</summary>
[Properties(nameof(effectivity), nameof(Radius), nameof(checkBounds))]
[Children(nameof(shapes), nameof(tail))]
[SkipLocalsInit]
internal sealed class SUnion : UnionBase, IShape
{
    /// <summary>The list of shapes with an alternative order, for shadow testing.</summary>
    private IShape[] lightBuffer;
    /// <summary>Last subitem, for tail recursion.</summary>
    private SUnion tail;

    /// <summary>Creates a union with a bounding sphere.</summary>
    /// <param name="shapes">The list of shapes in the union.</param>
    public SUnion(params IShape[] shapes)
        : base(shapes, false, false) => checkBounds = true;

    public double Radius => Sqrt(squaredRadius);

    #region IShape members.

    /// <summary>Find an intersection between union items and a ray.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    public bool ShadowTest(Ray ray)
    {
        SUnion self = this;
        do
        {
            if (self.checkBounds)
            {
                var (x, y, z) = self.Centroid - ray.Origin;
                double b = x * ray.Direction.X + y * ray.Direction.Y + z * ray.Direction.Z;
                double ray2 = ray.SquaredDir;
                y = (self.squaredRadius - x * x - y * y - z * z) * ray2 + b * b;
                if (y < 0)
                    return false;
                y = Sqrt(y);
                if (b - y > ray2 || b + y < 0)
                    return false;
            }
            foreach (IShape shape in self.lightBuffer)
                if (shape.ShadowTest(ray))
                {
                    if (!self.inTransform)
                        BaseLight.lastOccluder ??= shape;
                    return true;
                }
            self = self.tail;
        }
        while (self != null);
        return false;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    public bool HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        SUnion self = this;
        do
        {
            if (self.checkBounds)
            {
                var (x, y, z) = self.Centroid - ray.Origin;
                double b = ray.Direction.Dot(x, y, z);
                y = (b + y) * (b - y) + self.squaredRadius - x * x - z * z;
                if (y < 0.0)
                    return false;
                y = Sqrt(y);
                if (b - y > maxt || b + y < 0)
                    return false;
            }
            ref IShape shapes = ref self.shapes[0];
            int len = self.shapes.Length;
            for (int i = 0; i < len; i++)
                if (Unsafe.Add(ref shapes, i).HitTest(ray, maxt, ref info))
                {
                    while (++i < len)
                        Unsafe.Add(ref shapes, i).HitTest(ray, info.Time, ref info);
                    self.tail?.HitTest(ray, info.Time, ref info);
                    return true;
                }
            self = self.tail;
        }
        while (self != null);
        return false;
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits) =>
        throw new RenderException(Rsc.ErrorInvalidShapeInCsg, "Unions");

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        if (tail != null)
        {
            var l = shapes.Select(s => s.Clone(force)).ToList();
            l.Add(((ITransformable)tail).Clone(force));
            return new SUnion(l.ToArray()) { checkBounds = checkBounds };
        }
        for (int i = 0; i < shapes.Length; i++)
        {
            IShape s = shapes[i].Clone(force);
            if (force || s != shapes[i])
            {
                IShape[] newShapes = new IShape[shapes.Length];
                for (int j = 0; j < i; j++)
                    newShapes[j] = shapes[j];
                newShapes[i] = s;
                for (int j = i + 1; j < newShapes.Length; j++)
                    newShapes[j] = shapes[j].Clone(force);
                return new SUnion(newShapes) { checkBounds = checkBounds };
            }
        }
        return this;
    }

    /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
    /// <param name="scene">The scene this shape is included within.</param>
    /// <param name="inCsg">Is this shape included in a CSG operation?</param>
    /// <param name="inTransform">Is this shape nested inside a transform?</param>
    /// <remarks>Children are sorted regarding their distance to the camera.</remarks>
    public override void Initialize(IScene scene, bool inCsg, bool inTransform)
    {
        Array.Sort(shapes, (s1, s2) => s1.CompareTo(s2, scene.Camera.Location));
        lightBuffer = scene.Lights.Length == 1 ? scene.Lights[0].Sort(shapes) : shapes;
        foreach (IShape s in shapes)
            s.NotifySphericBounds(Centroid, squaredRadius);
        base.Initialize(scene, inCsg, inTransform);
        if (tail == null)
        {
            tail = shapes[^1] as SUnion;
            if (tail != null)
            {
                Array.Resize(ref shapes, shapes.Length - 1);
                for (int pos = Array.IndexOf(lightBuffer, tail);
                    pos < lightBuffer.Length - 1; pos++)
                    lightBuffer[pos] = lightBuffer[pos + 1];
                Array.Resize(ref lightBuffer, lightBuffer.Length - 1);
            }
            else
            {
                tail = shapes[0] as SUnion;
                if (tail != null)
                {
                    Array.Copy(shapes, 1, shapes, 0, shapes.Length - 1);
                    Array.Resize(ref shapes, shapes.Length - 1);
                    for (int pos = Array.IndexOf(lightBuffer, tail);
                        pos < lightBuffer.Length - 1; pos++)
                        lightBuffer[pos] = lightBuffer[pos + 1];
                    Array.Resize(ref lightBuffer, lightBuffer.Length - 1);
                }
            }
        }
   }

    /// <summary>Notifies the shape that its container is checking spheric bounds.</summary>
    /// <param name="centroid">Centroid of parent's spheric bounds.</param>
    /// <param name="squaredRadius">Square radius of parent's spheric bounds.</param>
    public override void NotifySphericBounds(in Vector centroid, double squaredRadius)
    {
        // If spheric bounds are the same as ours, we can skip bound checking.
        if (this.squaredRadius > 0.9 * squaredRadius)
            checkBounds = false;
    }

    #endregion
}

/// <summary>A spheric union containing two items.</summary>
[Properties(nameof(effectivity), nameof(squaredRadius))]
[Children(nameof(shape0), nameof(shape1))]
[SkipLocalsInit]
internal sealed class SUnion2 : UnionBase, IShape
{
    private IShape shape0, shape1;
    private Hit[] hits0, hits1;

    /// <summary>Creates a binary union with a bounding sphere.</summary>
    /// <param name="shapes">The two involved shapes.</param>
    public SUnion2(IShape[] shapes)
        : base(shapes, false, false) => checkBounds = true;

    /// <summary>Updates direct references to shapes in the shape list.</summary>
    protected override void Rebind() => (shape1, shape0) = (shapes[1], shapes[0]);

    internal IShape ToUncheckedUnion()
    {
        Union2 result = new(shapes);
        ((IUnion)result).IsChecking = false;
        return result;
    }

    public double Radius => Sqrt(squaredRadius);

    #region IShape members.

    /// <summary>Find an intersection between union items and a ray.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        var (x, y, z) = Centroid - ray.Origin;
        double b = ray.Direction.Dot(x, y, z);
        double ray2 = ray.SquaredDir;
        y = (squaredRadius - x * x - y * y - z * z) * ray2 + b * b;
        if (y < 0 || b - (y = Sqrt(y)) > ray2 || b + y < 0)
            return false;
        if (shape0.ShadowTest(ray))
        {
            if (!inTransform)
                BaseLight.lastOccluder ??= shape0;
            return true;
        }
        if (shape1.ShadowTest(ray))
        {
            if (!inTransform)
                BaseLight.lastOccluder ??= shape1;
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
        var (x, y, z) = Centroid - ray.Origin;
        double b = ray.Direction.Dot(x, y, z);
        y = (b + x) * (b - x) + squaredRadius - y * y - z * z;
        if (y >= 0 && b - (y = Sqrt(y)) <= maxt && b + y >= 0)
            if (shape0.HitTest(ray, maxt, ref info))
            {
                shape1.HitTest(ray, info.Time, ref info);
                return true;
            }
            else
                return shape1.HitTest(ray, maxt, ref info);
        return false;
    }

    /// <summary>Intersects the shape with a ray and returns all intersections.</summary>
    /// <param name="ray">Ray to be tested.</param>
    /// <param name="hits">Preallocated array of intersections.</param>
    /// <returns>Number of found intersections.</returns>
    int IShape.GetHits(Ray ray, Hit[] hits)
    {
        int total1 = shape0.GetHits(ray, hits0);
        if (total1 == 0)
            return shape1.GetHits(ray, hits);
        int total2 = shape1.GetHits(ray, hits1);
        if (total2 == 0)
        {
            Array.Copy(hits0, hits, total1);
            return total1;
        }
        int i1 = 0, i2 = 0, total = 0;
        bool inside1 = false, inside2 = false, inside = false;
        do
            if (i1 < total1 && (i2 >= total2 || hits0[i1].Time <= hits1[i2].Time))
            {
                bool newInside = (inside1 = !inside1) | inside2;
                if (inside != newInside)
                {
                    hits[total++] = hits0[i1];
                    inside = newInside;
                }
                i1++;
            }
            else
            {
                bool newInside = inside1 | (inside2 = !inside2);
                if (inside != newInside)
                {
                    hits[total++] = hits1[i2];
                    inside = newInside;
                }
                i2++;
            }
        while (i1 < total1 || i2 < total2);
        return total;
    }

    #endregion

    #region ITransformable members.

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    IShape ITransformable.Clone(bool force)
    {
        IShape s0 = shape0.Clone(force), s1 = shape1.Clone(force);
        return force || s0 != shape0 || s1 != shape1 || hits0 != null
            ? new SUnion2(new[] { s0, s1 }) { checkBounds = checkBounds }
            : this;
    }

    /// <summary>Last minute optimizations and initializations requiring the camera.</summary>
    /// <param name="scene">The scene this shape is included within.</param>
    /// <param name="inCsg">Is this shape included in a CSG operation?</param>
    /// <param name="inTransform">Is this shape nested inside a transform?</param>
    /// <remarks>Children are sorted regarding their distance to the camera.</remarks>
    public override void Initialize(IScene scene, bool inCsg, bool inTransform)
    {
        Array.Sort(shapes, (s1, s2) => s1.CompareTo(s2, scene.Camera.Location));
        Rebind();
        if (inCsg)
        {
            hits0 = new Hit[shape0.MaxHits];
            hits1 = new Hit[shape1.MaxHits];
        }
        shape0.NotifySphericBounds(Centroid, squaredRadius);
        shape1.NotifySphericBounds(Centroid, squaredRadius);
        base.Initialize(scene, inCsg, inTransform);
    }

    #endregion
}
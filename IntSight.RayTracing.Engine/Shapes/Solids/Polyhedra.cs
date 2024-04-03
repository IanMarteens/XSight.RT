namespace IntSight.RayTracing.Engine;

/// <summary>Common base for all polyhedra.</summary>
public abstract class Polyhedron : MaterialShape, IShape
{
    protected readonly Vector[] normals;
    protected readonly double[] offsets;
    protected Vector centroid, p0;
    protected double squaredRadius;
    protected int lastIdx0, lastIdx1;
    protected bool negated;

    protected Polyhedron(Vector[] normals, double[] offsets,
        Vector centroid, double squaredRadius, Bounds bounds,
        IMaterial material)
        : base(material)
    {
        this.normals = normals;
        this.offsets = offsets;
        this.bounds = bounds;
        this.centroid = centroid;
        this.squaredRadius = squaredRadius;
    }

    protected Polyhedron(Vector[] normals, double offset,
        Vector centroid, double squaredRadius, IMaterial material)
        : this(
            normals, GetOffsets(normals.Length, offset), centroid, squaredRadius,
            Bounds.FromSphere(centroid, Math.Sqrt(squaredRadius)), material)
    { }

    protected static Vector Face(
        double x, double y, double z, double rx, double ry, double rz) =>
        Matrix.Rotation(0, 0, rz) * (Matrix.Rotation(0, ry, 0)
            * Matrix.Rotation(rx, 0, 0).Rotate(x, y, z));

    protected static Vector Face(double x, double y, double z, double rx, double ry) =>
        Matrix.Rotation(0, ry, 0) * Matrix.Rotation(rx, 0, 0).Rotate(x, y, z);

    protected static double[] GetOffsets(int count, double value)
    {
        double[] result = new double[count];
        for (int i = 0; i < result.Length; i++)
            result[i] = value;
        return result;
    }

    private double FindRoot(Ray ray, ref int index)
    {
        Vector org = ray.Origin, dir = ray.Direction;
        double h0 = double.MinValue, h1 = double.MaxValue;
        int idx0 = -1, idx1 = -1;
        for (int i = 0; i < normals.Length; i++)
        {
            Vector v = normals[i];
            double cos = v * dir;
            if (cos < 0.0)
            {
                double t = (offsets[i] - v * org) / cos;
                if (t > h0)
                {
                    h0 = t;
                    if (h0 > h1) return -1.0;
                    idx0 = i;
                }
            }
            else
            {
                double t = (offsets[i] - v * org) / cos;
                if (t < h1)
                {
                    h1 = t;
                    if (h0 > h1) return -1.0;
                    idx1 = i;
                }
            }
        }
        if (h0 >= Tolerance.Epsilon)
        {
            index = idx0;
            return h0;
        }
        if (h1 >= Tolerance.Epsilon)
        {
            index = idx1;
            return h1;
        }
        return -1.0;
    }

    #region IShape members.

    /// <summary>The center of the bounding sphere.</summary>
    public override Vector Centroid => centroid;

    /// <summary>The square radius of the bounding sphere.</summary>
    public override double SquaredRadius => squaredRadius;

    /// <summary>Checks whether a given ray intersects the shape.</summary>
    /// <param name="ray">Ray emitted by a light source.</param>
    /// <returns>True, when such an intersection exists.</returns>
    bool IShape.ShadowTest(Ray ray)
    {
        int index = 0;
        double t = FindRoot(ray, ref index);
        return 0.0 <= t && t <= 1.0;
    }

    /// <summary>Test intersection with a given ray.</summary>
    /// <param name="ray">Ray to be tested (direction is always normalized).</param>
    /// <param name="maxt">Upper bound for the intersection time.</param>
    /// <param name="info">Hit information, when an intersection is found.</param>
    /// <returns>True when an intersection is found.</returns>
    bool IShape.HitTest(Ray ray, double maxt, ref HitInfo info)
    {
        int index = 0;
        double t = FindRoot(ray, ref index);
        if (0.0 <= t && t <= maxt)
        {
            info.Time = t;
            info.HitPoint = ray[t];
            info.Normal = normals[index];
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
        Vector org = ray.Origin, dir = ray.Direction;
        double h0 = double.MinValue, h1 = double.MaxValue;
        int idx0 = -1, idx1 = -1;
        for (int i = 0; i < normals.Length; i++)
        {
            Vector v = normals[i];
            double cosine = v * dir;
            if (cosine < 0.0)
            {
                double t = (offsets[i] - v * org) / cosine;
                if (t > h0)
                {
                    h0 = t;
                    if (h0 > h1) return 0;
                    idx0 = i;
                }
            }
            else
            {
                double t = (offsets[i] - v * org) / cosine;
                if (t < h1)
                {
                    h1 = t;
                    if (h0 > h1) return 0;
                    idx1 = i;
                }
            }
        }
        hits[1].Time = h1;
        hits[0].Time = h0;
        hits[0].Shape = hits[1].Shape = this;
        lastIdx0 = idx0; lastIdx1 = idx1;
        p0 = ray[h0];
        return 2;
    }

    /// <summary>Computes the surface normal at a given location.</summary>
    /// <param name="location">Point to be tested.</param>
    /// <returns>Normal vector at that point.</returns>
    Vector IShape.GetNormal(in Vector location)
    {
        Vector normal = Math.Abs(location.X - p0.X) < Tolerance.Epsilon &&
            Math.Abs(location.Y - p0.Y) < Tolerance.Epsilon &&
            Math.Abs(location.Z - p0.Z) < Tolerance.Epsilon
            ? normals[lastIdx0]
            : normals[lastIdx1];
        return negated ? -normal : normal;
    }

    #endregion

    #region ITransformable members.

    /// <summary>Maximum number of hits when intersected with an arbitrary ray.</summary>
    /// <remarks>We are dealing with convex polyhedra only.</remarks>
    int ITransformable.MaxHits => 2;

    /// <summary>Gets the estimated complexity.</summary>
    ShapeCost ITransformable.Cost => ShapeCost.PainInTheNeck;

    /// <summary>Invert normals for the right operand in a difference.</summary>
    void ITransformable.Negate() => negated = !negated;

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    public abstract IShape Clone(bool force);

    /// <summary>Finds out how expensive would be statically rotating this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanRotate(in Matrix rotation) => TransformationCost.Ok;

    /// <summary>Finds out how expensive would be statically scaling this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The cost of the transformation.</returns>
    public override TransformationCost CanScale(in Vector factor) => TransformationCost.Ok;

    /// <summary>Translates this shape.</summary>
    /// <param name="translation">Translation distance.</param>
    public override void ApplyTranslation(in Vector translation)
    {
        for (int i = 0; i < normals.Length; i++)
            offsets[i] += translation * normals[i];
        material = material.Translate(translation);
        bounds += translation;
        centroid += translation;
    }

    /// <summary>Rotates this shape.</summary>
    /// <param name="rotation">Rotation amount.</param>
    public override void ApplyRotation(in Matrix rotation)
    {
        for (int i = 0; i < normals.Length; i++)
            normals[i] = rotation * normals[i];
        centroid = rotation * centroid;
        bounds = bounds.Rotate(rotation) * Bounds.FromSphere(
            centroid, Math.Sqrt(squaredRadius));
    }

    /// <summary>Scales this shape.</summary>
    /// <param name="factor">Scale factor.</param>
    public override void ApplyScale(in Vector factor)
    {
        Vector inv = new(1 / factor.X, 1 / factor.Y, 1 / factor.Z);
        for (int i = 0; i < normals.Length; i++)
        {
            Vector newNormal = normals[i].Scale(inv);
            double len = newNormal.Length;
            offsets[i] /= len;
            normals[i] = newNormal / len;
        }
        bounds = bounds.Scale(factor);
        centroid = centroid.Scale(factor);
        double mf = Math.Max(factor.X, Math.Max(factor.Y, factor.Z));
        squaredRadius *= mf * mf;
    }

    #endregion
}

/// <summary>A regular polyhedron with four faces.</summary>
[XSight, Properties(nameof(squaredRadius))]
public sealed class Tetrahedron : Polyhedron
{
    public Tetrahedron(IMaterial material)
        : base(GetNormals(), 1.0 / 3.0, new Vector(), 1.0, material) { }

    private Tetrahedron(Vector[] normals, double[] offsets,
        Vector centroid, double squaredRadius, Bounds bounds, IMaterial material)
        : base(normals, offsets, centroid, squaredRadius, bounds, material) { }

    private static Vector[] GetNormals() =>
    [
        Face(0, -1, 0,     0,   0),
        Face(0, 0, -1, 19.47,   0),
        Face(0, 0, -1, 19.47, 120),
        Face(0, 0, -1, 19.47, 240)
    ];

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    public override IShape Clone(bool force) =>
        new Tetrahedron(
            (Vector[])normals.Clone(), (double[])offsets.Clone(),
            centroid, squaredRadius, bounds,
            material.Clone(force));
}

/// <summary>A regular polyhedron with eight faces.</summary>
[XSight, Properties(nameof(squaredRadius))]
public sealed class Octahedron : Polyhedron
{
    public Octahedron(IMaterial material)
        : base(GetNormals(), 1.0 / 1.7321, new Vector(), 1.0, material) { }

    private Octahedron(Vector[] normals, double[] offsets,
        Vector centroid, double squaredRadius, Bounds bounds, IMaterial material)
        : base(normals, offsets, centroid, squaredRadius, bounds, material) { }

    private static Vector[] GetNormals() =>
    [
        Face(0, 0, +1, +35.26438968275, 0),
        Face(0, 0, +1, -35.26438968275, 0),
        Face(0, 0, -1, +35.26438968275, 0),
        Face(0, 0, -1, -35.26438968275, 0),
        Face(+1, 0, 0, 0, 0, +35.26438968275),
        Face(+1, 0, 0, 0, 0, -35.26438968275),
        Face(-1, 0, 0, 0, 0, +35.26438968275),
        Face(-1, 0, 0, 0, 0, -35.26438968275)
    ];

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    public override IShape Clone(bool force) =>
        new Octahedron(
            (Vector[])normals.Clone(), (double[])offsets.Clone(),
            centroid, squaredRadius, bounds,
            material.Clone(force));
}

/// <summary>A regular polyhedron with twelve regular pentagonal faces.</summary>
[XSight, Properties(nameof(squaredRadius))]
public sealed class Dodecahedron : Polyhedron
{
    public Dodecahedron(IMaterial material)
        : base(GetNormals(), 1.0 / 1.2585, new Vector(), 1.0, material) { }

    private Dodecahedron(Vector[] normals, double[] offsets,
        Vector centroid, double squaredRadius, Bounds bounds, IMaterial material)
        : base(normals, offsets, centroid, squaredRadius, bounds, material) { }

    private static Vector[] GetNormals() =>
    [
        Face(0, 0, -1, -26.56505117708,    0),
        Face(0, 0, -1, -26.56505117708,  -72),
        Face(0, 0, -1, -26.56505117708, -144),
        Face(0, 0, -1, -26.56505117708, -216),
        Face(0, 0, -1, -26.56505117708, -288),
        Face(0, 0, -1, +26.56505117708,  -36),
        Face(0, 0, -1, +26.56505117708, -108),
        Face(0, 0, -1, +26.56505117708, -180),
        Face(0, 0, -1, +26.56505117708, -252),
        Face(0, 0, -1, +26.56505117708, -324),
        Face(0, +1, 0, 0, 0),
        Face(0, -1, 0, 0, 0)
    ];

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    public override IShape Clone(bool force) =>
        new Dodecahedron(
            (Vector[])normals.Clone(), (double[])offsets.Clone(),
            centroid, squaredRadius, bounds,
            material.Clone(force));
}

/// <summary>A regular polyhedron with twenty equilateral triangles as faces.</summary>
[XSight, Properties(nameof(squaredRadius))]
public sealed class Icosahedron : Polyhedron
{
    public Icosahedron(IMaterial material)
        : base(GetNormals(), 1.0 / 1.2585, new Vector(), 1.0, material) { }

    private Icosahedron(Vector[] normals, double[] offsets,
        Vector centroid, double squaredRadius, Bounds bounds, IMaterial material)
        : base(normals, offsets, centroid, squaredRadius, bounds, material) { }

    private static Vector[] GetNormals() =>
    [
        Face(0, 0, -1, +52.6625,    0),
        Face(0, 0, -1, +52.6625,  -72),
        Face(0, 0, -1, +52.6625, -144),
        Face(0, 0, -1, +52.6625, -216),
        Face(0, 0, -1, +52.6625, -288),

        Face(0, 0, -1, +10.8125,    0),
        Face(0, 0, -1, +10.8125,  -72),
        Face(0, 0, -1, +10.8125, -144),
        Face(0, 0, -1, +10.8125, -216),
        Face(0, 0, -1, +10.8125, -288),

        Face(0, 0, -1, -52.6625,  -36),
        Face(0, 0, -1, -52.6625, -108),
        Face(0, 0, -1, -52.6625, -180),
        Face(0, 0, -1, -52.6625, -252),
        Face(0, 0, -1, -52.6625, -324),

        Face(0, 0, -1, -10.8125,  -36),
        Face(0, 0, -1, -10.8125, -108),
        Face(0, 0, -1, -10.8125, -180),
        Face(0, 0, -1, -10.8125, -252),
        Face(0, 0, -1, -10.8125, -324)
    ];

    /// <summary>Creates a new independent copy of the icosahedron.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    public override IShape Clone(bool force) =>
        new Icosahedron(
            (Vector[])normals.Clone(), (double[])offsets.Clone(),
            centroid, squaredRadius, bounds,
            material.Clone(force));
}

[XSight, Properties(nameof(squaredRadius), nameof(centroid))]
public sealed class Pyramid : Polyhedron
{
    private double side, height;

    public Pyramid(double side, double height, IMaterial material)
        : base(GetNormals(side, height), GetOffsets(side, height),
        GetCentroid(side, height), GetSquaredRadius(side, height),
        Bounds.Create(-0.5 * side, 0.0, -0.5 * side, +0.5 * side, height, +0.5 * side),
        material) => (this.side, this.height) = (side, height);

    private Pyramid(Vector[] normals, double[] offsets,
        Vector centroid, double squaredRadius, Bounds bounds, IMaterial material)
        : base(normals, offsets, centroid, squaredRadius, bounds, material) { }

    /// <summary>Creates a new independent copy of the whole shape.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new shape.</returns>
    public override IShape Clone(bool force) =>
        new Pyramid(
            (Vector[])normals.Clone(), (double[])offsets.Clone(),
            centroid, squaredRadius, bounds,
            material.Clone(force))
        {
            side = side,
            height = height
        };

    private static Vector GetCentroid(double side, double height) =>
        Math.Sqrt(2.0) * height < side
            ? new()
            : new(0.0, 0.5 * height - 0.25 * side * side / height, 0.0);

    private static double GetSquaredRadius(double side, double height)
    {
        if (Math.Sqrt(2.0) * height < side)
            return 0.5 * side * side;
        else
        {
            double r = 0.5 * height + 0.25 * side * side / height;
            return r * r;
        }
    }

    private static Vector[] GetNormals(double side, double height) =>
    [
        Face(0, -1, 0, 0, 0),
        new Vector(+height, 0.5 * side, 0).Normalized(),
        new Vector(0, 0.5 * side, +height).Normalized(),
        new Vector(-height, 0.5 * side, 0).Normalized(),
        new Vector(0, 0.5 * side, -height).Normalized()
    ];

    private static double[] GetOffsets(double side, double height)
    {
        double offset = 0.5 * side * height /
            Math.Sqrt(0.25 * side * side + height * height);
        return [0.0, offset, offset, offset, offset];
    }
}
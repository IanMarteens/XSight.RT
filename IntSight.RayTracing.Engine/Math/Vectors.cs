using System.Xml;
using static System.Math;

namespace IntSight.RayTracing.Engine;

/// <summary>Three dimensional immutable vectors.</summary>
[SkipLocalsInit]
public readonly struct Vector
{
    /// <summary>The vector with zero in all of its components.</summary>
    public static readonly Vector Null;
    /// <summary>Unit vector along the X ray.</summary>
    public static readonly Vector XRay = new(1, 0, 0);
    /// <summary>Unit vector along the Y ray.</summary>
    public static readonly Vector YRay = new(0, 1, 0);
    /// <summary>Unit vector along the Z ray.</summary>
    public static readonly Vector ZRay = new(0, 0, 1);
    /// <summary>Unit vector opposite to the X ray.</summary>
    public static readonly Vector XRayM = new(-1, 0, 0);
    /// <summary>Unit vector opposite to the Y ray.</summary>
    public static readonly Vector YRayM = new(0, -1, 0);
    /// <summary>Unit vector opposite to the Z ray.</summary>
    public static readonly Vector ZRayM = new(0, 0, -1);

    /// <summary>X component.</summary>
    public readonly double X;
    /// <summary>Y component.</summary>
    public readonly double Y;
    /// <summary>Z component.</summary>
    public readonly double Z;

    /// <summary>Creates a vector given its three components.</summary>
    /// <param name="x">X component.</param>
    /// <param name="y">Y component.</param>
    /// <param name="z">Z component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector(double x, double y, double z) => (X, Y, Z) = (x, y, z);

    /// <summary>Creates a diagonal vector.</summary>
    /// <param name="value">Value for each vector's component.</param>
    public Vector(double value) => X = Y = Z = value;

    /// <summary>Deconstruct a vector into its three components.</summary>
    /// <param name="x">X component.</param>
    /// <param name="y">Y component.</param>
    /// <param name="z">Z component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out double x, out double y, out double z) =>
        (x, y, z) = (X, Y, Z);

    /// <summary>Gets the Pithagoric length of the vector.</summary>
    public double Length => Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>Gets the squared length of the vector.</summary>
    public double Squared => X * X + Y * Y + Z * Z;

    /// <summary>Gets the reciprocal of the vector.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Invert() => new(1 / X, 1 / Y, 1 / Z);

    /// <summary>Creates a scaled copy of a vector.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>A scaled copy of the original vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Scale(in Vector factor) =>
        new(X * factor.X, Y * factor.Y, Z * factor.Z);

    /// <summary>Scales a vector and normalizes the result.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The scaled and normalized vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector ScaleNormal(in Vector factor)
    {
        double x = X * factor.X, y = Y * factor.Y, z = Z * factor.Z;
        double len = 1.0 / Sqrt(x * x + y * y + z * z);
        return new(x * len, y * len, z * len);
    }

    /// <summary>Scales a vector and normalizes the result.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <param name="length">Length of the vector after scaling.</param>
    /// <returns>The scaled and normalized vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector ScaleNormal(in Vector factor, out double length)
    {
        double x = X * factor.X, y = Y * factor.Y, z = Z * factor.Z;
        double len = length = 1.0 / Sqrt(x * x + y * y + z * z);
        return new(x * len, y * len, z * len);
    }

    /// <summary>Checks whether the parameter is a vector with the same components.</summary>
    /// <param name="obj">Object to check.</param>
    /// <returns>True when a vector is passed with identical components.</returns>
    public override bool Equals(object obj) =>
        obj is Vector vector && this == vector;

    /// <summary>Returns the hash code for this vector.</summary>
    /// <returns>The combined hash code for its three components.</returns>
    public override int GetHashCode() =>
        X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

    /// <summary>Checks two vectors for equality.</summary>
    public static bool operator ==(in Vector v1, in Vector v2) =>
        v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z;

    /// <summary>Checks two vectors for inequality.</summary>
    public static bool operator !=(in Vector v1, in Vector v2) =>
        v1.X != v2.X || v1.Y != v2.Y || v1.Z != v2.Z;

    /// <summary>Vector addition.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator +(in Vector v1, in Vector v2) =>
        new(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);

    /// <summary>Vector substraction.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator -(in Vector v1, in Vector v2) =>
        new(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);

    /// <summary>Unary minus: inverts vector components.</summary>
    public static Vector operator -(in Vector v) =>
        new(-v.X, -v.Y, -v.Z);

    /// <summary>Scalar product.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator *(in Vector v, double f) =>
        new(v.X * f, v.Y * f, v.Z * f);

    /// <summary>Scalar product.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator *(double f, in Vector v) =>
        new(v.X * f, v.Y * f, v.Z * f);

    /// <summary>Inner vectorial product.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double operator *(in Vector v1, in Vector v2) =>
        v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;

    /// <summary>Scalar division.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator /(in Vector v, double f) => v * (1 / f);

    /// <summary>Cross product operator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector operator ^(in Vector v1, in Vector v2) =>
        new(
            v1.Y * v2.Z - v1.Z * v2.Y,
            v1.Z * v2.X - v1.X * v2.Z,
            v1.X * v2.Y - v1.Y * v2.X);

    /// <summary>Normalized copy.</summary>
    /// <remarks>
    /// This method handles the special case of zero vectors.
    /// </remarks>
    /// <returns>A normalized vector, or the same vector if zero.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Norm()
    {
        double len = 1.0 / Sqrt(X * X + Y * Y + Z * Z);
        return !double.IsInfinity(len) ? new(X * len, Y * len, Z * len) : this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Normalized()
    {
        double len = 1.0 / Sqrt(X * X + Y * Y + Z * Z);
        return new(X * len, Y * len, Z * len);
    }

    /// <summary>Inner vectorial product.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double Dot(double x, double y, double z) => x * X + y * Y + z * Z;

    /// <summary>Normalized mirror vector relative to an arbitrary axis.</summary>
    /// <remarks>Input vectors are expected to be "almost" normalized</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Mirror(in Vector axis)
    {
        double dot = 2.0 * (X * axis.X + Y * axis.Y + Z * axis.Z);
        double x = X - dot * axis.X, y = Y - dot * axis.Y, z = Z - dot * axis.Z;
        dot = 1.0 / Sqrt(x * x + y * y + z * z);
        return new(x * dot, y * dot, z * dot);
    }

    /// <summary>Normalized vector difference.</summary>
    public Vector Difference(in Vector origin)
    {
        double dx = X - origin.X, dy = Y - origin.Y, dz = Z - origin.Z;
        double len = Sqrt(dx * dx + dy * dy + dz * dz);
        return len > 0 ?
            new(dx / len, dy / len, dz / len) :
            new();
    }

    /// <summary>Adds a 2D vector to the X, Y components.</summary>
    /// <param name="x">The X component to add.</param>
    /// <param name="y">The Y component to add.</param>
    /// <returns>The summand.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector Add2D(double x, double y) =>
        new(X + x, Y + y, Z);

    /// <summary>Pythagoric distance between vectors.</summary>
    /// <param name="another">Second vector.</param>
    /// <returns>Distance between vectors.</returns>
    public double Distance(in Vector another)
    {
        double dx = X - another.X, dy = Y - another.Y, dz = Z - another.Z;
        return Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>Squared distance between this vector and the given coordinates.</summary>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="z">Z position.</param>
    /// <returns>The squared distance to the given point.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double SquaredDistance(double x, double y, double z)
    {
        x -= X; y -= Y; z -= Z;
        return x * x + y * y + z * z;
    }

    /// <summary>Manhattan distance between this vector and the given coordinates.</summary>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="z">Z position.</param>
    /// <returns>The Manhattan distance to the given point.</returns>
    public double ManhattanDistance(double x, double y, double z) =>
        Abs(X - x) + Abs(Y - y) + Abs(Z - z);

    /// <summary>Writes this vector as an element into a XML document.</summary>
    /// <param name="writer">The XML target.</param>
    /// <param name="elementName">Name of the element to be written.</param>
    public void WriteXmlAttribute(XmlWriter writer, string elementName)
    {
        writer.WriteStartElement(elementName);
        writer.WriteStartAttribute(nameof(X));
        writer.WriteValue(X);
        writer.WriteEndAttribute();
        writer.WriteStartAttribute(nameof(Y));
        writer.WriteValue(Y);
        writer.WriteEndAttribute();
        writer.WriteStartAttribute(nameof(Z));
        writer.WriteValue(Z);
        writer.WriteEndAttribute();
        writer.WriteEndElement();
    }

    /// <summary>Gets a textual representation for this vector.</summary>
    /// <returns>The three vector components formatted with the invariant culture.</returns>
    public override string ToString() =>
        string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0:F2},{1:F2},{2:F2}", X, Y, Z);
}

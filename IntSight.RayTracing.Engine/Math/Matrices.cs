using System.Runtime.CompilerServices;
using System.Xml;
using static System.Math;

namespace IntSight.RayTracing.Engine
{
    /// <summary>3D transformation matrices.</summary>
    [SkipLocalsInit]
    public readonly struct Matrix
    {
        /// <summary>Conversion factor between radians and degrees.</summary>
        private const double PI_180 = PI / 180.0;

        /// <summary>The identity transformation matrix.</summary>
        public static readonly Matrix Identity = new(1, 1, 1);

        /// <summary>Matrix components.</summary>
        public readonly double A11, A12, A13, A21, A22, A23, A31, A32, A33;

        /// <summary>Creates a matrix given each of his components.</summary>
        public Matrix(
            double a11, double a12, double a13,
            double a21, double a22, double a23,
            double a31, double a32, double a33)
        {
            A11 = a11; A12 = a12; A13 = a13;
            A21 = a21; A22 = a22; A23 = a23;
            A31 = a31; A32 = a32; A33 = a33;
        }

        /// <summary>Creates a transformation matrix given three orthogonal vectors.</summary>
        /// <param name="eye">Origin of transformation.</param>
        /// <param name="lookAt">Target point.</param>
        /// <param name="sky">Vector indicating the sky position.</param>
        public Matrix(in Vector eye, in Vector lookAt, in Vector sky)
        {
            (A13, A23, A33) = (lookAt - eye).Normalized();
            (A11, A21, A31) = (sky ^ new Vector(A13, A23, A33)).Normalized();
            (A12, A22, A32) = new Vector(A13, A23, A33) ^ new Vector(A11, A21, A31);
        }

        /// <summary>Creates a diagonal matrix given the diagonal components.</summary>
        /// <param name="x">First diagonal component.</param>
        /// <param name="y">Second diagonal component.</param>
        /// <param name="z">Third diagonal component.</param>
        public Matrix(double x, double y, double z)
        {
            A11 = x; A12 = 0; A13 = 0;
            A21 = 0; A22 = y; A23 = 0;
            A31 = 0; A32 = 0; A33 = z;
        }

        /// <summary>Rotates a vector.</summary>
        /// <param name="m">Transforming matrix.</param>
        /// <param name="v">Vector to rotate.</param>
        /// <returns>A new rotated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector operator*(in Matrix m, in Vector v) => new(
            m.A11 * v.X + m.A12 * v.Y + m.A13 * v.Z,
            m.A21 * v.X + m.A22 * v.Y + m.A23 * v.Z,
            m.A31 * v.X + m.A32 * v.Y + m.A33 * v.Z);

        /// <summary>Rotates a vector given its component.</summary>
        /// <param name="x">X component.</param>
        /// <param name="y">Y component.</param>
        /// <param name="z">Z component.</param>
        /// <returns>A new rotated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector Rotate(double x, double y, double z) => new(
            A11 * x + A12 * y + A13 * z,
            A21 * x + A22 * y + A23 * z,
            A31 * x + A32 * y + A33 * z);

        /// <summary>Rotates a vector and normalizes the result.</summary>
        /// <param name="v">Input vector.</param>
        /// <param name="length">The inverted length of the rotated vector.</param>
        /// <returns>Output vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector RotateNorm(in Vector v, out double length)
        {
            double x = A11 * v.X + A12 * v.Y + A13 * v.Z;
            double y = A21 * v.X + A22 * v.Y + A23 * v.Z;
            double z = A31 * v.X + A32 * v.Y + A33 * v.Z;
            double len = 1.0 / Sqrt(x * x + y * y + z * z);
            length = len;
            return new(x * len, y * len, z * len);
        }

        /// <summary>Transforms vector components and normalize the result.</summary>
        /// <remarks>Assumes the input vector to be not null.</remarks>
        /// <param name="vx">X component.</param>
        /// <param name="vy">Y component.</param>
        /// <param name="vz">Z component.</param>
        /// <returns>Normalized transformation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector RotateNorm(double vx, double vy, double vz)
        {
            double x = A11 * vx + A12 * vy + A13 * vz;
            double y = A21 * vx + A22 * vy + A23 * vz;
            double z = A31 * vx + A32 * vy + A33 * vz;
            vx = 1.0 / Sqrt(x * x + y * y + z * z);
            return new(x * vx, y * vx, z * vx);
        }

        /// <summary>Rotates and find the reciprocal of a vector.</summary>
        /// <param name="v">Vector to rotate.</param>
        /// <returns>Rotated and inverted vector</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector RotateInvert(in Vector v) => new(
            1.0 / (A11 * v.X + A12 * v.Y + A13 * v.Z),
            1.0 / (A21 * v.X + A22 * v.Y + A23 * v.Z),
            1.0 / (A31 * v.X + A32 * v.Y + A33 * v.Z));

        /// <summary>Rotates a vector, finds its reciprocal and normalizes it.</summary>
        /// <param name="v">Input vector.</param>
        /// <param name="r">Output vector.</param>
        /// <returns>The length of the rotated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double RotateInvertNorm(in Vector v, out Vector r)
        {
            double dx = A11 * v.X + A12 * v.Y + A13 * v.Z;
            double dy = A21 * v.X + A22 * v.Y + A23 * v.Z;
            double dz = A31 * v.X + A32 * v.Y + A33 * v.Z;
            double len = Sqrt(dx * dx + dy * dy + dz * dz);
            r = new(len / dx, len / dy, len / dz);
            return len;
        }

        /// <summary>Rotates a vector with null Y component.</summary>
        /// <param name="x">Vector's X component.</param>
        /// <param name="z">Vector's Y component.</param>
        /// <returns>The rotated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector RotateXZ(double x, double z) =>
            new(A11 * x + A13 * z, A21 * x + A23 * z, A31 * x + A33 * z);

        /// <summary>Combines the effects of two transformations.</summary>
        public static Matrix operator *(in Matrix m1, in Matrix m2) => new(
            m1.A11 * m2.A11 + m1.A12 * m2.A21 + m1.A13 * m2.A31,
            m1.A11 * m2.A12 + m1.A12 * m2.A22 + m1.A13 * m2.A32,
            m1.A11 * m2.A13 + m1.A12 * m2.A23 + m1.A13 * m2.A33,
            m1.A21 * m2.A11 + m1.A22 * m2.A21 + m1.A23 * m2.A31,
            m1.A21 * m2.A12 + m1.A22 * m2.A22 + m1.A23 * m2.A32,
            m1.A21 * m2.A13 + m1.A22 * m2.A23 + m1.A23 * m2.A33,
            m1.A31 * m2.A11 + m1.A32 * m2.A21 + m1.A33 * m2.A31,
            m1.A31 * m2.A12 + m1.A32 * m2.A22 + m1.A33 * m2.A32,
            m1.A31 * m2.A13 + m1.A32 * m2.A23 + m1.A33 * m2.A33);

        /// <summary>Scalar product for matrices.</summary>
        public static Matrix operator *(double f, in Matrix m) => new(
            f * m.A11, f * m.A12, f * m.A13,
            f * m.A21, f * m.A22, f * m.A23,
            f * m.A31, f * m.A32, f * m.A33);

        /// <summary>Transposed and inverse are the same for orthogonal matrices.</summary>
        public Matrix Transpose() =>
            new(A11, A21, A31, A12, A22, A32, A13, A23, A33);

        /// <summary>Creates a rotation matrix from three rotation angles.</summary>
        /// <param name="x">Rotation around the X axis, in degrees.</param>
        /// <param name="y">Rotation around the Y axis, in degrees.</param>
        /// <param name="z">Rotation around the Z axis, in degrees.</param>
        /// <returns>The corresponding orthogonal matrix.</returns>
        public static Matrix Rotation(double x, double y, double z)
        {
            double sx, cx, sy, cy, sxsy, cxsy, sz, cz, sxsz, cxsz;
            if (x == 0.0)
            {
                sx = 0.0;
                cx = 1.0;
            }
            else
            {
                sx = Sin(x *= PI_180);
                cx = Cos(x);
            }
            if (y == 0.0)
            {
                sy = sxsy = cxsy = 0.0;
                cy = 1.0;
            }
            else
            {
                sy = Sin(y *= PI_180);
                cy = Cos(y);
                sxsy = sx * sy;
                cxsy = cx * sy;
            }
            if (z == 0.0)
            {
                sz = sxsz = cxsz = 0.0;
                cz = 1.0;
            }
            else
            {
                sz = Sin(z *= PI_180);
                cz = Cos(z);
                sxsz = sx * sz;
                cxsz = cx * sz;
            }
            return new(
                cy * cz, sxsy * cz - cxsz, cxsy * cz + sxsz,
                cy * sz, sxsy * sz + cx * cz, cxsy * sz - sx * cz,
                -sy, sx * cy, cx * cy);
        }

        /// <summary>Decompose the matrix into three rotations: X, Y and then Z.</summary>
        /// <returns>A vector containing the three angles.</returns>
        public Vector GetRotations() => 
            !Tolerance.Near(Abs(A31), 1)
                ? new(Atan2(A32, A33), -Asin(A31), Atan2(A21, A11))
                : new(0.0, -Asin(A31), Atan2(A23, A13));

        /// <summary>Has this matrix been composed by rotations in right angles?</summary>
        public bool HasRightAngles =>
            Tolerance.Near(A22, 1.0) &&
            Tolerance.Zero(A12) && Tolerance.Zero(A21) &&
            Tolerance.Zero(A23) && Tolerance.Zero(A32) &&
            (Tolerance.Zero(A11) && Tolerance.Zero(A33) ||
            Tolerance.Zero(A13) && Tolerance.Zero(A31)) ||

            Tolerance.Near(A11, 1.0) &&
            Tolerance.Zero(A12) && Tolerance.Zero(A13) &&
            Tolerance.Zero(A21) && Tolerance.Zero(A31) &&
            (Tolerance.Zero(A22) && Tolerance.Zero(A33) ||
            Tolerance.Zero(A23) && Tolerance.Zero(A32)) ||

            Tolerance.Near(A33, 1.0) &&
            Tolerance.Zero(A13) && Tolerance.Zero(A23) &&
            Tolerance.Zero(A31) && Tolerance.Zero(A32) &&
            (Tolerance.Zero(A11) && Tolerance.Zero(A22) ||
            Tolerance.Zero(A12) && Tolerance.Zero(A21));

        /// <summary>Determinant is always 1 for orthogonal matrices.</summary>
        public double Determinant =>
            (A22 * A33 - A23 * A32) * A11 +
            (A23 * A31 - A21 * A33) * A12 +
            (A32 * A21 - A31 * A22) * A13;

        /// <summary>Is this the identity matrix?</summary>
        public bool IsIdentity =>
            A11 == 1.0 && A12 == 0.0 && A13 == 0.0 &&
            A21 == 0.0 && A22 == 1.0 && A23 == 0.0 &&
            A31 == 0.0 && A32 == 0.0 && A33 == 1.0;

        /// <summary>Is this a diagonal matrix with the specified diagonal values?</summary>
        /// <param name="x">Value expected in the A11 component.</param>
        /// <param name="y">Value expected in the A22 component.</param>
        /// <param name="z">Value expected in the A33 component.</param>
        /// <returns>True, if this is a diagonal matrix.</returns>
        public bool IsDiagonal(double x, double y, double z) =>
            Tolerance.Near(A11, x) && A12 == 0.0 && A13 == 0.0 &&
            A21 == 0.0 && Tolerance.Near(A22, y) && A23 == 0.0 &&
            A31 == 0.0 && A32 == 0.0 && Tolerance.Near(A33, z);

        /// <summary>Does this transformation commutes with a scale change?</summary>
        /// <param name="factor">Scale factor.</param>
        /// <returns>True if the transformations are commutable.</returns>
        public bool CanScale(in Vector factor) =>
            factor.X == factor.Y && factor.Y == factor.Z ||
            factor.Y == factor.Z && Tolerance.IsZero(A11 - 1.0, A21, A31) ||
            factor.X == factor.Z && Tolerance.IsZero(A12, A22 - 1.0, A32) ||
            factor.X == factor.Y && Tolerance.IsZero(A13, A23, A33 - 1.0);

        /// <summary>Writes a matrix component as an XML attribute.</summary>
        /// <param name="writer">XML writer.</param>
        /// <param name="attrName">The attribute name.</param>
        /// <param name="value">The value from the matrix component.</param>
        private static void Write(XmlWriter writer, string attrName, double value)
        {
            writer.WriteStartAttribute(attrName);
            writer.WriteValue(value);
            writer.WriteEndAttribute();
        }

        /// <summary>Writes an XML element representing this matrix.</summary>
        /// <param name="writer">XML writer.</param>
        /// <param name="elementName">A name for the generated element.</param>
        public void WriteXmlAttribute(XmlWriter writer, string elementName)
        {
            writer.WriteStartElement(elementName);
            Write(writer, nameof(A11), A11);
            Write(writer, nameof(A12), A12);
            Write(writer, nameof(A13), A13);
            Write(writer, nameof(A21), A21);
            Write(writer, nameof(A22), A22);
            Write(writer, nameof(A23), A23);
            Write(writer, nameof(A31), A31);
            Write(writer, nameof(A32), A32);
            Write(writer, nameof(A33), A33);
            writer.WriteEndElement();
        }
    }
}
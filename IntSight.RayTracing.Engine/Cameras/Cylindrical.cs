using System;

namespace IntSight.RayTracing.Engine
{
    /// <summary>Common base class for cylindrical cameras.</summary>
    public abstract class BaseCylindricalCamera : BasicCamera
    {
        protected readonly double angle;
        protected readonly double aRow, bRow, aCol, bCol;
        protected readonly Matrix transform, inverse;
        protected double focusRow, focusCol;

        public BaseCylindricalCamera(
            Vector location, Vector target, Vector up,
            double angle, int width, int height)
            : base(location, target, up, width, height)
        {
            this.angle = angle;
            aRow = (Math.PI * angle / 180.0) * distance / width;
            bRow = aRow * (1 - height) / 2.0;
            double alpha = angle * Math.PI / 180.0;
            aCol = alpha / width;
            bCol = 0.5 * alpha * (1.0 - width) / width;
            // Camera rotation
            transform = new(location, target, up);
            inverse = transform.Transpose();
        }

        public void FocusRow(int row) => focusRow = aRow * row + bRow;

        public void FocusColumn(int column) => focusCol = aCol * column + bCol;

        public double[] InitJitter(double[] jitter)
        {
            for (int i = jitter.Length - 1; i >= 0;)
            {
                jitter[i--] *= aRow;
                jitter[i--] *= aCol;
            }
            return jitter;
        }

        public void GetRayCoordinates(Ray ray, out int row, out int column)
        {
            Vector v = inverse * ray.Direction;
            double factor = distance / Math.Sqrt(v.X * v.X + v.Z * v.Z);
            row = (int)((v.Y * factor - bRow) / aRow);
            column = (int)((Math.Asin(v.X * factor / distance) - bCol) / aCol);
        }

        public IShape CheckRoot(IShape root) =>
            root is IUnion union && union.IsChecking && root.Bounds.Contains(Location) ?
                UncheckUnion(union) : root;
    }

    [XSight(Alias = "Cylindrical")]
    public sealed class CylindricalCamera : BaseCylindricalCamera, ICamera
    {
        public CylindricalCamera(
            [Proposed("[0,0,-10]")] Vector location,
            [Proposed("^0")] Vector target,
            [Proposed("^Y")] Vector up,
            [Proposed("60")] double angle,
            [Proposed("320")] int width,
            [Proposed("240")] int height)
            : base(location, target, up, angle, width, height) { }

        public CylindricalCamera(
            [Proposed("[0,0,-10]")] Vector location,
            [Proposed("[0,0,0]")] Vector target,
            [Proposed("60")] double angle,
            [Proposed("200")] int width,
            [Proposed("200")] int height)
            : this(location, target, Vector.YRay, angle, width, height) { }

        public CylindricalCamera(
            [Proposed("[0,0,-10]")] Vector location,
            [Proposed("[0,0,0]")] Vector target,
            [Proposed("200")] int width,
            [Proposed("200")] int height)
            : this(location, target, Vector.YRay, 60, width, height) { }

        #region ICamera members.

        /// <summary>Creates an independent copy of the camera.</summary>
        /// <returns>The new camera.</returns>
        ICamera ICamera.Clone() =>
            new CylindricalCamera(Location, Target, Up, angle, width, height);

        /// <summary>Creates a copy of the camera with a rotated location.</summary>
        /// <param name="rotationAngle">Rotation angle, in degrees.</param>
        /// <returns>The new camera.</returns>
        ICamera ICamera.Rotate(double rotationAngle, bool keepCameraHeight) =>
            new CylindricalCamera(
                RotateLocation(rotationAngle, keepCameraHeight),
                Target, Up, angle, width, height);

        /// <summary>Initializes the camera ray for a given row and column.</summary>
        /// <param name="row">Row for the camera ray.</param>
        /// <param name="column">Column for the camera ray.</param>
        void ICamera.Focus(int row, int column)
        {
            ray.Origin = Location;
            double alpha = Math.Sin(aCol * column + bCol);
            ray.Direction = transform.RotateNorm(
                distance * alpha,
                aRow * row + bRow,
                distance * Math.Sqrt(1.0 - alpha * alpha));
        }

        void ICamera.GetRay(double dY, double dX)
        {
            ray.Origin = Location;
            double vz = Math.Sin(dX + focusCol);
            double vx = distance * vz;
            vz = distance * Math.Sqrt(1.0 - vz * vz);
            dY += focusRow;
            double x = transform.A11 * vx + transform.A12 * dY + transform.A13 * vz;
            double y = transform.A21 * vx + transform.A22 * dY + transform.A23 * vz;
            double z = transform.A31 * vx + transform.A32 * dY + transform.A33 * vz;
            double len = 1.0 / Math.Sqrt(x * x + y * y + z * z);
            ray.Direction = new(x * len, y * len, z * len);
        }

        void ICamera.GetRay(double dY, double dX, double odY, double odX)
        {
            double vz = Math.Sin(aCol * dX + focusCol);
            double vx = distance * vz;
            double vy = aRow * dY + focusRow;
            vz = distance * Math.Sqrt(1.0 - vz * vz);

            double a13 = delta.X - odX, a23 = delta.Y - odY, a33;
            double d0 = 1.0 / Math.Sqrt(a13 * a13 + a23 * a23 + dz2);
            a13 *= d0; a23 *= d0; a33 = delta.Z * d0;
            double a11 = up.Y * a33 - up.Z * a23;
            double a21 = up.Z * a13 - up.Z * a33;
            double a31 = up.Z * a23 - up.Y * a13;
            d0 = 1.0 / Math.Sqrt(a11 * a11 + a21 * a21 + a31 * a31);
            double x = d0 * (a11 * vx + (a23 * a31 - a33 * a21) * vy) + a13 * vz;
            double y = d0 * (a21 * vx + (a33 * a11 - a13 * a31) * vy) + a23 * vz;
            double z = d0 * (a31 * vx + (a13 * a21 - a23 * a11) * vy) + a33 * vz;
            double len = 1.0 / Math.Sqrt(x * x + y * y + z * z);
            ray.Direction = new(x * len, y * len, z * len);
            ray.Origin = loc.Add2D(odX, odY);
        }

        /// <summary>Selects the best camera for a given scene.</summary>
        /// <param name="sampler">Sampler to be used.</param>
        /// <returns>The own camera, or a wide cylindrical, when needed.</returns>
        ICamera ICamera.Simplify(ISampler sampler) =>
            angle >= 180
                ? new WideCylindricalCamera(Location, Target, Up, angle, width, height)
                : ((Up - Vector.YRay).Length < Tolerance.Epsilon)
                ? (ICamera)new VerticalCylindricalCamera(Location, Target, angle, width, height)
                : this;

        #endregion
    }

    /// <summary>Vertical-aligned cylindrical cameras.</summary>
    internal sealed class VerticalCylindricalCamera : BaseCylindricalCamera, ICamera
    {
        public VerticalCylindricalCamera(
            Vector location, Vector target, double angle, int width, int height)
            : base(location, target, Vector.YRay, angle, width, height) { }

        #region ICamera members.

        /// <summary>Creates an independent copy of the camera.</summary>
        /// <returns>The new camera.</returns>
        ICamera ICamera.Clone() =>
            new VerticalCylindricalCamera(Location, Target, angle, width, height);

        /// <summary>Creates a copy of the camera with a rotated location.</summary>
        /// <param name="rotationAngle">Rotation angle, in degrees.</param>
        /// <returns>The new camera.</returns>
        ICamera ICamera.Rotate(double rotationAngle, bool keepCameraHeight) =>
            new VerticalCylindricalCamera(
                RotateLocation(rotationAngle, keepCameraHeight),
                Target, angle, width, height);

        /// <summary>Initializes the camera ray for a given row and column.</summary>
        /// <param name="row">Row for the camera ray.</param>
        /// <param name="column">Column for the camera ray.</param>
        void ICamera.Focus(int row, int column)
        {
            ray.Origin = Location;
            double alpha = Math.Sin(aCol * column + bCol);
            ray.Direction = transform.RotateNorm(
                distance * alpha,
                aRow * row + bRow,
                distance * Math.Sqrt(1.0 - alpha * alpha));
        }

        void ICamera.GetRay(double dY, double dX)
        {
            ray.Origin = Location;
            double vz = Math.Sin(dX + focusCol);
            double vx = distance * vz;
            vz = distance * Math.Sqrt(1.0 - vz * vz);
            dY += focusRow;
            double x = transform.A11 * vx + transform.A12 * dY + transform.A13 * vz;
            // When the sky vector is the Y ray, transform.A21 is zero.
            double y = transform.A22 * dY + transform.A23 * vz;
            double z = transform.A31 * vx + transform.A32 * dY + transform.A33 * vz;
            double len = 1.0 / Math.Sqrt(x * x + y * y + z * z);
            ray.Direction = new(x * len, y * len, z * len);
        }

        void ICamera.GetRay(double dY, double dX, double odY, double odX)
        {
            double vz = Math.Sin(aCol * dX + focusCol);
            double vx = distance * vz;
            double vy = aRow * dY + focusRow;
            vz = distance * Math.Sqrt(1.0 - vz * vz);

            double a13 = delta.X - odX, a23 = delta.Y - odY, a33;
            double d0 = 1.0 / Math.Sqrt(a13 * a13 + a23 * a23 + dz2);
            a13 *= d0; a23 *= d0; a33 = delta.Z * d0;
            d0 = Math.Sqrt(a33 * a33 + a13 * a13);
            double x = (a33 * vx - a23 * a13 * vy) / d0 + a13 * vz;
            double y = d0 * vy + a23 * vz;
            double z = a33 * vz - (a23 * a33 * vy + a13 * vx) / d0;
            double len = 1.0 / Math.Sqrt(x * x + y * y + z * z);
            ray.Direction = new(x * len, y * len, z * len);
            ray.Origin = loc.Add2D(odX, odY);
        }

        #endregion
    }

    /// <summary>Cylindrical cameras with angle greater than 90 degrees.</summary>
    internal sealed class WideCylindricalCamera : BaseCylindricalCamera, ICamera
    {
        public WideCylindricalCamera(
            Vector location, Vector target, Vector up,
            double angle, int width, int height)
            : base(location, target, up, angle, width, height) { }

        #region ICamera members.

        /// <summary>Creates an independent copy of the camera.</summary>
        /// <returns>The new camera.</returns>
        ICamera ICamera.Clone() =>
            new WideCylindricalCamera(Location, Target, Up, angle, width, height);

        /// <summary>Creates a copy of the camera with a rotated location.</summary>
        /// <param name="rotationAngle">Rotation angle, in degrees.</param>
        /// <returns>The new camera.</returns>
        ICamera ICamera.Rotate(double rotationAngle, bool keepCameraHeight) =>
            new WideCylindricalCamera(
                RotateLocation(rotationAngle, keepCameraHeight),
                Target, Up, angle, width, height);

        /// <summary>Initializes the camera ray for a given row and column.</summary>
        /// <param name="row">Row for the camera ray.</param>
        /// <param name="column">Column for the camera ray.</param>
        void ICamera.Focus(int row, int column)
        {
            ray.Origin = Location;
            double alpha = aCol * column + bCol;
            ray.Direction = transform.RotateNorm(
                distance * Math.Sin(alpha),
                aRow * row + bRow,
                distance * Math.Cos(alpha));
        }

        void ICamera.GetRay(double dY, double dX)
        {
            ray.Origin = Location;
            double vz = distance * Math.Cos(dX + focusCol);
            double vx = distance * Math.Sin(dX + focusCol);
            dY += focusRow;
            double x = transform.A11 * vx + transform.A12 * dY + transform.A13 * vz;
            double y = transform.A21 * vx + transform.A22 * dY + transform.A23 * vz;
            double z = transform.A31 * vx + transform.A32 * dY + transform.A33 * vz;
            vz = 1.0 / Math.Sqrt(x * x + y * y + z * z);
            ray.Direction = new Vector(x * vz, y * vz, z * vz);
        }

        void ICamera.GetRay(double dY, double dX, double odY, double odX)
        {
            double vz = aCol * dX + focusCol;
            double vx = distance * Math.Sin(vz);
            double vy = aRow * dY + focusRow;
            vz = distance * Math.Cos(vz);

            double a13 = delta.X - odX, a23 = delta.Y - odY;
            double d0 = 1.0 / Math.Sqrt(a13 * a13 + a23 * a23 + dz2);
            a13 *= d0; a23 *= d0;
            double a33 = delta.Z * d0;
            double a11 = up.Y * a33 - up.Z * a23;
            double a21 = up.Z * a13 - up.X * a33;
            double a31 = up.X * a23 - up.Y * a13;
            d0 = 1.0 / Math.Sqrt(a11 * a11 + a21 * a21 + a31 * a31);
            double x = d0 * (a11 * vx + (a23 * a31 - a33 * a21) * vy) + a13 * vz;
            double y = d0 * (a21 * vx + (a33 * a11 - a13 * a31) * vy) + a23 * vz;
            double z = d0 * (a31 * vx + (a13 * a21 - a23 * a11) * vy) + a33 * vz;
            d0 = 1.0 / Math.Sqrt(x * x + y * y + z * z);
            ray.Direction = new(x * d0, y * d0, z * d0);
            ray.Origin = loc.Add2D(odX, odY);
        }

        #endregion
    }
}

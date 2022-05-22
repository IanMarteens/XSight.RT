namespace IntSight.RayTracing.Engine;

/// <summary>Implements a container for traced photons.</summary>
public sealed partial class PhotonMap
{
    /// <summary>Quantum of flux emitted by a light source.</summary>
    public sealed class Photon
    {
        private static readonly double[] cosTheta = CreateTable(Math.Cos);
        private static readonly double[] sinTheta = CreateTable(Math.Sin);
        private static readonly double[] cosPhi = CreateTable(x => Math.Cos(2 * x));
        private static readonly double[] sinPhi = CreateTable(x => Math.Sin(2 * x));

        private static double[] CreateTable(Func<double, double> f)
        {
            double[] result = new double[256];
            for (int i = 0; i < 256; i++)
            {
                double angle = i * Math.PI / 256.0;
                result[i] = f(angle);
            }
            return result;
        }

        /// <summary>Hit point where the photon has been absorbed.</summary>
        public float x, y, z;
        /// <summary>Photon power.</summary>
        public Pixel Power { get; private set; }
        /// <summary>Kd-tree plane where this photon has been stored.</summary>
        public Axis axis;
        /// <summary>Packed representation of the photon's direction.</summary>
        public byte theta, phi;

        private const double M256OverPi = 256.0 / Math.PI;
        private const double M256Over2Pi = 128.0 / Math.PI;

        /// <summary>Creates a photon.</summary>
        /// <param name="position">Hit point.</param>
        /// <param name="power">Photon's power.</param>
        /// <param name="direction">Incoming direction.</param>
        public Photon(in Vector position, in Pixel power, in Vector direction)
        {
            x = (float)position.X;
            y = (float)position.Y;
            z = (float)position.Z;
            Power = power;
            int i = (int)(Math.Acos(direction.Y) * M256OverPi);
            theta = (byte)(i >= 255 ? 255 : i);
            i = (int)(Math.Atan2(position.Z, position.X) * M256Over2Pi);
            phi = (byte)(i > 255 ? 255 : i < 0 ? i + 256 : i);
        }

        /// <summary>Unpacks the photon's direction.</summary>
        /// <returns>Photon's direction as a normalized vector.</returns>
        public Vector GetDirection() => new(
            sinTheta[theta] * cosPhi[phi], cosTheta[theta], sinTheta[theta] * sinPhi[phi]);

        /// <summary>Scales the photon's power.</summary>
        /// <param name="factor">Attenuation factor.</param>
        public void Scale(float factor) => Power *= factor;
    }

}

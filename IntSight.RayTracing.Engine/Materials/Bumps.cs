using static System.Math;

namespace IntSight.RayTracing.Engine;

/// <summary>A normal perturbator based on the Perlin noise generator.</summary>
[XSight]
public sealed class Bumps : IPerturbator
{
    private readonly double amount;
    private readonly double threshold;
    private readonly Vector scale;
    private readonly SolidNoise uNoise = new(1453);
    private readonly SolidNoise vNoise = new(3541);

    /// <summary>Creates a bumpy perturbator.</summary>
    /// <param name="amount">Amount of perturbation.</param>
    /// <param name="scale">Spacial scale.</param>
    /// <param name="threshold">Minimum amount before perturbation.</param>
    public Bumps(double amount, Vector scale, double threshold)
    {
        this.amount = amount;
        this.scale = scale;
        this.threshold = threshold;
    }

    public Bumps(double amount, double scale, double threshold)
        : this(amount, new Vector(scale), threshold) { }

    public Bumps(double amount, Vector scale) : this(amount, scale, 0.0) { }

    public Bumps(double amount, double scale) : this(amount, new Vector(scale), 0.0) { }

    public Bumps(double amount) : this(amount, new Vector(1), 0.0) { }

    #region IPerturbator members

    /// <summary>Modifies a normal vector given a location.</summary>
    /// <param name="normal">Normal to perturbate.</param>
    /// <param name="hitPoint">Intersection point.</param>
    void IPerturbator.Perturbate(ref Vector normal, in Vector hitPoint)
    {
        var (hx, hy, hz) = hitPoint.Scale(scale);
        double u = uNoise[hx, hy, hz];
        if (Abs(u) >= threshold)
        {
            u *= amount;
            double v = vNoise[hx, hy, hz] * amount;
            double vx, vy, vz;
            if (Tolerance.Near(normal.X, 1.0))
            {
                double uz = 1.0 / Sqrt(normal.Z * normal.Z + 1.0);
                double ux = -normal.Z * uz;
                vx = normal.X + u * ux + v * normal.Y * uz;
                vy = normal.Y + (normal.Z * ux - uz) * v;
                vz = normal.Z + u * uz - v * normal.Y * ux;
            }
            else
            {
                vx = Sqrt(1.0 - normal.X * normal.X);
                double uy = normal.Z / vx;
                double uz = -normal.Y / vx;
                vx = normal.X - vx * v;
                vy = normal.Y + u * uy - v * normal.X * uz;
                vz = normal.Z + u * uz + v * normal.X * uy;
            }
            u = 1.0 / Sqrt(vx * vx + vy * vy + vz * vz);
            normal = new(u * vx, u * vy, u * vz);
        }
    }

    /// <summary>Creates an independent copy of this object.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new perturbator.</returns>
    IPerturbator IPerturbator.Clone(bool force) =>
        force ? new Bumps(amount, scale, threshold) : this;

    #endregion
}

/// <summary>A normal perturbator based on the Crackle noise generator.</summary>
[XSight]
public sealed class Wrinkles : IPerturbator
{
    private readonly double amount;
    private readonly Vector scale;
    private readonly short turbulence;
    private readonly short metric;
    private readonly CrackleNoise noise = new(145377);

    /// <summary>Creates a wrinkled perturbator.</summary>
    /// <param name="amount">Amount of perturbation.</param>
    /// <param name="scale">Spacial scale.</param>
    /// <param name="turbulence">Turbulence.</param>
    /// <param name="metric">Metric used by the noise generator.</param>
    public Wrinkles(double amount, Vector scale, int turbulence, int metric)
    {
        this.amount = amount;
        this.scale = scale;
        this.turbulence = (short)turbulence;
        this.metric = (short)metric;
    }

    public Wrinkles(double amount, Vector scale, int turbulence)
        : this(amount, scale, turbulence, 1) { }

    public Wrinkles(double amount, double scale, int turbulence)
        : this(amount, new Vector(scale), turbulence, 1) { }

    public Wrinkles(double amount, double scale)
        : this(amount, new Vector(scale), 0, 1) { }

    public Wrinkles(double amount, Vector scale) : this(amount, scale, 0, 1) { }

    public Wrinkles(double amount) : this(amount, new Vector(1), 0, 1) { }

    #region IPerturbator members

    /// <summary>Modifies a normal vector given a location.</summary>
    /// <param name="normal">Normal to perturbate.</param>
    /// <param name="hitPoint">Intersection point.</param>
    void IPerturbator.Perturbate(ref Vector normal, in Vector hitPoint)
    {
        var (hx, hy, hz) = hitPoint.Scale(scale);
        double z;
        if (metric == 0)
            z = noise.Facets(hx, hy, hz, turbulence);
        else if (metric == 1)
            z = noise.Bubbles(hx, hy, hz, turbulence);
        else
            z = noise[hx, hy, hz, turbulence];
        double x = (z = (z - 0.5) * amount) + normal.X;
        double y = normal.Y + z;
        z += normal.Z;
        double len = x * x + y * y + z * z;
        if (len > Tolerance.Epsilon)
        {
            len = 1 / Sqrt(len);
            normal = new(x * len, y * len, z * len);
        }
    }

    /// <summary>Creates an independent copy of this object.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new perturbator.</returns>
    IPerturbator IPerturbator.Clone(bool force) =>
        new Wrinkles(amount, scale, turbulence, metric);

    #endregion
}

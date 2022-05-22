namespace IntSight.RayTracing.Engine;

[XSight]
[Properties(nameof(minReflection), nameof(maxReflection),
    nameof(ior), nameof(phongAmount), nameof(phongSize))]
public sealed class Glass : BaseMaterial, IMaterial
{
    private const float MAX = 0.98F;
    private readonly double phongAmount;
    private readonly double phongSize;
    private readonly double ior;
    private readonly float minReflection;
    private readonly float maxReflection;
    private readonly float delta;

    public Glass(double ior, Pixel filter, double minReflection, double maxReflection,
        double phongAmount, double phongSize, IPerturbator perturbator)
        : base(0.0, perturbator)
    {
        this.ior = ior;
        AttenuationFactor = filter;
        this.minReflection = (float)minReflection;
        this.maxReflection = Math.Min(MAX, (float)maxReflection);
        delta = this.maxReflection - this.minReflection;
        this.phongAmount = phongAmount;
        this.phongSize = phongSize + 1.0;
        // Transform the attenuation filter.
        if (filter != Pixel.White)
        {
            HasAttenuation = true;
            AttenuationFactor = new(
                Math.Log(filter.Red), Math.Log(filter.Green), Math.Log(filter.Blue));
        }
    }

    public Glass(double ior, double minReflection, double maxReflection,
        double phongAmount, double phongSize, IPerturbator perturbator)
        : this(ior, Pixel.White, minReflection, maxReflection,
        phongAmount, phongSize, perturbator)
    { }

    public Glass(double ior, Pixel filter, double minReflection, double maxReflection,
        double phongAmount, double phongSize)
        : this(ior, filter, minReflection, maxReflection, phongAmount, phongSize, null) { }

    public Glass(double ior, double minReflection, double maxReflection,
        double phongAmount, double phongSize)
        : this(ior, Pixel.White, minReflection, maxReflection, phongAmount, phongSize) { }

    public Glass(double ior, Pixel filter, double phongAmount, double phongSize)
        : this(ior, filter, GetMinReflection(ior), MAX, phongAmount, phongSize, null) { }

    public Glass(double ior, double phongAmount, double phongSize)
        : this(ior, Pixel.White, phongAmount, phongSize) { }

    public Glass(double ior, Pixel filter, double minReflection)
        : this(ior, filter, minReflection, MAX, 0.0, 0.0, null) { }

    public Glass(double ior, double minReflection)
        : this(ior, Pixel.White, minReflection, MAX, 0.0, 0.0, null) { }

    public Glass(double ior, Pixel filter, IPerturbator perturbator)
        : this(ior, filter, GetMinReflection(ior), MAX, 0.0, 0.0, perturbator) { }

    public Glass(double ior, IPerturbator perturbator)
        : this(ior, Pixel.White, perturbator) { }

    public Glass(double ior, Pixel filter) : this(ior, filter, null) { }

    public Glass(double ior) : this(ior, Pixel.White, null) { }

    public Glass(Pixel filter) : this(1.5, filter, null) { }

    public Glass() : this(1.5, Pixel.White, null) { }

    /// <summary>Estimates the minimum reflection factor from the IOR.</summary>
    /// <param name="ior">The index of refraction.</param>
    /// <returns>Reflection at normal light incidence.</returns>
    private static double GetMinReflection(double ior) =>
        Math.Pow((ior - 1.0) / (ior + 1.0), 2);

    #region IMaterial members.

    /// <summary>Does this material feature exponential light attenuation?</summary>
    public bool HasAttenuation { get; }

    /// <summary>The exponential attenuation filter.</summary>
    public Pixel AttenuationFactor { get; }

    /// <summary>Gets the index of refraction.</summary>
    double IMaterial.IndexOfRefraction => ior;

    /// <summary>Gets the reflection and refraction coefficients at a given angle.</summary>
    /// <param name="cosine">The cosine of the incidence angle.</param>
    /// <param name="filter">Transmition coefficient.</param>
    /// <param name="refraction">Refraction coefficient.</param>
    /// <returns>The reflection coefficient.</returns>
    float IMaterial.Reflection(double cosine, out Pixel filter, out float refraction)
    {
        filter = new(1F);
        // Schlick's aproximation for the Fresnel coefficient.
        cosine = 1.0 - Math.Abs(cosine);
        double c2 = cosine * cosine;
        float result = (float)(c2 * c2 * cosine) * delta + minReflection;
        refraction = MAX - result;
        return result;
    }

    /// <summary>Computes the amount of diffused light from a source at a point.</summary>
    /// <param name="location">Incidence point.</param>
    /// <param name="normal">Normal at the hit point.</param>
    /// <param name="reflection">The direction of the reflected ray.</param>
    /// <param name="light">The light source.</param>
    /// <param name="color">The color of the hit point: always black for glass.</param>
    /// <returns>The contributed radiance.</returns>
    Pixel IMaterial.Shade(
        in Vector location, in Vector normal, in Vector reflection,
        ILight light, in Pixel color)
    {
        // Shade returns the amount of diffussed light.
        // A perfect reflector/refractor doesn't diffuse any light.
        if (phongAmount > 0.0)
        {
            Vector lightRay = light.GetDirection(location);
            double f = lightRay * reflection;
            if (f > 0.0)
            {
                double cosine = lightRay * normal;
                double c2 = cosine * cosine;
                // Interpolation between black and the light's color.
                return light.Color
                    * (float)(Math.Pow(f, phongSize) * c2 * c2 * cosine * phongAmount);
            }
        }
        return new Pixel();
    }

    /// <summary>Creates an independent copy of the material.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The original instance.</returns>
    IMaterial IMaterial.Clone(bool force)
    {
        IPerturbator newPerturbator = ClonePerturbator(force);
        if (force || newPerturbator != perturbator)
        {
            Pixel f = HasAttenuation ? new(
                Math.Exp(AttenuationFactor.Red),
                Math.Exp(AttenuationFactor.Green),
                Math.Exp(AttenuationFactor.Blue)) :
                AttenuationFactor;
            return new Glass(ior, f, minReflection, minReflection,
                phongAmount, phongSize - 1.0, newPerturbator);
        }
        else
            return this;
    }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color: always black for glass.</param>
    /// <param name="location">Sampling location.</param>
    /// <returns>True, if the material has a normal perturbated surface.</returns>
    bool IMaterial.GetColor(out Pixel color, in Vector location)
    {
        color = new();
        return hasBumps;
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel IMaterial.DraftColor => Pixel.Black;

    #endregion
}

namespace IntSight.RayTracing.Engine;

/// <summary>Common ancestor for metallic materials.</summary>
public abstract class MetalPhongMaterial : BaseMaterial, IMaterial
{
    private readonly Pixel lightFilter;
    protected double diffuse, phongAmount, phongSize, lnAmt;
    protected float minReflection, maxReflection, delta;

    /// <summary>Initializes a metallic material.</summary>
    /// <param name="mainColor">Metal's color.</param>
    /// <param name="minReflection">Minimal reflection.</param>
    /// <param name="maxReflection">Maximal reflection.</param>
    /// <param name="diffuse">Diffusion amount.</param>
    /// <param name="phongAmount">Phong intensity.</param>
    /// <param name="phongSize">Phong area factor.</param>
    /// <param name="roughness">Roughness of surface.</param>
    /// <param name="perturbator">Normal vector modifier.</param>
    public MetalPhongMaterial(Pixel mainColor,
        double minReflection, double maxReflection, double diffuse,
        double phongAmount, double phongSize, double roughness, IPerturbator perturbator)
        : base(roughness, perturbator)
    {
        this.diffuse = diffuse;
        this.phongAmount = Math.Max(0.0, phongAmount);
        this.phongSize = phongSize + 1.0;
        if (this.phongAmount > 0.0)
            lnAmt = Math.Log(this.phongAmount);
        this.minReflection = Math.Max(Math.Min(1.0F, (float)minReflection), 0.0F);
        this.maxReflection = Math.Max(Math.Min(1.0F, (float)maxReflection), 0.0F);
        delta = this.maxReflection - this.minReflection;
        float maxClr = mainColor.Dominant;
        lightFilter = maxClr < 1F / 255F ? Pixel.White : 1F - mainColor * (1 / maxClr);
    }

    #region IMaterial members.

    /// <summary>Gets the index of refraction of the material.</summary>
    double IMaterial.IndexOfRefraction => 0.0;

    /// <summary>Does this material feature exponential light attenuation?</summary>
    bool IMaterial.HasAttenuation => false;

    /// <summary>The exponential attenuation filter.</summary>
    Pixel IMaterial.AttenuationFactor => Pixel.White;

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    /// <returns>True, if the material has a normal perturbated surface.</returns>
    public abstract bool GetColor(out Pixel color, in Vector location);

    /// <summary>Gets a quick estimation of the color.</summary>
    public abstract Pixel DraftColor { get; }

    /// <summary>Gets the reflection and refraction coefficients at a given angle.</summary>
    /// <param name="cosine">The cosine of the incidence angle.</param>
    /// <param name="filter">Transmition coefficient.</param>
    /// <param name="refraction">Refraction coefficient.</param>
    /// <returns>The reflection coefficient.</returns>
    float IMaterial.Reflection(double cosine, out Pixel filter, out float refraction)
    {
        refraction = 0.0F;
        if (cosine < 0.0)
        {
            float c5 = 1.0F + (float)cosine, c52 = c5 * c5;
            c5 *= c52 * c52;
            // Interpolate between lightFilter and white.
            filter = 1F - lightFilter * c5;
            // Schlick's aproximation for Fresnel coefficient.
            return c5 * delta + minReflection;
        }
        else
        {
            filter = 1F - lightFilter;
            return maxReflection;
        }
    }

    /// <summary>Computes the amount of diffused light from a source at a point.</summary>
    /// <param name="location">Incidence point.</param>
    /// <param name="normal">Normal at the hit point.</param>
    /// <param name="reflection">The direction of the reflected ray.</param>
    /// <param name="light">The light source.</param>
    /// <param name="color">The color of the hit point.</param>
    /// <returns>The contributed radiance.</returns>
    Pixel IMaterial.Shade(
        in Vector location, in Vector normal, in Vector reflection,
        ILight light, in Pixel color)
    {
        Vector lightRay = light.GetDirection(location);
        float cosine = (float)(lightRay * normal * diffuse);
        if (cosine > 0.0F)
        {
            Pixel result = color * cosine;
            if (phongAmount > 0.0)
            {
                double f = lightRay * reflection;
                if (f > 0.0)
                {
                    float c = cosine * cosine;
                    result = result.Add(color, c * c * cosine,
                        light.Color, (float)(Math.Pow(f, phongSize) * phongAmount));
                }
            }
            return result;
        }
        else
            return new();
    }

    /// <summary>Creates an independent copy of the material.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The original instance or an equivalent one.</returns>
    public abstract IMaterial Clone(bool force);

    #endregion
}

/// <summary>Represents a solid color metal surface.</summary>
[XSight, Properties(
    nameof(color), nameof(minReflection), nameof(maxReflection), nameof(diffuse),
    nameof(phongAmount), nameof(phongSize), nameof(Roughness))]
public sealed class Metal : MetalPhongMaterial
{
    private readonly Pixel color;

    public Metal(Pixel color,
        double minReflection, double maxReflection, double diffuse,
        double phongAmount, double phongSize, double roughness, IPerturbator perturbator)
        : base(color, minReflection, maxReflection,
            diffuse, phongAmount, phongSize, roughness, perturbator) => this.color = color;

    public Metal(Pixel color,
        double minReflection, double maxReflection, double diffuse,
        double phongAmount, double phongSize, double roughness)
        : this(color, minReflection, maxReflection,
            diffuse, phongAmount, phongSize, roughness, null) { }

    public Metal(Pixel color, double minReflection, double maxReflection,
        double diffuse, double phongAmount, double phongSize, IPerturbator perturbator)
        : this(color, minReflection, maxReflection,
            diffuse, phongAmount, phongSize, 0.0, perturbator) { }

    public Metal(Pixel color, double minReflection, double maxReflection,
        double diffuse, double phongAmount, double phongSize)
        : this(color, minReflection, maxReflection,
            diffuse, phongAmount, phongSize, 0.0, null) { }

    public Metal(Pixel color, double minReflection, double maxReflection, double diffuse)
        : this(color, minReflection, maxReflection, diffuse, 0.0, 120.0, 0.0) { }

    public Metal(Pixel color, double minReflection, double maxReflection)
        : this(color, minReflection, maxReflection, 1.0, 0.0, 120.0, 0.0) { }

    public Metal(Pixel color, double minReflection, IPerturbator perturbator)
        : this(color, minReflection, 1.0, 1.0, 0.0, 120.0, 0.0, perturbator) { }

    public Metal(Pixel color, double minReflection)
        : this(color, minReflection, 1.0, 1.0, 0.0, 120.0, 0.0, null) { }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    /// <returns>True, if the material has a normal perturbated surface.</returns>
    public override bool GetColor(out Pixel color, in Vector location)
    {
        color = this.color;
        return hasBumps;
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    public override Pixel DraftColor => color;

    /// <summary>Creates an independent copy of the material.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The original instance or an equivalent one.</returns>
    public override IMaterial Clone(bool force)
    {
        IPerturbator newPert = ClonePerturbator(force);
        return force || hasRoughness || newPert != perturbator
            ? new Metal(color, minReflection, maxReflection, diffuse,
                phongAmount, phongSize, Roughness, newPert)
            : this;
    }
}

/// <summary>Represents a pigmented surface with metallic reflections.</summary>
[XSight, Children(nameof(pigment)), Properties(
    nameof(minReflection), nameof(maxReflection), nameof(diffuse),
    nameof(phongAmount), nameof(phongSize), nameof(Roughness))]
public sealed class MetalPigment : MetalPhongMaterial
{
    private IPigment pigment;

    public MetalPigment(IPigment pigment,
        double minReflection, double maxReflection, double diffuse,
        double phongAmount, double phongSize, double roughness, IPerturbator perturbator)
        : base(pigment.DraftColor, minReflection, maxReflection, diffuse,
            phongAmount, phongSize, roughness, perturbator) => this.pigment = pigment;

    public MetalPigment(IPigment pigment,
        double minReflection, double maxReflection, double diffuse,
        double phongAmount, double phongSize, double roughness)
        : this(pigment, minReflection, maxReflection, diffuse,
            phongAmount, phongSize, roughness, null) { }

    public MetalPigment(IPigment pigment,
        double minReflection, double maxReflection, double diffuse,
        double phongAmount, double phongSize)
        : this(pigment, minReflection, maxReflection, diffuse,
            phongAmount, phongSize, 0.0, null) { }

    public MetalPigment(IPigment pigment,
        double minReflection, double maxReflection, double diffuse)
        : this(pigment, minReflection, maxReflection, diffuse, 0.0, 120.0, 0.0, null) { }

    public MetalPigment(IPigment pigment,
        double minReflection, double maxReflection)
        : this(pigment, minReflection, maxReflection, 1.0, 0.0, 120.0, 0.0, null) { }

    public MetalPigment(IPigment pigment, double minReflection, IPerturbator perturbator)
        : this(pigment, minReflection, 1.0, 1.0, 0.0, 120.0, 0.0, perturbator) { }

    public MetalPigment(IPigment pigment, double minReflection)
        : this(pigment, minReflection, 1.0, 1.0, 0.0, 120.0, 0.0, null) { }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color, according to the pigment.</param>
    /// <param name="location">Sampling location.</param>
    /// <returns>True, if the material has a normal perturbated surface.</returns>
    public override bool GetColor(out Pixel color, in Vector location)
    {
        pigment.GetColor(out color, location);
        return hasBumps;
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    public override Pixel DraftColor => pigment.DraftColor;

    /// <summary>Translates the pigment of the material.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated material.</returns>
    public override IMaterial Translate(in Vector translation)
    {
        pigment = pigment.Translate(translation);
        return this;
    }

    /// <summary>Rotates the pigment of the material.</summary>
    /// <param name="rotation">Rotation matrix.</param>
    /// <returns>The rotated material.</returns>
    public override IMaterial Rotate(in Matrix rotation)
    {
        pigment = pigment.Rotate(rotation);
        return this;
    }

    /// <summary>Creates an independent copy of the material.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The original instance or an equivalent one.</returns>
    public override IMaterial Clone(bool force)
    {
        IPerturbator newPert = ClonePerturbator(force);
        IPigment newPigm = pigment.Clone(force);
        return force || hasRoughness || newPigm != pigment || newPert != perturbator
            ? new MetalPigment(newPigm, minReflection, maxReflection, diffuse,
                phongAmount, phongSize, Roughness, newPert)
            : this;
    }
}

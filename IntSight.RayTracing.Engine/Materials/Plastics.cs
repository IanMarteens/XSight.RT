namespace IntSight.RayTracing.Engine;

/// <summary>Common class for non-metallic materials with Phong diffuse reflection.</summary>
public abstract class PhongMaterial : BaseMaterial, IMaterial
{
    /// <summary>Intensity of specular diffuse highlights.</summary>
    protected readonly float phongAmount;
    /// <summary>The inverse area of specular diffuse highlights.</summary>
    protected readonly float phongSize;
    /// <summary>The constant reflection coefficient.</summary>
    protected readonly float reflection;

    protected PhongMaterial(double reflection,
        double phongAmount, double phongSize, double roughness, IPerturbator perturbator)
        : base(roughness, perturbator)
    {
        this.reflection = (float)reflection;
        this.phongAmount = (float)phongAmount;
        this.phongSize = (float)phongSize + 1F;
    }

    protected PhongMaterial(double reflection,
        double phongAmount, double phongSize, double roughness)
        : base(roughness)
    {
        this.reflection = (float)reflection;
        this.phongAmount = (float)phongAmount;
        this.phongSize = (float)phongSize + 1F;
    }

    protected PhongMaterial(double reflection, double phongAmount, double phongSize)
        : this(reflection, phongAmount, phongSize, 0.0) { }

    protected PhongMaterial(double reflection)
        : this(reflection, 0.0, 0.0, 0.0) { }

    protected PhongMaterial()
        : this(0.0, 0.0, 0.0, 0.0) { }

    #region IMaterial members

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
        filter = new Pixel(1F);
        refraction = 0.0F;
        return reflection;
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
        float factor = (float)(lightRay * normal);
        if (factor <= 0.0F)
            return new();
        Pixel c = light.Color, result = color * factor * c;
        if (phongAmount > 0.0)
            if ((factor = (float)(lightRay * reflection)) > 0.0F)
                result = result.Add(c, MathF.Pow(factor, phongSize) * phongAmount);
        return result;
    }

    /// <summary>Creates an independent copy of the material.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The original instance or an equivalent one.</returns>
    public abstract IMaterial Clone(bool force);

    #endregion
}

/// <summary>A solid color surface with non-metallic Phong reflections.</summary>
[XSight, Properties("color", "reflection", "phongAmount", "phongSize", "roughness")]
public sealed class Plastic : PhongMaterial
{
    private readonly Pixel color;

    public Plastic(Pixel color,
        double reflection, double phongAmount, double phongSize, double roughness,
        IPerturbator perturbator)
        : base(reflection, phongAmount, phongSize, roughness, perturbator) => this.color = color;

    public Plastic(Pixel color,
        double reflection, double phongAmount, double phongSize, double roughness)
        : this(color, reflection, phongAmount, phongSize, roughness, null) { }

    public Plastic(Pixel color,
        double reflection, double phongAmount, double phongSize, IPerturbator perturbator)
        : this(color, reflection, phongAmount, phongSize, 0.0, perturbator) { }

    public Plastic(Pixel color,
        double reflection, double phongAmount, double phongSize)
        : this(color, reflection, phongAmount, phongSize, 0.0, null) { }

    public Plastic(Pixel color, double reflection)
        : this(color, reflection, 0.0, 0.0, 0.0, null) { }

    public Plastic(Pixel color, double reflection, IPerturbator perturbator)
        : this(color, reflection, 0.0, 0.0, 0.0, perturbator) { }

    public Plastic(Pixel color)
        : this(color, 0.0, 0.0, 0.0, 0.0, null) { }

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
        IPerturbator newPerturbator = ClonePerturbator(force);
        return force || hasRoughness || newPerturbator != perturbator
            ? new Plastic(color, reflection, phongAmount, phongSize,
                Roughness, newPerturbator)
            : this;
    }
}

/// <summary>A default container for pigments, without reflections.</summary>
/// <remarks>
/// This material is used when a pigment is used where a material is expected.
/// </remarks>
[Children(nameof(pigment))]
public sealed class DefaultPigment : BaseMaterial, IMaterial
{
    private IPigment pigment;

    /// <summary>Creates an instance of the default pigment material.</summary>
    /// <param name="pigment">Pigment determining the surface color.</param>
    public DefaultPigment(IPigment pigment)
        : base(0.0) => this.pigment = pigment;

    /// <summary>Gets the index of refraction (IOR).</summary>
    double IMaterial.IndexOfRefraction => 0.0;

    /// <summary>Does this material feature exponential light attenuation?</summary>
    bool IMaterial.HasAttenuation => false;

    /// <summary>The exponential attenuation filter.</summary>
    Pixel IMaterial.AttenuationFactor => Pixel.White;

    /// <summary>Gets the reflection and refraction coefficients at a given angle.</summary>
    /// <param name="cosine">The cosine of the incidence angle.</param>
    /// <param name="filter">Transmition coefficient.</param>
    /// <param name="refraction">Refraction coefficient.</param>
    /// <returns>The reflection coefficient.</returns>
    float IMaterial.Reflection(double cosine, out Pixel filter, out float refraction)
    {
        filter= new Pixel(1F);
        refraction = 0.0F;
        return 0.0F;
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
        float factor = (float)(light.GetDirection(location) * normal);
        return factor > 0.0F ? light.Color * factor * color : new();
    }

    /// <summary>Does this material have a rough surface?</summary>
    bool IMaterial.HasRoughness => false;

    Vector IMaterial.Perturbate(in Vector reflection, in Vector normal, bool sign) => reflection;

    /// <summary>Creates an independent copy of the material.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The original instance or an equivalent one.</returns>
    IMaterial IMaterial.Clone(bool force)
    {
        // This material has neither roughness nor perturbators.
        IPigment newPigment = pigment.Clone(force);
        return force || newPigment != pigment ? new DefaultPigment(newPigment) : this;
    }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color, according to the pigment.</param>
    /// <param name="location">Sampling location.</param>
    bool IMaterial.GetColor(out Pixel color, in Vector location)
    {
        pigment.GetColor(out color, location);
        return hasBumps;
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel IMaterial.DraftColor => pigment.DraftColor;

    /// <summary>Translates the pigment of the material.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated material.</returns>
    public override IMaterial Translate(in Vector translation)
    {
        pigment = pigment.Translate(translation);
        return this;
    }
}

[XSight]
[Properties("reflection", "phongAmount", "phongSize", "roughness")]
[Children(nameof(pigment))]
public sealed class Pigment : PhongMaterial
{
    private IPigment pigment;

    public Pigment(IPigment pigment,
        double reflection, double phongAmount, double phongSize, double roughness,
        IPerturbator perturbator)
        : base(reflection, phongAmount, phongSize, roughness, perturbator) => this.pigment = pigment;

    public Pigment(IPigment pigment,
        double reflection, double phongAmount, double phongSize, double roughness)
        : base(reflection, phongAmount, phongSize, roughness) => this.pigment = pigment;

    public Pigment(IPigment pigment,
        double reflection, double phongAmount, double phongSize, IPerturbator perturbator)
        : this(pigment, reflection, phongAmount, phongSize, 0.0, perturbator) { }

    public Pigment(IPigment pigment,
        double reflection, double phongAmount, double phongSize)
        : this(pigment, reflection, phongAmount, phongSize, 0.0) { }

    public Pigment(IPigment pigment, double reflection, IPerturbator perturbator)
        : this(pigment, reflection, 0.0, 0.0, 0.0, perturbator) { }

    public Pigment(IPigment pigment, double reflection)
        : this(pigment, reflection, 0.0, 0.0, 0.0) { }

    public Pigment(IPigment pigment, IPerturbator perturbator)
        : this(pigment, 0.0, 0.0, 0.0, 0.0, perturbator) { }

    public Pigment(IPigment pigment)
        : this(pigment, 0.0, 0.0, 0.0, 0.0) { }

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
        IPerturbator newPerturbator = ClonePerturbator(force);
        IPigment newPigment = pigment.Clone(force);
        return force || hasRoughness || newPigment != pigment || newPerturbator != perturbator
            ? new Pigment(newPigment, reflection, phongAmount, phongSize,
                Roughness, newPerturbator)
            : this;
    }
}

[XSight, Properties(
    "color1", "color2", "scale", "stripes", "octaves",
    "reflection", "phongAmount", "phongSize", "roughness")]
public sealed class Marble : PhongMaterial
{
    private readonly SolidNoise noise = new(1984);
    private readonly Pixel color1, color2, delta;
    private readonly double scale;
    private readonly double freq;
    private readonly double stripes;
    private readonly short octaves;
    private Vector translation;

    public Marble(Pixel color1, Pixel color2,
        double scale, double stripes, int octaves,
        double reflection, double phongAmount, double phongSize,
        double roughness, IPerturbator perturbator)
        : base(reflection, phongAmount, phongSize, roughness, perturbator)
    {
        this.color1 = color1;
        this.color2 = color2;
        delta = color1 - color2;
        this.scale = scale;
        this.stripes = stripes;
        freq = Math.PI * stripes;
        this.octaves = (short)octaves;
    }

    public Marble(Pixel color1, Pixel color2,
        double scale, double stripes, int octaves,
        double reflection, double phongAmount, double phongSize, double roughness)
        : this(color1, color2, scale, stripes, octaves, reflection, phongAmount, phongSize,
        roughness, null)
    { }

    public Marble(Pixel color1, Pixel color2,
        double scale, double stripes, int octaves,
        double reflection, double phongAmount, double phongSize)
        : this(color1, color2, scale, stripes, octaves,
            reflection, phongAmount, phongSize, 0.0)
    { }

    public Marble(Pixel color1, Pixel color2,
        double scale, double stripes, int octaves, double reflection)
        : this(color1, color2, scale, stripes, octaves, reflection, 0.5, 80.0) { }

    public Marble(Pixel color1, Pixel color2,
        double scale, double stripes, int octaves)
        : this(color1, color2, scale, stripes, octaves, 0.1, 0.5, 80.0) { }

    public Marble(Pixel color1, Pixel color2, double scale)
        : this(color1, color2, scale, 0.15, 8, 0.1, 0.5, 80.0) { }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    /// <returns>True, if the material has a normal perturbated surface.</returns>
    public override bool GetColor(out Pixel color, in Vector location)
    {
        const double inv_pi = 1.0 / Math.PI;
        double t = (location.X - translation.X) * freq;
        float time = (float)CheapSin(
            (t + scale * noise[t,
                (location.Y - translation.Y) * freq,
                (location.Z - translation.Z) * freq, octaves]) * inv_pi);
        if (time > 1.0F)
            time -= 1.0F;
        color = color2.Lerp(delta, time);
        return hasBumps;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double CheapSin(double x)
    {
        x -= Math.Floor(x);
        return 8.0 * x * (1.0 - x);
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    public override Pixel DraftColor => color1;

    /// <summary>Translates the pigment of the material.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated material.</returns>
    public override IMaterial Translate(in Vector translation)
    {
        this.translation += translation;
        return this;
    }

    /// <summary>Creates an independent copy of the material.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The original instance or an equivalent one.</returns>
    public override IMaterial Clone(bool force)
    {
        IPerturbator newPerturbator = ClonePerturbator(force);
        return force || hasRoughness || newPerturbator != perturbator
            ? new Marble(color1, color2, scale, stripes, octaves,
                reflection, phongAmount, phongSize, Roughness, newPerturbator)
            : this;
    }
}

[XSight, Properties(
    "color1", "color2", "scale", "turbulence",
    "reflection", "phongAmount", "phongSize", "roughness")]
public sealed class Spotted : PhongMaterial
{
    private readonly SolidNoise noise = new(1984);
    private readonly Pixel color1, color2, c1, c2;
    private readonly double scale;
    private readonly short turbulence;

    public Spotted(Pixel color1, Pixel color2, double scale, int turbulence,
        double reflection, double phongAmount, double phongSize,
        double roughness, IPerturbator perturbator)
        : base(reflection, phongAmount, phongSize, roughness, perturbator)
    {
        this.color1 = color1;
        this.color2 = color2;
        c1 = (color1 + color2) * 0.5f;
        c2 = (color2 - color1) * 0.5f;
        this.scale = scale;
        this.turbulence = (short)turbulence;
    }

    public Spotted(Pixel color1, Pixel color2, double scale, int turbulence,
        double reflection, double phongAmount, double phongSize, double roughness)
        : this(color1, color2, scale, turbulence,
            reflection, phongAmount, phongSize, roughness, null)
    { }

    public Spotted(Pixel color1, Pixel color2, double scale, int turbulence,
        double reflection, double phongAmount, double phongSize)
        : this(color1, color2, scale, turbulence, reflection, phongAmount, phongSize, 0.0) { }

    public Spotted(Pixel color1, Pixel color2, double scale, int turbulence)
        : this(color1, color2, scale, turbulence, 0.0, 0.0, 0.0) { }

    public Spotted(Pixel color1, Pixel color2)
        : this(color1, color2, 5.0, 2, 0.0, 0.0, 0.0) { }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    /// <returns>True, if the material has a normal perturbated surface.</returns>
    public override bool GetColor(out Pixel color, in Vector location)
    {
        float t = (float)noise[scale * location.X,
            scale * location.Y, scale * location.Z, turbulence];
        color = c1.Lerp(c2, t * t * t);
        return hasBumps;
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    public override Pixel DraftColor => color1;

    /// <summary>Creates an independent copy of the material.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The original instance or an equivalent one.</returns>
    public override IMaterial Clone(bool force)
    {
        IPerturbator newPerturbator = ClonePerturbator(force);
        return force || hasRoughness || newPerturbator != perturbator
            ? new Spotted(color1, color2, scale, turbulence,
                reflection, phongAmount, phongSize, Roughness, newPerturbator)
            : this;
    }
}
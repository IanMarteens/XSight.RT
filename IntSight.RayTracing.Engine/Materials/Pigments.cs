using System.Drawing;

namespace IntSight.RayTracing.Engine;

/// <summary>Common base for <see cref="IPigment"/> implementors.</summary>
public abstract class PigmentBase
{
    /// <summary>A classification of special power profiles.</summary>
    protected enum PowerCase { Lineal, SquareRoot, Squared, Cubic, S, Other }

    /// <summary>Quickly detection of special power profiles.</summary>
    /// <param name="power">Numeric power profile.</param>
    /// <returns>One of the special power profiles.</returns>
    protected static PowerCase GetPowerCase(double power)
    {
        if (Tolerance.Near(power, 1.0))
            return PowerCase.Lineal;
        else if (Tolerance.Near(power, 0.5))
            return PowerCase.SquareRoot;
        else if (Tolerance.Near(power, 2.0))
            return PowerCase.Squared;
        else if (Tolerance.Near(power, 3.0))
            return PowerCase.Cubic;
        else if (Tolerance.Near(power, -3.0))
            return PowerCase.S;
        else
            return PowerCase.Other;
    }

    /// <summary>Translates the pigment.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated pigment.</returns>
    public virtual IPigment Translate(in Vector translation) => (IPigment)this;

    /// <summary>Rotates the pigment.</summary>
    /// <param name="rotation">The rotation matrix.</param>
    /// <returns>The rotated pigment.</returns>
    public virtual IPigment Rotate(in Matrix rotation) => (IPigment)this;

    /// <summary>Scales the pigment.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The scaled pigment.</returns>
    public virtual IPigment Scale(in Vector factor) => (IPigment)this;
}

[XSight, Properties("color1", "color2", "point1", "point2")]
public sealed class Gradient(
    [Proposed("[0,0,0]")] Vector p1,
    [Proposed("Black")] Pixel c1,
    [Proposed("[0,0,0]")] Vector p2,
    [Proposed("White")] Pixel c2) : PigmentBase, IPigment
{
    private Vector unit = p2.Difference(p1);
    private readonly Pixel delta = c2 - c1;
    private readonly double max = p2.Distance(p1);

    public Gradient(
        [Proposed("Black")]Pixel c1,
        [Proposed("[0,0,0]")]Vector p1,
        [Proposed("White")]Pixel c2,
        [Proposed("[0,0,0]")]Vector p2)
        : this(p1, c1, p2, c2) { }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    void IPigment.GetColor(out Pixel color, in Vector location)
    {
        double d = (location - p1) * unit;
        color = d <= 0.0 ? c1 : d >= max ? c2 : c1.Lerp(delta, (float)(d / max));
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel IPigment.DraftColor => c1;

    /// <summary>Creates an independent copy of this pigment.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new pigment.</returns>
    IPigment IPigment.Clone(bool force) => force ? new Gradient(p1, c1, p2, c2) : this;

    /// <summary>Translates the pigment.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated pigment.</returns>
    public override IPigment Translate(in Vector translation)
    {
        p1 += translation;
        p2 += translation;
        return this;
    }

    /// <summary>Rotates the pigment.</summary>
    /// <param name="rotation">The rotation matrix.</param>
    /// <returns>The rotated pigment.</returns>
    public override IPigment Rotate(in Matrix rotation)
    {
        p1 = rotation * p1;
        p2 = rotation * p2;
        unit = p2.Difference(p1);
        return this;
    }

    /// <summary>Scales the pigment.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The scaled pigment.</returns>
    public override IPigment Scale(in Vector factor)
    {
        p1 = p1.Scale(factor);
        p2 = p2.Scale(factor);
        unit = p2.Difference(p1);
        return this;
    }
}

[XSight, Properties(nameof(color1), nameof(color2), nameof(power), nameof(direction))]
public sealed class Stripes : PigmentBase, IPigment
{
    private readonly Pixel color1, color2, delta;
    private readonly Vector direction;
    private readonly Vector unit;
    private readonly double length;
    private readonly double power;
    private readonly PowerCase powerCase;

    public Stripes(Pixel color1, Pixel color2, Vector direction, double power)
    {
        this.color1 = color1;
        this.color2 = color2;
        delta = color2 - color1;
        this.direction = direction;
        length = direction.Length;
        unit = direction / length;
        length /= Math.PI;
        this.power = power < Tolerance.Epsilon ? 0.0 : power;
        powerCase = GetPowerCase(this.power);
    }

    public Stripes(Pixel c1, Pixel c2, Vector direction)
        : this(c1, c2, direction, 1.0) { }

    public Stripes(Pixel c1, Pixel c2, double x, double y, double z)
        : this(c1, c2, new Vector(x, y, z), 1.0) { }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    void IPigment.GetColor(out Pixel color, in Vector location)
    {
        double t = 0.5 * (1.0 + Math.Cos(location * unit / length));
        switch (powerCase)
        {
            case PowerCase.Lineal: break;
            case PowerCase.SquareRoot: t = Math.Sqrt(t); break;
            case PowerCase.Squared: t *= t; break;
            case PowerCase.Cubic: t = t * t * t; break;
            case PowerCase.S: t = (3.0 - 2.0 * t) * t * t; break;
            default: t = Math.Pow(t, power); break;
        }
        color = color1.Lerp(delta, (float)t);
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel IPigment.DraftColor => color1;

    /// <summary>Creates an independent copy of this pigment.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new pigment.</returns>
    IPigment IPigment.Clone(bool force) =>
        force ? new Stripes(color1, color2, direction, power) : (this);
}

[XSight, Properties(nameof(color1), nameof(color2), nameof(scale))]
public sealed class Checkers : PigmentBase, IPigment
{
    private readonly Pixel color1, color2;
    private readonly Vector scale;
    private Vector translation;
    private readonly double width;

    /// <summary>Creates a checker pigment.</summary>
    /// <param name="color1">First color.</param>
    /// <param name="color2">Second color.</param>
    /// <param name="scale">Scale.</param>
    /// <param name="width">If greater than zero, line width.</param>
    /// <remarks>
    /// When the line width is zero, the pigment shows tiles with alternating colors.
    /// Otherwise, it shows tiles with the second color, separated by lines with the
    /// specified width and using the first color.
    /// </remarks>
    public Checkers(Pixel color1, Pixel color2, Vector scale, double width)
    {
        this.color1 = color1;
        this.color2 = color2;
        this.scale = scale;
        this.width = Math.Max(0.0, width);
        if (this.width > 0.0)
            translation = new Vector(0.5);
    }

    public Checkers(Pixel color1, Pixel color2, double scale, double width)
        : this(color1, color2, new Vector(scale), width) { }

    public Checkers(Pixel color1, Pixel color2, Vector scale)
        : this(color1, color2, scale, 0) { }

    public Checkers(Pixel color1, Pixel color2, double scale)
        : this(color1, color2, new Vector(scale)) { }

    public Checkers(Pixel color1, Pixel color2)
        : this(color1, color2, 1.0) { }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    void IPigment.GetColor(out Pixel color, in Vector location)
    {
        if (width > 0.0)
        {
            double t = ((location.X - translation.X) + Tolerance.Epsilon) * scale.X;
            if (Math.Abs(t - Math.Round(t)) <= width)
                color = color1;
            else
            {
                t = ((location.Y - translation.Y) + Tolerance.Epsilon) * scale.Y;
                if (Math.Abs(t - Math.Round(t)) <= width)
                    color = color1;
                else
                {
                    t = ((location.Z - translation.Z) + Tolerance.Epsilon) * scale.Z;
                    color = Math.Abs(t - Math.Round(t)) <= width ? color1 : color2;
                }
            }
        }
        else if ((((int)Math.Floor((
            (location.X - translation.X) + Tolerance.Epsilon) * scale.X)) % 2 == 0) ^
            (((int)Math.Floor((
            (location.Y - translation.Y) + Tolerance.Epsilon) * scale.Y)) % 2 == 0) ^
            (((int)Math.Floor((
            (location.Z - translation.Z) + Tolerance.Epsilon) * scale.Z)) % 2 == 0))
            color = color1;
        else
            color = color2;
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel IPigment.DraftColor => color1;

    /// <summary>Creates an independent copy of this pigment.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new pigment.</returns>
    IPigment IPigment.Clone(bool force) =>
        force
            ? new Checkers(color1, color2, scale, width) { translation = translation }
            : this;

    /// <summary>Translates the pigment.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated pigment.</returns>
    public override IPigment Translate(in Vector translation)
    {
        this.translation += translation;
        return this;
    }
}

[XSight, Properties("color1", "color2", "turbulence", "power", "scale")]
public sealed class Spots(Pixel color1, Pixel color2, Vector scale, int turbulence, double power) : PigmentBase, IPigment
{
    private readonly SolidNoise noise = new(1492);
    private readonly Pixel delta = color2 - color1;
    private Vector translation;
    private readonly short turbulence = (short)turbulence;
    private readonly double power = power < Tolerance.Epsilon ? 0.0 : power;
    private readonly PowerCase powerCase = GetPowerCase(power);

    public Spots(Pixel color1, Pixel color2, double scale, int turbulence, double power)
        : this(color1, color2, new Vector(scale), turbulence, power) { }

    public Spots(Pixel color1, Pixel color2, Vector scale, int turbulence)
        : this(color1, color2, scale, turbulence, 1.0) { }

    public Spots(Pixel color1, Pixel color2, double scale, int turbulence)
        : this(color1, color2, new Vector(scale), turbulence, 1.0) { }

    public Spots(Pixel color1, Pixel color2, Vector scale)
        : this(color1, color2, scale, 2, 1.0) { }

    public Spots(Pixel color1, Pixel color2, double scale)
        : this(color1, color2, new Vector(scale), 2, 1.0) { }

    public Spots(Pixel color1, Pixel color2)
        : this(color1, color2, new Vector(5), 2, 1.0) { }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    void IPigment.GetColor(out Pixel color, in Vector location)
    {
        double t = 0.5 * (noise[
            scale.X * (location.X - translation.X),
            scale.Y * (location.Y - translation.Y),
            scale.Z * (location.Z - translation.Z), turbulence] + 1.0);
        switch (powerCase)
        {
            case PowerCase.Lineal: break;
            case PowerCase.SquareRoot: t = Math.Sqrt(t); break;
            case PowerCase.Squared: t *= t; break;
            case PowerCase.Cubic: t = t * t * t; break;
            case PowerCase.S: t = (3.0 - 2.0 * t) * t * t; break;
            default: t = Math.Pow(t, power); break;
        }
        color = color1.Lerp(delta, (float)t);
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel IPigment.DraftColor => color1;

    /// <summary>Creates an independent copy of this pigment.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new pigment.</returns>
    IPigment IPigment.Clone(bool force) =>
        force
            ? new Spots(color1, color2, scale, turbulence, power) { translation = translation }
            : this;

    /// <summary>Translate the pigment.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated pigment.</returns>
    public override IPigment Translate(in Vector translation)
    {
        this.translation += translation;
        return this;
    }

    /// <summary>Scales the pigment.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The scaled pigment.</returns>
    public override IPigment Scale(in Vector factor)
    {
        translation = translation.Scale(factor);
        scale = scale.Scale(factor);
        return this;
    }
}

[XSight, Properties("color1, color2, low, high, scale, form")]
public sealed class Crackle : PigmentBase, IPigment
{
    private readonly CrackleNoise noise;
    private readonly Pixel color1;
    private readonly Pixel color2;
    private readonly Pixel delta;
    private readonly Vector scale;
    private readonly Vector form;
    private readonly double low;
    private readonly double high;
    private readonly double mult;
    private readonly bool isLinear;

    public Crackle(Pixel color1, Pixel color2,
        double low, double high, Vector scale, Vector form)
    {
        noise = new CrackleNoise(9125, form);
        this.color1 = color1;
        this.color2 = color2;
        delta = color2 - color1;
        this.scale = scale;
        if (low < 0.0)
            low = 0.0;
        if (high > 1.0)
            high = 1.0;
        if (low >= high)
        {
            low = 0.0;
            high = 1.0;
        }
        this.low = low;
        this.high = high;
        mult = 1.0 / (high - low);
        this.form = form;
    }

    public Crackle(Pixel color1, Pixel color2,
        double low, double high, double scale, Vector form)
        : this(color1, color2, low, high, new Vector(scale), form) { }

    public Crackle(Pixel color1, Pixel color2, double low, double high, Vector scale)
        : this(color1, color2, low, high, scale, new Vector(-1, 1, 0)) { }

    public Crackle(Pixel color1, Pixel color2, double low, double high, double scale)
        : this(color1, color2, low, high, new Vector(scale)) { }

    public Crackle(Pixel color1, Pixel color2, double low, double high)
        : this(color1, color2, low, high, new Vector(1)) { }

    public Crackle(Pixel color1, Pixel color2, Vector scale, Vector form)
        : this(color1, color2, 0, 1, scale, form) => isLinear = true;

    public Crackle(Pixel color1, Pixel color2, Vector scale)
        : this(color1, color2, scale, new Vector(-1, 1, 0)) { }

    public Crackle(Pixel color1, Pixel color2, double scale)
        : this(color1, color2, new Vector(scale), new Vector(-1, 1, 0)) { }

    public Crackle(Pixel color1, Pixel color2, double scale, Vector form)
        : this(color1, color2, new Vector(scale), form) { }

    public Crackle(Pixel color1, Pixel color2)
        : this(color1, color2, new Vector(1), new Vector(-1, 1, 0)) { }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    void IPigment.GetColor(out Pixel color, in Vector location)
    {
        double t = noise[scale.X * location.X, scale.Y * location.Y, scale.Z * location.Z];
        if (isLinear)
            color = color1.Lerp(delta, (float)t);
        else if (t <= low)
            color = color1;
        else if (t >= high)
            color = color2;
        else
        {
            t = mult * (t - low);
            t = (3.0 - 2.0 * t) * t * t;
            color = color1.Lerp(delta, (float)t);
        }
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel IPigment.DraftColor => color1;

    /// <summary>Creates an independent copy of this pigment.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new pigment.</returns>
    /// <remarks><see cref="Crackle"/> is not a stateless class.</remarks>
    IPigment IPigment.Clone(bool force) => new Crackle(color1, color2, low, high, scale, form);
}

[XSight, Properties("color1, color2, low, high, scl, turb")]
public sealed class Bubbles(Pixel color1, Pixel color2, Vector scale, int turbulence) : PigmentBase, IPigment
{
    private readonly CrackleNoise noise = new(9125, Vector.XRay);
    private readonly Pixel delta = color2 - color1;
    private readonly short turb = (short)turbulence;

    public Bubbles(Pixel color1, Pixel color2, Vector scale)
        : this(color1, color2, scale, 0) { }

    public Bubbles(Pixel color1, Pixel color2, double scale)
        : this(color1, color2, new Vector(scale), 0) { }

    public Bubbles(Pixel color1, Pixel color2, double scale, int turbulence)
        : this(color1, color2, new Vector(scale), turbulence) { }

    public Bubbles(Pixel color1, Pixel color2)
        : this(color1, color2, new Vector(1), 0) { }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    void IPigment.GetColor(out Pixel color, in Vector location)
    {
        double t = turb < 2 ?
            noise.Bubbles(
                scale.X * location.X, scale.Y * location.Y, scale.Z * location.Z) :
            noise.Bubbles(
                scale.X * location.X, scale.Y * location.Y, scale.Z * location.Z, turb);
        color = color1.Lerp(delta, (float)t);
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel IPigment.DraftColor => color1;

    /// <summary>Creates an independent copy of this pigment.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new pigment.</returns>
    /// <remarks><see cref="Bubbles"/> is not a stateless class.</remarks>
    IPigment IPigment.Clone(bool force) => new Bubbles(color1, color2, scale, turb);
}

[XSight, Properties("color1, color2, scl, turb")]
public sealed class Wood(Pixel color1, Pixel color2, Vector scale, int turbulence) : PigmentBase, IPigment
{
    private readonly SolidNoise noise = new(9125);
    private readonly Pixel delta = color2 - color1;
    private Vector translation;
    private readonly short turb = (short)turbulence;

    public Wood(Pixel color1, Pixel color2, double scale, int turbulence)
        : this(color1, color2, new Vector(scale), turbulence) { }

    public Wood(Pixel color1, Pixel color2, Vector scale)
        : this(color1, color2, scale, 0) { }

    public Wood(Pixel color1, Pixel color2, double scale)
        : this(color1, color2, new Vector(scale), 0) { }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    void IPigment.GetColor(out Pixel color, in Vector location)
    {
        double t = noise[
            scale.X * (location.X - translation.X),
            scale.Y * (location.Y - translation.Y),
            scale.Z * (location.Z - translation.Z), turb] * 20;
        color = color1.Lerp(delta, (float)(t - Math.Floor(t)));
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel IPigment.DraftColor => color1;

    /// <summary>Creates an independent copy of this pigment.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new pigment.</returns>
    IPigment IPigment.Clone(bool force) =>
        force ? new Wood(color1, color2, scale, turb) { translation = translation } : this;

    /// <summary>Translates the pigment.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated pigment.</returns>
    public override IPigment Translate(in Vector translation)
    {
        this.translation += translation;
        return this;
    }

    /// <summary>Scales the pigment.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The scaled pigment.</returns>
    public override IPigment Scale(in Vector factor)
    {
        translation = translation.Scale(factor);
        scale = scale.Scale(factor);
        return this;
    }
}

[XSight(Alias = "Bitmap"), Properties()]
public sealed class BitmapPigment : PigmentBase, IPigment
{
    private readonly string path;
    private readonly Bitmap bitmap;
    private Vector scale, translation;

    public BitmapPigment(string path)
    {
        this.path = FileService.FindFile(path);
        bitmap = new(this.path);
    }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    void IPigment.GetColor(out Pixel color, in Vector location)
    {
        const double InvPI = 1 / Math.PI;
        const double Inv2PI = 0.5 / Math.PI;
        color = bitmap.GetPixel(
            (int)((Math.Atan2(location.Z, location.X) + Math.PI) * Inv2PI * bitmap.Width),
            (int)((0.5 - Math.Atan2(location.Y, Math.Sqrt(
                location.X * location.X + location.Z * location.Z)) * InvPI) * bitmap.Height));
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel IPigment.DraftColor => Color.RoyalBlue;

    /// <summary>Creates an independent copy of this pigment.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new pigment.</returns>
    IPigment IPigment.Clone(bool force) =>
        new BitmapPigment(path) { scale = scale, translation = translation };

    /// <summary>Translates the pigment.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated pigment.</returns>
    public override IPigment Translate(in Vector translation)
    {
        this.translation += translation;
        return this;
    }

    /// <summary>Scales the pigment.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The scaled pigment.</returns>
    public override IPigment Scale(in Vector factor)
    {
        translation = translation.Scale(factor);
        scale = scale.Scale(factor);
        return this;
    }

    /// <summary>Rotates the pigment.</summary>
    /// <param name="rotation">The rotation matrix.</param>
    /// <returns>The rotated pigment.</returns>
    public override IPigment Rotate(in Matrix rotation) => base.Rotate(rotation);
}

[XSight, Properties]
public sealed class Layers : PigmentBase, IPigment
{
    private IPigment pigment1;
    private IPigment pigment2;
    private readonly float t;
    private readonly float tinv;

    public Layers(IPigment pigment1, double transparency, IPigment pigment2)
    {
        this.pigment1 = pigment1;
        this.pigment2 = pigment2;
        t = (float)transparency;
        tinv = 1.0F - t;
    }

    /// <summary>Gets the color at a given point.</summary>
    /// <param name="color">Resulting color.</param>
    /// <param name="location">Sampling location.</param>
    void IPigment.GetColor(out Pixel color, in Vector location)
    {
        pigment2.GetColor(out color, location);
        pigment1.GetColor(out Pixel filter, location);
        color = color * t + filter * tinv;
    }

    /// <summary>Gets a quick estimation of the color.</summary>
    Pixel IPigment.DraftColor => pigment2.DraftColor;

    /// <summary>Creates an independent copy of this pigment.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new pigment.</returns>
    IPigment IPigment.Clone(bool force)
    {
        IPigment p1 = pigment1.Clone(force);
        IPigment p2 = pigment2.Clone(force);
        return force || p1 != pigment1 || p2 != pigment2 ? new Layers(p1, t, p2) : this;
    }

    /// <summary>Translates the pigment.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated pigment.</returns>
    public override IPigment Translate(in Vector translation)
    {
        pigment1 = pigment1.Translate(translation);
        pigment2 = pigment2.Translate(translation);
        return this;
    }

    /// <summary>Scales the pigment.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The scaled pigment.</returns>
    public override IPigment Scale(in Vector factor)
    {
        pigment1 = pigment1.Scale(factor);
        pigment2 = pigment2.Scale(factor);
        return this;
    }

    /// <summary>Rotates the pigment.</summary>
    /// <param name="rotation">The rotation matrix.</param>
    /// <returns>The rotated pigment.</returns>
    public override IPigment Rotate(in Matrix rotation)
    {
        pigment1 = pigment1.Rotate(rotation);
        pigment2 = pigment2.Rotate(rotation);
        return this;
    }
}

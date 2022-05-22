#if USE_SSE

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Xml;
using static System.MathF;

#else

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Xml;
using static System.MathF;

#endif

namespace IntSight.RayTracing.Engine;

#if !USE_SSE

/// <summary>Represents RGB pixels with continue color components.</summary>
public readonly struct Pixel : IEquatable<Pixel>
{
    public static readonly Pixel Black;
    public static readonly Pixel White = new Pixel(1F);
    public static readonly Pixel RoyalBlue = Color.RoyalBlue;

    /// <summary>Red component of the pixel.</summary>
    private readonly float r;
    /// <summary>Green component of the pixel.</summary>
    private readonly float g;
    /// <summary>Blue component of the pixel.</summary>
    private readonly float b;

    /// <summary>Creates a pixel given its RGB coordinates as float values.</summary>
    /// <param name="red">The red component.</param>
    /// <param name="green">The green component.</param>
    /// <param name="blue">The blue component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Pixel(float red, float green, float blue) => (r, g, b) = (red, green, blue);

    /// <summary>Creates a pixel given its RGB coordinates as double values.</summary>
    /// <param name="red">The red component.</param>
    /// <param name="green">The green component.</param>
    /// <param name="blue">The blue component.</param>
    public Pixel(double red, double green, double blue)
    {
        r = red > 1.0 ? 1.0F : (float)red;
        g = green > 1.0 ? 1.0F : (float)green;
        b = blue > 1.0 ? 1.0F : (float)blue;
    }

    /// <summary>Creates a gray pixel given its intensity.</summary>
    /// <param name="intensity">Common value for all three RGB components.</param>
    public Pixel(double intensity) =>
        r = g = b = (float)Math.Min(Math.Max(intensity, 0.0), 1.0);

    /// <summary>Creates a gray pixel given its intensity, w/ no clipping.</summary>
    /// <param name="intensity">Common value for all three RGB components.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel(float intensity) => r = g = b = intensity;

    /// <summary>Creates a RGB pixel from an HSV color.</summary>
    /// <param name="hsv">Source color.</param>
    public Pixel(in HsvPixel hsv)
    {
        if (hsv.Sat == 0.0)
            r = g = b = (float)hsv.Val;
        else
        {
            float sectorPos = (float)(hsv.Hue / 60.0);
            int sectorNumber = (int)(Math.Floor(sectorPos));
            float fractionalSector = sectorPos - sectorNumber;
            float p = (float)(hsv.Val * (1.0 - hsv.Sat));
            float q = (float)(hsv.Val * (1.0 - (hsv.Sat * fractionalSector)));
            float t = (float)(hsv.Val * (1.0 - (hsv.Sat * (1.0 - fractionalSector))));
            switch (sectorNumber)
            {
                case 0:
                    r = (float)hsv.Val; g = t; b = p; break;
                case 1:
                    r = q; g = (float)hsv.Val; b = p; break;
                case 2:
                    r = p; g = (float)hsv.Val; b = t; break;
                case 3:
                    r = p; g = q; b = (float)hsv.Val; break;
                case 4:
                    r = t; g = p; b = (float)hsv.Val; break;
                default:
                    r = (float)hsv.Val; g = p; b = q; break;
            }
        }
    }

    /// <summary>Creates a color from a hue value in the HSV color space.</summary>
    /// <param name="hue">Hue of the new color.</param>
    /// <returns>A color with full saturation and brightness.</returns>
    public static Pixel FromHue(double hue)
    {
        // Clip hue between 0 and 360.
        float sectorPos = hue <= 0.0 ? 0.0F : hue >= 360.0 ? 60.0F : (float)(hue / 60.0);
        int sectorNumber = (int)sectorPos;
        return sectorNumber switch
        {
            0 => new(1.0F, sectorPos - sectorNumber, 0.0F),
            1 => new(1.0F - sectorPos + sectorNumber, 1.0F, 0.0F),
            2 => new(0.0F, 1.0F, sectorPos - sectorNumber),
            3 => new(0.0F, 1.0F - sectorPos + sectorNumber, 1.0F),
            4 => new(sectorPos - sectorNumber, 0.0F, 1.0F),
            _ => new(1.0F, 0.0F, 1.0F - sectorPos + sectorNumber)
        };
    }

    /// <summary>Extract the three components of this pixel.</summary>
    /// <param name="r">The red component.</param>
    /// <param name="g">The green component.</param>
    /// <param name="b">The blue component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out float r, out float g, out float b) =>
        (r, g, b) = (this.r, this.g, this.b);

    /// <summary>Gets the value of the red channel.</summary>
    public float Red => r;
    /// <summary>Gets the value of the green channel.</summary>
    public float Green => g;
    /// <summary>Gets the value of the blue channel.</summary>
    public float Blue => b;

    /// <summary>Gets the value of the dominant channel.</summary>
    public float Dominant => Max(Max(r, g), b);

    /// <summary>Checks whether the parameter is a pixel with the same components.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals([AllowNull] Pixel other) =>
        this == other;

    /// <summary>Checks whether the parameter is a pixel with the same components.</summary>
    /// <param name="obj">Object to check.</param>
    /// <returns>True when a pixel is passed with identical components.</returns>
    public override bool Equals(object obj) =>
        obj is Pixel pixel && this == pixel;

    /// <summary>Returns the hash code for this pixel.</summary>
    /// <returns>The combined hash code for its three components.</returns>
    public override int GetHashCode() =>
        r.GetHashCode() ^ g.GetHashCode() ^ b.GetHashCode();

    /// <summary>Checks two pixels for equality.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator==(in Pixel p1, in Pixel p2) =>
        p1.r == p2.r && p1.g == p2.g && p1.b == p2.b;

    /// <summary>Checks two pixels for inequality.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(in Pixel p1, in Pixel p2) =>
        p1.r != p2.r || p1.g != p2.g || p1.b != p2.b;

    /// <summary>Non-clipping addition.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel operator +(in Pixel p1, in Pixel p2) =>
        new(p1.r + p2.r, p1.g + p2.g, p1.b + p2.b);

    /// <summary>Creates the difference between two pixels for interpolation.</summary>
    /// <remarks>Deltas may contain negative components.</remarks>
    /// <param name="p1">First pixel.</param>
    /// <param name="p2">Second pixel.</param>
    /// <returns>A pixel representing the difference between the arguments.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel operator -(in Pixel p1, in Pixel p2) =>
        new(p1.r - p2.r, p1.g - p2.g, p1.b - p2.b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel operator -(float amount, in Pixel p) =>
        new(amount - p.r, amount - p.g, amount - p.b);

    /// <summary>Non-clipping multiplication by a scalar.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel operator *(in Pixel p1, float amount) =>
        new(p1.r * amount, p1.g * amount, p1.b * amount);

    /// <summary>Non-clipping multiplication.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel operator *(in Pixel p1, in Pixel p2) =>
        new(p1.r * p2.r, p1.g * p2.g, p1.b * p2.b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pixel(Color color) =>
        new(color.R * (1F / 255F), color.G * (1F / 255F), color.B * (1F / 255F));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pixel(TransPixel color) =>
        new(color.R * (1F / 255F), color.G * (1F / 255F), color.B * (1F / 255F));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Color(Pixel p) => Color.FromArgb(0xFF << 24 |
        (byte)(p.r * 255) << 16 | (byte)(p.g * 255) << 8 | (byte)(p.b * 255));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator TransPixel(Pixel p) => new(0xFF000000 |
        (uint)(byte)(p.r * 255) << 16 |
        (uint)(byte)(p.g * 255) << 8 |
        (byte)(p.b * 255));

    /// <summary>Converts a pixel into a color, with an alpha channel.</summary>
    /// <param name="alpha">Transparency: a value between 0 and 255.</param>
    /// <returns>The corresponding color.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TransPixel ToTransPixel(int alpha) => new(unchecked((uint)(
        alpha << 24 | (byte)(r * 255) << 16 | (byte)(g * 255) << 8 | (byte)(b * 255))));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TransPixel ToTransPixel(int alpha, float weight) => new(unchecked((uint)(
        alpha << 24
        | (byte)(r * weight) << 16
        | (byte)(g * weight) << 8
        | (byte)(b * weight))));

    /// <summary>Gets a weighted gray level from this pixel.</summary>
    /// <remarks>Green has the highest weight, then red and, finally, blue.</remarks>
    public double GrayLevel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => 0.212671 * r + 0.715160 * g + 0.072169 * b;
    }

    /// <summary>Adds and clips a weighted pixel to another pixel.</summary>
    /// <param name="newColor">Pixel to add.</param>
    /// <param name="filter">Light filter, for weighting the second pixel.</param>
    /// <param name="weight">Weight, between 0 and 1.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Add(in Pixel newColor, in Pixel filter, float weight)
    {
        float r1 = FusedMultiplyAdd(newColor.r * filter.r, weight, r);
        float g1 = FusedMultiplyAdd(newColor.g * filter.g, weight, g);
        float b1 = FusedMultiplyAdd(newColor.b * filter.b, weight, b);
        if (r1 > 1.0F) r1 = 1.0F;
        if (g1 > 1.0F) g1 = 1.0F;
        if (b1 > 1.0F) b1 = 1.0F;
        return new(r1, g1, b1);
    }

    /// <summary>Adds and clips a weighted pixel to another pixel.</summary>
    /// <param name="newColor">Pixel to add.</param>
    /// <param name="weight">Weight, between 0 and 1.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Add(in Pixel newColor, float weight) =>
        Lerp(newColor, weight).Clip();

    /// <summary>Adds and clips a weighted interpolated color to this pixel.</summary>
    /// <param name="color1">Original color in interpolation.</param>
    /// <param name="amount">Interpolation amount.</param>
    /// <param name="color2">Target color in interpolation.</param>
    /// <param name="weight">Weight of the interpolated color.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Add(in Pixel color1, float amount, in Pixel color2, float weight)
    {
        float r1 = FusedMultiplyAdd(FusedMultiplyAdd(color2.r - color1.r, amount, color1.r), weight, r);
        float g1 = FusedMultiplyAdd(FusedMultiplyAdd(color2.g - color1.g, amount, color1.g), weight, g);
        float b1 = FusedMultiplyAdd(FusedMultiplyAdd(color2.b - color1.b, amount, color1.b), weight, b);
        if (r1 > 1.0F) r1 = 1.0F;
        if (g1 > 1.0F) g1 = 1.0F;
        if (b1 > 1.0F) b1 = 1.0F;
        return new(r1, g1, b1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Clip() =>
        new(r > 1F ? 1F : r, g > 1F ? 1F : g, b > 1F ? 1F : b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Clip(in Pixel max) =>
        new(r > max.r ? max.r : r, g > max.g ? max.g : g, b > max.b ? max.b : b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Lerp(in Pixel delta, float amount) =>
        new(
            FusedMultiplyAdd(amount, delta.r, r),
            FusedMultiplyAdd(amount, delta.g, g),
            FusedMultiplyAdd(amount, delta.b, b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Lerp(in Pixel other, in Pixel filter, float weight) =>
        Lerp(other * filter, weight);

    /// <summary>Beer's attenuation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Attenuate(in Pixel attFilter, float time) => new(
        Exp(attFilter.r * time) * r,
        Exp(attFilter.g * time) * g,
        Exp(attFilter.b * time) * b);

    /// <summary>Saves this pixel as an XML attribute.</summary>
    /// <param name="writer">An XML writer.</param>
    /// <param name="attributeName">The XML attribute name.</param>
    public void WriteXmlAttribute(XmlWriter writer, string attributeName)
    {
        writer.WriteStartAttribute(attributeName);
        writer.WriteValue(string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0,5:F3},{1,5:F3},{2,5:F3}", r, g, b));
        writer.WriteEndAttribute();
    }

    /// <summary>Gets a text representation of the pixel's components.</summary>
    public override string ToString() =>
        string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "rgb<{0,5:F2}, {1,5:F2}, {2,5:F2}>", r, g, b);
}

#else

/// <summary>Represents RGB pixels with continue color components.</summary>
/// <remarks>This class does not support transparency by design.</remarks>
public readonly struct Pixel: IEquatable<Pixel>
{
    /// <summary>Represents a vector formed by ones.</summary>
    private static readonly Vector128<float> Ones =
        Vector128.Create(1f, 1f, 1f, 0f);

    public static readonly Pixel Black;
    public static readonly Pixel White = Color.White;
    public static readonly Pixel RoyalBlue = Color.RoyalBlue;

    /// <summary>The RGB components of the pixel.</summary>
    private readonly Vector128<float> rgb;

    /// <summary>Creates a pixel using vectorized RGB components.</summary>
    /// <param name="rgb">An SSE vector with the RGB components.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Pixel(in Vector128<float> rgb) => this.rgb = rgb;

    /// <summary>Creates a gray pixel given its intensity, w/ no clipping.</summary>
    /// <param name="intensity">Common value for all three RGB components.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel(float brightness) =>
        rgb = Vector128.Create(brightness, brightness, brightness, 0f);

    /// <summary>Creates a gray pixel given its intensity.</summary>
    /// <param name="intensity">Common value for all three RGB components.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel(double brightness) : this(Min((float)brightness, 1f)) { }

    /// <summary>Creates a pixel given its RGB coordinates as eight bytes values.</summary>
    /// <param name="red">The red component.</param>
    /// <param name="green">The green component.</param>
    /// <param name="blue">The blue component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel(double red, double green, double blue) =>
        rgb = Sse.Min(Ones, Vector128.Create(
            (float)red, (float)green, (float)blue, 0f));

    /// <summary>Creates a RGB pixel from an HSV color.</summary>
    /// <param name="hsv">Source color.</param>
    public Pixel(in HsvPixel hsv)
    {
        float r, g, b;
        if (hsv.Sat == 0.0)
            r = g = b = (float)hsv.Val;
        else
        {
            float sectorPos = (float)(hsv.Hue / 60.0);
            int sectorNumber = (int)(Math.Floor(sectorPos));
            float fractionalSector = sectorPos - sectorNumber;
            float p = (float)(hsv.Val * (1.0 - hsv.Sat));
            float q = (float)(hsv.Val * (1.0 - (hsv.Sat * fractionalSector)));
            float t = (float)(hsv.Val * (1.0 - (hsv.Sat * (1.0 - fractionalSector))));
            switch (sectorNumber)
            {
                case 0:
                    r = (float)hsv.Val; g = t; b = p; break;
                case 1:
                    r = q; g = (float)hsv.Val; b = p; break;
                case 2:
                    r = p; g = (float)hsv.Val; b = t; break;
                case 3:
                    r = p; g = q; b = (float)hsv.Val; break;
                case 4:
                    r = t; g = p; b = (float)hsv.Val; break;
                default:
                    r = (float)hsv.Val; g = p; b = q; break;
            }
        }
        rgb = Vector128.Create(r, g, b, 0f);
    }

    /// <summary>Creates a color from a hue value in the HSV color space.</summary>
    /// <param name="hue">Hue of the new color.</param>
    /// <returns>A color with full saturation and brightness.</returns>
    public static Pixel FromHue(double hue)
    {
        // Clip hue between 0 and 360.
        float sectorPos = hue <= 0.0 ? 0.0F : hue >= 360.0 ? 60.0F : (float)(hue / 60.0);
        int sectorNumber = (int)sectorPos;
        return sectorNumber switch
        {
            0 => new(1.0F, sectorPos - sectorNumber, 0.0F),
            1 => new(1.0F - sectorPos + sectorNumber, 1.0F, 0.0F),
            2 => new(0.0F, 1.0F, sectorPos - sectorNumber),
            3 => new(0.0F, 1.0F - sectorPos + sectorNumber, 1.0F),
            4 => new(sectorPos - sectorNumber, 0.0F, 1.0F),
            _ => new(1.0F, 0.0F, 1.0F - sectorPos + sectorNumber)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pixel(in Vector128<float> rgb) => new(rgb);

    public static implicit operator Pixel(Color color) =>
        Vector128.Create(color.R * (1F / 255F), color.G * (1F / 255F), color.B * (1F / 255F), 0f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pixel(TransPixel color) =>
        Vector128.Create(color.R * (1F / 255F), color.G * (1F / 255F), color.B * (1F / 255F), 0f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Color(in Pixel p)
    {
        var (r, g, b) = p * 255f;
        return Color.FromArgb(0xFF << 24 | (byte)r << 16 | (byte)g << 8 | (byte)b);
    }

    /// <summary>Converts a pixel into a pixel with transparency.</summary>
    /// <param name="p">Pixel to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator TransPixel(Pixel p)
    {
        var (r, g, b) = p * 255f;
        return new TransPixel(0xFF000000 |
            (uint)(byte)r << 16 | (uint)(byte)g << 8 | (byte)b);
    }

    /// <summary>Converts a pixel into a color, with an alpha channel.</summary>
    /// <param name="alpha">Transparency: a value between 0 and 255.</param>
    /// <returns>The corresponding color.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TransPixel ToTransPixel(int alpha)
    {
        var (r, g, b) = this * 255f;
        return new TransPixel(unchecked((uint)(
            alpha << 24 | (byte)r << 16 | (byte)g << 8 | (byte)b)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TransPixel ToTransPixel(int alpha, float weight)
    {
        var (r, g, b) = this * weight;
        return new TransPixel(unchecked((uint)(
            alpha << 24 | (byte)r << 16 | (byte)g << 8 | (byte)b)));
    }

    /// <summary>Extract the three components of this pixel.</summary>
    /// <param name="r">The red component.</param>
    /// <param name="g">The green component.</param>
    /// <param name="b">The blue component.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out float r, out float g, out float b) =>
        (r, g, b) = (rgb.ToScalar(), rgb.GetElement(1), rgb.GetElement(2));

    /// <summary>Gets the value of the red channel.</summary>
    public float Red
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => rgb.ToScalar();
    }

    /// <summary>Gets the value of the green channel.</summary>
    public float Green
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => rgb.GetElement(1);
    }

    /// <summary>Gets the value of the blue channel.</summary>
    public float Blue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => rgb.GetElement(2);
    }

    /// <summary>Gets a weighted gray level from this pixel.</summary>
    /// <remarks>Green has the highest weight, then red and, finally, blue.</remarks>
    public readonly double GrayLevel
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var (r, g, b) = this;
            return 0.212671 * r + 0.715160 * g + 0.072169 * b;
        }
    }

    /// <summary>Gets the dominant color channel.</summary>
    public float Dominant
    {
        get
        {
            var (r, g, b) = this;
            return Max(Max(r, g), b);
        }
    }

    /// <summary>Checks whether the parameter is a pixel with the same components.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals([AllowNull] Pixel other) => this == other;

    /// <summary>Checks whether the parameter is a pixel with the same components.</summary>
    /// <param name="obj">Object to check.</param>
    /// <returns>True when a pixel is passed with identical components.</returns>
    public override bool Equals(object obj) => obj is Pixel p && this == p;

    /// <summary>Returns the hash code for this pixel.</summary>
    /// <returns>The combined hash code for its three components.</returns>
    public override int GetHashCode() => rgb.GetHashCode();

    /// <summary>Checks two pixels for equality.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(in Pixel p1, in Pixel p2) =>
        p1.rgb.Equals(p2.rgb);

    /// <summary>Checks two pixels for inequality.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(in Pixel p1, in Pixel p2) =>
        !p1.rgb.Equals(p2.rgb);

    /// <summary>Non-clipping addition.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel operator +(in Pixel p1, in Pixel p2) =>
        Sse.Add(p1.rgb, p2.rgb);

    /// <summary>Creates the difference between two pixels for interpolation.</summary>
    /// <remarks>Deltas may contain negative components.</remarks>
    /// <param name="p1">First pixel.</param>
    /// <param name="p2">Second pixel.</param>
    /// <returns>A pixel representing the difference between the arguments.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel operator -(in Pixel p1, in Pixel p2) =>
        Sse.Subtract(p1.rgb, p2.rgb);

    /// <summary>Difference between a gray pixel and another one.</summary>
    /// <param name="amount">Intensity of the first pixel.</param>
    /// <param name="p2">Second pixel.</param>
    /// <returns>A pixel representing the difference between the arguments.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel operator -(float amount, in Pixel p) =>
        Sse.Subtract(Vector128.Create(amount, amount, amount, 0F), p.rgb);

    /// <summary>Non-clipping multiplication by a scalar.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel operator *(in Pixel p1, float amount) =>
        Sse.Multiply(Vector128.Create(amount, amount, amount, 0), p1.rgb);

    /// <summary>Non-clipping multiplication.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Pixel operator *(in Pixel p1, in Pixel p2) =>
        Sse.Multiply(p1.rgb, p2.rgb);

    /// <summary>Clips this pixel to white.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Clip() =>
        Sse.Min(rgb, Ones);

    /// <summary>Clips this pixel to a given color.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Clip(in Pixel max) =>
        Sse.Min(rgb, max.rgb);

    /// <summary>Adds and clips a weighted pixel to another pixel.</summary>
    /// <param name="newColor">Pixel to add.</param>
    /// <param name="filter">Light filter, for weighting the second pixel.</param>
    /// <param name="weight">Weight, between 0 and 1.</param>
    /// <returns>The new pixel.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Add(in Pixel newColor, in Pixel filter, float weight) =>
        Sse.Min(Sse.Add(Sse.Multiply(Sse.Multiply(
            Vector128.Create(weight),
            newColor.rgb), filter.rgb), rgb), Ones);

    /// <summary>Adds and clips a weighted pixel to another pixel.</summary>
    /// <param name="newColor">Pixel to add.</param>
    /// <param name="weight">Weight, between 0 and 1.</param>
    /// <returns>The new pixel.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Add(in Pixel newColor, float weight) =>
        Sse.Min(
            Fma.MultiplyAdd(
                Vector128.Create(weight),
                newColor.rgb, 
                rgb), 
            Ones);

    /// <summary>Adds and clips a weighted interpolated color to this pixel.</summary>
    /// <param name="color1">Original color in interpolation.</param>
    /// <param name="amount">Interpolation amount.</param>
    /// <param name="color2">Target color in interpolation.</param>
    /// <param name="weight">Weight of the interpolated color.</param>
    /// <returns>The resulting color.</returns>
    public Pixel Add(in Pixel color1, float amount, in Pixel color2, float weight) =>
        Sse.Min(
            Fma.MultiplyAdd(
                Fma.MultiplyAdd(
                    Sse.Subtract(color2.rgb, color1.rgb),
                    Vector128.Create(amount),
                    color1.rgb),
                Vector128.Create(weight),
                rgb),
            Ones);

    /// <summary>Adds a weighted pixel to another pixel with no clipping.</summary>
    /// <param name="newColor">Pixel to add.</param>
    /// <param name="filter">Light filter, for weighting the second pixel.</param>
    /// <param name="weight">Weight, between 0 and 1.</param>
    /// <returns>The new pixel.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Lerp(in Pixel newColor, in Pixel filter, float weight) =>
        Sse.Add(Sse.Multiply(Sse.Multiply(
            Vector128.Create(weight),
            newColor.rgb), filter.rgb), rgb);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Attenuate(in Pixel attFilter, float time)
    {
        var (r, g, b) = attFilter * time;
        return this * Vector128.Create(Exp(r), Exp(g), Exp(b), 0f);
    }

    /// <summary>Adds a weighted pixel to another pixel without clipping.</summary>
    /// <param name="delta">Pixel to add.</param>
    /// <param name="weight">Weight, between 0 and 1.</param>
    /// <returns>The new pixel.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pixel Lerp(in Pixel delta, float weight) =>
        Fma.MultiplyAdd(Vector128.Create(weight), delta.rgb, rgb);

    /// <summary>Saves this pixel as an XML attribute.</summary>
    /// <param name="writer">An XML writer.</param>
    /// <param name="attributeName">The XML attribute name.</param>
    public readonly void WriteXmlAttribute(XmlWriter writer, string attributeName)
    {
        var (r, g, b) = this;
        writer.WriteStartAttribute(attributeName);
        writer.WriteValue(string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0,5:F3},{1,5:F3},{2,5:F3}", r, g, b));
        writer.WriteEndAttribute();
    }

    /// <summary>Gets a text representation of the pixel's components.</summary>
    public override readonly string ToString()
    {
        var (r, g, b) = this;
        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "rgb<{0,5:F2}, {1,5:F2}, {2,5:F2}>", r, g, b);
    }
}

#endif

namespace IntSight.RayTracing.Engine;

/// <summary>Represents a pixel in the Hue/Saturation/Value color space.</summary>
public readonly struct HsvPixel
{
    public readonly double Hue;
    public readonly double Sat;
    public readonly double Val;

    /// <summary>Creates an HSV pixel from its three components.</summary>
    /// <param name="hue">Hue.</param>
    /// <param name="sat">Saturation.</param>
    /// <param name="val">Value.</param>
    public HsvPixel(double hue, double sat, double val) =>
        (Hue, Sat, Val) = (hue, sat, val);

    /// <summary>Creates an HSV pixel from a RGB color.</summary>
    /// <param name="p">Source color.</param>
    public HsvPixel(in Pixel p)
    {
        var (r, g, b) = p;
        double min = Math.Min(Math.Min(r, g), b);
        double max = Math.Max(Math.Max(r, g), b);
        double delta = max - min;

        Val = max;
        if (max == 0.0 || delta == 0.0)
            Sat = Hue = 0.0;
        else
        {
            Sat = delta / max;
            Hue = (r == max) ? 60.0 * (g - b) / delta :
                (g == max) ? 120.0 + 60.0 * (b - r) / delta :
                240.0 + 60.0 * (r - g) / delta;
            if (Hue < 0.0)
                Hue += 360.0;
        }
    }

    /// <summary>Color interpolation in the HSV space.</summary>
    /// <param name="amount">Weight of the second color, between 0.0 and 1.0.</param>
    /// <param name="other">Second color for interpolation.</param>
    /// <returns>An HSV pixel between the two supplied colors.</returns>
    public HsvPixel Interpolate(double amount, in HsvPixel other) =>
        amount <= 0 ? this :
            new(
                Hue + amount * (other.Hue - Hue),
                Sat + amount * (other.Sat - Sat),
                Val + amount * (other.Val - Val));
}

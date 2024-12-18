﻿using System.Drawing;

namespace IntSight.RayTracing.Engine;

/// <summary>A cheaper substitute for <see cref="Color"/>.</summary>
/// <remarks>Creates a color from its internal numeric representation.</remarks>
/// <param name="value">Alpha, red, green and blue channels.</param>
public readonly struct TransPixel(uint value)
{
    /// <summary>Gets the blue channel of the color.</summary>
    public byte B => unchecked((byte)value);
    /// <summary>Gets the green channel of the color.</summary>
    public byte G => unchecked((byte)(value >> 8));
    /// <summary>Gets the red channel of the color.</summary>
    public byte R => unchecked((byte)(value >> 16));

    public int ToArgb() => unchecked((int)value);

    public static TransPixel FromArgb(byte alpha, byte r, byte g, byte b) =>
        new((uint)alpha << 24 | (uint)r << 16 | (uint)g << 8 | b);

    public static TransPixel FromArgb(byte r, byte g, byte b) =>
        new(0xFF000000 | (uint)r << 16 | (uint)g << 8 | b);
}

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace IntSight.RayTracing.Engine;

/// <summary>Represents a rectangular pixel matrix.</summary>
public sealed class PixelMap
{
    /// <summary>Internal array containing pixels.</summary>
    internal readonly TransPixel[] pixs;
    private int[] scanLine;
    private readonly PixelStrip[] strips;
    private int nextStrip;

    internal PixelMap(int width, int height, int stripCount, bool incremental, IScene scene)
    {
        Width = width;
        Height = height;
        pixs = new TransPixel[height * width];
        Incremental = incremental;
        Scene = scene;
        strips = new PixelStrip[stripCount];
        int fromRow = 0, stripHeight = height / stripCount;
        for (int i = 0; i < strips.Length; i++)
            if (i == stripCount - 1)
                strips[i] = new PixelStrip(this, scene, fromRow, height - 1);
            else
            {
                strips[i] = new PixelStrip(this, scene, fromRow, fromRow + stripHeight - 1);
                fromRow += stripHeight;
            }
    }

    /// <summary>Creates a pixel map.</summary>
    /// <param name="width">Width of the map, in pixels.</param>
    /// <param name="height">Height of the map, in pixels.</param>
    public PixelMap(int width, int height)
        : this(width, height, 1, true, null) { }

    /// <summary>Creates a pixel map from a pixel array.</summary>
    /// <param name="array">An array of already computed pixels.</param>
    /// <param name="scene">The source scene, if any.</param>
    /// <remarks>
    /// This constructor is used by the motion blur composer.
    /// The scene passed as source is always the central scene.
    /// </remarks>
    public PixelMap(Pixel[] array, IScene scene)
    {
        Height = scene.Camera.Height;
        Width = scene.Camera.Width;
        pixs = new TransPixel[array.Length];
        for (int i = 0; i < pixs.Length; i++)
            pixs[i] = array[i];
        Incremental = true;
        Scene = scene;
        strips = new[] { new PixelStrip(this, scene, 0, Height - 1) };
    }

    internal PixelStrip GetStrip()
    {
        int assignedStrip = Interlocked.Increment(ref nextStrip);
        return assignedStrip > strips.Length ? null : strips[assignedStrip - 1];
    }

    /// <summary>Adds a weighted copy of this map to an array of pixels.</summary>
    /// <param name="array">Array used as accumulator.</param>
    /// <param name="weight">Weight of this map.</param>
    /// <returns>The modified accumulator.</returns>
    public Pixel[] Add(Pixel[] array, double weight)
    {
        array ??= new Pixel[Height * Width];
        float w = (float)weight;
        for (int i = 0; i < array.Length; i++)
            array[i] = array[i].Add(pixs[i], w);
        return array;
    }

    /// <summary>Pixel map's width.</summary>
    public int Width { get; }
    /// <summary>Pixel map's height.</summary>
    public int Height { get; }
    /// <summary>Symbolic scene description used for rendering.</summary>
    public IScene Scene { get; }
    /// <summary>Gets the scene's title.</summary>
    public string Title { get; init; }

    /// <summary>The pixel map should be drawn as created.</summary>
    public bool Incremental { get; }

    /// <summary>Total number of samples taken for all the scene.</summary>
    public int SuperSamples { get; set; }
    /// <summary>Gets the rendering time, in milliseconds.</summary>
    public int RenderTime { get; set; }
    /// <summary>Gets the parsing time, in milliseconds.</summary>
    public int ParsingTime { get; set; }
    /// <summary>Gets number of garbage collections while rendering.</summary>
    public int CollectionCount { get; set; }
    public bool Prepared { get; set; }

    /// <summary>Highest number of bounces for a ray in the scene.</summary>
    public int MaxDepth
    {
        get
        {
            int result = 0;
            foreach (PixelStrip strip in strips)
                if (strip.MaxDepth > result)
                    result = strip.MaxDepth;
            return result;
        }
    }

    /// <summary>Gets the average number of samples by pixel.</summary>
    public double SamplesPerPixel => (double)SuperSamples / (Width * Height);

    /// <summary>Gets the total number of drawn lines so far.</summary>
    public int LinesDrawn
    {
        get
        {
            int total = 0;
            foreach (PixelStrip strip in strips)
                total += strip.nextDraw - strip.fromRow;
            return total;
        }
    }

    /// <summary>Gets the total number of rendered rows so far.</summary>
    public int Completed
    {
        get
        {
            int total = 0;
            foreach (PixelStrip strip in strips)
                total += strip.nextRow - strip.fromRow;
            return total;
        }
    }

    /// <summary>Gets or sets the color at the given coordinates.</summary>
    /// <param name="row">Row position.</param>
    /// <param name="column">Column position.</param>
    /// <returns>The color at the specified pixel.</returns>
    public TransPixel this[int row, int column]
    {
        get => pixs[row * Width + column];
        set => pixs[row * Width + column] = value;
    }

    /// <summary>Weights an intensity for the edge-finding filter.</summary>
    /// <param name="value">A channel intensity.</param>
    /// <returns>The weighted intensity.</returns>
    private static byte Scl(int value) =>
        (byte)(value <= 0
            ? 0
            : value <= 10
            ? 4 * value
            : value <= 40
            ? 10 + 3 * value
            : value <= 80
            ? 50 + 2 * value
            : value <= 125
            ? 130 + value
            : 255);

    /// <summary>
    /// Applies a high pass filter (HPF) to the image to detect sharp transitions.
    /// </summary>
    /// <returns>A pixel map with non-zero values at the edges of the original image.</returns>
    /// <remarks>
    /// <see cref="AdaptiveSampler"/> has a very similar algorithm for detecting edges.
    /// It blurs the edges in order to oversample pixels in a border or near one of them.
    /// </remarks>
    public PixelMap EdgeMap()
    {
        PixelMap result = new(Width, Height, 1, Incremental, Scene);
        int max = pixs.Length - Width - 1;
        for (int i = Width + 1; i < max; i++)
        {
            TransPixel c1 = pixs[i - Width - 1], c2 = pixs[i - Width], c3 = pixs[i - Width + 1];
            TransPixel c4 = pixs[i - 1], c5 = pixs[i], c6 = pixs[i + 1];
            TransPixel c7 = pixs[i + Width - 1], c8 = pixs[i + Width], c9 = pixs[i + Width + 1];
            result.pixs[i] = TransPixel.FromArgb(255,
                Scl((8 * c5.R - c1.R - c2.R - c3.R - c4.R - c6.R - c7.R - c8.R - c9.R) / 8),
                Scl((8 * c5.G - c1.G - c2.G - c3.G - c4.G - c6.G - c7.G - c8.G - c9.G) / 8),
                Scl((8 * c5.B - c1.B - c2.B - c3.B - c4.B - c6.B - c7.B - c8.B - c9.B) / 8));
        }
        return result;
    }

    /// <summary>Converts a pixel map into a bitmap, with no transparency channel.</summary>
    /// <returns>A new bitmap.</returns>
    public Bitmap ToBitmap() => ToBitmap(false);

    /// <summary>Converts a pixel map into a bitmap.</summary>
    /// <param name="useAlpha">True, if a transparency channel must be used.</param>
    /// <returns>A new bitmap.</returns>
    public Bitmap ToBitmap(bool useAlpha)
    {
        Bitmap bmp = new(Width, Height, PixelFormat.Format32bppArgb);
        try
        {
            PartialDraw(bmp, 0, Height - 1, useAlpha);
        }
        catch
        {
            bmp.Dispose();
            throw;
        }
        finally
        {
            scanLine = null;
        }
        return bmp;
    }

    /// <summary>Copies a band of the pixel map into a bitmap.</summary>
    /// <param name="target">Bitmap where the band will be copied.</param>
    /// <param name="row0">First row to copy.</param>
    /// <param name="row1">Last row to copy.</param>
    /// <param name="useAlpha">Must we respect transparency?</param>
    /// <returns>The invalid rectangle inside the bitmap.</returns>
    private Rectangle PartialDraw(Bitmap target, int row0, int row1, bool useAlpha)
    {
        // Precondition: row0 <= row1
        Rectangle invArea = new(0, Height - row1 - 1, Width, row1 - row0 + 1);
        BitmapData data = target.LockBits(
            invArea, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            int len = (row1 - row0 + 1) * Width, pixIdx = (row1 + 1) * Width, i = 0;
            int[] scan = scanLine;
            if (scan == null || len > scan.Length)
                scanLine = scan = new int[len];
            ref int s = ref scan[0];
            ref TransPixel p = ref pixs[0];
            if (useAlpha)
                while (i < len)
                    Unsafe.Add(ref s, i++) = Unsafe.Add(ref p, --pixIdx).ToArgb();
            else
                while (i < len)
                    Unsafe.Add(ref s, i++) =
                        (int)((uint)Unsafe.Add(ref p, --pixIdx).ToArgb() | 0xff000000);
            Marshal.Copy(scan, 0, data.Scan0, len);
        }
        finally
        {
            target.UnlockBits(data);
        }
        return invArea;
    }

    public Rectangle Draw(Bitmap bitmap, int fromRow, int toRow) =>
        PartialDraw(bitmap, fromRow, toRow, false);

    /// <summary>Draw the already completed areas in a pixel map into a bitmap.</summary>
    /// <param name="bitmap">Target bitmap.</param>
    /// <returns>The invalid rectangle, for redraw.</returns>
    public Rectangle Draw(Bitmap bitmap)
    {
        Rectangle result = Rectangle.Empty;
        if (Incremental)
            foreach (PixelStrip strip in strips)
            {
                int row = strip.nextRow - 1, draw = strip.nextDraw;
                if (row > draw)
                {
                    result = Rectangle.Union(result,
                        PartialDraw(bitmap, draw, row - 1, false));
                    strip.nextDraw = row;
                }
            }
        return result;
    }

    /// <summary>Computes the sum of supersamples for all strips.</summary>
    /// <returns>The total number of supersamples.</returns>
    internal int TotalSuperSamples()
    {
        int total = 0;
        foreach (PixelStrip strip in strips)
            total += strip.SuperSamples;
        return total;
    }
}

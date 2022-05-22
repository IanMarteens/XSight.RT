using System.Drawing;
using Rsc = IntSight.RayTracing.Engine.Properties.Resources;

namespace IntSight.RayTracing.Engine;

[XSight(Alias = "Flat")]
public sealed class FlatBackground : IBackground
{
    private readonly Pixel color1, color2;
    private readonly Vector up;

    [Preferred]
    public FlatBackground(
        [Proposed("Black")] Pixel bgcolor)
    {
        color1 = color2 = bgcolor;
        up = Vector.YRay;
    }

    public FlatBackground(
        [Proposed("0.1")] double intensity)
    {
        color1 = color2 = new Pixel(intensity);
        up = Vector.YRay;
    }

    public FlatBackground(
        [Proposed("Black")] Pixel color1,
        [Proposed("MidnightBlue")] Pixel color2,
        [Proposed("[0,1,0]")] Vector up)
    {
        this.color1 = color1;
        this.color2 = color2;
        this.up = up;
    }

    public FlatBackground(
        [Proposed("Black")] Pixel color1,
        [Proposed("MidnightBlue")] Pixel color2)
    {
        this.color1 = color1;
        this.color2 = color2;
        up = Vector.YRay;
    }

    #region IBackground members.

    IBackground IBackground.Clone() => this;

    void IBackground.Initialize(IScene scene) { }
    Pixel IBackground.this[Ray ray] => color1;
    Pixel IBackground.DraftColor(Ray ray) => color1;

    IBackground IBackground.Simplify() =>
        color1 == color2
            ? new FlatBackgroundImpl(color1)
            : new GradientBackgroundImpl(color1, color2, up);

    #endregion
}

internal sealed class FlatBackgroundImpl : IBackground
{
    private readonly Pixel color;

    public FlatBackgroundImpl(Pixel bgcolor) => color = bgcolor;

    #region IBackground members.

    IBackground IBackground.Clone() => this;

    void IBackground.Initialize(IScene scene) { }
    Pixel IBackground.this[Ray ray] => color;
    Pixel IBackground.DraftColor(Ray ray) => color;

    IBackground IBackground.Simplify() => this;

    #endregion
}

internal sealed class GradientBackgroundImpl : IBackground
{
    private readonly Pixel color1, color2, middle, delta;
    private readonly Vector up;

    public GradientBackgroundImpl(Pixel color1, Pixel color2, Vector up)
    {
        this.color1 = color1;
        this.color2 = color2;
        middle = (color1 + color2) * 0.5f;
        delta = (color2 - color1) * 0.5f;
        this.up = up.Normalized();
    }

    #region IBackground members.

    IBackground IBackground.Clone() => this;

    void IBackground.Initialize(IScene scene) { }

    Pixel IBackground.this[Ray ray] => middle.Lerp(delta, (float)(ray.Direction * up));

    Pixel IBackground.DraftColor(Ray ray) => up * ray.Direction > 0.0 ? color2 : color1;

    IBackground IBackground.Simplify() => this;

    #endregion
}

[XSight(Alias = "Sky")]
public sealed class SkyBackground : IBackground
{
    private readonly Pixel delta;
    private readonly SolidNoise noise = new(2001);
    private readonly short turbulence;

    public SkyBackground(
       [Proposed("RoyalBlue")] Pixel bgcolor,
       [Proposed("White")] Pixel clouds,
       [Proposed("3")]int turbulence)
    {
        BgColor = bgcolor;
        Clouds = clouds;
        delta = clouds - bgcolor;
        if (turbulence > 0)
            this.turbulence = (short)turbulence;
    }

    public SkyBackground(
        [Proposed("RoyalBlue")] Pixel bgcolor,
        [Proposed("3")]int turbulence)
        : this(bgcolor, Pixel.White, turbulence) { }

    [Preferred]
    public SkyBackground(
        [Proposed("RoyalBlue")] Pixel bgcolor,
        [Proposed("White")]Pixel clouds)
        : this(bgcolor, clouds, 3) { }

    public SkyBackground(
        [Proposed("RoyalBlue")] Pixel bgcolor)
        : this(bgcolor, Pixel.White, 3) { }

    public SkyBackground()
        : this(Pixel.RoyalBlue, Pixel.White, 3) { }

    public Pixel BgColor { get; }
    public Pixel Clouds { get; }

    #region IBackground members

    IBackground IBackground.Clone() => this;

    void IBackground.Initialize(IScene scene) { }

    Pixel IBackground.this[Ray ray]
    {
        get
        {
            float t = 0.5F * (float)(noise[5 * ray.Direction.X, 5 * ray.Direction.Y,
                5 * ray.Direction.Z, turbulence] + 1.0);
            return BgColor.Lerp(delta, t * t * t);
        }
    }

    Pixel IBackground.DraftColor(Ray ray) => BgColor;

    IBackground IBackground.Simplify() => this;

    #endregion
}

[XSight(Alias = "FlatSky")]
public sealed class FlatSkyBackground : IBackground
{
    private readonly Pixel delta;
    private readonly Vector scale;
    private readonly float threshold;
    private readonly float quadSize;
    private readonly double y0;
    private readonly double den0;
    private readonly double den1;
    private readonly SolidNoise noise = new(2001);
    private readonly short turbulence;
    private readonly short profile;
    private readonly bool changeQuadrant;

    public FlatSkyBackground(
        [Proposed("RoyalBlue")] Pixel bgcolor,
        [Proposed("White")] Pixel clouds,
        [Proposed("[0.5,1,0.5]")] Vector scale,
        [Proposed("3")] int turbulence,
        [Proposed("0.5")] double threshold,
        [Proposed("2")] int profile,
        [Proposed("90")] double quadSize)
    {
        BgColor = bgcolor;
        Clouds = clouds;
        delta = clouds - bgcolor;
        if (threshold < 0.0)
            this.threshold = 0F;
        else if (threshold > 1.0)
            this.threshold = 1F;
        else
            this.threshold = (float)threshold;
        if (profile < 1)
            profile = 1;
        else if (profile > 3)
            profile = 3;
        this.profile = (short)profile;
        if (turbulence > 0)
            this.turbulence = (short)turbulence;
        this.scale = scale;
        this.quadSize = (float)quadSize;
        changeQuadrant = !Tolerance.Zero(quadSize - 90.0);
        y0 = Math.Cos(quadSize * Math.PI / 180.0);
        den0 = 1.0 / (1.0 - y0);
        den1 = 1.0 / (1.0 + y0);
    }

    public FlatSkyBackground(
        [Proposed("RoyalBlue")] Pixel bgcolor,
        [Proposed("White")] Pixel clouds,
        [Proposed("[0.5,1,0.5]")] Vector scale,
        [Proposed("3")] int turbulence,
        [Proposed("0.0")] double threshold,
        [Proposed("2")] int profile)
        : this(bgcolor, clouds, scale, turbulence, threshold, profile, 90.0) { }

    public FlatSkyBackground(
        [Proposed("RoyalBlue")] Pixel bgcolor,
        [Proposed("White")] Pixel clouds,
        [Proposed("[0.5,1,0.5]")] Vector scale,
        [Proposed("3")] int turbulence)
        : this(bgcolor, clouds, scale, turbulence, 0.0, 2) { }

    public FlatSkyBackground(
        [Proposed("RoyalBlue")] Pixel bgcolor,
        [Proposed("White")] Pixel clouds,
        [Proposed("[0.5,1,0.5]")] Vector scale)
        : this(bgcolor, clouds, scale, 3, 0.0, 2) { }

    public FlatSkyBackground(
        [Proposed("RoyalBlue")] Pixel bgcolor,
        [Proposed("[0.5,1,0.5]")] Vector scale,
        [Proposed("3")]int turbulence)
        : this(bgcolor, Pixel.White, scale, turbulence, 0.0, 2) { }

    public FlatSkyBackground(
        [Proposed("RoyalBlue")] Pixel bgcolor,
        [Proposed("[0.5,1,0.5]")] Vector scale)
        : this(bgcolor, Pixel.White, scale, 3, 0.0, 2) { }

    [Preferred]
    public FlatSkyBackground(
        [Proposed("RoyalBlue")] Pixel bgcolor,
        [Proposed("White")]Pixel clouds)
        : this(bgcolor, clouds, new Vector(0.5, 1.0, 0.5), 3, 0.0, 2) { }

    public FlatSkyBackground(
        [Proposed("RoyalBlue")] Pixel bgcolor)
        : this(bgcolor, Pixel.White, new Vector(10), 3, 0.0, 2) { }

    public FlatSkyBackground(
        [Proposed("[10,10,10]")] Vector scale)
        : this(Pixel.RoyalBlue, Pixel.White, scale, 3, 0.0, 2) { }

    public FlatSkyBackground(
        [Proposed("[10,10,10]")] Vector scale,
        [Proposed("90")] double quadSize)
        : this(Pixel.RoyalBlue, Pixel.White, scale, 3, 0.0, 2, quadSize) { }

    public FlatSkyBackground(
        [Proposed("[10,10,10]")] Vector scale,
        [Proposed("3")] int turbulence,
        [Proposed("0.0")] double threshold,
        [Proposed("2")] int profile)
        : this(Pixel.RoyalBlue, Pixel.White, scale, turbulence, threshold, profile) { }

    public FlatSkyBackground(
        [Proposed("[10,10,10]")] Vector scale,
        [Proposed("3")] int turbulence,
        [Proposed("0.0")] double threshold,
        [Proposed("2")] int profile,
        [Proposed("90")] double quadSize)
        : this(Pixel.RoyalBlue, Pixel.White,
            scale, turbulence, threshold, profile, quadSize)
    { }

    public FlatSkyBackground()
        : this(Pixel.RoyalBlue, Pixel.White, new Vector(10), 3, 0.0, 2) { }

    public Pixel BgColor { get; }
    public Pixel Clouds { get; }

    #region IBackground members.

    IBackground IBackground.Clone() =>
        new FlatSkyBackground(
            BgColor, Clouds, scale, turbulence, threshold, profile, quadSize);

    void IBackground.Initialize(IScene scene) { }

    Pixel IBackground.this[Ray ray]
    {
        get
        {
            Vector dir = ray.Direction;
            if (changeQuadrant)
            {
                double newY = (dir.Y - y0) * (dir.Y >= y0 ? den0 : den1);
                double len = 1.0 / Math.Sqrt(1.0 - dir.Y * dir.Y + newY * newY);
                dir = new(dir.X * len, newY * len, dir.Z * len);
            }
            if (Tolerance.Zero(dir.Y))
                return Clouds;
            else
            {
                float t = 0.5F * (float)(noise[
                    scale.X * dir.X / dir.Y,
                    scale.Y,
                    scale.Z * dir.Z / dir.Y, turbulence] + 1.0);
                if (t < threshold)
                    t = 0.0F;
                else
                    t = (t - threshold) / (1F - threshold);
                return profile switch
                {
                    2 => BgColor.Lerp(delta, t * t),
                    3 => BgColor.Lerp(delta, t * t * t),
                    _ => BgColor.Lerp(delta, t)
                };
            }
        }
    }

    Pixel IBackground.DraftColor(Ray ray) => BgColor;

    IBackground IBackground.Simplify() => this;

    #endregion
}

[XSight(Alias = "Bmp")]
public sealed class BitmapBackground : IBackground
{
    private enum Method { File, Flat, Noise }

    private readonly string fileName;
    private Bitmap bitmap;
    private ICamera camera;
    private Vector camPos;
    private double aperture;
    private int height, width;
    private readonly Method method;
    private readonly int turbulence;
    private readonly int seed;
    private readonly double scaleX;
    private readonly double scaleY;
    private readonly double offX;
    private readonly double offY;
    private readonly Pixel color;

    /// <summary>Creates a background based on an image in a file.</summary>
    /// <param name="bitmap">The bitmap file.</param>
    public BitmapBackground(string bitmap) =>
        (method, fileName) = (Method.File, FileService.FindFile(bitmap));

    public BitmapBackground(int turbulence)
    {
        method = Method.Noise;
        this.turbulence = (short)turbulence;
        seed = Environment.TickCount;
        scaleX = scaleY = 1.0;
    }

    [Preferred]
    public BitmapBackground(
        [Proposed("0")] int turbulence,
        [Proposed("42")] int seed)
    {
        method = Method.Noise;
        this.turbulence = (short)turbulence;
        this.seed = seed;
        scaleX = scaleY = 1.0;
    }

    public BitmapBackground(int turbulence, int seed, double scaleX, double scaleY)
    {
        method = Method.Noise;
        this.turbulence = (short)turbulence;
        this.seed = seed;
        this.scaleX = scaleX;
        this.scaleY = scaleY;
    }

    public BitmapBackground(int turbulence, int seed,
        double scaleX, double scaleY, double offX, double offY)
    {
        method = Method.Noise;
        this.turbulence = (short)turbulence;
        this.seed = seed;
        this.scaleX = scaleX;
        this.scaleY = scaleY;
        this.offX = offX;
        this.offY = offY;
    }

    public BitmapBackground(Pixel color) => (method, this.color) = (Method.Flat, color);

    #region IBackground members.

    IBackground IBackground.Clone() =>
        method switch
        {
            Method.File => new BitmapBackground(fileName),
            Method.Flat => new BitmapBackground(color),
            _ => new BitmapBackground(turbulence, seed, scaleX, scaleY, offX, offY)
        };


    void IBackground.Initialize(IScene scene)
    {
        if (method == Method.File && !System.IO.File.Exists(fileName))
            throw new RenderException(Rsc.ErrorBitmapNotFound, fileName);
        camera = scene.Camera;
        camPos = camera.Location;
        aperture = scene.Sampler.Aperture;
        if (method == Method.Flat)
        {
            bitmap = new(camera.Width, camera.Height);
            using Graphics g = Graphics.FromImage(bitmap);
            g.FillRectangle(new SolidBrush(color),
                new(0, 0, camera.Width, camera.Height));
        }
        else if (method == Method.Noise)
            bitmap = SolidNoise.GenerateNoise(
                (short)turbulence, seed, scaleX, scaleY, offX, offY,
                camera.Width, camera.Height).ToBitmap();
        else
            using (Bitmap bmp = new(fileName))
                bitmap = new(bmp, new Size(camera.Width, camera.Height));
        height = bitmap.Height - 1;
        width = bitmap.Width - 1;
    }

    Pixel IBackground.this[Ray ray]
    {
        get
        {
            if (Math.Abs(ray.Origin.X - camPos.X) <= aperture &&
                Math.Abs(ray.Origin.Y - camPos.Y) <= aperture &&
                Math.Abs(ray.Origin.Z - camPos.Z) <= aperture)
            {
                camera.GetRayCoordinates(ray, out int r, out int c);
                return r < 0 || r > height || c < 0 || c > width
                    ? Pixel.Black
                    : (Pixel)bitmap.GetPixel(c, height - r);
            }
            else
                return new();
        }
    }

    Pixel IBackground.DraftColor(Ray ray) => new();

    IBackground IBackground.Simplify() => this;

    #endregion
}
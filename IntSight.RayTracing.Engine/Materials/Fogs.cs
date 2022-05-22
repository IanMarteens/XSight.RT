namespace IntSight.RayTracing.Engine;

[XSight]
public sealed class ConstantFog : IMedia
{
    private readonly double distance;
    private readonly float filter;
    private readonly float threshold;
    private readonly Pixel tint, f, min, delta;

    public ConstantFog(Pixel tint, double distance, double filter, double threshold)
    {
        this.tint = tint;
        this.distance = Math.Max(distance, Tolerance.Epsilon);
        this.filter = (float)filter;
        this.threshold = (float)Math.Max(Math.Min(threshold, 1.0), 0.0);
        f = 1F - this.filter - tint * this.filter;
        min = tint * (1F - this.threshold);
        delta = f * this.threshold;
    }

    public ConstantFog(Pixel tint, double distance, double filter)
        : this(tint, distance, filter, 0.0) { }

    public ConstantFog(Pixel tint, double distance)
        : this(tint, distance, 0.0, 0.0) { }

    public ConstantFog(double distance)
        : this(Pixel.White, distance, 0.0, 0.0) { }

    #region IMedia members.

    IMedia IMedia.Clone() => new ConstantFog(tint, distance, filter, threshold);

    void IMedia.Initialize(IScene scene) { }

    /// <summary>Modifies color for a finite ray.</summary>
    /// <param name="ray">Primary or secondary ray.</param>
    /// <param name="time">Time to intersection.</param>
    /// <param name="color">Color, before and after media interaction.</param>
    void IMedia.Modify(Ray ray, double time, ref Pixel color)
    {
        float att = (float)Math.Exp(-ray.Direction.Length * time / distance);
        if (att < threshold)
            att = threshold;
        color = tint + (f * color - tint) * att;
    }

    Pixel IMedia.Modify(Ray ray, in Pixel color) =>
        threshold == 0 ? tint : min + delta * color;

    #endregion
}

[XSight(Alias = "Fog")]
public sealed class GroundFog : IMedia
{
    private readonly double distance;
    private readonly double fade;
    private readonly double offset;
    private readonly double offFade;
    private readonly double filter;
    private readonly float invDist;
    private readonly float attMax;
    private readonly float threshold;
    private readonly Pixel tint, f;
    private Vector up;

    public GroundFog(Pixel tint, double distance, double fade, double offset,
        double filter, double threshold)
    {
        this.tint = tint;
        this.distance = Math.Max(distance, Tolerance.Epsilon);
        this.fade = Math.Max(fade, Tolerance.Epsilon);
        this.offset = offset;
        this.filter = filter;
        this.threshold = MathF.Max(MathF.Min((float)threshold, 1F), 0F);
        double invFade = 1.0 / this.fade;
        up = Vector.YRay * invFade;
        offFade = offset * invFade;
        invDist = 1F / (float)distance;
        attMax = (float)Math.Exp(-0.5 * Math.PI / this.distance);
        float flt = (float)filter;
        f = Pixel.White + (tint - Pixel.White) * flt;
    }

    public GroundFog(Pixel tint, double distance,
        double fade, double offset, double filter)
        : this(tint, distance, fade, offset, filter, 0.0) { }

    public GroundFog(Pixel tint, double distance, double fade, double offset)
        : this(tint, distance, fade, offset, 0.0, 0.0) { }

    public GroundFog(double distance, double fade, double offset)
        : this(Pixel.White, distance, fade, offset, 0.0, 0.0) { }

    #region IMedia members.

    IMedia IMedia.Clone() =>
        new GroundFog(tint, distance, fade, offset, filter, threshold);

    void IMedia.Initialize(IScene scene) =>
        up = scene.Camera.Up.Normalized() / fade;

    /// <summary>Modifies color for a finite ray.</summary>
    /// <param name="ray">Primary or secondary ray.</param>
    /// <param name="time">Time to intersection.</param>
    /// <param name="color">Color, before and after media interaction.</param>
    void IMedia.Modify(Ray ray, double time, ref Pixel color)
    {
        // Compute the initial height, relative to the "up" vector and
        // scale according to the fog's offset and fade.
        float h0 = (float)(ray.Origin * up - offFade);
        float dh = (float)(ray.Direction * up * time);
        float h1 = h0 + dh;
        float att;
        if (h0 <= 0.0)
            att = h1 <= 0F ? invDist : (MathF.Atan(h1) - h0) * invDist / dh;
        else if (h1 <= 0)
            att = (h1 - MathF.Atan(h0)) * invDist / dh;
        else if (MathF.Abs(dh) > Tolerance.Epsilon)
            att = (MathF.Atan(h1) - MathF.Atan(h0)) * invDist / dh;
        else
            att = invDist / (1F + h0 * h0);
        att = MathF.Exp(-(float)(ray.Direction.Length * time) * att);
        if (att < threshold)
            att = threshold;
        color = tint + (color * f - tint) * att;
    }

    Pixel IMedia.Modify(Ray ray, in Pixel color)
    {
        float att = threshold;
        if (ray.Direction * up >= 0.0)
        {
            float h0 = (float)(ray.Origin * up - offFade);
            if (h0 > 0.0)
                h0 = MathF.Atan(h0);
            if ((att = attMax * MathF.Exp(h0 * invDist)) < threshold)
                att = threshold;
        }
        return tint + (color * f - tint) * att;
    }

    #endregion
}

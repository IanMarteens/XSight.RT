using static System.Runtime.CompilerServices.Unsafe;

namespace IntSight.RayTracing.Engine;

/// <summary>A sampler with no reflections, no antialiasing and no textures.</summary>
internal sealed class DraftSampler : SamplerBase, ISampler
{
    public DraftSampler() { }

    public override ISampler Clone() => new DraftSampler();

    void ISampler.Render(PixelStrip strip)
    {
        HitInfo info = new();
        ref TransPixel p = ref strip.FirstPixel;
        int w = camera.Width, row;
        while ((row = strip.NextRow()) >= 0)
        {
            int col = w;
            do
            {
                camera.Focus(row, --col);
                if (shapes.HitTest(cameraRay, double.MaxValue, ref info))
                {
                    Vector location = cameraRay[info.Time];
                    Vector reflection = cameraRay.Direction.Mirror(info.Normal);
                    IMaterial material = info.Material;
                    Pixel surface = material.DraftColor;
                    Pixel result = surface * ambient[location, info.Normal];
                    foreach (ILight light in lights)
                    {
                        float factor = light.DraftIntensity(location);
                        if (factor > 0.0F)
                            result = result.Add(material.Shade(location, info.Normal,
                                reflection, light, surface), factor);
                    }
                    p = result;
                }
                else
                    p = background.DraftColor(cameraRay);
                p = ref Add(ref p, 1);
            }
            while (col > 0);
        }
        strip.MaxDepth = 1;
        strip.SuperSamples = strip.Area;
    }
}

/// <summary>A sampler with textires but neither reflections nor antialiasing.</summary>
internal sealed class TexturedDraftSampler : SamplerBase, ISampler
{
    public TexturedDraftSampler() { }

    /// <summary>Create an independent copy of the sampler.</summary>
    /// <returns>The new sampler.</returns>
    public override ISampler Clone() => new TexturedDraftSampler();

    void ISampler.Render(PixelStrip strip)
    {
        HitInfo info = new();
        ref TransPixel p = ref strip.FirstPixel;
        int w = camera.Width, row;
        while ((row = strip.NextRow()) >= 0)
        {
            int col = w;
            do
            {
                camera.Focus(row, --col);
                if (shapes.HitTest(cameraRay, double.MaxValue, ref info))
                {
                    Vector location = cameraRay[info.Time];
                    Vector reflection = cameraRay.Direction.Mirror(info.Normal);
                    IMaterial material = info.Material;
                    material.GetColor(out Pixel surface, info.HitPoint);
                    Pixel result = surface * ambient[location, info.Normal];
                    foreach (ILight light in lights)
                    {
                        float factor = light.DraftIntensity(location);
                        if (factor > 0.0F)
                            result = result.Add(material.Shade(location, info.Normal,
                                reflection, light, surface), factor);
                    }
                    p = result;
                }
                else
                    p = background[cameraRay];
                p = ref Add(ref p, 1);
            }
            while (col > 0);
        }
        strip.MaxDepth = 1;
        strip.SuperSamples = strip.Area;
    }
}

/// <summary>A sampler that computes the distance to the camera.</summary>
internal sealed class SonarSampler : SamplerBase, ISampler
{
    /// <summary>Creates an instance of the Sonar mode sampler.</summary>
    public SonarSampler() { }

    /// <summary>Create an independent copy of the sampler.</summary>
    /// <returns>The new sampler.</returns>
    public override ISampler Clone() => new SonarSampler();

    /// <summary>Renders the scene as a depth map.</summary>
    /// <param name="strip">Band of the pixel map to render.</param>
    void ISampler.Render(PixelStrip strip)
    {
        Pixel bgColor = Properties.Settings.Default.SonarBackColor;
        Pixel nColor = Properties.Settings.Default.SonarForeColor;
        Pixel fColor = Properties.Settings.Default.SonarFarColor;

        double[,] distanceMap = new double[camera.Height, camera.Width];
        double min = double.MaxValue, max = double.MinValue;

        ref TransPixel p = ref strip.FirstPixel;
        HitInfo info = new();
        int w = camera.Width, row;
        while ((row = strip.NextRow()) >= 0)
        {
            int col = w;
            do
            {
                camera.Focus(row, --col);
                if (shapes.HitTest(cameraRay, double.MaxValue, ref info))
                {
                    double d = info.Time;
                    if (d < min) min = d;
                    else if (d > max) max = d;
                    distanceMap[row, col] = d;
                }
                else
                    distanceMap[row, col] = -1.0;
            }
            while (col > 0);
        }
        double range = max - min;
        if (range < Tolerance.Epsilon)
            min = double.MaxValue;
        Pixel delta = fColor - nColor;
        for (row = strip.FromRow; row <= strip.ToRow; row++)
        {
            int col = w;
            do
            {
                double d = distanceMap[row, --col];
                p = d < min ? bgColor : nColor.Lerp(delta, (float)((d - min) / range));
                p = ref Add(ref p, 1);
            }
            while (col > 0);
        }
        strip.MaxDepth = 1;
        strip.SuperSamples = strip.Area;
    }
}

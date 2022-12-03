namespace IntSight.RayTracing.Engine;

/// <summary>Common base class for all samplers.</summary>
public abstract class SamplerBase
{
    /// <summary>Scene to be rendered.</summary>
    protected IScene scene;
    protected ICamera camera;
    protected IShape shapes;
    protected IAmbient ambient;
    protected IBackground background;
    protected IMedia media;
    protected ILight[] lights;
    /// <summary>Optional photon map.</summary>
    protected PhotonMap photons;
    /// <summary>A direct reference to the ray managed by the camera.</summary>
    protected Ray cameraRay;
    /// <summary>Estimated number of traced rays by pixel.</summary>
    protected int oversampling = 1;

    /// <summary>Initializes the sampler before rendering.</summary>
    /// <param name="scene">Scene to render.</param>
    public void Initialize(IScene scene)
    {
        if (this.scene != scene)
        {
            this.scene = scene;
            camera = scene.Camera;
            shapes = scene.Root;
            lights = scene.Lights;
            ambient = scene.Ambient;
            background = scene.Background;
            media = scene.Media;
            photons = scene.Photons;
            cameraRay = camera.PrimaryRay;
            foreach (ILight light in lights)
                light.Initialize(scene);
            background.Initialize(scene);
            ambient.Initialize(scene);
            media?.Initialize(scene);
        }
        BaseLight.lastOccluder = null;
    }

    /// <summary>Gets the estimated number of rays by pixel.</summary>
    public int Oversampling => oversampling;

    /// <summary>Gets or sets the lens aperture, for focal samplers.</summary>
    /// <remarks>
    /// Some background objects use this parameter for detecting primary rays
    /// by checking their origins.
    /// </remarks>
    public double Aperture { get; protected set; }

    /// <summary>Create an independent copy of the sampler.</summary>
    /// <returns>The new sampler.</returns>
    public abstract ISampler Clone();

    /// <summary>Return jittered pairs of (y, x) values for a given grid size.</summary>
    /// <param name="width">Number of horizontal samples.</param>
    /// <param name="height">Number of vertical samples.</param>
    /// <returns>Random numbers between 0 and 1.</returns>
    public static IEnumerable<double> Grid(int width, int height, Random seed)
    {
        var pairs = new (int row, int col)[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                pairs[x, y] = (y, x);
        for (int times = 0; times < width; times++)
        {
            int i = seed.Next(width), j = seed.Next(width);
            if (i != j)
                for (int row = 0; row < height; row++)
                    (pairs[j, row], pairs[i, row]) = (pairs[i, row], pairs[j, row]);
        }
        for (int times = 0; times < height; times++)
        {
            int i = seed.Next(height), j = seed.Next(height);
            if (i != j)
                for (int col = 0; col < width; col++)
                    (pairs[col, j], pairs[col, i]) = (pairs[col, i], pairs[col, j]);
        }
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                (int row, int col) = pairs[x, y];
                yield return (y + (col + seed.NextDouble()) / height) / height;
                yield return (x + (row + seed.NextDouble()) / width) / width;
            }
    }
}

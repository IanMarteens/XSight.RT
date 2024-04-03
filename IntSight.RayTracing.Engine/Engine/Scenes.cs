using System.Reflection;
using System.Xml;
using Resources = IntSight.RayTracing.Engine.Properties.Resources;

namespace IntSight.RayTracing.Engine;

/// <summary>The one and only implementation of <c>IScene</c>.</summary>
public sealed class Scene : IScene
{
    /// <summary>Creates a scene given its components.</summary>
    /// <param name="title">Optional scene title.</param>
    /// <param name="sampler">The sampler, for collecting light rays.</param>
    /// <param name="camera">The camera, for mapping pixels to rays.</param>
    /// <param name="lights">Lights in the scene.</param>
    /// <param name="ambient">Omnidirectional ambient light.</param>
    /// <param name="background">The scene background.</param>
    /// <param name="media">An optional media.</param>
    /// <param name="root">The root shape.</param>
    public Scene(
        string title,
        ISampler sampler, ICamera camera,
        ILight[] lights, IAmbient ambient, IBackground background,
        IMedia media, IShape root)
        : this(title, sampler, camera, lights, ambient, background, media, root, true) { }

    /// <summary>Creates a scene given its components.</summary>
    /// <param name="title">Optional scene title.</param>
    /// <param name="sampler">The sampler, for collecting light rays.</param>
    /// <param name="camera">The camera, for mapping pixels to rays.</param>
    /// <param name="lights">Lights in the scene.</param>
    /// <param name="ambient">Omnidirectional ambient light.</param>
    /// <param name="background">The scene background.</param>
    /// <param name="media">An optional media.</param>
    /// <param name="root">The root shape.</param>
    /// <param name="optimize">True if the scene must be optimized.</param>
    private Scene(
        string title,
        ISampler sampler, ICamera camera,
        ILight[] lights, IAmbient ambient, IBackground background,
        IMedia media, IShape root, bool optimize)
    {
        Title = title ?? string.Empty;
        Sampler = sampler;
        Camera = camera.Simplify(sampler);
        Lights = lights;
        if (optimize)
        {
            int idx = Array.FindIndex(lights, lg => lg is Photons);
            if (idx >= 0)
            {
                var photonParameters = lights[idx] as Photons;
                var list = new List<ILight>(lights);
                list.RemoveAt(idx);
                Lights = [.. list];
                Photons = new PhotonMap(
                    photonParameters.TotalPhotons,
                    photonParameters.GatherCount);
            }
        }
        Ambient = ambient ?? new ConstantAmbient(0.0);
        Background = (background ?? new FlatBackground(0.0)).Simplify();
        Media = media;
        Root = root;
        // Optimize the scene tree.
        int optTime = Environment.TickCount;
        if (optimize)
            Root = Camera.CheckRoot(Root.Simplify().Substitute());
        Root.Initialize(
            scene: this,
            inCsg: false,
            inTransform: false);
        OptimizationTime = Environment.TickCount - optTime;
    }

    /// <summary>Gets the time in milliseconds spend in the last optimization.</summary>
    public int OptimizationTime { get; }

    #region IScene Members

    /// <summary>Gets the omnidirectional ambient light.</summary>
    public IAmbient Ambient { get; }
    /// <summary>Gets the scene background.</summary>
    public IBackground Background { get; }
    /// <summary>Gets the camera that maps pixels to light rays.</summary>
    public ICamera Camera { get; }
    /// <summary>Gets the array of lights in the scene.</summary>
    public ILight[] Lights { get; }
    /// <summary>Gets the optional atmospheric media.</summary>
    public IMedia Media { get; }
    /// <summary>Gets the optional photon map.</summary>
    public PhotonMap Photons { get; init; }
    /// <summary>Gets the sampler that will render and collect light rays.</summary>
    public ISampler Sampler { get; private set; }
    /// <summary>Gets the root shape in the scene.</summary>
    public IShape Root { get; }
    /// <summary>Gets the scene's title.</summary>
    public string Title { get; }

    /// <summary>Renders the scene using the contained components.</summary>
    /// <param name="mode">Render mode: optimal, draft, sonar, etc.</param>
    /// <param name="multithread">If true, strips are rendered in parallel.</param>
    /// <param name="listener">Event listener for progress information.</param>
    /// <returns>The generated image.</returns>
    PixelMap IScene.Render(RenderMode mode, bool multithread, IRenderListener listener)
    {
        CheckComponents();
        if (mode == RenderMode.Sonar)
            multithread = false;
        int coreCount = Environment.ProcessorCount;
        int stripCount = Math.Min(6 * coreCount, Camera.Height / 20);
        var pixelMap = new PixelMap(Camera.Width, Camera.Height,
            multithread ? stripCount : 1, mode != RenderMode.Sonar, this)
        { Title = Title };

        ISampler saveSampler = Sampler;
        Sampler = PickSampler();
        try
        {
            lastReport = Environment.TickCount;
            this.listener = listener;
            progressInfo = new(pixelMap);
            Photons?.Trace(this);
            if (multithread)
            {
                // Preallocate the first strip.
                PixelStrip strip = pixelMap.GetStrip();
                Task[] results = new Task[coreCount - 1];
                for (int i = 0; i < results.Length; i++)
                    results[i] = Task.Run(() => Render(Clone()));
                // Part of the scene is rendered in this same thread.
                Sampler.Initialize(this);
                Sampler.Render(strip);
                do
                {
                    strip = pixelMap.GetStrip();
                    if (strip == null)
                        break;
                    Sampler.Render(strip);
                }
                while (!progressInfo.CancellationPending);
                BaseLight.lastOccluder = null;

                Task.WaitAll(results);
            }
            else
            {
                Sampler.Initialize(this);
                Sampler.Render(pixelMap.GetStrip());
            }
            pixelMap.SuperSamples = pixelMap.TotalSuperSamples();
            Photons?.Preview(pixelMap, Camera);
            return pixelMap;
        }
        finally
        {
            Sampler = saveSampler;
        }

        /// <summary>Check if all mandatory components have been assigned.</summary>
        void CheckComponents()
        {
            if (Root == null)
                throw new RenderException(Resources.ErrorNoScene);
            if (Camera == null)
                throw new RenderException(Resources.ErrorNoCamera);
            if (Lights == null)
                throw new RenderException(Resources.ErrorNoLights);
        }

        /// <summary>Select the sampler corresponding to the render mode.</summary>
        /// <returns>A sampler instance.</returns>
        ISampler PickSampler() => mode switch
        {
            RenderMode.Sonar => new SonarSampler(),
            RenderMode.Draft => new DraftSampler(),
            RenderMode.TexturedDraft => new TexturedDraftSampler(),
            RenderMode.Basic => new BasicSampler(2, 1.0 / 255.0),
            RenderMode.GoodEnough => new AntialiasSampler(3, 0.001, 2),
            _ => Sampler
        };

        /// <summary>Renders the supplied scene on as many strips as possible.</summary>
        void Render(IScene scene)
        {
            scene.Sampler.Initialize(scene);
            do
            {
                PixelStrip strip = pixelMap.GetStrip();
                if (strip == null)
                    break;
                scene.Sampler.Render(strip);
            }
            while (!progressInfo.CancellationPending);
            BaseLight.lastOccluder = null;
        }

        /// <summary>Creates an independent copy of this scene.</summary>
        IScene Clone()
        {
            var lights = new ILight[Lights.Length];
            for (int i = 0; i < lights.Length; i++)
                lights[i] = Lights[i].Clone();
            return new Scene(
                title: Title,
                sampler: Sampler.Clone(),
                camera: Camera.Clone(),
                lights: lights,
                ambient: Ambient?.Clone(),
                background: Background?.Clone(),
                media: Media?.Clone(),
                root: Root.Clone(false),
                optimize: false)
            {
                Photons = Photons,
            };
        }
    }

    /// <summary>Minimum time interval in milliseconds between two progress events.</summary>
    private const int MIN_INTERVAL = 1650;

    private int lastReport, access;
    private IRenderListener listener;
    private RenderProgressInfo progressInfo;

    /// <summary>Decides whether progress is shown or not.</summary>
    /// <returns>True if rendering must go on; false, if it has been cancelled.</returns>
    internal bool ReportProgress()
    {
        int elapsed = Environment.TickCount;
        if (elapsed - lastReport > MIN_INTERVAL)
        {
            // The thread gives up if some other thread is showing progress.
            if (0 == Interlocked.Exchange(ref access, 1))
            {
                lastReport = elapsed;
                listener?.Progress(progressInfo);
                access = 0;
            }
            return !progressInfo.CancellationPending;
        }
        else
            return true;
    }

    /// <summary>Shows a preview when using the adaptive sampler.</summary>
    /// <param name="map">The rendered image.</param>
    /// <param name="from">Initial row to show.</param>
    /// <param name="to">Final row to show.</param>
    internal void Preview(PixelMap map, int from, int to)
    {
        if (0 == Interlocked.Exchange(ref access, 1))
        {
            listener?.Preview(map, from, to);
            access = 0;
        }
    }

    /// <summary>Saves a scene description to an XML writer.</summary>
    /// <param name="writer">XML writer.</param>
    public void Write(XmlWriter writer) => Writer.Write(writer, Root);

    /// <summary>Saves a scene description to an XML file.</summary>
    /// <param name="outputFileName">Output file path.</param>
    public void Write(string outputFileName)
    {
        using var writer = XmlWriter.Create(
            outputFileName: outputFileName,
            settings: new() { Indent = true, IndentChars = "\t" });
        Writer.Write(writer, Root);
        writer.Flush();
    }

    #endregion

    private static class Writer
    {
        private static PropertiesAttribute GetPropertiesAttribute(Type type)
        {
            var attributes = (PropertiesAttribute[])
                type.GetCustomAttributes(typeof(PropertiesAttribute), false);
            return attributes != null && attributes.Length == 1 ? attributes[0] : null;
        }

        private static ChildrenAttribute GetChildrenAttribute(Type type)
        {
            var attributes = (ChildrenAttribute[])
                type.GetCustomAttributes(typeof(ChildrenAttribute), false);
            return attributes != null && attributes.Length == 1 ? attributes[0] : null;
        }

        public static void Write(XmlWriter writer, object item)
        {
            if (item != null)
            {
                Type itemType = item.GetType();
                writer.WriteStartElement(itemType.Name.ToLowerInvariant());
                WriteProperty(writer, item, itemType, "bounds");
                PropertiesAttribute pAttrs = GetPropertiesAttribute(itemType);
                if (pAttrs != null)
                    foreach (string propertyName in pAttrs.Names)
                        WriteProperty(writer, item, itemType, propertyName);
                ChildrenAttribute cAttrs = GetChildrenAttribute(itemType);
                if (cAttrs != null)
                    foreach (string childName in cAttrs.Names)
                        WriteProperty(writer, item, itemType, childName);
                writer.WriteEndElement();
            }
        }

        private static void WriteValue(XmlWriter writer, string name, object value)
        {
            if (value != null)
            {
                Type type = value.GetType();
                if (type.IsArray)
                    foreach (object item in (object[])value)
                        Write(writer, item);
                else if (type == typeof(Vector))
                    ((Vector)value).WriteXmlAttribute(writer, name);
                else if (type == typeof(Matrix))
                    ((Matrix)value).WriteXmlAttribute(writer, name);
                else if (type == typeof(Pixel))
                    ((Pixel)value).WriteXmlAttribute(writer, name);
                else if (type == typeof(Bounds))
                    ((Bounds)value).WriteXmlAttribute(writer);
                else if (type.IsEnum)
                {
                    writer.WriteStartAttribute(name);
                    writer.WriteValue(value.ToString());
                    writer.WriteEndAttribute();
                }
                else
                {
                    PropertiesAttribute properties = GetPropertiesAttribute(type);
                    ChildrenAttribute children = GetChildrenAttribute(type);
                    if (properties != null || children != null)
                    {
                        writer.WriteStartElement(type.Name.ToLowerInvariant());
                        WriteProperty(writer, value, type, "bounds");
                        if (properties != null)
                            foreach (string propertyName in properties.Names)
                                WriteProperty(writer, value, type, propertyName);
                        if (children != null)
                            foreach (string childName in children.Names)
                                WriteProperty(writer, value, type, childName);
                        writer.WriteEndElement();
                    }
                    else
                    {
                        writer.WriteStartAttribute(name);
                        writer.WriteValue(value);
                        writer.WriteEndAttribute();
                    }
                }
            }
        }

        /// <summary>Writes the value of a property to an XML writer.</summary>
        /// <param name="writer">The XML writer.</param>
        /// <param name="item">The instance which the property belongs.</param>
        /// <param name="itemType">The type of the instance.</param>
        /// <param name="propertyName">The name of the property.</param>
        private static void WriteProperty(
            XmlWriter writer, object item, Type itemType, string propertyName)
        {
            const BindingFlags bf =
                BindingFlags.IgnoreCase | BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic;

            PropertyInfo propInfo = itemType.GetProperty(propertyName, bf);
            if (propInfo != null)
                WriteValue(writer, propertyName, propInfo.GetValue(item, null));
            else
            {
                FieldInfo fieldInfo = itemType.GetField(propertyName, bf);
                if (fieldInfo != null)
                    WriteValue(writer, propertyName, fieldInfo.GetValue(item));
            }
        }
    }
}

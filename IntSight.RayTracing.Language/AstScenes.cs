using IntSight.Parser;
using IntSight.RayTracing.Engine;
using System.Collections.Generic;
using Rsc = IntSight.RayTracing.Language.Properties.Resources;

namespace IntSight.RayTracing.Language
{
    public sealed class AstScene : IAstNode
    {
        private readonly List<IAstValue> lights = new();
        private readonly List<IAstValue> objects = new();
        private IScene scene;
        private bool rotateCamera;
        private bool keepCameraHeight;
        private double clockValue;

        public IAstValue Sampler { get; private set; }
        public IAstValue Camera { get; private set; }
        public List<IAstValue> Ambients { get; } = new();
        public IAstValue Background { get; private set; }
        public IAstValue Media { get; private set; }
        public string Title { get; private set; }

        /// <summary>Gets the source range taken by the scene.</summary>
        public SourceRange Position { get; set; }

        public IScene Scene => scene ?? CreateScene();

        public static AstScene SetTitle(
            object scene,
            object title,
            SourceRange titleRange,
            Errors errors)
        {
            var s = (AstScene)scene;
            if (s.Title != null)
                errors.Add(titleRange, Rsc.TitleAlreadyDefined);
            else
                s.Title = title?.ToString() ?? string.Empty;
            return s;
        }

        public static AstScene SetSampler(object scene, IAstValue sampler, Errors errors)
        {
            var s = (AstScene)scene;
            if (s.Sampler != null)
                errors.Add(sampler.Position, Rsc.SamplerAlreadyDefined);
            else
                s.Sampler = sampler;
            return s;
        }

        public static AstScene SetCamera(object scene, IAstValue camera, Errors errors)
        {
            var s = (AstScene)scene;
            if (s.Camera != null)
                errors.Add(camera.Position, Rsc.CameraAlreadyDefined);
            else
                s.Camera = camera;
            return s;
        }

        public static AstScene SetMedia(object scene, IAstValue media)
        {
            var s = (AstScene)scene;
            s.Media = media;
            return s;
        }

        public static AstScene SetAmbient(
            object scene, List<IAstValue> ambients, Errors errors)
        {
            var s = (AstScene)scene;
            for (int i = 0; i < ambients.Count; i++)
            {
                IAstValue ambient = ambients[i];
                if (!(ambient is AstObject))
                    if (ambient is AstColor || ambient is AstNumber)
                        ambients[i] = new AstObject(
                            ambient.Position, "ConstantAmbient", ambient);
                    else
                        errors.Add(ambient.Position, Rsc.AmbientInvalid);
            }
            s.Ambients.AddRange(ambients);
            return s;
        }

        public static AstScene SetBackground(
            object scene, IAstValue background, Errors errors)
        {
            var s = (AstScene)scene;
            if (s.Background != null)
                errors.Add(background.Position, Rsc.BackgroundAlreadyDefined);
            else if (background is AstObject)
                s.Background = background;
            else if (background is AstColor || background is AstNumber)
                s.Background = new AstObject(
                    background.Position, "FlatBackground", background);
            else
                errors.Add(background.Position, Rsc.BackgroundInvalid);
            return s;
        }

        public static AstScene SetLights(object scene, object lights)
        {
            var s = (AstScene)scene;
            s.lights.AddRange((List<IAstValue>)lights);
            return s;
        }

        public static AstScene SetObjects(object scene, object objects)
        {
            var s = (AstScene)scene;
            s.objects.AddRange((List<IAstValue>)objects);
            return s;
        }

        public AstScene Verify(Errors errors)
        {
            if (errors.Count == 0)
            {
                Background ??= new AstObject(SourceRange.Default, "FlatBackground",
                    new AstColor());
                Background.Verify(typeof(IBackground), errors);
                Media?.Verify(typeof(IMedia), errors);
                if (Camera == null)
                    errors.Add(SourceRange.Default, Rsc.SceneNoCamera);
                else
                    Camera.Verify(typeof(ICamera), errors);
                Sampler ??= new AstObject(SourceRange.Default, "AdaptiveSampler",
                    new AstNumber("25"), new AstNumber("0.0005"), new AstNumber("4"));
                Sampler.Verify(typeof(ISampler), errors);
                if (Ambients.Count == 0)
                    Ambients.Add(new AstObject(SourceRange.Default, "ConstantAmbient",
                        new AstNumber("0")));
                foreach (IAstValue obj in Ambients)
                    obj.Verify(typeof(IAmbient), errors);
                if (lights.Count == 0)
                    lights.Add(new AstObject(SourceRange.Default, "PointLight"));
                foreach (IAstValue obj in lights)
                    obj.Verify(typeof(ILight), errors);
                foreach (IAstValue obj in objects)
                    obj.Verify(typeof(IShape), errors);
            }
            return this;
        }

        public void RotateCamera(double clock, bool keepCameraHeight)
        {
            rotateCamera = true;
            this.keepCameraHeight = keepCameraHeight;
            clockValue = clock;
        }

        public PixelMap Evaluate(RenderMode mode, bool dualMode, IRenderListener listener) =>
            CreateScene().Render(mode, dualMode, listener);

        private IScene CreateScene()
        {
            var cam = (ICamera)Camera.Value;
            if (rotateCamera)
                cam = cam?.Rotate(360 * clockValue, keepCameraHeight);
            return scene = new Scene(
                title: Title,
                sampler: (ISampler)Sampler.Value,
                camera: cam,
                lights: lights.ConvertAll(v => (ILight)v.Value).ToArray(),
                ambient: Ambients.Count == 1 ?
                    (IAmbient)Ambients[0].Value :
                    new ComboAmbient(Ambients.ConvertAll(v => (IAmbient)v.Value).ToArray()),
                background: (IBackground)Background.Value,
                media: (IMedia)Media?.Value,
                root: new Union(objects.ConvertAll(v => (IShape)v.Value).ToArray()));
        }
    }
}
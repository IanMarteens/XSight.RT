using System.Diagnostics;
using System.Drawing;

namespace IntSight.RayTracing.Engine;

/// <summary>Contains methods for executing XSight standard benchmarks.</summary>
public static class Benchmark
{
    public struct BenchmarkId
    {
        public int Id { get; init; }
        public string Description { get; init; }
        public string Remarks { get; init; }
    }

    public static List<BenchmarkId> GetBenchmarks() =>
        new()
        {
            new BenchmarkId
            {
                Id = -1, Description = "All",
                Remarks = "Run all available benchmarks"
            },
            new BenchmarkId
            {
                Id = 0, Description = Properties.Resources.BenchmarkSphlakes,
                Remarks = "Adaptive sampling, spheric lights, spheric unions"
            },
            new BenchmarkId
            {
                Id = 1, Description = Properties.Resources.BenchmarkMeridians,
                Remarks = "Focal sampling, tori, spheric unions"
            },
            new BenchmarkId
            {
                Id = 2, Description = Properties.Resources.BenchmarkWater,
                Remarks = "Focal sampling, cylindrical camera, refraction"
            },
            new BenchmarkId
            {
                Id = 3, Description = "Blobs",
                Remarks = "Adaptive sampling, perspective camera, blobs"
            },
        };

    public static IScene GetScene(int benchmarkId, int downScale) =>
        benchmarkId switch
        {
            0 => CreateSphlakes(downScale),
            1 => CreateMeridians(downScale),
            2 => CreateWater(downScale),
            3 => CreateBlobs(downScale),
            _ => null,
        };

    public static int Run(int benchmarkId, bool useThreads, int downScale = 1)
    {
        IScene scene = GetScene(benchmarkId, downScale);
        if (scene != null)
        {
            Stopwatch watch = Stopwatch.StartNew();
            scene.Render(RenderMode.Normal, useThreads, null);
            watch.Stop();
            return (int)watch.ElapsedMilliseconds;
        }
        IScene scene1 = CreateSphlakes(downScale);
        IScene scene2 = CreateMeridians(downScale);
        IScene scene3 = CreateWater(downScale);
        IScene scene4 = CreateBlobs(downScale);
        Stopwatch watch1 = Stopwatch.StartNew();
        scene1.Render(RenderMode.Normal, useThreads, null);
        scene2.Render(RenderMode.Normal, useThreads, null);
        scene3.Render(RenderMode.Normal, useThreads, null);
        scene4.Render(RenderMode.Normal, useThreads, null);
        watch1.Stop();
        return (int)watch1.ElapsedMilliseconds;
    }

    public static Bitmap Render(int benchmarkId, int downScale = 1) =>
        GetScene(benchmarkId, downScale)?.Render(
            RenderMode.Normal, true, null).ToBitmap() ?? null;

    private static IShape Spin(this IShape shape, double x, double y, double z) =>
        new Rotate(shape, x, y, z);

    private static IShape Move(this IShape shape, double x, double y, double z) =>
        new Translate(shape, x, y, z);

    private static IScene CreateSphlakes(int downScale)
    {
        IShape shapes =
            SphlakePiece(25.00, 34.22, Color.MidnightBlue,
            SphlakePiece(9.26, 12.69, Color.RoyalBlue,
            SphlakePiece(3.43, 4.70, Color.Goldenrod,
            SphlakePiece(1.27, 1.74, Color.SlateGray,
            SphlakePiece(0.40, 0.55, Color.SeaShell,
            new Sphere(Vector.Null, 0.15, new Metal(Color.Lavender, 0.75, 0.90)))))));
        return new Scene(Properties.Resources.BenchmarkSphlakes,
            new AdaptiveSampler(25, 0.0001, 3, 6),
            new PerspectiveCamera(
                location: new(100, 120, 20), 
                target: new(110, 120, 100),
                up: Vector.YRay,
                angle: 85,
                width: 640 / downScale,
                height: 480 / downScale),
            new ILight[]
            {
                new PointLight(new(0, 200, -100), new(0.05F)),
                new SphericLight(new(110, -50, 80), 4, new(0.4F), 9),
            },
            new ConstantAmbient(0.1),
            new FlatBackground(Color.Black, new(0, 0, 0.2), new(0, 1, 0.5)),
            null,
            shapes.Spin(10, 20, -10).Move(110, 125, 100));
    }

    private static IScene CreateMeridians(int downScale) =>
        new Scene(Properties.Resources.BenchmarkMeridians,
            new FocalSampler(12, 0.0001, 0.05, 6),
            new PerspectiveCamera(
                location: new(1.2, 1.2, -10),
                target: Vector.Null,
                up: Vector.YRay,
                angle: 65,
                width: 640 / downScale,
                height: 480 / downScale),
            new ILight[] { new PointLight() },
            new ConstantAmbient(0.1),
            new FlatBackground(Color.White),
            null,
            MeridianRoot());

    private static IScene CreateWater(int downScale) =>
        new Scene(Properties.Resources.BenchmarkWater,
            new FocalSampler(90, 0.0001, 0.10, 4),
            new CylindricalCamera(
                new(0, 0, -8), Vector.Null, Vector.YRay, 60, 
                640 / downScale, 480 / downScale),
            new ILight[] { new PointLight() },
            new ConstantAmbient(0.1),
            new SkyBackground(Color.RoyalBlue, Color.White),
            null,
            new Union(
                new Shadowless(
                    new Difference(
                        new Cylinder(new(0, -2, 0), new Vector(0, +2, 0), 2.0,
                            new Glass(1.667)),
                        new Cylinder(new(0, +1, 0), new Vector(0, +3, 0), 1.9,
                            new Glass(1.667)))),
                new Shadowless(
                    new Cylinder(new(0, -1.75, 0), new Vector(0, 1, 0), 0.9,
                        new Glass(1.3333))),
                new Cylinder(new(-1, -1.7, 0), new Vector(1.95, 2.45, 0), 0.1,
                    new Plastic(Color.White))));

    private static IScene CreateBlobs(int downScale) =>
        new Scene("Blobs",
            new AdaptiveSampler(6, 0.001, 2, 6),
            new PerspectiveCamera(
                new(0, 0, -5), Vector.Null, Vector.YRay, 40,
                640 / downScale, 480 / downScale),
            new ILight[]
            {
                new PointLight(-15, 30, -25),
                new PointLight(+15, 30, -25)
            },
            new ComboAmbient(
                new ConstantAmbient(0.1),
                new LocalAmbient(Vector.Null, new(0.7F), 0.4)),
            new FlatBackground(new Pixel(0, 0, 0.2)),
            null,
            new Blob(
                new IBlobItem[]
                {
                    new Ball(new(0.75, 0, 0), 1),
                    new Ball(new(-0.375, +0.64952, 0), 1),
                    new Ball(new(-0.375, -0.64952, 0), 1)
                },
                0.6,
                new Metal(Color.Gold, 0, 0.1, 0.1, 1, 12)).Spin(-15, 0, 0).Spin(0, 45, 0));

    private static IShape SphlakePiece(double rad, double radp, Color color, IShape sat) =>
        new Union(
            new Sphere(Vector.Null, rad, new Metal(color, 0.75, 0.90)),
            sat.Clone(true).Spin(0, 0, +90).Move(-radp, 0, 0),
            sat.Clone(true).Spin(0, 0, -90).Move(+radp, 0, 0),
            sat.Clone(true).Spin(0, 0, 0).Move(0, +radp, 0),
            sat.Clone(true).Spin(0, 0, 180).Move(0, -radp, 0),
            sat.Clone(true).Spin(-90, 0, 0).Move(0, 0, -radp),
            sat.Clone(true).Spin(+90, 0, 0).Move(0, 0, +radp));

    private static IShape MeridianBase(double r0, double r1, IMaterial m) =>
        new Union(
            new Torus(Vector.Null, r0, r1, m.Clone(true)),
            new Torus(Vector.Null, r0, r1, m.Clone(true)).Spin(90, 0, 0),
            new Torus(Vector.Null, r0, r1, m.Clone(true)).Spin(0, 0, 90));

    private static IShape MeridianEnsemble(double amt, IShape shape) =>
        new Union(
            shape.Clone(true).Move(+amt, 0, 0),
            shape.Clone(true).Move(-amt, 0, 0),
            shape.Clone(true).Move(0, +amt, 0),
            shape.Clone(true).Move(0, -amt, 0),
            shape.Clone(true).Move(0, 0, +amt),
            shape.Clone(true).Move(0, 0, -amt));

    private static IShape MeridianRoot() =>
        new Union(
            MeridianBase(4.1, 0.1,
                new Metal(Color.Silver, 0.06, 0.08, 0.9, 0.6, 10)),
            MeridianEnsemble(2,
                new Union(
                    MeridianBase(2, 0.05,
                        new Metal(Color.Gold, 0.05, 0.25, 0.9, 0.6, 12)),
                    MeridianEnsemble(1,
                        MeridianBase(1, 0.025,
                            new Metal(Color.Gold, 0.05, 0.25, 0.9, 0.6, 12))))));
}

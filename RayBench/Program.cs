#undef MAINTEST
#undef CAMTEST
#define SCENETEST

using IntSight.RayTracing.Engine;
using RayBench;
using System;
using System.Diagnostics;
using System.Drawing;

#if MAINTEST
MainTest();
#endif
#if CAMTEST
CamTest();
#endif
#if SCENETEST
SceneTest();
#endif

#if MAINTEST
static void MainTest()
{
    Console.WriteLine("Warming...");
    for (int i = 0; i < 20; i++) Benchmark.Run(0, true);
    Console.WriteLine("XSight RT benchmark running...");
    int renderTime = Benchmark.Run(0, true);
    Console.WriteLine($"Render time: {renderTime:N0} msecs.");
}
#endif

#if CAMTEST
static void CamTest()
{
    var test = new TestCam();
    Console.WriteLine("Warming camera...");
    for (int i = 0; i < 1000; i++)
        test.Run();
    Console.WriteLine("Running camera test...");
    var sw = Stopwatch.StartNew();
    int total = test.Run();
    for (int i = 0; i < 999; i++)
        test.Run();
    sw.Stop();
    Console.WriteLine($"Camera: {sw.ElapsedMilliseconds / 1000d:N3} msecs.");
    Console.WriteLine(total);
}
#endif

#if SCENETEST
static void SceneTest()
{
    IScene scene = new Scene("Sphlakes",
        new BasicSampler(25, 0.0001),
        new PerspectiveCamera(
            location: new(100, 120, 20),
            target: new(110, 120, 100),
            up: Vector.YRay,
            angle: 85,
            width: 640,
            height: 480),
        [
            new PointLight(new(0, 200, -100), new(0.05F)),
            new PointLight(new(110, -50, 80), new(0.4F)),
        ],
        new ConstantAmbient(0.1),
        new FlatBackground(Color.Black, new(0, 0, 0.2), new(0, 1, 0.5)),
        null,
        new Sphere(new(110, 125, 100), 25, new Plastic(Color.White)));
    for (int i = 0; i < 200; i++)
        scene.Render(RenderMode.Normal, false, null);
    var sw = Stopwatch.StartNew();
    scene.Render(RenderMode.Normal, false, null);
    sw.Stop();
    Console.WriteLine($"Scene: {sw.ElapsedMilliseconds / 1000d:N3} msecs.");
}
#endif

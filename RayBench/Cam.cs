using IntSight.RayTracing.Engine;

namespace RayBench
{
    public class TestCam
    {
        private readonly ICamera cam;
        private readonly IShape shape;
        private readonly Bounds bounds;

        public TestCam()
        {
            cam = new VerticalAlignedCamera(
                location: new(100, 120, 20),
                target: new(110, 120, 100),
                //up: Vector.YRay,
                angle: 85,
                width: 640,
                height: 480);
            shape = new Sphere(new Vector(110, 125, 100), 25, new Plastic(Pixel.White)).Substitute();
            bounds = Bounds.FromSphere(new Vector(110 - 20, 125 + 20, 100 - 150), 25);
            //bounds = Bounds.FromSphere(new Vector(110, 125, 100), 25);
        }

        public int Run()
        {
            var camera = cam;
            int w = camera.Width, h = camera.Height;
            int total = 0;
            Ray r = camera.PrimaryRay;
            //Ray r1 = new();
            //HitInfo hit = default;
            for (int row = 0; row < h; row++)
            {
                int col = w;
                do
                {
                    camera.FocusRow(row);
                    camera.FocusColumn(--col);
                    camera.GetRay(0.0, 0.0);
                    //r1.FromTo(r.Origin, r.Origin + r.Direction * 2000);
                    //if (shape.ShadowTest(r1))
                    //if (shape.HitTest(r, double.MaxValue, ref hit))
                    if (bounds.Intersects(r, double.MaxValue))
                    {
                        total++;
                    }
                }
                while (col > 0);
            }
            return total;
        }

        public Ray GetRay(int row, int col, bool focal)
        {
            cam.FocusRow(row);
            cam.FocusColumn(col);
            if (focal)
                cam.GetRay(0, 0, 0, 0);
            else
                cam.GetRay(0.0, 0.0);
            return cam.PrimaryRay;
        }

        public bool TestRay(int row, int col)
        {
            cam.FocusRow(row);
            cam.FocusColumn(col);
            cam.GetRay(0, 0, 0, 0);
            Ray r = cam.PrimaryRay;
            Ray r1 = new();
            r1.FromTo(r.Origin, r.Origin + r.Direction * 2000);
            return shape.ShadowTest(r1);
        }
    }
}

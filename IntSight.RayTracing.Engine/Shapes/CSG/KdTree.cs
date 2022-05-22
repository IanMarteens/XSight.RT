namespace IntSight.RayTracing.Engine;

public sealed class KdTree : Shape
{
    public sealed class KdNode
    {
        public Axis Axis;
        public double Split;
    }
}

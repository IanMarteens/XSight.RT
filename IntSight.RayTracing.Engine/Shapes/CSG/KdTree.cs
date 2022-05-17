using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntSight.RayTracing.Engine
{
    public sealed class KdTree : Shape
    {
        public sealed class KdNode
        {
            public Axis Axis;
            public double Split;
        }
    }
}

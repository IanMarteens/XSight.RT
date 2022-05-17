using static System.Runtime.CompilerServices.Unsafe;

namespace IntSight.RayTracing.Engine
{
    /// <summary>A band inside a <see cref="PixelMap"/>.</summary>
    public sealed class PixelStrip
    {
        private readonly Scene scene;
        internal int fromRow, toRow;
        internal int nextDraw, nextRow;

        internal PixelStrip(PixelMap pixelMap, IScene scene, int fromRow, int toRow)
        {
            PixelMap = pixelMap;
            this.fromRow = fromRow;
            this.toRow = toRow;
            nextRow = fromRow;
            nextDraw = fromRow;
            this.scene = (Scene)scene;
        }

        /// <summary>Gets the underlying pixel map.</summary>
        public PixelMap PixelMap { get; }
        /// <summary>Gets the index of the lowest row in the strip.</summary>
        public int FromRow => fromRow;
        /// <summary>Gets the index of the highest row in the strip.</summary>
        public int ToRow => toRow;

        /// <summary>Gets the width of the strip.</summary>
        public int Width => PixelMap.Width;
        /// <summary>Gets the height of the strip.</summary>
        public int Height => toRow - fromRow + 1;
        /// <summary>Gets the number of pixels in the strip.</summary>
        public int Area => PixelMap.Width * (toRow - fromRow + 1);
        /// <summary>References the first pixel in the pixel map.</summary>
        public ref TransPixel FirstPixel => ref PixelMap.pixs[fromRow * PixelMap.Width];

        /// <summary>Highest number of bounces for a ray in the scene.</summary>
        public int MaxDepth { get; internal set; }
        /// <summary>Total number of samples taken for the strip.</summary>
        public int SuperSamples { get; internal set; }

        /// <summary>Gets the next row to render.</summary>
        /// <returns>A row number from the area controlled by the strip.</returns>
        public int NextRow()
        {
            if (nextRow <= toRow)
            {
                nextRow++;
                if (scene.ReportProgress())
                    return nextRow - 1;
            }
            else
                nextRow = toRow + 2;
            return -1;
        }

        private static TransPixel Whiter(in TransPixel c) =>
            TransPixel.FromArgb(
                unchecked((byte)((255 + c.R) / 2)),
                unchecked((byte)((255 + c.G) / 2)),
                unchecked((byte)((255 + c.B) / 2)));

        public void Preview(TransPixel[,] matrix)
        {
            ref TransPixel p = ref FirstPixel;
            int index = 0;
            for (int row = 0; row <= toRow - fromRow; row++)
                for (int col = PixelMap.Width - 1; col >= 0; col--)
                    Add(ref p, index++) = Whiter(matrix[row, col]);
            scene.Preview(PixelMap, fromRow, toRow);
        }
    }
}

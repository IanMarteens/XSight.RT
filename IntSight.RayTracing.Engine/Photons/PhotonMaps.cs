using static System.Runtime.CompilerServices.Unsafe;

namespace IntSight.RayTracing.Engine;

/// <summary>Implements a container for traced photons.</summary>
public sealed partial class PhotonMap
{
    /// <summary>Actual number of stored photons.</summary>
    private int count;
    /// <summary>Index of the first non-scaled photon.</summary>
    private int prevScale;
    /// <summary>The list of stored photons.</summary>
    private readonly Photon[] photons;
    /// <summary>Lower bounds of the volume enclosing all stored photons.</summary>
    private double x0, y0, z0;
    /// <summary>Upper bounds of the volume enclosing all stored photons.</summary>
    private double x1, y1, z1;

    /// <summary>Creates a photon map.</summary>
    /// <param name="capacity">Capacity of the photon map.</param>
    /// <param name="gatherCount">Number of photons to be gathered.</param>
    public PhotonMap(int capacity, int gatherCount)
    {
        Capacity = capacity;
        GatherCount = gatherCount;
        photons = new Photon[capacity];
        x0 = y0 = z0 = double.PositiveInfinity;
        x1 = y1 = z1 = double.NegativeInfinity;
    }

    /// <summary>Maximum number of photons to be stored.</summary>
    public int Capacity { get; }

    /// <summary>Maximum number of photons to be gathered.</summary>
    public int GatherCount { get; }

    public int Bounces { get; } = 5;

    /// <summary>Fills this photon map for a given scene.</summary>
    /// <param name="scene">Scene to be light traced.</param>
    public void Trace(IScene scene) =>
        // Creates a kd-tree from the raw array of photons.
        new PhotonTracer(this).Trace(scene).Sort(1, count - 1);

    public PhotonSet CreateGatherer() =>
        new(photons, count, GatherCount, 1F);

    /// <summary>Stores a single photon at the end of the array.</summary>
    /// <param name="position">Hit point.</param>
    /// <param name="power">Photon's power.</param>
    /// <param name="direction">Incoming direction.</param>
    private void Store(in Vector position, in Pixel power, in Vector direction)
    {
        if (count < Capacity)
        {
            // Update global bounds.
            if (count == 0)
                (x0, y0, z0) = (x1, y1, z1) = position;
            else
            {
                if (position.X < x0) x0 = position.X;
                else if (position.X > x1) x1 = position.X;
                if (position.Y < y0) y0 = position.Y;
                else if (position.Y > y1) y1 = position.Y;
                if (position.Z < z0) z0 = position.Z;
                else if (position.Z > z1) z1 = position.Z;
            }
            // Add to the raw list.
            photons[count++] = new(position, power, direction);
        }
    }

    /// <summary>Scales the power of all photons emitted from a given source.</summary>
    /// <param name="factor">Attenuation factor.</param>
    private void ScalePhotonPower(float factor)
    {
        if (count > prevScale)
        {
            ref Photon rph = ref photons[0];
            for (int i = prevScale; i < count; i++)
                Add(ref rph, i).Scale(factor);
            prevScale = count;
        }
    }

    /// <summary>Partially sorts and splits an array segment around the X median.</summary>
    /// <param name="left">First index of segment.</param>
    /// <param name="right">Final index of segment.</param>
    /// <param name="middle">The index of the median.</param>
    private void HalfSortX(int left, int right, int middle)
    {
        if (left < right)
        {
            int j = (left + right) / 2;
            Photon temp = photons[j];
            photons[j] = photons[left + 1];
            photons[left + 1] = temp;
            if (photons[left + 1].x > photons[right].x)
            {
                temp = photons[left + 1];
                photons[left + 1] = photons[right];
                photons[right] = temp;
            }
            if (photons[left].x > photons[right].x)
            {
                temp = photons[left];
                photons[left] = photons[right];
                photons[right] = temp;
            }
            if (photons[left + 1].x > photons[left].x)
            {
                temp = photons[left + 1];
                photons[left + 1] = photons[left];
                photons[left] = temp;
            }
            j = left + 1;
            int k = right;
            float v = photons[left].x;
            while (j <= k)
            {
                for (j++; j <= right && photons[j].x < v; j++) ;
                for (k--; k >= left && photons[k].x > v; k--) ;
                if (j < k)
                {
                    temp = photons[j]; photons[j] = photons[k]; photons[k] = temp;
                }
            }
            temp = photons[left]; photons[left] = photons[k]; photons[k] = temp;
            if (k - left > 0 && middle >= left && middle < k)
                HalfSortX(left, k - 1, middle);
            else if (right - k > 0 && middle > k && middle <= right)
                HalfSortX(k + 1, right, middle);
        }
    }

    /// <summary>Partially sorts and splits an array segment around the Y median.</summary>
    /// <param name="left">First index of segment.</param>
    /// <param name="right">Final index of segment.</param>
    /// <param name="middle">The index of the median.</param>
    private void HalfSortY(int left, int right, int middle)
    {
        if (left < right)
        {
            int j = (left + right) / 2;
            Photon temp = photons[j];
            photons[j] = photons[left + 1];
            photons[left + 1] = temp;
            if (photons[left + 1].y > photons[right].y)
            {
                temp = photons[left + 1];
                photons[left + 1] = photons[right];
                photons[right] = temp;
            }
            if (photons[left].y > photons[right].y)
            {
                temp = photons[left];
                photons[left] = photons[right];
                photons[right] = temp;
            }
            if (photons[left + 1].y > photons[left].y)
            {
                temp = photons[left + 1];
                photons[left + 1] = photons[left];
                photons[left] = temp;
            }
            j = left + 1;
            int k = right;
            float v = photons[left].y;
            while (j <= k)
            {
                for (j++; j <= right && photons[j].y < v; j++) ;
                for (k--; k >= left && photons[k].y > v; k--) ;
                if (j < k)
                {
                    temp = photons[j]; photons[j] = photons[k]; photons[k] = temp;
                }
            }
            temp = photons[left]; photons[left] = photons[k]; photons[k] = temp;
            if (k - left > 0 && middle >= left && middle < k)
                HalfSortY(left, k - 1, middle);
            else if (right - k > 0 && middle > k && middle <= right)
                HalfSortY(k + 1, right, middle);
        }
    }

    /// <summary>Partially sorts and splits an array segment around the Z median.</summary>
    /// <param name="left">First index of segment.</param>
    /// <param name="right">Final index of segment.</param>
    /// <param name="middle">The index of the median.</param>
    private void HalfSortZ(int left, int right, int middle)
    {
        if (left < right)
        {
            int j = (left + right) / 2;
            Photon temp = photons[j];
            photons[j] = photons[left + 1];
            photons[left + 1] = temp;
            if (photons[left + 1].z > photons[right].z)
            {
                temp = photons[left + 1];
                photons[left + 1] = photons[right];
                photons[right] = temp;
            }
            if (photons[left].z > photons[right].z)
            {
                temp = photons[left];
                photons[left] = photons[right];
                photons[right] = temp;
            }
            if (photons[left + 1].z > photons[left].z)
            {
                temp = photons[left + 1];
                photons[left + 1] = photons[left];
                photons[left] = temp;
            }
            j = left + 1;
            int k = right;
            float v = photons[left].z;
            while (j <= k)
            {
                for (j++; j <= right && photons[j].z < v; j++) ;
                for (k--; k >= left && photons[k].z > v; k--) ;
                if (j < k)
                {
                    temp = photons[j]; photons[j] = photons[k]; photons[k] = temp;
                }
            }
            temp = photons[left]; photons[left] = photons[k]; photons[k] = temp;
            if (k - left > 0 && middle >= left && middle < k)
                HalfSortZ(left, k - 1, middle);
            else if (right - k > 0 && middle > k && middle <= right)
                HalfSortZ(k + 1, right, middle);
        }
    }

    /// <summary>Sorts and splits a segment of the photon array.</summary>
    /// <param name="start">Lowest index of the segment.</param>
    /// <param name="end">Highest index of the segment.</param>
    private void Sort(int start, int end)
    {
        if (start < end)
        {
            int middle = (start + end) / 2;
            double tmp;
            switch (GetDominantAxis())
            {
                case Axis.X:
                    if (end - start >= 2)
                        HalfSortX(start, end, middle);
                    photons[middle].axis = Axis.X;
                    tmp = x1; x1 = photons[middle].x;
                    Sort(start, middle - 1);
                    x1 = tmp; tmp = x0; x0 = photons[middle].x;
                    Sort(middle + 1, end);
                    x0 = tmp;
                    break;
                case Axis.Y:
                    if (end - start >= 2)
                        HalfSortY(start, end, middle);
                    photons[middle].axis = Axis.Y;
                    tmp = y1; y1 = photons[middle].y;
                    Sort(start, middle - 1);
                    y1 = tmp; tmp = y0; y0 = photons[middle].y;
                    Sort(middle + 1, end);
                    y0 = tmp;
                    break;
                default:
                    if (end - start >= 2)
                        HalfSortZ(start, end, middle);
                    photons[middle].axis = Axis.Z;
                    tmp = z1; z1 = photons[middle].z;
                    Sort(start, middle - 1);
                    z1 = tmp; tmp = z0; z0 = photons[middle].z;
                    Sort(middle + 1, end);
                    z0 = tmp;
                    break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Axis GetDominantAxis()
    {
        double w = x1 - x0;
        double w1 = y1 - y0;
        Axis result;
        if (w1 > w) { result = Axis.Y; w = w1; } else result = Axis.X;
        return (z1 - z0 > w) ? Axis.Z : result;
    }

    internal void Preview(PixelMap pixels, ICamera camera)
    {
        Ray ray = new();
        Pixel red = new(1.0, 0.0, 0.0);
        Vector lookAt = camera.Target - camera.Location;
        for (int i = 0; i < count; i++)
        {
            Photon p = photons[i];
            Vector pos = new(p.x, p.y, p.z);
            ray.Direction = ray.FromTo(camera.Location, pos).Direction.Norm();
            if (lookAt * ray.Direction >= 0)
            {
                camera.GetRayCoordinates(ray, out int row, out int col);
                col = camera.Width - col;
                if (row >= 0 && row < camera.Height && col >= 0 && col < camera.Width)
                    pixels[row, col] = red.Lerp(pixels[row, col] - red,
                        (float)p.Power.GrayLevel).ToTransPixel(255);
            }
        }
    }
}
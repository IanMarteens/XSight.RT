namespace IntSight.RayTracing.Engine;

/// <summary>A noise generator based on Voronoi diagrams.</summary>
public sealed class CrackleNoise
{
    private static class HashFactory
    {
        private static readonly Dictionary<int, short[]> cache = [];

        public static short[] Create(int seed)
        {
            lock (cache)
            {
                if (seed == 0 || !cache.TryGetValue(seed, out short[] table))
                {
                    table = new short[8192];
                    for (short i = 0; i < table.Length; i++)
                        table[i] = i;
                    Random rnd = seed == 0 ? new Random() : new Random(seed);
                    for (int i = 0; i < table.Length; i++)
                    {
                        int lo = rnd.Next(table.Length);
                        int hi = rnd.Next(table.Length);
                        if (lo != hi)
                        {
                            (table[hi], table[lo]) = (table[lo], table[hi]);
                        }
                    }
                }
                return table;
            }
        }
    }

    private static readonly bool[] validCubelets = CreateCubelets();
    private readonly short[] table;

    //private const int firstValid = 25 * (-2 + 2) + 5 * (-1 + 2) + (-1 + 2);
    //private const int secondValid = 25 * (-2 + 2) + 5 * (-1 + 2) + (0 + 2);
    //private const int thirdValid = 25 * (-2 + 2) + 5 * (-1 + 2) + (1 + 2);
    private const int fourthValid = 25 * (-2 + 2) + 5 * (0 + 2) + (-1 + 2);
    private const int lastValid = 25 * (2 + 2) + 5 * (1 + 2) + (1 + 2);

    private static bool[] CreateCubelets()
    {
        bool[] result = new bool[lastValid + 1];
        int idx = 0;
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
                for (int dz = -2; dz <= 2; dz++)
                {
                    int count = 0;
                    if (dx == -2 || dx == 2) count++;
                    if (dy == -2 || dy == 2) count++;
                    if (dz == -2 || dz == 2) count++;
                    result[idx++] = count <= 1;
                    if (idx > lastValid)
                        return result;
                }
        return result;
    }

    private readonly CrackleNoise sidekick;
    private readonly Vector[] cubelets = new Vector[lastValid + 1];
    private readonly Vector form;
    private Vector firstCubelet;
    private Vector secondCubelet;
    private Vector thirdCubelet;
    private int lastSeed = -1;

    public CrackleNoise(int seed, Vector form)
    {
        this.form = form;
        table = HashFactory.Create(seed);
        sidekick = new(this);
    }

    public CrackleNoise(Vector form)
        : this(0, form) { }

    public CrackleNoise(int seed)
        : this(seed, new Vector(-1, 1, 0)) { }

    public CrackleNoise()
        : this(0, new Vector(-1, 1, 0)) { }

    private CrackleNoise(CrackleNoise source)
    {
        form = source.form;
        table = source.table;
    }

    public double this[double x, double y, double z]
    {
        get
        {
            int ix = (int)Math.Floor(x - Tolerance.Epsilon);
            int iy = (int)Math.Floor(y - Tolerance.Epsilon);
            int iz = (int)Math.Floor(z - Tolerance.Epsilon);
            int seed = table[table[table[ix & 0x0FFF] ^ (iy & 0x0FFF)] ^ (iz & 0x0FFF)];
            if (seed != lastSeed)
            {
                PickVector(ix - 2, iy - 1, iz - 1, ref firstCubelet);
                PickVector(ix - 2, iy - 1, iz, ref secondCubelet);
                PickVector(ix - 2, iy - 1, iz + 1, ref thirdCubelet);
                for (int idx = fourthValid; idx < validCubelets.Length; idx++)
                    if (validCubelets[idx])
                        PickVector(ix, iy, iz, idx, ref cubelets[idx]);
                lastSeed = seed;
            }
            double min1 = firstCubelet.SquaredDistance(x, y, z);
            double min2 = secondCubelet.SquaredDistance(x, y, z);
            if (min1 > min2)
            {
                (min1, min2) = (min2, min1);
            }
            double d = thirdCubelet.SquaredDistance(x, y, z);
            if (d < min1)
            {
                min2 = min1; min1 = d;
            }
            else if (d < min2)
                min2 = d;
            for (int idx = fourthValid; idx < validCubelets.Length; idx++)
                if (validCubelets[idx])
                    if ((d = cubelets[idx].SquaredDistance(x, y, z)) < min1)
                    {
                        min2 = min1; min1 = d;
                    }
                    else if (d < min2)
                    {
                        min2 = d;
                    }
            return (min1 = form.X * Math.Sqrt(min1) + form.Y * Math.Sqrt(min2)) < 0.0
                ? 0.0
                : min1 > 1.0 ? 1.0 : min1;
        }
    }

    public double Bubbles(double x, double y, double z)
    {
        int ix = (int)Math.Floor(x - Tolerance.Epsilon);
        int iy = (int)Math.Floor(y - Tolerance.Epsilon);
        int iz = (int)Math.Floor(z - Tolerance.Epsilon);
        int seed = table[table[table[ix & 0x0FFF] ^ (iy & 0x0FFF)] ^ (iz & 0x0FFF)];
        if (seed != lastSeed)
        {
            PickVector(ix - 2, iy - 1, iz - 1, ref firstCubelet);
            PickVector(ix - 2, iy - 1, iz, ref secondCubelet);
            PickVector(ix - 2, iy - 1, iz + 1, ref thirdCubelet);
            for (int idx = fourthValid; idx < validCubelets.Length; idx++)
                if (validCubelets[idx])
                    PickVector(ix, iy, iz, idx, ref cubelets[idx]);
            lastSeed = seed;
        }
        double min = firstCubelet.SquaredDistance(x, y, z);
        double d = secondCubelet.SquaredDistance(x, y, z);
        if (d < min) min = d;
        if ((d = thirdCubelet.SquaredDistance(x, y, z)) < min)
            min = d;
        for (int idx = fourthValid; idx < validCubelets.Length; idx++)
            if (validCubelets[idx])
                if ((d = cubelets[idx].SquaredDistance(x, y, z)) < min)
                    min = d;
        return min > 1.0 ? 1.0 : Math.Sqrt(min);
    }

    public double Facets(double x, double y, double z)
    {
        int ix = (int)Math.Floor(x - Tolerance.Epsilon);
        int iy = (int)Math.Floor(y - Tolerance.Epsilon);
        int iz = (int)Math.Floor(z - Tolerance.Epsilon);
        int seed = table[table[table[ix & 0x0FFF] ^ (iy & 0x0FFF)] ^ (iz & 0x0FFF)];
        if (seed != lastSeed)
        {
            PickVector(ix - 2, iy - 1, iz - 1, ref firstCubelet);
            PickVector(ix - 2, iy - 1, iz, ref secondCubelet);
            PickVector(ix - 2, iy - 1, iz + 1, ref thirdCubelet);
            for (int idx = fourthValid; idx < validCubelets.Length; idx++)
                if (validCubelets[idx])
                    PickVector(ix, iy, iz, idx, ref cubelets[idx]);
            lastSeed = seed;
        }
        double min = firstCubelet.ManhattanDistance(x, y, z);
        double d = secondCubelet.ManhattanDistance(x, y, z);
        if (d < min) min = d;
        if ((d = thirdCubelet.ManhattanDistance(x, y, z)) < min)
            min = d;
        for (int idx = fourthValid; idx < validCubelets.Length; idx++)
            if (validCubelets[idx])
                if ((d = cubelets[idx].ManhattanDistance(x, y, z)) < min)
                    min = d;
        return min > 1.0 ? 1.0 : min;
    }

    /// <summary>Turbulent crackle noise function.</summary>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="z">Z position.</param>
    /// <param name="depth">Turbulence.</param>
    /// <returns>A random value between 0 and 1.</returns>
    public double this[double x, double y, double z, short depth]
    {
        get
        {
            // I'm unrolling the loop inline.
            switch (depth)
            {
                case 0:
                case 1:
                    return this[x, y, z];
                case 2:
                    return (this[x, y, z] + sidekick[x + x, y + y, z + z] * 0.5) * 0.6666;
            }
            double result = this[x, y, z];
            result += sidekick[x += x, y += y, z += z] * 0.5;
            depth -= 2;
            double w = 2.0;
            do
                result += sidekick[x += x, y += y, z += z] / (w += w);
            while (--depth > 0);
            return w * result / (w + w - 1);
        }
    }

    /// <summary>Turbulent bubble noise function.</summary>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="z">Z position.</param>
    /// <param name="depth">Turbulence.</param>
    /// <returns>A random value between 0 and 1.</returns>
    public double Bubbles(double x, double y, double z, short depth)
    {
        // I'm unrolling the loop inline.
        switch (depth)
        {
            case 0:
            case 1:
                return Bubbles(x, y, z);
            case 2:
                return (Bubbles(x, y, z) + sidekick.Bubbles(x + x, y + y, z + z) * 0.5) * 0.6666;
        }
        double result = Bubbles(x, y, z);
        result += sidekick.Bubbles(x += x, y += y, z += z) * 0.5;
        depth -= 2;
        double w = 2.0;
        do
            result += sidekick.Bubbles(x += x, y += y, z += z) / (w += w);
        while (--depth > 0);
        return w * result / (w + w - 1);
    }

    public double Facets(double x, double y, double z, short depth)
    {
        // I'm unrolling the loop inline.
        switch (depth)
        {
            case 0:
            case 1:
                return Facets(x, y, z);
            case 2:
                return (Facets(x, y, z) + sidekick.Facets(x + x, y + y, z + z) * 0.5) * 0.6666;
        }
        double result = Facets(x, y, z);
        result += sidekick.Facets(x += x, y += y, z += z) * 0.5;
        depth -= 2;
        double w = 2.0;
        do
            result += sidekick.Facets(x += x, y += y, z += z) / (w += w);
        while (--depth > 0);
        return w * result / (w + w - 1);
    }

    public void PickVector(int ix, int iy, int iz, ref Vector v)
    {
        const double divisor = 65536.0 * 65536.0;
        uint seed = (uint)table[table[table[
            ix & 0x0FFF] ^ (iy & 0x0FFF)] ^ (iz & 0x0FFF)] * 0x08088405 + 1;
        double x = seed / divisor + ix;
        seed = seed * 0x08088405 + 1;
        v = new(x, seed / divisor + iy, (seed * 0x08088405 + 1) / divisor + iz);
    }

    public void PickVector(int ix, int iy, int iz, int index, ref Vector v)
    {
        const double divisor = 65536.0 * 65536.0;
        uint seed = (uint)table[table[table[
            (ix += index / 25 - 2) & 0x0FFF] ^
            ((iy += (index % 25) / 5 - 2) & 0x0FFF)] ^
            ((iz += (index % 5) - 2) & 0x0FFF)] * 0x08088405 + 1;
        double x = seed / divisor + ix;
        seed = seed * 0x08088405 + 1;
        v = new(x, seed / divisor + iy, (seed * 0x08088405 + 1) / divisor + iz);
    }
}

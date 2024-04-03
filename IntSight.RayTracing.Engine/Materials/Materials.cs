namespace IntSight.RayTracing.Engine;

/// <summary>Common base for all materials.</summary>
public abstract class BaseMaterial
{
    /// <summary>A table with perturbation factors.</summary>
    private readonly struct PerturbationList
    {
        /// <summary>Maps roughness coefficients into perturbation tables.</summary>
        private static readonly Dictionary<double, PerturbationList> roughnessMaps = [];

        public readonly Vector[] perturbations;
        public readonly float[] weights;

        /// <summary>Creates a perturbation table for a given roughness.</summary>
        /// <param name="roughness">The roughness coefficient.</param>
        private PerturbationList(double roughness)
        {
            const int TABLE_SIZE = 4096;

            perturbations = new Vector[TABLE_SIZE];
            weights = new float[TABLE_SIZE];
            Random seed = new(3140);
            double rfactor = roughness / (1.0 + roughness);
            double power = 1.0 + 1.0 / roughness;
            for (int i = 0; i < TABLE_SIZE; i++)
            {
                double cosTheta = Math.Cos(Math.Acos(Math.Pow(
                    1.0 - seed.NextDouble(), rfactor)) / 3.0);
                double sinTheta = Math.Sqrt(1.0 - cosTheta * cosTheta);
                double r = 2 * Math.PI * seed.NextDouble();
                perturbations[i] = new(
                    sinTheta * Math.Cos(r), sinTheta * Math.Sin(r), cosTheta);
                weights[i] = (float)Math.Pow(cosTheta, power);
            }
        }

        /// <summary>Creates or retrieve a perturbation table for a given roughness.</summary>
        /// <param name="roughness">The roughness coefficient.</param>
        /// <returns>The perturbation table.</returns>
        public static PerturbationList Create(double roughness)
        {
            if (!roughnessMaps.TryGetValue(roughness, out PerturbationList result))
                roughnessMaps.Add(roughness, result = new PerturbationList(roughness));
            return result;
        }
    }

    private readonly PerturbationList perturbations;
    private int perturbationIndex;
    protected readonly bool hasBumps;
    protected readonly bool hasRoughness;
    /// <summary>An optional normal perturbator.</summary>
    protected IPerturbator perturbator;

    /// <summary>Initializes the material.</summary>
    /// <param name="roughness">Roughness coefficient.</param>
    /// <param name="perturbator">An optional normal pertubator.</param>
    protected BaseMaterial(double roughness, IPerturbator perturbator)
    {
        Roughness = roughness < 0.0 ? 0.0 : roughness;
        hasRoughness = roughness > 0.0;
        if (hasRoughness)
            perturbations = PerturbationList.Create(Roughness);
        this.perturbator = perturbator;
        hasBumps = perturbator != null;
    }

    /// <summary>Initializes a material with no normal perturbations.</summary>
    /// <param name="roughness">Roughness coefficient.</param>
    protected BaseMaterial(double roughness)
        : this(roughness, null) { }

    /// <summary>Modifies a normal vector given a location.</summary>
    /// <param name="normal">Normal to perturbate.</param>
    /// <param name="hitPoint">The intersection point.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Bump(ref Vector normal, in Vector hitPoint) =>
        // If we are here, then the pertubator is not null.
        perturbator.Perturbate(ref normal, hitPoint);

    /// <summary>Distorts a reflection vector in a small amount.</summary>
    /// <param name="reflection">Original reflection direction.</param>
    /// <param name="normal">Normal at the hit point.</param>
    /// <param name="sign">The sign of the cosine factor.</param>
    /// <returns>New reflection vector.</returns>
    public Vector Perturbate(in Vector reflection, in Vector normal, bool sign)
    {
        int initial = perturbationIndex;
        double len = 1.0 - reflection.Y * reflection.Y;
        if (len < Tolerance.Epsilon)
        {
            do
            {
                Vector p = perturbations.perturbations[perturbationIndex++];
                if (perturbationIndex >= perturbations.perturbations.Length)
                    perturbationIndex = 0;
                if ((p.Z * normal.Y + p.Y * normal.Z >= p.X * normal.X) == sign)
                    return new(-p.X, p.Z, p.Y);
            }
            while (perturbationIndex != initial);
        }
        else
        {
            len = Math.Sqrt(len);
            do
            {
                Vector p = perturbations.perturbations[perturbationIndex++];
                if (perturbationIndex >= perturbations.perturbations.Length)
                    perturbationIndex = 0;
                double x = (p.X * reflection.Z - p.Y * reflection.X * reflection.Y) / len +
                    p.Z * reflection.X;
                double y = p.Z * reflection.Y + p.Y * len;
                double z = p.Z * reflection.Z -
                    (p.Y * reflection.Y * reflection.Z + p.X * reflection.X) / len;
                if ((x * normal.X + y * normal.Y + z * normal.Z >= 0.0) == sign)
                {
                    len = 1.0 / Math.Sqrt(x * x + y * y + z * z);
                    return new(x * len, y * len, z * len);
                }
            }
            while (perturbationIndex != initial);
        }
        return reflection;
    }

    /// <summary>Splits a reflection vector into two distorted directions.</summary>
    /// <param name="reflection">Original reflection vector.</param>
    /// <param name="normal">Normal at the hit point.</param>
    /// <param name="r1">A ray for the first reflection vector.</param>
    /// <param name="r2">Second reflection vector.</param>
    /// <returns>The relative weight of the first reflection vector.</returns>
    public float Perturbate(in Vector reflection, in Vector normal,
        Ray r1, out Vector r2)
    {
        r1.Direction = Perturbate(reflection, normal, true);
        float d1 = perturbationIndex == 0 ?
            perturbations.weights[^1] :
            perturbations.weights[perturbationIndex - 1];

        int initial = perturbationIndex;
        double len = 1.0 - reflection.Y * reflection.Y;
        if (len < Tolerance.Epsilon)
        {
            do
            {
                float d2 = perturbations.weights[perturbationIndex];
                Vector p = perturbations.perturbations[perturbationIndex++];
                if (perturbationIndex >= perturbations.perturbations.Length)
                    perturbationIndex = 0;
                if (p.Z * normal.Y + p.Y * normal.Z >= p.X * normal.X)
                {
                    r2 = new(-p.X, p.Z, p.Y);
                    return d1 / (d1 + d2);
                }
            }
            while (perturbationIndex != initial);
        }
        else
        {
            len = Math.Sqrt(len);
            do
            {
                float d2 = perturbations.weights[perturbationIndex];
                Vector p = perturbations.perturbations[perturbationIndex++];
                if (perturbationIndex >= perturbations.perturbations.Length)
                    perturbationIndex = 0;
                double x = (p.X * reflection.Z - p.Y * reflection.X * reflection.Y) / len +
                    p.Z * reflection.X;
                double y = p.Z * reflection.Y + p.Y * len;
                double z = p.Z * reflection.Z -
                    (p.Y * reflection.Y * reflection.Z + p.X * reflection.X) / len;
                if (x * normal.X + y * normal.Y + z * normal.Z >= 0.0)
                {
                    len = 1.0 / Math.Sqrt(x * x + y * y + z * z);
                    r2 = new(x * len, y * len, z * len);
                    return d1 / (d1 + d2);
                }
            }
            while (perturbationIndex != initial);
        }
        r2 = reflection;
        return d1 / (1.0F + d1);
    }

    /// <summary>Does this material have a rough surface?</summary>
    public bool HasRoughness => hasRoughness;
    /// <summary>Gets the roughness factor of this surface.</summary>
    public double Roughness { get; }

    /// <summary>Clones the material's normal perturbator, if present.</summary>
    /// <param name="force">True when a new copy is needed.</param>
    /// <returns>The new perturbator.</returns>
    protected IPerturbator ClonePerturbator(bool force) => perturbator?.Clone(force);

    /// <summary>Translates the pigment of the material.</summary>
    /// <param name="translation">Translation distance.</param>
    /// <returns>The translated material.</returns>
    public virtual IMaterial Translate(in Vector translation) => (IMaterial)this;

    /// <summary>Rotates the pigment of the material.</summary>
    /// <param name="rotation">Rotation matrix.</param>
    /// <returns>The rotated material.</returns>
    public virtual IMaterial Rotate(in Matrix rotation) => (IMaterial)this;

    /// <summary>Scales the pigment of the material.</summary>
    /// <param name="factor">Scale factor.</param>
    /// <returns>The scaled material.</returns>
    public virtual IMaterial Scale(in Vector factor) => (IMaterial)this;
}
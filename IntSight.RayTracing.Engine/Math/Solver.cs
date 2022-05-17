using static System.Math;

namespace IntSight.RayTracing.Engine
{
    /// <summary>A simple analytical algebraic equations solver.</summary>
    public static class Solver
    {
        /// <summary>A small enough quantity.</summary>
        private const double eps = 1.0E-10;

        /// <summary>
        /// Represents up to four roots for a quartic or lesser degree polynomial.
        /// </summary>
        /// <remarks>
        /// Due to the particular nature of the root solver, there are a couple of
        /// remarkable class invariants to take advantage of:
        /// <code>
        ///     Count == 2 .IMPLIES. R0 .LE. R1
        ///     Count == 4 .IMPLIES. R0 .LE. R1 && R2 .LE. R3
        /// </code>
        /// </remarks>
        public struct Roots
        {
            public int Count;
            public double R0, R1, R2, R3;

            public double HitTest(double maxt)
            {
                if (Count == 4)
                {
                    double rslt = -1.0;
                    if (R0 >= Tolerance.Epsilon)
                    {
                        if (R0 <= maxt)
                            rslt = maxt = R0;
                    }
                    else if (Tolerance.Epsilon <= R1 && R1 <= maxt)
                        rslt = maxt = R1;
                    if (Tolerance.Epsilon <= R2 && R2 <= maxt)
                        rslt = maxt = R2;
                    if (Tolerance.Epsilon <= R3 && R3 <= maxt)
                        return R3;
                    return rslt;
                }
                else if (Count == 3)
                {
                    double rslt = -1.0;
                    if (Tolerance.Epsilon <= R0 && R0 <= maxt)
                        rslt = maxt = R0;
                    if (Tolerance.Epsilon <= R1 && R1 <= maxt)
                        rslt = maxt = R1;
                    if (Tolerance.Epsilon <= R2 && R2 <= maxt)
                        return R2;
                    return rslt;
                }
                else if (Count == 2)
                {
                    if (R0 >= Tolerance.Epsilon)
                    {
                        if (R0 <= maxt)
                            return R0;
                    }
                    else if (Tolerance.Epsilon <= R1 && R1 <= maxt)
                        return R1;
                    return -1.0;
                }
                else if (Count == 1)
                {
                    if (Tolerance.Epsilon <= R0 && R0 <= maxt)
                        return R0;
                    return -1.0;
                }
                else
                    return -1.0;
            }

            public double HitTest(double lo, double hi, double maxt)
            {
                double rslt = -1.0;
                if (Count > 0)
                {
                    if (lo < Tolerance.Epsilon)
                        lo = Tolerance.Epsilon;
                    if (hi > maxt)
                        hi = maxt;
                    if (lo <= R0 && R0 <= hi)
                        rslt = hi = R0;
                    if (Count > 1)
                    {
                        if (lo <= R1 && R1 < hi)
                            rslt = hi = R1;
                        if (Count > 2)
                        {
                            if (lo <= R2 && R2 < hi)
                                rslt = hi = R2;
                            if (Count > 3)
                                if (lo <= R3 && R3 < hi)
                                    rslt = R3;
                        }
                    }
                }
                return rslt;
            }

            public void GetHits4Roots(Hit[] hits)
            {
                if (R0 <= R2)
                {
                    if (R1 <= R2)
                    {
                        hits[3].Time = R3;
                        hits[2].Time = R2;
                        hits[1].Time = R1;
                    }
                    else
                    {
                        if (R1 <= R3)
                        {
                            hits[3].Time = R3;
                            hits[2].Time = R1;
                        }
                        else
                        {
                            hits[3].Time = R1;
                            hits[2].Time = R3;
                        }
                        hits[1].Time = R2;
                    }
                    hits[0].Time = R0;
                }
                else
                {
                    if (R3 <= R0)
                    {
                        hits[3].Time = R1;
                        hits[2].Time = R0;
                        hits[1].Time = R3;
                    }
                    else
                    {
                        if (R1 <= R3)
                        {
                            hits[3].Time = R3;
                            hits[2].Time = R1;
                        }
                        else
                        {
                            hits[3].Time = R1;
                            hits[2].Time = R3;
                        }
                        hits[1].Time = R0;
                    }
                    hits[0].Time = R2;
                }
            }

            public override string ToString() => Count switch
            {
                0 => "RTS[]",
                1 => $"RTS[{R0}]",
                2 => $"RTS[{R0}, {R1}]",
                3 => $"RTS[{R0}, {R1}, {R2}]",
                _ => $"RTS[{R0}, {R1}, {R2}, {R3}]"
            };
        }

        private const double Pi_2_3 = 2.0 * PI / 3.0;
        private const double Pi_4_3 = 4.0 * PI / 3.0;

        /// <summary>Solves a cubic equation.</summary>
        public static void Solve(double a1, double a2, double a3, ref Roots result)
        {
            double Q, Q3, R, R2, d, a = a1 * a1;
            Q = (a - 3.0 * a2) / 9.0;
            R = (a1 * (a - 4.5 * a2) + 13.5 * a3) / 27.0;
            Q3 = Q * Q * Q;
            R2 = R * R;
            d = Q3 - R2;
            double an = a1 / 3.0;
            if (d >= 0.0)
            {
                result.Count = 3;
                double theta = Acos(R / Sqrt(Q3)) / 3.0;
                Q = -2.0 * Sqrt(Q);
                result.R0 = Q * Cos(theta) - an;
                result.R1 = Q * Cos(theta + Pi_2_3) - an;
                result.R2 = Q * Cos(theta + Pi_4_3) - an;
            }
            else
            {
                result.Count = 1;
                double sQ = Exp(Log(Sqrt(R2 - Q3) + Abs(R)) / 3.0);
                result.R0 = R < 0 ? (sQ + Q / sQ) - an : -(sQ + Q / sQ) - an;
            }
        }

        /// <summary>Solves a quartic equation.</summary>
        public static void Solve(double a1, double a2, double a3, double a4, ref Roots roots)
        {
            const double OneThird = 1.0 / 3.0;
            if (a4 == 0.0 ||
                Abs(a4) < 1e-32 && Abs(a3) > 1e-22)
            {
                // This equation has a trivial root at 0.0
                // The original equation can be reduced to a cubic equation.
                double d = a1 * a1;
                double Q = (d - 3.0 * a2) * (1.0 / 9.0);
                double R = ((d - 4.5 * a2) * a1 + 13.5 * a3) * (1.0 / 27.0);
                double Q3 = Q * Q * Q;
                double an = a1 * OneThird;
                if ((d = Q3 - R * R) >= 0.0)
                {
                    roots.Count = 3;
                    d = Acos(R / Sqrt(Q3)) * OneThird;
                    Q = -2.0 * Sqrt(Q);
                    roots.R0 = Q * Cos(d) - an;
                    roots.R1 = Q * Cos(d + Pi_2_3) - an;
                    roots.R2 = Q * Cos(d + Pi_4_3) - an;
                }
                else
                {
                    roots.Count = 2;
                    d = Exp(Log(Sqrt(-d) + Abs(R)) * OneThird);
                    roots.R0 = R < 0.0 ? (d + Q / d) - an : -(d + Q / d) - an;
                    // The 0.0 root must be reintroduced.
                    // That's also true for the previous case but, that's a very
                    // infrequent situation, and in most cases, it makes no harm,
                    // since ShadowTest and HitTest discards roots < Tolerance.Epsilon.
                    if (roots.R0 <= 0.0)
                        roots.R1 = 0.0;
                    else
                    {
                        roots.R1 = roots.R0;
                        roots.R0 = 0.0;
                    }
                }
                return;
            }

            double z, a = a1 * a1;
            double p = FusedMultiplyAdd(-0.375, a, a2);
            double q = FusedMultiplyAdd((0.125 * a - 0.5 * a2), a1, a3);
            double r = (0.0625 * a2 - 0.01171875 * a) * a - 0.25 * a1 * a3 + a4;

            {
                double aux1 = p * (1.0 / -6.0);
                // Compared with a "normal" cubic equation, a1 has been divided by 3,
                // a2 is negated and a3 is halved.
                double aux = aux1 * aux1;
                double Q = FusedMultiplyAdd(r, OneThird, aux);
                double R = FusedMultiplyAdd(0.5, r, aux) * aux1 + 0.25 * r * p - 0.0625 * q * q;
                double R2 = R * R - Q * Q * Q;
                if (R2 <= 0.0)
                {
                    z = Sqrt(Q);
                    z = -Cos(Acos(R / (z * Q)) * OneThird) * (z + z) - aux1;
                }
                else if (R < 0)
                {
                    z = Exp(Log(Sqrt(R2) - R) * OneThird);
                    z = z + Q / z - aux1;
                }
                else
                {
                    z = Exp(Log(Sqrt(R2) + R) * OneThird);
                    z = -z - Q / z - aux1;
                }
            }

            double d1 = z + z - p, d2, q1;
            if (d1 < eps)
            {
                if (d1 < 0.0)
                {
                    if (d1 <= -eps)
                    {
                        roots.Count = 0;
                        return;
                    }
                    d1 = q1 = 0.0;
                }
                else
                    q1 = d1 * d1;
                if ((d2 = z * z - r) < 0.0)
                {
                    roots.Count = 0;
                    return;
                }
                d2 = Sqrt(d2);
            }
            else
            {
                q1 = d1;
                d1 = Sqrt(d1);
                d2 = 0.5 * q / d1;
            }

            p = (r = q1 - (z + d2) * 4.0) + 8 * d2;
            q1 = -0.25 * a1;
            if (p > 0.0)
            {
                p = Sqrt(p);
                roots.R0 = FusedMultiplyAdd(-0.5, (d1 + p), q1);
                roots.R1 = roots.R0 + p;
                if (r > 0.0)
                {
                    r = Sqrt(r);
                    roots.R2 = FusedMultiplyAdd((d1 - r), 0.5, q1);
                    roots.R3 = roots.R2 + r;
                    roots.Count = 4;
                }
                else if (r == 0.0)
                {
                    roots.R2 = 0.5 * d1 - q1;
                    roots.Count = 3;
                }
                else
                    roots.Count = 2;
            }
            else if (p < 0.0)
                if (r > 0.0)
                {
                    r = Sqrt(r);
                    roots.R0 = FusedMultiplyAdd((d1 - r), 0.5, q1);
                    roots.R1 = roots.R0 + r;
                    roots.Count = 2;
                }
                else if (r == 0.0)
                {
                    roots.R0 = 0.5 * d1 - q1;
                    roots.Count = 1;
                }
                else
                    roots.Count = 0;
            else
            {
                roots.R0 = -0.5 * d1 - q1;
                if (r > 0.0)
                {
                    r = Sqrt(r);
                    roots.R1 = FusedMultiplyAdd((d1 - r), 0.5, q1);
                    roots.R2 = roots.R1 + r;
                    roots.Count = 3;
                }
                else if (r == 0.0)
                {
                    roots.R1 = roots.R0 + d1;
                    roots.Count = 2;
                }
                else
                    roots.Count = 1;
            }
        }
    }
}

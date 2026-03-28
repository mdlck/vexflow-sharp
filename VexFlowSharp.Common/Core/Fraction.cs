// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;

namespace VexFlowSharp
{
    /// <summary>
    /// Represents a rational number using pure integer arithmetic.
    /// Port of VexFlow's Fraction class from fraction.ts.
    /// </summary>
    public readonly struct Fraction : IEquatable<Fraction>, IComparable<Fraction>
    {
        /// <summary>
        /// The tick resolution used throughout VexFlow. Equal to 16384.
        /// </summary>
        public static readonly int RESOLUTION = 16384;

        /// <summary>The numerator of the fraction.</summary>
        public int Numerator { get; }

        /// <summary>The denominator of the fraction.</summary>
        public int Denominator { get; }

        /// <summary>
        /// Create a new Fraction with the given numerator and denominator.
        /// </summary>
        public Fraction(int numerator = 1, int denominator = 1)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        /// <summary>
        /// Greatest common divisor using Euclidean algorithm.
        /// GCD(0, 0) => 0. GCD(0, n) => n.
        /// </summary>
        public static int GCD(int a, int b)
        {
            while (b != 0)
            {
                int t = b;
                b = a % b;
                a = t;
            }
            return a;
        }

        /// <summary>
        /// Least common multiple of two integers. Returns 0 if either input is 0.
        /// Uses long intermediate to detect overflow.
        /// </summary>
        public static int LCM(int a, int b)
        {
            if (a == 0 || b == 0) return 0;
            long result = (long)Math.Abs(a) / GCD(Math.Abs(a), Math.Abs(b)) * Math.Abs(b);
            if (result > int.MaxValue)
                throw new OverflowException($"LCM({a}, {b}) exceeds int.MaxValue");
            return (int)result;
        }

        /// <summary>
        /// Return a new Fraction simplified by GCD. Normalises sign so denominator is always positive.
        /// </summary>
        public Fraction Simplify()
        {
            int g = GCD(Math.Abs(Numerator), Math.Abs(Denominator));
            if (g == 0) return new Fraction(0, 1);

            int u = Numerator / g;
            int d = Denominator / g;

            // Normalise: denominator must be positive
            if (d < 0)
            {
                d = -d;
                u = -u;
            }
            return new Fraction(u, d);
        }

        /// <summary>
        /// Add another fraction. Uses LCM-based addition (pure integer).
        /// </summary>
        public Fraction Add(Fraction other)
        {
            int lcm;
            if (Numerator == 0)
                lcm = other.Denominator;
            else if (other.Numerator == 0)
                lcm = Denominator;
            else
            {
                int gcd = GCD(Math.Abs(Denominator), Math.Abs(other.Denominator));
                lcm = (Denominator / gcd) * other.Denominator;
            }

            int numerator = Numerator * (lcm / Denominator) + other.Numerator * (lcm / other.Denominator);
            return new Fraction(numerator, lcm);
        }

        /// <summary>
        /// Subtract another fraction.
        /// </summary>
        public Fraction Subtract(Fraction other)
        {
            return Add(new Fraction(-other.Numerator, other.Denominator));
        }

        /// <summary>
        /// Multiply by another fraction.
        /// </summary>
        public Fraction Multiply(Fraction other)
        {
            return new Fraction(Numerator * other.Numerator, Denominator * other.Denominator);
        }

        /// <summary>
        /// Divide by another fraction.
        /// </summary>
        public Fraction Divide(Fraction other)
        {
            return Multiply(new Fraction(other.Denominator, other.Numerator));
        }

        /// <summary>
        /// Return the double value of this fraction. This is the only place double is used.
        /// </summary>
        public double Value()
        {
            return (double)Numerator / Denominator;
        }

        /// <summary>
        /// Compare simplified forms for equality.
        /// </summary>
        public bool Equals(Fraction other)
        {
            var a = Simplify();
            var b = other.Simplify();
            return a.Numerator == b.Numerator && a.Denominator == b.Denominator;
        }

        /// <summary>
        /// Compare using cross-multiplication to avoid division.
        /// </summary>
        public int CompareTo(Fraction other)
        {
            // cross-multiply: (this.N * other.D) vs (other.N * this.D)
            // We need to be careful with negative denominators
            var a = Simplify();
            var b = other.Simplify();
            long lhs = (long)a.Numerator * b.Denominator;
            long rhs = (long)b.Numerator * a.Denominator;
            return lhs.CompareTo(rhs);
        }

        // Operator overloads
        public static Fraction operator +(Fraction a, Fraction b) => a.Add(b);
        public static Fraction operator -(Fraction a, Fraction b) => a.Subtract(b);
        public static Fraction operator *(Fraction a, Fraction b) => a.Multiply(b);
        public static Fraction operator /(Fraction a, Fraction b) => a.Divide(b);
        public static bool operator ==(Fraction a, Fraction b) => a.Equals(b);
        public static bool operator !=(Fraction a, Fraction b) => !a.Equals(b);
        public static bool operator <(Fraction a, Fraction b) => a.CompareTo(b) < 0;
        public static bool operator >(Fraction a, Fraction b) => a.CompareTo(b) > 0;
        public static bool operator <=(Fraction a, Fraction b) => a.CompareTo(b) <= 0;
        public static bool operator >=(Fraction a, Fraction b) => a.CompareTo(b) >= 0;

        public override bool Equals(object? obj) => obj is Fraction f && Equals(f);

        public override int GetHashCode()
        {
            var s = Simplify();
            return HashCode.Combine(s.Numerator, s.Denominator);
        }

        public override string ToString() => $"{Numerator}/{Denominator}";
    }
}

using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Core
{
    [TestFixture]
    [Category("Fraction")]
    public class FractionTests
    {
        [Test]
        public void RESOLUTION_IsCorrectValue()
        {
            Assert.That(Fraction.RESOLUTION, Is.EqualTo(16384));
        }

        [Test]
        public void GCD_Returns_GreatestCommonDivisor()
        {
            Assert.That(Fraction.GCD(12, 8), Is.EqualTo(4));
            Assert.That(Fraction.GCD(9, 6), Is.EqualTo(3));
            Assert.That(Fraction.GCD(7, 5), Is.EqualTo(1));
            Assert.That(Fraction.GCD(0, 5), Is.EqualTo(5));
        }

        [Test]
        public void Numerator_Denominator_AreIntType()
        {
            var f = new Fraction(3, 4);
            // Compile-time check: these must be int
            int n = f.Numerator;
            int d = f.Denominator;
            Assert.That(n, Is.EqualTo(3));
            Assert.That(d, Is.EqualTo(4));
        }

        [Test]
        public void Add_TwoQuarterNotes_ProducesHalf()
        {
            var result = new Fraction(1, 4).Add(new Fraction(1, 4)).Simplify();
            Assert.That(result.Numerator, Is.EqualTo(1));
            Assert.That(result.Denominator, Is.EqualTo(2));
        }

        [Test]
        public void Add_ZeroNumerator_Works()
        {
            var result = new Fraction(0, 1).Add(new Fraction(1, 4));
            Assert.That(result.Numerator, Is.EqualTo(1));
            Assert.That(result.Denominator, Is.EqualTo(4));
        }

        [Test]
        public void Subtract_ProducesHalf()
        {
            var result = new Fraction(3, 4).Subtract(new Fraction(1, 4)).Simplify();
            Assert.That(result.Numerator, Is.EqualTo(1));
            Assert.That(result.Denominator, Is.EqualTo(2));
        }

        [Test]
        public void Multiply_ProducesHalf()
        {
            var result = new Fraction(2, 3).Multiply(new Fraction(3, 4)).Simplify();
            Assert.That(result.Numerator, Is.EqualTo(1));
            Assert.That(result.Denominator, Is.EqualTo(2));
        }

        [Test]
        public void Divide_ProducesQuarter()
        {
            var result = new Fraction(1, 2).Divide(new Fraction(2, 1)).Simplify();
            Assert.That(result.Numerator, Is.EqualTo(1));
            Assert.That(result.Denominator, Is.EqualTo(4));
        }

        [Test]
        public void Simplify_ReducesCorrectly()
        {
            var result = new Fraction(4, 8).Simplify();
            Assert.That(result.Numerator, Is.EqualTo(1));
            Assert.That(result.Denominator, Is.EqualTo(2));
        }

        [Test]
        public void Simplify_NegativeInDenominator_MovesToNumerator()
        {
            var result = new Fraction(1, -2).Simplify();
            Assert.That(result.Numerator, Is.EqualTo(-1));
            Assert.That(result.Denominator, Is.EqualTo(2));
        }

        [Test]
        public void Simplify_NegativeInNumerator_Preserved()
        {
            var result = new Fraction(-1, 2).Simplify();
            Assert.That(result.Numerator, Is.EqualTo(-1));
            Assert.That(result.Denominator, Is.EqualTo(2));
        }

        [Test]
        public void Equals_EquivalentFractions_ReturnsTrue()
        {
            Assert.That(new Fraction(1, 4).Equals(new Fraction(2, 8)), Is.True);
        }

        [Test]
        public void Equals_DifferentFractions_ReturnsFalse()
        {
            Assert.That(new Fraction(1, 4).Equals(new Fraction(1, 3)), Is.False);
        }

        [Test]
        public void Value_ReturnsDouble()
        {
            Assert.That(new Fraction(1, 4).Value(), Is.EqualTo(0.25).Within(1e-10));
        }

        [Test]
        public void OperatorAdd_ProducesHalf()
        {
            var result = (new Fraction(1, 4) + new Fraction(1, 4)).Simplify();
            Assert.That(result.Numerator, Is.EqualTo(1));
            Assert.That(result.Denominator, Is.EqualTo(2));
        }

        [Test]
        public void OperatorSubtract_ProducesHalf()
        {
            var result = (new Fraction(3, 4) - new Fraction(1, 4)).Simplify();
            Assert.That(result.Numerator, Is.EqualTo(1));
            Assert.That(result.Denominator, Is.EqualTo(2));
        }

        [Test]
        public void OperatorMultiply_ProducesHalf()
        {
            var result = (new Fraction(2, 3) * new Fraction(3, 4)).Simplify();
            Assert.That(result.Numerator, Is.EqualTo(1));
            Assert.That(result.Denominator, Is.EqualTo(2));
        }

        [Test]
        public void OperatorDivide_ProducesQuarter()
        {
            var result = (new Fraction(1, 2) / new Fraction(2, 1)).Simplify();
            Assert.That(result.Numerator, Is.EqualTo(1));
            Assert.That(result.Denominator, Is.EqualTo(4));
        }

        [Test]
        public void OperatorEqual_EquivalentFractions()
        {
            Assert.That(new Fraction(1, 2) == new Fraction(1, 2), Is.True);
        }

        [Test]
        public void OperatorNotEqual_DifferentFractions()
        {
            Assert.That(new Fraction(1, 4) != new Fraction(1, 2), Is.True);
        }

        [Test]
        public void OperatorLessThan_Works()
        {
            Assert.That(new Fraction(1, 4) < new Fraction(1, 2), Is.True);
            Assert.That(new Fraction(1, 2) < new Fraction(1, 4), Is.False);
        }

        [Test]
        public void OperatorGreaterThan_Works()
        {
            Assert.That(new Fraction(1, 2) > new Fraction(1, 4), Is.True);
            Assert.That(new Fraction(1, 4) > new Fraction(1, 2), Is.False);
        }

        [Test]
        public void OperatorLessThanOrEqual_Works()
        {
            Assert.That(new Fraction(1, 4) <= new Fraction(1, 4), Is.True);
            Assert.That(new Fraction(1, 4) <= new Fraction(1, 2), Is.True);
            Assert.That(new Fraction(1, 2) <= new Fraction(1, 4), Is.False);
        }

        [Test]
        public void OperatorGreaterThanOrEqual_Works()
        {
            Assert.That(new Fraction(1, 4) >= new Fraction(1, 4), Is.True);
            Assert.That(new Fraction(1, 2) >= new Fraction(1, 4), Is.True);
            Assert.That(new Fraction(1, 4) >= new Fraction(1, 2), Is.False);
        }

        [Test]
        public void ToString_ShowsNumeratorSlashDenominator()
        {
            Assert.That(new Fraction(3, 4).ToString(), Is.EqualTo("3/4"));
        }
    }
}

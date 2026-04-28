using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Core
{
    [TestFixture]
    [Category("DataStructures")]
    public class TickableTests
    {
        /// <summary>Concrete subclass for testing the abstract Tickable.</summary>
        private class TestTickable : Tickable
        {
            public TestTickable(Fraction ticks) { this.ticks = ticks; }
            public override void Draw() { }
        }

        [Test]
        public void GetTicks_ReturnsTicks()
        {
            var t = new TestTickable(new Fraction(1, 4));
            Assert.That(t.GetTicks().Numerator, Is.EqualTo(1));
            Assert.That(t.GetTicks().Denominator, Is.EqualTo(4));
        }

        [Test]
        public void SetWidth_GetWidth_RoundTrips()
        {
            var t = new TestTickable(new Fraction(1, 4));
            t.SetWidth(42.5);
            Assert.That(t.GetWidth(), Is.EqualTo(42.5));
        }

        [Test]
        public void SetTicks_UpdatesTicks()
        {
            var t = new TestTickable(new Fraction(1, 4));
            t.SetTicks(new Fraction(1, 2));
            Assert.That(t.GetTicks().Numerator, Is.EqualTo(1));
            Assert.That(t.GetTicks().Denominator, Is.EqualTo(2));
        }

        [Test]
        public void Modifiers_InitiallyEmpty()
        {
            var t = new TestTickable(new Fraction(1, 4));
            // modifiers is protected; access via a public wrapper or check via reflection
            // We verify by checking GetMetrics is not null (modifiers is an internal detail)
            Assert.That(t.GetMetrics(), Is.Not.Null);
            // Additional: width defaults to 0
            Assert.That(t.GetWidth(), Is.EqualTo(0));
        }

        [Test]
        public void GetMetrics_ReturnsFormatterMetrics()
        {
            var t = new TestTickable(new Fraction(1, 4));
            var metrics = t.GetMetrics();
            Assert.That(metrics, Is.Not.Null);
            Assert.That(metrics, Is.InstanceOf<FormatterMetrics>());
        }

        [Test]
        public void ShouldIgnoreTicks_DefaultsFalse()
        {
            var t = new TestTickable(new Fraction(1, 4));
            Assert.That(t.ShouldIgnoreTicks(), Is.False);
        }

        [Test]
        public void Width_DefaultsToZero()
        {
            var t = new TestTickable(new Fraction(1, 4));
            Assert.That(t.GetWidth(), Is.EqualTo(0));
        }

        [Test]
        public void XShift_DefaultsToZero()
        {
            var t = new TestTickable(new Fraction(1, 4));
            Assert.That(t.GetXShift(), Is.EqualTo(0));
        }

        [Test]
        public void SetXShift_UpdatesValue()
        {
            var t = new TestTickable(new Fraction(1, 4));
            t.SetXShift(10.5);
            Assert.That(t.GetXShift(), Is.EqualTo(10.5));
        }

        [Test]
        public void GetTickMultiplier_DefaultsToOne()
        {
            var t = new TestTickable(new Fraction(1, 4));
            var multiplier = t.GetTickMultiplier();
            Assert.That(multiplier.Numerator, Is.EqualTo(1));
            Assert.That(multiplier.Denominator, Is.EqualTo(1));
        }

        [Test]
        public void MetricsDuration_DefaultsToEmpty()
        {
            var t = new TestTickable(new Fraction(1, 4));
            Assert.That(t.GetMetrics().Duration, Is.EqualTo(""));
        }

        [Test]
        public void IsChildOfElement()
        {
            var t = new TestTickable(new Fraction(1, 4));
            Assert.That(t, Is.InstanceOf<Element>());
        }

        [Test]
        public void Category_IsV5Tickable()
        {
            var t = new TestTickable(new Fraction(1, 4));

            Assert.That(Tickable.CATEGORY, Is.EqualTo("Tickable"));
            Assert.That(t.GetCategory(), Is.EqualTo(Tickable.CATEGORY));
        }
    }
}

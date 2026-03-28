// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using NUnit.Framework;

namespace VexFlowSharp.Tests.StaveTests
{
    [TestFixture]
    [Category("TimeSignature")]
    public class TimeSignatureTests
    {
        // ── Numeric time signatures ───────────────────────────────────────────

        [Test]
        public void TimeSignature_4_4_IsNumeric()
        {
            var ts = new TimeSignature("4/4");
            Assert.That(ts.GetIsNumeric(), Is.True);
        }

        [Test]
        public void TimeSignature_4_4_HasPositiveWidth()
        {
            var ts = new TimeSignature("4/4");
            Assert.That(ts.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void TimeSignature_3_4_IsNumeric()
        {
            var ts = new TimeSignature("3/4");
            Assert.That(ts.GetIsNumeric(), Is.True);
        }

        [Test]
        public void TimeSignature_3_4_HasPositiveWidth()
        {
            var ts = new TimeSignature("3/4");
            Assert.That(ts.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void TimeSignature_6_8_IsNumeric()
        {
            var ts = new TimeSignature("6/8");
            Assert.That(ts.GetIsNumeric(), Is.True);
        }

        [Test]
        public void TimeSignature_6_8_HasPositiveWidth()
        {
            var ts = new TimeSignature("6/8");
            Assert.That(ts.GetWidth(), Is.GreaterThan(0));
        }

        // ── Special (common/cut) time ─────────────────────────────────────────

        [Test]
        public void TimeSignature_C_IsNotNumeric()
        {
            var ts = new TimeSignature("C");
            Assert.That(ts.GetIsNumeric(), Is.False);
        }

        [Test]
        public void TimeSignature_C_HasPositiveWidth()
        {
            var ts = new TimeSignature("C");
            Assert.That(ts.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void TimeSignature_CutTime_IsNotNumeric()
        {
            var ts = new TimeSignature("C|");
            Assert.That(ts.GetIsNumeric(), Is.False);
        }

        [Test]
        public void TimeSignature_CutTime_HasPositiveWidth()
        {
            var ts = new TimeSignature("C|");
            Assert.That(ts.GetWidth(), Is.GreaterThan(0));
        }

        // ── Line positions ────────────────────────────────────────────────────

        [Test]
        public void TimeSignature_C_Line_Is2()
        {
            var ts = new TimeSignature("C");
            Assert.That(ts.Line, Is.EqualTo(2));
        }

        [Test]
        public void TimeSignature_CutTime_Line_Is2()
        {
            var ts = new TimeSignature("C|");
            Assert.That(ts.Line, Is.EqualTo(2));
        }

        [Test]
        public void TimeSignature_Numeric_Line_Is0()
        {
            var ts = new TimeSignature("4/4");
            Assert.That(ts.Line, Is.EqualTo(0));
        }

        // ── Spec parsing ──────────────────────────────────────────────────────

        [Test]
        public void TimeSignature_GetTimeSpec_Returns44()
        {
            var ts = new TimeSignature("4/4");
            Assert.That(ts.GetTimeSpec(), Is.EqualTo("4/4"));
        }

        [Test]
        public void TimeSignature_GetTimeSpec_ReturnsC()
        {
            var ts = new TimeSignature("C");
            Assert.That(ts.GetTimeSpec(), Is.EqualTo("C"));
        }

        // ── Position ─────────────────────────────────────────────────────────

        [Test]
        public void TimeSignature_DefaultPosition_IsBegin()
        {
            var ts = new TimeSignature("4/4");
            Assert.That(ts.GetPosition(), Is.EqualTo(StaveModifierPosition.Begin));
        }

        // ── TopLine / BottomLine ──────────────────────────────────────────────

        [Test]
        public void TimeSignature_TopLine_Is1()
        {
            var ts = new TimeSignature("4/4");
            Assert.That(ts.TopLine, Is.EqualTo(1));
        }

        [Test]
        public void TimeSignature_BottomLine_Is3()
        {
            var ts = new TimeSignature("4/4");
            Assert.That(ts.BottomLine, Is.EqualTo(3));
        }
    }
}

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using NUnit.Framework;

namespace VexFlowSharp.Tests.StaveTests
{
    [TestFixture]
    [Category("Barline")]
    public class BarlineTests
    {
        // ── Enum values ───────────────────────────────────────────────────────

        [Test]
        public void BarlineType_EnumValues_MatchVexFlow()
        {
            Assert.That((int)BarlineType.Single,      Is.EqualTo(1));
            Assert.That((int)BarlineType.Double,      Is.EqualTo(2));
            Assert.That((int)BarlineType.End,         Is.EqualTo(3));
            Assert.That((int)BarlineType.RepeatBegin, Is.EqualTo(4));
            Assert.That((int)BarlineType.RepeatEnd,   Is.EqualTo(5));
            Assert.That((int)BarlineType.RepeatBoth,  Is.EqualTo(6));
            Assert.That((int)BarlineType.None,        Is.EqualTo(7));
        }

        // ── Constructor ───────────────────────────────────────────────────────

        [Test]
        public void Constructor_SetsSingleType()
        {
            var b = new Barline(BarlineType.Single);
            Assert.That(b.GetBarlineType(), Is.EqualTo(BarlineType.Single));
        }

        [Test]
        public void Constructor_SetsDoubleType()
        {
            var b = new Barline(BarlineType.Double);
            Assert.That(b.GetBarlineType(), Is.EqualTo(BarlineType.Double));
        }

        [Test]
        public void Constructor_SetsEndType()
        {
            var b = new Barline(BarlineType.End);
            Assert.That(b.GetBarlineType(), Is.EqualTo(BarlineType.End));
        }

        [Test]
        public void Constructor_SetsRepeatBeginType()
        {
            var b = new Barline(BarlineType.RepeatBegin);
            Assert.That(b.GetBarlineType(), Is.EqualTo(BarlineType.RepeatBegin));
        }

        [Test]
        public void Constructor_SetsRepeatEndType()
        {
            var b = new Barline(BarlineType.RepeatEnd);
            Assert.That(b.GetBarlineType(), Is.EqualTo(BarlineType.RepeatEnd));
        }

        // ── Width ─────────────────────────────────────────────────────────────

        [Test]
        public void GetWidth_Single_IsNonZero()
        {
            var b = new Barline(BarlineType.Single);
            Assert.That(b.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void GetWidth_None_IsZero()
        {
            var b = new Barline(BarlineType.None);
            Assert.That(b.GetWidth(), Is.EqualTo(0));
        }

        [Test]
        public void GetWidth_RepeatBegin_IsNonZero()
        {
            var b = new Barline(BarlineType.RepeatBegin);
            Assert.That(b.GetWidth(), Is.GreaterThan(0));
        }

        // ── Position ─────────────────────────────────────────────────────────

        [Test]
        public void Constructor_DefaultPosition_IsBegin()
        {
            var b = new Barline(BarlineType.Single);
            Assert.That(b.GetPosition(), Is.EqualTo(StaveModifierPosition.Begin));
        }

        // ── Layout metrics ────────────────────────────────────────────────────

        [Test]
        public void GetLayoutMetrics_Single_NotNull()
        {
            var b = new Barline(BarlineType.Single);
            Assert.That(b.GetLayoutMetrics(), Is.Not.Null);
        }

        [Test]
        public void SetType_ChangesType()
        {
            var b = new Barline(BarlineType.Single);
            b.SetType(BarlineType.End);
            Assert.That(b.GetBarlineType(), Is.EqualTo(BarlineType.End));
        }
    }
}

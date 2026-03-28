// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using NUnit.Framework;

namespace VexFlowSharp.Tests.StaveTests
{
    [TestFixture]
    [Category("Clef")]
    public class ClefTests
    {
        // ── Constructor ───────────────────────────────────────────────────────

        [Test]
        public void Clef_Treble_HasPositiveWidth()
        {
            var clef = new Clef("treble");
            Assert.That(clef.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void Clef_Bass_HasPositiveWidth()
        {
            var clef = new Clef("bass");
            Assert.That(clef.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void Clef_Alto_UsesCClef()
        {
            var clef = new Clef("alto");
            Assert.That(clef.GetClefInfo().Code, Is.EqualTo("cClef"));
        }

        [Test]
        public void Clef_Tenor_UsesCClef()
        {
            var clef = new Clef("tenor");
            Assert.That(clef.GetClefInfo().Code, Is.EqualTo("cClef"));
        }

        [Test]
        public void Clef_Treble_UsesGClef()
        {
            var clef = new Clef("treble");
            Assert.That(clef.GetClefInfo().Code, Is.EqualTo("gClef"));
        }

        [Test]
        public void Clef_Bass_UsesFClef()
        {
            var clef = new Clef("bass");
            Assert.That(clef.GetClefInfo().Code, Is.EqualTo("fClef"));
        }

        [Test]
        public void Clef_Percussion_HasPositiveWidth()
        {
            var clef = new Clef("percussion");
            Assert.That(clef.GetWidth(), Is.GreaterThan(0));
        }

        // ── Staff line positions ──────────────────────────────────────────────

        [Test]
        public void Clef_Treble_Line_Is3()
        {
            var clef = new Clef("treble");
            Assert.That(clef.GetClefInfo().Line, Is.EqualTo(3));
        }

        [Test]
        public void Clef_Bass_Line_Is1()
        {
            var clef = new Clef("bass");
            Assert.That(clef.GetClefInfo().Line, Is.EqualTo(1));
        }

        [Test]
        public void Clef_Alto_Line_Is2()
        {
            var clef = new Clef("alto");
            Assert.That(clef.GetClefInfo().Line, Is.EqualTo(2));
        }

        // ── Position ─────────────────────────────────────────────────────────

        [Test]
        public void Clef_DefaultPosition_IsBegin()
        {
            var clef = new Clef("treble");
            Assert.That(clef.GetPosition(), Is.EqualTo(StaveModifierPosition.Begin));
        }

        // ── Types map ─────────────────────────────────────────────────────────

        [Test]
        public void Clef_Types_ContainsAllStandardClefs()
        {
            Assert.That(Clef.Types, Contains.Key("treble"));
            Assert.That(Clef.Types, Contains.Key("bass"));
            Assert.That(Clef.Types, Contains.Key("alto"));
            Assert.That(Clef.Types, Contains.Key("tenor"));
            Assert.That(Clef.Types, Contains.Key("percussion"));
        }

        [Test]
        public void Clef_InvalidType_Throws()
        {
            var ex = Assert.Throws<VexFlowException>(() => new Clef("nonexistent"));
            Assert.That(ex!.Code, Is.EqualTo("BadArguments"));
        }

        // ── SetType ───────────────────────────────────────────────────────────

        [Test]
        public void Clef_SetType_ChangesCleInfo()
        {
            var clef = new Clef("treble");
            clef.SetType("bass");
            Assert.That(clef.GetClefInfo().Code, Is.EqualTo("fClef"));
        }

        // ── GetPoint ──────────────────────────────────────────────────────────

        [Test]
        public void GetPoint_Default_IsNotationFontScale()
        {
            Assert.That(Clef.GetPoint("default"), Is.EqualTo(Tables.NOTATION_FONT_SCALE));
        }

        [Test]
        public void GetPoint_Small_IsTwoThirdsOfDefault()
        {
            double expected = (Tables.NOTATION_FONT_SCALE / 3) * 2;
            Assert.That(Clef.GetPoint("small"), Is.EqualTo(expected).Within(0.001));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.TableTests
{
    [TestFixture]
    [Category("Tables")]
    public class TablesTests
    {
        // ── SanitizeDuration ──────────────────────────────────────────────────

        [Test]
        public void SanitizeDuration_AliasQ_Returns4()
        {
            Assert.That(Tables.SanitizeDuration("q"), Is.EqualTo("4"));
        }

        [Test]
        public void SanitizeDuration_AliasW_Returns1()
        {
            Assert.That(Tables.SanitizeDuration("w"), Is.EqualTo("1"));
        }

        [Test]
        public void SanitizeDuration_AliasH_Returns2()
        {
            Assert.That(Tables.SanitizeDuration("h"), Is.EqualTo("2"));
        }

        [Test]
        public void SanitizeDuration_AliasB_Returns256()
        {
            Assert.That(Tables.SanitizeDuration("b"), Is.EqualTo("256"));
        }

        [Test]
        public void SanitizeDuration_DirectValue_PassesThrough()
        {
            Assert.That(Tables.SanitizeDuration("8"), Is.EqualTo("8"));
        }

        [Test]
        public void SanitizeDuration_Invalid_Throws()
        {
            var ex = Assert.Throws<VexFlowException>(() => Tables.SanitizeDuration("x99"));
            Assert.That(ex!.Code, Is.EqualTo("BadArguments"));
        }

        // ── DurationToTicks ───────────────────────────────────────────────────

        [Test]
        public void DurationToTicks_Quarter_Returns4096()
        {
            Assert.That(Tables.DurationToTicks("4"), Is.EqualTo(4096));
            Assert.That(Tables.DurationToTicks("q"), Is.EqualTo(4096));
        }

        [Test]
        public void DurationToTicks_Whole_Returns16384()
        {
            Assert.That(Tables.DurationToTicks("1"), Is.EqualTo(16384));
        }

        [Test]
        public void DurationToTicks_Half_Returns8192()
        {
            Assert.That(Tables.DurationToTicks("2"), Is.EqualTo(8192));
        }

        // ── KeySignature ──────────────────────────────────────────────────────

        [Test]
        public void KeySignature_C_ReturnsEmpty()
        {
            var result = Tables.KeySignature("C");
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void KeySignature_G_OneSharp()
        {
            var result = Tables.KeySignature("G");
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Type, Is.EqualTo("#"));
            Assert.That(result[0].Line, Is.EqualTo(0));
        }

        [Test]
        public void KeySignature_F_OneFlat()
        {
            var result = Tables.KeySignature("F");
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Type, Is.EqualTo("b"));
            Assert.That(result[0].Line, Is.EqualTo(2));
        }

        [Test]
        public void KeySignature_D_TwoSharps()
        {
            var result = Tables.KeySignature("D");
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Type, Is.EqualTo("#"));
            Assert.That(result[1].Type, Is.EqualTo("#"));
        }

        [Test]
        public void KeySignature_Bb_TwoFlats()
        {
            var result = Tables.KeySignature("Bb");
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void KeySignature_Unknown_Throws()
        {
            var ex = Assert.Throws<VexFlowException>(() => Tables.KeySignature("Z#m"));
            Assert.That(ex!.Code, Is.EqualTo("BadKeySignature"));
        }

        // ── KeyProperties ─────────────────────────────────────────────────────

        [Test]
        public void KeyProperties_C4_TrebleClef_ReturnsCorrectLine()
        {
            // Middle C (c/4) on treble clef: index=0, octave=4
            // baseIndex = 4*7 - 4*7 = 0; line = (0+0)/2 = 0; clef_shift = 0 => line=0
            var props = Tables.KeyProperties("c/4", "treble");
            Assert.That(props.Line, Is.EqualTo(0.0));
        }

        [Test]
        public void KeyProperties_B4_TrebleClef()
        {
            // B4: index=6, octave=4
            // baseIndex = 4*7 - 28 = 0; line = (0+6)/2 = 3
            var props = Tables.KeyProperties("b/4", "treble");
            Assert.That(props.Line, Is.EqualTo(3.0));
        }

        [Test]
        public void KeyProperties_E4_TrebleClef()
        {
            // E4: index=2, octave=4
            // baseIndex=0; line = (0+2)/2 = 1
            var props = Tables.KeyProperties("e/4", "treble");
            Assert.That(props.Line, Is.EqualTo(1.0));
        }

        [Test]
        public void KeyProperties_InvalidKey_Throws()
        {
            var ex = Assert.Throws<VexFlowException>(() => Tables.KeyProperties("z/4", "treble"));
            Assert.That(ex!.Code, Is.EqualTo("BadArguments"));
        }

        // ── CodeNoteHead ──────────────────────────────────────────────────────

        [Test]
        public void CodeNoteHead_Normal_Quarter_ReturnsEmpty()
        {
            // "N" is the normal notehead code — it returns a specific glyph based on duration
            var code = Tables.CodeNoteHead("N", "4");
            Assert.That(code, Is.Not.Empty);
            Assert.That(code, Is.EqualTo("noteheadBlack"));
        }

        [Test]
        public void CodeNoteHead_Normal_Whole_ReturnsWhole()
        {
            var code = Tables.CodeNoteHead("N", "1");
            Assert.That(code, Is.EqualTo("noteheadWhole"));
        }

        [Test]
        public void CodeNoteHead_UnknownType_ReturnsEmpty()
        {
            var code = Tables.CodeNoteHead("UNKNOWN_TYPE_ZZZ", "4");
            Assert.That(code, Is.EqualTo(""));
        }

        // ── GetGlyphProps ─────────────────────────────────────────────────────

        [Test]
        public void GetGlyphProps_Quarter_HasStem()
        {
            var props = Tables.GetGlyphProps("4");
            Assert.That(props.Stem, Is.True);
        }

        [Test]
        public void GetGlyphProps_Whole_NoStem()
        {
            var props = Tables.GetGlyphProps("1");
            Assert.That(props.Stem, Is.False);
        }

        [Test]
        public void GetGlyphProps_Rest_Quarter_IsRest()
        {
            var props = Tables.GetGlyphProps("4", "r");
            Assert.That(props.Rest, Is.True);
            Assert.That(props.CodeHead, Is.EqualTo("restQuarter"));
        }

        // ── Constants ─────────────────────────────────────────────────────────

        [Test]
        public void Constants_AreCorrect()
        {
            Assert.That(Tables.RESOLUTION,           Is.EqualTo(16384));
            Assert.That(Tables.STEM_HEIGHT,          Is.EqualTo(35.0));
            Assert.That(Tables.NOTATION_FONT_SCALE,  Is.EqualTo(39.0));
            Assert.That(Tables.STEM_WIDTH,           Is.EqualTo(1.5));
            Assert.That(Tables.SLASH_NOTEHEAD_WIDTH, Is.EqualTo(15.0));
        }
    }
}

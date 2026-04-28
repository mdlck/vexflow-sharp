// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using NUnit.Framework;

namespace VexFlowSharp.Tests.StaveTests
{
    [TestFixture]
    [Category("KeySignature")]
    public class KeySignatureTests
    {
        // ── Accidental counts ─────────────────────────────────────────────────

        [Test]
        public void KeySignature_C_HasZeroAccidentals()
        {
            var ks = new KeySignature("C");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(0));
        }

        [Test]
        public void KeySignature_G_Has1Sharp()
        {
            var ks = new KeySignature("G");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(1));
        }

        [Test]
        public void KeySignature_D_Has2Sharps()
        {
            var ks = new KeySignature("D");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(2));
        }

        [Test]
        public void KeySignature_A_Has3Sharps()
        {
            var ks = new KeySignature("A");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(3));
        }

        [Test]
        public void KeySignature_E_Has4Sharps()
        {
            var ks = new KeySignature("E");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(4));
        }

        [Test]
        public void KeySignature_B_Has5Sharps()
        {
            var ks = new KeySignature("B");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(5));
        }

        [Test]
        public void KeySignature_FSharp_Has6Sharps()
        {
            var ks = new KeySignature("F#");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(6));
        }

        [Test]
        public void KeySignature_CSharp_Has7Sharps()
        {
            var ks = new KeySignature("C#");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(7));
        }

        [Test]
        public void KeySignature_F_Has1Flat()
        {
            var ks = new KeySignature("F");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(1));
        }

        [Test]
        public void KeySignature_Bb_Has2Flats()
        {
            var ks = new KeySignature("Bb");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(2));
        }

        [Test]
        public void KeySignature_Eb_Has3Flats()
        {
            var ks = new KeySignature("Eb");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(3));
        }

        [Test]
        public void KeySignature_Ab_Has4Flats()
        {
            var ks = new KeySignature("Ab");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(4));
        }

        [Test]
        public void KeySignature_Db_Has5Flats()
        {
            var ks = new KeySignature("Db");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(5));
        }

        [Test]
        public void KeySignature_Gb_Has6Flats()
        {
            var ks = new KeySignature("Gb");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(6));
        }

        [Test]
        public void KeySignature_Cb_Has7Flats()
        {
            var ks = new KeySignature("Cb");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(7));
        }

        // ── Width ─────────────────────────────────────────────────────────────

        [Test]
        public void KeySignature_C_WidthIsZero()
        {
            var ks = new KeySignature("C");
            Assert.That(ks.GetWidth(), Is.EqualTo(0));
        }

        [Test]
        public void KeySignature_G_WidthIsPositive()
        {
            var ks = new KeySignature("G");
            Assert.That(ks.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void KeySignature_CSharp_Width_GreaterThan_G()
        {
            var ksG  = new KeySignature("G");
            var ksCSharp = new KeySignature("C#");
            Assert.That(ksCSharp.GetWidth(), Is.GreaterThan(ksG.GetWidth()));
        }

        [Test]
        public void KeySignature_D_WidthUsesMetricAccidentalSpacing()
        {
            var ks = new KeySignature("D");
            var (code, _) = Tables.AccidentalCodes("#");
            double scale = Glyph.GetScale(Tables.NOTATION_FONT_SCALE, BravuraGlyphs.Data);
            Assert.That(BravuraGlyphs.Data.Glyphs.TryGetValue(code, out var glyph), Is.True);

            double glyphWidth = (glyph!.XMax - glyph.XMin) * scale;
            double expected = glyphWidth * 2 + Metrics.GetDouble("KeySignature.accidentalSpacing");

            Assert.That(ks.GetWidth(), Is.EqualTo(expected).Within(0.0001));
        }

        [Test]
        public void KeySignature_PetalumaWidthUsesFontSpecificGlyphScale()
        {
            try
            {
                Font.ClearRegistry();
                VexFlow.LoadFonts("Petaluma", "Petaluma Script");
                VexFlow.SetFonts("Petaluma", "Petaluma Script");

                var ks = new KeySignature("D");
                var (code, _) = Tables.AccidentalCodes("#");
                Assert.That(PetalumaGlyphs.Data.Glyphs.TryGetValue(code, out var glyph), Is.True);

                double width = glyph!.XMax - glyph.XMin;
                double expected = width * Glyph.GetScale(Tables.NOTATION_FONT_SCALE, PetalumaGlyphs.Data) * 2
                    + Metrics.GetDouble("KeySignature.accidentalSpacing");
                double legacyBravuraOnlyWidth = width * (Tables.NOTATION_FONT_SCALE * 0.72 / PetalumaGlyphs.Data.Resolution) * 2
                    + Metrics.GetDouble("KeySignature.accidentalSpacing");

                Assert.That(ks.GetWidth(), Is.EqualTo(expected).Within(0.0001));
                Assert.That(ks.GetWidth(), Is.GreaterThan(legacyBravuraOnlyWidth * 1.2));
            }
            finally
            {
                Font.ClearRegistry();
                Font.Load("Bravura", BravuraGlyphs.Data);
                VexFlow.SetFonts("Bravura", "Academico");
            }
        }

        [Test]
        public void KeySignature_CancelPreviousKey_AddsNaturals()
        {
            var ks = new KeySignature("C", "G");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(1));
            Assert.That(ks.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void KeySignature_CancelDifferentAccidentalType_CancelsAllPreviousAccidentals()
        {
            var ks = new KeySignature("F", "D");
            Assert.That(ks.GetAccidentalCount(), Is.EqualTo(3));
        }

        // ── Position ─────────────────────────────────────────────────────────

        [Test]
        public void KeySignature_DefaultPosition_IsBegin()
        {
            var ks = new KeySignature("G");
            Assert.That(ks.GetPosition(), Is.EqualTo(StaveModifierPosition.Begin));
        }

        // ── Padding ───────────────────────────────────────────────────────────

        [Test]
        public void KeySignature_C_GetPadding_IsZero()
        {
            var ks = new KeySignature("C");
            // C major has no accidentals; padding should be 0
            Assert.That(ks.GetPadding(2), Is.EqualTo(0));
        }
    }
}

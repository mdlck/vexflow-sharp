// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using NUnit.Framework;

namespace VexFlowSharp.Tests.Fonts
{
    [TestFixture]
    [Category("Glyph")]
    public class BravuraGlyphsTests
    {
        [Test]
        public void Data_Resolution_Is1000()
        {
            Assert.That(BravuraGlyphs.Data.Resolution, Is.EqualTo(1000));
        }

        [Test]
        public void Glyphs_Contains_GClef()
        {
            Assert.That(BravuraGlyphs.Data.Glyphs, Contains.Key("gClef"));
        }

        [Test]
        public void Glyphs_Contains_FClef()
        {
            Assert.That(BravuraGlyphs.Data.Glyphs, Contains.Key("fClef"));
        }

        [Test]
        public void Glyphs_Contains_NoteheadBlack()
        {
            Assert.That(BravuraGlyphs.Data.Glyphs, Contains.Key("noteheadBlack"));
        }

        [Test]
        public void Glyphs_Contains_V5MetronomeTempoGlyphs()
        {
            Assert.That(BravuraGlyphs.Data.Glyphs, Contains.Key("metNoteQuarterUp"));
            Assert.That(BravuraGlyphs.Data.Glyphs, Contains.Key("metNote8thUp"));
            Assert.That(BravuraGlyphs.Data.Glyphs, Contains.Key("metAugmentationDot"));
        }

        [Test]
        public void GClef_CachedOutline_IsNotNullAndNonEmpty()
        {
            var glyph = BravuraGlyphs.Data.Glyphs["gClef"];
            Assert.That(glyph.CachedOutline, Is.Not.Null);
            Assert.That(glyph.CachedOutline!.Length, Is.GreaterThan(0));
        }

        [Test]
        public void GClef_Ha_IsGreaterThanZero()
        {
            var glyph = BravuraGlyphs.Data.Glyphs["gClef"];
            Assert.That(glyph.Ha, Is.GreaterThan(0));
        }

        [Test]
        public void Glyphs_Count_IsAtLeast400()
        {
            Assert.That(BravuraGlyphs.Data.Glyphs.Count, Is.GreaterThanOrEqualTo(400));
        }
    }
}

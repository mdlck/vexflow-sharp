using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("RepeatNote")]
    public class RepeatNoteTests
    {
        [TestCase("1", "repeat1Bar", true)]
        [TestCase("2", "repeat2Bars", true)]
        [TestCase("4", "repeat4Bars", true)]
        [TestCase("slash", "repeatBarSlash", false)]
        [TestCase("unknown", "repeat1Bar", true)]
        public void Constructor_MapsTypeToGlyphAndCentering(string type, string expectedGlyph, bool expectedCentering)
        {
            var note = new RepeatNote(type);

            Assert.That(note.GetGlyph(), Is.EqualTo(expectedGlyph));
            Assert.That(note.GetDuration(), Is.EqualTo("4"));
            Assert.That(note.IsCenterAligned(), Is.EqualTo(expectedCentering));
            Assert.That(note.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void Draw_RendersRepeatGlyph()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 220);
            stave.SetContext(ctx);
            var note = new RepeatNote("2");
            note.SetStave(stave).SetX(90);

            note.Draw();

            Assert.That(ctx.HasCall("Fill"), Is.True);
            Assert.That(note.IsRendered(), Is.True);
        }
    }
}

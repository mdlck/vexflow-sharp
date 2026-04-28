using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("GlyphNote")]
    public class GlyphNoteTests
    {
        [Test]
        public void Constructor_SetsGlyphTicksAndWidth()
        {
            var note = new GlyphNote("repeat1Bar", new NoteStruct { Duration = "q" });

            Assert.That(note.GetGlyph(), Is.EqualTo("repeat1Bar"));
            Assert.That(note.GetDuration(), Is.EqualTo("4"));
            Assert.That(note.GetWidth(), Is.GreaterThan(0));
            Assert.That(note.ShouldIgnoreTicks(), Is.False);
        }

        [Test]
        public void Constructor_CanIgnoreTicksAndSetLine()
        {
            var note = new GlyphNote("repeat1Bar", new NoteStruct { Duration = "q" },
                new GlyphNoteOptions { IgnoreTicks = true, Line = 3 });

            Assert.That(note.ShouldIgnoreTicks(), Is.True);
            Assert.That(note.GetOptions().Line, Is.EqualTo(3));
        }

        [Test]
        public void Draw_RendersGlyphAtStaveLine()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 220);
            stave.SetContext(ctx);
            var note = new GlyphNote("repeat1Bar", new NoteStruct { Duration = "q" });
            note.SetStave(stave).SetX(80);

            note.Draw();

            Assert.That(ctx.HasCall("Fill"), Is.True);
            Assert.That(note.IsRendered(), Is.True);
        }
    }
}

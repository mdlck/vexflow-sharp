using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("TextNote")]
    [Category("Phase5")]
    public class TextNoteTests
    {
        [Test]
        public void Constructor_StoresV5TextNoteStructOptions()
        {
            var font = new MetricsFontInfo { Family = "Academico", Size = 14, Weight = "bold", Style = "italic" };

            var note = new TextNote(new TextNoteStruct
            {
                Text = "lyric",
                Duration = "8",
                Line = 2,
                Justification = TextJustification.Center,
                Font = font,
            });

            Assert.That(note.GetCategory(), Is.EqualTo(TextNote.CATEGORY));
            Assert.That(note.GetText(), Is.EqualTo("lyric"));
            Assert.That(note.GetDuration(), Is.EqualTo("8"));
            Assert.That(note.GetTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION / 8, 1)));
            Assert.That(note.GetLine(), Is.EqualTo(2));
            Assert.That(note.GetJustification(), Is.EqualTo(TextJustification.Center));
            Assert.That(note.GetFontInfo(), Is.SameAs(font));
            Assert.That(note.GetWidth(), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Draw_RendersTextAtConfiguredLine()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 60, 200);
            stave.SetContext(ctx);

            var note = new TextNote(new TextNoteStruct
            {
                Text = "cue",
                Duration = "q",
                Line = 1,
                Font = new MetricsFontInfo { Family = "Arial", Size = 12 },
            });
            note.SetX(80).SetStave(stave).SetContext(ctx);

            note.Draw();

            Assert.That(ctx.HasCall("SetFont"), Is.True);
            Assert.That(ctx.GetCall("SetFont").Args[0], Is.EqualTo(12));
            Assert.That(ctx.HasCall("FillText"), Is.True);
            Assert.That(ctx.GetCall("FillText").Args[0], Is.EqualTo(80).Within(0.0001));
            Assert.That(ctx.GetCall("FillText").Args[1], Is.EqualTo(stave.GetYForLine(1)).Within(0.0001));
        }
    }
}

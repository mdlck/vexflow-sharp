using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("PedalMarking")]
    public class PedalMarkingTests
    {
        private static StaveNote Note(double x)
        {
            return new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
            }).SetX(x) as StaveNote ?? throw new AssertionException("Expected StaveNote");
        }

        private static (RecordingRenderContext Ctx, VexFlowSharp.Stave Stave, List<StaveNote> Notes) MakeSystem(params double[] xs)
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 300);
            stave.SetContext(ctx);

            var notes = xs.Select(Note).ToList();
            foreach (var note in notes)
                note.SetStave(stave);

            new Voice().SetMode(VoiceMode.SOFT).AddTickables(notes.Cast<Tickable>().ToList());

            return (ctx, stave, notes);
        }

        [Test]
        public void Constructor_UsesTextSustainDefaults()
        {
            var (_, _, notes) = MakeSystem(40, 120);
            var pedal = new PedalMarking(notes);

            Assert.That(pedal.GetCategory(), Is.EqualTo(PedalMarking.CATEGORY));
            Assert.That(pedal.GetPedalType(), Is.EqualTo(PedalMarkingType.Text));
            Assert.That(pedal.GetDepressText(), Is.EqualTo(PedalMarking.PedalDepressGlyph));
            Assert.That(pedal.GetReleaseText(), Is.EqualTo(PedalMarking.PedalReleaseGlyph));
            Assert.That(pedal.RenderOptions.BracketHeight, Is.EqualTo(Metrics.GetDouble("PedalMarking.bracketHeight")));
            Assert.That(pedal.RenderOptions.TextMarginRight, Is.EqualTo(Metrics.GetDouble("PedalMarking.textMarginRight")));
            Assert.That(pedal.RenderOptions.BracketLineWidth, Is.EqualTo(Metrics.GetDouble("PedalMarking.bracketLineWidth")));
        }

        [Test]
        public void Draw_TextMode_RendersDepressAndReleaseText()
        {
            var (ctx, _, notes) = MakeSystem(40, 220);
            var pedal = new PedalMarking(notes);
            pedal.SetContext(ctx);

            pedal.Draw();

            Assert.That(pedal.IsRendered(), Is.True);
            Assert.That(ctx.GetCalls("FillText").Count(), Is.EqualTo(2));
            Assert.That(ctx.HasCall("SetFont"), Is.True);
        }

        [Test]
        public void Draw_BracketMode_RendersStartAndEndBrackets()
        {
            var (ctx, _, notes) = MakeSystem(40, 220);
            var pedal = new PedalMarking(notes).SetType(PedalMarkingType.Bracket);
            pedal.SetContext(ctx);

            pedal.Draw();

            Assert.That(ctx.GetCalls("Stroke").Count(), Is.EqualTo(2));
            Assert.That(ctx.HasCall("SetLineWidth"), Is.True);
            Assert.That(ctx.GetCall("SetLineWidth").Args[0], Is.EqualTo(Metrics.GetDouble("PedalMarking.bracketLineWidth")));
            Assert.That(ctx.GetCalls("LineTo").Count(), Is.EqualTo(3));
        }

        [Test]
        public void Draw_MixedMode_StartsWithTextThenBracket()
        {
            var (ctx, _, notes) = MakeSystem(40, 220);
            var pedal = PedalMarking.CreateSostenuto(notes);
            pedal.SetContext(ctx);

            pedal.Draw();

            Assert.That(pedal.GetPedalType(), Is.EqualTo(PedalMarkingType.Mixed));
            Assert.That(pedal.GetDepressText(), Is.EqualTo("Sost. Ped."));
            Assert.That(ctx.GetCalls("FillText").Count(), Is.EqualTo(1));
            Assert.That(ctx.GetCalls("Stroke").Count(), Is.EqualTo(1));
        }

        [Test]
        public void CreateUnaCorda_UsesCustomReleaseTextFont()
        {
            var (ctx, _, notes) = MakeSystem(40, 120);
            var pedal = PedalMarking.CreateUnaCorda(notes);
            pedal.SetContext(ctx);

            pedal.Draw();

            Assert.That(pedal.GetDepressText(), Is.EqualTo("una corda"));
            Assert.That(pedal.GetReleaseText(), Is.EqualTo("tre corda"));
            Assert.That(ctx.GetCall("SetFont").Args[0], Is.EqualTo(Metrics.GetDouble("PedalMarking.text.fontSize")));
        }

        [Test]
        public void DrawBracketed_ThrowsWhenNotesAreOutOfOrder()
        {
            var (ctx, _, notes) = MakeSystem(120, 40);
            var pedal = new PedalMarking(notes).SetType("bracket");
            pedal.SetContext(ctx);

            Assert.Throws<VexFlowException>(() => pedal.Draw());
        }
    }
}

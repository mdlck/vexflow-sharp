using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Bend")]
    [Category("Modifiers")]
    public class BendTests
    {
        private static TabNote MakeTabNote()
        {
            var stave = new TabStave(10, 20, 300);
            var note = new TabNote(new TabNoteStruct
            {
                Duration = "4",
                Positions = new[] { new TabNotePosition { Str = 2, Fret = 7 } },
            });
            note.SetStave(stave).SetX(80);
            note.PreFormat();
            return note;
        }

        [Test]
        public void Constructor_UsesV5CategoryAndComputesWidth()
        {
            var bend = new Bend(new List<BendPhrase>
            {
                new BendPhrase { Type = Bend.UP, Text = "Full" },
            });

            Assert.That(bend.GetCategory(), Is.EqualTo(Bend.CATEGORY));
            Assert.That(bend.GetPosition(), Is.EqualTo(ModifierPosition.Right));
            Assert.That(bend.GetWidth(), Is.GreaterThan(0));
            Assert.That(bend.GetStyleLine().StrokeStyle, Is.EqualTo(Metrics.GetStyle("Bend.line").StrokeStyle));
        }

        [Test]
        public void Format_TabNoteConsumesRightShiftAndUsesStringLine()
        {
            var note = MakeTabNote();
            var bend = new Bend(new List<BendPhrase>
            {
                new BendPhrase { Type = Bend.UP, Text = "Full", Width = 12, DrawWidth = 6 },
            });
            note.AddModifier(bend);
            var state = new ModifierContextState();

            Bend.Format(new List<Bend> { bend }, state);

            Assert.That(bend.GetTextLine(), Is.EqualTo(note.LeastString() - 1));
            Assert.That(state.RightShift, Is.EqualTo(bend.GetWidth()).Within(0.0001));
            Assert.That(state.TopTextLine, Is.EqualTo(note.LeastString()));
        }

        [Test]
        public void Draw_RendersCurveArrowAndText()
        {
            var ctx = new RecordingRenderContext();
            var note = MakeTabNote();
            var bend = new Bend(new List<BendPhrase>
            {
                new BendPhrase { Type = Bend.UP, Text = "Full", Width = 12, DrawWidth = 6 },
            });
            note.AddModifier(bend);
            bend.SetContext(ctx);

            bend.Draw();

            Assert.That(ctx.HasCall("QuadraticCurveTo"), Is.True);
            Assert.That(ctx.HasCall("FillText"), Is.True);
            Assert.That(ctx.HasCall("Fill"), Is.True);
            Assert.That(ctx.GetCalls("SetStrokeStyle"), Is.Not.Empty);
            Assert.That(ctx.GetCalls("SetLineWidth"), Is.Not.Empty);
        }
    }
}

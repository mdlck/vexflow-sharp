using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Stroke")]
    [Category("Modifiers")]
    public class StrokeTests
    {
        private static StaveNote MakeNote()
        {
            var note = new StaveNote(new StaveNoteStruct
            {
                Keys = new[] { "c/4", "e/4", "g/4" },
                Duration = "4",
            });
            note.SetStave(new Stave(10, 20, 200));
            note.SetX(40);
            note.PreFormat();
            return note;
        }

        [Test]
        public void Constructor_DefaultsToLeftPosition()
        {
            var stroke = new Stroke(StrokeType.RollDown);

            Assert.That(stroke.GetCategory(), Is.EqualTo(Stroke.CATEGORY));
            Assert.That(stroke.GetPosition(), Is.EqualTo(ModifierPosition.Left));
            Assert.That(stroke.GetStrokeType(), Is.EqualTo(StrokeType.RollDown));
            Assert.That(stroke.GetWidth(), Is.EqualTo(10));
            Assert.That(stroke.GetAllVoices(), Is.True);
        }

        [Test]
        public void Format_ConsumesLeftShiftAndAccountsForDisplacedHead()
        {
            var note = MakeNote();
            note.SetLeftDisplacedHeadPx(4);
            var stroke = new Stroke(StrokeType.RollDown);
            note.AddModifier(stroke, 0);

            var state = new ModifierContextState { LeftShift = 3 };

            Stroke.Format(new System.Collections.Generic.List<Stroke> { stroke }, state);

            Assert.That(stroke.GetXShift(), Is.EqualTo(-7).Within(0.0001));
            Assert.That(state.LeftShift, Is.EqualTo(3 + stroke.GetWidth() + Metrics.GetDouble("Stroke.spacing")).Within(0.0001));
        }

        [Test]
        public void AddEndNote_StoresEndNote()
        {
            var note = MakeNote();
            var stroke = new Stroke(StrokeType.RollDown).AddEndNote(note);

            Assert.That(stroke.GetEndNote(), Is.SameAs(note));
        }

        [Test]
        public void Draw_BrushStrokeRendersFilledRectangle()
        {
            var ctx = new RecordingRenderContext();
            var note = MakeNote();
            var stroke = new Stroke(StrokeType.BrushDown);
            note.AddModifier(stroke, 0);
            stroke.SetContext(ctx);

            stroke.Draw();

            Assert.That(ctx.HasCall("FillRect"), Is.True);
            var rect = ctx.GetCall("FillRect").Args;
            Assert.That(rect[2], Is.EqualTo(1));
            Assert.That(rect[3], Is.GreaterThan(0));
        }

        [Test]
        public void Draw_RollStrokeUsesRotationAndGlyphOutlines()
        {
            var ctx = new RecordingRenderContext();
            var note = MakeNote();
            var stroke = new Stroke(StrokeType.RollDown);
            note.AddModifier(stroke, 0);
            stroke.SetContext(ctx);

            stroke.Draw();

            Assert.That(ctx.HasCall("OpenRotation"), Is.True);
            Assert.That(ctx.HasCall("CloseRotation"), Is.True);
            Assert.That(ctx.GetCalls("Fill").Count(), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Draw_RasgueadoStrokeRendersTextR()
        {
            var ctx = new RecordingRenderContext();
            var note = MakeNote();
            var stroke = new Stroke(StrokeType.RasgueadoDown);
            note.AddModifier(stroke, 0);
            stroke.SetContext(ctx);

            stroke.Draw();

            Assert.That(ctx.HasCall("SetFont"), Is.True);
            Assert.That(ctx.HasCall("FillText"), Is.True);
        }
    }
}

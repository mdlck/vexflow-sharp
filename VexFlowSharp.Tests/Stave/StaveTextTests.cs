using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.StaveTests
{
    [TestFixture]
    [Category("StaveText")]
    [Category("Stave")]
    public class StaveTextTests
    {
        [Test]
        public void Constructor_StoresTextPositionAndDefaultJustification()
        {
            var text = new StaveText("Fine", StaveModifierPosition.Above);

            Assert.That(text.GetText(), Is.EqualTo("Fine"));
            Assert.That(text.GetPosition(), Is.EqualTo(StaveModifierPosition.Above));
            Assert.That(text.GetJustification(), Is.EqualTo(TextJustification.Center));
            Assert.That(text.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void SetText_UpdatesWidth()
        {
            var text = new StaveText("A", StaveModifierPosition.Above);
            double oldWidth = text.GetWidth();

            text.SetText("Longer text");

            Assert.That(text.GetText(), Is.EqualTo("Longer text"));
            Assert.That(text.GetWidth(), Is.GreaterThan(oldWidth));
        }

        [Test]
        public void Options_ApplyShiftsAndJustification()
        {
            var text = new StaveText("Fine", StaveModifierPosition.Below, new StaveTextOptions
            {
                ShiftX = 3,
                ShiftY = -2,
                Justification = TextJustification.Right,
            });

            Assert.That(text.GetXShift(), Is.EqualTo(3));
            Assert.That(text.GetYShift(), Is.EqualTo(-2));
            Assert.That(text.GetJustification(), Is.EqualTo(TextJustification.Right));
        }

        [Test]
        public void Draw_AboveCenterJustifiesTextOverStave()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 200);
            stave.SetContext(ctx);
            var text = new StaveText("Fine", StaveModifierPosition.Above);

            text.Draw(stave, 0);

            Assert.That(ctx.HasCall("SetFont"), Is.True);
            Assert.That(ctx.GetCall("SetFont").Args[0], Is.EqualTo(Metrics.GetDouble("StaveText.fontSize")));
            var fill = ctx.GetCall("FillText").Args;
            Assert.That(fill[0], Is.EqualTo(stave.GetX() + stave.GetWidth() / 2 - text.GetWidth() / 2).Within(0.0001));
            Assert.That(fill[1], Is.EqualTo(stave.GetYForTopText(2) + 4).Within(0.0001));
        }

        [Test]
        public void Draw_RightPositionPlacesTextOutsideStave()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 200);
            stave.SetContext(ctx);
            var text = new StaveText("Solo", StaveModifierPosition.Right);

            text.Draw(stave, 0);

            var fill = ctx.GetCall("FillText").Args;
            Assert.That(fill[0], Is.EqualTo(stave.GetX() + stave.GetWidth() + 24).Within(0.0001));
            Assert.That(fill[1], Is.EqualTo((stave.GetYForLine(0) + stave.GetYForLine(4)) / 2 + 4).Within(0.0001));
        }

        [Test]
        public void Draw_LeftPositionHonorsShift()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 200);
            stave.SetContext(ctx);
            var text = new StaveText("Solo", StaveModifierPosition.Left, new StaveTextOptions
            {
                ShiftX = 5,
                ShiftY = 6,
            });

            text.Draw(stave, 0);

            var fill = ctx.GetCall("FillText").Args;
            Assert.That(fill[0], Is.EqualTo(stave.GetX() - text.GetWidth() - 24 + 5).Within(0.0001));
            Assert.That(fill[1], Is.EqualTo((stave.GetYForLine(0) + stave.GetYForLine(4)) / 2 + 6 + 4).Within(0.0001));
        }
    }
}

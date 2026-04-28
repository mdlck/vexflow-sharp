using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.StaveTests
{
    [TestFixture]
    [Category("Repetition")]
    [Category("Stave")]
    public class RepetitionTests
    {
        [Test]
        public void Constructor_StoresTypeAndShifts()
        {
            var repetition = new Repetition(RepetitionType.DC, x: 7, yShift: -3).SetShiftX(2);

            Assert.That(repetition.GetRepetitionType(), Is.EqualTo(RepetitionType.DC));
            Assert.That(repetition.GetShiftX(), Is.EqualTo(2));
            Assert.That(repetition.GetShiftY(), Is.EqualTo(-3));
            Assert.That(repetition.GetPosition(), Is.EqualTo(StaveModifierPosition.Above));
        }

        [Test]
        public void Stave_DefaultVerticalBarWidthMatchesV5()
        {
            var stave = new VexFlowSharp.Stave(10, 60, 200);

            Assert.That(stave.GetVerticalBarWidth(), Is.EqualTo(10));
        }

        [Test]
        public void Draw_DC_RendersTextWithMetricFont()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 200);
            stave.SetContext(ctx);
            var repetition = new Repetition(RepetitionType.DC);

            repetition.Draw(stave, 0);

            Assert.That(ctx.HasCall("SetFont"), Is.True);
            Assert.That(ctx.GetCall("SetFont").Args[0], Is.EqualTo(Metrics.GetDouble("Repetition.text.fontSize")));
            Assert.That(ctx.HasCall("FillText"), Is.True);
        }

        [Test]
        public void Draw_CodaRight_RendersCodaGlyph()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 200);
            stave.SetContext(ctx);
            var repetition = new Repetition(RepetitionType.CodaRight);

            repetition.Draw(stave, 0);

            Assert.That(ctx.HasCall("Fill"), Is.True);
            Assert.That(ctx.HasCall("FillText"), Is.False);
        }

        [Test]
        public void Draw_DCAlCoda_RendersTextAndCodaGlyph()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 200);
            stave.SetContext(ctx);
            var repetition = new Repetition(RepetitionType.DCAlCoda);

            repetition.Draw(stave, 0);

            Assert.That(ctx.HasCall("FillText"), Is.True);
            Assert.That(ctx.HasCall("Fill"), Is.True);
        }
    }
}

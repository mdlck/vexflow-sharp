using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.StaveTests
{
    [TestFixture]
    [Category("StaveTempo")]
    [Category("Stave")]
    public class StaveTempoTests
    {
        [Test]
        public void Constructor_StoresTempoAndDefaultShifts()
        {
            var options = new StaveTempoOptions { Name = "Allegro", Duration = "q", Bpm = 120 };
            var tempo = new StaveTempo(options, x: 7, shiftY: -3);

            Assert.That(tempo.GetTempo(), Is.SameAs(options));
            Assert.That(tempo.GetX(), Is.EqualTo(7));
            Assert.That(tempo.GetXShift(), Is.EqualTo(Metrics.GetDouble("StaveTempo.xShift")));
            Assert.That(tempo.GetYShift(), Is.EqualTo(-3));
            Assert.That(tempo.GetPosition(), Is.EqualTo(StaveModifierPosition.Above));
        }

        [Test]
        public void Width_UsesMetricSpacingForMetronomeDots()
        {
            var plain = new StaveTempo(new StaveTempoOptions
            {
                Duration = "q",
                Bpm = 120,
            });
            var dotted = new StaveTempo(new StaveTempoOptions
            {
                Duration = "q",
                Dots = 1,
                Bpm = 120,
            });

            double expectedExtra = Glyph.GetWidth("metAugmentationDot", Metrics.GetDouble("StaveTempo.glyph.fontSize"))
                + Metrics.GetDouble("StaveTempo.spacing");

            Assert.That(dotted.GetWidth() - plain.GetWidth(), Is.EqualTo(expectedExtra).Within(0.0001));
        }

        [Test]
        public void Draw_NameDurationAndBpm_RendersTextAndGlyphs()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 220);
            stave.SetContext(ctx);
            var tempo = new StaveTempo(new StaveTempoOptions
            {
                Name = "Allegro",
                Duration = "q",
                Bpm = 120,
            });

            tempo.Draw(stave, 0);

            Assert.That(ctx.GetCalls("SetFont").Any(c => c.Args[0] == Metrics.GetDouble("StaveTempo.name.fontSize")), Is.True);
            Assert.That(ctx.GetCalls("FillText").Count(), Is.GreaterThanOrEqualTo(4));
            Assert.That(ctx.HasCall("Fill"), Is.True);
        }

        [Test]
        public void Draw_ParenthesizedDottedMetricModulation_RendersBothNoteGlyphsAndDots()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 220);
            stave.SetContext(ctx);
            var tempo = new StaveTempo(new StaveTempoOptions
            {
                Parenthesis = true,
                Duration = "8",
                Dots = 1,
                Duration2 = "16",
                Dots2 = 1,
            });

            tempo.Draw(stave, 0);

            Assert.That(ctx.GetCalls("Fill").Count(), Is.GreaterThanOrEqualTo(4));
            Assert.That(ctx.GetCalls("FillText").Count(), Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void Draw_InvalidDuration_Throws()
        {
            Assert.Throws<VexFlowException>(() =>
                new StaveTempo(new StaveTempoOptions { Duration = "not-a-duration" }));
        }
    }
}

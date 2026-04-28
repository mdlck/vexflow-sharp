using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.StaveTests
{
    [TestFixture]
    [Category("StaveSection")]
    [Category("Stave")]
    public class StaveSectionTests
    {
        [Test]
        public void Constructor_UsesMetricsAndDefaultsToDrawingRectangle()
        {
            var section = new StaveSection("A");

            Assert.That(section.GetText(), Is.EqualTo("A"));
            Assert.That(section.GetDrawRect(), Is.True);
            Assert.That(section.GetPosition(), Is.EqualTo(StaveModifierPosition.Above));
            Assert.That(section.GetWidth(), Is.GreaterThan(0));
            Assert.That(section.GetHeight(), Is.EqualTo(Metrics.GetDouble("StaveSection.fontSize")));
            Assert.That(section.GetPadding(2), Is.EqualTo(Metrics.GetDouble("StaveSection.padding")));
        }

        [Test]
        public void SetText_UpdatesWidth()
        {
            var section = new StaveSection("A");
            double oldWidth = section.GetWidth();

            section.SetText("Section A");

            Assert.That(section.GetText(), Is.EqualTo("Section A"));
            Assert.That(section.GetWidth(), Is.GreaterThan(oldWidth));
        }

        [Test]
        public void SetDrawRect_TogglesRectangleRendering()
        {
            var section = new StaveSection("B").SetDrawRect(false);

            Assert.That(section.GetDrawRect(), Is.False);
        }

        [Test]
        public void Draw_RendersRectAndTextWithMetricFont()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 200);
            stave.SetContext(ctx);
            var section = new StaveSection("A", x: 4, yShift: -2);

            section.Draw(stave, 0);

            Assert.That(ctx.HasCall("SetFont"), Is.True);
            Assert.That(ctx.GetCall("SetFont").Args[0], Is.EqualTo(Metrics.GetDouble("StaveSection.fontSize")));
            Assert.That(ctx.HasCall("Rect"), Is.True);
            Assert.That(ctx.HasCall("Stroke"), Is.True);
            Assert.That(ctx.HasCall("FillText"), Is.True);
        }

        [Test]
        public void Draw_WhenRectDisabled_RendersOnlyText()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 200);
            stave.SetContext(ctx);
            var section = new StaveSection("A").SetDrawRect(false);

            section.Draw(stave, 0);

            Assert.That(ctx.HasCall("FillText"), Is.True);
            Assert.That(ctx.HasCall("Rect"), Is.False);
        }
    }
}

using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.StaveTests
{
    [TestFixture]
    [Category("Volta")]
    [Category("Stave")]
    public class VoltaTests
    {
        [Test]
        public void Constructor_StoresTypeLabelAndMeasuresWidth()
        {
            var volta = new Volta(VoltaType.Begin, "1.", x: 10, yShift: -2);

            Assert.That(volta.GetVoltaType(), Is.EqualTo(VoltaType.Begin));
            Assert.That(volta.GetText(), Is.EqualTo("1."));
            Assert.That(volta.GetYShift(), Is.EqualTo(-2));
            Assert.That(volta.GetPosition(), Is.EqualTo(StaveModifierPosition.Above));
            Assert.That(volta.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void Draw_BeginVolta_RendersLeftPostLabelAndHorizontalLine()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 200);
            stave.SetContext(ctx);
            var volta = new Volta(VoltaType.Begin, "1.", x: 10);

            volta.Draw(stave, 0);

            Assert.That(ctx.GetCalls("FillRect").Count(), Is.EqualTo(2));
            Assert.That(ctx.HasCall("SetFont"), Is.True);
            Assert.That(ctx.GetCall("SetFont").Args[0], Is.EqualTo(Metrics.GetDouble("Volta.fontSize")));
            Assert.That(ctx.HasCall("FillText"), Is.True);
            Assert.That(ctx.GetCall("FillText").Args[0], Is.EqualTo(10 + Metrics.GetDouble("Volta.textXOffset")).Within(0.0001));
            Assert.That(ctx.GetCall("FillText").Args[1], Is.EqualTo(stave.GetYForTopText(stave.GetNumLines()) + Metrics.GetDouble("Volta.textYOffset")).Within(0.0001));
            Assert.That(ctx.GetCall("FillRect").Args[2], Is.EqualTo(Metrics.GetDouble("Volta.lineWidth")).Within(0.0001));
            Assert.That(ctx.GetCall("FillRect").Args[3], Is.EqualTo(Metrics.GetDouble("Volta.verticalHeightLines") * stave.GetSpacingBetweenLines()).Within(0.0001));
        }

        [Test]
        public void Draw_EndVolta_RendersRightPostAndNoLabel()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 200);
            stave.SetContext(ctx);
            var volta = new Volta(VoltaType.End, "2.", x: 10);

            volta.Draw(stave, 0);

            Assert.That(ctx.GetCalls("FillRect").Count(), Is.EqualTo(2));
            Assert.That(ctx.HasCall("FillText"), Is.False);
            Assert.That(ctx.GetCall("FillRect").Args[0], Is.EqualTo(10 + 200 - Metrics.GetDouble("Volta.endAdjustment")).Within(0.0001));
        }

        [Test]
        public void Draw_BeginEndVolta_RendersBothPosts()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 200);
            stave.SetContext(ctx);
            var volta = new Volta(VoltaType.BeginEnd, "1-2.", x: 10);

            volta.Draw(stave, 0);

            Assert.That(ctx.GetCalls("FillRect").Count(), Is.EqualTo(3));
            Assert.That(ctx.HasCall("FillText"), Is.True);
            Assert.That(ctx.GetCalls("FillRect").ElementAt(1).Args[0], Is.EqualTo(10 + 200 - Metrics.GetDouble("Volta.beginEndAdjustment")).Within(0.0001));
        }
    }
}

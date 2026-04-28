using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Elements
{
    [TestFixture]
    [Category("MultiMeasureRest")]
    public class MultiMeasureRestTests
    {
        [Test]
        public void Constructor_StoresNumberAndDefaultOptions()
        {
            var rest = new MultiMeasureRest(12);

            Assert.That(rest.GetNumberOfMeasures(), Is.EqualTo(12));
            Assert.That(rest.RenderOptions.ShowNumber, Is.True);
            Assert.That(rest.RenderOptions.UseSymbols, Is.False);
            Assert.That(rest.RenderOptions.NumberLine, Is.EqualTo(Metrics.GetDouble("MultiMeasureRest.numberLine")));
            Assert.That(rest.RenderOptions.Line, Is.EqualTo(Metrics.GetDouble("MultiMeasureRest.line")));
            Assert.That(rest.RenderOptions.SpacingBetweenLinesPx, Is.EqualTo(Metrics.GetDouble("MultiMeasureRest.spacingBetweenLinesPx")));
            Assert.That(rest.RenderOptions.SemibreveRestGlyphScale, Is.EqualTo(Metrics.GetDouble("MultiMeasureRest.semibreveRestGlyphScale")));
            Assert.That(rest.RenderOptions.LineThickness, Is.EqualTo(Metrics.GetDouble("MultiMeasureRest.lineThickness")));
            Assert.That(rest.RenderOptions.SerifThickness, Is.EqualTo(Metrics.GetDouble("MultiMeasureRest.serifThickness")));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.linePaddingRatio"), Is.EqualTo(0.1));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.centerRatio"), Is.EqualTo(0.5));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.symbolLineOffset"), Is.EqualTo(-1));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.numberBaselineRatio"), Is.EqualTo(0.5));
            Assert.That(rest.GetCategory(), Is.EqualTo(MultiMeasureRest.CATEGORY));
        }

        [Test]
        public void Draw_LineMode_RendersHBarAndNumberGlyphs()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 260);
            stave.SetContext(ctx);
            var rest = new MultiMeasureRest(8);
            rest.SetStave(stave).SetContext(ctx);

            rest.Draw();

            Assert.That(rest.IsRendered(), Is.True);
            Assert.That(rest.GetXs().Left, Is.LessThan(rest.GetXs().Right));
            Assert.That(ctx.GetCalls("Fill").Count, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void Draw_SymbolMode_RendersRestSymbolsAndCanHideNumber()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 260);
            stave.SetContext(ctx);
            var rest = new MultiMeasureRest(7, new MultiMeasureRestRenderOptions
            {
                UseSymbols = true,
                ShowNumber = false,
                SymbolSpacing = 3,
            });
            rest.SetStave(stave).SetContext(ctx);

            rest.Draw();

            Assert.That(rest.IsRendered(), Is.True);
            Assert.That(ctx.GetCalls("Fill").Count, Is.EqualTo(3));
        }

        [Test]
        public void Draw_UsesExplicitPaddingWhenProvided()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 260);
            stave.SetContext(ctx);
            var rest = new MultiMeasureRest(4, new MultiMeasureRestRenderOptions
            {
                PaddingLeft = 20,
                PaddingRight = 30,
            });
            rest.SetStave(stave).SetContext(ctx);

            rest.Draw();

            Assert.That(rest.GetXs().Left, Is.EqualTo(30));
            Assert.That(rest.GetXs().Right, Is.EqualTo(240));
        }
    }
}

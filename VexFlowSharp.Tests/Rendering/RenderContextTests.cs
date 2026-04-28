using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Rendering
{
    [TestFixture]
    [Category("Rendering")]
    [Category("Phase5")]
    public class RenderContextTests
    {
        [Test]
        public void Category_IsV5RenderContext()
        {
            Assert.That(RenderContext.CATEGORY, Is.EqualTo("RenderContext"));
        }

        [Test]
        public void RecordingContext_CoversV5PointerAndAddSurface()
        {
            var ctx = new RecordingRenderContext();
            var child = new object();

            Assert.That(ctx.PointerRect(1, 2, 3, 4), Is.SameAs(ctx));
            Assert.That(ctx.Fill(new { stroke = "#000" }), Is.SameAs(ctx));
            ctx.OpenGroup("note", "n1");
            ctx.CloseGroup();
            ctx.Add(child);

            Assert.That(ctx.GetCall("PointerRect").Args, Is.EqualTo(new[] { 1.0, 2.0, 3.0, 4.0 }));
            Assert.That(ctx.HasCall("Fill"), Is.True);
            Assert.That(ctx.HasCall("OpenGroup"), Is.True);
            Assert.That(ctx.HasCall("CloseGroup"), Is.True);
            Assert.That(ctx.HasCall("Add"), Is.True);
        }

        [Test]
        public void SkiaContext_SaveRestoreRestoresFontState()
        {
            using var ctx = new SkiaRenderContext(120, 60);
            ctx.SetFont("Arial", 10, "normal", "normal");

            ctx.Save();
            ctx.SetFont("Times New Roman", 24, "bold", "italic");
            ctx.Restore();

            Assert.That(ctx.GetFont(), Is.EqualTo("normal normal 10px Arial"));
        }

        [Test]
        public void RenderContext_ExposesV5StyleProperties()
        {
            var ctx = new RecordingRenderContext();

            ctx.FillStyle = "#123456";
            ctx.StrokeStyle = "#abcdef";

            Assert.That(ctx.FillStyle, Is.EqualTo("#123456"));
            Assert.That(ctx.StrokeStyle, Is.EqualTo("#abcdef"));
            Assert.That(ctx.HasCall("SetFillStyle"), Is.True);
            Assert.That(ctx.HasCall("SetStrokeStyle"), Is.True);
        }

        [Test]
        public void SkiaContext_SaveRestoreRestoresStyleProperties()
        {
            using var ctx = new SkiaRenderContext(120, 60);
            ctx.FillStyle = "#111111";
            ctx.StrokeStyle = "#222222";

            ctx.Save();
            ctx.FillStyle = "#333333";
            ctx.StrokeStyle = "#444444";
            ctx.Restore();

            Assert.That(ctx.FillStyle, Is.EqualTo("#111111"));
            Assert.That(ctx.StrokeStyle, Is.EqualTo("#222222"));
        }
    }
}

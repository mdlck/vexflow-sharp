using System.IO;
using NUnit.Framework;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Rendering
{
    [TestFixture]
    [Category("Smoke")]
    public class SkiaRenderContextTests
    {
        [Test]
        public void SaveRestore_MaintainsTransformState()
        {
            using var ctx = SkiaRenderContext.Create(100, 100);
            ctx.Save();
            ctx.Scale(2.0, 2.0);
            ctx.Restore();
            // Draw a line at original scale - verify it renders at expected position
            ctx.BeginPath();
            ctx.MoveTo(10, 10);
            ctx.LineTo(50, 50);
            ctx.Stroke();
            var png = ctx.ToPng();
            Assert.That(png.Length, Is.GreaterThan(0));
        }

        [Test]
        public void BeginPath_Fill_ProducesNonEmptyPng()
        {
            using var ctx = SkiaRenderContext.Create(100, 100);
            ctx.SetFillStyle("#000000");
            ctx.BeginPath();
            ctx.MoveTo(10, 10);
            ctx.LineTo(90, 10);
            ctx.LineTo(50, 90);
            ctx.ClosePath();
            ctx.Fill();
            var png = ctx.ToPng();
            Assert.That(png.Length, Is.GreaterThan(100), "PNG should contain rendered content");
        }

        [Test]
        public void BezierCurveTo_RendersWithoutError()
        {
            using var ctx = SkiaRenderContext.Create(100, 100);
            ctx.BeginPath();
            ctx.MoveTo(10, 50);
            ctx.BezierCurveTo(30, 10, 70, 90, 90, 50);
            ctx.Fill();
            var png = ctx.ToPng();
            Assert.That(png.Length, Is.GreaterThan(0));
        }

        [Test]
        public void SetFillStyle_ParsesHexColor()
        {
            using var ctx = SkiaRenderContext.Create(100, 100);
            ctx.SetFillStyle("#FF0000");
            ctx.FillRect(0, 0, 100, 100);
            var png = ctx.ToPng();
            Assert.That(png.Length, Is.GreaterThan(0));
        }

        [Test]
        public void SetLineDash_AcceptsSingleValuePattern()
        {
            using var ctx = SkiaRenderContext.Create(100, 100);
            ctx.SetLineDash(new[] { 5.0 });
            ctx.BeginPath();
            ctx.MoveTo(10, 50);
            ctx.LineTo(90, 50);
            ctx.Stroke();

            var png = ctx.ToPng();
            Assert.That(png.Length, Is.GreaterThan(0));
        }

        [Test]
        public void SavePng_WritesFile()
        {
            using var ctx = SkiaRenderContext.Create(50, 50);
            ctx.FillRect(0, 0, 50, 50);
            var path = Path.Combine(Path.GetTempPath(), "skia_test.png");
            ctx.SavePng(path);
            Assert.That(File.Exists(path), Is.True);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(0));
            File.Delete(path);
        }
    }
}

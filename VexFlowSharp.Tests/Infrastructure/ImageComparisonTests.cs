using System;
using NUnit.Framework;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Infrastructure
{
    /// <summary>
    /// Tests for the ImageComparison utility.
    /// Verifies: 0% diff for identical PNGs, >0% diff for different PNGs,
    /// 2% threshold constant, dimension mismatch throws, AssertImagesMatch behavior.
    /// </summary>
    [TestFixture]
    [Category("ImageCompare")]
    public class ImageComparisonTests
    {
        [Test]
        public void IdenticalImages_ReturnZeroDiff()
        {
            using var ctx = SkiaRenderContext.Create(100, 100);
            ctx.SetFillStyle("#000000");
            ctx.FillRect(10, 10, 30, 30);
            var png = ctx.ToPng();
            double diff = ImageComparison.PixelDiffPercentage(png, png);
            Assert.That(diff, Is.EqualTo(0.0));
        }

        [Test]
        public void DifferentImages_ReturnPositiveDiff()
        {
            // Image A: white background with black square at top-left
            using var ctxA = SkiaRenderContext.Create(100, 100);
            ctxA.SetFillStyle("#000000");
            ctxA.FillRect(10, 10, 30, 30);
            var pngA = ctxA.ToPng();

            // Image B: white background with black square at bottom-right
            using var ctxB = SkiaRenderContext.Create(100, 100);
            ctxB.SetFillStyle("#000000");
            ctxB.FillRect(60, 60, 30, 30);
            var pngB = ctxB.ToPng();

            double diff = ImageComparison.PixelDiffPercentage(pngA, pngB);
            Assert.That(diff, Is.GreaterThan(0.0));
        }

        [Test]
        public void AssertImagesMatch_IdenticalImages_Passes()
        {
            using var ctx = SkiaRenderContext.Create(50, 50);
            ctx.FillRect(0, 0, 50, 50);
            var png = ctx.ToPng();
            Assert.DoesNotThrow(() => ImageComparisonAssert.AssertImagesMatch(png, png));
        }

        [Test]
        public void AssertImagesMatch_VeryDifferentImages_FailsAt2Percent()
        {
            // Image A: all white (default background)
            using var ctxA = SkiaRenderContext.Create(100, 100);
            var pngA = ctxA.ToPng();

            // Image B: all black
            using var ctxB = SkiaRenderContext.Create(100, 100);
            ctxB.SetFillStyle("#000000");
            ctxB.FillRect(0, 0, 100, 100);
            var pngB = ctxB.ToPng();

            double diff = ImageComparison.PixelDiffPercentage(pngA, pngB);
            Assert.That(diff, Is.GreaterThan(2.0), "All-white vs all-black should exceed 2% threshold");
        }

        [Test]
        public void MismatchedDimensions_ThrowsArgumentException()
        {
            using var ctxA = SkiaRenderContext.Create(100, 100);
            using var ctxB = SkiaRenderContext.Create(200, 200);
            var pngA = ctxA.ToPng();
            var pngB = ctxB.ToPng();
            Assert.Throws<ArgumentException>(() => ImageComparison.PixelDiffPercentage(pngA, pngB));
        }

        [Test]
        public void DefaultThreshold_IsTwo()
        {
            Assert.That(ImageComparison.DefaultThresholdPercent, Is.EqualTo(2.0));
        }
    }
}

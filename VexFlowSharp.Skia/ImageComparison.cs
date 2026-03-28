using System;
using System.IO;
using SkiaSharp;

namespace VexFlowSharp.Skia
{
    /// <summary>
    /// Pixel-level image comparison utility for PNG regression tests.
    /// Used to verify that C# SkiaSharp rendering output matches VexFlow reference PNGs.
    /// No NUnit dependency — use ImageComparisonAssert in the test project for assertion helpers.
    /// </summary>
    public static class ImageComparison
    {
        /// <summary>Maximum allowed pixel difference percentage before a test is considered failing.</summary>
        public const double DefaultThresholdPercent = 2.0;  // TEST-04: 2% max

        /// <summary>Per-channel tolerance to accommodate minor anti-aliasing differences.</summary>
        public const int PerChannelTolerance = 5;

        /// <summary>
        /// Computes the percentage of pixels that differ between two PNG byte arrays.
        /// Pixels are considered different if any R, G, or B channel differs by more than PerChannelTolerance.
        /// </summary>
        /// <param name="actualPng">PNG bytes of the actual rendered image.</param>
        /// <param name="referencePng">PNG bytes of the reference image.</param>
        /// <returns>Percentage of differing pixels (0.0–100.0).</returns>
        /// <exception cref="ArgumentException">Thrown if the two images have different dimensions.</exception>
        public static double PixelDiffPercentage(byte[] actualPng, byte[] referencePng)
        {
            using var actualBitmap = SKBitmap.Decode(actualPng);
            using var referenceBitmap = SKBitmap.Decode(referencePng);

            if (actualBitmap.Width != referenceBitmap.Width || actualBitmap.Height != referenceBitmap.Height)
                throw new ArgumentException(
                    $"Bitmap dimensions differ: actual={actualBitmap.Width}x{actualBitmap.Height}, " +
                    $"reference={referenceBitmap.Width}x{referenceBitmap.Height}");

            int totalPixels = actualBitmap.Width * actualBitmap.Height;
            int diffPixels = 0;
            for (int y = 0; y < actualBitmap.Height; y++)
            {
                for (int x = 0; x < actualBitmap.Width; x++)
                {
                    var a = actualBitmap.GetPixel(x, y);
                    var r = referenceBitmap.GetPixel(x, y);
                    if (Math.Abs(a.Red - r.Red) > PerChannelTolerance ||
                        Math.Abs(a.Green - r.Green) > PerChannelTolerance ||
                        Math.Abs(a.Blue - r.Blue) > PerChannelTolerance ||
                        Math.Abs(a.Alpha - r.Alpha) > PerChannelTolerance)
                    {
                        diffPixels++;
                    }
                }
            }
            return (double)diffPixels / totalPixels * 100.0;
        }

        /// <summary>
        /// Computes the percentage of pixels that differ between two PNG files on disk.
        /// </summary>
        /// <param name="actualPath">File path to the actual rendered PNG.</param>
        /// <param name="referencePath">File path to the reference PNG.</param>
        /// <returns>Percentage of differing pixels (0.0–100.0).</returns>
        public static double PixelDiffPercentage(string actualPath, string referencePath)
        {
            return PixelDiffPercentage(File.ReadAllBytes(actualPath), File.ReadAllBytes(referencePath));
        }
    }
}

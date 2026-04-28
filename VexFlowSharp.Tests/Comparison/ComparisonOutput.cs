// VexFlowSharp - C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using SkiaSharp;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Comparison
{
    internal static class ComparisonOutput
    {
        public sealed record ImagePairDiagnostic(
            string Name,
            string ActualPath,
            string ReferencePath,
            int Width,
            int Height,
            double DiffPercent,
            SKRectI? ActualInkBounds,
            SKRectI? ReferenceInkBounds,
            string? HeatmapPath)
        {
            public string ToReportLine()
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0,-28} {1}x{2,-4} diff={3,6:F2}% vf_bbox={4} vx_bbox={5}{6}",
                    Name,
                    Width,
                    Height,
                    DiffPercent,
                    FormatBounds(ActualInkBounds),
                    FormatBounds(ReferenceInkBounds),
                    HeatmapPath is null ? string.Empty : $" heatmap={HeatmapPath}");
            }
        }

        public static void CopyReferenceImage(string referenceImagesDir, string outputDir, string filename)
        {
            Directory.CreateDirectory(outputDir);

            string sourcePath = Path.Combine(referenceImagesDir, filename);
            string outputPath = Path.Combine(outputDir, filename);

            Assert.That(File.Exists(sourcePath), Is.True, $"Reference PNG missing: {sourcePath}");
            File.Copy(sourcePath, outputPath, overwrite: true);
            Assert.That(File.Exists(outputPath), Is.True, $"Reference PNG must exist at {outputPath}");
            Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0), "Reference PNG must be non-zero bytes");
        }

        public static IReadOnlyList<ImagePairDiagnostic> DiagnosePairedImages(
            string outputDir,
            bool writeHeatmaps = false)
        {
            Assert.That(Directory.Exists(outputDir), Is.True, $"Comparison output directory missing: {outputDir}");

            var diagnostics = new List<ImagePairDiagnostic>();
            foreach (string actualPath in Directory.EnumerateFiles(outputDir, "*-vfsharp.png").OrderBy(static p => p))
            {
                string filename = Path.GetFileName(actualPath);
                string referencePath = Path.Combine(outputDir, filename.Replace("-vfsharp.png", "-vexflow.png"));
                if (!File.Exists(referencePath)) continue;

                string name = filename[..^"-vfsharp.png".Length];
                diagnostics.Add(DiagnosePair(name, actualPath, referencePath, writeHeatmaps));
            }

            return diagnostics
                .OrderByDescending(static d => d.DiffPercent)
                .ThenBy(static d => d.Name, StringComparer.Ordinal)
                .ToList();
        }

        private static ImagePairDiagnostic DiagnosePair(
            string name,
            string actualPath,
            string referencePath,
            bool writeHeatmap)
        {
            using var actual = DecodeOnWhite(actualPath);
            using var reference = DecodeOnWhite(referencePath);

            Assert.That(actual.Width, Is.EqualTo(reference.Width),
                $"Image width differs for {name}: actual={actual.Width}, reference={reference.Width}");
            Assert.That(actual.Height, Is.EqualTo(reference.Height),
                $"Image height differs for {name}: actual={actual.Height}, reference={reference.Height}");

            var actualInkBounds = GetInkBounds(actual);
            var referenceInkBounds = GetInkBounds(reference);
            double diffPercent = PixelDiffPercentage(actual, reference);
            string? heatmapPath = writeHeatmap ? WriteHeatmap(name, actual, reference, actualPath) : null;

            return new ImagePairDiagnostic(
                name,
                actualPath,
                referencePath,
                actual.Width,
                actual.Height,
                diffPercent,
                actualInkBounds,
                referenceInkBounds,
                heatmapPath);
        }

        private static SKBitmap DecodeOnWhite(string path)
        {
            using var decoded = SKBitmap.Decode(path);
            var normalized = new SKBitmap(decoded.Width, decoded.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(normalized);
            canvas.Clear(SKColors.White);
            canvas.DrawBitmap(decoded, 0, 0);
            return normalized;
        }

        private static double PixelDiffPercentage(SKBitmap actual, SKBitmap reference)
        {
            int totalPixels = actual.Width * actual.Height;
            int diffPixels = 0;

            for (int y = 0; y < actual.Height; y++)
            {
                for (int x = 0; x < actual.Width; x++)
                {
                    var a = actual.GetPixel(x, y);
                    var r = reference.GetPixel(x, y);
                    if (Math.Abs(a.Red - r.Red) > ImageComparison.PerChannelTolerance ||
                        Math.Abs(a.Green - r.Green) > ImageComparison.PerChannelTolerance ||
                        Math.Abs(a.Blue - r.Blue) > ImageComparison.PerChannelTolerance ||
                        Math.Abs(a.Alpha - r.Alpha) > ImageComparison.PerChannelTolerance)
                    {
                        diffPixels++;
                    }
                }
            }

            return (double)diffPixels / totalPixels * 100.0;
        }

        private static SKRectI? GetInkBounds(SKBitmap bitmap)
        {
            int minX = bitmap.Width;
            int minY = bitmap.Height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    if (Math.Abs(color.Red - 255) <= ImageComparison.PerChannelTolerance &&
                        Math.Abs(color.Green - 255) <= ImageComparison.PerChannelTolerance &&
                        Math.Abs(color.Blue - 255) <= ImageComparison.PerChannelTolerance &&
                        Math.Abs(color.Alpha - 255) <= ImageComparison.PerChannelTolerance)
                    {
                        continue;
                    }

                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }

            return maxX < 0 ? null : new SKRectI(minX, minY, maxX + 1, maxY + 1);
        }

        private static string WriteHeatmap(string name, SKBitmap actual, SKBitmap reference, string actualPath)
        {
            string heatmapPath = Path.Combine(
                Path.GetDirectoryName(actualPath)!,
                $"{name}-diff.png");

            using var heatmap = new SKBitmap(actual.Width, actual.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            for (int y = 0; y < actual.Height; y++)
            {
                for (int x = 0; x < actual.Width; x++)
                {
                    var a = actual.GetPixel(x, y);
                    var r = reference.GetPixel(x, y);
                    int delta = Math.Max(
                        Math.Max(Math.Abs(a.Red - r.Red), Math.Abs(a.Green - r.Green)),
                        Math.Max(Math.Abs(a.Blue - r.Blue), Math.Abs(a.Alpha - r.Alpha)));

                    heatmap.SetPixel(x, y, delta <= ImageComparison.PerChannelTolerance
                        ? new SKColor(255, 255, 255)
                        : new SKColor(255, (byte)Math.Max(0, 255 - delta), 0));
                }
            }

            using var image = SKImage.FromBitmap(heatmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(heatmapPath);
            data.SaveTo(stream);
            return heatmapPath;
        }

        private static string FormatBounds(SKRectI? bounds)
        {
            if (bounds is not { } b) return "none";
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0},{1},{2},{3})",
                b.Left,
                b.Top,
                b.Right,
                b.Bottom);
        }
    }
}

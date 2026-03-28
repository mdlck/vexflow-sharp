using NUnit.Framework;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Infrastructure;

/// <summary>
/// NUnit assertion helper for image comparison.
/// Wraps ImageComparison.PixelDiffPercentage with NUnit Assert semantics.
/// Kept in the test project because it depends on NUnit.Framework.Assert.
/// </summary>
public static class ImageComparisonAssert
{
    public static void AssertImagesMatch(byte[] actual, byte[] reference, double thresholdPercent = ImageComparison.DefaultThresholdPercent)
    {
        double diff = ImageComparison.PixelDiffPercentage(actual, reference);
        Assert.That(diff, Is.LessThanOrEqualTo(thresholdPercent),
            $"Image difference {diff:F2}% exceeds threshold {thresholdPercent:F2}%");
    }
}

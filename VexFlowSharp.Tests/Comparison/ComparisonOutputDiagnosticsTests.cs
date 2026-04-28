// VexFlowSharp - C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.IO;
using NUnit.Framework;

namespace VexFlowSharp.Tests.Comparison
{
    [TestFixture]
    [Category("Comparison")]
    public class ComparisonOutputDiagnosticsTests
    {
        private static string OutputDir
        {
            get
            {
                string assemblyDir = Path.GetDirectoryName(
                    typeof(ComparisonOutputDiagnosticsTests).Assembly.Location)!;
                return Path.GetFullPath(
                    Path.Combine(assemblyDir, "../../../Comparison/Output"));
            }
        }

        [Test]
        public void ReportPairedImageDiagnostics()
        {
            bool writeHeatmaps = string.Equals(
                Environment.GetEnvironmentVariable("VEXFLOW_COMPARISON_HEATMAPS"),
                "1",
                StringComparison.Ordinal);

            var diagnostics = ComparisonOutput.DiagnosePairedImages(OutputDir, writeHeatmaps);
            Assert.That(diagnostics, Is.Not.Empty, "No paired comparison images found.");

            foreach (var diagnostic in diagnostics)
            {
                TestContext.Out.WriteLine(diagnostic.ToReportLine());
            }
        }
    }
}

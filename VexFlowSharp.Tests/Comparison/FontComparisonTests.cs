// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// FontComparisonTests — visual parity checks for every VexFlow music font
// stack supported by VexFlowSharp's generated built-in font registry.

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Skia;
using VexFlowSharp.Tests.Infrastructure;

namespace VexFlowSharp.Tests.Comparison
{
    [TestFixture]
    [Category("Comparison")]
    public class FontComparisonTests
    {
        public sealed class FontStack
        {
            public FontStack(string slug, params string[] fonts)
            {
                Slug = slug;
                Fonts = fonts;
            }

            public string Slug { get; }
            public string[] Fonts { get; }

            public override string ToString() => Slug;
        }

        private static IEnumerable<FontStack> FontStacks
        {
            get
            {
                yield return new FontStack("bravura", "Bravura", "Academico");
                yield return new FontStack("finale_ash", "Finale Ash", "Finale Ash Text");
                yield return new FontStack("finale_broadway", "Finale Broadway", "Finale Broadway Text");
                yield return new FontStack("finale_jazz", "Finale Jazz", "Finale Jazz Text");
                yield return new FontStack("finale_maestro", "Finale Maestro", "Finale Maestro Text");
                yield return new FontStack("gonville", "Gonville", "Academico");
                yield return new FontStack("gootville", "Gootville", "Gootville Text");
                yield return new FontStack("leipzig", "Leipzig", "Academico");
                yield return new FontStack("leland", "Leland", "Leland Text");
                yield return new FontStack("musejazz", "MuseJazz", "MuseJazz Text");
                yield return new FontStack("petaluma", "Petaluma", "Petaluma Script");
                yield return new FontStack("sebastian", "Sebastian", "Sebastian Text");
            }
        }

        private static string OutputDir
        {
            get
            {
                string assemblyDir = Path.GetDirectoryName(
                    typeof(FontComparisonTests).Assembly.Location)!;
                return Path.GetFullPath(
                    Path.Combine(assemblyDir, "../../../Comparison/Output"));
            }
        }

        private static string ReferenceImagesDir
        {
            get
            {
                string assemblyDir = Path.GetDirectoryName(
                    typeof(FontComparisonTests).Assembly.Location)!;
                return Path.GetFullPath(
                    Path.Combine(assemblyDir, "../../../reference-images"));
            }
        }

        [SetUp]
        public void SetUp()
        {
            Font.ClearRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            Font.ClearRegistry();
            Font.Load("Bravura", BravuraGlyphs.Data);
            VexFlow.SetFonts("Bravura", "Academico");
        }

        [TestCaseSource(nameof(FontStacks))]
        public void FontStack_PixelComparison(FontStack fontStack)
        {
            string actualFilename = $"font_{fontStack.Slug}-vfsharp.png";
            string referenceFilename = $"font_{fontStack.Slug}-vexflow.png";

            RenderFontStack(fontStack, actualFilename);
            ComparisonOutput.CopyReferenceImage(ReferenceImagesDir, OutputDir, referenceFilename);

            string actualPath = Path.Combine(OutputDir, actualFilename);
            string referencePath = Path.Combine(ReferenceImagesDir, referenceFilename);
            ImageComparisonAssert.AssertImagesMatch(
                File.ReadAllBytes(actualPath),
                File.ReadAllBytes(referencePath),
                thresholdPercent: ImageComparison.CrossEngineThresholdPercent);
        }

        private static void RenderFontStack(FontStack fontStack, string filename)
        {
            VexFlow.LoadFonts(fontStack.Fonts);
            VexFlow.SetFonts(fontStack.Fonts);

            using var ctx = new SkiaRenderContext(620, 190);
            FillWhiteBackground(ctx, 620, 190);

            var stave = new Stave(15, 38, 585);
            stave.AddClef("treble").AddKeySignature("D").AddTimeSignature("4/4").SetContext(ctx);
            stave.Draw();

            var first = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "8" });
            var second = new StaveNote(new StaveNoteStruct { Keys = new[] { "d#/4" }, Duration = "8" });
            second.AddModifier(new Accidental("#"), 0);
            var third = new StaveNote(new StaveNoteStruct { Keys = new[] { "e/4" }, Duration = "q" });
            var fourth = new StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" }, Duration = "q" });
            fourth.AddModifier(new Accidental("b"), 0);
            var fifth = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/4" }, Duration = "q" });

            var notes = new List<StaveNote> { first, second, third, fourth, fifth };
            var voice = new Voice(new VoiceTime { NumBeats = 4, BeatValue = 4 });
            voice.AddTickables(new List<Tickable>(notes));
            var beams = Beam.GenerateBeams(new List<StemmableNote> { first, second });

            new Formatter().JoinVoices(new List<Voice> { voice }).FormatToStave(new List<Voice> { voice }, stave);
            voice.Draw(ctx, stave);
            foreach (var beam in beams) beam.SetContext(ctx).Draw();

            Directory.CreateDirectory(OutputDir);
            string outPath = Path.Combine(OutputDir, filename);
            ctx.SavePng(outPath);
            Assert.That(File.Exists(outPath), Is.True, $"Output PNG must exist at {outPath}");
            Assert.That(new FileInfo(outPath).Length, Is.GreaterThan(0), "Output PNG must be non-zero bytes");
        }

        private static void FillWhiteBackground(SkiaRenderContext ctx, double width, double height)
        {
            ctx.Save();
            ctx.SetFillStyle("#FFFFFF");
            ctx.FillRect(0, 0, width, height);
            ctx.Restore();
        }
    }
}

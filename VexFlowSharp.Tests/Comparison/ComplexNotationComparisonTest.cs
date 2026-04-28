// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// ComplexNotationComparisonTest — renders the same complex notation scene as
// generate_comparison_complex.js using the VexFlowSharp C# API.
//
// Purpose: Enable visual regression comparison between VexFlow JS and VexFlowSharp C#
// by producing matching PNG outputs in a shared Comparison/Output directory.

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Comparison
{
    [TestFixture]
    [Category("Comparison")]
    public class ComplexNotationComparisonTest
    {
        // ── Output path ───────────────────────────────────────────────────────

        private static string OutputDir
        {
            get
            {
                // Walk up from test assembly (bin/Debug/net10.0/) to VexFlowSharp.Tests/,
                // then into Comparison/Output/ where both PNGs should reside side-by-side.
                string assemblyDir = Path.GetDirectoryName(
                    typeof(ComplexNotationComparisonTest).Assembly.Location)!;
                return Path.GetFullPath(
                    Path.Combine(assemblyDir, "../../../Comparison/Output"));
            }
        }

        private static string ReferenceImagesDir
        {
            get
            {
                string assemblyDir = Path.GetDirectoryName(
                    typeof(ComplexNotationComparisonTest).Assembly.Location)!;
                return Path.GetFullPath(
                    Path.Combine(assemblyDir, "../../../reference-images"));
            }
        }

        // ── SetUp ─────────────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            Font.ClearRegistry();
            Font.Load("Bravura", BravuraGlyphs.Data);
        }

        // ── Test ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Renders the same complex notation scene as generate_comparison_complex.js:
        ///   - Treble clef, 4/4 time
        ///   - Dotted 8th C4 (## accidental) + 16th D4 (b accidental), auto-beamed
        ///   - Quarter E4
        ///   - Quarter E4 (tied to previous)
        ///   - Quarter G4
        ///
        /// Saves complex_notation-vfsharp.png alongside complex_notation-vexflow.png
        /// in VexFlowSharp.Tests/Comparison/Output/ for side-by-side visual comparison.
        /// </summary>
        [Test]
        public void ComplexNotation_RendersToFile()
        {
            using var ctx = new SkiaRenderContext(600, 200);
            ctx.Save();
            ctx.SetFillStyle("#FFFFFF");
            ctx.FillRect(0, 0, 600, 200);
            ctx.Restore();

            // Draw stave with treble clef and 4/4 time signature
            var stave = new Stave(10, 40, 570);
            stave.AddClef("treble").AddTimeSignature("4/4");
            stave.SetContext(ctx);
            stave.Draw();

            // Build notes — must add up to 4/4 exactly:
            //   dotted 8th (3/16) + 16th (1/16) + quarter + quarter + quarter = 4 beats
            //
            // Use "8d" (duration string with dot) to match VexFlow JS: duration: '8d'
            // This gives 3072 ticks and no Dot modifier, matching JS behavior exactly.

            var dotted8thC4 = new StaveNote(new StaveNoteStruct
            {
                Keys     = new[] { "c/4" },
                Duration = "8d",
            });
            dotted8thC4.AddModifier(new Accidental("##"), 0);

            var sixteenth_D4 = new StaveNote(new StaveNoteStruct
            {
                Keys     = new[] { "d/4" },
                Duration = "16",
            });
            sixteenth_D4.AddModifier(new Accidental("b"), 0);

            var quarter_E4_first = new StaveNote(new StaveNoteStruct
            {
                Keys     = new[] { "e/4" },
                Duration = "q",
            });

            var quarter_E4_second = new StaveNote(new StaveNoteStruct
            {
                Keys     = new[] { "e/4" },
                Duration = "q",
            });

            var quarter_G4 = new StaveNote(new StaveNoteStruct
            {
                Keys     = new[] { "g/4" },
                Duration = "q",
            });

            var allNotes = new List<StaveNote>
            {
                dotted8thC4, sixteenth_D4, quarter_E4_first, quarter_E4_second, quarter_G4
            };

            // Voice with 4 beats in 4/4
            var voice = new Voice(new VoiceTime { NumBeats = 4, BeatValue = 4 });
            voice.AddTickables(new List<Tickable>
            {
                dotted8thC4, sixteenth_D4, quarter_E4_first, quarter_E4_second, quarter_G4
            });

            // Auto-beam only the beamable short note pair (dotted 8th + 16th),
            // matching VexFlow JS: Beam.generateBeams([dotted8thC4, sixteenth_D4])
            var beams = Beam.GenerateBeams(new List<StemmableNote> { dotted8thC4, sixteenth_D4 });

            // Tie the two E4 quarter notes (notes[2] → notes[3])
            var tie = new StaveTie(new TieNotes
            {
                FirstNote  = quarter_E4_first,
                LastNote   = quarter_E4_second,
                FirstIndex = 0,
                LastIndex  = 0,
            });
            // Use VexFlow JS stavetie.ts defaults (cp1=8, cp2=12) for visual parity
            tie.RenderOptions.Cp1 = 8;
            tie.RenderOptions.Cp2 = 12;

            // Format voice onto the stave (500px of note space after clef/time sig)
            new Formatter().JoinVoices(new List<Voice> { voice }).Format(new List<Voice> { voice }, 500);

            // Draw notes
            voice.Draw(ctx, stave);

            // Draw beams
            foreach (var beam in beams)
            {
                beam.SetContext(ctx);
                beam.Draw();
            }

            // Draw tie
            tie.SetContext(ctx);
            tie.Draw();

            // Save PNG to shared comparison output directory alongside the VexFlow reference
            Directory.CreateDirectory(OutputDir);
            string outPath = Path.Combine(OutputDir, "complex_notation-vfsharp.png");
            ctx.SavePng(outPath);

            // Assertions
            Assert.That(File.Exists(outPath), Is.True,
                $"Output PNG must exist at {outPath}");

            var fileInfo = new System.IO.FileInfo(outPath);
            Assert.That(fileInfo.Length, Is.GreaterThan(0),
                "Output PNG must be non-zero bytes");

            ComparisonOutput.CopyReferenceImage(
                ReferenceImagesDir,
                OutputDir,
                "complex_notation-vexflow.png");
        }

        [Test]
        public void ComplexNotation_PixelComparison()
        {
            ComplexNotation_RendersToFile();

            string actualPath = Path.Combine(OutputDir, "complex_notation-vfsharp.png");
            string referencePath = Path.Combine(ReferenceImagesDir, "complex_notation-vexflow.png");
            Assert.That(File.Exists(actualPath), Is.True, $"Actual PNG missing: {actualPath}");
            Assert.That(File.Exists(referencePath), Is.True, $"Reference PNG missing: {referencePath}");

            Infrastructure.ImageComparisonAssert.AssertImagesMatch(
                File.ReadAllBytes(actualPath),
                File.ReadAllBytes(referencePath),
                thresholdPercent: ImageComparison.CrossEngineThresholdPercent);
        }
    }
}

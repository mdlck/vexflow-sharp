// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Skia;
using VexFlowSharp.Tests.Infrastructure;

namespace VexFlowSharp.Tests.SystemTests
{
    [TestFixture]
    [Category("System")]
    [Category("Phase5")]
    public class SystemTests
    {
        // ── Reference image path ──────────────────────────────────────────────

        private static string ReferenceImagesDir
        {
            get
            {
                string assemblyDir = Path.GetDirectoryName(
                    typeof(SystemTests).Assembly.Location)!;
                return Path.GetFullPath(
                    Path.Combine(assemblyDir, "../../../reference-images"));
            }
        }

        private string RefPath(string filename) =>
            Path.Combine(ReferenceImagesDir, filename);

        // ── SetUp ─────────────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            Font.ClearRegistry();
            Font.Load("Bravura", BravuraGlyphs.Data);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static StaveNote MakeNote(string duration, string key, string clef = "treble")
            => new StaveNote(new StaveNoteStruct
            {
                Duration = duration,
                Keys = new[] { key },
                Clef = clef,
            });

        private static (VexFlowSharp.Common.Formatting.System system, Stave treble, Stave bass)
            BuildGrandStaff(SkiaRenderContext ctx)
        {
            var trebleStave = new Stave(10, 10, 570);
            trebleStave.AddClef("treble");
            trebleStave.AddTimeSignature("4/4");

            var bassStave = new Stave(10, 10, 570);
            bassStave.AddClef("bass");
            bassStave.AddTimeSignature("4/4");

            var trebleNotes = new List<Tickable>
            {
                MakeNote("4", "c/4"),
                MakeNote("4", "d/4"),
                MakeNote("4", "e/4"),
                MakeNote("4", "f/4"),
            };
            var trebleVoice = new Voice();
            trebleVoice.AddTickables(new List<Tickable>(trebleNotes));

            var bassNotes = new List<Tickable>
            {
                MakeNote("4", "c/3", "bass"),
                MakeNote("4", "d/3", "bass"),
                MakeNote("4", "e/3", "bass"),
                MakeNote("4", "f/3", "bass"),
            };
            var bassVoice = new Voice();
            bassVoice.AddTickables(new List<Tickable>(bassNotes));

            var system = new VexFlowSharp.Common.Formatting.System(new SystemOptions
            {
                X     = 10,
                Y     = 10,
                Width = 570,
            });
            system.SetContext(ctx);

            system.AddStave(new SystemStave
            {
                Stave  = trebleStave,
                Voices = new List<Voice> { trebleVoice },
            });
            system.AddStave(new SystemStave
            {
                Stave  = bassStave,
                Voices = new List<Voice> { bassVoice },
            });

            return (system, trebleStave, bassStave);
        }

        // ── Test 1: Grand staff layout — functional / structural test ─────────

        [Test]
        public void Category_IsSystem()
        {
            var system = new VexFlowSharp.Common.Formatting.System();

            Assert.That(system.GetCategory(), Is.EqualTo(VexFlowSharp.Common.Formatting.System.CATEGORY));
        }

        [Test]
        public void SystemOptions_DefaultsComeFromMetrics()
        {
            var options = new SystemOptions();

            Assert.That(options.X, Is.EqualTo(Metrics.GetDouble("System.x")));
            Assert.That(options.Y, Is.EqualTo(Metrics.GetDouble("System.y")));
            Assert.That(options.Width, Is.EqualTo(Metrics.GetDouble("System.width")));
            Assert.That(options.SpaceBetweenStaves, Is.EqualTo(Metrics.GetDouble("System.spaceBetweenStaves")));
            Assert.That(options.FormatIterations, Is.EqualTo((int)Metrics.GetDouble("System.formatIterations")));
            Assert.That(options.Alpha, Is.EqualTo(Metrics.GetDouble("System.alpha")));
        }

        /// <summary>
        /// Phase 5 plan 05-01 success criterion:
        /// System formats a grand staff with treble and bass staves, producing
        /// aligned note columns across both staves (noteStartX equal after Format()).
        ///
        /// This is a structural test — it verifies that System.Format() correctly
        /// calls formatter.JoinVoices and formatter.Format, that both staves end up
        /// at the same noteStartX, and that rendering completes without exceptions.
        /// </summary>
        [Test]
        public void GrandStaffRenders()
        {
            using var ctx = new SkiaRenderContext(600, 250);
            var (system, treble, bass) = BuildGrandStaff(ctx);

            // Format: unified Formatter pass across both staves
            Assert.DoesNotThrow(() => system.Format(),
                "System.Format() must run without exceptions");

            // Both staves must have equal noteStartX after Format() (column alignment)
            double trebleStartX = treble.GetNoteStartX();
            double bassStartX   = bass.GetNoteStartX();
            Assert.That(trebleStartX, Is.EqualTo(bassStartX).Within(0.5),
                $"Treble noteStartX ({trebleStartX:F2}) must match bass noteStartX ({bassStartX:F2}) after System.Format()");

            // NoteStartX must be > stave X (clef + time sig consumed space)
            Assert.That(trebleStartX, Is.GreaterThan(10),
                "NoteStartX must be > 10 (clef + time sig padding consumed)");

            // Both staves must be at different Y positions (stacked vertically)
            Assert.That(bass.GetY(), Is.GreaterThan(treble.GetY()),
                "Bass stave must be below treble stave after Format()");

            var box = system.GetBoundingBox();
            Assert.That(box, Is.Not.Null);
            Assert.That(box!.GetX(), Is.EqualTo(10));
            Assert.That(box.GetY(), Is.EqualTo(10));
            Assert.That(box.GetW(), Is.EqualTo(570));
            Assert.That(box.GetH(), Is.GreaterThan(bass.GetY() - box.GetY()));

            // Rendering must succeed without exceptions
            Assert.DoesNotThrow(() =>
            {
                treble.SetContext(ctx);
                treble.Draw();
                bass.SetContext(ctx);
                bass.Draw();
            }, "Stave drawing must not throw");
        }

        // ── Test 2: Grand staff pixel comparison ─────────────────────────────

        /// <summary>
        /// Pixel-level comparison of System grand staff against VexFlow reference PNG.
        ///
        /// Uses the cross-engine tolerance because SkiaSharp and node-canvas
        /// rasterize glyphs with different anti-aliasing and ink density.
        /// </summary>
        [Test]
        [Category("ImageCompare")]
        public void GrandStaffRenders_PixelComparison()
        {
            using var ctx = new SkiaRenderContext(600, 250);
            ctx.SetFillStyle("#FFFFFF");
            ctx.FillRect(0, 0, 600, 250);
            var (system, treble, bass) = BuildGrandStaff(ctx);

            system.Format();

            treble.SetContext(ctx);
            treble.Draw();
            bass.SetContext(ctx);
            bass.Draw();

            string refPath = RefPath("system_grandstaff-vexflow.png");
            Assert.That(File.Exists(refPath), Is.True, $"Reference PNG missing: {refPath}");

            byte[] actual = ctx.ToPng();
            byte[] reference = File.ReadAllBytes(refPath);
            ImageComparisonAssert.AssertImagesMatch(actual, reference, thresholdPercent: ImageComparison.CrossEngineThresholdPercent);
        }
    }
}

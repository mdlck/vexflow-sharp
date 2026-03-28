// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// PercussionComparisonTest — renders the same percussion scenes as percussion_tests.ts
// using the VexFlowSharp C# API.
//
// Covers:
//   PercussionClef   — percussion clef on a stave
//   PercussionBasic0 — hi-hat + kick/snare pattern, two voices, beams
//   PercussionBasic1 — simple quarter-note two-voice percussion
//   PercussionBasic2 — mixed note heads (x, circle-x, normal) with dotted note
//   PercussionSnare0 — accent + L/R hand sticking annotations
//   PercussionSnare1 — open hi-hat (ah) and choke (a,) articulations
//
// Note: snare2 / snare3 from the JS source require Tremolo, which is not yet
// implemented in VexFlowSharp and are therefore omitted here.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Api;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Comparison
{
    [TestFixture]
    [Category("Comparison")]
    public class PercussionComparisonTest
    {
        // ── Output path ───────────────────────────────────────────────────────

        private static string OutputDir
        {
            get
            {
                string assemblyDir = Path.GetDirectoryName(
                    typeof(PercussionComparisonTest).Assembly.Location)!;
                return Path.GetFullPath(
                    Path.Combine(assemblyDir, "../../../Comparison/Output"));
            }
        }

        // ── SetUp ─────────────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            Font.ClearRegistry();
            Font.Load("Bravura", BravuraGlyphs.Data);
        }

        // ── Save helper ───────────────────────────────────────────────────────

        private static void SaveAndAssert(SkiaRenderContext ctx, string filename)
        {
            Directory.CreateDirectory(OutputDir);
            string outPath = Path.Combine(OutputDir, filename);
            ctx.SavePng(outPath);
            Assert.That(File.Exists(outPath), Is.True, $"Output PNG must exist at {outPath}");
            Assert.That(new FileInfo(outPath).Length, Is.GreaterThan(0), "Output PNG must be non-zero bytes");
        }

        // ── percussion_clef ───────────────────────────────────────────────────

        /// <summary>
        /// Renders a stave with a percussion clef.
        /// Mirrors the 'Percussion Clef' draw() test from percussion_tests.ts.
        /// </summary>
        [Test]
        public void PercussionClef_RendersToFile()
        {
            using var ctx = new SkiaRenderContext(400, 120);
            var stave = new Stave(10, 10, 300);
            stave.AddClef("percussion");
            stave.SetContext(ctx);
            stave.Draw();
            SaveAndAssert(ctx, "percussion_clef-vfsharp.png");
        }

        // ── percussion_basic0 ─────────────────────────────────────────────────

        /// <summary>
        /// Hi-hat + kick/snare two-voice pattern in 4/4, all eighth notes.
        /// Voice 0: eight hi-hat x2 noteheads on G5.
        /// Voice 1: kick (F4) and snare (D4/x2 + C5 chord) with stem-down.
        /// Mirrors the 'Percussion Basic0' basic0 test from percussion_tests.ts.
        /// </summary>
        [Test]
        public void PercussionBasic0_RendersToFile()
        {
            using var ctx = new SkiaRenderContext(500, 200);
            var f = new Factory(ctx, 500, 200);
            var stave = f.Stave().AddClef("percussion").AddTimeSignature("4/4");

            var voice0 = f.Voice().AddTickables(new List<Tickable>
            {
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" }, Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" }, Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" }, Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" }, Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" }, Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" }, Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" }, Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" }, Duration = "8" }),
            });

            var voice1 = f.Voice().AddTickables(new List<Tickable>
            {
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },           Duration = "8", StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },           Duration = "8", StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "d/4/x2", "c/5" }, Duration = "4", StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },           Duration = "8", StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },           Duration = "8", StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "d/4/x2", "c/5" }, Duration = "4", StemDirection = Stem.DOWN }),
            });

            f.Beam(voice0.GetTickables().Cast<StemmableNote>().ToList());
            f.Beam(voice1.GetTickables().Take(2).Cast<StemmableNote>().ToList());
            f.Beam(voice1.GetTickables().Skip(3).Take(2).Cast<StemmableNote>().ToList());

            f.Formatter().JoinVoices(f.GetVoices()).FormatToStave(f.GetVoices(), stave);
            f.Draw();

            SaveAndAssert(ctx, "percussion_basic0-vfsharp.png");
        }

        // ── percussion_basic1 ─────────────────────────────────────────────────

        /// <summary>
        /// Simple quarter-note two-voice percussion in 4/4.
        /// Voice 0: four F5/x2 quarter notes (stem up).
        /// Voice 1: alternating kick (F4) and snare (D4/x2 + C5 chord), stem down.
        /// Mirrors the 'Percussion Basic1' basic1 test from percussion_tests.ts.
        /// </summary>
        [Test]
        public void PercussionBasic1_RendersToFile()
        {
            using var ctx = new SkiaRenderContext(500, 200);
            var f = new Factory(ctx, 500, 200);
            var stave = f.Stave().AddClef("percussion").AddTimeSignature("4/4");

            f.Voice().AddTickables(new List<Tickable>
            {
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/5/x2" }, Duration = "4" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/5/x2" }, Duration = "4" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/5/x2" }, Duration = "4" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/5/x2" }, Duration = "4" }),
            });

            f.Voice().AddTickables(new List<Tickable>
            {
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },           Duration = "4", StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "d/4/x2", "c/5" }, Duration = "4", StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },           Duration = "4", StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "d/4/x2", "c/5" }, Duration = "4", StemDirection = Stem.DOWN }),
            });

            f.Formatter().JoinVoices(f.GetVoices()).FormatToStave(f.GetVoices(), stave);
            f.Draw();

            SaveAndAssert(ctx, "percussion_basic1-vfsharp.png");
        }

        // ── percussion_basic2 ─────────────────────────────────────────────────

        /// <summary>
        /// Mixed percussion note heads — x, circle-x, normal, and x on chord — with
        /// a dotted rhythm in voice 1 and a beam spanning most of voice 0.
        /// Mirrors the 'Percussion Basic2' basic2 test from percussion_tests.ts.
        /// </summary>
        [Test]
        public void PercussionBasic2_RendersToFile()
        {
            using var ctx = new SkiaRenderContext(500, 200);
            var f = new Factory(ctx, 500, 200);
            var stave = f.Stave().AddClef("percussion").AddTimeSignature("4/4");

            var voice0 = f.Voice().AddTickables(new List<Tickable>
            {
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "a/5/x3" },        Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" },        Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5" },           Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/4/n", "g/5/x2" }, Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" },        Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" },        Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" },        Duration = "8" }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" },        Duration = "8" }),
            });
            // Beam notes 1–7 (indices 1..7), leaving first note unbeamed
            f.Beam(voice0.GetTickables().Skip(1).Cast<StemmableNote>().ToList());

            var note4 = f.StaveNote(new StaveNoteStruct { Keys = new[] { "d/4/x2", "c/5" }, Duration = "8d", StemDirection = Stem.DOWN });
            var note5 = f.StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" },            Duration = "16",  StemDirection = Stem.DOWN });

            var voice1Notes = new List<StaveNote>
            {
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },           Duration = "8",  StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },           Duration = "8",  StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "d/4/x2", "c/5" }, Duration = "4",  StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },           Duration = "4",  StemDirection = Stem.DOWN }),
                note4,
                note5,
            };
            Dot.BuildAndAttach(new List<VexFlowSharp.Note> { note4 }, allNotes: true);

            var voice1 = f.Voice().AddTickables(voice1Notes.Cast<Tickable>().ToList());

            f.Beam(voice1.GetTickables().Take(2).Cast<StemmableNote>().ToList());
            f.Beam(new List<StemmableNote> { note4, note5 });

            f.Formatter().JoinVoices(f.GetVoices()).FormatToStave(f.GetVoices(), stave);
            f.Draw();

            SaveAndAssert(ctx, "percussion_basic2-vfsharp.png");
        }

        // ── percussion_snare0 ─────────────────────────────────────────────────

        /// <summary>
        /// Snare drum with accent articulation and L/R sticking annotations.
        /// Mirrors the 'Percussion Snare0' snare0 test from percussion_tests.ts.
        /// </summary>
        [Test]
        public void PercussionSnare0_RendersToFile()
        {
            using var ctx = new SkiaRenderContext(500, 200);
            var f = new Factory(ctx, 500, 200);
            var stave = f.Stave().AddClef("percussion").AddTimeSignature("4/4");

            f.Voice().AddTickables(new List<Tickable>
            {
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" }, Duration = "4", StemDirection = Stem.DOWN })
                    .AddModifier(f.Articulation("a>"), 0)
                    .AddModifier(f.Annotation("L").SetFont("Arial", 14, "bold italic"), 0),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" }, Duration = "4", StemDirection = Stem.DOWN })
                    .AddModifier(f.Annotation("R").SetFont("Arial", 14, "bold italic"), 0),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" }, Duration = "4", StemDirection = Stem.DOWN })
                    .AddModifier(f.Annotation("L").SetFont("Arial", 14, "bold italic"), 0),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" }, Duration = "4", StemDirection = Stem.DOWN })
                    .AddModifier(f.Annotation("L").SetFont("Arial", 14, "bold italic"), 0),
            });

            f.Formatter().JoinVoices(f.GetVoices()).FormatToStave(f.GetVoices(), stave);
            f.Draw();

            SaveAndAssert(ctx, "percussion_snare0-vfsharp.png");
        }

        // ── percussion_snare1 ─────────────────────────────────────────────────

        /// <summary>
        /// Hi-hat with open (ah) and choke (a,) articulations, plus a circle-x notehead.
        /// Mirrors the 'Percussion Snare1' snare1 test from percussion_tests.ts.
        /// </summary>
        [Test]
        public void PercussionSnare1_RendersToFile()
        {
            using var ctx = new SkiaRenderContext(500, 200);
            var f = new Factory(ctx, 500, 200);
            var stave = f.Stave().AddClef("percussion").AddTimeSignature("4/4");

            f.Voice().AddTickables(new List<Tickable>
            {
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" }, Duration = "4", StemDirection = Stem.DOWN })
                    .AddModifier(f.Articulation("ah"), 0),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" }, Duration = "4", StemDirection = Stem.DOWN }),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "g/5/x2" }, Duration = "4", StemDirection = Stem.DOWN })
                    .AddModifier(f.Articulation("ah"), 0),
                f.StaveNote(new StaveNoteStruct { Keys = new[] { "a/5/x3" }, Duration = "4", StemDirection = Stem.DOWN })
                    .AddModifier(f.Articulation("a,"), 0),
            });

            f.Formatter().JoinVoices(f.GetVoices()).FormatToStave(f.GetVoices(), stave);
            f.Draw();

            SaveAndAssert(ctx, "percussion_snare1-vfsharp.png");
        }
    }
}

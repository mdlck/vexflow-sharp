// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// BeethovenComparisonTest — renders the first system (measures 1-4) of
// Beethoven's "An die ferne Geliebte" Op. 98 using the VexFlowSharp C# API.
//
// Mirrors generate_comparison_beethoven.js exactly for side-by-side PNG comparison.
//
// Purpose: Demonstrate VexFlowSharp can render a real-world complex score
// (grand staff, key/time signatures, chords, beams, slurs, dotted notes, rests)
// with visual parity to VexFlow JS.

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
    public class BeethovenComparisonTest
    {
        // ── Output path ───────────────────────────────────────────────────────

        private static string OutputDir
        {
            get
            {
                // Walk up from test assembly (bin/Debug/net10.0/) to VexFlowSharp.Tests/,
                // then into Comparison/Output/ where both PNGs should reside side-by-side.
                string assemblyDir = Path.GetDirectoryName(
                    typeof(BeethovenComparisonTest).Assembly.Location)!;
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

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Create a visible rest StaveNote (b/4 placeholder pitch, rest flag).</summary>
        private static StaveNote Rest(string duration)
        {
            return new StaveNote(new StaveNoteStruct
            {
                Keys     = new[] { "b/4" },
                Duration = duration + "r",
            });
        }

        // ── Test ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Renders the first system (measures 1-4) of Beethoven Op. 98 as:
        ///   - Voice stave (treble, P1): solo voice melody
        ///   - Piano RH stave (treble, P2 staff 1): right-hand piano
        ///   - Piano LH stave (bass, P2 staff 2): left-hand piano
        ///
        /// Saves beethoven_op98-vfsharp.png alongside beethoven_op98-vexflow.png
        /// in VexFlowSharp.Tests/Comparison/Output/ for side-by-side visual comparison.
        /// </summary>
        [Test]
        public void BeethovenOp98_RendersToFile()
        {
            const int CANVAS_W = 1200;
            const int CANVAS_H = 700;

            using var ctx = new SkiaRenderContext(CANVAS_W, CANVAS_H);

            const int X_START  = 10;
            const int STAVE_W  = 1170;
            const int Y_VOICE  = 40;
            const int Y_RH     = 200;
            const int Y_LH     = 360;
            const double FORMAT_W = STAVE_W - 70;

            // ── VOICE STAVE (P1, treble) ──────────────────────────────────────

            var staveVoice = new Stave(X_START, Y_VOICE, STAVE_W);
            staveVoice.AddClef("treble").AddKeySignature("Eb").AddTimeSignature("3/4");
            staveVoice.SetContext(ctx);
            staveVoice.Draw();

            // Measure 1: quarter rest, Bb4 (stem down), Bb4 (stem down)
            var v_m1_1 = Rest("q");
            var v_m1_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "bb/4" }, Duration = "q", StemDirection = Stem.DOWN });
            var v_m1_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "bb/4" }, Duration = "q", StemDirection = Stem.DOWN });

            // Measure 2: dotted quarter Bb4, 8th C5, beamed [D5, Eb5]
            var v_m2_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "bb/4" }, Duration = "qd", StemDirection = Stem.DOWN });
            var v_m2_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" },  Duration = "8",  StemDirection = Stem.DOWN });
            var v_m2_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "d/5" },  Duration = "8",  StemDirection = Stem.DOWN });
            var v_m2_4 = new StaveNote(new StaveNoteStruct { Keys = new[] { "eb/5" }, Duration = "8",  StemDirection = Stem.DOWN });

            // Measure 3: quarter Eb5 (stem down), 8th G4 (stem up), 8th rest, beamed [Ab4, G4] (stem up)
            var v_m3_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "eb/5" }, Duration = "q", StemDirection = Stem.DOWN });
            var v_m3_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/4" },  Duration = "8", StemDirection = Stem.UP });
            var v_m3_3 = Rest("8");
            var v_m3_4 = new StaveNote(new StaveNoteStruct { Keys = new[] { "ab/4" }, Duration = "8", StemDirection = Stem.UP });
            var v_m3_5 = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/4" },  Duration = "8", StemDirection = Stem.UP });

            // Measure 4: beamed [F4, Ab4] (stem up), quarter C5 (stem down), quarter C5 (stem down)
            var v_m4_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },  Duration = "8", StemDirection = Stem.UP });
            var v_m4_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "ab/4" }, Duration = "8", StemDirection = Stem.UP });
            var v_m4_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" },  Duration = "q", StemDirection = Stem.DOWN });
            var v_m4_4 = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" },  Duration = "q", StemDirection = Stem.DOWN });

            var voiceVoice = new Voice(new VoiceTime { NumBeats = 12, BeatValue = 4 });
            voiceVoice.SetMode(VoiceMode.SOFT);
            voiceVoice.AddTickables(new List<Tickable>
            {
                v_m1_1, v_m1_2, v_m1_3, new BarNote(),
                v_m2_1, v_m2_2, v_m2_3, v_m2_4, new BarNote(),
                v_m3_1, v_m3_2, v_m3_3, v_m3_4, v_m3_5, new BarNote(),
                v_m4_1, v_m4_2, v_m4_3, v_m4_4,
            });

            var v_beams_m2 = Beam.GenerateBeams(new List<StemmableNote> { v_m2_3, v_m2_4 });
            var v_beams_m3 = Beam.GenerateBeams(new List<StemmableNote> { v_m3_4, v_m3_5 });
            var v_beams_m4 = Beam.GenerateBeams(new List<StemmableNote> { v_m4_1, v_m4_2 });

            new Formatter().JoinVoices(new List<Voice> { voiceVoice }).Format(new List<Voice> { voiceVoice }, FORMAT_W);

            voiceVoice.Draw(ctx, staveVoice);
            foreach (var b in v_beams_m2) { b.SetContext(ctx); b.Draw(); }
            foreach (var b in v_beams_m3) { b.SetContext(ctx); b.Draw(); }
            foreach (var b in v_beams_m4) { b.SetContext(ctx); b.Draw(); }

            // ── PIANO RH STAVE (P2 staff 1, treble) ──────────────────────────

            var staveRH = new Stave(X_START, Y_RH, STAVE_W);
            staveRH.AddClef("treble").AddKeySignature("Eb").AddTimeSignature("3/4");
            staveRH.SetContext(ctx);
            staveRH.Draw();

            // M1: chord [Bb3,Eb4,G4,Bb4] (stem up), Bb4 (stem down, slur start), Ab4 (stem up, slur stop)
            var rh_m1_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "bb/3", "eb/4", "g/4", "bb/4" }, Duration = "q", StemDirection = Stem.UP });
            var rh_m1_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "bb/4" }, Duration = "q", StemDirection = Stem.DOWN });
            var rh_m1_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "ab/4" }, Duration = "q", StemDirection = Stem.UP });

            // M2: G4 (stem up), chord [G3,Eb4,G4] (stem up), quarter rest
            var rh_m2_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/4" },              Duration = "q", StemDirection = Stem.UP });
            var rh_m2_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/3", "eb/4", "g/4" }, Duration = "q", StemDirection = Stem.UP });
            var rh_m2_3 = Rest("q");

            // M3: quarter rest, chord [G3,C4,Eb4,G4] (stem up), 8th rest + 8th chord [G3,Eb4,G4] (stem up)
            var rh_m3_1 = Rest("q");
            var rh_m3_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/3", "c/4", "eb/4", "g/4" }, Duration = "q", StemDirection = Stem.UP });
            var rh_m3_3 = Rest("8");
            var rh_m3_4 = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/3", "eb/4", "g/4" }, Duration = "8", StemDirection = Stem.UP });

            // M4: beamed 8th pair [F4, Ab4] (stem up), quarter C5 (stem up), quarter C5 (stem up)
            var rh_m4_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },  Duration = "8", StemDirection = Stem.UP });
            var rh_m4_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "ab/4" }, Duration = "8", StemDirection = Stem.UP });
            var rh_m4_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" },  Duration = "q", StemDirection = Stem.UP });
            var rh_m4_4 = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" },  Duration = "q", StemDirection = Stem.UP });

            var rhVoice = new Voice(new VoiceTime { NumBeats = 12, BeatValue = 4 });
            rhVoice.SetMode(VoiceMode.SOFT);
            rhVoice.AddTickables(new List<Tickable>
            {
                rh_m1_1, rh_m1_2, rh_m1_3, new BarNote(),
                rh_m2_1, rh_m2_2, rh_m2_3, new BarNote(),
                rh_m3_1, rh_m3_2, rh_m3_3, rh_m3_4, new BarNote(),
                rh_m4_1, rh_m4_2, rh_m4_3, rh_m4_4,
            });

            var rh_beams_m4 = Beam.GenerateBeams(new List<StemmableNote> { rh_m4_1, rh_m4_2 });

            new Formatter().JoinVoices(new List<Voice> { rhVoice }).Format(new List<Voice> { rhVoice }, FORMAT_W);

            rhVoice.Draw(ctx, staveRH);
            foreach (var b in rh_beams_m4) { b.SetContext(ctx); b.Draw(); }

            // Slur: rh_m1_2 (Bb4) to rh_m1_3 (Ab4)
            var rh_slur1 = new Curve(rh_m1_2, rh_m1_3);
            rh_slur1.SetContext(ctx);
            rh_slur1.Draw();

            // ── PIANO LH STAVE (P2 staff 2, bass) ────────────────────────────

            var staveLH = new Stave(X_START, Y_LH, STAVE_W);
            staveLH.AddClef("bass").AddKeySignature("Eb").AddTimeSignature("3/4");
            staveLH.SetContext(ctx);
            staveLH.Draw();

            // M1 LH: chord [Eb2,Eb3] (bass), then treble clef change -> G4, F4 (slur start/stop)
            var lh_m1_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "eb/2", "eb/3" }, Duration = "q", Clef = "bass",   StemDirection = Stem.UP });
            var lh_m1_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/4" },           Duration = "q", Clef = "treble", StemDirection = Stem.UP });
            var lh_m1_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" },            Duration = "q", Clef = "treble", StemDirection = Stem.UP });

            // M2 LH: Eb4 (treble), then bass clef change -> chord [Eb2,Eb3], quarter rest
            var lh_m2_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "eb/4" },           Duration = "q", Clef = "treble", StemDirection = Stem.UP });
            var lh_m2_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "eb/2", "eb/3" }, Duration = "q", Clef = "bass",   StemDirection = Stem.UP });
            var lh_m2_3 = Rest("q");

            // M3 LH: quarter rest, chord [C2,C3], 8th rest + 8th chord [Bb1,Bb2]
            var lh_m3_1 = Rest("q");
            var lh_m3_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/2", "c/3" },   Duration = "q", Clef = "bass", StemDirection = Stem.UP });
            var lh_m3_3 = Rest("8");
            var lh_m3_4 = new StaveNote(new StaveNoteStruct { Keys = new[] { "bb/1", "bb/2" }, Duration = "8", Clef = "bass", StemDirection = Stem.UP });

            // M4 LH: chord [Ab1,Ab2], chord [Ab1,Ab2], quarter A1/A2 (from MusicXML)
            var lh_m4_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "ab/1", "ab/2" }, Duration = "q", Clef = "bass", StemDirection = Stem.UP });
            var lh_m4_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "ab/1", "ab/2" }, Duration = "q", Clef = "bass", StemDirection = Stem.UP });
            var lh_m4_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "a/1",  "a/2"  }, Duration = "q", Clef = "bass", StemDirection = Stem.UP });

            var lhVoice = new Voice(new VoiceTime { NumBeats = 12, BeatValue = 4 });
            lhVoice.SetMode(VoiceMode.SOFT);
            lhVoice.AddTickables(new List<Tickable>
            {
                lh_m1_1, lh_m1_2, lh_m1_3, new BarNote(),
                lh_m2_1, lh_m2_2, lh_m2_3, new BarNote(),
                lh_m3_1, lh_m3_2, lh_m3_3, lh_m3_4, new BarNote(),
                lh_m4_1, lh_m4_2, lh_m4_3,
            });

            new Formatter().JoinVoices(new List<Voice> { lhVoice }).Format(new List<Voice> { lhVoice }, FORMAT_W);

            lhVoice.Draw(ctx, staveLH);

            // Slur: lh_m1_2 (G4 treble) to lh_m1_3 (F4 treble)
            var lh_slur1 = new Curve(lh_m1_2, lh_m1_3);
            lh_slur1.SetContext(ctx);
            lh_slur1.Draw();

            // ── Save PNG ──────────────────────────────────────────────────────

            Directory.CreateDirectory(OutputDir);
            string outPath = Path.Combine(OutputDir, "beethoven_op98-vfsharp.png");
            ctx.SavePng(outPath);

            // Assertions
            Assert.That(File.Exists(outPath), Is.True,
                $"Output PNG must exist at {outPath}");

            var fileInfo = new System.IO.FileInfo(outPath);
            Assert.That(fileInfo.Length, Is.GreaterThan(0),
                "Output PNG must be non-zero bytes");
        }
    }
}

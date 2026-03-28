// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// SchubertComparisonTest — renders measures 3-4 of Schubert's "Ave Maria" (D. 839)
// using the VexFlowSharp C# API.
//
// Mirrors generate_comparison_schubert.js exactly for side-by-side PNG comparison.
//
// Purpose: Demonstrate VexFlowSharp can render a second real-world score excerpt
// (Bb major, 4/4 time, dotted rhythms, grace notes, beams, slurs) with visual
// parity to VexFlow JS.

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
    public class SchubertComparisonTest
    {
        // ── Output path ───────────────────────────────────────────────────────

        private static string OutputDir
        {
            get
            {
                // Walk up from test assembly (bin/Debug/net10.0/) to VexFlowSharp.Tests/,
                // then into Comparison/Output/ where both PNGs should reside side-by-side.
                string assemblyDir = Path.GetDirectoryName(
                    typeof(SchubertComparisonTest).Assembly.Location)!;
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
        /// Renders measures 3-4 of Schubert's Ave Maria (D. 839) as:
        ///   - Voice stave (treble, P1): solo voice melody
        ///   - Piano RH stave (treble, P2 staff 1): right-hand piano (simplified chords)
        ///   - Piano LH stave (bass, P2 staff 2): left-hand piano
        ///
        /// Saves schubert_avemaria-vfsharp.png alongside schubert_avemaria-vexflow.png
        /// in VexFlowSharp.Tests/Comparison/Output/ for side-by-side visual comparison.
        /// </summary>
        [Test]
        public void SchubertAveMaria_RendersToFile()
        {
            const int CANVAS_W = 1400;
            const int CANVAS_H = 700;

            using var ctx = new SkiaRenderContext(CANVAS_W, CANVAS_H);

            const int    X_START  = 10;
            const int    STAVE_W  = 1370;
            const int    Y_VOICE  = 40;
            const int    Y_RH     = 230;
            const int    Y_LH     = 420;
            const double FORMAT_W = STAVE_W - 70;

            // ── VOICE STAVE (P1, treble) ──────────────────────────────────────

            var staveVoice = new Stave(X_START, Y_VOICE, STAVE_W);
            staveVoice.AddClef("treble").AddKeySignature("Bb").AddTimeSignature("4/4");
            staveVoice.SetContext(ctx);
            staveVoice.Draw();

            // Measure 3: dotted quarter Bb4 (stem down)
            var v_m3_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "bb/4" }, Duration = "qd",  StemDirection = Stem.DOWN });
            // Beamed 16th pair: A4 (up), Bb4 (up)
            var v_m3_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "a/4" },  Duration = "16",  StemDirection = Stem.UP });
            var v_m3_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "bb/4" }, Duration = "16",  StemDirection = Stem.UP });
            // Double-dotted quarter D5 (stem down, slur start)
            var v_m3_4 = new StaveNote(new StaveNoteStruct { Keys = new[] { "d/5" },  Duration = "qdd", StemDirection = Stem.DOWN });
            // 16th C5 (stem down, slur stop)
            var v_m3_5 = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" },  Duration = "16",  StemDirection = Stem.DOWN });

            // Measure 4: quarter Bb4 (stem down)
            var v_m4_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "bb/4" }, Duration = "q",   StemDirection = Stem.DOWN });
            // Quarter rest
            var v_m4_2 = Rest("q");
            // Quarter C5 (stem down, slur start)
            var v_m4_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/5" },  Duration = "q",   StemDirection = Stem.DOWN });
            // Grace notes: 32nd D5 + 32nd C5 (beamed, attached to first 16th below)
            var v_grace1 = new GraceNote(new GraceNoteStruct { Keys = new[] { "d/5" }, Duration = "32", StemDirection = Stem.UP });
            var v_grace2 = new GraceNote(new GraceNoteStruct { Keys = new[] { "c/5" }, Duration = "32", StemDirection = Stem.UP });
            // Beamed 16th group: Bb4, A4 (slur stop), G4 (second slur start), A4 (second slur stop)
            var v_m4_4 = new StaveNote(new StaveNoteStruct { Keys = new[] { "bb/4" }, Duration = "16",  StemDirection = Stem.UP });
            var v_m4_5 = new StaveNote(new StaveNoteStruct { Keys = new[] { "a/4" },  Duration = "16",  StemDirection = Stem.UP });
            var v_m4_6 = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/4" },  Duration = "16",  StemDirection = Stem.UP });
            var v_m4_7 = new StaveNote(new StaveNoteStruct { Keys = new[] { "a/4" },  Duration = "16",  StemDirection = Stem.UP });

            // Attach grace notes to v_m4_4
            var graceGroup = new GraceNoteGroup(new List<GraceNote> { v_grace1, v_grace2 }, showSlur: true);
            v_m4_4.AddModifier(graceGroup, 0);

            var voiceVoice = new Voice(new VoiceTime { NumBeats = 8, BeatValue = 4 });
            voiceVoice.SetMode(VoiceMode.SOFT);
            voiceVoice.AddTickables(new List<Tickable>
            {
                v_m3_1, v_m3_2, v_m3_3, v_m3_4, v_m3_5, new BarNote(),
                v_m4_1, v_m4_2, v_m4_3, v_m4_4, v_m4_5, v_m4_6, v_m4_7,
            });

            var v_beams_m3 = Beam.GenerateBeams(new List<StemmableNote> { v_m3_2, v_m3_3 });
            var v_beams_m4 = Beam.GenerateBeams(new List<StemmableNote> { v_m4_4, v_m4_5, v_m4_6, v_m4_7 });

            new Formatter().JoinVoices(new List<Voice> { voiceVoice }).Format(new List<Voice> { voiceVoice }, FORMAT_W);

            voiceVoice.Draw(ctx, staveVoice);
            foreach (var b in v_beams_m3) { b.SetContext(ctx); b.Draw(); }
            foreach (var b in v_beams_m4) { b.SetContext(ctx); b.Draw(); }

            // Slur m3: D5 (v_m3_4) to C5 (v_m3_5)
            var v_slur_m3 = new Curve(v_m3_4, v_m3_5);
            v_slur_m3.SetContext(ctx);
            v_slur_m3.Draw();

            // Slur m4 first: C5 (v_m4_3) to A4 (v_m4_5, slur stop)
            var v_slur_m4a = new Curve(v_m4_3, v_m4_5);
            v_slur_m4a.SetContext(ctx);
            v_slur_m4a.Draw();

            // Slur m4 second: G4 (v_m4_6) to A4 (v_m4_7)
            var v_slur_m4b = new Curve(v_m4_6, v_m4_7);
            v_slur_m4b.SetContext(ctx);
            v_slur_m4b.Draw();

            // ── PIANO RH STAVE (P2 staff 1, treble) ──────────────────────────
            // Simplified: one block chord per beat (representing sextuplet arpeggio harmony)

            var staveRH = new Stave(X_START, Y_RH, STAVE_W);
            staveRH.AddClef("treble").AddKeySignature("Bb").AddTimeSignature("4/4");
            staveRH.SetContext(ctx);
            staveRH.Draw();

            // Measure 3 RH: 4 quarter chords
            var rh_m3_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "d/4", "f/4", "bb/4" }, Duration = "q", StemDirection = Stem.UP });
            var rh_m3_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "d/4", "f/4", "bb/4" }, Duration = "q", StemDirection = Stem.UP });
            var rh_m3_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4", "eb/4", "a/4" }, Duration = "q", StemDirection = Stem.UP });
            var rh_m3_4 = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4", "f/4", "a/4" },  Duration = "q", StemDirection = Stem.UP });

            // Measure 4 RH: 4 quarter chords
            var rh_m4_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "d/4", "g/4", "bb/4" }, Duration = "q", StemDirection = Stem.UP });
            var rh_m4_2 = new StaveNote(new StaveNoteStruct { Keys = new[] { "d/4", "g/4", "bb/4" }, Duration = "q", StemDirection = Stem.UP });
            var rh_m4_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "eb/4", "g/4", "c/5" }, Duration = "q", StemDirection = Stem.UP });
            var rh_m4_4 = new StaveNote(new StaveNoteStruct { Keys = new[] { "eb/4", "f/4", "a/4" }, Duration = "q", StemDirection = Stem.UP });

            var rhVoice = new Voice(new VoiceTime { NumBeats = 8, BeatValue = 4 });
            rhVoice.SetMode(VoiceMode.SOFT);
            rhVoice.AddTickables(new List<Tickable>
            {
                rh_m3_1, rh_m3_2, rh_m3_3, rh_m3_4, new BarNote(),
                rh_m4_1, rh_m4_2, rh_m4_3, rh_m4_4,
            });

            new Formatter().JoinVoices(new List<Voice> { rhVoice }).Format(new List<Voice> { rhVoice }, FORMAT_W);
            rhVoice.Draw(ctx, staveRH);

            // ── PIANO LH STAVE (P2 staff 2, bass) ────────────────────────────

            var staveLH = new Stave(X_START, Y_LH, STAVE_W);
            staveLH.AddClef("bass").AddKeySignature("Bb").AddTimeSignature("4/4");
            staveLH.SetContext(ctx);
            staveLH.Draw();

            // Measure 3 LH (8th chord + 8th rest pattern, 4 beats):
            var lh_m3_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "bb/1", "bb/2" }, Duration = "8", Clef = "bass", StemDirection = Stem.UP });
            var lh_m3_2 = Rest("8");
            var lh_m3_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/1", "g/2" },   Duration = "8", Clef = "bass", StemDirection = Stem.UP });
            var lh_m3_4 = Rest("8");
            var lh_m3_5 = new StaveNote(new StaveNoteStruct { Keys = new[] { "f/1", "f/2" },   Duration = "8", Clef = "bass", StemDirection = Stem.UP });
            var lh_m3_6 = Rest("8");
            var lh_m3_7 = new StaveNote(new StaveNoteStruct { Keys = new[] { "f/1", "f/2" },   Duration = "8", Clef = "bass", StemDirection = Stem.UP });
            var lh_m3_8 = Rest("8");

            // Measure 4 LH (8th chord + 8th rest pattern, 4 beats):
            var lh_m4_1 = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/1", "g/2" },   Duration = "8", Clef = "bass", StemDirection = Stem.UP });
            var lh_m4_2 = Rest("8");
            var lh_m4_3 = new StaveNote(new StaveNoteStruct { Keys = new[] { "g/1", "g/2" },   Duration = "8", Clef = "bass", StemDirection = Stem.UP });
            var lh_m4_4 = Rest("8");
            var lh_m4_5 = new StaveNote(new StaveNoteStruct { Keys = new[] { "eb/1", "eb/2" }, Duration = "8", Clef = "bass", StemDirection = Stem.UP });
            var lh_m4_6 = Rest("8");
            var lh_m4_7 = new StaveNote(new StaveNoteStruct { Keys = new[] { "f/1", "f/2" },   Duration = "8", Clef = "bass", StemDirection = Stem.UP });
            var lh_m4_8 = Rest("8");

            var lhVoice = new Voice(new VoiceTime { NumBeats = 8, BeatValue = 4 });
            lhVoice.SetMode(VoiceMode.SOFT);
            lhVoice.AddTickables(new List<Tickable>
            {
                lh_m3_1, lh_m3_2, lh_m3_3, lh_m3_4, lh_m3_5, lh_m3_6, lh_m3_7, lh_m3_8, new BarNote(),
                lh_m4_1, lh_m4_2, lh_m4_3, lh_m4_4, lh_m4_5, lh_m4_6, lh_m4_7, lh_m4_8,
            });

            new Formatter().JoinVoices(new List<Voice> { lhVoice }).Format(new List<Voice> { lhVoice }, FORMAT_W);
            lhVoice.Draw(ctx, staveLH);

            // ── Save PNG ──────────────────────────────────────────────────────

            Directory.CreateDirectory(OutputDir);
            string outPath = Path.Combine(OutputDir, "schubert_avemaria-vfsharp.png");
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

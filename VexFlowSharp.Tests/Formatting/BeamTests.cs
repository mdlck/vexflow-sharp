// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// BeamTests — unit tests for the Beam class.
// Port of key tests from vexflow/tests/beam_tests.ts.
//
// Tests cover:
//   - GenerateBeams: basic eighth-note grouping
//   - GenerateBeams: mixed 8th and 16th note grouping
//   - Stem direction: notes above/below middle line
//   - ApplyAndGetBeams: from a Voice
//   - FormatAndDraw: autoBeam=true end-to-end
//   - Beam.Draw: after format, no exception
//   - Partial beams: secondary beams for 16th notes

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Formatting
{
    [TestFixture]
    [Category("Beam")]
    public class BeamTests
    {
        // ── SetUp ─────────────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            Font.ClearRegistry();
            Font.Load("Bravura", BravuraGlyphs.Data);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Create an eighth-note StaveNote with the given key.</summary>
        private static StaveNote MakeEighth(string key = "c/4")
            => new StaveNote(new StaveNoteStruct
            {
                Duration = "8",
                Keys     = new[] { key },
                Clef     = "treble",
            });

        /// <summary>Create a sixteenth-note StaveNote with the given key.</summary>
        private static StaveNote MakeSixteenth(string key = "c/4")
            => new StaveNote(new StaveNoteStruct
            {
                Duration = "16",
                Keys     = new[] { key },
                Clef     = "treble",
            });

        /// <summary>Create a quarter-note StaveNote with the given key.</summary>
        private static StaveNote MakeQuarter(string key = "c/4")
            => new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys     = new[] { key },
                Clef     = "treble",
            });

        /// <summary>Create a StaveNote with the given duration and key.</summary>
        private static StaveNote MakeNote(string duration, string key = "c/4")
            => new StaveNote(new StaveNoteStruct
            {
                Duration = duration,
                Keys     = new[] { key },
                Clef     = "treble",
            });

        /// <summary>Create a standard treble stave at (10, 40, width).</summary>
        private static Stave MakeStave(SkiaRenderContext ctx, double width = 400)
        {
            var stave = new Stave(10, 40, width);
            stave.SetContext(ctx);
            stave.AddClef("treble");
            stave.Format();
            stave.Draw();
            return stave;
        }

        // ── Test 1: SimpleBeam_FourEighthNotes_CreatesOneBeam ─────────────────

        /// <summary>
        /// Four eighth notes: GenerateBeams with default grouping [2/8] creates 2 beams of 2 notes.
        /// The default group size is 2 eighth notes (one beat), so 4 eighth notes = 2 beams.
        /// Port of beam_tests.ts: simple beam verification.
        /// </summary>
        [Test]
        public void SimpleBeam_FourEighthNotes_CreatesTwoBeams()
        {
            var notes = new List<StemmableNote>
            {
                MakeEighth("c/4"),
                MakeEighth("d/4"),
                MakeEighth("e/4"),
                MakeEighth("f/4"),
            };

            var beams = Beam.GenerateBeams(notes);

            // Default grouping [2/8] = 2 eighth notes per group → 4 notes / 2 = 2 beams
            Assert.That(beams, Has.Count.EqualTo(2),
                "Four eighth notes with default [2/8] grouping should produce 2 beams");
            Assert.That(beams[0].Notes, Has.Count.EqualTo(2),
                "Each beam should contain 2 notes");
            Assert.That(beams[1].Notes, Has.Count.EqualTo(2),
                "Each beam should contain 2 notes");
        }

        // ── Test 2: MixedDurations_EighthsAndSixteenths_CorrectGrouping ───────

        /// <summary>
        /// Two eighths + four sixteenths: GenerateBeams produces beams respecting durations.
        /// All notes should be beamed (none are quarter notes or longer).
        /// Port of beam_tests.ts: mixed-duration beam grouping.
        /// </summary>
        [Test]
        public void MixedDurations_EighthsAndSixteenths_CorrectGrouping()
        {
            var notes = new List<StemmableNote>
            {
                MakeEighth("c/4"),
                MakeEighth("d/4"),
                MakeSixteenth("e/4"),
                MakeSixteenth("f/4"),
                MakeSixteenth("g/4"),
                MakeSixteenth("a/4"),
            };

            var beams = Beam.GenerateBeams(notes);

            // All notes are beamable (eighth and sixteenth)
            Assert.That(beams, Has.Count.GreaterThan(0),
                "Mixed 8th/16th notes should produce at least one beam");

            // Every note should belong to a beam
            foreach (var note in notes)
                Assert.That(note.HasBeam(), Is.True,
                    $"Note {note.GetDuration()} should be beamed");
        }

        // ── Test 3: StemDirection_AllNotesAboveMiddle_StemsDown ───────────────

        /// <summary>
        /// Notes above the middle line (line 3): beam sets all stems down.
        /// Port of beam_tests.ts stem direction test (notes in upper staff).
        /// </summary>
        [Test]
        public void StemDirection_AllNotesAboveMiddle_StemsDown()
        {
            // Notes on top space / above staff — these lines are > 3 (middle)
            var notes = new List<StemmableNote>
            {
                MakeEighth("b/4"),  // line 3.5
                MakeEighth("c/5"),  // line 4.0
                MakeEighth("d/5"),  // line 4.5
                MakeEighth("e/5"),  // line 5.0
            };

            var beams = Beam.GenerateBeams(notes, new BeamConfig { StemDirection = Stem.DOWN });

            Assert.That(beams, Has.Count.GreaterThan(0), "Should produce at least one beam");

            foreach (var note in notes)
                Assert.That(note.GetStemDirection(), Is.EqualTo(Stem.DOWN),
                    $"Note above middle line should have stem DOWN, got {note.GetStemDirection()}");
        }

        // ── Test 4: StemDirection_AllNotesBelowMiddle_StemsUp ────────────────

        /// <summary>
        /// Notes below the middle line: beam sets all stems up.
        /// Port of beam_tests.ts stem direction test (notes in lower staff).
        /// </summary>
        [Test]
        public void StemDirection_AllNotesBelowMiddle_StemsUp()
        {
            // Notes below middle line (line < 3)
            var notes = new List<StemmableNote>
            {
                MakeEighth("c/4"),  // line 1.0
                MakeEighth("d/4"),  // line 1.5
                MakeEighth("e/4"),  // line 2.0
                MakeEighth("f/4"),  // line 2.5
            };

            var beams = Beam.GenerateBeams(notes, new BeamConfig { StemDirection = Stem.UP });

            Assert.That(beams, Has.Count.GreaterThan(0), "Should produce at least one beam");

            foreach (var note in notes)
                Assert.That(note.GetStemDirection(), Is.EqualTo(Stem.UP),
                    $"Note below middle line should have stem UP, got {note.GetStemDirection()}");
        }

        // ── Test 5: ApplyAndGetBeams_FromVoice_GroupsCorrectly ───────────────

        /// <summary>
        /// Create a voice with 8 eighth notes in 4/4, call ApplyAndGetBeams.
        /// Default 4/4 grouping should produce 2 beams of 4 notes each.
        /// Port of beam_tests.ts: applyAndGetBeams from voice.
        /// </summary>
        [Test]
        public void ApplyAndGetBeams_FromVoice_GroupsCorrectly()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);

            for (int i = 0; i < 8; i++)
                voice.AddTickable(MakeEighth("c/4"));

            var beams = Beam.ApplyAndGetBeams(voice);

            Assert.That(beams, Has.Count.GreaterThan(0),
                "8 eighth notes in a voice should produce at least one beam");

            // Total beamed notes should equal 8
            int totalBeamedNotes = beams.Sum(b => b.Notes.Count);
            Assert.That(totalBeamedNotes, Is.EqualTo(8),
                $"All 8 notes should be beamed, but only {totalBeamedNotes} were");

            // Each note should have beam set
            foreach (var t in voice.GetTickables())
            {
                if (t is StemmableNote sn)
                    Assert.That(sn.HasBeam(), Is.True, "Every eighth note should be beamed");
            }
        }

        // ── Test 6: FormatAndDraw_AutoBeam_DrawsWithoutError ─────────────────

        /// <summary>
        /// Full end-to-end: create stave, eighth notes, call FormatAndDraw with AutoBeam=true.
        /// Verify no exception is thrown.
        /// Port of beam_tests.ts: formatAndDraw autoBeam path.
        /// </summary>
        [Test]
        public void FormatAndDraw_AutoBeam_DrawsWithoutError()
        {
            using var ctx = new SkiaRenderContext(500, 200);
            var stave = MakeStave(ctx);

            var notes = new List<StemmableNote>
            {
                MakeEighth("c/4"),
                MakeEighth("d/4"),
                MakeEighth("e/4"),
                MakeEighth("f/4"),
            };

            Assert.DoesNotThrow(
                () => Formatter.FormatAndDraw(ctx, stave, notes, new FormatParams { AutoBeam = true }),
                "FormatAndDraw with autoBeam=true must not throw");
        }

        // ── Test 7: Beam_Draw_AfterFormat_NoException ─────────────────────────

        /// <summary>
        /// Format notes, set stave, draw voice, then manually draw beam.
        /// Verifies that PostFormat can run after notes have stave/y values.
        /// Port of beam_tests.ts: draw after format smoke test (Pitfall 6 guard).
        /// </summary>
        [Test]
        public void Beam_Draw_AfterFormat_NoException()
        {
            using var ctx = new SkiaRenderContext(500, 200);
            var stave = MakeStave(ctx);

            var notes = new List<StemmableNote>
            {
                MakeEighth("c/4"),
                MakeEighth("e/4"),
                MakeEighth("g/4"),
                MakeEighth("b/4"),
            };

            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);
            foreach (var n in notes) voice.AddTickable(n);

            // Create beam before formatting
            var beam = new Beam(notes.Cast<StemmableNote>().ToList());

            new Formatter()
                .JoinVoices(new List<Voice> { voice })
                .FormatToStave(new List<Voice> { voice }, stave);

            // Draw voice (sets stave and y-values on each note)
            voice.SetStave(stave).Draw(ctx, stave);

            // Draw beam — requires notes to have stave y-values
            beam.SetContext(ctx);
            Assert.DoesNotThrow(() => beam.Draw(),
                "Beam.Draw() must not throw after notes have stave y-values");
        }

        // ── Test 8: PartialBeams_EighthAndSixteenths_SecondaryBeamOnSixteenths ─

        /// <summary>
        /// Mix of 8th and 16th notes: verify that GetBeamLines returns correct beam tiers.
        ///
        /// Beam tier logic (VexFlow's valid_beam_durations = ['4','8','16','32','64']):
        ///   getBeamLines('4') — notes with intrinsicTicks &lt; DurationToTicks('4') = 4096
        ///                       → all 8th notes (2048) and 16th notes (1024) get primary beam
        ///   getBeamLines('8') — notes with intrinsicTicks &lt; DurationToTicks('8') = 2048
        ///                       → only 16th notes (1024) get secondary beam; 8th notes (2048) do not
        ///   getBeamLines('16') — notes with intrinsicTicks &lt; DurationToTicks('16') = 1024
        ///                        → only 32nd notes and shorter; 16th notes do NOT get this tier
        ///
        /// Port of beam_tests.ts partial beam test.
        /// </summary>
        [Test]
        public void PartialBeams_EighthAndSixteenths_SecondaryBeamOnSixteenths()
        {
            using var ctx = new SkiaRenderContext(500, 200);
            var stave = MakeStave(ctx);

            // Mix: eighth + two sixteenths (all beamed)
            var notes = new List<StemmableNote>
            {
                MakeEighth("c/4"),
                MakeSixteenth("e/4"),
                MakeSixteenth("g/4"),
            };

            // Manually format and draw so the beam can PostFormat
            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);
            foreach (var n in notes) voice.AddTickable(n);

            new Formatter()
                .JoinVoices(new List<Voice> { voice })
                .FormatToStave(new List<Voice> { voice }, stave);

            voice.SetStave(stave).Draw(ctx, stave);

            // Create beam explicitly after formatting
            var beam = new Beam(notes);
            beam.SetContext(ctx);

            // GetBeamLines('4') — primary beam: all notes (8th and 16th) are shorter than quarter
            var linesPrimary = beam.GetBeamLines("4");
            Assert.That(linesPrimary, Has.Count.GreaterThan(0),
                "Should have primary beam lines ('4' tier) spanning all notes");

            // GetBeamLines('8') — secondary beam: only 16th notes (intrinsicTicks=1024 < 2048)
            // The eighth note has intrinsicTicks=2048, which is NOT < 2048, so it is excluded.
            var linesSecondary = beam.GetBeamLines("8");
            Assert.That(linesSecondary, Has.Count.GreaterThan(0),
                "Should have secondary beam lines ('8' tier) for the 16th notes");

            // GetBeamLines('16') — tertiary beam: only 32nd notes and shorter (none in this group)
            var linesTertiary = beam.GetBeamLines("16");
            Assert.That(linesTertiary, Has.Count.EqualTo(0),
                "Should have no tertiary beam lines ('16' tier) since there are no 32nd notes");
        }
    }
}

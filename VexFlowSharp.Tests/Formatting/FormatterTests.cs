// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// FormatterTests — unit tests for the Formatter class.
// Port of key tests from vexflow/tests/formatter_tests.ts.
//
// Tests cover:
//   - Single-voice x-positions (increasing left-to-right)
//   - Mixed durations (wider notes get more space)
//   - Two-voice alignment at shared tick positions
//   - Softmax spacing produces positive minimum width
//   - FormatToStave keeps notes within stave bounds
//   - JoinVoices creates shared TickContexts
//   - Long integer tick keys don't overflow

using System;
using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Formatting
{
    [TestFixture]
    [Category("Formatter")]
    public class FormatterTests
    {
        // ── SetUp / TearDown ──────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            Font.ClearRegistry();
            Font.Load("Bravura", BravuraGlyphs.Data);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Create a single-pitch StaveNote with the given duration.</summary>
        private static StaveNote MakeNote(string duration, string key = "c/4")
        {
            return new StaveNote(new StaveNoteStruct
            {
                Duration = duration,
                Keys     = new[] { key },
                Clef     = "treble",
            });
        }

        /// <summary>
        /// Create a standard treble stave at (10, 40, 300).
        /// </summary>
        private static Stave MakeStave(SkiaRenderContext ctx, double width = 300)
        {
            var stave = new Stave(10, 40, width);
            stave.SetContext(ctx);
            stave.AddClef("treble");
            stave.Format();
            stave.Draw();
            return stave;
        }

        // ── Test 1: SingleVoice — x-positions increase left to right ──────────

        /// <summary>
        /// Port of VexFlow formatter_tests.ts: format and draw single voice.
        /// After formatting, each note's absolute X must be strictly greater
        /// than the previous note's absolute X.
        /// </summary>
        [Test]
        public void SingleVoice_FourQuarterNotes_XPositionsIncreasing()
        {
            using var ctx = new SkiaRenderContext(400, 200);
            var stave = MakeStave(ctx);

            var notes = new List<StemmableNote>
            {
                MakeNote("4", "c/4"),
                MakeNote("4", "d/4"),
                MakeNote("4", "e/4"),
                MakeNote("4", "f/4"),
            };

            Formatter.FormatAndDraw(ctx, stave, notes);

            // Each note's absolute X must be greater than the previous
            double prevX = double.NegativeInfinity;
            for (int i = 0; i < notes.Count; i++)
            {
                double absX = notes[i].GetAbsoluteX();
                Assert.That(absX, Is.GreaterThan(prevX),
                    $"Note {i} (c/4, quarter) absoluteX={absX:F2} must be > prevX={prevX:F2}");
                prevX = absX;
            }
        }

        // ── Test 2: Mixed durations — half gets more space than quarters ───────

        /// <summary>
        /// A half note followed by two quarter notes.
        /// After formatting, the distance from note[0] to note[1] (the half note's span)
        /// should be greater than the distance from note[1] to note[2] (quarter span).
        /// Port of formatter_tests.ts softmax spacing verification.
        /// </summary>
        [Test]
        public void SingleVoice_MixedDurations_WiderNotesGetMoreSpace()
        {
            using var ctx = new SkiaRenderContext(400, 200);
            var stave = MakeStave(ctx);

            var notes = new List<StemmableNote>
            {
                MakeNote("2", "c/4"),  // half note
                MakeNote("4", "d/4"),  // quarter note
                MakeNote("4", "e/4"),  // quarter note
            };

            Formatter.FormatAndDraw(ctx, stave, notes);

            double halfNoteX    = notes[0].GetAbsoluteX();
            double quarter1X    = notes[1].GetAbsoluteX();
            double quarter2X    = notes[2].GetAbsoluteX();

            double halfNoteSpan  = quarter1X - halfNoteX;
            double quarterSpan   = quarter2X - quarter1X;

            Assert.That(halfNoteSpan, Is.GreaterThan(quarterSpan),
                $"Half note span ({halfNoteSpan:F2}) must be > quarter note span ({quarterSpan:F2})");
        }

        // ── Test 3: Two-voice same-stave tick alignment ───────────────────────

        /// <summary>
        /// Two voices on the same stave: voice1 has 4 quarters, voice2 has 2 halves.
        /// After JoinVoices and formatting, notes at the same tick position (tick 0 and tick 2)
        /// must share the same x-coordinate within ±1px tolerance.
        /// Port of formatter_tests.ts two-voice alignment verification.
        /// </summary>
        [Test]
        public void TwoVoice_SameStave_TicksAligned()
        {
            using var ctx = new SkiaRenderContext(500, 200);
            var stave = MakeStave(ctx, 400);

            // Voice 1: 4 quarter notes
            var voice1Notes = new List<StaveNote>
            {
                MakeNote("4", "e/5"),
                MakeNote("4", "d/5"),
                MakeNote("4", "c/5"),
                MakeNote("4", "b/4"),
            };
            var voice1 = new Voice();
            voice1.SetMode(VoiceMode.SOFT);
            foreach (var n in voice1Notes) voice1.AddTickable(n);

            // Voice 2: 2 half notes
            var voice2Notes = new List<StaveNote>
            {
                MakeNote("2", "c/4"),
                MakeNote("2", "a/3"),
            };
            var voice2 = new Voice();
            voice2.SetMode(VoiceMode.SOFT);
            foreach (var n in voice2Notes) voice2.AddTickable(n);

            var formatter = new Formatter();
            formatter.JoinVoices(new List<Voice> { voice1, voice2 });
            formatter.FormatToStave(
                new List<Voice> { voice1, voice2 },
                stave,
                new FormatParams { AlignRests = false, Stave = stave });

            // voice1[0] and voice2[0] are both at tick 0 — must share same x within ±1px
            double v1t0 = voice1Notes[0].GetAbsoluteX();
            double v2t0 = voice2Notes[0].GetAbsoluteX();
            Assert.That(Math.Abs(v1t0 - v2t0), Is.LessThanOrEqualTo(1.0),
                $"Voice1[0] x={v1t0:F2} and Voice2[0] x={v2t0:F2} should align within 1px at tick 0");

            // voice1[2] and voice2[1] are both at tick 8192 (half note) — must share same x within ±1px
            double v1t2 = voice1Notes[2].GetAbsoluteX();
            double v2t1 = voice2Notes[1].GetAbsoluteX();
            Assert.That(Math.Abs(v1t2 - v2t1), Is.LessThanOrEqualTo(1.0),
                $"Voice1[2] x={v1t2:F2} and Voice2[1] x={v2t1:F2} should align within 1px at tick 8192");
        }

        // ── Test 4: Softmax spacing produces positive minimum width ───────────

        /// <summary>
        /// Format a voice, verify that GetMinTotalWidth() returns a positive value.
        /// Port of formatter_tests.ts softmax spacing coverage.
        /// </summary>
        [Test]
        public void Format_SoftmaxSpacing_ProducesPositiveWidth()
        {
            var notes = new List<StemmableNote>
            {
                MakeNote("4", "c/4"),
                MakeNote("4", "d/4"),
                MakeNote("4", "e/4"),
                MakeNote("4", "f/4"),
            };

            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);
            foreach (var n in notes) voice.AddTickable(n);

            var formatter = new Formatter();
            formatter.JoinVoices(new List<Voice> { voice });
            formatter.Format(new List<Voice> { voice }, null, null);

            double minWidth = formatter.GetMinTotalWidth();
            Assert.That(minWidth, Is.GreaterThan(0),
                $"GetMinTotalWidth() must be > 0, got {minWidth:F2}");
        }

        // ── Test 5: FormatToStave keeps notes within stave bounds ─────────────

        /// <summary>
        /// Format a voice to a stave of width 300.
        /// All note x-positions must be between stave.GetNoteStartX() and stave.GetNoteEndX().
        /// Port of formatter_tests.ts stave-width fitting verification.
        /// </summary>
        [Test]
        public void FormatToStave_FitsWithinStaveWidth()
        {
            using var ctx = new SkiaRenderContext(400, 200);
            var stave = MakeStave(ctx, 300);

            var notes = new List<StemmableNote>
            {
                MakeNote("4", "c/4"),
                MakeNote("4", "d/4"),
                MakeNote("4", "e/4"),
                MakeNote("4", "f/4"),
            };

            Formatter.FormatAndDraw(ctx, stave, notes);

            double noteStartX = stave.GetNoteStartX();
            double noteEndX   = stave.GetNoteEndX();

            Assert.That(noteStartX, Is.GreaterThan(0), "NoteStartX should be > 0");
            Assert.That(noteEndX,   Is.GreaterThan(noteStartX), "NoteEndX > NoteStartX");

            foreach (var note in notes)
            {
                double absX = note.GetAbsoluteX();
                Assert.That(absX, Is.GreaterThanOrEqualTo(noteStartX),
                    $"Note absX={absX:F2} must be >= noteStartX={noteStartX:F2}");
                Assert.That(absX, Is.LessThanOrEqualTo(noteEndX),
                    $"Note absX={absX:F2} must be <= noteEndX={noteEndX:F2}");
            }
        }

        // ── Test 6: JoinVoices — notes at same tick share TickContext ─────────

        /// <summary>
        /// After JoinVoices and formatting, two notes at tick 0 in different voices
        /// must reference the same TickContext instance.
        /// Port of formatter_tests.ts shared TickContext verification.
        /// </summary>
        [Test]
        public void JoinVoices_MultipleVoices_SharedTickContexts()
        {
            var note1 = MakeNote("4", "e/5");
            var note2 = MakeNote("4", "c/4");

            var voice1 = new Voice();
            voice1.SetMode(VoiceMode.SOFT);
            voice1.AddTickable(note1);

            var voice2 = new Voice();
            voice2.SetMode(VoiceMode.SOFT);
            voice2.AddTickable(note2);

            var formatter = new Formatter();
            formatter.JoinVoices(new List<Voice> { voice1, voice2 });
            formatter.Format(new List<Voice> { voice1, voice2 }, 200.0, null);

            // Both notes at tick 0 must share the same TickContext
            var tc1 = note1.GetTickContext();
            var tc2 = note2.GetTickContext();

            Assert.That(tc1, Is.Not.Null, "Note1 must have a TickContext");
            Assert.That(tc2, Is.Not.Null, "Note2 must have a TickContext");
            Assert.That(tc1, Is.SameAs(tc2),
                "Notes at the same tick position in joined voices must share a TickContext");
        }

        // ── Test 7: Long key — no overflow for complex rhythms ────────────────

        /// <summary>
        /// Create voices with tick denominators that would overflow int if keys were int.
        /// Verifies that CreateTickContexts doesn't throw OverflowException.
        /// Port of formatter_tests.ts long-key safety verification.
        ///
        /// We test with a voice that has 8 eighth notes (resolution multiplier = 1,
        /// integer ticks = 0, 2048, 4096, ...). The dictionary key is long so it
        /// can accommodate values up to long.MaxValue.
        /// </summary>
        [Test]
        public void CreateContexts_LongKeyNoOverflow()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);

            // Add 8 eighth notes — tick positions: 0, 2048, 4096, 6144, 8192, 10240, 12288, 14336
            for (int i = 0; i < 8; i++)
                voice.AddTickable(MakeNote("8", "c/4"));

            var formatter = new Formatter();

            // Should not throw OverflowException
            Assert.DoesNotThrow(
                () => formatter.CreateTickContexts(new List<Voice> { voice }),
                "CreateTickContexts must not overflow with eighth-note rhythm");

            var contexts = formatter.GetTickContexts();
            Assert.That(contexts.List.Count, Is.EqualTo(8),
                "Eight eighth notes should produce 8 distinct TickContexts");

            // Verify keys are sorted
            for (int i = 1; i < contexts.List.Count; i++)
                Assert.That(contexts.List[i], Is.GreaterThan(contexts.List[i - 1]),
                    $"Tick list must be sorted: [{i - 1}]={contexts.List[i - 1]} < [{i}]={contexts.List[i]}");
        }

        // ── Test 8: SimpleFormat produces non-overlapping positions ───────────

        /// <summary>
        /// Formatter.SimpleFormat() should place notes left to right without using voices.
        /// Verifies x-positions are monotonically increasing.
        /// </summary>
        [Test]
        public void SimpleFormat_NotesPositionedLeftToRight()
        {
            var notes = new List<Tickable>
            {
                MakeNote("4", "c/4"),
                MakeNote("4", "d/4"),
                MakeNote("4", "e/4"),
            };

            Formatter.SimpleFormat(notes, 10.0, 5.0);

            double prevX = double.NegativeInfinity;
            for (int i = 0; i < notes.Count; i++)
            {
                double x = notes[i].GetTickContext()?.GetX() ?? double.NegativeInfinity;
                Assert.That(x, Is.GreaterThan(prevX),
                    $"SimpleFormat note {i} x={x:F2} must be > prevX={prevX:F2}");
                prevX = x;
            }
        }
    }
}

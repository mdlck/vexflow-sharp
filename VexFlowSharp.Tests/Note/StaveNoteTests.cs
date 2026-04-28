// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// StaveNote unit tests.
// Ports test scenarios from vexflow/tests/stavenote_tests.ts:
//   - Tick counts for all durations
//   - Stem direction (manual and auto)
//   - Stem extension pitch
//   - Adjacent-note displacement
//   - Staff line positions
//   - PreFormat width
//   - TickContext fallback (GetAbsoluteX without TickContext)
//   - Rest detection
//   - All standard durations construct without error

using System;
using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("StaveNote")]
    public class StaveNoteTests
    {
        [SetUp]
        public void SetUp()
        {
            Font.ClearRegistry();
            Font.Load("Bravura", BravuraGlyphs.Data);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        /// <summary>Convenience: create a StaveNote with minimal struct.</summary>
        private static StaveNote Note(string duration, string[] keys, bool autoStem = false, int stemDir = Stem.UP)
        {
            return new StaveNote(new StaveNoteStruct
            {
                Duration = duration,
                Keys = keys,
                AutoStem = autoStem ? (bool?)true : null,
                StemDirection = autoStem ? (int?)null : stemDir,
            });
        }

        // ── StaveNote_Tick ────────────────────────────────────────────────────

        /// <summary>
        /// Quarter note has RESOLUTION/4 ticks.
        /// Port of VexFlow stavenote_tests.ts::ticks.
        /// </summary>
        [Test]
        public void StaveNote_Tick_Quarter_Is4096()
        {
            var n = Note("4", new[] { "c/4", "e/4", "g/4" });
            Assert.That(n.GetTicks().Numerator, Is.EqualTo(Tables.RESOLUTION / 4));
        }

        [Test]
        public void StaveNote_Tick_Eighth_Is2048()
        {
            var n = Note("8", new[] { "c/4" });
            Assert.That(n.GetTicks().Numerator, Is.EqualTo(Tables.RESOLUTION / 8));
        }

        [Test]
        public void StaveNote_Tick_Half_Is8192()
        {
            var n = Note("2", new[] { "c/4" });
            Assert.That(n.GetTicks().Numerator, Is.EqualTo(Tables.RESOLUTION / 2));
        }

        [Test]
        public void StaveNote_Tick_Whole_Is16384()
        {
            var n = Note("1", new[] { "c/4" });
            Assert.That(n.GetTicks().Numerator, Is.EqualTo(Tables.RESOLUTION));
        }

        [Test]
        public void StaveNote_Tick_DottedHalf_Is3xBeat()
        {
            // Dotted half = RESOLUTION/2 + RESOLUTION/4 = 12288
            // Use Dots=1 property syntax (VexFlow "hd" string syntax uses type 'd' in C# port)
            var n = new StaveNote(new StaveNoteStruct { Duration = "2", Dots = 1, Keys = new[] { "c/4" } });
            int expected = Tables.RESOLUTION / 2 + Tables.RESOLUTION / 4;
            Assert.That(n.GetTicks().Numerator, Is.EqualTo(expected));
        }

        // ── StaveNote_Stem ────────────────────────────────────────────────────

        /// <summary>
        /// Default stem direction is UP; can be overridden to DOWN.
        /// Port of VexFlow stavenote_tests.ts::stem.
        /// </summary>
        [Test]
        public void StaveNote_Stem_DefaultIsUp()
        {
            var n = Note("w", new[] { "c/4", "e/4", "g/4" });
            Assert.That(n.GetStemDirection(), Is.EqualTo(Stem.UP));
        }

        [Test]
        public void StaveNote_Stem_ManualDown()
        {
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
                StemDirection = Stem.DOWN,
            });
            Assert.That(n.GetStemDirection(), Is.EqualTo(Stem.DOWN));
        }

        [Test]
        public void StaveNote_Stem_ManualUp()
        {
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
                StemDirection = Stem.UP,
            });
            Assert.That(n.GetStemDirection(), Is.EqualTo(Stem.UP));
        }

        // ── StaveNote_AutoStem ────────────────────────────────────────────────

        /// <summary>
        /// AutoStem chooses direction from average staff line vs median 3.0.
        /// Port of VexFlow stavenote_tests.ts::autoStem.
        /// </summary>
        [Test]
        public void StaveNote_AutoStem_HighNotes_StemDown()
        {
            // c/5 e/5 g/5 — all above line 3
            var n = Note("8", new[] { "c/5", "e/5", "g/5" }, autoStem: true);
            Assert.That(n.GetStemDirection(), Is.EqualTo(Stem.DOWN));
        }

        [Test]
        public void StaveNote_AutoStem_LowNotes_StemUp()
        {
            // e/4 g/4 c/5 — average line below 3
            var n = Note("8", new[] { "e/4", "g/4", "c/5" }, autoStem: true);
            Assert.That(n.GetStemDirection(), Is.EqualTo(Stem.UP));
        }

        [Test]
        public void StaveNote_AutoStem_SingleHighNote_StemDown()
        {
            // c/5 is above middle of staff
            var n = Note("8", new[] { "c/5" }, autoStem: true);
            Assert.That(n.GetStemDirection(), Is.EqualTo(Stem.DOWN));
        }

        [Test]
        public void StaveNote_AutoStem_b4_StemDown()
        {
            // b/4 is at line 3 (the median), so stem goes DOWN (>= 3)
            var n = Note("8", new[] { "b/4" }, autoStem: true);
            Assert.That(n.GetStemDirection(), Is.EqualTo(Stem.DOWN));
        }

        // ── StaveNote_StemExtensionPitch ──────────────────────────────────────

        /// <summary>
        /// Stem extension is 0 for notes near middle; positive for far notes.
        /// Port of VexFlow stavenote_tests.ts::stemExtensionPitch.
        /// </summary>
        [Test]
        public void StaveNote_StemExtension_NearMiddle_IsZero()
        {
            var n = Note("4", new[] { "c/5", "e/5", "g/5" }, autoStem: true);
            Assert.That(n.GetStemExtension(), Is.EqualTo(0));
        }

        [Test]
        public void StaveNote_StemExtension_LowNote_IsPositive()
        {
            // f/3 with auto-stem UP should extend the stem
            var n = Note("4", new[] { "f/3" }, autoStem: true);
            Assert.That(n.GetStemExtension(), Is.GreaterThan(0));
        }

        [Test]
        public void StaveNote_StemExtension_ManualOppositeDirection_IsZero()
        {
            // f/3 with forced DOWN (opposite of optimal) — no extension
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "f/3" },
                StemDirection = Stem.DOWN,
            });
            Assert.That(n.GetStemExtension(), Is.EqualTo(0));
        }

        [Test]
        public void StaveNote_StemExtension_WideStave_ScalesWithSpacing()
        {
            // With wider stave spacing, stem extension should scale proportionally
            var n = Note("4", new[] { "f/3" }, autoStem: true);
            double normalExtension = n.GetStemExtension();

            // Wide stave (20px spacing instead of 10px)
            var stave = new Stave(10, 10, 300, new StaveOptions { SpacingBetweenLinesPx = 20 });
            n.SetStave(stave);

            // Extension should double with doubled spacing
            Assert.That(n.GetStemExtension(), Is.EqualTo(normalExtension * 2));
        }

        // ── StaveNote_StemDirectionDisplacement ───────────────────────────────

        /// <summary>
        /// Adjacent noteheads (c/5, d/5) cause one to be displaced.
        /// Port of VexFlow stavenote_tests.ts::setStemDirectionDisplacement.
        /// </summary>
        [Test]
        public void StaveNote_StemDirectionDisplacement_StemUp_MiddleNoteDisplaced()
        {
            // c/5 d/5 g/5 stem-up: d/5 is adjacent to c/5 so it displaces
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/5", "d/5", "g/5" },
                StemDirection = Stem.UP,
            });
            var heads = n.GetNoteHeads();
            // Stem-up order: bottom-to-top → [c/5, d/5, g/5]; d/5 is adjacent to c/5
            // Expected: [false, true, false]
            Assert.That(heads[0].IsDisplaced(), Is.False,  "c/5 not displaced");
            Assert.That(heads[1].IsDisplaced(), Is.True,   "d/5 displaced");
            Assert.That(heads[2].IsDisplaced(), Is.False,  "g/5 not displaced");
        }

        [Test]
        public void StaveNote_StemDirectionDisplacement_StemDown_FirstNoteDisplaced()
        {
            // Stem-down order iterates top-to-bottom; c/5 is adjacent to d/5
            // Expected: [true, false, false]
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/5", "d/5", "g/5" },
                StemDirection = Stem.DOWN,
            });
            var heads = n.GetNoteHeads();
            Assert.That(heads[0].IsDisplaced(), Is.True,   "c/5 displaced (stem down)");
            Assert.That(heads[1].IsDisplaced(), Is.False,  "d/5 not displaced (stem down)");
            Assert.That(heads[2].IsDisplaced(), Is.False,  "g/5 not displaced");
        }

        [Test]
        public void StaveNote_StemDirectionDisplacement_SetStemDirection_RebuildsHeads()
        {
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/5", "d/5", "g/5" },
                StemDirection = Stem.UP,
            });
            // Verify initial UP state
            Assert.That(n.GetNoteHeads()[1].IsDisplaced(), Is.True);

            // Switch to DOWN and verify displacement updated
            n.SetStemDirection(Stem.DOWN);
            Assert.That(n.GetNoteHeads()[0].IsDisplaced(), Is.True);
            Assert.That(n.GetNoteHeads()[1].IsDisplaced(), Is.False);
        }

        [Test]
        public void StaveNote_SetKeyStyle_PreservesStyleAcrossReset()
        {
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/5", "d/5" },
                StemDirection = Stem.UP,
            });
            n.SetKeyStyle(1, new ElementStyle { FillStyle = "red", StrokeStyle = "blue" });

            n.SetStemDirection(Stem.DOWN);

            var style = n.GetKeyStyle(1);
            Assert.That(style, Is.Not.Null);
            Assert.That(style!.FillStyle, Is.EqualTo("red"));
            Assert.That(style.StrokeStyle, Is.EqualTo("blue"));
        }

        [Test]
        public void StaveNote_SetKeyLine_PreservesKeyStyle()
        {
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/5" },
                StemDirection = Stem.UP,
            });
            n.SetKeyStyle(0, new ElementStyle { FillStyle = "green" });

            n.SetKeyLine(0, 2.5);

            Assert.That(n.GetKeyStyle(0)?.FillStyle, Is.EqualTo("green"));
            Assert.That(n.GetKeyLine(0), Is.EqualTo(2.5));
        }

        [Test]
        public void StaveNote_UnisonKeys_MarkKeyPropsDisplacedForModifierLayout()
        {
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/5", "c/5" },
                StemDirection = Stem.DOWN,
            });

            var props = n.GetKeyProps();
            Assert.That(props[0].Displaced, Is.True);
            Assert.That(props[1].Displaced, Is.True);
        }

        // ── StaveNote_StaveLine ───────────────────────────────────────────────

        /// <summary>
        /// Key props line values for c/4 e/4 a/4 in treble clef.
        /// Port of VexFlow stavenote_tests.ts::staveLine.
        /// </summary>
        [Test]
        public void StaveNote_StaveLine_C4_IsLine0()
        {
            var n = Note("w", new[] { "c/4", "e/4", "a/4" });
            var props = n.GetKeyProps();
            Assert.That(props[0].Line, Is.EqualTo(0.0), "C/4 on line 0");
        }

        [Test]
        public void StaveNote_StaveLine_E4_IsLine1()
        {
            var n = Note("w", new[] { "c/4", "e/4", "a/4" });
            var props = n.GetKeyProps();
            Assert.That(props[1].Line, Is.EqualTo(1.0), "E/4 on line 1");
        }

        [Test]
        public void StaveNote_StaveLine_A4_IsLine2_5()
        {
            var n = Note("w", new[] { "c/4", "e/4", "a/4" });
            var props = n.GetKeyProps();
            Assert.That(props[2].Line, Is.EqualTo(2.5), "A/4 on line 2.5");
        }

        [Test]
        public void StaveNote_StaveLine_YValues_FromStave()
        {
            var stave = new Stave(10, 10, 300);
            var n = Note("w", new[] { "c/4", "e/4", "a/4" });
            n.SetStave(stave);

            var ys = n.GetYs();
            // GetYForNote: y + headroom*spacing + 5*spacing - line*spacing
            // = 10 + 4*10 + 5*10 - line*10 = 100 - line*10
            Assert.That(ys.Length, Is.EqualTo(3), "Chord should have 3 Y values");
            Assert.That(ys[0], Is.EqualTo(100.0), "Y for C/4 (line 0)");
            Assert.That(ys[1], Is.EqualTo(90.0),  "Y for E/4 (line 1)");
            Assert.That(ys[2], Is.EqualTo(75.0),  "Y for A/4 (line 2.5)");
        }

        // ── StaveNote_Width ───────────────────────────────────────────────────

        /// <summary>
        /// After PreFormat(), width should be positive.
        /// </summary>
        [Test]
        public void StaveNote_Width_AfterPreFormat_IsPositive()
        {
            var n = Note("4", new[] { "c/4", "e/4", "a/4" });
            n.PreFormat();
            Assert.That(n.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void StaveNote_Width_WholeNote_AfterPreFormat_IsPositive()
        {
            var n = Note("1", new[] { "c/4" });
            n.PreFormat();
            Assert.That(n.GetWidth(), Is.GreaterThan(0));
        }

        // ── StaveNote_TickContext ─────────────────────────────────────────────

        /// <summary>
        /// GetAbsoluteX() returns x when tickContext is null (Phase 2 Pitfall 4 guard).
        /// Port of VexFlow stavenote_tests.ts::tickContext.
        /// </summary>
        [Test]
        public void StaveNote_TickContext_GetAbsoluteX_ReturnsX_WithoutTickContext()
        {
            var n = Note("w", new[] { "c/4", "e/4", "a/4" });
            n.SetX(42.0);
            // No TickContext set — should return x directly
            Assert.That(n.GetAbsoluteX(), Is.EqualTo(42.0));
        }

        [Test]
        public void StaveNote_TickContext_NoException_WithStave()
        {
            var stave = new Stave(10, 10, 400);
            var n = Note("w", new[] { "c/4", "e/4", "a/4" });
            Assert.DoesNotThrow(() => n.SetStave(stave));
        }

        [Test]
        public void StaveNote_XNoteheadStemUp_UsesStemAnchorYOffset()
        {
            var stave = new Stave(10, 10, 400);
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4/x2" },
                StemDirection = Stem.UP,
            });

            n.SetStave(stave);

            var expectedBaseY = stave.GetYForNote(0) - Tables.NoteHeadStemYOffsets["noteheadXBlack"].Up * Tables.STAVE_LINE_DISTANCE;
            Assert.That(n.CheckStem().GetExtents().BaseY, Is.EqualTo(expectedBaseY).Within(0.001));
        }

        // ── StaveNote_Duration_Rest ───────────────────────────────────────────

        /// <summary>
        /// StaveNote with duration "4r" is a rest.
        /// </summary>
        [Test]
        public void StaveNote_Duration_Rest_IsRest()
        {
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4r",
                Keys = new[] { "r/4" },
            });
            Assert.That(n.IsRest(), Is.True);
        }

        [Test]
        public void StaveNote_Duration_NormalNote_IsNotRest()
        {
            var n = Note("4", new[] { "c/4" });
            Assert.That(n.IsRest(), Is.False);
        }

        [Test]
        public void StaveNote_Duration_Rest_NoteType_Is_r()
        {
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "4r",
                Keys = new[] { "r/4" },
            });
            Assert.That(n.GetNoteType(), Is.EqualTo("r"));
        }

        // ── StaveNote_AllDurations ────────────────────────────────────────────

        /// <summary>
        /// All standard durations construct without exception and have positive width after PreFormat.
        /// </summary>
        [Test]
        public void StaveNote_AllDurations_ConstructWithoutException()
        {
            string[] durations = { "1", "2", "4", "8", "16", "32", "64" };
            foreach (var dur in durations)
            {
                Assert.DoesNotThrow(
                    () => Note(dur, new[] { "c/4" }),
                    $"Duration '{dur}' should construct without exception");
            }
        }

        [Test]
        public void StaveNote_AllDurations_PositiveWidthAfterPreFormat()
        {
            string[] durations = { "1", "2", "4", "8", "16", "32", "64" };
            foreach (var dur in durations)
            {
                var n = Note(dur, new[] { "c/4" });
                n.PreFormat();
                Assert.That(n.GetWidth(), Is.GreaterThan(0),
                    $"Duration '{dur}' should have positive width after PreFormat");
            }
        }

        [Test]
        public void StaveNote_AllRestDurations_ConstructWithoutException()
        {
            string[] durations = { "1r", "2r", "4r", "8r", "16r", "32r", "64r" };
            foreach (var dur in durations)
            {
                Assert.DoesNotThrow(
                    () => new StaveNote(new StaveNoteStruct { Duration = dur, Keys = new[] { "r/4" } }),
                    $"Rest duration '{dur}' should construct without exception");
            }
        }

        [Test]
        public void StaveNote_RestTextMetrics_ExposeGlyphBounds()
        {
            var n = new StaveNote(new StaveNoteStruct { Duration = "32r", Keys = new[] { "r/4" } });
            var metrics = n.GetNoteHeads()[0].GetTextMetrics();

            Assert.That(metrics.ActualBoundingBoxAscent, Is.GreaterThan(0));
            Assert.That(metrics.ActualBoundingBoxDescent, Is.GreaterThan(0));
        }

        [Test]
        public void StaveNote_GetModifierStartXY_RestUsesV5RestShift()
        {
            var stave = new Stave(10, 10, 400);
            var n = new StaveNote(new StaveNoteStruct { Duration = "32r", Keys = new[] { "r/4" } });
            n.SetStave(stave);
            n.SetX(100);
            n.PreFormat();

            var xy = n.GetModifierStartXY(ModifierPosition.Right, 0);

            Assert.That(xy.X, Is.EqualTo(100 + n.GetGlyphWidth() + 2).Within(0.001));
            Assert.That(xy.Y, Is.EqualTo(stave.GetYForNote(3) - 1.5 * stave.GetSpacingBetweenLines()).Within(0.001));
        }

        [Test]
        public void StaveNote_GetModifierStartXY_ForceFlagRightAddsFlagWidth()
        {
            var stave = new Stave(10, 10, 400);
            var n = new StaveNote(new StaveNoteStruct
            {
                Duration = "8",
                Keys = new[] { "c/4", "e/4" },
                StemDirection = Stem.UP,
            });
            n.SetStave(stave);
            n.SetX(100);
            n.PreFormat();

            var plain = n.GetModifierStartXY(ModifierPosition.Right, 0);
            var forced = n.GetModifierStartXY(ModifierPosition.Right, 0, new StaveNoteModifierStartOptions { ForceFlagRight = true });

            Assert.That(forced.X, Is.GreaterThan(plain.X + 1));
            Assert.That(plain.Y, Is.EqualTo(stave.GetYForNote(0)).Within(0.001));
        }

        [Test]
        public void StaveNote_Format_UnisonSameStyleDoesNotShift()
        {
            var upper = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
                StemDirection = Stem.UP,
            });
            var lower = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
                StemDirection = Stem.UP,
            });
            var state = new ModifierContextState();

            StaveNote.Format(new List<StaveNote> { upper, lower }, state);

            Assert.That(upper.GetXShift(), Is.EqualTo(0));
            Assert.That(lower.GetXShift(), Is.EqualTo(0));
            Assert.That(state.RightShift, Is.EqualTo(0));
        }

        [Test]
        public void StaveNote_Format_UnisonDifferentStyleShiftsUpperVoice()
        {
            var upper = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
                StemDirection = Stem.UP,
            });
            var lower = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
                StemDirection = Stem.UP,
            });
            lower.SetStyle(new ElementStyle { FillStyle = "red" });
            var state = new ModifierContextState();

            StaveNote.Format(new List<StaveNote> { upper, lower }, state);

            Assert.That(upper.GetXShift(), Is.GreaterThan(0));
            Assert.That(lower.GetXShift(), Is.EqualTo(0));
            Assert.That(state.RightShift, Is.EqualTo(upper.GetXShift()));
        }

        // ── StaveNote_IsChord ─────────────────────────────────────────────────

        [Test]
        public void StaveNote_IsChord_MultipleKeys_True()
        {
            var n = Note("4", new[] { "c/4", "e/4", "g/4" });
            Assert.That(n.IsChord(), Is.True);
        }

        [Test]
        public void StaveNote_IsChord_SingleKey_False()
        {
            var n = Note("4", new[] { "c/4" });
            Assert.That(n.IsChord(), Is.False);
        }

        // ── StaveNote_NoteHeadCount ───────────────────────────────────────────

        [Test]
        public void StaveNote_NoteHeadCount_MatchesKeyCount()
        {
            var n = Note("4", new[] { "c/4", "e/4", "g/4" });
            Assert.That(n.GetNoteHeads().Count, Is.EqualTo(3));
        }

        [Test]
        public void StaveNote_NoteHeadCount_SingleNote()
        {
            var n = Note("4", new[] { "c/4" });
            Assert.That(n.GetNoteHeads().Count, Is.EqualTo(1));
        }
    }
}

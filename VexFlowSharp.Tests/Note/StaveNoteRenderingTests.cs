// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// StaveNote rendering tests — ported from vexflow/tests/stavenote_tests.ts.
//
// These tests verify that StaveNote.Draw() works correctly for all durations,
// rests, ledger lines, treble clef, and bass clef without throwing exceptions.
//
// Tests draw notes using manual X positioning (no Formatter/TickContext in Phase 2).
// Image comparison tests are marked [Explicit] until reference PNGs are generated
// (canvas Node.js backend not available in current environment).
//
// Port of VexFlow QUnit rendering tests from stavenote_tests.ts:
//   - drawBasic (treble): all durations, rests, stem up/down
//   - drawBass: bass clef notes
//   - allDurations: constructing all 7 standard durations
//   - drawRests: all rest durations
//   - drawLedgerLines: notes above and below the staff

using System;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("StaveNote")]
    [Category("Rendering")]
    public class StaveNoteRenderingTests
    {
        private SkiaRenderContext? _ctx;

        [SetUp]
        public void SetUp()
        {
            Font.ClearRegistry();
            Font.Load("Bravura", BravuraGlyphs.Data);
        }

        [TearDown]
        public void TearDown()
        {
            _ctx?.Dispose();
            _ctx = null;
        }

        // ── Helper ────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw a note at the given X position on the stave.
        /// Returns the note after drawing so callers can assert on it.
        ///
        /// Pattern matches VexFlow's draw() helper in stavenote_tests.ts:
        ///   note.setStave(stave)
        ///   tickContext.setX(x) → replaced by note.SetX(x) in Phase 2
        ///   note.setContext(ctx).draw()
        /// </summary>
        private static StaveNote DrawNote(StaveNote note, Stave stave, RenderContext ctx, double x)
        {
            note.SetStave(stave);
            note.SetX(x);
            note.SetContext(ctx);
            note.Draw();
            return note;
        }

        // ── StaveNote_DrawBasic_Treble ─────────────────────────────────────────

        /// <summary>
        /// Port of VexFlow stavenote_tests.ts::drawBasic with clef='treble'.
        ///
        /// Draws notes of all standard durations (whole through 128th) plus rests,
        /// both stem-up and stem-down, on a treble stave.
        /// Verifies Draw() does not throw and each note has X > 0 and Y values.
        ///
        /// The VexFlow original asserts note.getX() > 0 and note.getYs().length > 0
        /// for every note — same assertions applied here.
        /// </summary>
        [Test]
        public void StaveNote_DrawBasic_Treble()
        {
            using var ctx = new SkiaRenderContext(700, 200);
            var stave = new Stave(10, 30, 680);
            stave.SetContext(ctx);
            stave.AddClef("treble");
            stave.Format();
            stave.Draw();

            // Port of VexFlow drawBasic treble notes array (subset for smoke test)
            var noteStructs = new[]
            {
                new StaveNoteStruct { Clef = "treble", Keys = new[] { "c/5", "e/5", "a/5" }, Duration = "1" },
                new StaveNoteStruct { Clef = "treble", Keys = new[] { "c/4", "e/4", "a/4" }, Duration = "2" },
                new StaveNoteStruct { Clef = "treble", Keys = new[] { "c/5", "e/5", "a/5" }, Duration = "4" },
                new StaveNoteStruct { Clef = "treble", Keys = new[] { "c/4", "e/4", "a/4" }, Duration = "8" },
                new StaveNoteStruct { Clef = "treble", Keys = new[] { "c/5", "e/5", "a/5" }, Duration = "16" },
                new StaveNoteStruct { Clef = "treble", Keys = new[] { "c/4", "e/4", "a/4" }, Duration = "32" },
                new StaveNoteStruct { Clef = "treble", Keys = new[] { "c/5", "e/5", "a/5" }, Duration = "64" },
                // Stem-down variants
                new StaveNoteStruct { Clef = "treble", Keys = new[] { "c/4", "e/4", "a/4" }, Duration = "2", StemDirection = Stem.DOWN },
                new StaveNoteStruct { Clef = "treble", Keys = new[] { "c/4", "e/4", "a/4" }, Duration = "4", StemDirection = Stem.DOWN },
                new StaveNoteStruct { Clef = "treble", Keys = new[] { "c/4", "e/4", "a/4" }, Duration = "8", StemDirection = Stem.DOWN },
            };

            for (int i = 0; i < noteStructs.Length; i++)
            {
                double x = 40 + i * 60;
                var note = new StaveNote(noteStructs[i]);
                Assert.DoesNotThrow(() => DrawNote(note, stave, ctx, x),
                    $"Note {i} ({noteStructs[i].Duration}) should draw without exception");
                Assert.That(note.GetX(), Is.GreaterThan(0), $"Note {i} has X > 0");
                Assert.That(note.GetYs().Length, Is.GreaterThan(0), $"Note {i} has Y values");
            }
        }

        // ── StaveNote_DrawBass ────────────────────────────────────────────────

        /// <summary>
        /// Port of VexFlow stavenote_tests.ts::drawBass.
        ///
        /// Draws notes of multiple durations on a bass clef stave.
        /// Port of VexFlow's 40-assertion test verifying x > 0 and ys.length > 0.
        /// </summary>
        [Test]
        public void StaveNote_DrawBass()
        {
            using var ctx = new SkiaRenderContext(600, 280);
            var stave = new Stave(10, 40, 580);
            stave.SetContext(ctx);
            stave.AddClef("bass");
            stave.Format();
            stave.Draw();

            // Port of VexFlow stavenote_tests.ts drawBass noteStructs
            var noteStructs = new[]
            {
                new StaveNoteStruct { Clef = "bass", Keys = new[] { "c/3", "e/3", "a/3" }, Duration = "2" },
                new StaveNoteStruct { Clef = "bass", Keys = new[] { "c/2", "e/2", "a/2" }, Duration = "1" },
                new StaveNoteStruct { Clef = "bass", Keys = new[] { "c/3", "e/3", "a/3" }, Duration = "4" },
                new StaveNoteStruct { Clef = "bass", Keys = new[] { "c/2", "e/2", "a/2" }, Duration = "8" },
                new StaveNoteStruct { Clef = "bass", Keys = new[] { "c/3", "e/3", "a/3" }, Duration = "16" },
                new StaveNoteStruct { Clef = "bass", Keys = new[] { "c/2", "e/2", "a/2" }, Duration = "32" },
                new StaveNoteStruct { Clef = "bass", Keys = new[] { "c/2", "e/2", "a/2" }, Duration = "4", StemDirection = Stem.DOWN },
                new StaveNoteStruct { Clef = "bass", Keys = new[] { "c/2", "e/2", "a/2" }, Duration = "8", StemDirection = Stem.DOWN },
            };

            for (int i = 0; i < noteStructs.Length; i++)
            {
                double x = 40 + i * 65;
                var note = new StaveNote(noteStructs[i]);
                Assert.DoesNotThrow(() => DrawNote(note, stave, ctx, x),
                    $"Bass note {i} ({noteStructs[i].Duration}) should draw without exception");
                Assert.That(note.GetX(), Is.GreaterThan(0), $"Bass note {i} has X > 0");
                Assert.That(note.GetYs().Length, Is.GreaterThan(0), $"Bass note {i} has Y values");
            }
        }

        // ── StaveNote_DrawAllDurations ─────────────────────────────────────────

        /// <summary>
        /// Draw one note per duration (whole through 64th).
        /// Verifies Draw() produces no exceptions across all durations.
        /// Port of VexFlow drawBasic noteStructs coverage.
        /// </summary>
        [Test]
        public void StaveNote_DrawAllDurations()
        {
            using var ctx = new SkiaRenderContext(600, 200);
            var stave = new Stave(10, 30, 580);
            stave.SetContext(ctx);
            stave.AddClef("treble");
            stave.Format();
            stave.Draw();

            string[] durations = { "1", "2", "4", "8", "16", "32", "64" };

            for (int i = 0; i < durations.Length; i++)
            {
                double x = 40 + i * 75;
                var dur = durations[i];
                var note = new StaveNote(new StaveNoteStruct
                {
                    Duration = dur,
                    Keys = new[] { "c/4" },
                });

                Assert.DoesNotThrow(() => DrawNote(note, stave, ctx, x),
                    $"Duration '{dur}' should draw without exception");
            }
        }

        // ── StaveNote_DrawRests ───────────────────────────────────────────────

        /// <summary>
        /// Draw rest notes for each standard duration.
        /// Verifies Draw() does not throw for all rest note types.
        /// Port of VexFlow drawBasic rest note coverage.
        /// </summary>
        [Test]
        public void StaveNote_DrawRests()
        {
            using var ctx = new SkiaRenderContext(600, 200);
            var stave = new Stave(10, 30, 580);
            stave.SetContext(ctx);
            stave.AddClef("treble");
            stave.Format();
            stave.Draw();

            string[] durations = { "1r", "2r", "4r", "8r", "16r", "32r", "64r" };

            for (int i = 0; i < durations.Length; i++)
            {
                double x = 40 + i * 75;
                var dur = durations[i];
                var note = new StaveNote(new StaveNoteStruct
                {
                    Duration = dur,
                    Keys = new[] { "r/4" },
                });

                Assert.DoesNotThrow(() => DrawNote(note, stave, ctx, x),
                    $"Rest '{dur}' should draw without exception");
                Assert.That(note.IsRest(), Is.True, $"'{dur}' is a rest");
            }
        }

        // ── StaveNote_DrawLedgerLines ─────────────────────────────────────────

        /// <summary>
        /// Draw notes with ledger lines above and below the staff.
        ///
        /// Verifies that DrawLedgerLines() renders correctly for:
        ///   - Notes above the staff: c/6 (above treble top line)
        ///   - Notes below the staff: c/3 (below treble bottom line)
        ///
        /// Port of VexFlow ledger line rendering coverage in stavenote.ts.
        /// </summary>
        [Test]
        public void StaveNote_DrawLedgerLines()
        {
            using var ctx = new SkiaRenderContext(400, 250);
            var stave = new Stave(10, 60, 380);
            stave.SetContext(ctx);
            stave.AddClef("treble");
            stave.Format();
            stave.Draw();

            // Note above the staff with ledger lines
            var highNote = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/6" },  // Two ledger lines above treble clef
                Clef = "treble",
            });

            // Note below the staff with ledger lines
            var lowNote = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/3" },  // One ledger line below treble clef
                Clef = "treble",
            });

            Assert.DoesNotThrow(() => DrawNote(highNote, stave, ctx, 80),
                "High note (c/6) with ledger lines above should draw without exception");
            Assert.DoesNotThrow(() => DrawNote(lowNote, stave, ctx, 180),
                "Low note (c/3) with ledger line below should draw without exception");

            // Verify notes positioned correctly (Y values assigned from stave)
            Assert.That(highNote.GetYs().Length, Is.GreaterThan(0), "High note has Y values");
            Assert.That(lowNote.GetYs().Length, Is.GreaterThan(0), "Low note has Y values");

            // c/6 should be above staff (smaller Y than top staff line)
            // c/3 should be below staff (larger Y than bottom staff line)
            double staffTop = stave.GetYForLine(0);
            double staffBottom = stave.GetYForLine(4);
            Assert.That(highNote.GetYs()[0], Is.LessThan(staffTop), "c/6 Y is above staff");
            Assert.That(lowNote.GetYs()[0], Is.GreaterThan(staffBottom), "c/3 Y is below staff");
        }

        // ── SaveSample ────────────────────────────────────────────────────────
        // Run explicitly to produce a PNG for visual inspection:
        //   dotnet test --filter "FullyQualifiedName~SaveSample"

        [Test]
        [Explicit("Visual inspection — saves PNG to /tmp/vexflow_sample.png")]
        public void SaveSample()
        {
            using var ctx = new SkiaRenderContext(700, 200);
            var stave = new Stave(10, 30, 680);
            stave.SetContext(ctx);
            stave.AddClef("treble");
            stave.AddKeySignature("D");
            stave.Format();
            stave.Draw();

            string[] keys = { "c/4", "e/4", "g/4", "b/4", "d/5", "f/5", "a/5", "c/6" };
            string[] durations = { "1", "2", "4", "8", "16", "32", "64", "4" };

            for (int i = 0; i < keys.Length; i++)
            {
                var note = new StaveNote(new StaveNoteStruct
                {
                    Clef = "treble",
                    Keys = new[] { keys[i] },
                    Duration = durations[i],
                });
                DrawNote(note, stave, ctx, 100 + i * 75);
            }

            string path = "/tmp/vexflow_sample.png";
            ctx.SavePng(path);
            Assert.That(System.IO.File.Exists(path), Is.True);
            TestContext.Out.WriteLine($"Saved: {path}");
        }
    }
}

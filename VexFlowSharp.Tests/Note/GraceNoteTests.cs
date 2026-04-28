// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// GraceNote and GraceNoteGroup unit tests.
// Verifies: SCALE=0.66, LEDGER_LINE_OFFSET=2, 0.66 scale, width=3, GraceNoteGroup.GetWidth() > 0.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("GraceNote")]
    public class GraceNoteTests
    {
        [SetUp]
        public void SetUp()
        {
            Font.ClearRegistry();
            Font.Load("Bravura", BravuraGlyphs.Data);
        }

        // ── Constants ──────────────────────────────────────────────────────────

        /// <summary>
        /// GraceNote.SCALE is 0.66 — two thirds of standard notehead size.
        /// Port of VexFlow gracenote.ts::SCALE.
        /// </summary>
        [Test]
        public void GraceNote_SCALE_Is066()
        {
            Assert.That(GraceNote.SCALE, Is.EqualTo(0.66));
        }

        /// <summary>
        /// GraceNote.LEDGER_LINE_OFFSET is 2 — shorter ledger lines than standard.
        /// Port of VexFlow gracenote.ts::LEDGER_LINE_OFFSET.
        /// </summary>
        [Test]
        public void GraceNote_LEDGER_LINE_OFFSET_Is2()
        {
            Assert.That(GraceNote.LEDGER_LINE_OFFSET, Is.EqualTo(2));
        }

        // ── Construction ───────────────────────────────────────────────────────

        /// <summary>
        /// GraceNote constructs without throwing for a simple quarter c/4.
        /// </summary>
        [Test]
        public void GraceNote_Construction_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var _ = new GraceNote(new GraceNoteStruct
                {
                    Duration = "4",
                    Keys = new[] { "c/4" },
                });
            });
        }

        [Test]
        public void GraceNote_CategoryIsV5Category()
        {
            var n = new GraceNote(new GraceNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
            });

            Assert.That(n.GetCategory(), Is.EqualTo(GraceNote.CATEGORY));
            Assert.That(GraceNote.CATEGORY, Is.EqualTo("GraceNote"));
        }

        // ── Scale ──────────────────────────────────────────────────────────────

        /// <summary>
        /// GetStaveNoteScale() returns 0.66 for a GraceNote.
        /// Port of VexFlow gracenote.ts::getStaveNoteScale().
        /// </summary>
        [Test]
        public void GraceNote_GetStaveNoteScale_Returns066()
        {
            var n = new GraceNote(new GraceNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
            });
            Assert.That(n.GetStaveNoteScale(), Is.EqualTo(0.66));
        }

        [Test]
        public void GraceNote_GetStemExtension_AppliesV5ScaleAdjustment()
        {
            var n = new GraceNote(new GraceNoteStruct
            {
                Duration = "8",
                Keys = new[] { "c/4" },
            });

            Assert.That(n.GetStemExtension(), Is.EqualTo(Stem.HEIGHT * GraceNote.SCALE - Stem.HEIGHT).Within(1e-9));
        }

        // ── Width ──────────────────────────────────────────────────────────────

        /// <summary>
        /// GraceNote width is 3 after construction.
        /// Port of VexFlow gracenote.ts constructor: this.width = 3.
        /// </summary>
        [Test]
        public void GraceNote_Width_Is3_AfterConstruction()
        {
            var n = new GraceNote(new GraceNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
            });
            Assert.That(n.GetWidth(), Is.EqualTo(3));
        }

        // ── GraceNoteGroup ─────────────────────────────────────────────────────

        /// <summary>
        /// GraceNoteGroup with 2 grace notes has GetWidth() > 0 after PreFormat().
        /// Port of VexFlow gracenotegroup.ts::preFormat() + getWidth().
        /// </summary>
        [Test]
        public void GraceNoteGroup_WithTwoNotes_GetWidth_IsPositive()
        {
            var g1 = new GraceNote(new GraceNoteStruct { Duration = "8", Keys = new[] { "c/4" } });
            var g2 = new GraceNote(new GraceNoteStruct { Duration = "8", Keys = new[] { "d/4" } });
            var group = new GraceNoteGroup(new List<GraceNote> { g1, g2 });
            group.PreFormat();
            Assert.That(group.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void GraceNoteGroup_CategoryIsV5Category()
        {
            var group = new GraceNoteGroup(new List<GraceNote>());

            Assert.That(group.GetCategory(), Is.EqualTo(GraceNoteGroup.CATEGORY));
            Assert.That(GraceNoteGroup.CATEGORY, Is.EqualTo("GraceNoteGroup"));
        }

        [Test]
        public void GraceNoteGroup_Format_ConsumesLeftShiftAndSetsSpacing()
        {
            var note = new StaveNote(new StaveNoteStruct { Duration = "4", Keys = new[] { "c/4" } });
            var g1 = new GraceNote(new GraceNoteStruct { Duration = "8", Keys = new[] { "c/4" } });
            var g2 = new GraceNote(new GraceNoteStruct { Duration = "8", Keys = new[] { "d/4" } });
            var group = new GraceNoteGroup(new List<GraceNote> { g1, g2 });
            note.AddModifier(group);
            var state = new ModifierContextState();

            GraceNoteGroup.Format(new List<GraceNoteGroup> { group }, state);

            Assert.That(state.LeftShift, Is.EqualTo(group.GetWidth() + 4).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(0).Within(0.0001));
            Assert.That(group.GetSpacingFromNextModifier(), Is.EqualTo(StaveNote.minNoteheadPadding).Within(0.0001));
        }

        [Test]
        public void GraceNoteGroup_BeamNotes_AttachesV5GraceBeamOptions()
        {
            var g1 = new GraceNote(new GraceNoteStruct { Duration = "8", Keys = new[] { "c/4" } });
            var g2 = new GraceNote(new GraceNoteStruct { Duration = "8", Keys = new[] { "d/4" } });
            var group = new GraceNoteGroup(new List<GraceNote> { g1, g2 });

            Assert.That(group.BeamNotes(), Is.SameAs(group));

            Assert.That(g1.HasBeam(), Is.True);
            Assert.That(g2.HasBeam(), Is.True);
            Assert.That(g1.GetBeam()!.RenderOptions.BeamWidth, Is.EqualTo(3));
            Assert.That(g1.GetBeam()!.RenderOptions.PartialBeamLength, Is.EqualTo(4));
        }

        [Test]
        public void BeamedGraceNote_UsesScaledStemBeamExtension()
        {
            var g1 = new GraceNote(new GraceNoteStruct { Duration = "32", Keys = new[] { "c/4" } });
            var g2 = new GraceNote(new GraceNoteStruct { Duration = "32", Keys = new[] { "d/4" } });

            new GraceNoteGroup(new List<GraceNote> { g1, g2 }).BeamNotes();

            double expected = Stem.HEIGHT * GraceNote.SCALE - Stem.HEIGHT + 7.5 * GraceNote.SCALE;
            Assert.That(g1.GetStemExtension(), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void GraceNoteGroup_DrawSlur_UsesStaveTieShape()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new StaveNote(new StaveNoteStruct { Duration = "4", Keys = new[] { "c/4" } });
            var grace = new GraceNote(new GraceNoteStruct { Duration = "8", Keys = new[] { "d/4" } });
            var group = new GraceNoteGroup(new List<GraceNote> { grace }, showSlur: true);
            note.SetStave(stave).SetX(100).AddModifier(group);
            note.PreFormat();
            group.SetContext(ctx);

            group.Draw();

            Assert.That(ctx.GetCalls("QuadraticCurveTo").Count(), Is.EqualTo(2));
            Assert.That(ctx.HasCall("Fill"), Is.True);
        }

        [Test]
        public void GraceNoteGroup_RenderOptions_ApplySlurYShift()
        {
            double DrawAndGetFirstControlY(double slurYShift)
            {
                var ctx = new RecordingRenderContext();
                var stave = new Stave(10, 20, 300);
                stave.SetContext(ctx);
                var note = new StaveNote(new StaveNoteStruct { Duration = "4", Keys = new[] { "c/4" } });
                var grace = new GraceNote(new GraceNoteStruct { Duration = "8", Keys = new[] { "d/4" } });
                var group = new GraceNoteGroup(new List<GraceNote> { grace }, showSlur: true);
                group.RenderOptions.SlurYShift = slurYShift;
                note.SetStave(stave).SetX(100).AddModifier(group);
                note.PreFormat();
                group.SetContext(ctx);

                group.Draw();

                return ctx.GetCalls("QuadraticCurveTo").First().Args[1];
            }

            double defaultControlY = DrawAndGetFirstControlY(0);
            double shiftedControlY = DrawAndGetFirstControlY(4);

            Assert.That(shiftedControlY, Is.Not.EqualTo(defaultControlY));
        }

        // ── Slash flag ─────────────────────────────────────────────────────────

        /// <summary>
        /// GraceNote with Slash=true constructs without error.
        /// </summary>
        [Test]
        public void GraceNote_Slash_Construction_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var _ = new GraceNote(new GraceNoteStruct
                {
                    Duration = "8",
                    Keys = new[] { "c/4" },
                    Slash = true,
                });
            });
        }
    }
}

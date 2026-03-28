// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// GraceNote and GraceNoteGroup unit tests.
// Verifies: SCALE=0.66, LEDGER_LINE_OFFSET=2, 0.66 scale, width=3, GraceNoteGroup.GetWidth() > 0.

using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;

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

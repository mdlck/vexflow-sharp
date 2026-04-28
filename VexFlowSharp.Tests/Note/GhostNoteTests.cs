// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// GhostNote unit tests.
// Verifies: invisible spacer with width=0, IsRest=true, ticks still counted, no-op Draw().

using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("GhostNote")]
    public class GhostNoteTests
    {
        [SetUp]
        public void SetUp()
        {
            Font.ClearRegistry();
            Font.Load("Bravura", BravuraGlyphs.Data);
        }

        // ── Width ──────────────────────────────────────────────────────────────

        /// <summary>
        /// GhostNote has zero visual width — it takes up no space.
        /// </summary>
        [Test]
        public void GhostNote_Width_IsZero()
        {
            var n = new GhostNote("4");
            Assert.That(n.GetWidth(), Is.EqualTo(0));
            Assert.That(n.GetCategory(), Is.EqualTo(GhostNote.CATEGORY));
        }

        // ── Rest ───────────────────────────────────────────────────────────────

        /// <summary>
        /// GhostNote.IsRest() always returns true — it is not a pitched note.
        /// Port of VexFlow ghostnote behavior.
        /// </summary>
        [Test]
        public void GhostNote_IsRest_ReturnsTrue()
        {
            var n = new GhostNote("4");
            Assert.That(n.IsRest(), Is.True);
        }

        // ── Ticks ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Ghost notes still have tick values — they occupy rhythmic time even if invisible.
        /// ignoreTicks must be false.
        /// Port of VexFlow ghostnote behavior.
        /// </summary>
        [Test]
        public void GhostNote_ShouldIgnoreTicks_IsFalse()
        {
            var n = new GhostNote("4");
            Assert.That(n.ShouldIgnoreTicks(), Is.False);
        }

        /// <summary>
        /// Quarter ghost note has the same ticks as a quarter pitch note (RESOLUTION/4).
        /// </summary>
        [Test]
        public void GhostNote_GetTicks_Quarter_Is4096()
        {
            var n = new GhostNote("4");
            Assert.That(n.GetTicks().Numerator, Is.EqualTo(Tables.RESOLUTION / 4));
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw() is a no-op and should not throw when a context is set.
        /// Port of VexFlow ghostnote.draw() behavior.
        /// </summary>
        [Test]
        public void GhostNote_Draw_DoesNotThrow()
        {
            var n = new GhostNote("4");
            using var ctx = new SkiaRenderContext(200, 200);
            n.SetContext(ctx);

            Assert.DoesNotThrow(() => n.Draw());
        }

        // ── NoteStruct constructor ─────────────────────────────────────────────

        /// <summary>
        /// GhostNote can also be constructed from a NoteStruct with the same result.
        /// </summary>
        [Test]
        public void GhostNote_NoteStruct_Constructor_IsZeroWidth()
        {
            var n = new GhostNote(new NoteStruct { Duration = "4" });
            Assert.That(n.GetWidth(), Is.EqualTo(0));
        }
    }
}

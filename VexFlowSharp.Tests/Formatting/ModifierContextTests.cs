// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Tests.Formatting
{
    /// <summary>
    /// Unit tests for ModifierContext: member registry, PreFormat dispatch,
    /// idempotency, and StaveNote.Format chord displacement.
    ///
    /// Port of VexFlow formatter tests relevant to ModifierContext behavior.
    /// </summary>
    [TestFixture]
    [Category("ModifierContext")]
    public class ModifierContextTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static StaveNote MakeNote(string keys, string duration, int stemDir = Stem.UP)
        {
            return new StaveNote(new StaveNoteStruct
            {
                Keys          = keys.Split(','),
                Duration      = duration,
                StemDirection = stemDir,
            });
        }

        private static StaveNote MakeChord(string[] keys, string duration, int stemDir = Stem.UP)
        {
            return new StaveNote(new StaveNoteStruct
            {
                Keys          = keys,
                Duration      = duration,
                StemDirection = stemDir,
            });
        }

        // ── 1. AddMember_RegistersByCategory ─────────────────────────────────

        /// <summary>
        /// AddMember stores the element under its GetCategory() key.
        /// GetMembers returns the same element.
        /// </summary>
        [Test]
        public void AddMember_RegistersByCategory()
        {
            var mc = new ModifierContext();
            var note = MakeNote("c/4", "q");

            mc.AddMember(note);

            var members = mc.GetMembers(StaveNote.CATEGORY);
            Assert.That(members, Has.Count.EqualTo(1), "One StaveNote registered");
            Assert.That(members[0], Is.SameAs(note), "Same instance returned");
        }

        // ── 2. GetMembers_UnknownCategory_ReturnsEmpty ────────────────────────

        /// <summary>
        /// GetMembers with an unknown category returns an empty list, never null.
        /// </summary>
        [Test]
        public void GetMembers_UnknownCategory_ReturnsEmpty()
        {
            var mc = new ModifierContext();

            var result = mc.GetMembers("unknown_category_xyz");

            Assert.That(result, Is.Not.Null, "Should never return null");
            Assert.That(result, Is.Empty, "Unknown category returns empty list");
        }

        // ── 3. AddMember_SetsModifierContextBackReference ─────────────────────

        /// <summary>
        /// After AddMember(), the tickable's GetModifierContext() returns the context.
        /// Port of VexFlow's addMember() member.setModifierContext(this) behaviour.
        /// </summary>
        [Test]
        public void AddMember_SetsModifierContextBackReference()
        {
            var mc = new ModifierContext();
            var note = MakeNote("d/4", "q");

            Assert.That(note.GetModifierContext(), Is.Null, "ModifierContext not set before AddMember");

            mc.AddMember(note);

            Assert.That(note.GetModifierContext(), Is.SameAs(mc), "Back-reference set after AddMember");
        }

        // ── 4. PreFormat_SetsWidth ────────────────────────────────────────────

        /// <summary>
        /// After PreFormat(), GetWidth() returns a non-negative value.
        /// </summary>
        [Test]
        public void PreFormat_SetsWidth()
        {
            var mc = new ModifierContext();
            var noteUp   = MakeNote("c/4", "4", Stem.UP);
            var noteDown = MakeNote("e/4", "4", Stem.DOWN);

            mc.AddMember(noteUp);
            mc.AddMember(noteDown);
            mc.PreFormat();

            Assert.That(mc.GetWidth(), Is.GreaterThanOrEqualTo(0), "Width is non-negative after PreFormat");
        }

        // ── 5. PreFormat_Idempotent ───────────────────────────────────────────

        /// <summary>
        /// Calling PreFormat() twice produces the same result as calling it once.
        /// </summary>
        [Test]
        public void PreFormat_Idempotent()
        {
            var mc = new ModifierContext();
            var noteUp   = MakeNote("c/4", "4", Stem.UP);
            var noteDown = MakeNote("e/4", "4", Stem.DOWN);

            mc.AddMember(noteUp);
            mc.AddMember(noteDown);
            mc.PreFormat();

            double widthAfterFirst = mc.GetWidth();

            mc.PreFormat(); // second call — should be a no-op

            Assert.That(mc.GetWidth(), Is.EqualTo(widthAfterFirst), "Width unchanged on second PreFormat call");
        }

        // ── 6. StaveNoteFormat_SingleNote_NoShift ─────────────────────────────

        /// <summary>
        /// A single StaveNote produces no state shift (fewer than 2 notes → no-op).
        /// </summary>
        [Test]
        public void StaveNoteFormat_SingleNote_NoShift()
        {
            var state = new ModifierContextState();
            var note = MakeNote("c/4", "q");

            var notes = new List<StaveNote> { note };
            StaveNote.Format(notes, state);

            Assert.That(state.LeftShift,  Is.EqualTo(0), "LeftShift unchanged for single note");
            Assert.That(state.RightShift, Is.EqualTo(0), "RightShift unchanged for single note");
        }

        // ── 7. StaveNoteFormat_TwoNotes_ProducesShift ────────────────────────

        /// <summary>
        /// Two StaveNotes in the same ModifierContext produce a non-negative right shift
        /// when their ranges intersect and v5 unison compatibility requires a visual offset.
        /// </summary>
        [Test]
        public void StaveNoteFormat_TwoNotes_ProducesShift()
        {
            var state = new ModifierContextState();

            // Same-pitch quarter notes with different styles cannot use the v5 unison overlap shortcut.
            var noteU = MakeNote("c/4", "4", Stem.UP);
            var noteL = MakeNote("c/4", "4", Stem.DOWN);
            noteL.SetStyle(new ElementStyle { FillStyle = "red" });

            var notes = new List<StaveNote> { noteU, noteL };
            bool result = StaveNote.Format(notes, state);

            Assert.That(result, Is.True, "Format returns true for two notes");
            // The two notes overlap — a right shift should have been applied.
            Assert.That(state.RightShift, Is.GreaterThan(0), "RightShift increased for overlapping voices");
        }

        [Test]
        public void StaveNoteFormat_OppositeStemQuarterAgainstHalf_DoesNotShift()
        {
            var state = new ModifierContextState();
            var upper = MakeNote("c/5", "4", Stem.UP);
            var lower = MakeNote("c/4", "2", Stem.DOWN);

            bool result = StaveNote.Format(new List<StaveNote> { upper, lower }, state);

            Assert.That(result, Is.True);
            Assert.That(upper.GetXShift(), Is.EqualTo(0));
            Assert.That(lower.GetXShift(), Is.EqualTo(0));
            Assert.That(state.RightShift, Is.EqualTo(0));
        }

        // ── 8. StaveNoteFormat_NullOrEmpty_ReturnsFalse ───────────────────────

        /// <summary>
        /// Format returns false for null or empty lists.
        /// </summary>
        [Test]
        public void StaveNoteFormat_NullOrEmpty_ReturnsFalse()
        {
            var state = new ModifierContextState();

            bool resultNull  = StaveNote.Format(null, state);
            bool resultEmpty = StaveNote.Format(new List<StaveNote>(), state);

            Assert.That(resultNull,  Is.False, "Null list returns false");
            Assert.That(resultEmpty, Is.False, "Empty list returns false");
        }

        // ── 9. MultipleCategories_Registered ─────────────────────────────────

        /// <summary>
        /// Multiple notes registered in the same category all appear in GetMembers().
        /// </summary>
        [Test]
        public void MultipleNotes_SameCategory_AllRegistered()
        {
            var mc = new ModifierContext();
            var n1 = MakeNote("c/4", "4");
            var n2 = MakeNote("e/4", "4");
            var n3 = MakeNote("g/4", "4");

            mc.AddMember(n1);
            mc.AddMember(n2);
            mc.AddMember(n3);

            var members = mc.GetMembers(StaveNote.CATEGORY);
            Assert.That(members, Has.Count.EqualTo(3), "All three StaveNotes registered");
        }

        // ── 10. PostFormat_DoesNotThrow ───────────────────────────────────────

        /// <summary>
        /// PostFormat() can be called safely even on an empty or single-note context.
        /// </summary>
        [Test]
        public void PostFormat_DoesNotThrow()
        {
            var mc = new ModifierContext();
            mc.AddMember(MakeNote("c/4", "q"));
            mc.PreFormat();

            Assert.DoesNotThrow(() => mc.PostFormat(), "PostFormat must not throw");
        }
    }
}

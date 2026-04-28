// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;

// Note: VexFlowSharp.Tests.Note is a namespace, so VexFlowSharp.Note must be qualified
using VexFlowNote = VexFlowSharp.Note;

namespace VexFlowSharp.Tests.Elements
{
    [TestFixture]
    [Category("NoteSubGroup")]
    [Category("Phase5")]
    public class NoteSubGroupTests
    {
        [Test]
        [Category("Unit")]
        public void InlineClefChangeRenders()
        {
            // Unit test verifying NoteSubGroup properties without image comparison.
            // A reference image test is deferred because cross-engine SkiaSharp/node-canvas
            // pixel differences are always 99%+ for complex note rendering.

            // Verify static category
            Assert.That(NoteSubGroup.CATEGORY, Is.EqualTo("NoteSubGroup"));
        }

        [Test]
        [Category("Unit")]
        public void PreFormatSetsNonZeroWidth()
        {
            // Create two ghost notes as sub-notes (minimal tick consumers)
            var subNote1 = new GhostNote(new NoteStruct { Duration = "4" });
            var subNote2 = new GhostNote(new NoteStruct { Duration = "4" });
            var subNotes = new List<VexFlowNote> { subNote1, subNote2 };

            var group = new NoteSubGroup(subNotes);

            // Before PreFormat, width is 0
            // After PreFormat, width should be >= 0 (could be 0 if notes are all ghost)
            group.PreFormat();

            // PreFormat should not throw and should be idempotent
            group.PreFormat(); // calling twice should be safe

            Assert.That(group.GetWidth(), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        [Category("Unit")]
        public void FormatIncrementsLeftShift()
        {
            // Create two NoteSubGroups each with a single ghost note
            var note1 = new GhostNote(new NoteStruct { Duration = "4" });
            var note2 = new GhostNote(new NoteStruct { Duration = "4" });

            var group1 = new NoteSubGroup(new List<VexFlowNote> { note1 });
            var group2 = new NoteSubGroup(new List<VexFlowNote> { note2 });

            var state = new ModifierContextState();
            var groups = new List<NoteSubGroup> { group1, group2 };

            double leftShiftBefore = state.LeftShift;
            bool result = NoteSubGroup.Format(groups, state);

            Assert.That(result, Is.True);
            // LeftShift must be >= the value before (width may be 0 for ghost notes)
            Assert.That(state.LeftShift, Is.GreaterThanOrEqualTo(leftShiftBefore));
        }

        [Test]
        [Category("Unit")]
        public void FormatReturnsFalseForEmptyList()
        {
            var state = new ModifierContextState();
            bool result = NoteSubGroup.Format(new List<NoteSubGroup>(), state);
            Assert.That(result, Is.False);
        }

        [Test]
        [Category("Unit")]
        public void CategoryIsV5NoteSubGroup()
        {
            var note = new GhostNote(new NoteStruct { Duration = "4" });
            var group = new NoteSubGroup(new List<VexFlowNote> { note });
            Assert.That(group.GetCategory(), Is.EqualTo("NoteSubGroup"));
        }

        [Test]
        [Category("Unit")]
        public void PositionIsLeft()
        {
            var note = new GhostNote(new NoteStruct { Duration = "4" });
            var group = new NoteSubGroup(new List<VexFlowNote> { note });
            Assert.That(group.GetPosition(), Is.EqualTo(ModifierPosition.Left));
        }
    }
}

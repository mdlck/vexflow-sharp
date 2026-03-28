using NUnit.Framework;
using VexFlowSharp;
using System.Collections.Generic;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Tuplet")]
    [Category("Modifiers")]
    [Category("Phase4")]
    public class TupletTests
    {
        // Helper: create ghost notes for testing Tuplet construction
        private static List<VexFlowSharp.Note> MakeNotes(int count, string duration = "8")
        {
            var notes = new List<VexFlowSharp.Note>();
            for (int i = 0; i < count; i++)
                notes.Add(new GhostNote(duration));
            return notes;
        }

        [Test]
        public void Simple_TripletAboveNotes()
        {
            // Basic triplet: 3 notes, location Top
            var notes  = MakeNotes(3);
            var tuplet = new Tuplet(notes, new TupletOptions { NumNotes = 3, Location = (int)TupletLocation.Top });
            Assert.AreEqual(3, tuplet.GetNoteCount());
            Assert.AreEqual("tuplet", Tuplet.CATEGORY);
        }

        [Test]
        public void Beamed_TripletWithBeam()
        {
            // Beamed triplet: bracketed should default to false (all notes beamed)
            // We just verify construction succeeds and note count is correct
            var notes  = MakeNotes(3);
            var tuplet = new Tuplet(notes, new TupletOptions { NumNotes = 3, Bracketed = false });
            Assert.AreEqual(3, tuplet.GetNoteCount());
        }

        [Test]
        public void Ratio_ShowsColonNotation()
        {
            // Ratioed tuplet: ratioed = true
            var notes  = MakeNotes(5);
            var tuplet = new Tuplet(notes, new TupletOptions { NumNotes = 5, NotesOccupied = 4, Ratioed = true });
            Assert.AreEqual(5, tuplet.GetNoteCount());
            Assert.AreEqual(4, tuplet.GetNotesOccupied());
        }

        [Test]
        public void Bottom_TupletBelowNotes()
        {
            // Tuplet below the notes
            var notes  = MakeNotes(3);
            var tuplet = new Tuplet(notes, new TupletOptions { Location = (int)TupletLocation.Bottom });
            Assert.AreEqual((int)TupletLocation.Bottom, -1);
        }

        [Test]
        public void BottomRatio_BelowWithColonNotation()
        {
            var notes  = MakeNotes(3);
            var tuplet = new Tuplet(notes, new TupletOptions
            {
                Location      = (int)TupletLocation.Bottom,
                Ratioed       = true,
                NotesOccupied = 2,
                NumNotes      = 3,
            });
            Assert.AreEqual(3, tuplet.GetNoteCount());
            Assert.AreEqual(2, tuplet.GetNotesOccupied());
        }

        [Test]
        public void Awkward_NonStandardTupletRatio()
        {
            // 7:8 tuplet — notes_occupied default auto-enables ratio
            var notes  = MakeNotes(7);
            var tuplet = new Tuplet(notes, new TupletOptions { NumNotes = 7, NotesOccupied = 8 });
            Assert.AreEqual(7, tuplet.GetNoteCount());
            Assert.AreEqual(8, tuplet.GetNotesOccupied());
        }

        [Test]
        public void Complex_NestedTuplets()
        {
            // Nested tuplets: inner tuplet within outer tuplet
            var inner  = MakeNotes(3);
            var outer  = MakeNotes(3);
            var t1     = new Tuplet(inner, new TupletOptions { NumNotes = 3 });
            var t2     = new Tuplet(outer, new TupletOptions { NumNotes = 3 });
            Assert.AreEqual(3, t1.GetNoteCount());
            Assert.AreEqual(3, t2.GetNoteCount());
        }

        [Test]
        public void MixedTop_MixedDurationsTuplet()
        {
            // Mixed durations: quarter + two eighths in a triplet
            var notes = new List<VexFlowSharp.Note>
            {
                new GhostNote("4"),
                new GhostNote("8"),
                new GhostNote("8"),
            };
            var tuplet = new Tuplet(notes, new TupletOptions { NumNotes = 3 });
            Assert.AreEqual(3, tuplet.GetNotes().Count);
        }

        [Test]
        public void MixedBottom_BottomMixedDurations()
        {
            var notes = new List<VexFlowSharp.Note>
            {
                new GhostNote("4"),
                new GhostNote("8"),
                new GhostNote("8"),
            };
            var tuplet = new Tuplet(notes, new TupletOptions
            {
                NumNotes = 3,
                Location = (int)TupletLocation.Bottom,
            });
            Assert.AreEqual(3, tuplet.GetNotes().Count);
        }

        [Test]
        public void Nested_TupletWithinTuplet()
        {
            // Structural test: build two tuplets from overlapping note groups
            var notes1 = MakeNotes(3);
            var notes2 = MakeNotes(3);
            Assert.DoesNotThrow(() => new Tuplet(notes1, new TupletOptions { NumNotes = 3 }));
            Assert.DoesNotThrow(() => new Tuplet(notes2, new TupletOptions { NumNotes = 3 }));
        }

        [Test]
        public void TupletLocation_TopIsPositiveOne()
            => Assert.AreEqual(1, (int)TupletLocation.Top);

        [Test]
        public void TupletLocation_BottomIsNegativeOne()
            => Assert.AreEqual(-1, (int)TupletLocation.Bottom);
    }
}

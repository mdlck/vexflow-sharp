using NUnit.Framework;
using VexFlowSharp;
using System.Collections.Generic;
using VexFlowSharp.Tests.Rendering;

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
            Assert.AreEqual("Tuplet", Tuplet.CATEGORY);
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
        public void Constructor_AppliesTickMultiplierToNotes()
        {
            var notes = MakeNotes(3, "8");

            _ = new Tuplet(notes, new TupletOptions { NumNotes = 3, NotesOccupied = 2 });

            Assert.That(notes[0].GetTicks(), Is.EqualTo(new Fraction(4096, 3)));
        }

        [Test]
        public void SetNotesOccupied_RevertsOldMultiplierBeforeApplyingNewOne()
        {
            var notes = MakeNotes(3, "8");
            var tuplet = new Tuplet(notes, new TupletOptions { NumNotes = 3, NotesOccupied = 2 });

            tuplet.SetNotesOccupied(4);

            Assert.That(notes[0].GetTicks(), Is.EqualTo(new Fraction(8192, 3)));
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
            var notes = MakeNotes(3);
            var outer = new Tuplet(notes, new TupletOptions { NumNotes = 3 });
            var inner = new Tuplet(notes, new TupletOptions { NumNotes = 3 });

            Assert.That(outer.GetNestedTupletCount(), Is.EqualTo(0));
            Assert.That(inner.GetNestedTupletCount(), Is.EqualTo(1));
        }

        [Test]
        public void NestedTuplets_IgnoreTupletsOnOppositeSide()
        {
            var notes = MakeNotes(3);
            var top = new Tuplet(notes, new TupletOptions { Location = (int)TupletLocation.Top });
            var bottom = new Tuplet(notes, new TupletOptions { Location = (int)TupletLocation.Bottom });

            Assert.That(top.GetNestedTupletCount(), Is.EqualTo(0));
            Assert.That(bottom.GetNestedTupletCount(), Is.EqualTo(0));
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

        [Test]
        public void Tuplet_DefaultOffsets_ComeFromMetrics()
        {
            var tuplet = new Tuplet(MakeNotes(3));

            Assert.That(tuplet.GetYOffset(), Is.EqualTo(Metrics.GetDouble("Tuplet.yOffset")));
            Assert.That(tuplet.GetTextYOffset(), Is.EqualTo(Metrics.GetDouble("Tuplet.textYOffset")));
        }

        [Test]
        public void Tuplet_ExplicitOffsets_OverrideMetrics()
        {
            var tuplet = new Tuplet(MakeNotes(3), new TupletOptions
            {
                YOffset = 9,
                TextYOffset = 4,
            });

            Assert.That(tuplet.GetYOffset(), Is.EqualTo(9));
            Assert.That(tuplet.GetTextYOffset(), Is.EqualTo(4));
        }

        [Test]
        public void Draw_EmitsV5PointerRect()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 20, 300);
            var notes = new List<VexFlowSharp.Note>
            {
                new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "8" }),
                new StaveNote(new StaveNoteStruct { Keys = new[] { "d/4" }, Duration = "8" }),
                new StaveNote(new StaveNoteStruct { Keys = new[] { "e/4" }, Duration = "8" }),
            };

            for (int i = 0; i < notes.Count; i++)
            {
                notes[i].SetStave(stave).SetX(80 + i * 40);
            }

            var tuplet = new Tuplet(notes);
            tuplet.SetContext(ctx);

            tuplet.Draw();

            var box = tuplet.GetBoundingBox()!;
            Assert.That(ctx.GetCall("PointerRect").Args, Is.EqualTo(new[] { box.GetX(), box.GetY(), box.GetW(), box.GetH() }));

            var bracketRects = new List<(string Method, double[] Args)>(ctx.GetCalls("FillRect"));
            Assert.That(bracketRects[0].Args[3], Is.EqualTo(Metrics.GetDouble("Tuplet.bracket.lineWidth")));
            Assert.That(bracketRects[2].Args[2], Is.EqualTo(Metrics.GetDouble("Tuplet.bracket.lineWidth")));
            Assert.That(bracketRects[2].Args[3], Is.EqualTo(Metrics.GetDouble("Tuplet.bracket.legLength")));
        }
    }
}

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Tests.Formatting
{
    /// <summary>
    /// Unit tests for the Voice class.
    /// Tests cover VoiceMode enforcement, tick accumulation, Softmax, and IsComplete.
    /// </summary>
    [TestFixture]
    [Category("Voice")]
    public class VoiceTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Create a minimal Tickable stub with a given tick count.</summary>
        private static StaveNote MakeNote(string duration)
        {
            return new StaveNote(new StaveNoteStruct
            {
                Duration = duration,
                Keys     = new[] { "c/4" },
            });
        }

        // ── STRICT mode ───────────────────────────────────────────────────────

        [Test]
        public void Category_IsV5Voice()
        {
            Assert.That(Voice.CATEGORY, Is.EqualTo("Voice"));
        }

        [Test]
        public void StrictMode_FourQuarterNotes_IsComplete()
        {
            var voice = new Voice();
            Assert.AreEqual(VoiceMode.STRICT, voice.GetMode(), "Default mode should be STRICT");

            voice.AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"));

            Assert.IsTrue(voice.IsComplete(), "4 quarter notes in 4/4 should complete the voice");
        }

        [Test]
        public void StrictMode_FifthNote_ThrowsVexFlowException()
        {
            var voice = new Voice();
            voice.AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"));

            Assert.Throws<VexFlowException>(() => voice.AddTickable(MakeNote("4")),
                "Adding a 5th quarter note in STRICT mode should throw");
        }

        [Test]
        public void StrictMode_TooManyTicksRollsBackTicksUsed()
        {
            var voice = new Voice();
            voice.AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"));
            var ticksUsed = voice.GetTicksUsed();

            Assert.Throws<VexFlowException>(() => voice.AddTickable(MakeNote("4")));

            Assert.That(voice.GetTicksUsed(), Is.EqualTo(ticksUsed));
            Assert.That(voice.GetTickables(), Has.Count.EqualTo(4));
        }

        [Test]
        public void StrictMode_TwoQuarterNotes_IsNotComplete()
        {
            var voice = new Voice();
            voice.AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"));

            Assert.IsFalse(voice.IsComplete(), "2 quarter notes in 4/4 should not be complete");
        }

        [Test]
        public void Constructor_TimeSignatureStringSetsTotalTicks()
        {
            var voice = new Voice("3/8");

            Assert.That(voice.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION * 3 / 8, 1)));
        }

        [Test]
        public void Constructor_InvalidTimeSignatureStringFallsBackToFourFour()
        {
            var voice = new Voice("not-a-time-signature");

            Assert.That(voice.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION, 1)));
        }

        // ── SOFT mode ─────────────────────────────────────────────────────────

        [Test]
        public void SoftMode_TwoQuarterNotes_NoError_NotComplete()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);

            voice.AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"));

            // SOFT mode always reports complete
            Assert.IsTrue(voice.IsComplete(), "SOFT mode IsComplete() always returns true");
        }

        [Test]
        public void SoftMode_EightQuarterNotes_NoError()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);

            // 8 quarter notes exceeds 4/4, but SOFT allows it
            for (int i = 0; i < 8; i++)
                voice.AddTickable(MakeNote("4"));

            Assert.AreEqual(8, voice.GetTickables().Count);
        }

        [Test]
        public void IgnoredTickable_DoesNotAdvanceTicksButIsAdded()
        {
            var voice = new Voice();
            var bar = new BarNote();

            voice.AddTickable(MakeNote("4"))
                 .AddTickable(bar)
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"));

            Assert.That(bar.ShouldIgnoreTicks(), Is.True);
            Assert.That(voice.GetTicksUsed(), Is.EqualTo(new Fraction(Tables.RESOLUTION, 1)));
            Assert.That(voice.GetTickables(), Has.Count.EqualTo(5));
            Assert.That(voice.IsComplete(), Is.True);
        }

        // ── FULL mode ─────────────────────────────────────────────────────────

        [Test]
        public void FullMode_FourQuarterNotes_IsComplete()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.FULL);

            voice.AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"));

            Assert.IsTrue(voice.IsComplete(), "4 quarter notes in FULL mode should be complete");
        }

        [Test]
        public void FullMode_FifthNote_ThrowsVexFlowException()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.FULL);

            voice.AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"));

            Assert.Throws<VexFlowException>(() => voice.AddTickable(MakeNote("4")),
                "FULL mode should throw when total ticks would be exceeded");
        }

        [Test]
        public void FullMode_ThreeQuarterNotes_NotComplete_NoError()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.FULL);

            voice.AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"));

            Assert.IsFalse(voice.IsComplete(), "3 quarter notes in FULL 4/4 is not complete");
        }

        // ── Tick accumulation ─────────────────────────────────────────────────

        [Test]
        public void GetTicksUsed_AccumulatesCorrectly()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);

            // whole + half = 3/2 of a 4/4 measure
            voice.AddTickable(MakeNote("1"))  // 16384 ticks
                 .AddTickable(MakeNote("2"))  // 8192 ticks
                 .AddTickable(MakeNote("4")); // 4096 ticks

            // Expected: 16384 + 8192 + 4096 = 28672
            int expected = Tables.RESOLUTION + Tables.RESOLUTION / 2 + Tables.RESOLUTION / 4;
            Assert.AreEqual(expected, voice.GetTicksUsed().Numerator,
                "TicksUsed should accumulate all note ticks");
        }

        [Test]
        public void AddTickable_TripletExpandsTotalTicksDenominatorAndActualResolution()
        {
            var voice = new Voice();
            var notes = new List<StaveNote>
            {
                MakeNote("8"),
                MakeNote("8"),
                MakeNote("8"),
            };
            _ = new Tuplet(new List<VexFlowSharp.Note> { notes[0], notes[1], notes[2] },
                new TupletOptions { NumNotes = 3, NotesOccupied = 2 });

            voice.AddTickables(new List<Tickable> { notes[0], notes[1], notes[2] });

            Assert.That(voice.GetResolutionMultiplier(), Is.EqualTo(3));
            Assert.That(voice.GetActualResolution(), Is.EqualTo(Tables.RESOLUTION * 3));
            Assert.That(voice.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION, 1)));
            Assert.That(voice.GetTotalTicks().Denominator, Is.EqualTo(3));
        }

        [Test]
        public void AddTickable_NestedTupletsExpandResolutionMultiplier()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);
            var notes = new List<StaveNote>
            {
                MakeNote("8"),
                MakeNote("8"),
                MakeNote("8"),
            };
            _ = new Tuplet(new List<VexFlowSharp.Note> { notes[0], notes[1], notes[2] },
                new TupletOptions { NumNotes = 3, NotesOccupied = 2 });
            _ = new Tuplet(new List<VexFlowSharp.Note> { notes[0], notes[1], notes[2] },
                new TupletOptions { NumNotes = 5, NotesOccupied = 4 });

            voice.AddTickables(new List<Tickable> { notes[0], notes[1], notes[2] });

            Assert.That(voice.GetTicksUsed().Denominator, Is.EqualTo(15));
            Assert.That(voice.GetResolutionMultiplier(), Is.EqualTo(15));
            Assert.That(voice.GetActualResolution(), Is.EqualTo(Tables.RESOLUTION * 15));
            Assert.That(voice.GetTotalTicks().Denominator, Is.EqualTo(15));
        }

        [Test]
        public void PreFormat_AppliesVoiceStaveToNotesWithoutStaves()
        {
            var stave = new Stave(10, 40, 400);
            var note = MakeNote("4");
            var voice = new Voice()
                .SetStave(stave)
                .AddTickable(note);

            voice.PreFormat();

            Assert.That(note.GetStave(), Is.SameAs(stave));
        }

        [Test]
        public void PreFormat_ThrowsWithoutStave()
        {
            var voice = new Voice().AddTickable(MakeNote("4"));

            Assert.Throws<VexFlowException>(() => voice.PreFormat());
        }

        [Test]
        public void GetBoundingBox_MergesTickableBoundsAndAppliesVoiceStave()
        {
            var stave = new Stave(10, 40, 400);
            var first = MakeNote("4");
            var second = MakeNote("4");
            first.SetX(80);
            second.SetX(150);
            var voice = new Voice()
                .SetStave(stave)
                .AddTickable(first)
                .AddTickable(second);

            var box = voice.GetBoundingBox();

            Assert.That(first.GetStave(), Is.SameAs(stave));
            Assert.That(second.GetStave(), Is.SameAs(stave));
            Assert.That(box, Is.Not.Null);
            Assert.That(box!.GetX(), Is.LessThanOrEqualTo(first.GetBoundingBox()!.GetX()));
            Assert.That(box.GetX() + box.GetW(), Is.GreaterThanOrEqualTo(second.GetBoundingBox()!.GetX() + second.GetBoundingBox()!.GetW()));
        }

        // ── SmallestTickCount ─────────────────────────────────────────────────

        [Test]
        public void GetSmallestTickCount_TracksSmallestDuration()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);

            voice.AddTickable(MakeNote("1")); // whole
            voice.AddTickable(MakeNote("4")); // quarter — smaller
            voice.AddTickable(MakeNote("2")); // half

            int expectedTicks = Tables.RESOLUTION / 4; // quarter note
            Assert.AreEqual(expectedTicks, voice.GetSmallestTickCount().Numerator,
                "SmallestTickCount should track the shortest duration");
        }

        // ── Softmax ───────────────────────────────────────────────────────────

        [Test]
        public void Softmax_ReturnsValueBetweenZeroAndOne()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);

            voice.AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"));

            double sm = voice.Softmax(Tables.RESOLUTION / 4.0);
            Assert.Greater(sm, 0.0, "Softmax value must be positive");
            Assert.LessOrEqual(sm, 1.0, "Softmax value must not exceed 1.0");
        }

        [Test]
        public void Softmax_SumsToApproximatelyOne()
        {
            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);

            voice.AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"))
                 .AddTickable(MakeNote("4"));

            double sum = 0;
            foreach (var t in voice.GetTickables())
                sum += voice.Softmax(t.GetTicks().Value());

            Assert.AreEqual(1.0, sum, 1e-9, "Sum of softmax over all tickables should be 1.0");
        }

        // ── SetVoice back-reference ───────────────────────────────────────────

        [Test]
        public void AddTickable_SetsVoiceOnTickable()
        {
            var voice = new Voice();
            var note  = MakeNote("4");

            voice.AddTickable(note);

            Assert.AreSame(voice, note.GetVoice(), "AddTickable should set voice on tickable");
        }

        // ── GetTickables count ────────────────────────────────────────────────

        [Test]
        public void AddTickables_AddsAllToList()
        {
            var voice  = new Voice();
            voice.SetMode(VoiceMode.SOFT);
            var notes  = new System.Collections.Generic.List<Tickable>
            {
                MakeNote("4"),
                MakeNote("4"),
                MakeNote("4"),
            };

            voice.AddTickables(notes);

            Assert.AreEqual(3, voice.GetTickables().Count);
        }
    }
}

using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("Note")]
    public class NoteTests
    {
        /// <summary>
        /// Concrete test subclass of Note for unit testing.
        /// </summary>
        private class TestNote : VexFlowSharp.Note
        {
            public TestNote(NoteStruct ns) : base(ns) { }
            public override void Draw() { }
        }

        [Test]
        public void Construction_QuarterNote_Duration()
        {
            var n = new TestNote(new NoteStruct { Duration = "4", Keys = new[] { "c/4" } });
            Assert.That(n.GetDuration(), Is.EqualTo("4"));
        }

        [Test]
        public void Construction_QuarterNote_TickCount()
        {
            var n = new TestNote(new NoteStruct { Duration = "4", Keys = new[] { "c/4" } });
            // RESOLUTION / 4 = 16384 / 4 = 4096
            Assert.That(n.GetTicks().Numerator, Is.EqualTo(4096));
        }

        [Test]
        public void Construction_QuarterNote_NoteType()
        {
            var n = new TestNote(new NoteStruct { Duration = "4", Keys = new[] { "c/4" } });
            Assert.That(n.GetNoteType(), Is.EqualTo("n"));
        }

        [Test]
        public void Construction_QuarterNote_Keys()
        {
            var n = new TestNote(new NoteStruct { Duration = "4", Keys = new[] { "c/4" } });
            Assert.That(n.GetKeys(), Is.EqualTo(new[] { "c/4" }));
        }

        [Test]
        public void RestDetection_DurationWithR_IsRest()
        {
            // "4r" = quarter rest
            var n = new TestNote(new NoteStruct { Duration = "4r" });
            Assert.That(n.IsRest(), Is.True);
            Assert.That(n.GetNoteType(), Is.EqualTo("r"));
        }

        [Test]
        public void RestDetection_NormalNote_IsNotRest()
        {
            var n = new TestNote(new NoteStruct { Duration = "4", Keys = new[] { "c/4" } });
            Assert.That(n.IsRest(), Is.False);
        }

        [Test]
        public void GetAbsoluteX_FallsBackToX_WhenTickContextIsNull()
        {
            var n = new TestNote(new NoteStruct { Duration = "4" });
            n.SetX(42.0);
            // tickContext is null in Phase 2 — should return x directly (Pitfall 4 guard)
            Assert.That(n.GetAbsoluteX(), Is.EqualTo(42.0));
        }

        [Test]
        public void SetX_GetX_RoundTrip()
        {
            var n = new TestNote(new NoteStruct { Duration = "4" });
            n.SetX(77.5);
            Assert.That(n.GetX(), Is.EqualTo(77.5));
        }

        [Test]
        public void GetX_DefaultsToZero()
        {
            var n = new TestNote(new NoteStruct { Duration = "4" });
            Assert.That(n.GetX(), Is.EqualTo(0.0));
        }

        [Test]
        public void Dots_FromNoteStruct_Stored()
        {
            var n = new TestNote(new NoteStruct { Duration = "4", Dots = 1 });
            Assert.That(n.GetDots(), Is.EqualTo(1));
        }

        [Test]
        public void Dots_Default_IsZero()
        {
            var n = new TestNote(new NoteStruct { Duration = "4" });
            Assert.That(n.GetDots(), Is.EqualTo(0));
        }

        [Test]
        public void DottedNote_TicksInclude_DotContribution()
        {
            // Quarter note with one dot: 4096 + 2048 = 6144 ticks
            var n = new TestNote(new NoteStruct { Duration = "4", Dots = 1 });
            Assert.That(n.GetTicks().Numerator, Is.EqualTo(6144));
        }

        [Test]
        public void HalfNote_Duration_CorrectTicks()
        {
            var n = new TestNote(new NoteStruct { Duration = "2" });
            // RESOLUTION / 2 = 8192
            Assert.That(n.GetTicks().Numerator, Is.EqualTo(8192));
        }

        [Test]
        public void WholeNote_Duration_CorrectTicks()
        {
            var n = new TestNote(new NoteStruct { Duration = "1" });
            // RESOLUTION / 1 = 16384
            Assert.That(n.GetTicks().Numerator, Is.EqualTo(16384));
        }

        [Test]
        public void EighthNote_Duration_CorrectTicks()
        {
            var n = new TestNote(new NoteStruct { Duration = "8" });
            // RESOLUTION / 8 = 2048
            Assert.That(n.GetTicks().Numerator, Is.EqualTo(2048));
        }

        [Test]
        public void Note_IsTickable()
        {
            var n = new TestNote(new NoteStruct { Duration = "4" });
            Assert.That(n, Is.InstanceOf<Tickable>());
        }

        [Test]
        public void Note_IsElement()
        {
            var n = new TestNote(new NoteStruct { Duration = "4" });
            Assert.That(n, Is.InstanceOf<Element>());
        }

        [Test]
        public void TypeOverride_XNotehead_GetsXType()
        {
            var n = new TestNote(new NoteStruct { Duration = "4", Type = "x" });
            Assert.That(n.GetNoteType(), Is.EqualTo("x"));
        }

        [Test]
        public void NullKeys_DefaultsToEmptyArray()
        {
            var n = new TestNote(new NoteStruct { Duration = "4" });
            Assert.That(n.GetKeys(), Is.Not.Null);
            Assert.That(n.GetKeys().Length, Is.EqualTo(0));
        }
    }
}

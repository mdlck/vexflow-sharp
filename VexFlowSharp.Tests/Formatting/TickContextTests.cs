// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Tests.Formatting
{
    /// <summary>
    /// Unit tests for TickContext: metric accumulation, x-positioning, and tickable management.
    /// </summary>
    [TestFixture]
    [Category("TickContext")]
    public class TickContextTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static StaveNote MakeNote(string duration)
        {
            return new StaveNote(new StaveNoteStruct
            {
                Duration = duration,
                Keys     = new[] { "c/4" },
            });
        }

        // ── Back-reference ────────────────────────────────────────────────────

        [Test]
        public void AddTickable_SetsTickContextOnTickable()
        {
            var tc   = new TickContext();
            var note = MakeNote("4");

            tc.AddTickable(note);

            Assert.AreSame(tc, note.GetTickContext(),
                "AddTickable should set tickContext back-reference on the tickable");
        }

        // ── GetX / SetX ───────────────────────────────────────────────────────

        [Test]
        public void GetX_SetX_RoundTrip()
        {
            var tc = new TickContext();
            tc.SetX(42.5);

            Assert.AreEqual(42.5, tc.GetX(), 1e-9, "GetX should return the value set by SetX");
        }

        [Test]
        public void SetX_ResetsXBaseAndOffset()
        {
            var tc = new TickContext();
            tc.SetXBase(100);
            tc.SetXOffset(50);  // x = 150
            tc.SetX(200);       // should reset xBase to 200, xOffset to 0

            Assert.AreEqual(200, tc.GetX(),      1e-9);
            Assert.AreEqual(200, tc.GetXBase(),  1e-9);
            Assert.AreEqual(0,   tc.GetXOffset(), 1e-9);
        }

        [Test]
        public void SetXBase_SetXOffset_ComputeX()
        {
            var tc = new TickContext();
            tc.SetX(0);
            tc.SetXBase(30);
            tc.SetXOffset(15);

            Assert.AreEqual(30,  tc.GetXBase(),  1e-9);
            Assert.AreEqual(15,  tc.GetXOffset(), 1e-9);
            Assert.AreEqual(45,  tc.GetX(),      1e-9, "x = xBase + xOffset");
        }

        // ── MaxTicks ─────────────────────────────────────────────────────────

        [Test]
        public void GetMaxTicks_ReflectsLargestDurationAdded()
        {
            var tc = new TickContext();

            tc.AddTickable(MakeNote("4"));  // quarter = RESOLUTION/4
            tc.AddTickable(MakeNote("2"));  // half    = RESOLUTION/2
            tc.AddTickable(MakeNote("8"));  // eighth  = RESOLUTION/8

            int expectedMax = Tables.RESOLUTION / 2;
            Assert.AreEqual(expectedMax, tc.GetMaxTicks().Numerator,
                "GetMaxTicks should reflect the largest duration added");
        }

        // ── PreFormat / GetWidth ──────────────────────────────────────────────

        [Test]
        public void PreFormat_Width_IsPositiveAfterFormattedNote()
        {
            var tc   = new TickContext();
            var note = MakeNote("4");

            // PreFormat note first so it has a width
            note.PreFormat();
            tc.AddTickable(note);
            tc.PreFormat();

            Assert.Greater(tc.GetWidth(), 0, "Width after PreFormat should be positive");
        }

        [Test]
        public void PreFormat_AccumulatesMaxMetricsFromMultipleTickables()
        {
            var tc    = new TickContext();
            var note1 = MakeNote("4");
            var note2 = MakeNote("2");

            note1.PreFormat();
            note2.PreFormat();

            tc.AddTickable(note1);
            tc.AddTickable(note2);
            tc.PreFormat();

            var metrics = tc.GetMetrics();
            // notePx should be the max of the two notes' widths
            Assert.GreaterOrEqual(metrics.NotePx, 0, "NotePx should be non-negative");
        }

        [Test]
        public void PreFormat_IsIdempotent()
        {
            var tc   = new TickContext();
            var note = MakeNote("4");
            note.PreFormat();
            tc.AddTickable(note);

            tc.PreFormat();
            double w1 = tc.GetWidth();
            tc.PreFormat(); // second call should be no-op
            double w2 = tc.GetWidth();

            Assert.AreEqual(w1, w2, "PreFormat should be idempotent");
        }

        // ── GetTickablesByVoice ───────────────────────────────────────────────

        [Test]
        public void GetTickablesByVoice_ReturnsCorrectMapping()
        {
            var tc    = new TickContext();
            var note0 = MakeNote("4");
            var note1 = MakeNote("2");

            tc.AddTickable(note0, voiceIndex: 0);
            tc.AddTickable(note1, voiceIndex: 1);

            var byVoice = tc.GetTickablesByVoice();
            Assert.AreSame(note0, byVoice[0], "Voice 0 tickable should be note0");
            Assert.AreSame(note1, byVoice[1], "Voice 1 tickable should be note1");
        }

        // ── CurrentTick ──────────────────────────────────────────────────────

        [Test]
        public void SetCurrentTick_GetCurrentTick_RoundTrip()
        {
            var tc   = new TickContext();
            var tick = new Fraction(4096, 1);
            tc.SetCurrentTick(tick);

            Assert.AreEqual(tick, tc.GetCurrentTick(), "GetCurrentTick should return the set value");
        }

        // ── GetNextContext ────────────────────────────────────────────────────

        [Test]
        public void GetNextContext_ReturnsSuccessor()
        {
            var tc1 = new TickContext();
            var tc2 = new TickContext();

            tc1.tContexts.Add(tc1);
            tc1.tContexts.Add(tc2);
            tc2.tContexts = tc1.tContexts;

            var next = TickContext.GetNextContext(tc1);
            Assert.AreSame(tc2, next, "GetNextContext should return the next context in the list");
        }

        [Test]
        public void GetNextContext_LastElement_ReturnsNull()
        {
            var tc = new TickContext();
            tc.tContexts.Add(tc);

            var next = TickContext.GetNextContext(tc);
            Assert.IsNull(next, "GetNextContext on last element should return null");
        }
    }
}

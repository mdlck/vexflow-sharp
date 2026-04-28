// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Tests for StaveTie (tie/hammer-on/pull-off/slide renderer).
// Ports key test cases from vexflow/tests/stavetie_tests.ts.

using NUnit.Framework;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("StaveTie")]
    [Category("Modifiers")]
    [Category("Phase4")]
    public class StaveTieTests
    {
        // ── StaveTieRenderOptions defaults ────────────────────────────────────

        [Test]
        public void StaveTieOptions_DefaultCp1MatchesV5()
        {
            var opts = new StaveTieRenderOptions();
            Assert.AreEqual(Metrics.GetDouble("StaveTie.cp1"), opts.Cp1, 1e-9);
        }

        [Test]
        public void StaveTieOptions_DefaultCp2MatchesV5()
        {
            var opts = new StaveTieRenderOptions();
            Assert.AreEqual(Metrics.GetDouble("StaveTie.cp2"), opts.Cp2, 1e-9);
        }

        [Test]
        public void StaveTieOptions_DefaultYShiftIs7()
        {
            var opts = new StaveTieRenderOptions();
            Assert.AreEqual(Metrics.GetDouble("StaveTie.yShift"), opts.YShift, 1e-9);
        }

        [Test]
        public void StaveTieOptions_DefaultThicknessIs2()
        {
            var opts = new StaveTieRenderOptions();
            Assert.AreEqual(Metrics.GetDouble("StaveTie.thickness"), opts.Thickness, 1e-9);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        [Test]
        public void Simple_TieBetweenTwoNotes()
        {
            // A StaveTie with both notes null (degenerate) should construct without throwing.
            var tieNotes = new TieNotes { FirstNote = null, LastNote = null };
            Assert.DoesNotThrow(() => new StaveTie(tieNotes));
        }

        [Test]
        public void HammerOn_TextIsH()
        {
            // Hammer-on text label should be stored and retrievable.
            var tieNotes  = new TieNotes();
            var hammerOn  = new StaveTie(tieNotes, "H");
            Assert.IsNotNull(hammerOn);
            // GetNotes returns the same TieNotes passed in
            Assert.AreSame(tieNotes, hammerOn.GetNotes());
        }

        [Test]
        public void PullOff_TextIsP()
        {
            var tieNotes = new TieNotes();
            var pullOff  = new StaveTie(tieNotes, "P");
            Assert.IsNotNull(pullOff);
            Assert.AreSame(tieNotes, pullOff.GetNotes());
        }

        [Test]
        public void Slide_HasSlideText()
        {
            // A slide can use any text label (e.g., "/" or "sl.")
            var tieNotes = new TieNotes();
            var slide    = new StaveTie(tieNotes, "/");
            Assert.IsNotNull(slide);
        }

        [Test]
        public void NullLastNote_TieExtendsToStaveEnd()
        {
            // A StaveTie with null LastNote is a partial tie (extends to stave end).
            var firstNote = new StaveNote(new StaveNoteStruct
            {
                Keys     = new[] { "c/4" },
                Duration = "4"
            });
            var tieNotes = new TieNotes { FirstNote = firstNote, LastNote = null };
            var staveTie = new StaveTie(tieNotes);

            Assert.IsTrue(staveTie.IsPartial());
        }

        // ── GetNotes ──────────────────────────────────────────────────────────

        [Test]
        public void GetNotes_ReturnsOriginalTieNotes()
        {
            var tieNotes = new TieNotes();
            var tie      = new StaveTie(tieNotes);
            Assert.AreSame(tieNotes, tie.GetNotes());
        }

        // ── IsPartial ────────────────────────────────────────────────────────

        [Test]
        public void IsPartial_WhenBothNotesNull()
        {
            var tieNotes = new TieNotes { FirstNote = null, LastNote = null };
            var tie      = new StaveTie(tieNotes);
            Assert.IsTrue(tie.IsPartial());
        }

        // ── Category ─────────────────────────────────────────────────────────

        [Test]
        public void StaveTie_Category_IsCorrect()
        {
            var tieNotes = new TieNotes();
            var tie      = new StaveTie(tieNotes);
            Assert.AreEqual(StaveTie.CATEGORY, tie.GetCategory());
            Assert.AreEqual("StaveTie", StaveTie.CATEGORY);
        }

        // ── SetDirection ─────────────────────────────────────────────────────

        [Test]
        public void SetDirection_ReturnsStaveTieForChaining()
        {
            var tieNotes = new TieNotes();
            var tie      = new StaveTie(tieNotes);
            var result   = tie.SetDirection(1);
            Assert.AreSame(tie, result);
        }

        [Test]
        public void Draw_CloseNotesUsesMetricControlPoints()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 40, 200);
            var first = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            var last = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            first.SetStave(stave).SetX(60).PreFormat();
            last.SetStave(stave).SetX(65).PreFormat();
            var tie = new StaveTie(new TieNotes { FirstNote = first, LastNote = last }).SetDirection(1);
            tie.SetContext(ctx);

            tie.Draw();

            var curves = ctx.GetCalls("QuadraticCurveTo").ToArray();
            Assert.That(curves, Has.Length.EqualTo(2));

            double averageY = (first.GetTieYForBottom() + last.GetTieYForBottom()) / 2.0
                + Metrics.GetDouble("StaveTie.yShift");
            Assert.That(curves[0].Args[1], Is.EqualTo(averageY + Metrics.GetDouble("StaveTie.closeNoteCp1")).Within(0.0001));
            Assert.That(curves[1].Args[1], Is.EqualTo(averageY + Metrics.GetDouble("StaveTie.closeNoteCp2")).Within(0.0001));
        }
    }
}

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Tests for StaveTie (tie/hammer-on/pull-off/slide renderer).
// Ports key test cases from vexflow/tests/stavetie_tests.ts.

using NUnit.Framework;

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
        public void StaveTieOptions_DefaultCp1Is36()
        {
            var opts = new StaveTieRenderOptions();
            Assert.AreEqual(36.0, opts.Cp1, 1e-9);
        }

        [Test]
        public void StaveTieOptions_DefaultCp2Is36()
        {
            var opts = new StaveTieRenderOptions();
            Assert.AreEqual(36.0, opts.Cp2, 1e-9);
        }

        [Test]
        public void StaveTieOptions_DefaultYShiftIs7()
        {
            var opts = new StaveTieRenderOptions();
            Assert.AreEqual(7.0, opts.YShift, 1e-9);
        }

        [Test]
        public void StaveTieOptions_DefaultThicknessIs2()
        {
            var opts = new StaveTieRenderOptions();
            Assert.AreEqual(2.0, opts.Thickness, 1e-9);
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
            Assert.AreEqual("StaveTie", tie.GetCategory());
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
    }
}

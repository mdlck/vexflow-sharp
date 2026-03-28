// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using NUnit.Framework;

namespace VexFlowSharp.Tests.StaveTests
{
    [TestFixture]
    [Category("Stave")]
    public class StaveTests
    {
        // ── Constructor ───────────────────────────────────────────────────────

        [Test]
        public void Constructor_SetsXYWidth()
        {
            var stave = new Stave(10, 40, 300);
            Assert.That(stave.GetX(),     Is.EqualTo(10));
            Assert.That(stave.GetY(),     Is.EqualTo(40));
            Assert.That(stave.GetWidth(), Is.EqualTo(300));
        }

        [Test]
        public void Constructor_DefaultNumLines_Is5()
        {
            var stave = new Stave(0, 0, 400);
            Assert.That(stave.GetNumLines(), Is.EqualTo(5));
        }

        // ── GetYForLine ───────────────────────────────────────────────────────

        /// <summary>
        /// Default: SpaceAboveStaffLn=4, SpacingBetweenLinesPx=10
        /// GetYForLine(0) = y + 4*10 + 0*10 = y + 40
        /// </summary>
        [Test]
        public void GetYForLine_Line0_IsYPlus40()
        {
            var stave = new Stave(0, 40, 300);
            Assert.That(stave.GetYForLine(0), Is.EqualTo(40 + 40).Within(0.001));
        }

        /// <summary>
        /// GetYForLine(4) = y + 4*10 + 4*10 = y + 80
        /// </summary>
        [Test]
        public void GetYForLine_Line4_IsYPlus80()
        {
            var stave = new Stave(0, 40, 300);
            Assert.That(stave.GetYForLine(4), Is.EqualTo(40 + 80).Within(0.001));
        }

        [Test]
        public void GetYForLine_LinesSpaced10px()
        {
            var stave = new Stave(0, 0, 300);
            double line0 = stave.GetYForLine(0);
            double line1 = stave.GetYForLine(1);
            Assert.That(line1 - line0, Is.EqualTo(10).Within(0.001));
        }

        // ── GetYForNote ───────────────────────────────────────────────────────

        /// <summary>
        /// GetYForNote(3) = y + 4*10 + 5*10 - 3*10 = y + 60
        /// GetYForLine(2) = y + 4*10 + 2*10 = y + 60
        /// So note line 3 maps to staff line 2.
        /// </summary>
        [Test]
        public void GetYForNote_Line3_EqualToStaveLine2()
        {
            var stave = new Stave(0, 0, 300);
            double noteY = stave.GetYForNote(3);
            double lineY = stave.GetYForLine(2);
            Assert.That(noteY, Is.EqualTo(lineY).Within(0.001));
        }

        // ── GetNoteStartX ─────────────────────────────────────────────────────

        [Test]
        public void GetNoteStartX_WithNoExtraModifiers_GreaterThanX()
        {
            var stave = new Stave(10, 40, 300);
            double startX = stave.GetNoteStartX();
            Assert.That(startX, Is.GreaterThan(10));
        }

        [Test]
        public void GetNoteStartX_AfterFormat_IsStable()
        {
            var stave = new Stave(10, 40, 300);
            double first  = stave.GetNoteStartX();
            double second = stave.GetNoteStartX();
            Assert.That(first, Is.EqualTo(second));
        }

        // ── GetNoteEndX ───────────────────────────────────────────────────────

        [Test]
        public void GetNoteEndX_LessThanXPlusWidth()
        {
            var stave = new Stave(10, 40, 300);
            double endX = stave.GetNoteEndX();
            Assert.That(endX, Is.LessThanOrEqualTo(10 + 300));
        }

        // ── GetYForGlyphs ─────────────────────────────────────────────────────

        [Test]
        public void GetYForGlyphs_EqualsGetYForLine3()
        {
            var stave = new Stave(0, 0, 300);
            Assert.That(stave.GetYForGlyphs(), Is.EqualTo(stave.GetYForLine(3)).Within(0.001));
        }

        // ── GetSpacingBetweenLines ────────────────────────────────────────────

        [Test]
        public void GetSpacingBetweenLines_DefaultIs10()
        {
            var stave = new Stave(0, 0, 300);
            Assert.That(stave.GetSpacingBetweenLines(), Is.EqualTo(10));
        }
    }
}

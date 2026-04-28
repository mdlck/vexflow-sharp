// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Linq;
using NUnit.Framework;
using VexFlowSharp.Tests.Rendering;

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
            Assert.That(stave.GetNumLines(), Is.EqualTo((int)Metrics.GetDouble("Stave.numLines")));
        }

        [Test]
        public void StaveOptions_DefaultsComeFromMetrics()
        {
            var options = new StaveOptions();

            Assert.That(options.NumLines, Is.EqualTo((int)Metrics.GetDouble("Stave.numLines")));
            Assert.That(options.SpacingBetweenLinesPx, Is.EqualTo(Metrics.GetDouble("Stave.spacingBetweenLinesPx")));
            Assert.That(options.SpaceAboveStaffLn, Is.EqualTo(Metrics.GetDouble("Stave.spaceAboveStaffLn")));
            Assert.That(options.SpaceBelowStaffLn, Is.EqualTo(Metrics.GetDouble("Stave.spaceBelowStaffLn")));
            Assert.That(options.TopTextPosition, Is.EqualTo(Metrics.GetDouble("Stave.topTextPosition")));
            Assert.That(options.BottomTextPosition, Is.EqualTo(Metrics.GetDouble("Stave.bottomTextPosition")));
            Assert.That(options.VerticalBarWidth, Is.EqualTo(Metrics.GetDouble("Stave.verticalBarWidth")));
            Assert.That(options.FillStyle, Is.EqualTo(Metrics.GetString("Stave.strokeStyle")));
        }

        [Test]
        public void Setters_ReturnStaveForV5StyleChaining()
        {
            var stave = new Stave(0, 0, 400);

            var returned = stave
                .SetX(10)
                .SetY(20)
                .SetWidth(300)
                .SetNumLines(4)
                .SetMeasure(12);

            Assert.That(returned, Is.SameAs(stave));
            Assert.That(stave.GetX(), Is.EqualTo(10));
            Assert.That(stave.GetY(), Is.EqualTo(20));
            Assert.That(stave.GetWidth(), Is.EqualTo(300));
            Assert.That(stave.GetNumLines(), Is.EqualTo(4));
            Assert.That(stave.GetMeasure(), Is.EqualTo(12));
        }

        [Test]
        public void Category_IsStave()
        {
            var stave = new Stave(0, 0, 400);

            Assert.That(stave.GetCategory(), Is.EqualTo(Stave.CATEGORY));
        }

        [Test]
        public void StaveModifierCategories_MatchV5CategoryNames()
        {
            Assert.That(new Barline(BarlineType.Single).GetCategory(), Is.EqualTo(Barline.CATEGORY));
            Assert.That(new Clef("treble").GetCategory(), Is.EqualTo(Clef.CATEGORY));
            Assert.That(new KeySignature("G").GetCategory(), Is.EqualTo(KeySignature.CATEGORY));
            Assert.That(new TimeSignature("4/4").GetCategory(), Is.EqualTo(TimeSignature.CATEGORY));
            Assert.That(new Repetition(RepetitionType.DC).GetCategory(), Is.EqualTo(Repetition.CATEGORY));
            Assert.That(new StaveSection("A").GetCategory(), Is.EqualTo(StaveSection.CATEGORY));
            Assert.That(new StaveTempo(new StaveTempoOptions { Duration = "q", Bpm = 120 }).GetCategory(), Is.EqualTo(StaveTempo.CATEGORY));
            Assert.That(new StaveText("Fine", StaveModifierPosition.Above).GetCategory(), Is.EqualTo(StaveText.CATEGORY));
            Assert.That(new Volta(VoltaType.Begin, "1.").GetCategory(), Is.EqualTo(Volta.CATEGORY));
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

        [Test]
        public void TopAndBottomLineBounds_UseStrokeAlignedCoordinates()
        {
            var stave = new Stave(0, 0, 300);

            Assert.That(stave.GetTopLineTopY(), Is.EqualTo(stave.GetYForLine(0)).Within(0.001));
            Assert.That(stave.GetBottomLineBottomY(), Is.EqualTo(stave.GetYForLine(4) + Tables.STAVE_LINE_THICKNESS).Within(0.001));
        }

        [Test]
        public void StaveLineBounds_UseConfiguredLineWidth()
        {
            var stave = new Stave(0, 0, 300);
            stave.SetStyle(new ElementStyle { LineWidth = 4 });

            Assert.That(stave.GetBottomLineBottomY(), Is.EqualTo(stave.GetYForLine(4) + 4).Within(0.001));
        }

        [Test]
        public void Draw_UsesConfiguredLineWidthForLineRectangles()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 20, 300, new StaveOptions { LeftBar = false, RightBar = false });
            stave.SetStyle(new ElementStyle { LineWidth = 4 });
            stave.SetContext(ctx);

            stave.Draw();

            var firstLine = ctx.GetCalls("FillRect").First();
            Assert.That(firstLine.Args, Is.EqualTo(new[] { 10.0, stave.GetYForLine(0), 300.0, 4.0 }));
        }

        [Test]
        public void GetBoundingBox_UsesV5BottomY()
        {
            var stave = new Stave(10, 20, 300);

            var box = stave.GetBoundingBox();

            Assert.That(stave.GetBottomLineY(), Is.EqualTo(stave.GetYForLine(5)).Within(0.001));
            Assert.That(stave.GetBottomY(), Is.EqualTo(stave.GetBottomLineY() + 40).Within(0.001));
            Assert.That(box, Is.Not.Null);
            Assert.That(box!.GetX(), Is.EqualTo(10));
            Assert.That(box.GetY(), Is.EqualTo(20));
            Assert.That(box.GetW(), Is.EqualTo(300));
            Assert.That(box.GetH(), Is.EqualTo(stave.GetBottomY() - stave.GetY()).Within(0.001));
        }

        [Test]
        public void DefaultLedgerLineStyle_UsesV5LineWidth()
        {
            var stave = new Stave(0, 0, 300);

            Assert.That(stave.GetDefaultLedgerLineStyle().StrokeStyle, Is.EqualTo("#444"));
            Assert.That(stave.GetDefaultLedgerLineStyle().LineWidth, Is.EqualTo(2));
        }

        [Test]
        public void DefaultLedgerLineStyle_MergesStaveStyle()
        {
            var stave = new Stave(0, 0, 300);
            stave.SetStyle(new ElementStyle { ShadowBlur = 5, StrokeStyle = "red" });

            var style = stave.GetDefaultLedgerLineStyle();

            Assert.That(style.ShadowBlur, Is.EqualTo(5));
            Assert.That(style.StrokeStyle, Is.EqualTo("#444"));
        }

        // ── GetSpacingBetweenLines ────────────────────────────────────────────

        [Test]
        public void GetSpacingBetweenLines_DefaultIs10()
        {
            var stave = new Stave(0, 0, 300);
            Assert.That(stave.GetSpacingBetweenLines(), Is.EqualTo(Metrics.GetDouble("Stave.spacingBetweenLinesPx")));
        }

        [Test]
        public void GetModifierXShift_NoBeginModifiersAfterBarline_IsZero()
        {
            var stave = new Stave(10, 40, 300);

            Assert.That(stave.GetModifierXShift(), Is.EqualTo(0).Within(0.001));
        }

        [Test]
        public void GetModifierXShift_UsesFormattedBeginModifierShift()
        {
            var stave = new Stave(10, 40, 300);
            stave.AddClef("treble");

            Assert.That(stave.GetModifierXShift(), Is.EqualTo(stave.GetNoteStartX() - stave.GetX()).Within(0.001));
            Assert.That(stave.GetModifierXShift(), Is.GreaterThan(0));
        }

        [Test]
        public void GetModifierXShift_RepeatBeginSubtractsBeginRepeatWidth()
        {
            var stave = new Stave(10, 40, 300);
            stave.SetBegBarType(BarlineType.RepeatBegin);
            stave.AddClef("treble");

            var beginBarline = (Barline)stave.GetModifiers(StaveModifierPosition.Begin)[0];

            Assert.That(
                stave.GetModifierXShift(),
                Is.EqualTo(stave.GetNoteStartX() - stave.GetX() - beginBarline.GetWidth()).Within(0.001));
        }

        [Test]
        public void GetModifierXShift_RightPositionModifierReturnsZero()
        {
            var stave = new Stave(10, 40, 300);
            stave.AddClef("treble");
            stave.AddClef("treble", position: StaveModifierPosition.Right);

            Assert.That(stave.GetModifierXShift(3), Is.EqualTo(0).Within(0.001));
        }

        [Test]
        public void LineConfig_CanHideIndividualLine()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(0, 0, 300, new StaveOptions { LeftBar = false, RightBar = false });
            stave.SetContext(ctx);

            stave.SetConfigForLine(2, new StaveLineConfig { Visible = false });
            stave.Draw();

            Assert.That(stave.GetConfigForLines()[2].Visible, Is.False);
            Assert.That(ctx.GetCalls("FillRect").Count(), Is.EqualTo(4));
        }

        [Test]
        public void LineConfig_ValidatesLineRangeAndVisibleValue()
        {
            var stave = new Stave(0, 0, 300);

            Assert.Throws<VexFlowException>(() => stave.SetConfigForLine(5, new StaveLineConfig { Visible = true }));
            Assert.Throws<VexFlowException>(() => stave.SetConfigForLine(0, new StaveLineConfig()));
        }

        [Test]
        public void LineConfig_AllLinesMustMatchNumLines()
        {
            var stave = new Stave(0, 0, 300);

            Assert.Throws<VexFlowException>(() => stave.SetConfigForLines(new System.Collections.Generic.List<StaveLineConfig>
            {
                new StaveLineConfig { Visible = true },
            }));
        }

        [Test]
        public void Format_RepeatBeginWithBeginModifiersPlacesRepeatAfterClef()
        {
            var stave = new Stave(10, 40, 300);
            stave.SetBegBarType(BarlineType.RepeatBegin);
            stave.AddClef("treble");

            stave.Format();

            var repeat = (Barline)stave.GetModifiers(StaveModifierPosition.Begin, "Barline")[0];
            var clef = stave.GetModifiers(StaveModifierPosition.Begin, "Clef")[0];
            Assert.That(repeat.GetX(), Is.GreaterThan(clef.GetX()));
        }
    }
}

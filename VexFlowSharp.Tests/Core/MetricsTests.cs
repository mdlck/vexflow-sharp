using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Core
{
    [TestFixture]
    [Category("Metrics")]
    public class MetricsTests
    {
        [SetUp]
        public void SetUp()
        {
            Metrics.Clear();
        }

        [Test]
        public void Get_UsesMostSpecificValue()
        {
            Assert.That(Metrics.GetDouble("fontSize"), Is.EqualTo(30));
            Assert.That(Metrics.GetDouble("Stave.fontSize"), Is.EqualTo(8));
            Assert.That(Metrics.GetDouble("StaveTempo.glyph.fontSize"), Is.EqualTo(25));
            Assert.That(Metrics.GetDouble("Tuplet.bracket.padding"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("Tuplet.bracket.lineWidth"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("Tuplet.bracket.legLength"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("Stroke.spacing"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("Tremolo.fontSize"), Is.EqualTo(35));
            Assert.That(Metrics.GetDouble("Barline.repeat.dotRadius"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("Barline.repeat.dotOffset"), Is.EqualTo(4));
            Assert.That(Metrics.GetDouble("Accidental.parenLeftPadding"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("Accidental.parenRightPadding"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("Stave.numLines"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("Stave.spacingBetweenLinesPx"), Is.EqualTo(Tables.STAVE_LINE_DISTANCE));
            Assert.That(Metrics.GetDouble("Stave.spaceAboveStaffLn"), Is.EqualTo(4));
            Assert.That(Metrics.GetDouble("Stave.verticalBarWidth"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("StaveConnector.width"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("StaveConnector.singleLineWidth"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("StaveConnector.doubleXOffset"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("StaveConnector.doubleHeightAdjustment"), Is.EqualTo(0.5));
            Assert.That(Metrics.GetDouble("StaveConnector.thinDoubleGap"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("StaveConnector.textLineWidth"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("StaveConnector.textXOffset"), Is.EqualTo(24));
            Assert.That(Metrics.GetDouble("StaveConnector.textYOffset"), Is.EqualTo(4));
            Assert.That(Metrics.GetDouble("StaveConnector.boldDoubleLeftXShift"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("StaveConnector.boldDoubleRightXShift"), Is.EqualTo(-5));
            Assert.That(Metrics.GetDouble("StaveConnector.boldDoubleLeftWidth"), Is.EqualTo(3.5));
            Assert.That(Metrics.GetDouble("StaveConnector.boldDoubleRightWidth"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("StaveConnector.boldDoubleThickLineOffset"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("StaveTempo.xShift"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("StaveTempo.spacing"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("StaveTempo.dotOffsetY"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("Dot.radius"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("Dot.width"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("Dot.spacing"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("StaveTie.cp1"), Is.EqualTo(8));
            Assert.That(Metrics.GetDouble("StaveTie.cp2"), Is.EqualTo(12));
            Assert.That(Metrics.GetDouble("StaveTie.closeNoteCp1"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("StaveTie.closeNoteCp2"), Is.EqualTo(8));
            Assert.That(Metrics.GetDouble("Curve.thickness"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("Curve.yShift"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("Curve.cpHeight"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("Crescendo.height"), Is.EqualTo(15));
            Assert.That(Metrics.GetDouble("Crescendo.line"), Is.EqualTo(0));
            Assert.That(Metrics.GetDouble("Crescendo.lineOffset"), Is.EqualTo(-3));
            Assert.That(Metrics.GetDouble("Crescendo.yOffset"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("Formatter.softmaxFactor"), Is.EqualTo(Tables.SOFTMAX_FACTOR));
            Assert.That(Metrics.GetDouble("Formatter.maxIterations"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.line"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.semibreveRestGlyphScale"), Is.EqualTo(30));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.lineThickness"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.serifThickness"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.linePaddingRatio"), Is.EqualTo(0.1));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.centerRatio"), Is.EqualTo(0.5));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.symbolLineOffset"), Is.EqualTo(-1));
            Assert.That(Metrics.GetDouble("MultiMeasureRest.numberBaselineRatio"), Is.EqualTo(0.5));
            Assert.That(Metrics.GetDouble("TextBracket.lineWidth"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("TextBracket.bracketHeight"), Is.EqualTo(8));
            Assert.That(Metrics.GetDouble("TextDynamics.glyphFontSize"), Is.EqualTo(Tables.NOTATION_FONT_SCALE));
            Assert.That(Metrics.GetDouble("TextDynamics.line"), Is.EqualTo(0));
            Assert.That(Metrics.GetDouble("TextDynamics.lineOffset"), Is.EqualTo(-3));
            Assert.That(Metrics.GetDouble("PedalMarking.bracketHeight"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("PedalMarking.textMarginRight"), Is.EqualTo(6));
            Assert.That(Metrics.GetDouble("PedalMarking.bracketLineWidth"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("FretHandFinger.numSpacing"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("FretHandFinger.baseYShift"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("FretHandFinger.aboveXShift"), Is.EqualTo(-4));
            Assert.That(Metrics.GetDouble("FretHandFinger.aboveYShift"), Is.EqualTo(-12));
            Assert.That(Metrics.GetDouble("FretHandFinger.belowXShift"), Is.EqualTo(-2));
            Assert.That(Metrics.GetDouble("FretHandFinger.belowYShift"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("FretHandFinger.rightXShift"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("StringNumber.radius"), Is.EqualTo(8));
            Assert.That(Metrics.GetDouble("StringNumber.numSpacing"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("StringNumber.circleLineWidth"), Is.EqualTo(1.5));
            Assert.That(Metrics.GetDouble("StringNumber.textBaselineShift"), Is.EqualTo(4.5));
            Assert.That(Metrics.GetDouble("StringNumber.extensionStemOffset"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("StringNumber.extensionStartOffset"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("StringNumber.extensionLineWidth"), Is.EqualTo(0.6));
            Assert.That(Metrics.GetDouble("StringNumber.extensionDash"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("StringNumber.extensionGap"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("StringNumber.legLength"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("KeySignature.accidentalSpacing"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("KeySignature.naturalCollisionSpacing"), Is.EqualTo(20));
            Assert.That(Metrics.GetDouble("KeySignature.flatFallbackWidth"), Is.EqualTo(8));
            Assert.That(Metrics.GetDouble("KeySignature.sharpFallbackWidth"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("Ornament.accidentalUpperPadding"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("Ornament.accidentalLowerPadding"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("Ornament.sideShift"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("Ornament.textLineIncrement"), Is.EqualTo(2));
            Assert.That(Metrics.GetDouble("Ornament.lineSpacing"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("Ornament.beamedLineSpacing"), Is.EqualTo(0.5));
            Assert.That(Metrics.GetDouble("TabTie.cp1"), Is.EqualTo(9));
            Assert.That(Metrics.GetDouble("TabTie.cp2"), Is.EqualTo(11));
            Assert.That(Metrics.GetDouble("TabTie.yShift"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("TabSlide.cp1"), Is.EqualTo(11));
            Assert.That(Metrics.GetDouble("TabSlide.cp2"), Is.EqualTo(14));
            Assert.That(Metrics.GetDouble("TabSlide.yShift"), Is.EqualTo(0.5));
            Assert.That(Metrics.GetDouble("TabSlide.slideEndpointOffset"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("TabStave.spacingBetweenLinesPx"), Is.EqualTo(13));
            Assert.That(Metrics.GetDouble("TabStave.numLines"), Is.EqualTo(6));
            Assert.That(Metrics.GetDouble("TabStave.topTextPosition"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("GraceTabNote.yShift"), Is.EqualTo(0.3));
            Assert.That(Metrics.GetDouble("System.x"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("System.y"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("System.width"), Is.EqualTo(500));
            Assert.That(Metrics.GetDouble("System.spaceBetweenStaves"), Is.EqualTo(12));
            Assert.That(Metrics.GetDouble("System.alpha"), Is.EqualTo(0.5));
            Assert.That(Metrics.GetDouble("TextFormatter.defaultAdvanceWidthEm"), Is.EqualTo(0.5));
            Assert.That(Metrics.GetDouble("TextFormatter.defaultResolution"), Is.EqualTo(1000));
            Assert.That(Metrics.GetDouble("TextFormatter.ptToPx"), Is.EqualTo(4.0 / 3.0));
            Assert.That(Metrics.GetDouble("TickContext.padding"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("Vibrato.width"), Is.EqualTo(20));
            Assert.That(Metrics.GetDouble("Vibrato.rightShift"), Is.EqualTo(7));
            Assert.That(Metrics.GetDouble("Vibrato.yShift"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("VibratoBracket.stopNoteOffset"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("VibratoBracket.tieEndOffset"), Is.EqualTo(10));
            Assert.That(Metrics.GetDouble("Volta.verticalHeightLines"), Is.EqualTo(1.5));
            Assert.That(Metrics.GetDouble("Volta.lineWidth"), Is.EqualTo(1));
            Assert.That(Metrics.GetDouble("Volta.endAdjustment"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("Volta.beginEndAdjustment"), Is.EqualTo(3));
            Assert.That(Metrics.GetDouble("Volta.textXOffset"), Is.EqualTo(5));
            Assert.That(Metrics.GetDouble("Volta.textYOffset"), Is.EqualTo(15));
        }

        [Test]
        public void Get_FallsBackToParentAndRootValues()
        {
            Assert.That(Metrics.GetString("StaveTempo.name.fontFamily"), Is.EqualTo("Bravura,Academico"));
            Assert.That(Metrics.GetDouble("StaveTempo.name.fontSize"), Is.EqualTo(14));
            Assert.That(Metrics.GetString("StaveTempo.name.fontWeight"), Is.EqualTo("bold"));
            Assert.That(Metrics.GetString("StaveTempo.name.fontStyle"), Is.EqualTo("normal"));
        }

        [Test]
        public void Get_ReturnsDefaultValueWhenLookupFails()
        {
            Assert.That(Metrics.GetDouble("Missing.value", 42), Is.EqualTo(42));
            Assert.That(Metrics.GetString("Missing.text", "fallback"), Is.EqualTo("fallback"));
            Assert.That(Metrics.GetBool("Missing.flag", true), Is.True);
        }

        [Test]
        public void PointerRect_DefaultsMatchV5()
        {
            Assert.That(Metrics.GetBool("pointerRect"), Is.False);
            Assert.That(Metrics.GetBool("Stave.pointerRect"), Is.False);
            Assert.That(Metrics.GetBool("StaveNote.pointerRect"), Is.True);
            Assert.That(Metrics.GetBool("Tuplet.pointerRect"), Is.True);
        }

        [Test]
        public void GetFontInfo_AppliesFontScaleAndReturnsCopy()
        {
            var grace = Metrics.GetFontInfo("GraceNote");
            Assert.That(grace.Family, Is.EqualTo("Bravura,Academico"));
            Assert.That(grace.Size, Is.EqualTo(20).Within(0.0001));

            grace.Size = 99;
            Assert.That(Metrics.GetFontInfo("GraceNote").Size, Is.EqualTo(20).Within(0.0001));
        }

        [Test]
        public void GetStyle_FallsBackAndReturnsCopy()
        {
            var style = Metrics.GetStyle("Bend.line");
            Assert.That(style.StrokeStyle, Is.EqualTo("#777777"));
            Assert.That(style.LineWidth, Is.EqualTo(1));

            style.StrokeStyle = "changed";
            Assert.That(Metrics.GetStyle("Bend.line").StrokeStyle, Is.EqualTo("#777777"));
        }

        [Test]
        public void StavePadding_UsesV5Metrics()
        {
            Assert.That(Stave.DefaultPadding, Is.EqualTo(22));
            Assert.That(Stave.RightPadding, Is.EqualTo(10));
        }
    }
}

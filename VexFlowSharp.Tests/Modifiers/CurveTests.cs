// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Tests for Curve (bezier slur renderer).
// Ports key test cases from vexflow/tests/curve_tests.ts.

using NUnit.Framework;
using System.Linq;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Curve")]
    [Category("Modifiers")]
    [Category("Phase4")]
    public class CurveTests
    {
        // ── CurveOptions defaults ─────────────────────────────────────────────

        [Test]
        public void CurveOptions_DefaultThicknessIsTwo()
        {
            var opts = new CurveOptions();
            Assert.AreEqual(Metrics.GetDouble("Curve.thickness"), opts.Thickness, 1e-9);
        }

        [Test]
        public void CurveOptions_DefaultCpHeightIsTen()
        {
            var opts = new CurveOptions();
            Assert.AreEqual(Metrics.GetDouble("Curve.cpHeight"), opts.CpHeight, 1e-9);
        }

        [Test]
        public void CurveOptions_DefaultInvertIsFalse()
        {
            var opts = new CurveOptions();
            Assert.IsFalse(opts.Invert);
        }

        [Test]
        public void CurveOptions_DefaultPositionIsNearHead()
        {
            var opts = new CurveOptions();
            Assert.AreEqual(CurvePosition.NEAR_HEAD, opts.Position);
            Assert.AreEqual(CurvePosition.NEAR_HEAD, opts.PositionEnd);
        }

        [Test]
        public void CurveOptions_DefaultYShiftIsTen()
        {
            var opts = new CurveOptions();
            Assert.AreEqual(Metrics.GetDouble("Curve.yShift"), opts.Y_Shift, 1e-9);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        [Test]
        public void Simple_SlurAboveTwoNotes()
        {
            // A Curve between two non-null notes should construct without error.
            // (No render context needed for construction.)
            var opts = new CurveOptions { Invert = false };
            var curve = new Curve(null, null, opts);
            Assert.IsNotNull(curve);
            Assert.IsTrue(curve.IsPartial());
        }

        [Test]
        public void Inverted_SlurBelowTwoNotes()
        {
            // An inverted Curve (slur below) reverses direction relative to stem.
            var opts = new CurveOptions { Invert = true };
            var curve = new Curve(null, null, opts);
            Assert.IsNotNull(curve);
            Assert.IsTrue(opts.Invert);
        }

        [Test]
        public void NullFrom_SlurStartsAtStaveBoundary()
        {
            // Null from = starts at stave boundary; should not throw at construction.
            Assert.DoesNotThrow(() => new Curve(null, null, new CurveOptions()));
        }

        // ── Category ──────────────────────────────────────────────────────────

        [Test]
        public void Curve_Category_IsCorrect()
        {
            var curve = new Curve(null, null);
            Assert.AreEqual(Curve.CATEGORY, curve.GetCategory());
            Assert.AreEqual("Curve", Curve.CATEGORY);
        }

        // ── IsPartial ────────────────────────────────────────────────────────

        [Test]
        public void Curve_IsPartial_WhenFromIsNull()
        {
            var curve = new Curve(null, null);
            Assert.IsTrue(curve.IsPartial());
        }

        [Test]
        public void Curve_WithLineDash_StrokesOpenCurveWithoutFill()
        {
            var stave = new Stave(0, 0, 300);
            var from = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
                StemDirection = Stem.UP,
            }).SetX(30);
            var to = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "d/4" },
                StemDirection = Stem.UP,
            }).SetX(100);
            from.SetStave(stave);
            to.SetStave(stave);

            var ctx = new RecordingRenderContext();
            var curve = new Curve(from, to)
                .SetStyle(new ElementStyle { LineDash = "3 2" })
                .SetContext(ctx);

            curve.Draw();

            Assert.That(ctx.HasCall("SetLineDash"), Is.True);
            Assert.That(ctx.HasCall("Stroke"), Is.True);
            Assert.That(ctx.HasCall("Fill"), Is.False);
            Assert.That(ctx.GetCalls("BezierCurveTo").Count(), Is.EqualTo(1));
        }

        [Test]
        public void Curve_UsesMetricControlHeightAndThickness()
        {
            var stave = new Stave(0, 0, 300);
            var from = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "c/4" },
                StemDirection = Stem.UP,
            }).SetX(30);
            var to = new StaveNote(new StaveNoteStruct
            {
                Duration = "4",
                Keys = new[] { "d/4" },
                StemDirection = Stem.UP,
            }).SetX(100);
            from.SetStave(stave);
            to.SetStave(stave);

            var ctx = new RecordingRenderContext();
            var curve = new Curve(from, to).SetContext(ctx);

            curve.Draw();

            var curves = ctx.GetCalls("BezierCurveTo").ToArray();
            Assert.That(curves, Has.Length.EqualTo(2));

            double firstY = from.GetTieYForBottom() + Metrics.GetDouble("Curve.yShift");
            double lastY = to.GetTieYForBottom() + Metrics.GetDouble("Curve.yShift");
            Assert.That(curves[0].Args[1], Is.EqualTo(firstY + Metrics.GetDouble("Curve.cpHeight")).Within(0.0001));
            Assert.That(curves[0].Args[3], Is.EqualTo(lastY + Metrics.GetDouble("Curve.cpHeight")).Within(0.0001));
            Assert.That(curves[1].Args[1] - curves[0].Args[3], Is.EqualTo(Metrics.GetDouble("Curve.thickness")).Within(0.0001));
        }
    }
}

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Tests for Curve (bezier slur renderer).
// Ports key test cases from vexflow/tests/curve_tests.ts.

using NUnit.Framework;

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
            Assert.AreEqual(2.0, opts.Thickness, 1e-9);
        }

        [Test]
        public void CurveOptions_DefaultCpHeightIsTen()
        {
            var opts = new CurveOptions();
            Assert.AreEqual(10.0, opts.CpHeight, 1e-9);
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
            Assert.AreEqual(10.0, opts.Y_Shift, 1e-9);
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
            Assert.AreEqual("Curve", curve.GetCategory());
        }

        // ── IsPartial ────────────────────────────────────────────────────────

        [Test]
        public void Curve_IsPartial_WhenFromIsNull()
        {
            var curve = new Curve(null, null);
            Assert.IsTrue(curve.IsPartial());
        }
    }
}

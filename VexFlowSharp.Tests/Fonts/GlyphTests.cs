// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using NUnit.Framework;

namespace VexFlowSharp.Tests.Fonts
{
    /// <summary>
    /// A recording RenderContext that logs all method calls for assertion in tests.
    /// </summary>
    internal class RecordingRenderContext : RenderContext
    {
        public List<(string Method, double[] Args)> Calls { get; } = new();

        private void Record(string method, params double[] args) =>
            Calls.Add((method, args));

        public override void Clear() => Record("Clear");
        public override RenderContext Save() { Record("Save"); return this; }
        public override RenderContext Restore() { Record("Restore"); return this; }
        public override RenderContext SetFillStyle(string style) { Record("SetFillStyle"); return this; }
        public override RenderContext SetBackgroundFillStyle(string style) { Record("SetBackgroundFillStyle"); return this; }
        public override RenderContext SetStrokeStyle(string style) { Record("SetStrokeStyle"); return this; }
        public override RenderContext SetShadowColor(string color) { Record("SetShadowColor"); return this; }
        public override RenderContext SetShadowBlur(double blur) { Record("SetShadowBlur", blur); return this; }
        public override RenderContext SetLineWidth(double width) { Record("SetLineWidth", width); return this; }
        public override RenderContext SetLineCap(string capType) { Record("SetLineCap"); return this; }
        public override RenderContext SetLineDash(double[] dashPattern) { Record("SetLineDash"); return this; }
        public override RenderContext Scale(double x, double y) { Record("Scale", x, y); return this; }
        public override RenderContext Resize(double width, double height) { Record("Resize", width, height); return this; }
        public override RenderContext Rect(double x, double y, double width, double height) { Record("Rect", x, y, width, height); return this; }
        public override RenderContext FillRect(double x, double y, double width, double height) { Record("FillRect", x, y, width, height); return this; }
        public override RenderContext ClearRect(double x, double y, double width, double height) { Record("ClearRect", x, y, width, height); return this; }
        public override RenderContext BeginPath() { Record("BeginPath"); return this; }
        public override RenderContext MoveTo(double x, double y) { Record("MoveTo", x, y); return this; }
        public override RenderContext LineTo(double x, double y) { Record("LineTo", x, y); return this; }
        public override RenderContext BezierCurveTo(double cp1x, double cp1y, double cp2x, double cp2y, double x, double y)
        {
            Record("BezierCurveTo", cp1x, cp1y, cp2x, cp2y, x, y);
            return this;
        }
        public override RenderContext QuadraticCurveTo(double cpx, double cpy, double x, double y)
        {
            Record("QuadraticCurveTo", cpx, cpy, x, y);
            return this;
        }
        public override RenderContext Arc(double x, double y, double radius, double startAngle, double endAngle, bool counterclockwise)
        {
            Record("Arc", x, y, radius, startAngle, endAngle);
            return this;
        }
        public override RenderContext ClosePath() { Record("ClosePath"); return this; }
        public override RenderContext Fill() { Record("Fill"); return this; }
        public override RenderContext Stroke() { Record("Stroke"); return this; }
        public override RenderContext FillText(string text, double x, double y) { Record("FillText", x, y); return this; }
        public override RenderContext SetFont(string family, double size, string weight = "normal", string style = "normal")
        {
            Record("SetFont", size);
            return this;
        }
        public override string GetFont() => "";
        public override TextMeasure MeasureText(string text) => new TextMeasure();

        public bool WasCalled(string method)
        {
            foreach (var call in Calls)
                if (call.Method == method) return true;
            return false;
        }
    }

    [TestFixture]
    [Category("Glyph")]
    public class GlyphTests
    {
        [Test]
        public void Category_IsV5Glyph()
        {
            var glyph = new Glyph("noteheadBlack", 40);

            Assert.That(Glyph.CATEGORY, Is.EqualTo("Glyph"));
            Assert.That(glyph.GetCategory(), Is.EqualTo(Glyph.CATEGORY));
        }

        /// <summary>
        /// Test 1: Y-inversion correctness.
        /// MOVE to (100,200) then LINE to (300,400) with scale=1.0, origin=(0,0)
        /// should yield MoveTo(100, -200) and LineTo(300, -400) — Y is negated.
        /// </summary>
        [Test]
        public void RenderOutline_YInversion_NegatesY()
        {
            var ctx = new RecordingRenderContext();
            // MOVE: code=0, x=100, y=200; LINE: code=1, x=300, y=400
            var outline = new int[] { 0, 100, 200, 1, 300, 400 };

            Glyph.RenderOutline(ctx, outline, scale: 1.0, xPos: 0, yPos: 0);

            // Verify MoveTo was called with (100, -200)
            bool foundMove = false, foundLine = false;
            foreach (var call in ctx.Calls)
            {
                if (call.Method == "MoveTo")
                {
                    Assert.That(call.Args[0], Is.EqualTo(100.0).Within(0.001), "MoveTo X");
                    Assert.That(call.Args[1], Is.EqualTo(-200.0).Within(0.001), "MoveTo Y must be negated");
                    foundMove = true;
                }
                else if (call.Method == "LineTo")
                {
                    Assert.That(call.Args[0], Is.EqualTo(300.0).Within(0.001), "LineTo X");
                    Assert.That(call.Args[1], Is.EqualTo(-400.0).Within(0.001), "LineTo Y must be negated");
                    foundLine = true;
                }
            }
            Assert.That(foundMove, Is.True, "MoveTo should have been called");
            Assert.That(foundLine, Is.True, "LineTo should have been called");
        }

        /// <summary>
        /// Test 2: Bezier argument reordering.
        /// Outline: MOVE(0,0) then BEZIER with endpoint=(100,200), cp1=(10,20), cp2=(50,80).
        /// BezierCurveTo must be called with: (cp1x=10, cp1y=-20, cp2x=50, cp2y=-80, endX=100, endY=-200).
        /// The endpoint must be the LAST two arguments.
        /// </summary>
        [Test]
        public void RenderOutline_BezierArgumentOrder_EndpointIsLast()
        {
            var ctx = new RecordingRenderContext();
            // MOVE: code=0, x=0, y=0
            // BEZIER: code=3, endX=100, endY=200, cp1X=10, cp1Y=20, cp2X=50, cp2Y=80
            var outline = new int[] { 0, 0, 0, 3, 100, 200, 10, 20, 50, 80 };

            Glyph.RenderOutline(ctx, outline, scale: 1.0, xPos: 0, yPos: 0);

            bool foundBezier = false;
            foreach (var call in ctx.Calls)
            {
                if (call.Method == "BezierCurveTo")
                {
                    foundBezier = true;
                    // BezierCurveTo(cp1x, cp1y, cp2x, cp2y, endX, endY)
                    Assert.That(call.Args[0], Is.EqualTo(10.0).Within(0.001), "cp1x");
                    Assert.That(call.Args[1], Is.EqualTo(-20.0).Within(0.001), "cp1y (Y-inverted)");
                    Assert.That(call.Args[2], Is.EqualTo(50.0).Within(0.001), "cp2x");
                    Assert.That(call.Args[3], Is.EqualTo(-80.0).Within(0.001), "cp2y (Y-inverted)");
                    Assert.That(call.Args[4], Is.EqualTo(100.0).Within(0.001), "endX (last)");
                    Assert.That(call.Args[5], Is.EqualTo(-200.0).Within(0.001), "endY (last, Y-inverted)");
                }
            }
            Assert.That(foundBezier, Is.True, "BezierCurveTo should have been called");
        }

        /// <summary>
        /// Test 3: Quadratic argument reordering.
        /// Outline: MOVE(0,0) then QUADRATIC with endpoint=(100,200), cp=(50,80).
        /// QuadraticCurveTo must be called with: (cpx=50, cpy=-80, endX=100, endY=-200).
        /// Control point must be FIRST, endpoint LAST.
        /// </summary>
        [Test]
        public void RenderOutline_QuadraticArgumentOrder_ControlPointFirst()
        {
            var ctx = new RecordingRenderContext();
            // MOVE: code=0, x=0, y=0
            // QUADRATIC: code=2, endX=100, endY=200, cpX=50, cpY=80
            var outline = new int[] { 0, 0, 0, 2, 100, 200, 50, 80 };

            Glyph.RenderOutline(ctx, outline, scale: 1.0, xPos: 0, yPos: 0);

            bool foundQuad = false;
            foreach (var call in ctx.Calls)
            {
                if (call.Method == "QuadraticCurveTo")
                {
                    foundQuad = true;
                    // QuadraticCurveTo(cpx, cpy, endX, endY)
                    Assert.That(call.Args[0], Is.EqualTo(50.0).Within(0.001), "cpx (control point, first)");
                    Assert.That(call.Args[1], Is.EqualTo(-80.0).Within(0.001), "cpy (Y-inverted)");
                    Assert.That(call.Args[2], Is.EqualTo(100.0).Within(0.001), "endX (endpoint, last)");
                    Assert.That(call.Args[3], Is.EqualTo(-200.0).Within(0.001), "endY (Y-inverted, last)");
                }
            }
            Assert.That(foundQuad, Is.True, "QuadraticCurveTo should have been called");
        }

        /// <summary>
        /// Test 4: Glyph renders via filled path only (REND-05).
        /// RenderOutline must call BeginPath() then Fill() and must NOT call FillText.
        /// </summary>
        [Test]
        public void RenderOutline_UsesFilledPath_NeverFillText()
        {
            var ctx = new RecordingRenderContext();
            var outline = new int[] { 0, 0, 0, 1, 100, 100 };

            Glyph.RenderOutline(ctx, outline, scale: 1.0, xPos: 0, yPos: 0);

            Assert.That(ctx.WasCalled("BeginPath"), Is.True, "BeginPath must be called");
            Assert.That(ctx.WasCalled("Fill"), Is.True, "Fill must be called (filled path)");
            Assert.That(ctx.WasCalled("FillText"), Is.False, "FillText must NOT be called (REND-05)");
        }
    }
}

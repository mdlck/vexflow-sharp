using System;
using System.Collections.Generic;
using System.Linq;

namespace VexFlowSharp.Tests.Rendering
{
    /// <summary>
    /// A RenderContext implementation that records all method calls for assertion in unit tests.
    /// Used by Glyph outline walker tests (plan 03) and any test that needs to verify
    /// rendering calls without producing a PNG.
    /// </summary>
    public class RecordingRenderContext : RenderContext
    {
        public List<(string Method, double[] Args)> Calls { get; } = new List<(string, double[])>();

        public override void Clear() => Calls.Add(("Clear", Array.Empty<double>()));
        public override RenderContext Save() { Calls.Add(("Save", Array.Empty<double>())); return this; }
        public override RenderContext Restore() { Calls.Add(("Restore", Array.Empty<double>())); return this; }
        public override RenderContext BeginPath() { Calls.Add(("BeginPath", Array.Empty<double>())); return this; }
        public override RenderContext MoveTo(double x, double y) { Calls.Add(("MoveTo", new[] { x, y })); return this; }
        public override RenderContext LineTo(double x, double y) { Calls.Add(("LineTo", new[] { x, y })); return this; }
        public override RenderContext BezierCurveTo(double cp1x, double cp1y, double cp2x, double cp2y, double x, double y)
            { Calls.Add(("BezierCurveTo", new[] { cp1x, cp1y, cp2x, cp2y, x, y })); return this; }
        public override RenderContext QuadraticCurveTo(double cpx, double cpy, double x, double y)
            { Calls.Add(("QuadraticCurveTo", new[] { cpx, cpy, x, y })); return this; }
        public override RenderContext Arc(double x, double y, double radius, double startAngle, double endAngle, bool counterclockwise)
            { Calls.Add(("Arc", new[] { x, y, radius, startAngle, endAngle, counterclockwise ? 1.0 : 0.0 })); return this; }
        public override RenderContext ClosePath() { Calls.Add(("ClosePath", Array.Empty<double>())); return this; }
        public override RenderContext Fill() { Calls.Add(("Fill", Array.Empty<double>())); return this; }
        public override RenderContext Stroke() { Calls.Add(("Stroke", Array.Empty<double>())); return this; }
        public override RenderContext FillText(string text, double x, double y) { Calls.Add(("FillText", new[] { x, y })); return this; }
        public override RenderContext SetFillStyle(string style) { Calls.Add(("SetFillStyle", Array.Empty<double>())); return this; }
        public override RenderContext SetBackgroundFillStyle(string style) { Calls.Add(("SetBackgroundFillStyle", Array.Empty<double>())); return this; }
        public override RenderContext SetStrokeStyle(string style) { Calls.Add(("SetStrokeStyle", Array.Empty<double>())); return this; }
        public override RenderContext SetShadowColor(string color) { Calls.Add(("SetShadowColor", Array.Empty<double>())); return this; }
        public override RenderContext SetShadowBlur(double blur) { Calls.Add(("SetShadowBlur", new[] { blur })); return this; }
        public override RenderContext SetLineWidth(double width) { Calls.Add(("SetLineWidth", new[] { width })); return this; }
        public override RenderContext SetLineCap(string capType) { Calls.Add(("SetLineCap", Array.Empty<double>())); return this; }
        public override RenderContext SetLineDash(double[] dashPattern) { Calls.Add(("SetLineDash", dashPattern)); return this; }
        public override RenderContext Scale(double x, double y) { Calls.Add(("Scale", new[] { x, y })); return this; }
        public override RenderContext Resize(double w, double h) { Calls.Add(("Resize", new[] { w, h })); return this; }
        public override RenderContext Rect(double x, double y, double w, double h) { Calls.Add(("Rect", new[] { x, y, w, h })); return this; }
        public override RenderContext FillRect(double x, double y, double w, double h) { Calls.Add(("FillRect", new[] { x, y, w, h })); return this; }
        public override RenderContext ClearRect(double x, double y, double w, double h) { Calls.Add(("ClearRect", new[] { x, y, w, h })); return this; }
        public override RenderContext SetFont(string family, double size, string weight = "normal", string style = "normal")
            { Calls.Add(("SetFont", new[] { size })); return this; }
        public override string GetFont() => "";
        public override TextMeasure MeasureText(string text) => default;

        // Helper methods for test assertions

        public bool HasCall(string methodName) => Calls.Any(c => c.Method == methodName);
        public (string Method, double[] Args) GetCall(string methodName) => Calls.First(c => c.Method == methodName);
        public IEnumerable<(string Method, double[] Args)> GetCalls(string methodName) => Calls.Where(c => c.Method == methodName);
    }
}

namespace VexFlowSharp
{
    /// <summary>
    /// Abstract rendering interface ported from VexFlow's rendercontext.ts.
    /// All drawing operations go through this class. Implementations include
    /// SkiaRenderContext (test/PNG output) and UnityRenderContext (Unity Painter2D).
    /// </summary>
    public abstract class RenderContext
    {
        public const string CATEGORY = "RenderContext";

        // State
        public abstract void Clear();
        public abstract RenderContext Save();
        public abstract RenderContext Restore();

        // Styles
        public abstract RenderContext SetFillStyle(string style);
        public abstract RenderContext SetBackgroundFillStyle(string style);
        public abstract RenderContext SetStrokeStyle(string style);
        public abstract RenderContext SetShadowColor(string color);
        public abstract RenderContext SetShadowBlur(double blur);
        public abstract RenderContext SetLineWidth(double width);
        public abstract RenderContext SetLineCap(string capType);
        public abstract RenderContext SetLineDash(double[] dashPattern);
        public virtual string FillStyle { get => ""; set => SetFillStyle(value); }
        public virtual string StrokeStyle { get => ""; set => SetStrokeStyle(value); }

        // Transform
        public abstract RenderContext Scale(double x, double y);
        public abstract RenderContext Resize(double width, double height);
        public virtual RenderContext OpenRotation(double degrees, double x, double y) { return this; }
        public virtual RenderContext CloseRotation() { return this; }

        // Rectangles
        public abstract RenderContext Rect(double x, double y, double width, double height);
        public abstract RenderContext FillRect(double x, double y, double width, double height);
        public abstract RenderContext ClearRect(double x, double y, double width, double height);
        public virtual RenderContext PointerRect(double x, double y, double width, double height) { return this; }

        // Path
        public abstract RenderContext BeginPath();
        public abstract RenderContext MoveTo(double x, double y);
        public abstract RenderContext LineTo(double x, double y);
        public abstract RenderContext BezierCurveTo(double cp1x, double cp1y, double cp2x, double cp2y, double x, double y);
        public abstract RenderContext QuadraticCurveTo(double cpx, double cpy, double x, double y);
        public abstract RenderContext Arc(double x, double y, double radius, double startAngle, double endAngle, bool counterclockwise);
        public abstract RenderContext ClosePath();
        public abstract RenderContext Fill();
        public virtual RenderContext Fill(object attributes) => Fill();
        public abstract RenderContext Stroke();

        // Text
        public abstract RenderContext FillText(string text, double x, double y);
        public abstract RenderContext SetFont(string family, double size, string weight = "normal", string style = "normal");
        public abstract string GetFont();
        public abstract TextMeasure MeasureText(string text);

        // Grouping (SVG-only; no-op in Canvas/Skia backends)
        public virtual void OpenGroup(string cls = null, string id = null) { }
        public virtual void CloseGroup() { }
        public virtual void Add(object child) { }
    }
}

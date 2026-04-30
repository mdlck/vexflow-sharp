// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Section label drawn above a stave, optionally inside a rectangle.
    /// Port of VexFlow's StaveSection class from stavesection.ts.
    /// </summary>
    public class StaveSection : StaveModifier
    {
        public new const string CATEGORY = "StaveSection";

        public override string GetCategory() => CATEGORY;

        private string text;
        private readonly double initialX;
        private double yShift;
        private bool drawRect;
        private double height;

        public StaveSection(string section, double x = 0, double yShift = 0, bool drawRect = true)
        {
            text = section;
            initialX = x;
            this.yShift = yShift;
            this.drawRect = drawRect;
            padding = Metrics.GetDouble("StaveSection.padding");
            position = StaveModifierPosition.Above;
            UpdateMetrics();
        }

        public string GetText() => text;
        public double GetYShift() => yShift;
        public bool GetDrawRect() => drawRect;
        public double GetHeight() => height;

        public StaveSection SetText(string section)
        {
            text = section;
            UpdateMetrics();
            return this;
        }

        public StaveSection SetYShift(double shift)
        {
            yShift = shift;
            return this;
        }

        public StaveSection SetDrawRect(bool shouldDrawRect)
        {
            drawRect = shouldDrawRect;
            return this;
        }

        private void UpdateMetrics()
        {
            var font = Metrics.GetFontInfo("StaveSection");
            width = TextFormatter.Create(font.Family, font.Size).GetWidthForTextInPx(text);
            height = font.Size;
        }

        public override void Draw(Stave stave, double xShift)
        {
            var ctx = stave.CheckContext();
            SetContext(ctx);
            SetStave(stave);
            rendered = true;

            var font = Metrics.GetFontInfo("StaveSection");
            double boxWidth = width + 2 * padding;
            double boxHeight = height + 2 * padding;
            double y = stave.GetYForTopText(1.5) + yShift;
            double drawX = stave.GetX() + initialX + xShift;
            double textX = drawX + padding;
            double textY = y - padding;

            ctx.Save();
            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);

            if (drawRect)
            {
                var style = Metrics.GetStyle("StaveSection");
                if (style.StrokeStyle != null) ctx.SetStrokeStyle(style.StrokeStyle);
                if (style.LineWidth.HasValue) ctx.SetLineWidth(style.LineWidth.Value);

                ctx.BeginPath();
                ctx.Rect(drawX, y - boxHeight, boxWidth, boxHeight);
                ctx.Stroke();
            }

            ctx.FillText(text, textX, textY);
            ctx.Restore();
        }
    }
}

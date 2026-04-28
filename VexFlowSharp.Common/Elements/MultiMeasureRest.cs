#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;

namespace VexFlowSharp
{
    public class MultiMeasureRestRenderOptions
    {
        public int NumberOfMeasures { get; set; }
        public bool UseSymbols { get; set; }
        public double SymbolSpacing { get; set; }
        public bool ShowNumber { get; set; } = true;
        public double NumberLine { get; set; } = Metrics.GetDouble("MultiMeasureRest.numberLine");
        public double? NumberGlyphPoint { get; set; }
        public double? PaddingLeft { get; set; }
        public double? PaddingRight { get; set; }
        public double Line { get; set; } = Metrics.GetDouble("MultiMeasureRest.line");
        public double SpacingBetweenLinesPx { get; set; } = Metrics.GetDouble("MultiMeasureRest.spacingBetweenLinesPx");
        public double SemibreveRestGlyphScale { get; set; } = Metrics.GetDouble("MultiMeasureRest.semibreveRestGlyphScale");
        public double LineThickness { get; set; } = Metrics.GetDouble("MultiMeasureRest.lineThickness");
        public double SerifThickness { get; set; } = Metrics.GetDouble("MultiMeasureRest.serifThickness");
    }

    /// <summary>
    /// Multiple-measure rest. Port of VexFlow's MultiMeasureRest class from multimeasurerest.ts.
    /// </summary>
    public class MultiMeasureRest : Element
    {
        public new const string CATEGORY = "MultiMeasureRest";

        private readonly int numberOfMeasures;
        private readonly bool hasPaddingLeft;
        private readonly bool hasPaddingRight;
        private Stave? stave;
        private (double Left, double Right) xs = (double.NaN, double.NaN);

        public MultiMeasureRestRenderOptions RenderOptions { get; }

        public MultiMeasureRest(int numberOfMeasures, MultiMeasureRestRenderOptions? options = null)
        {
            this.numberOfMeasures = numberOfMeasures;
            RenderOptions = options ?? new MultiMeasureRestRenderOptions();
            RenderOptions.NumberOfMeasures = numberOfMeasures;
            RenderOptions.NumberGlyphPoint ??= Metrics.GetDouble("MultiMeasureRest.fontSize");
            hasPaddingLeft = RenderOptions.PaddingLeft.HasValue;
            hasPaddingRight = RenderOptions.PaddingRight.HasValue;

            double digitWidth = 0;
            foreach (var digit in numberOfMeasures.ToString())
            {
                digitWidth += Glyph.GetWidth($"timeSig{digit}", RenderOptions.NumberGlyphPoint.Value);
            }
            boundingBox = new BoundingBox(0, 0, digitWidth, RenderOptions.NumberGlyphPoint.Value);
        }

        public int GetNumberOfMeasures() => numberOfMeasures;
        public (double Left, double Right) GetXs() => xs;

        public MultiMeasureRest SetStave(Stave newStave)
        {
            stave = newStave;
            return this;
        }

        public Stave? GetStave() => stave;

        public Stave CheckStave()
            => stave ?? throw new VexFlowException("NoStave", "No stave attached to instance.");

        public override string GetCategory() => CATEGORY;

        private static double DrawGlyph(RenderContext ctx, string code, double point, double x, double y)
        {
            var glyph = new Glyph(code, point);
            glyph.Render(ctx, x, y);
            return Glyph.GetWidth(code, point);
        }

        private void DrawLine(Stave stave, RenderContext ctx, double left, double right)
        {
            double y = stave.GetYForLine(RenderOptions.Line);
            double padding = (right - left) * Metrics.GetDouble("MultiMeasureRest.linePaddingRatio");
            left += padding;
            right -= padding;

            double point = Metrics.GetDouble("fontSize");
            double leftWidth = Glyph.GetWidth("restHBarLeft", point);
            double middleWidth = Glyph.GetWidth("restHBarMiddle", point);
            double rightWidth = Glyph.GetWidth("restHBarRight", point);
            if (middleWidth <= 0) throw new VexFlowException("BadGlyph", "Cannot draw multi-measure rest line if restHBarMiddle width is 0.");

            double totalWidth = leftWidth + rightWidth + middleWidth;
            while (totalWidth + middleWidth <= right - left)
                totalWidth += middleWidth;

            double centerRatio = Metrics.GetDouble("MultiMeasureRest.centerRatio");
            double x = left + (right - left) * centerRatio - totalWidth * centerRatio;
            x += DrawGlyph(ctx, "restHBarLeft", point, x, y);
            while (x + middleWidth + rightWidth <= left + (right - left) * centerRatio + totalWidth * centerRatio)
                x += DrawGlyph(ctx, "restHBarMiddle", point, x, y);
            DrawGlyph(ctx, "restHBarRight", point, x, y);
        }

        private void DrawSymbols(Stave stave, RenderContext ctx, double left, double right)
        {
            int n4 = numberOfMeasures / 4;
            int n = numberOfMeasures % 4;
            int n2 = n / 2;
            int n1 = n % 2;

            double point = RenderOptions.SemibreveRestGlyphScale;
            double width = n4 * (Glyph.GetWidth("restLonga", point) + RenderOptions.SymbolSpacing)
                + n2 * (Glyph.GetWidth("restDoubleWhole", point) + RenderOptions.SymbolSpacing)
                + n1 * (Glyph.GetWidth("restWhole", point) + RenderOptions.SymbolSpacing);
            if (width > 0) width -= RenderOptions.SymbolSpacing;

            double centerRatio = Metrics.GetDouble("MultiMeasureRest.centerRatio");
            double x = left + (right - left) * centerRatio - width * centerRatio;
            double yTop = stave.GetYForLine(RenderOptions.Line + Metrics.GetDouble("MultiMeasureRest.symbolLineOffset"));
            double yMiddle = stave.GetYForLine(RenderOptions.Line);

            for (int i = 0; i < n4; i++)
                x += DrawGlyph(ctx, "restLonga", point, x, yMiddle) + RenderOptions.SymbolSpacing;
            for (int i = 0; i < n2; i++)
                x += DrawGlyph(ctx, "restDoubleWhole", point, x, yMiddle) + RenderOptions.SymbolSpacing;
            for (int i = 0; i < n1; i++)
                x += DrawGlyph(ctx, "restWhole", point, x, yTop) + RenderOptions.SymbolSpacing;
        }

        private void DrawNumber(Stave stave, RenderContext ctx, double left, double right)
        {
            if (!RenderOptions.ShowNumber) return;

            double point = RenderOptions.NumberGlyphPoint ?? Metrics.GetDouble("MultiMeasureRest.fontSize");
            double width = 0;
            foreach (var digit in numberOfMeasures.ToString())
                width += Glyph.GetWidth($"timeSig{digit}", point);

            double centerRatio = Metrics.GetDouble("MultiMeasureRest.centerRatio");
            double x = left + (right - left) * centerRatio - width * centerRatio;
            double y = stave.GetYForLine(RenderOptions.NumberLine) - point * Metrics.GetDouble("MultiMeasureRest.numberBaselineRatio");
            foreach (var digit in numberOfMeasures.ToString())
            {
                x += DrawGlyph(ctx, $"timeSig{digit}", point, x, y);
            }
        }

        public override void Draw()
        {
            var ctx = CheckContext();
            var stave = CheckStave();
            rendered = true;

            double left = stave.GetNoteStartX();
            double right = stave.GetNoteEndX();

            var beginModifiers = stave.GetModifiers(StaveModifierPosition.Begin);
            if (beginModifiers.Count == 1 && beginModifiers[0] is Barline)
                left -= beginModifiers[0].GetWidth();

            if (hasPaddingLeft) left = stave.GetX() + RenderOptions.PaddingLeft!.Value;
            if (hasPaddingRight) right = stave.GetX() + stave.GetWidth() - RenderOptions.PaddingRight!.Value;

            xs = (left, right);

            if (RenderOptions.UseSymbols)
                DrawSymbols(stave, ctx, left, right);
            else
                DrawLine(stave, ctx, left, right);

            DrawNumber(stave, ctx, left, right);
        }
    }
}

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public enum TextJustification
    {
        Left = 1,
        Center = 2,
        Right = 3,
    }

    public class StaveTextOptions
    {
        public double? ShiftX { get; set; }
        public double? ShiftY { get; set; }
        public TextJustification? Justification { get; set; }
    }

    /// <summary>
    /// Text attached to a stave edge or above/below the stave.
    /// Port of VexFlow's StaveText class from stavetext.ts.
    /// </summary>
    public class StaveText : StaveModifier
    {
        public new const string CATEGORY = "StaveText";

        public override string GetCategory() => CATEGORY;

        private string text;
        private double xShift;
        private double yShift;
        private TextJustification justification;

        public StaveText(string text, StaveModifierPosition position, StaveTextOptions options = null)
        {
            options ??= new StaveTextOptions();
            this.text = text;
            this.position = position;
            xShift = options.ShiftX ?? 0;
            yShift = options.ShiftY ?? 0;
            justification = options.Justification ?? TextJustification.Center;
            UpdateMetrics();
        }

        public string GetText() => text;
        public double GetXShift() => xShift;
        public double GetYShift() => yShift;
        public TextJustification GetJustification() => justification;

        public StaveText SetText(string newText)
        {
            text = newText;
            UpdateMetrics();
            return this;
        }

        public StaveText SetXShift(double shift)
        {
            xShift = shift;
            return this;
        }

        public StaveText SetYShift(double shift)
        {
            yShift = shift;
            return this;
        }

        public StaveText SetJustification(TextJustification newJustification)
        {
            justification = newJustification;
            return this;
        }

        private void UpdateMetrics()
        {
            var font = Metrics.GetFontInfo("StaveText");
            width = TextFormatter.Create(font.Family, font.Size).GetWidthForTextInPx(text);
        }

        public override void Draw(Stave stave, double xShift)
        {
            var ctx = stave.CheckContext();
            SetContext(ctx);
            SetStave(stave);
            rendered = true;

            double drawX;
            double drawY;

            switch (position)
            {
                case StaveModifierPosition.Left:
                    drawY = (stave.GetYForLine(0) + stave.GetYForLine(stave.GetNumLines() - 1)) / 2;
                    drawX = stave.GetX() - width - 24;
                    break;
                case StaveModifierPosition.Right:
                    drawY = (stave.GetYForLine(0) + stave.GetYForLine(stave.GetNumLines() - 1)) / 2;
                    drawX = stave.GetX() + stave.GetWidth() + 24;
                    break;
                case StaveModifierPosition.Above:
                case StaveModifierPosition.Below:
                    drawX = stave.GetX();
                    if (justification == TextJustification.Center)
                        drawX += stave.GetWidth() / 2 - width / 2;
                    else if (justification == TextJustification.Right)
                        drawX += stave.GetWidth() - width;

                    drawY = position == StaveModifierPosition.Above
                        ? stave.GetYForTopText(2)
                        : stave.GetYForBottomText(2);
                    break;
                default:
                    throw new VexFlowException("InvalidPosition", "Value must be Left, Right, Above, or Below.");
            }

            var font = Metrics.GetFontInfo("StaveText");
            ctx.Save();
            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
            ctx.FillText(text, drawX + this.xShift + xShift, drawY + this.yShift + 4);
            ctx.Restore();
        }
    }
}

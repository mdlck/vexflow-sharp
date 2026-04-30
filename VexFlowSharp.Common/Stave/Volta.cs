// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public enum VoltaType
    {
        None = 1,
        Begin = 2,
        Mid = 3,
        End = 4,
        BeginEnd = 5,
    }

    /// <summary>
    /// Volta bracket stave modifier.
    /// Port of VexFlow's Volta class from stavevolta.ts.
    /// </summary>
    public class Volta : StaveModifier
    {
        public new const string CATEGORY = "Volta";

        public override string GetCategory() => CATEGORY;

        private readonly VoltaType type;
        private readonly string label;
        private readonly double yShift;

        public Volta(VoltaType type, string label, double x = 0, double yShift = 0)
        {
            this.type = type;
            this.label = label;
            this.x = x;
            this.yShift = yShift;
            position = StaveModifierPosition.Above;
            UpdateMetrics();
        }

        public VoltaType GetVoltaType() => type;
        public string GetText() => label;
        public double GetYShift() => yShift;

        private void UpdateMetrics()
        {
            var font = Metrics.GetFontInfo("Volta");
            width = TextFormatter.Create(font.Family, font.Size).GetWidthForTextInPx(label);
        }

        public override void Draw(Stave stave, double xShift)
        {
            var ctx = stave.CheckContext();
            SetContext(ctx);
            SetStave(stave);
            rendered = true;

            double modifierXShift = xShift;
            double bracketWidth = stave.GetWidth() - modifierXShift;
            double topY = stave.GetYForTopText(stave.GetNumLines()) + yShift;
            double lineWidth = Metrics.GetDouble("Volta.lineWidth");
            double vertHeight = Metrics.GetDouble("Volta.verticalHeightLines") * stave.GetSpacingBetweenLines();
            double startX = x + modifierXShift;

            switch (type)
            {
                case VoltaType.Begin:
                    ctx.FillRect(startX, topY, lineWidth, vertHeight);
                    break;
                case VoltaType.End:
                    bracketWidth -= Metrics.GetDouble("Volta.endAdjustment");
                    ctx.FillRect(startX + bracketWidth, topY, lineWidth, vertHeight);
                    break;
                case VoltaType.BeginEnd:
                    bracketWidth -= Metrics.GetDouble("Volta.beginEndAdjustment");
                    ctx.FillRect(startX, topY, lineWidth, vertHeight);
                    ctx.FillRect(startX + bracketWidth, topY, lineWidth, vertHeight);
                    break;
            }

            if (type == VoltaType.Begin || type == VoltaType.BeginEnd)
            {
                var font = Metrics.GetFontInfo("Volta");
                ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
                ctx.FillText(
                    label,
                    x + modifierXShift + Metrics.GetDouble("Volta.textXOffset"),
                    topY - yShift + Metrics.GetDouble("Volta.textYOffset"));
            }

            ctx.FillRect(startX, topY, bracketWidth, lineWidth);
        }
    }
}

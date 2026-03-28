// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Barline types.
    /// Port of VexFlow's BarlineType enum from stavebarline.ts.
    /// Note: VexFlow uses NONE=7; we mirror that value exactly.
    /// </summary>
    public enum BarlineType
    {
        Single      = 1,
        Double      = 2,
        End         = 3,
        RepeatBegin = 4,
        RepeatEnd   = 5,
        RepeatBoth  = 6,
        None        = 7,
    }

    /// <summary>
    /// Renders a barline (vertical bar) on a stave.
    /// Port of VexFlow's Barline class from stavebarline.ts.
    /// </summary>
    public class Barline : StaveModifier
    {
        private static readonly Dictionary<BarlineType, double> _widths;
        private static readonly Dictionary<BarlineType, double> _paddings;
        private static readonly Dictionary<BarlineType, LayoutMetrics> _layoutMetricsMap;

        private BarlineType type;

        static Barline()
        {
            _widths = new Dictionary<BarlineType, double>
            {
                { BarlineType.Single,      5 },
                { BarlineType.Double,      5 },
                { BarlineType.End,         5 },
                { BarlineType.RepeatBegin, 5 },
                { BarlineType.RepeatEnd,   5 },
                { BarlineType.RepeatBoth,  5 },
                { BarlineType.None,        5 },
            };

            _paddings = new Dictionary<BarlineType, double>
            {
                { BarlineType.Single,       0 },
                { BarlineType.Double,       0 },
                { BarlineType.End,          0 },
                { BarlineType.RepeatBegin, 15 },
                { BarlineType.RepeatEnd,   15 },
                { BarlineType.RepeatBoth,  15 },
                { BarlineType.None,         0 },
            };

            _layoutMetricsMap = new Dictionary<BarlineType, LayoutMetrics>
            {
                { BarlineType.Single,      new LayoutMetrics { XMin =   0, XMax =  1, PaddingLeft = 5, PaddingRight = 5 } },
                { BarlineType.Double,      new LayoutMetrics { XMin =  -3, XMax =  1, PaddingLeft = 5, PaddingRight = 5 } },
                { BarlineType.End,         new LayoutMetrics { XMin =  -5, XMax =  1, PaddingLeft = 5, PaddingRight = 5 } },
                { BarlineType.RepeatEnd,   new LayoutMetrics { XMin = -10, XMax =  1, PaddingLeft = 5, PaddingRight = 5 } },
                { BarlineType.RepeatBegin, new LayoutMetrics { XMin =  -2, XMax = 10, PaddingLeft = 5, PaddingRight = 5 } },
                { BarlineType.RepeatBoth,  new LayoutMetrics { XMin = -10, XMax = 10, PaddingLeft = 5, PaddingRight = 5 } },
                { BarlineType.None,        new LayoutMetrics { XMin =   0, XMax =  0, PaddingLeft = 5, PaddingRight = 5 } },
            };
        }

        /// <summary>Create a barline of the given type.</summary>
        public Barline(BarlineType barlineType, double xPos = 0)
        {
            SetPosition(StaveModifierPosition.Begin);
            x = xPos;
            SetType(barlineType);
        }

        public BarlineType GetBarlineType() => type;

        /// <summary>Set barline type, updating width, padding, and layout metrics.</summary>
        public Barline SetType(BarlineType barlineType)
        {
            type = barlineType;
            SetWidth(_widths[type]);
            SetPadding(_paddings[type]);
            if (_layoutMetricsMap.TryGetValue(type, out var lm))
                SetLayoutMetrics(lm);
            return this;
        }

        /// <summary>Get width. For None type, returns 0 (no visual space).</summary>
        public override double GetWidth()
        {
            if (type == BarlineType.None) return 0;
            return width;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public override void Draw(Stave stave, double xShift)
        {
            var ctx = stave.CheckContext();
            SetContext(ctx);

            switch (type)
            {
                case BarlineType.Single:
                    DrawVerticalBar(stave, x, false);
                    break;

                case BarlineType.Double:
                    DrawVerticalBar(stave, x, true);
                    break;

                case BarlineType.End:
                    DrawVerticalEndBar(stave, x);
                    break;

                case BarlineType.RepeatBegin:
                    DrawRepeatBar(stave, x, begin: true);
                    if (stave.GetX() != x)
                        DrawVerticalBar(stave, stave.GetX(), false);
                    break;

                case BarlineType.RepeatEnd:
                    DrawRepeatBar(stave, x, begin: false);
                    break;

                case BarlineType.RepeatBoth:
                    DrawRepeatBar(stave, x, begin: false);
                    DrawRepeatBar(stave, x, begin: true);
                    break;

                case BarlineType.None:
                default:
                    // Nothing to draw
                    break;
            }
        }

        // ── Private draw helpers ──────────────────────────────────────────────

        private void DrawVerticalBar(Stave stave, double barX, bool doubleBar)
        {
            var ctx  = stave.CheckContext();
            double topY = stave.GetTopLineTopY();
            double botY = stave.GetBottomLineBottomY();
            if (doubleBar)
                ctx.FillRect(barX - 3, topY, 1, botY - topY);
            ctx.FillRect(barX, topY, 1, botY - topY);
        }

        private void DrawVerticalEndBar(Stave stave, double barX)
        {
            var ctx  = stave.CheckContext();
            double topY = stave.GetTopLineTopY();
            double botY = stave.GetBottomLineBottomY();
            ctx.FillRect(barX - 5, topY, 1, botY - topY);
            ctx.FillRect(barX - 2, topY, 3, botY - topY);
        }

        private void DrawRepeatBar(Stave stave, double barX, bool begin)
        {
            var ctx  = stave.CheckContext();
            double topY = stave.GetTopLineTopY();
            double botY = stave.GetBottomLineBottomY();

            double xShift = begin ? 3 : -5;

            ctx.FillRect(barX + xShift, topY, 1, botY - topY);
            ctx.FillRect(barX - 2, topY, 3, botY - topY);

            double dotRadius = 2;
            if (begin) xShift += 4;
            else       xShift -= 4;

            double dotX  = barX + xShift + dotRadius / 2;
            double yOff  = (stave.GetNumLines() - 1) * stave.GetSpacingBetweenLines();
            yOff = yOff / 2 - stave.GetSpacingBetweenLines() / 2;
            double dotY  = topY + yOff + dotRadius / 2;

            ctx.BeginPath();
            ctx.Arc(dotX, dotY, dotRadius, 0, System.Math.PI * 2, false);
            ctx.Fill();

            dotY += stave.GetSpacingBetweenLines();
            ctx.BeginPath();
            ctx.Arc(dotX, dotY, dotRadius, 0, System.Math.PI * 2, false);
            ctx.Fill();
        }
    }
}

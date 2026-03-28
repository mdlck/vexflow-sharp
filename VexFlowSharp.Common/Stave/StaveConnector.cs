// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's StaveConnector class (staveconnector.ts, 316 lines).
// StaveConnector renders visual connectors (brace, bracket, barline) between
// staves in a multi-stave system.

using System;
using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Connector type enum for StaveConnector.
    /// Port of VexFlow's StaveConnector.type static record from staveconnector.ts.
    ///
    /// SINGLE_LEFT and SINGLE are aliases (same int value = 1) for compatibility
    /// with older versions of VexFlow that did not have right-sided connectors.
    /// C# allows multiple enum names to share the same underlying value.
    /// </summary>
    public enum StaveConnectorType
    {
        SingleRight    = 0,
        SingleLeft     = 1,
        Single         = 1,   // alias for SingleLeft
        Double         = 2,
        Brace          = 3,
        Bracket        = 4,
        BoldDoubleLeft  = 5,
        BoldDoubleRight = 6,
        ThinDouble     = 7,
        None           = 8,
    }

    /// <summary>
    /// Renders connector lines between staves of a system.
    /// Supports brace, bracket, single barline, double barline, bold double, and thin double connectors.
    ///
    /// Port of VexFlow's StaveConnector class from staveconnector.ts.
    /// </summary>
    public class StaveConnector : Element
    {
        // ── Static type string mapping ────────────────────────────────────────

        private static readonly Dictionary<string, StaveConnectorType> TypeStringMap =
            new Dictionary<string, StaveConnectorType>(StringComparer.OrdinalIgnoreCase)
            {
                { "singleRight",    StaveConnectorType.SingleRight    },
                { "singleLeft",     StaveConnectorType.SingleLeft     },
                { "single",         StaveConnectorType.Single         },
                { "double",         StaveConnectorType.Double         },
                { "brace",          StaveConnectorType.Brace          },
                { "bracket",        StaveConnectorType.Bracket        },
                { "boldDoubleLeft",  StaveConnectorType.BoldDoubleLeft  },
                { "boldDoubleRight", StaveConnectorType.BoldDoubleRight },
                { "thinDouble",     StaveConnectorType.ThinDouble     },
                { "none",           StaveConnectorType.None           },
            };

        // ── Fields ────────────────────────────────────────────────────────────

        private readonly Stave topStave;
        private readonly Stave bottomStave;
        private readonly double thickness;

        private StaveConnectorType type;
        private double xShift = 0;
        private double width  = 3;

        private readonly List<(string Content, double ShiftX, double ShiftY)> texts =
            new List<(string, double, double)>();

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a StaveConnector between two staves.
        /// Default type is Double (matching VexFlow's constructor default).
        /// </summary>
        public StaveConnector(Stave topStave, Stave bottomStave)
        {
            this.topStave    = topStave;
            this.bottomStave = bottomStave;
            this.type        = StaveConnectorType.Double;
            this.thickness   = Tables.STAVE_LINE_THICKNESS;
            this.xShift      = 0;
        }

        // ── Configuration ─────────────────────────────────────────────────────

        /// <summary>Set the connector type by enum value.</summary>
        public StaveConnector SetType(StaveConnectorType connectorType)
        {
            type = connectorType;
            return this;
        }

        /// <summary>
        /// Set the connector type by string name.
        /// Accepted values: "singleRight", "singleLeft", "single", "double", "brace",
        /// "bracket", "boldDoubleLeft", "boldDoubleRight", "thinDouble", "none".
        /// </summary>
        public StaveConnector SetType(string typeStr)
        {
            if (TypeStringMap.TryGetValue(typeStr, out var resolved))
                type = resolved;
            else
                throw new VexFlowException("InvalidConnector", $"Unknown StaveConnector type string: '{typeStr}'");
            return this;
        }

        /// <summary>Get the connector type.</summary>
        public StaveConnectorType GetConnectorType() => type;

        /// <summary>Set x shift for connector positioning.</summary>
        public StaveConnector SetXShift(double shift)
        {
            xShift = shift;
            return this;
        }

        /// <summary>Get the x shift.</summary>
        public double GetXShift() => xShift;

        /// <summary>Add a text label to this connector.</summary>
        public StaveConnector SetText(string text, double shiftX = 0, double shiftY = 0)
        {
            texts.Add((text, shiftX, shiftY));
            return this;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Render the connector and any associated text labels.
        /// Port of VexFlow's StaveConnector.draw() from staveconnector.ts lines 194-315.
        /// </summary>
        public override void Draw()
        {
            var ctx = CheckContext();

            double topY = topStave.GetYForLine(0);
            double botY = bottomStave.GetYForLine(bottomStave.GetNumLines() - 1) + thickness;
            double connectorWidth = width;
            double topX = topStave.GetX();

            // Right-sided connector types use the right edge of the stave
            bool isRightSided =
                type == StaveConnectorType.SingleRight ||
                type == StaveConnectorType.BoldDoubleRight ||
                type == StaveConnectorType.ThinDouble;

            if (isRightSided)
                topX = topStave.GetX() + topStave.GetWidth();

            double attachmentHeight = botY - topY;

            switch (type)
            {
                case StaveConnectorType.Single:     // same as SingleLeft (value = 1)
                case StaveConnectorType.SingleRight:
                    connectorWidth = 1;
                    break;

                case StaveConnectorType.Double:
                    topX -= width + 2;
                    topY -= thickness;
                    attachmentHeight += 0.5;
                    break;

                case StaveConnectorType.Brace:
                {
                    connectorWidth = 12;
                    double x1 = topStave.GetX() - 2 + xShift;
                    double y1 = topY;
                    double x3 = x1;
                    double y3 = botY;
                    double x2 = x1 - connectorWidth;
                    double y2 = y1 + attachmentHeight / 2.0;

                    double cpx1 = x2 - 0.9 * connectorWidth;
                    double cpy1 = y1 + 0.2 * attachmentHeight;
                    double cpx2 = x1 + 1.1 * connectorWidth;
                    double cpy2 = y2 - 0.135 * attachmentHeight;
                    double cpx3 = cpx2;
                    double cpy3 = y2 + 0.135 * attachmentHeight;
                    double cpx4 = cpx1;
                    double cpy4 = y3 - 0.2 * attachmentHeight;
                    double cpx5 = x2 - connectorWidth;
                    double cpy5 = cpy4;
                    double cpx6 = x1 + 0.4 * connectorWidth;
                    double cpy6 = y2 + 0.135 * attachmentHeight;
                    double cpx7 = cpx6;
                    double cpy7 = y2 - 0.135 * attachmentHeight;
                    double cpx8 = cpx5;
                    double cpy8 = cpy1;

                    ctx.BeginPath();
                    ctx.MoveTo(x1, y1);
                    ctx.BezierCurveTo(cpx1, cpy1, cpx2, cpy2, x2, y2);
                    ctx.BezierCurveTo(cpx3, cpy3, cpx4, cpy4, x3, y3);
                    ctx.BezierCurveTo(cpx5, cpy5, cpx6, cpy6, x2, y2);
                    ctx.BezierCurveTo(cpx7, cpy7, cpx8, cpy8, x1, y1);
                    ctx.Fill();
                    ctx.Stroke();
                    break;
                }

                case StaveConnectorType.Bracket:
                {
                    topY -= 6;
                    botY += 6;
                    attachmentHeight = botY - topY;
                    // Render bracket top and bottom glyphs
                    new Glyph("bracketTop",    40).Render(ctx, topX - 5, topY);
                    new Glyph("bracketBottom", 40).Render(ctx, topX - 5, botY);
                    topX -= width + 2;
                    break;
                }

                case StaveConnectorType.BoldDoubleLeft:
                    DrawBoldDoubleLine(ctx, type, topX + xShift, topY, botY - thickness);
                    break;

                case StaveConnectorType.BoldDoubleRight:
                    DrawBoldDoubleLine(ctx, type, topX, topY, botY - thickness);
                    break;

                case StaveConnectorType.ThinDouble:
                    connectorWidth = 1;
                    attachmentHeight -= thickness;
                    break;

                case StaveConnectorType.None:
                    // no-op
                    break;

                default:
                    throw new VexFlowException("InvalidType",
                        $"The provided StaveConnectorType ({type}) is invalid.");
            }

            // Draw the main fill rect for all types except Brace, BoldDouble*, and None
            if (type != StaveConnectorType.Brace      &&
                type != StaveConnectorType.BoldDoubleLeft  &&
                type != StaveConnectorType.BoldDoubleRight &&
                type != StaveConnectorType.None)
            {
                ctx.FillRect(topX, topY, connectorWidth, attachmentHeight);
            }

            // ThinDouble draws a parallel line 3px to the left
            if (type == StaveConnectorType.ThinDouble)
                ctx.FillRect(topX - 3, topY, connectorWidth, attachmentHeight);

            // Draw text labels
            ctx.Save();
            ctx.SetLineWidth(2);

            foreach (var (content, shiftX, shiftY) in texts)
            {
                var measure  = ctx.MeasureText(content);
                double textW = measure.Width;
                double tx    = topStave.GetX() - textW - 24 + shiftX;
                double ty    = (topStave.GetYForLine(0) +
                                bottomStave.GetYForLine(bottomStave.GetNumLines() - 1)) / 2.0
                               + shiftY;
                ctx.FillText(content, tx, ty + 4);
            }

            ctx.Restore();

            rendered = true;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Draw the bold double line (thin + thick) for BoldDoubleLeft/Right connector types.
        /// Port of VexFlow's drawBoldDoubleLine() helper from staveconnector.ts lines 13-31.
        /// </summary>
        private static void DrawBoldDoubleLine(
            RenderContext ctx,
            StaveConnectorType connectorType,
            double topX,
            double topY,
            double botY)
        {
            if (connectorType != StaveConnectorType.BoldDoubleLeft &&
                connectorType != StaveConnectorType.BoldDoubleRight)
            {
                throw new VexFlowException("InvalidConnector",
                    "A BoldDoubleLeft or BoldDoubleRight type must be provided.");
            }

            double xShiftLocal     = 3;
            double variableWidth   = 3.5; // width avoids anti-aliasing issues
            const double thickLineOffset = 2; // aesthetic offset

            if (connectorType == StaveConnectorType.BoldDoubleRight)
            {
                xShiftLocal   = -5;   // flips the thin line to the right side
                variableWidth = 3;
            }

            double height = botY - topY;

            // Thin line
            ctx.FillRect(topX + xShiftLocal, topY, 1, height);
            // Thick line
            ctx.FillRect(topX - thickLineOffset, topY, variableWidth, height);
        }
    }
}

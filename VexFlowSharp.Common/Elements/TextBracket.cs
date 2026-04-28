#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public enum TextBracketPosition
    {
        Top = 1,
        Bottom = -1,
    }

    public class TextBracketParams
    {
        public Note Start { get; set; } = null!;
        public Note Stop { get; set; } = null!;
        public string Text { get; set; } = string.Empty;
        public string Superscript { get; set; } = string.Empty;
        public TextBracketPosition Position { get; set; } = TextBracketPosition.Top;
    }

    public class TextBracketRenderOptions
    {
        public bool Dashed { get; set; } = true;
        public string Color { get; set; } = "black";
        public bool UnderlineSuperscript { get; set; } = true;
        public bool ShowBracket { get; set; } = true;
        public double[] Dash { get; set; } = new[] { 5.0 };
        public double LineWidth { get; set; } = Metrics.GetDouble("TextBracket.lineWidth");
        public double BracketHeight { get; set; } = Metrics.GetDouble("TextBracket.bracketHeight");
    }

    /// <summary>
    /// Text bracket spanning two notes, for octave transposition markings such as 8va.
    /// Port of VexFlow's TextBracket class from textbracket.ts.
    /// </summary>
    public class TextBracket : Element
    {
        public new const string CATEGORY = "TextBracket";

        private readonly Note start;
        private readonly Note stop;
        private readonly string text;
        private readonly string superscript;
        private readonly TextBracketPosition position;
        private double line = 1;

        public TextBracketRenderOptions RenderOptions { get; } = new TextBracketRenderOptions();

        public TextBracket(TextBracketParams parameters)
        {
            start = parameters.Start ?? throw new ArgumentNullException(nameof(parameters.Start));
            stop = parameters.Stop ?? throw new ArgumentNullException(nameof(parameters.Stop));
            text = parameters.Text;
            superscript = parameters.Superscript;
            position = parameters.Position;
        }

        public Note GetStart() => start;
        public Note GetStop() => stop;
        public string GetText() => text;
        public string GetSuperscript() => superscript;
        public TextBracketPosition GetPosition() => position;
        public double GetLine() => line;

        public TextBracket SetDashed(bool dashed, double[]? dash = null)
        {
            RenderOptions.Dashed = dashed;
            if (dash != null) RenderOptions.Dash = dash;
            return this;
        }

        public TextBracket SetLine(double newLine)
        {
            line = newLine;
            return this;
        }

        public override string GetCategory() => CATEGORY;

        private static double GetTextWidth(string value, double fontSize)
        {
            var font = Metrics.GetFontInfo("TextBracket");
            return TextFormatter.Create(font.Family, fontSize).GetWidthForTextInPx(value);
        }

        private static void DrawText(RenderContext ctx, string value, double x, double y, double fontSize)
        {
            var font = Metrics.GetFontInfo("TextBracket");
            ctx.SetFont(font.Family, fontSize, font.Weight, font.Style);
            ctx.FillText(value, x, y);
        }

        private static void DrawLine(RenderContext ctx, double fromX, double fromY, double toX, double toY, double[] dash)
        {
            ctx.SetLineDash(dash);
            ctx.BeginPath();
            ctx.MoveTo(fromX, fromY);
            ctx.LineTo(toX, toY);
            ctx.Stroke();
            ctx.SetLineDash(Array.Empty<double>());
        }

        public override void Draw()
        {
            var ctx = CheckContext();
            rendered = true;

            double y;
            switch (position)
            {
                case TextBracketPosition.Top:
                    y = start.CheckStave().GetYForTopText(line);
                    break;
                case TextBracketPosition.Bottom:
                    y = start.CheckStave().GetYForBottomText(line + Metrics.GetDouble("TextBracket.textHeightOffsetHack"));
                    break;
                default:
                    throw new VexFlowException("InvalidPosition", $"The position {position} is invalid.");
            }

            double startX = start.GetAbsoluteX();
            double endX = stop.GetAbsoluteX() + stop.GetWidth();
            double bracketHeight = RenderOptions.BracketHeight * (int)position;

            var font = Metrics.GetFontInfo("TextBracket");
            double mainFontSize = font.Size;
            double superFontSize = mainFontSize * 0.714286;
            double mainWidth = GetTextWidth(text, mainFontSize);
            double mainHeight = mainFontSize;
            double superY = y - mainHeight / 2.5;
            double superWidth = GetTextWidth(superscript, superFontSize);
            double superHeight = superFontSize;

            ctx.Save();
            ctx.SetStrokeStyle(RenderOptions.Color);
            ctx.SetFillStyle(RenderOptions.Color);
            ctx.SetLineWidth(RenderOptions.LineWidth);

            DrawText(ctx, text, startX, y, mainFontSize);
            DrawText(ctx, superscript, startX + mainWidth + 1, superY, superFontSize);

            double lineStartX = startX;
            double lineY = superY;
            if (position == TextBracketPosition.Top)
            {
                lineStartX += mainWidth + superWidth + 5;
                lineY -= superHeight / 2.7;
            }
            else
            {
                lineY += superHeight / 2.7;
                lineStartX += mainWidth + 2;
                if (!RenderOptions.UnderlineSuperscript)
                    lineStartX += superWidth;
            }

            if (RenderOptions.Dashed)
            {
                DrawLine(ctx, lineStartX, lineY, endX, lineY, RenderOptions.Dash);
                if (RenderOptions.ShowBracket)
                {
                    DrawLine(ctx, endX, lineY + (int)position, endX, lineY + bracketHeight, RenderOptions.Dash);
                }
            }
            else
            {
                ctx.BeginPath();
                ctx.MoveTo(lineStartX, lineY);
                ctx.LineTo(endX, lineY);
                if (RenderOptions.ShowBracket)
                    ctx.LineTo(endX, lineY + bracketHeight);
                ctx.Stroke();
                ctx.ClosePath();
            }

            ctx.Restore();
        }
    }
}

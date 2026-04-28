#nullable enable annotations

using System;
using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public class BendPhrase
    {
        public double? X { get; set; }
        public int Type { get; set; }
        public string Text { get; set; } = string.Empty;
        public double? Width { get; set; }
        public double? DrawWidth { get; set; }
    }

    public class BendRenderOptions
    {
        public double ReleaseWidth { get; set; } = 8;
        public double BendWidth { get; set; } = 8;
    }

    /// <summary>Tablature bend modifier, ported from VexFlow 5 bend.ts.</summary>
    public class Bend : Modifier
    {
        public new const string CATEGORY = "Bend";
        public const int UP = 0;
        public const int DOWN = 1;

        public override string GetCategory() => CATEGORY;

        private readonly List<BendPhrase> phrase;
        private string tap = string.Empty;
        private ElementStyle styleLine = Metrics.GetStyle("Bend.line");

        public BendRenderOptions RenderOptions { get; } = new BendRenderOptions();

        public Bend(List<BendPhrase> phrase)
        {
            this.phrase = phrase;
            position = ModifierPosition.Right;
            UpdateWidth();
        }

        public static bool Format(List<Bend> bends, ModifierContextState state)
        {
            if (bends == null || bends.Count == 0) return false;

            double lastWidth = 0;
            foreach (var bend in bends)
            {
                var note = bend.GetNote() as Note;
                if (note is TabNote tabNote)
                {
                    int stringPos = tabNote.LeastString() - 1;
                    if (state.TopTextLine < stringPos)
                        state.TopTextLine = stringPos;
                }

                bend.SetXShift(lastWidth);
                lastWidth = bend.GetWidth();
                bend.SetTextLine(state.TopTextLine);
            }

            state.RightShift += lastWidth;
            state.TopTextLine += 1;
            return true;
        }

        public new Bend SetXShift(double value)
        {
            xShift = value;
            UpdateWidth();
            return this;
        }

        public Bend SetTap(string value)
        {
            tap = value;
            return this;
        }

        public Bend SetStyleLine(ElementStyle style)
        {
            styleLine = style;
            return this;
        }

        public ElementStyle GetStyleLine() => styleLine;

        public IReadOnlyList<BendPhrase> GetPhrase() => phrase;

        public double GetTextHeight() => Metrics.GetDouble("Bend.fontSize") * Metrics.GetDouble("TextFormatter.ptToPx");

        private Bend UpdateWidth()
        {
            double totalWidth = 0;
            foreach (var bend in phrase)
            {
                if (bend.Width.HasValue)
                {
                    totalWidth += bend.Width.Value;
                    continue;
                }

                double additionalWidth = bend.Type == UP ? RenderOptions.BendWidth : RenderOptions.ReleaseWidth;
                double measured = MeasureText(bend.Text);
                bend.Width = Math.Max(additionalWidth, measured) + 3;
                bend.DrawWidth = bend.Width.Value / 2;
                totalWidth += bend.Width.Value;
            }

            SetWidth(totalWidth + xShift);
            return this;
        }

        public override void Draw()
        {
            var ctx = CheckContext();
            var note = (Note)GetNote();
            rendered = true;

            var start = note.GetModifierStartXY(ModifierPosition.Right, GetIndex() ?? 0);
            double startX = start.X + 3;
            double startY = start.Y + 0.5;

            var stave = note.CheckStave();
            double spacing = stave.GetSpacingBetweenLines();
            double lowestY = Max(note.GetYs());
            double bendHeight = startY - ((textLine + 1) * spacing + startY - lowestY) + 3;
            double annotationY = startY - ((textLine + 1) * spacing + startY - lowestY) - 1;

            void RenderBend(double x, double y, double drawWidth, double height)
            {
                ApplyLineStyle(ctx);
                ctx.BeginPath();
                ctx.MoveTo(x, y);
                ctx.QuadraticCurveTo(x + drawWidth, y, x + drawWidth, height);
                ctx.Stroke();
                ctx.Restore();
            }

            void RenderRelease(double x, double y, double drawWidth, double height)
            {
                ApplyLineStyle(ctx);
                ctx.BeginPath();
                ctx.MoveTo(x, height);
                ctx.QuadraticCurveTo(x + drawWidth, height, x + drawWidth, y);
                ctx.Stroke();
                ctx.Restore();
            }

            void RenderArrowHead(double x, double y, int direction)
            {
                const double arrowWidth = 4;
                double yBase = y + arrowWidth * direction;
                ctx.BeginPath();
                ctx.MoveTo(x, y);
                ctx.LineTo(x - arrowWidth, yBase);
                ctx.LineTo(x + arrowWidth, yBase);
                ctx.ClosePath();
                ctx.Fill();
            }

            void RenderText(double x, string text)
            {
                var font = Metrics.GetFontInfo("Bend");
                ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
                double renderX = x - MeasureText(text) / 2;
                ctx.FillText(text, renderX, annotationY);
            }

            BendPhrase? lastBend = null;
            double lastBendDrawWidth = 0;
            double lastDrawnWidth = 0;
            if (!string.IsNullOrEmpty(tap))
            {
                var tapStart = note.GetModifierStartXY(ModifierPosition.Center, GetIndex() ?? 0);
                RenderText(tapStart.X, tap);
            }

            foreach (var bend in phrase)
            {
                bend.DrawWidth ??= 0;
                if (lastBend == null)
                    bend.DrawWidth += xShift;

                lastDrawnWidth = bend.DrawWidth.Value + lastBendDrawWidth - (lastBend != null && phrase.IndexOf(bend) == 1 ? xShift : 0);
                if (bend.Type == UP)
                {
                    if (lastBend != null && lastBend.Type == UP)
                        RenderArrowHead(startX, bendHeight, +1);

                    RenderBend(startX, startY, lastDrawnWidth, bendHeight);
                }

                if (bend.Type == DOWN)
                {
                    if (lastBend != null && lastBend.Type == UP)
                        RenderRelease(startX, startY, lastDrawnWidth, bendHeight);

                    if (lastBend != null && lastBend.Type == DOWN)
                    {
                        RenderArrowHead(startX, startY, -1);
                        RenderRelease(startX, startY, lastDrawnWidth, bendHeight);
                    }

                    if (lastBend == null)
                    {
                        lastDrawnWidth = bend.DrawWidth.Value;
                        RenderRelease(startX, startY, lastDrawnWidth, bendHeight);
                    }
                }

                RenderText(startX + lastDrawnWidth, bend.Text);
                lastBend = bend;
                lastBendDrawWidth = bend.DrawWidth.Value;
                bend.X = startX;
                startX += lastDrawnWidth;
            }

            if (lastBend == null || !lastBend.X.HasValue)
                throw new VexFlowException("NoLastBendForBend", "Internal error.");

            if (lastBend.Type == UP)
                RenderArrowHead(lastBend.X.Value + lastDrawnWidth, bendHeight, +1);
            else if (lastBend.Type == DOWN)
                RenderArrowHead(lastBend.X.Value + lastDrawnWidth, startY, -1);
        }

        private void ApplyLineStyle(RenderContext ctx)
        {
            ctx.Save();
            if (styleLine.StrokeStyle != null) ctx.SetStrokeStyle(styleLine.StrokeStyle);
            if (styleLine.LineWidth.HasValue) ctx.SetLineWidth(styleLine.LineWidth.Value);
        }

        private static double MeasureText(string text)
        {
            var font = Metrics.GetFontInfo("Bend");
            return TextFormatter.Create(font.Family, font.Size).GetWidthForTextInPx(text);
        }

        private static double Max(double[] values)
        {
            if (values.Length == 0) return 0;
            double max = values[0];
            foreach (var value in values)
                if (value > max) max = value;
            return max;
        }
    }
}

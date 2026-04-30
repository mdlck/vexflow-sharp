// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public class StaveTempoOptions
    {
        public string Name { get; set; }
        public bool Parenthesis { get; set; }
        public string Duration { get; set; }
        public int Dots { get; set; }
        public object Bpm { get; set; }
        public string Duration2 { get; set; }
        public int Dots2 { get; set; }
    }

    /// <summary>
    /// Tempo text and metronome marks attached above a stave.
    /// Port of VexFlow's StaveTempo class from stavetempo.ts.
    /// </summary>
    public class StaveTempo : StaveModifier
    {
        public new const string CATEGORY = "StaveTempo";

        private readonly Dictionary<string, string> durationToCode = new Dictionary<string, string>
        {
            ["1/4"] = "metNoteDoubleWholeSquare",
            ["long"] = "metNoteDoubleWholeSquare",
            ["1/2"] = "metNoteDoubleWhole",
            ["breve"] = "metNoteDoubleWhole",
            ["1"] = "metNoteWhole",
            ["whole"] = "metNoteWhole",
            ["w"] = "metNoteWhole",
            ["2"] = "metNoteHalfUp",
            ["half"] = "metNoteHalfUp",
            ["h"] = "metNoteHalfUp",
            ["4"] = "metNoteQuarterUp",
            ["quarter"] = "metNoteQuarterUp",
            ["q"] = "metNoteQuarterUp",
            ["8"] = "metNote8thUp",
            ["eighth"] = "metNote8thUp",
            ["16"] = "metNote16thUp",
            ["16th"] = "metNote16thUp",
            ["32"] = "metNote32ndUp",
            ["32nd"] = "metNote32ndUp",
            ["64"] = "metNote64thUp",
            ["64th"] = "metNote64thUp",
            ["128"] = "metNote128thUp",
            ["128th"] = "metNote128thUp",
            ["256"] = "metNote256thUp",
            ["256th"] = "metNote256thUp",
            ["512"] = "metNote512thUp",
            ["512th"] = "metNote512thUp",
            ["1024"] = "metNote1024thUp",
            ["1024th"] = "metNote1024thUp",
        };

        private StaveTempoOptions tempo;
        private double xShift = Metrics.GetDouble("StaveTempo.xShift");
        private double yShift;

        public StaveTempo(StaveTempoOptions tempo, double x = 0, double shiftY = 0)
        {
            this.tempo = tempo;
            this.x = x;
            yShift = shiftY;
            position = StaveModifierPosition.Above;
            UpdateWidth();
        }

        public StaveTempoOptions GetTempo() => tempo;
        public double GetXShift() => xShift;
        public double GetYShift() => yShift;

        public StaveTempo SetTempo(StaveTempoOptions newTempo)
        {
            tempo = newTempo;
            UpdateWidth();
            return this;
        }

        public StaveTempo SetXShift(double shift)
        {
            xShift = shift;
            return this;
        }

        public StaveTempo SetYShift(double shift)
        {
            yShift = shift;
            return this;
        }

        public override string GetCategory() => CATEGORY;

        private void UpdateWidth()
        {
            double spacing = Metrics.GetDouble("StaveTempo.spacing");
            width = 0;
            if (!string.IsNullOrEmpty(tempo.Name))
            {
                var nameFont = Metrics.GetFontInfo("StaveTempo.name");
                width += TextFormatter.Create(nameFont.Family, nameFont.Size).GetWidthForTextInPx(tempo.Name);
            }

            if (!string.IsNullOrEmpty(tempo.Duration))
            {
                width += Glyph.GetWidth(GetCodeForDuration(tempo.Duration), Metrics.GetDouble("StaveTempo.glyph.fontSize"));
                width += tempo.Dots * (Glyph.GetWidth("metAugmentationDot", Metrics.GetDouble("StaveTempo.glyph.fontSize")) + spacing);
                width += GetTextWidth("=");

                if (!string.IsNullOrEmpty(tempo.Duration2))
                {
                    width += Glyph.GetWidth(GetCodeForDuration(tempo.Duration2), Metrics.GetDouble("StaveTempo.glyph.fontSize"));
                    width += tempo.Dots2 * (Glyph.GetWidth("metAugmentationDot", Metrics.GetDouble("StaveTempo.glyph.fontSize")) + spacing);
                }
                else if (tempo.Bpm != null)
                {
                    width += GetTextWidth(tempo.Bpm.ToString() ?? string.Empty);
                }
            }
        }

        private string GetCodeForDuration(string duration)
        {
            if (durationToCode.TryGetValue(duration, out var code)) return code;
            throw new VexFlowException("InvalidDuration", $"No StaveTempo glyph for duration: {duration}");
        }

        private static double GetTextWidth(string text)
        {
            var font = Metrics.GetFontInfo("StaveTempo");
            return TextFormatter.Create(font.Family, font.Size).GetWidthForTextInPx(text);
        }

        private static void DrawText(RenderContext ctx, string text, double x, double y, string metricsPath)
        {
            var font = Metrics.GetFontInfo(metricsPath);
            ctx.Save();
            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
            ctx.FillText(text, x, y);
            ctx.Restore();
        }

        private static double DrawGlyph(RenderContext ctx, string code, double x, double y)
        {
            double fontSize = Metrics.GetDouble("StaveTempo.glyph.fontSize");
            var glyph = new Glyph(code, fontSize);
            glyph.Render(ctx, x, y);
            return Glyph.GetWidth(code, fontSize);
        }

        public override void Draw(Stave stave, double xShift)
        {
            var ctx = stave.CheckContext();
            SetContext(ctx);
            SetStave(stave);
            rendered = true;

            double drawX = x + xShift;
            double y = stave.GetYForTopText(1);
            double spacing = Metrics.GetDouble("StaveTempo.spacing");
            double dotOffsetY = Metrics.GetDouble("StaveTempo.dotOffsetY");

            if (!string.IsNullOrEmpty(tempo.Name))
            {
                DrawText(ctx, tempo.Name, drawX + this.xShift, y + yShift, "StaveTempo.name");
                var nameFont = Metrics.GetFontInfo("StaveTempo.name");
                drawX += TextFormatter.Create(nameFont.Family, nameFont.Size).GetWidthForTextInPx(tempo.Name) + spacing;
            }

            if ((!string.IsNullOrEmpty(tempo.Name) && !string.IsNullOrEmpty(tempo.Duration)) || tempo.Parenthesis)
            {
                DrawText(ctx, "(", drawX + this.xShift, y + yShift, "StaveTempo");
                drawX += GetTextWidth("(") + spacing;
            }

            if (string.IsNullOrEmpty(tempo.Duration)) return;

            drawX += DrawGlyph(ctx, GetCodeForDuration(tempo.Duration), drawX + this.xShift, y + yShift) + spacing;
            for (int i = 0; i < tempo.Dots; i++)
            {
                drawX += DrawGlyph(ctx, "metAugmentationDot", drawX + this.xShift, y + dotOffsetY + yShift) + spacing;
            }

            DrawText(ctx, "=", drawX + this.xShift, y + yShift, "StaveTempo");
            drawX += GetTextWidth("=") + spacing;

            if (!string.IsNullOrEmpty(tempo.Duration2))
            {
                drawX += DrawGlyph(ctx, GetCodeForDuration(tempo.Duration2), drawX + this.xShift, y + yShift) + spacing;
                for (int i = 0; i < tempo.Dots2; i++)
                {
                    drawX += DrawGlyph(ctx, "metAugmentationDot", drawX + this.xShift, y + dotOffsetY + yShift) + spacing;
                }
            }
            else if (tempo.Bpm != null)
            {
                var bpmText = tempo.Bpm.ToString() ?? string.Empty;
                DrawText(ctx, bpmText, drawX + this.xShift, y + yShift, "StaveTempo");
                drawX += GetTextWidth(bpmText) + spacing;
            }

            if (!string.IsNullOrEmpty(tempo.Name) || tempo.Parenthesis)
            {
                DrawText(ctx, ")", drawX + this.xShift, y + yShift, "StaveTempo");
            }
        }
    }
}

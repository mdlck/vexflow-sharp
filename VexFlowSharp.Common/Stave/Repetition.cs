#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public enum RepetitionType
    {
        None = 1,
        CodaLeft = 2,
        CodaRight = 3,
        SegnoLeft = 4,
        SegnoRight = 5,
        DC = 6,
        DCAlCoda = 7,
        DCAlFine = 8,
        DS = 9,
        DSAlCoda = 10,
        DSAlFine = 11,
        Fine = 12,
        ToCoda = 13,
    }

    /// <summary>
    /// Repetition marker stave modifier: coda, segno, D.C., D.S., Fine, To Coda.
    /// Port of VexFlow's Repetition class from staverepetition.ts.
    /// </summary>
    public class Repetition : StaveModifier
    {
        public new const string CATEGORY = "Repetition";

        public override string GetCategory() => CATEGORY;

        private readonly RepetitionType symbolType;
        private double xShift;
        private double yShift;

        public Repetition(RepetitionType type, double x = 0, double yShift = 0)
        {
            symbolType = type;
            this.x = x;
            xShift = 0;
            this.yShift = yShift;
            position = StaveModifierPosition.Above;
        }

        public RepetitionType GetRepetitionType() => symbolType;
        public double GetShiftX() => xShift;
        public double GetShiftY() => yShift;

        public Repetition SetShiftX(double shift)
        {
            xShift = shift;
            return this;
        }

        public Repetition SetShiftY(double shift)
        {
            yShift = shift;
            return this;
        }

        public override void Draw(Stave stave, double xShiftFromStave)
        {
            SetStave(stave);
            SetContext(stave.CheckContext());
            rendered = true;

            double shift = xShiftFromStave + xShift;

            switch (symbolType)
            {
                case RepetitionType.CodaRight:
                    DrawCodaFixed(stave, shift + stave.GetWidth());
                    break;
                case RepetitionType.CodaLeft:
                    DrawSymbolText(stave, shift, "Coda", true);
                    break;
                case RepetitionType.SegnoLeft:
                    DrawSegnoFixed(stave, shift);
                    break;
                case RepetitionType.SegnoRight:
                    DrawSegnoFixed(stave, shift + stave.GetWidth());
                    break;
                case RepetitionType.DC:
                    DrawSymbolText(stave, shift, "D.C.", false);
                    break;
                case RepetitionType.DCAlCoda:
                    DrawSymbolText(stave, shift, "D.C. al", true);
                    break;
                case RepetitionType.DCAlFine:
                    DrawSymbolText(stave, shift, "D.C. al Fine", false);
                    break;
                case RepetitionType.DS:
                    DrawSymbolText(stave, shift, "D.S.", false);
                    break;
                case RepetitionType.DSAlCoda:
                    DrawSymbolText(stave, shift, "D.S. al", true);
                    break;
                case RepetitionType.DSAlFine:
                    DrawSymbolText(stave, shift, "D.S. al Fine", false);
                    break;
                case RepetitionType.Fine:
                    DrawSymbolText(stave, shift, "Fine", false);
                    break;
                case RepetitionType.ToCoda:
                    DrawSymbolText(stave, shift, "To", true);
                    break;
            }
        }

        public Repetition DrawCodaFixed(Stave stave, double x)
        {
            var y = stave.GetYForTopText(stave.GetNumLines()) + Metrics.GetDouble("Repetition.coda.offsetY") + yShift;
            new Glyph("coda", Metrics.GetDouble("fontSize")).Render(stave.CheckContext(), x, y);
            return this;
        }

        public Repetition DrawSegnoFixed(Stave stave, double x)
        {
            var y = stave.GetYForTopText(stave.GetNumLines()) + Metrics.GetDouble("Repetition.segno.offsetY") + yShift;
            new Glyph("segno", Metrics.GetDouble("fontSize")).Render(stave.CheckContext(), x, y);
            return this;
        }

        public Repetition DrawSymbolText(Stave stave, double x, string text, bool drawCoda)
        {
            var ctx = stave.CheckContext();
            var font = Metrics.GetFontInfo("Repetition.text");
            double textWidth = TextFormatter.Create(font.Family, font.Size).GetWidthForTextInPx(text);
            double textX = 0;

            switch (symbolType)
            {
                case RepetitionType.CodaLeft:
                    textX = stave.GetVerticalBarWidth();
                    break;
                case RepetitionType.DC:
                case RepetitionType.DCAlFine:
                case RepetitionType.DS:
                case RepetitionType.DSAlFine:
                case RepetitionType.Fine:
                default:
                    textX = x - (stave.GetNoteStartX() - this.x) + stave.GetWidth() - textWidth - Metrics.GetDouble("Repetition.text.offsetX");
                    break;
            }

            double y = stave.GetYForTopText(stave.GetNumLines()) + Metrics.GetDouble("Repetition.text.offsetY") + yShift;
            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
            ctx.FillText(text, textX, y);

            if (drawCoda)
            {
                double codaX = textX + textWidth + Metrics.GetDouble("Repetition.text.spacing");
                new Glyph("coda", font.Size).Render(ctx, codaX, y);
            }

            return this;
        }
    }
}

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public class TextNoteStruct : NoteStruct
    {
        public string Text { get; set; } = "";
        public double? Line { get; set; }
        public TextJustification? Justification { get; set; }
        public MetricsFontInfo Font { get; set; }
    }

    /// <summary>
    /// Tickable text positioned on a stave, useful for lyrics, labels, and inline text markings.
    /// First-pass port of VexFlow 5 TextNote.
    /// </summary>
    public class TextNote : Note
    {
        public new const string CATEGORY = "TextNote";
        public override string GetCategory() => CATEGORY;

        private string text;
        private double line;
        private TextJustification justification;
        private MetricsFontInfo font;
        private bool preFormatted;

        public TextNote(TextNoteStruct noteStruct) : base(noteStruct)
        {
            text = noteStruct.Text;
            line = noteStruct.Line ?? 0;
            justification = noteStruct.Justification ?? TextJustification.Left;
            font = noteStruct.Font ?? Metrics.GetFontInfo("StaveText");
            UpdateWidth();
        }

        public string GetText() => text;
        public double GetLine() => line;
        public TextJustification GetJustification() => justification;
        public MetricsFontInfo GetFontInfo() => font;

        public TextNote SetText(string newText)
        {
            text = newText;
            UpdateWidth();
            return this;
        }

        public TextNote SetLine(double newLine)
        {
            line = newLine;
            return this;
        }

        public TextNote SetJustification(TextJustification newJustification)
        {
            justification = newJustification;
            return this;
        }

        public TextNote SetFont(MetricsFontInfo newFont)
        {
            font = newFont;
            UpdateWidth();
            return this;
        }

        public override void PreFormat()
        {
            if (preFormatted) return;
            UpdateWidth();
            preFormatted = true;
        }

        public override void Draw()
        {
            var ctx = CheckContext();
            var stave = CheckStave();
            rendered = true;

            double x = GetAbsoluteX();
            if (justification == TextJustification.Center)
                x -= GetWidth() / 2;
            else if (justification == TextJustification.Right)
                x -= GetWidth();

            double y = stave.GetYForLine(line);

            ctx.Save();
            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
            ctx.FillText(text, x, y);
            ctx.Restore();
        }

        private void UpdateWidth()
        {
            SetWidth(TextFormatter.Create(font.Family, font.Size).GetWidthForTextInPx(text));
        }
    }
}

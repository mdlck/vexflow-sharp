#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public enum PedalMarkingType
    {
        Text = 1,
        Bracket = 2,
        Mixed = 3,
    }

    public class PedalMarkingRenderOptions
    {
        public string Color { get; set; } = "black";
        public double BracketHeight { get; set; } = Metrics.GetDouble("PedalMarking.bracketHeight");
        public double TextMarginRight { get; set; } = Metrics.GetDouble("PedalMarking.textMarginRight");
        public double BracketLineWidth { get; set; } = Metrics.GetDouble("PedalMarking.bracketLineWidth");
    }

    /// <summary>
    /// Sustain pedal markings rendered as text, brackets, or mixed text/brackets.
    /// Port of VexFlow 5's PedalMarking class.
    /// </summary>
    public class PedalMarking : Element
    {
        public new const string CATEGORY = "PedalMarking";

        public const string PedalDepressGlyph = "keyboardPedalPed";
        public const string PedalReleaseGlyph = "keyboardPedalUp";

        private readonly List<StaveNote> notes;
        private PedalMarkingType type = PedalMarkingType.Text;
        private int line = 0;
        private string depressText = PedalDepressGlyph;
        private string releaseText = PedalReleaseGlyph;
        private MetricsFontInfo font = Metrics.GetFontInfo("PedalMarking");

        public PedalMarkingRenderOptions RenderOptions { get; } = new PedalMarkingRenderOptions();

        public PedalMarking(IEnumerable<StaveNote> notes)
        {
            this.notes = new List<StaveNote>(notes ?? throw new ArgumentNullException(nameof(notes)));
        }

        public static PedalMarking CreateSustain(IEnumerable<StaveNote> notes)
            => new PedalMarking(notes);

        public static PedalMarking CreateSostenuto(IEnumerable<StaveNote> notes)
        {
            return new PedalMarking(notes)
                .SetType(PedalMarkingType.Mixed)
                .SetCustomText("Sost. Ped.");
        }

        public static PedalMarking CreateUnaCorda(IEnumerable<StaveNote> notes)
        {
            return new PedalMarking(notes)
                .SetType(PedalMarkingType.Text)
                .SetCustomText("una corda", "tre corda");
        }

        public IReadOnlyList<StaveNote> GetNotes() => notes;
        public PedalMarkingType GetPedalType() => type;
        public int GetLine() => line;
        public string GetDepressText() => depressText;
        public string GetReleaseText() => releaseText;

        public PedalMarking SetType(PedalMarkingType pedalType)
        {
            if (pedalType >= PedalMarkingType.Text && pedalType <= PedalMarkingType.Mixed)
                type = pedalType;
            return this;
        }

        public PedalMarking SetType(string pedalType)
        {
            switch (pedalType)
            {
                case "text":
                    return SetType(PedalMarkingType.Text);
                case "bracket":
                    return SetType(PedalMarkingType.Bracket);
                case "mixed":
                    return SetType(PedalMarkingType.Mixed);
                default:
                    return this;
            }
        }

        public PedalMarking SetCustomText(string depress, string? release = null)
        {
            depressText = depress ?? string.Empty;
            releaseText = release ?? string.Empty;
            font = Metrics.GetFontInfo("PedalMarking.text");
            return this;
        }

        public PedalMarking SetLine(int newLine)
        {
            line = newLine;
            return this;
        }

        public override string GetCategory() => CATEGORY;

        private static double MeasureText(RenderContext ctx, string text, MetricsFontInfo font)
        {
            if (IsPedalGlyph(text))
                return Glyph.GetWidth(text, font.Size);

            double measured = ctx.MeasureText(text).Width;
            if (measured > 0) return measured;
            return TextFormatter.Create(font.Family, font.Size).GetWidthForTextInPx(text);
        }

        private static bool IsPedalGlyph(string text)
            => text == PedalDepressGlyph || text == PedalReleaseGlyph;

        private static void DrawPedalText(RenderContext ctx, string text, double x, double y, MetricsFontInfo font)
        {
            if (IsPedalGlyph(text))
            {
                new Glyph(text, font.Size).Render(ctx, x, y);
                return;
            }

            ctx.FillText(text, x, y);
        }

        private static double GetNoteEndX(StaveNote note)
        {
            var voice = note.GetVoice();
            if (voice != null)
            {
                var tickables = voice.GetTickables();
                int index = tickables.IndexOf(note);
                if (index >= 0 && index + 1 < tickables.Count && tickables[index + 1] is Note nextNote)
                    return nextNote.GetAbsoluteX();
            }

            var stave = note.GetStave();
            return stave != null ? stave.GetX() + stave.GetWidth() : note.GetAbsoluteX() + note.GetWidth();
        }

        private double GetPedalY(StaveNote note) => note.CheckStave().GetYForBottomText(line + 3);

        public void DrawBracketed()
        {
            var ctx = CheckContext();
            bool isPedalDepressed = false;
            double prevX = 0;
            double prevY = 0;

            for (int index = 0; index < notes.Count; index++)
            {
                var note = notes[index];
                isPedalDepressed = !isPedalDepressed;

                double x = note.GetAbsoluteX();
                double y = GetPedalY(note);

                if (index > 0 && x < prevX)
                    throw new VexFlowException("InvalidConfiguration", "The notes provided must be in order of ascending x positions");

                bool nextNoteIsSame = index + 1 < notes.Count && ReferenceEquals(notes[index + 1], note);
                bool prevNoteIsSame = index > 0 && ReferenceEquals(notes[index - 1], note);
                double xShift = 0;

                if (isPedalDepressed)
                {
                    xShift = prevNoteIsSame ? 5 : 0;

                    if (type == PedalMarkingType.Mixed && !prevNoteIsSame)
                    {
                        double textWidth = MeasureText(ctx, depressText, font);
                        DrawPedalText(ctx, depressText, x, y, font);
                        xShift = textWidth + RenderOptions.TextMarginRight;
                    }
                    else
                    {
                        ctx.BeginPath();
                        ctx.MoveTo(x, y - RenderOptions.BracketHeight);
                        ctx.LineTo(x + xShift, y);
                        ctx.Stroke();
                        ctx.ClosePath();
                    }
                }
                else
                {
                    double noteEndX = GetNoteEndX(note);
                    ctx.BeginPath();
                    ctx.MoveTo(prevX, prevY);
                    ctx.LineTo(nextNoteIsSame ? x - 5 : noteEndX - 5, y);
                    ctx.LineTo(nextNoteIsSame ? x : noteEndX - 5, y - RenderOptions.BracketHeight);
                    ctx.Stroke();
                    ctx.ClosePath();
                }

                prevX = x + xShift;
                prevY = y;
            }
        }

        public void DrawText()
        {
            var ctx = CheckContext();
            bool isPedalDepressed = false;

            foreach (var note in notes)
            {
                isPedalDepressed = !isPedalDepressed;
                double x = note.GetAbsoluteX();
                double y = GetPedalY(note);

                if (isPedalDepressed)
                {
                    DrawPedalText(ctx, depressText, x, y, font);
                }
                else
                {
                    double textWidth = MeasureText(ctx, releaseText, font);
                    DrawPedalText(ctx, releaseText, GetNoteEndX(note) - textWidth, y, font);
                }
            }
        }

        public override void Draw()
        {
            var ctx = CheckContext();
            rendered = true;

            ctx.SetStrokeStyle(RenderOptions.Color);
            ctx.SetFillStyle(RenderOptions.Color);
            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);

            if (type == PedalMarkingType.Bracket || type == PedalMarkingType.Mixed)
            {
                ctx.SetLineWidth(RenderOptions.BracketLineWidth);
                DrawBracketed();
            }
            else
            {
                DrawText();
            }
        }
    }
}

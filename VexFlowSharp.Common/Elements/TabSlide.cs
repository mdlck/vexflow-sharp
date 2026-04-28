#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;

namespace VexFlowSharp
{
    public class TabSlide : TabTie
    {
        public new const string CATEGORY = "TabSlide";
        public const int SLIDE_UP = 1;
        public const int SLIDE_DOWN = -1;

        private readonly int direction;

        public static TabSlide CreateSlideUp(TieNotes notes) => new TabSlide(notes, SLIDE_UP);
        public static TabSlide CreateSlideDown(TieNotes notes) => new TabSlide(notes, SLIDE_DOWN);

        public TabSlide(TieNotes notes, int? direction = null) : base(notes, "sl.")
        {
            this.direction = direction ?? InferDirection(notes);
            RenderOptions.Cp1 = Metrics.GetDouble("TabSlide.cp1");
            RenderOptions.Cp2 = Metrics.GetDouble("TabSlide.cp2");
            RenderOptions.YShift = Metrics.GetDouble("TabSlide.yShift");
        }

        private static int InferDirection(TieNotes notes)
        {
            double first = GetFirstFret(notes.FirstNote as TabNote);
            double last = GetFirstFret(notes.LastNote as TabNote);
            if (double.IsNaN(first) || double.IsNaN(last)) return SLIDE_UP;
            return first > last ? SLIDE_DOWN : SLIDE_UP;
        }

        private static double GetFirstFret(TabNote? note)
        {
            if (note == null || note.GetPositions().Length == 0) return double.NaN;
            return double.TryParse(note.GetPositions()[0].Fret?.ToString(), out var fret) ? fret : double.NaN;
        }

        public new int GetDirection() => direction;
        public override string GetCategory() => CATEGORY;

        public override void Draw()
        {
            if (direction != SLIDE_UP && direction != SLIDE_DOWN)
                throw new VexFlowException("BadSlide", "Invalid slide direction");

            var ctx = CheckContext();
            var notes = GetNotes();
            if (notes.FirstNote == null || notes.LastNote == null)
                return;

            var firstYs = notes.FirstNote.GetYs();
            if (firstYs.Length == 0)
                throw new VexFlowException("BadArguments", "No Y-values to render");

            double firstX = notes.FirstNote.GetTieRightX();
            double lastX = notes.LastNote.GetTieLeftX();
            int[] firstIndexes = notes.FirstIndexes ?? new[] { notes.FirstIndex };

            foreach (var firstIndex in firstIndexes)
            {
                if (firstIndex < 0 || firstIndex >= firstYs.Length)
                    throw new VexFlowException("BadArguments", "Bad indexes for slide rendering.");

                double slideY = firstYs[firstIndex] + RenderOptions.YShift;
                if (double.IsNaN(slideY))
                    throw new VexFlowException("BadArguments", "Bad indexes for slide rendering.");

                ctx.BeginPath();
                double endpointOffset = Metrics.GetDouble("TabSlide.slideEndpointOffset");
                ctx.MoveTo(firstX, slideY + endpointOffset * direction);
                ctx.LineTo(lastX, slideY - endpointOffset * direction);
                ctx.ClosePath();
                ctx.Stroke();
            }
            RenderTextLabel(ctx, firstX, lastX);
            rendered = true;
        }

        private void RenderTextLabel(RenderContext ctx, double firstX, double lastX)
        {
            double centerX = (firstX + lastX) / 2.0;
            var measure = ctx.MeasureText("sl.");
            centerX -= measure.Width / 2.0;
            centerX += RenderOptions.TextShiftX;

            var stave = GetNotes().FirstNote?.GetStave() ?? GetNotes().LastNote?.GetStave();
            if (stave == null) return;

            var font = Metrics.GetFontInfo("TabSlide");
            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
            ctx.FillText("sl.", centerX, stave.GetYForTopText() + Metrics.GetDouble("TabSlide.labelYShift"));
        }
    }
}

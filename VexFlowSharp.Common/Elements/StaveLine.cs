#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public class StaveLineTextOptions
    {
        public string? Text { get; set; }
        public MetricsFontInfo? Font { get; set; }
    }

    public class StaveLineParams
    {
        public StaveNote From { get; set; } = null!;
        public StaveNote To { get; set; } = null!;
        public List<int> FirstIndexes { get; set; } = new List<int>();
        public List<int> LastIndexes { get; set; } = new List<int>();
        public StaveLineTextOptions? Options { get; set; }
    }

    /// <summary>
    /// Straight line connector between indexed noteheads.
    /// First-pass port of VexFlow 5 StaveLine.
    /// </summary>
    public class StaveLine : Element
    {
        public new const string CATEGORY = "StaveLine";
        public override string GetCategory() => CATEGORY;

        private readonly StaveNote from;
        private readonly StaveNote to;
        private readonly List<int> firstIndexes;
        private readonly List<int> lastIndexes;
        private readonly string? text;
        private readonly MetricsFontInfo font;

        public StaveLine(StaveLineParams parameters)
        {
            from = parameters.From;
            to = parameters.To;
            firstIndexes = parameters.FirstIndexes.Count > 0 ? parameters.FirstIndexes : new List<int> { 0 };
            lastIndexes = parameters.LastIndexes.Count > 0 ? parameters.LastIndexes : new List<int> { 0 };
            text = parameters.Options?.Text;
            font = parameters.Options?.Font ?? Metrics.GetFontInfo("StaveText");
        }

        public StaveNote GetStart() => from;
        public StaveNote GetStop() => to;
        public IReadOnlyList<int> GetFirstIndexes() => firstIndexes;
        public IReadOnlyList<int> GetLastIndexes() => lastIndexes;
        public string? GetText() => text;
        public MetricsFontInfo GetFontInfo() => font;

        public override void Draw()
        {
            var ctx = CheckContext();
            rendered = true;

            int count = System.Math.Max(firstIndexes.Count, lastIndexes.Count);
            for (int i = 0; i < count; i++)
            {
                int firstIndex = firstIndexes[System.Math.Min(i, firstIndexes.Count - 1)];
                int lastIndex = lastIndexes[System.Math.Min(i, lastIndexes.Count - 1)];
                double startX = from.GetTieRightX();
                double stopX = to.GetTieLeftX();
                double startY = GetY(from, firstIndex);
                double stopY = GetY(to, lastIndex);

                ctx.BeginPath();
                ctx.MoveTo(startX, startY);
                ctx.LineTo(stopX, stopY);
                ctx.Stroke();
            }

            if (!string.IsNullOrEmpty(text))
            {
                int firstIndex = firstIndexes[0];
                int lastIndex = lastIndexes[0];
                double startX = from.GetTieRightX();
                double stopX = to.GetTieLeftX();
                double y = (GetY(from, firstIndex) + GetY(to, lastIndex)) / 2 - 4;

                ctx.Save();
                ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
                ctx.FillText(text!, (startX + stopX) / 2, y);
                ctx.Restore();
            }
        }

        private static double GetY(StaveNote note, int index)
        {
            var ys = note.GetYs();
            if (ys.Length == 0) return note.GetTieYForTop();
            return ys[System.Math.Max(0, System.Math.Min(index, ys.Length - 1))];
        }
    }
}

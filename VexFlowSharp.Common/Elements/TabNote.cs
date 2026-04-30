// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;
using System.Linq;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public class TabNotePosition
    {
        public int Str { get; set; }
        public object Fret { get; set; } = 0;
    }

    public class TabNoteStruct : StaveNoteStruct
    {
        public TabNotePosition[] Positions { get; set; } = Array.Empty<TabNotePosition>();
    }

    public class TabNoteRenderOptions
    {
        public bool DrawStem { get; set; }
        public bool DrawDots { get; set; }
        public bool DrawStemThroughStave { get; set; }
        public double YShift { get; set; }
    }

    /// <summary>
    /// Tablature note rendered as fret text on stave strings.
    /// First-pass port of VexFlow 5's TabNote class.
    /// </summary>
    public class TabNote : StemmableNote
    {
        public new const string CATEGORY = "TabNote";

        private readonly List<string> fretTexts = new List<string>();
        private readonly List<double> fretWidths = new List<double>();
        private readonly List<bool> mutedFretGlyphs = new List<bool>();
        private bool ghost;
        private bool preFormatted;
        private readonly TabNotePosition[] positions;

        public TabNoteRenderOptions RenderOptions { get; } = new TabNoteRenderOptions();

        public TabNote(TabNoteStruct noteStruct, bool drawStem = false) : base(noteStruct)
        {
            positions = noteStruct.Positions ?? Array.Empty<TabNotePosition>();
            RenderOptions.DrawStem = drawStem;
            RenderOptions.DrawDots = drawStem;
            RenderOptions.DrawStemThroughStave = false;
            RenderOptions.YShift = 0;
            glyphProps = Tables.GetGlyphProps(duration, noteType);
            BuildStem();
            SetStemDirection(noteStruct.StemDirection ?? Stem.UP);
            UpdateWidth();
        }

        public override string GetCategory() => CATEGORY;
        public TabNotePosition[] GetPositions() => positions;
        public bool IsGhost() => ghost;
        public override bool HasStem() => RenderOptions.DrawStem;

        public int GreatestString() => positions.Length == 0 ? 0 : positions.Max(p => p.Str);
        public int LeastString() => positions.Length == 0 ? 0 : positions.Min(p => p.Str);

        public TabNote SetGhost(bool value)
        {
            ghost = value;
            UpdateWidth();
            return this;
        }

        public void UpdateWidth()
        {
            fretTexts.Clear();
            fretWidths.Clear();
            mutedFretGlyphs.Clear();
            width = 0;

            var font = Metrics.GetFontInfo("TabNote.text");
            var formatter = TextFormatter.Create(font.Family, font.Size);
            foreach (var position in positions)
            {
                string text = position.Fret.ToString() ?? string.Empty;
                if (ghost) text = "(" + text + ")";
                bool muted = string.Equals(text, "X", StringComparison.OrdinalIgnoreCase);
                fretTexts.Add(text);
                mutedFretGlyphs.Add(muted);
                double textWidth = muted
                    ? Glyph.GetWidth("accidentalDoubleSharp", Metrics.GetFontInfo("TabNote").Size)
                    : formatter.GetWidthForTextInPx(text);
                fretWidths.Add(textWidth);
                width = Math.Max(width, textWidth);
            }
        }

        public override Note SetStave(Stave newStave)
        {
            base.SetStave(newStave);
            var staveContext = newStave.GetContext();
            if (staveContext != null)
                SetContext(staveContext);

            SetYs(positions.Select(p => newStave.GetYForLine(p.Str - 1)).ToArray());
            if (stem != null)
                stem.SetYBounds(GetStemY(), GetStemY());
            return this;
        }

        public override (double X, double Y) GetModifierStartXY(ModifierPosition position, int index, object options = null)
        {
            if (!preFormatted)
                throw new VexFlowException("UnformattedNote", "Can't call GetModifierStartXY on an unformatted note");
            if (ys.Length == 0)
                throw new VexFlowException("NoYValues", "No Y-values calculated for this note.");

            double x = 0;
            if (position == ModifierPosition.Left)
                x = -2;
            else if (position == ModifierPosition.Right)
                x = width + 2;
            else if (position == ModifierPosition.Below || position == ModifierPosition.Above)
                x = width / 2;

            int safeIndex = Math.Max(0, Math.Min(index, ys.Length - 1));
            return (GetAbsoluteX() + x, ys[safeIndex]);
        }

        public override double GetLineForRest()
        {
            return positions.Length > 0 ? positions[0].Str : base.GetLineForRest();
        }

        public override double GetStemX() => GetCenterGlyphX();

        public double GetStemY()
        {
            int numLines = CheckStave().GetNumLines();
            double stemStartLine = GetStemDirection() == Stem.UP ? -0.5 : numLines - 0.5;
            return CheckStave().GetYForLine(stemStartLine);
        }

        public override void PreFormat()
        {
            if (preFormatted) return;
            modifierContext.PreFormat();
            preFormatted = true;
        }

        private void DrawPositions(RenderContext ctx)
        {
            var font = Metrics.GetFontInfo("TabNote.text");
            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
            double x = GetAbsoluteX();
            for (int i = 0; i < positions.Length; i++)
            {
                double y = ys[i] + RenderOptions.YShift;
                double textWidth = fretWidths[i];
                double tabX = x - textWidth / 2;
                ctx.ClearRect(tabX - 2, y - 3, textWidth + 4, 6);
                if (mutedFretGlyphs[i])
                {
                    var mutedGlyph = new Glyph("accidentalDoubleSharp", Metrics.GetFontInfo("TabNote").Size);
                    mutedGlyph.SetContext(ctx);
                    mutedGlyph.Render(ctx, tabX, y);
                }
                else
                {
                    ctx.FillText(fretTexts[i], tabX, y + font.Size / 3);
                }
            }
        }

        private void DrawStemThrough(RenderContext ctx)
        {
            if (!RenderOptions.DrawStem || !RenderOptions.DrawStemThroughStave) return;
            if (ys.Length == 0) return;
            double stemX = GetStemX();
            double stemY = GetStemY();
            var unusedStringGroups = GetUnusedStringGroups(CheckStave().GetNumLines(), positions.Select(p => p.Str).ToList());
            var stemLines = GetPartialStemLines(stemY, unusedStringGroups, CheckStave(), GetStemDirection());

            ctx.SetLineWidth(Stem.WIDTH);
            foreach (var bounds in stemLines)
            {
                if (bounds.Count == 0) continue;
                ctx.BeginPath();
                ctx.MoveTo(stemX, bounds.First());
                ctx.LineTo(stemX, bounds.Last());
                ctx.Stroke();
                ctx.ClosePath();
            }
        }

        private static List<List<int>> GetUnusedStringGroups(int numLines, List<int> stringsUsed)
        {
            var stemThrough = new List<List<int>>();
            var group = new List<int>();
            for (int str = 1; str <= numLines; str++)
            {
                if (!stringsUsed.Contains(str))
                {
                    group.Add(str);
                }
                else
                {
                    stemThrough.Add(group);
                    group = new List<int>();
                }
            }

            if (group.Count > 0)
                stemThrough.Add(group);

            return stemThrough;
        }

        private static List<List<double>> GetPartialStemLines(double stemY, List<List<int>> unusedStrings, Stave stave, int stemDirection)
        {
            bool upStem = stemDirection != Stem.UP;
            bool downStem = stemDirection != Stem.DOWN;
            double lineSpacing = stave.GetSpacingBetweenLines();
            int totalLines = stave.GetNumLines();
            var stemLines = new List<List<double>>();

            foreach (var originalGroup in unusedStrings)
            {
                var strings = new List<int>(originalGroup);
                bool containsLastString = strings.Contains(totalLines);
                bool containsFirstString = strings.Contains(1);

                if ((upStem && containsFirstString) || (downStem && containsLastString))
                    continue;

                if (strings.Count == 1)
                    strings.Add(strings[0]);

                var lineYs = new List<double>();
                for (int i = 0; i < strings.Count; i++)
                {
                    int str = strings[i];
                    bool isTopBound = str == 1;
                    bool isBottomBound = str == totalLines;
                    double y = stave.GetYForLine(str - 1);

                    if (i == 0 && !isTopBound)
                        y -= lineSpacing / 2 - 1;
                    else if (i == strings.Count - 1 && !isBottomBound)
                        y += lineSpacing / 2 - 1;

                    lineYs.Add(y);

                    if (stemDirection == Stem.UP && isTopBound)
                        lineYs.Add(stemY - 2);
                    else if (stemDirection == Stem.DOWN && isBottomBound)
                        lineYs.Add(stemY + 2);
                }

                lineYs.Sort();
                stemLines.Add(lineYs);
            }

            return stemLines;
        }

        private new void DrawFlag()
        {
            if (beam != null || !RenderOptions.DrawStem || flag == null || stem == null) return;

            double flagX = GetStemX();
            double flagY = GetStemDirection() == Stem.DOWN
                ? GetStemY() - stem.GetHeight() - GetStemExtension()
                : GetStemY() - stem.GetHeight() + GetStemExtension();

            flag.SetContext(CheckContext());
            flag.Render(CheckContext(), flagX, flagY);
        }

        public override void Draw()
        {
            var ctx = CheckContext();
            if (ys.Length == 0)
                throw new VexFlowException("NoYValues", "Can't draw note without Y values.");
            SetRendered();
            var renderStem = beam == null && RenderOptions.DrawStem;

            var cls = GetAttribute("class");
            ctx.OpenGroup(string.IsNullOrEmpty(cls) ? "tabnote" : "tabnote " + cls, GetId());
            DrawPositions(ctx);
            DrawStemThrough(ctx);

            if (stem != null && renderStem)
            {
                double stemX = GetStemX();
                stem.SetNoteHeadXBounds(stemX, stemX);
                stem.SetContext(ctx).DrawWithStyle();
            }

            DrawFlag();

            foreach (var modifier in modifiers)
            {
                if (modifier is Dot && !RenderOptions.DrawDots) continue;
                modifier.SetContext(ctx);
                modifier.DrawWithStyle();
            }
            DrawPointerRect();
            ctx.CloseGroup();
        }
    }
}

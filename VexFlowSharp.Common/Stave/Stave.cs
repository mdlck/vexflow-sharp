#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;
using System.Linq;

namespace VexFlowSharp
{
    /// <summary>
    /// Options for configuring a Stave.
    /// Port of VexFlow's StaveOptions interface from stave.ts.
    /// </summary>
    public class StaveOptions
    {
        public int    NumLines                 { get; set; } = 5;
        public double SpacingBetweenLinesPx    { get; set; } = Tables.STAVE_LINE_DISTANCE; // 10.0
        public double SpaceAboveStaffLn        { get; set; } = 4.0;
        public double SpaceBelowStaffLn        { get; set; } = 4.0;
        public double TopTextPosition          { get; set; } = 1.0;
        public double BottomTextPosition       { get; set; } = 4.0;  // set to NumLines in ResetLines
        public string FillStyle                { get; set; } = "#999999";
        public bool   LeftBar                  { get; set; } = true;
        public bool   RightBar                 { get; set; } = true;
        /// <summary>Whether each line is visible. Populated by ResetLines().</summary>
        public List<bool> LineConfig           { get; set; } = new List<bool>();
    }

    /// <summary>
    /// The musical staff: five horizontal lines on which notes are placed.
    /// Port of VexFlow's Stave class (stave.ts, ~929 lines).
    ///
    /// Coordinate system:
    ///   - GetYForLine(n)  → centre of staff line n (line 0 = top, line 4 = bottom)
    ///   - GetYForNote(n)  → y for a note at staff-line n (grows UP as n increases)
    /// </summary>
    public class Stave : Element
    {
        // ── Fields ────────────────────────────────────────────────────────────

        protected double x;
        protected double y;
        protected double width;
        protected double height;
        protected double startX;   // x after all BEGIN modifiers
        protected double endX;     // x before all END modifiers
        protected int    measure;
        protected bool   formatted;
        protected string clef     = "treble";
        protected string? endClef;

        protected readonly StaveOptions options;
        protected readonly List<StaveModifier> modifiers;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a stave at (x, y) with the given width.
        /// Automatically adds begin and end barlines.
        /// </summary>
        public Stave(double x, double y, double width, StaveOptions? options = null)
        {
            this.x      = x;
            this.y      = y;
            this.width  = width;
            this.startX = x + 5;
            this.endX   = x + width;
            this.measure   = 0;
            this.formatted = false;
            this.modifiers = new List<StaveModifier>();

            this.options = options ?? new StaveOptions();

            ResetLines();

            // Add begin barline (index 0) and end barline (index 1)
            AddModifier(new Barline(options?.LeftBar  ?? true  ? BarlineType.Single : BarlineType.None));
            AddEndModifier(new Barline(options?.RightBar ?? true ? BarlineType.Single : BarlineType.None));
        }

        // ── Lines / geometry setup ────────────────────────────────────────────

        /// <summary>
        /// Reset line configuration. Called from constructor and SetNumLines.
        /// </summary>
        public void ResetLines()
        {
            options.LineConfig = new List<bool>();
            for (int i = 0; i < options.NumLines; i++)
                options.LineConfig.Add(true);

            height = (options.NumLines + options.SpaceAboveStaffLn) * options.SpacingBetweenLinesPx;
            options.BottomTextPosition = options.NumLines;
        }

        // ── Y coordinate methods ──────────────────────────────────────────────

        /// <summary>
        /// Y for the centre of staff line n (line 0 = top, line 4 = bottom for a 5-line stave).
        /// Formula: y + headroom * spacing + line * spacing
        /// </summary>
        public double GetYForLine(double line)
        {
            double spacing = options.SpacingBetweenLinesPx;
            double headroom = options.SpaceAboveStaffLn;
            return y + headroom * spacing + line * spacing;
        }

        /// <summary>
        /// Y for a note at the given note-line (grows upward; line 3 ≈ middle of staff B4 in treble).
        /// Formula: y + headroom * spacing + 5 * spacing - line * spacing
        /// </summary>
        public double GetYForNote(double line)
        {
            double spacing = options.SpacingBetweenLinesPx;
            double headroom = options.SpaceAboveStaffLn;
            return y + headroom * spacing + 5 * spacing - line * spacing;
        }

        /// <summary>Y above the staff for text at the given line level (0 = immediately above).</summary>
        public double GetYForTopText(double line = 0)
        {
            return GetYForLine(-line - options.TopTextPosition);
        }

        /// <summary>Y below the staff for text at the given line level.</summary>
        public double GetYForBottomText(double line = 0)
        {
            return GetYForLine(options.BottomTextPosition + line);
        }

        /// <summary>Y for glyph rendering — centre of the staff (line 3).</summary>
        public double GetYForGlyphs() => GetYForLine(3);

        /// <summary>Y of the top edge of the topmost staff line.</summary>
        public double GetTopLineTopY()
            => GetYForLine(0) - Tables.STAVE_LINE_THICKNESS / 2;

        /// <summary>Y of the bottom edge of the bottommost staff line.</summary>
        public double GetBottomLineBottomY()
            => GetYForLine(options.NumLines - 1) + Tables.STAVE_LINE_THICKNESS / 2;

        /// <summary>Reverse of GetYForLine: staff line for a given y.</summary>
        public double GetLineForY(double yCoord)
        {
            double spacing  = options.SpacingBetweenLinesPx;
            double headroom = options.SpaceAboveStaffLn;
            return (yCoord - y) / spacing - headroom;
        }

        // ── X coordinate methods ──────────────────────────────────────────────

        /// <summary>Convert stave-space units to pixels.</summary>
        public double Space(double spacing)
        {
            return options.SpacingBetweenLinesPx * spacing;
        }

        /// <summary>Note start x (after all BEGIN modifiers). Forces Format() if needed.</summary>
        public double GetNoteStartX()
        {
            if (!formatted) Format();
            return startX;
        }

        /// <summary>Override the note start x position.</summary>
        public Stave SetNoteStartX(double x)
        {
            if (!formatted) Format();
            startX = x;
            return this;
        }

        /// <summary>
        /// Default padding (left + right) used by System to calculate justify width.
        /// Approximates VexFlow's stave.padding + stave.endPaddingMax from font metrics.
        /// </summary>
        public static double DefaultPadding => Tables.STAVE_LINE_DISTANCE;

        /// <summary>
        /// Right padding used by System when startX is already determined (autoWidth).
        /// Approximates VexFlow's stave.endPaddingMax from font metrics.
        /// </summary>
        public static double RightPadding => 0;

        /// <summary>
        /// Align beginning modifiers (clef, key signature, time signature) across multiple staves.
        /// Port of VexFlow's Stave.formatBegModifiers() from stave.ts.
        /// For each modifier category (Clef, KeySignature, TimeSignature), finds the maximum
        /// x across all staves and adjusts modifiers and noteStartX accordingly.
        /// </summary>
        public static void FormatBegModifiers(List<Stave> staves)
        {
            if (staves == null || staves.Count == 0) return;

            // Ensure all staves are formatted first
            foreach (var stave in staves)
            {
                if (!stave.formatted) stave.Format();
            }

            // Align each category by adjusting modifier x positions
            AlignCategoryStartX(staves, "Clef");
            AlignCategoryStartX(staves, "KeySignature");
            AlignCategoryStartX(staves, "TimeSignature");

            // Final pass: align note start x across all staves
            double maxX = 0;
            foreach (var stave in staves)
                maxX = Math.Max(maxX, stave.GetNoteStartX());
            foreach (var stave in staves)
                stave.SetNoteStartX(maxX);
        }

        private static void AlignCategoryStartX(List<Stave> staves, string category)
        {
            // Find the maximum x position for this modifier category across all staves
            double minStartX = 0;
            foreach (var stave in staves)
            {
                var mods = stave.GetModifiers(StaveModifierPosition.Begin, category);
                if (mods.Count > 0 && mods[0].GetX() > minStartX)
                    minStartX = mods[0].GetX();
            }

            // Shift modifiers and noteStartX for staves that are behind the max
            foreach (var stave in staves)
            {
                double adjustX = 0;
                var mods = stave.GetModifiers(StaveModifierPosition.Begin, category);
                foreach (var mod in mods)
                {
                    if (minStartX - mod.GetX() > adjustX)
                        adjustX = minStartX - mod.GetX();
                }

                if (adjustX > 0)
                {
                    // Apply adjustment to all begin modifiers at or after this category
                    bool bAdjust = false;
                    var allMods = stave.GetModifiers(StaveModifierPosition.Begin);
                    foreach (var mod in allMods)
                    {
                        if (mod.GetType().Name == category) bAdjust = true;
                        if (bAdjust) mod.SetX(mod.GetX() + adjustX);
                    }
                    stave.SetNoteStartX(stave.GetNoteStartX() + adjustX);
                }
            }
        }

        /// <summary>Note end x (before all END modifiers). Forces Format() if needed.</summary>
        public double GetNoteEndX()
        {
            if (!formatted) Format();
            return endX;
        }

        /// <summary>Tie start x (same as startX without triggering Format).</summary>
        public double GetTieStartX() => startX;

        /// <summary>Tie end x.</summary>
        public double GetTieEndX() => endX;

        // ── Properties ────────────────────────────────────────────────────────

        public double GetX() => x;

        public void SetX(double newX)
        {
            double shift = newX - x;
            formatted = false;
            x = newX;
            startX += shift;
            endX   += shift;
            foreach (var mod in modifiers)
                mod.SetX(mod.GetX() + shift);
        }

        public double GetY() => y;

        public void SetY(double newY) { y = newY; }

        public double GetWidth() => width;

        public void SetWidth(double w)
        {
            formatted = false;
            width = w;
            endX  = x + w;
        }

        public double GetHeight() => height;

        public int GetNumLines() => options.NumLines;

        public void SetNumLines(int n)
        {
            options.NumLines = n;
            ResetLines();
        }

        public double GetSpacingBetweenLines() => options.SpacingBetweenLinesPx;

        public string GetClef() => clef;

        public string? GetEndClef() => endClef;

        public int GetMeasure() => measure;
        public void SetMeasure(int m) { measure = m; }

        // ── Modifiers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Add a modifier to this stave at the given position.
        /// Sets the modifier's stave reference and marks the stave as unformatted.
        /// </summary>
        public Stave AddModifier(StaveModifier modifier, StaveModifierPosition? position = null)
        {
            if (position.HasValue)
                modifier.SetPosition(position.Value);
            modifier.SetStave(this);
            formatted = false;
            modifiers.Add(modifier);
            return this;
        }

        /// <summary>Add a modifier at the END position.</summary>
        public Stave AddEndModifier(StaveModifier modifier)
        {
            return AddModifier(modifier, StaveModifierPosition.End);
        }

        /// <summary>Get all modifiers, optionally filtered by position and/or category name.</summary>
        public List<StaveModifier> GetModifiers(StaveModifierPosition? position = null, string? category = null)
        {
            if (position == null && category == null)
                return modifiers;
            if (position == null)
                return modifiers.FindAll(m => GetCategory(m) == category);
            if (category == null)
                return modifiers.FindAll(m => m.GetPosition() == position.Value);
            return modifiers.FindAll(m => m.GetPosition() == position.Value && GetCategory(m) == category);
        }

        private static string GetCategory(StaveModifier m)
        {
            return m.GetType().Name;
        }

        // ── Convenience add methods ───────────────────────────────────────────

        /// <summary>Add a clef to this stave.</summary>
        public Stave AddClef(string clefType, string size = "default", string? annotation = null,
            StaveModifierPosition position = StaveModifierPosition.Begin)
        {
            if (position == StaveModifierPosition.Begin)
                this.clef = clefType;
            else if (position == StaveModifierPosition.End)
                this.endClef = clefType;
            AddModifier(new Clef(clefType, size, annotation), position);
            return this;
        }

        /// <summary>Add a key signature to this stave.</summary>
        public Stave AddKeySignature(string keySpec, string? cancelKeySpec = null,
            StaveModifierPosition position = StaveModifierPosition.Begin)
        {
            AddModifier(new KeySignature(keySpec, cancelKeySpec), position);
            return this;
        }

        /// <summary>Add a time signature to this stave.</summary>
        public Stave AddTimeSignature(string timeSpec, double customPadding = 15,
            StaveModifierPosition position = StaveModifierPosition.Begin)
        {
            AddModifier(new TimeSignature(timeSpec, customPadding), position);
            return this;
        }

        // ── Barline helpers ───────────────────────────────────────────────────

        /// <summary>Set the beginning barline type (Single, RepeatBegin, or None).</summary>
        public Stave SetBegBarType(BarlineType type)
        {
            if (type == BarlineType.Single || type == BarlineType.RepeatBegin || type == BarlineType.None)
            {
                ((Barline)modifiers[0]).SetType(type);
                formatted = false;
            }
            return this;
        }

        /// <summary>Set the ending barline type.</summary>
        public Stave SetEndBarType(BarlineType type)
        {
            if (type != BarlineType.RepeatBegin)
            {
                ((Barline)modifiers[1]).SetType(type);
                formatted = false;
            }
            return this;
        }

        // ── Format ────────────────────────────────────────────────────────────

        /// <summary>
        /// Calculate startX and endX by summing modifier widths and paddings.
        /// Must be called before GetNoteStartX/GetNoteEndX (auto-called on first access).
        /// Port of VexFlow's Stave.format() from stave.ts.
        /// </summary>
        public void Format()
        {
            var begModifiers = GetModifiers(StaveModifierPosition.Begin);
            var endModifiers = GetModifiers(StaveModifierPosition.End);

            // Sort begin modifiers: Barline(0), Clef(1), KeySignature(2), TimeSignature(3)
            SortByCategory(begModifiers, new Dictionary<string, int>
            {
                { "Barline", 0 }, { "Clef", 1 }, { "KeySignature", 2 }, { "TimeSignature", 3 }
            });

            // Sort end modifiers: TimeSignature(0), KeySignature(1), Barline(2), Clef(3)
            SortByCategory(endModifiers, new Dictionary<string, int>
            {
                { "TimeSignature", 0 }, { "KeySignature", 1 }, { "Barline", 2 }, { "Clef", 3 }
            });

            // Calculate startX by walking begin modifiers
            int offset = 0;
            double curX = x;
            for (int i = 0; i < begModifiers.Count; i++)
            {
                var mod     = begModifiers[i];
                double pad   = mod.GetPadding(i + offset);
                double w     = mod.GetWidth();
                curX        += pad;
                mod.SetX(curX);
                curX        += w;
                if (pad + w == 0) offset--;
            }
            startX = curX;

            // Calculate endX by walking end modifiers right-to-left from end of stave.
            // Port of VexFlow's Stave.format() end-modifier loop from stave.ts.
            // CRITICAL: if there is exactly one end modifier (just the barline), endX = x + width.
            // This matches: this.end_x = endModifiers.length === 1 ? this.x + this.width : x;
            curX = x + width;
            for (int i = 0; i < endModifiers.Count; i++)
            {
                var mod    = endModifiers[i];
                var lm     = mod.GetLayoutMetrics();

                double widthsRight       = 0;
                double widthsLeft        = 0;
                double widthsPaddingRight = 0;
                double widthsPaddingLeft  = 0;

                if (lm != null)
                {
                    if (i != 0)
                    {
                        widthsRight        = lm.XMax;
                        widthsPaddingRight = lm.PaddingRight;
                    }
                    widthsLeft       = -(lm.XMin);
                    widthsPaddingLeft = lm.PaddingLeft;

                    if (i == endModifiers.Count - 1)
                        widthsPaddingLeft = 0;
                }
                else
                {
                    widthsPaddingRight = mod.GetPadding(i); // no lastBarlineIdx tracking (simplified)
                    if (i != 0) widthsRight = mod.GetWidth();
                    if (i == 0) widthsLeft  = mod.GetWidth();
                }

                curX -= widthsPaddingRight;
                curX -= widthsRight;
                mod.SetX(curX);
                curX -= widthsLeft;
                curX -= widthsPaddingLeft;
            }
            endX = endModifiers.Count == 1 ? x + width : curX;

            formatted = true;
        }

        private void SortByCategory(List<StaveModifier> items, Dictionary<string, int> order)
        {
            // Stable insertion sort
            for (int i = items.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    var catJ  = items[j].GetType().Name;
                    var catJ1 = items[j + 1].GetType().Name;
                    int ordJ  = order.TryGetValue(catJ,  out var oj)  ? oj  : 99;
                    int ordJ1 = order.TryGetValue(catJ1, out var oj1) ? oj1 : 99;
                    if (ordJ > ordJ1)
                    {
                        var tmp    = items[j];
                        items[j]   = items[j + 1];
                        items[j + 1] = tmp;
                    }
                }
            }
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw the stave: 5 horizontal lines, then all modifiers.
        /// Matches VexFlow stave.ts applyStyle()/restoreStyle() pattern:
        /// save/restore wraps only the line drawing so fill color reverts to black
        /// before modifiers (clef, time sig, barlines) are drawn.
        /// </summary>
        public override void Draw()
        {
            var ctx = CheckContext();
            if (!formatted) Format();

            // Save context state, apply gray fill/stroke for stave lines,
            // then restore so subsequent drawing (notes, accidentals, ties) use black.
            ctx.Save();
            ctx.SetFillStyle(options.FillStyle);
            ctx.SetStrokeStyle(options.FillStyle);

            double lineThickness = Tables.STAVE_LINE_THICKNESS;

            for (int i = 0; i < options.NumLines; i++)
            {
                if (i < options.LineConfig.Count && !options.LineConfig[i])
                    continue;  // line hidden

                double lineY = GetYForLine(i) - lineThickness / 2;
                ctx.FillRect(x, lineY, width, lineThickness);
            }

            ctx.Restore();

            // Draw each modifier — outside save/restore so they use default (black) color
            foreach (var mod in modifiers)
            {
                mod.SetContext(ctx);
                mod.Draw(this, 0);
            }
        }
    }
}

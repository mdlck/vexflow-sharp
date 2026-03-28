// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of vexflow/src/accidental.ts (641 lines)
// Accidental modifier — multi-column stagger layout, SMuFL glyph rendering.

using System;
using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Per-line layout metrics used during accidental column assignment.
    /// Port of VexFlow's StaveLineAccidentalLayoutMetrics type from accidental.ts.
    /// </summary>
    public class StaveLineAccidentalLayoutMetrics
    {
        /// <summary>Column number assigned to this line (1-based).</summary>
        public int Column { get; set; }

        /// <summary>Stave-line position (in line units, 0.5 increments).</summary>
        public double Line { get; set; }

        /// <summary>
        /// True if all accidentals on this line are flat (b) or double-flat (bb).
        /// Flats need 2.5 lines clearance instead of 3.0 for other accidentals.
        /// </summary>
        public bool FlatLine { get; set; }

        /// <summary>
        /// True if all accidentals on this line are double-sharp (##).
        /// Double-sharps need only 2.5 lines clearance above and below.
        /// </summary>
        public bool DblSharpLine { get; set; }

        /// <summary>Number of accidentals stacked on this line.</summary>
        public int NumAcc { get; set; }

        /// <summary>Total x-width needed for all accidentals on this line.</summary>
        public double Width { get; set; }
    }

    /// <summary>
    /// Accidental modifier — attaches to a note head to indicate sharp, flat,
    /// natural, double-sharp, double-flat, or microtonal accidentals.
    ///
    /// The static Format() method implements VexFlow's multi-column stagger
    /// algorithm, which avoids vertical collisions among simultaneous accidentals
    /// using layout tables from Tables.AccidentalColumnsTable.
    ///
    /// Port of VexFlow's Accidental class from accidental.ts.
    /// </summary>
    public class Accidental : Modifier
    {
        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>Category string used by ModifierContext to group accidentals.</summary>
        public const string CATEGORY = "accidentals";

        /// <inheritdoc/>
        public override string GetCategory() => CATEGORY;

        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>The accidental type string ("#", "b", "n", "##", "bb", etc.).</summary>
        public readonly string Type;

        /// <summary>Whether this is a cautionary (parenthesised) accidental.</summary>
        private bool cautionary;

        /// <summary>Font scale for rendering the glyph.</summary>
        private double fontScale;

        // Padding between accidental and parentheses
        private const double ParenLeftPadding  = 2;
        private const double ParenRightPadding = 2;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create an accidental of the given type.
        /// Port of Accidental constructor from accidental.ts.
        /// </summary>
        /// <param name="type">Accidental string: "#", "b", "n", "##", "bb", etc.</param>
        public Accidental(string type)
        {
            Type      = type;
            position  = ModifierPosition.Left;
            cautionary = false;
            fontScale  = Tables.NOTATION_FONT_SCALE;

            // Validate that the type exists in the accidentals table
            Tables.AccidentalCodes(type); // throws VexFlowException if unknown
        }

        // ── Accessors ─────────────────────────────────────────────────────────

        /// <summary>Mark this accidental as cautionary (adds parentheses in Draw).</summary>
        public Accidental SetAsCautionary()
        {
            cautionary = true;
            fontScale  = 28;
            return this;
        }

        /// <summary>Returns true if this is a cautionary accidental.</summary>
        public bool IsCautionary() => cautionary;

        /// <summary>
        /// Get glyph width for this accidental (used by Format to measure column widths).
        /// Cautionary width includes the parenthesis glyphs.
        /// </summary>
        public new double GetWidth()
        {
            var (code, _) = Tables.AccidentalCodes(Type);
            double glyphW = Glyph.GetWidth(code, fontScale);
            if (!cautionary) return glyphW;

            var (leftCode, _)  = Tables.AccidentalCodes("{");
            var (rightCode, _) = Tables.AccidentalCodes("}");
            double parenW = Glyph.GetWidth(leftCode, fontScale)
                          + Glyph.GetWidth(rightCode, fontScale)
                          + ParenLeftPadding + ParenRightPadding;
            return glyphW + parenW;
        }

        // Helper: render a named SMuFL glyph with its RIGHT EDGE at (x, y).
        // Port of VexFlow's Accidental constructor which calls glyph.setOriginX(1.0),
        // meaning accidental glyphs are anchored at their right edge so they appear
        // to the LEFT of the accX position (i.e., before the note head).
        private static void RenderGlyph(RenderContext ctx, double x, double y, double scale, string code)
        {
            double glyphW = Glyph.GetWidth(code, scale);
            var g = new Glyph(code, scale);
            g.Render(ctx, x - glyphW, y);
        }

        // ── Format ────────────────────────────────────────────────────────────

        /// <summary>
        /// Arrange accidentals inside a ModifierContext.
        /// Implements the multi-column stagger algorithm:
        ///   1. Build line-position list for each accidental.
        ///   2. Sort by descending pitch (highest line first).
        ///   3. Group clashing accidentals and assign columns from AccidentalColumnsTable.
        ///   4. Resolve columns to x-offsets and set xShift on each accidental.
        ///
        /// Port of Accidental.format() from accidental.ts.
        /// </summary>
        public static bool Format(List<Accidental>? accidentals, ModifierContextState state)
        {
            if (accidentals == null || accidentals.Count == 0) return false;

            double noteheadAccidentalPadding = Tables.ACCIDENTAL_NOTEHEAD_PADDING;
            double leftShift = state.LeftShift + noteheadAccidentalPadding;
            double accidentalSpacing = Tables.ACCIDENTAL_SPACING;
            double additionalPadding = Tables.ACCIDENTAL_LEFT_PADDING;

            // ── Step 1: Collect line positions ────────────────────────────────

            var linePositions = new List<(double Y, double Line, double ExtraX,
                Accidental Acc, double? LineSpace)>();

            Note? prevNote = null;
            double extraXSpaceNeeded = 0;

            for (int i = 0; i < accidentals.Count; i++)
            {
                var acc  = accidentals[i];
                var note = (Note)acc.GetNote();
                var stave = note.GetStave();
                int idx  = acc.GetIndex() ?? 0;
                var props = note.GetKeyProps()[idx];

                if (!ReferenceEquals(note, prevNote))
                {
                    // Accumulate max left-displaced head space across all keys
                    for (int n = 0; n < note.GetKeys().Length; n++)
                    {
                        extraXSpaceNeeded = Math.Max(
                            note.GetLeftDisplacedHeadPx() - note.GetXShift(),
                            extraXSpaceNeeded);
                    }
                    prevNote = note;
                }

                if (stave != null)
                {
                    double lineSpace = stave.GetSpacingBetweenLines();
                    double y        = stave.GetYForLine(props.Line);
                    double accLine  = Math.Round((y / lineSpace) * 2) / 2.0;
                    linePositions.Add((y, accLine, extraXSpaceNeeded, acc, lineSpace));
                }
                else
                {
                    linePositions.Add((0, props.Line, extraXSpaceNeeded, acc, null));
                }
            }

            // Sort descending by line (highest pitch first)
            linePositions.Sort((a, b) => b.Line.CompareTo(a.Line));

            // ── Step 2: Build stave-line metrics list ─────────────────────────

            var staveLineMetrics = new List<StaveLineAccidentalLayoutMetrics>();
            double maxExtraX = 0;

            for (int i = 0; i < linePositions.Count; i++)
            {
                var (_, line, extraX, acc, _) = linePositions[i];

                StaveLineAccidentalLayoutMetrics? prior = staveLineMetrics.Count > 0
                    ? staveLineMetrics[staveLineMetrics.Count - 1] : null;

                StaveLineAccidentalLayoutMetrics cur;
                if (prior == null || prior.Line != line)
                {
                    cur = new StaveLineAccidentalLayoutMetrics
                    {
                        Line        = line,
                        FlatLine    = true,
                        DblSharpLine = true,
                        NumAcc      = 0,
                        Width       = 0,
                        Column      = 0,
                    };
                    staveLineMetrics.Add(cur);
                }
                else
                {
                    cur = prior;
                }

                // If any accidental on this line is not flat/bb, clear the flat flag
                if (acc.Type != "b" && acc.Type != "bb")
                    cur.FlatLine = false;

                // If any accidental on this line is not ##, clear the dblSharp flag
                if (acc.Type != "##")
                    cur.DblSharpLine = false;

                cur.NumAcc++;
                cur.Width += acc.GetWidth() + accidentalSpacing;
                maxExtraX = Math.Max(extraX, maxExtraX);
            }

            // ── Step 3: Assign columns ────────────────────────────────────────

            int totalColumns = 0;

            for (int i = 0; i < staveLineMetrics.Count; i++)
            {
                bool noFurtherConflicts = false;
                int groupStart = i;
                int groupEnd   = i;

                while (groupEnd + 1 < staveLineMetrics.Count && !noFurtherConflicts)
                {
                    if (CheckCollision(staveLineMetrics[groupEnd], staveLineMetrics[groupEnd + 1]))
                        groupEnd++;
                    else
                        noFurtherConflicts = true;
                }

                // Helper: get line metric at group-relative index
                StaveLineAccidentalLayoutMetrics GetGroupLine(int idx_) =>
                    staveLineMetrics[groupStart + idx_];

                double LineDifference(int a, int b)
                {
                    return GetGroupLine(a).Line - GetGroupLine(b).Line;
                }

                bool NotColliding(params int[][] indexPairs)
                {
                    foreach (var pair in indexPairs)
                        if (CheckCollision(GetGroupLine(pair[0]), GetGroupLine(pair[1]))) return false;
                    return true;
                }

                int groupLength = groupEnd - groupStart + 1;

                string endCase = CheckCollision(
                    staveLineMetrics[groupStart], staveLineMetrics[groupEnd]) ? "a" : "b";

                switch (groupLength)
                {
                    case 3:
                        if (endCase == "a"
                            && LineDifference(1, 2) == 0.5
                            && LineDifference(0, 1) != 0.5)
                            endCase = "second_on_bottom";
                        break;
                    case 4:
                        if (NotColliding(new[] { 0, 2 }, new[] { 1, 3 }))
                            endCase = "spaced_out_tetrachord";
                        break;
                    case 5:
                        if (endCase == "b" && NotColliding(new[] { 1, 3 }))
                        {
                            endCase = "spaced_out_pentachord";
                            if (NotColliding(new[] { 0, 2 }, new[] { 2, 4 }))
                                endCase = "very_spaced_out_pentachord";
                        }
                        break;
                    case 6:
                        if (NotColliding(new[] { 0, 3 }, new[] { 1, 4 }, new[] { 2, 5 }))
                            endCase = "spaced_out_hexachord";
                        if (NotColliding(new[] { 0, 2 }, new[] { 2, 4 }, new[] { 1, 3 }, new[] { 3, 5 }))
                            endCase = "very_spaced_out_hexachord";
                        break;
                }

                int groupMember;
                int column;

                if (groupLength >= 7)
                {
                    // 7+ accidentals: use ascending parallel columns
                    int patternLength = 2;
                    bool collisionDetected = true;
                    while (collisionDetected)
                    {
                        collisionDetected = false;
                        for (int line = 0; line + patternLength < staveLineMetrics.Count; line++)
                        {
                            if (CheckCollision(staveLineMetrics[line], staveLineMetrics[line + patternLength]))
                            {
                                collisionDetected = true;
                                patternLength++;
                                break;
                            }
                        }
                    }
                    for (groupMember = i; groupMember <= groupEnd; groupMember++)
                    {
                        column = ((groupMember - i) % patternLength) + 1;
                        staveLineMetrics[groupMember].Column = column;
                        if (column > totalColumns) totalColumns = column;
                    }
                }
                else
                {
                    // Use layout from AccidentalColumnsTable
                    for (groupMember = i; groupMember <= groupEnd; groupMember++)
                    {
                        column = Tables.AccidentalColumnsTable[groupLength][endCase][groupMember - i];
                        staveLineMetrics[groupMember].Column = column;
                        if (column > totalColumns) totalColumns = column;
                    }
                }

                i = groupEnd;
            }

            // ── Step 4: Convert columns to x-offsets ──────────────────────────

            var columnWidths  = new double[totalColumns + 1];
            var columnXOffsets = new double[totalColumns + 1];
            for (int i = 0; i <= totalColumns; i++)
            {
                columnWidths[i]   = 0;
                columnXOffsets[i] = 0;
            }

            columnWidths[0]   = leftShift + maxExtraX;
            columnXOffsets[0] = leftShift;

            // Fill each column with the widest accidental in that column
            foreach (var lm in staveLineMetrics)
            {
                if (lm.Width > columnWidths[lm.Column])
                    columnWidths[lm.Column] = lm.Width;
            }

            for (int i = 1; i < columnWidths.Length; i++)
                columnXOffsets[i] = columnWidths[i] + columnXOffsets[i - 1];

            double totalShift = columnXOffsets[columnXOffsets.Length - 1];

            // Assign xShift to each accidental
            int accCount = 0;
            foreach (var lm in staveLineMetrics)
            {
                double lineWidth    = 0;
                int    lastAccOnLine = accCount + lm.NumAcc;
                for (; accCount < lastAccOnLine; accCount++)
                {
                    double xShiftVal = columnXOffsets[lm.Column - 1] + lineWidth + maxExtraX;
                    linePositions[accCount].Acc.SetXShift(xShiftVal);
                    lineWidth += linePositions[accCount].Acc.GetWidth() + accidentalSpacing;
                }
            }

            state.LeftShift = totalShift + additionalPadding;
            return true;
        }

        /// <summary>
        /// Determine whether two stave-line accidental metrics would collide vertically.
        /// Port of Accidental.checkCollision() from accidental.ts.
        /// </summary>
        public static bool CheckCollision(
            StaveLineAccidentalLayoutMetrics line1,
            StaveLineAccidentalLayoutMetrics line2)
        {
            double clearance = line2.Line - line1.Line;
            double clearanceRequired;

            if (clearance > 0)
            {
                // line2 is higher on the page (lower line number = higher pitch)
                clearanceRequired = (line2.FlatLine || line2.DblSharpLine) ? 2.5 : 3.0;
                if (line1.DblSharpLine) clearance -= 0.5;
            }
            else
            {
                // line1 is higher
                clearanceRequired = (line1.FlatLine || line1.DblSharpLine) ? 2.5 : 3.0;
                if (line2.DblSharpLine) clearance -= 0.5;
            }

            return Math.Abs(clearance) < clearanceRequired;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Render the accidental glyph onto the canvas.
        /// Cautionary accidentals are wrapped in parentheses glyphs.
        /// Port of Accidental.draw() from accidental.ts.
        /// </summary>
        public override void Draw()
        {
            var ctx  = CheckContext();
            var note = (Note)GetNote();
            int idx  = GetIndex() ?? 0;

            var start = note.GetModifierStartXY(ModifierPosition.Left, idx);
            double accX = start.X + xShift;
            double accY = start.Y + yShift;

            var (code, parenRightPaddingAdj) = Tables.AccidentalCodes(Type);

            if (!cautionary)
            {
                RenderGlyph(ctx, accX, accY, fontScale, code);
            }
            else
            {
                var (leftCode, _)  = Tables.AccidentalCodes("{");
                var (rightCode, _) = Tables.AccidentalCodes("}");

                // Draw right paren, then accidental, then left paren (right to left)
                RenderGlyph(ctx, accX, accY, fontScale, rightCode);
                accX -= Glyph.GetWidth(rightCode, fontScale);
                accX -= ParenRightPadding;
                accX -= parenRightPaddingAdj;
                RenderGlyph(ctx, accX, accY, fontScale, code);
                accX -= Glyph.GetWidth(code, fontScale);
                accX -= ParenLeftPadding;
                RenderGlyph(ctx, accX, accY, fontScale, leftCode);
            }
        }
    }
}

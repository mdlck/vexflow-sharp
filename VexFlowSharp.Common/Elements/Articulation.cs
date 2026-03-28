// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of vexflow/src/articulation.ts (391 lines)
// Articulation modifier — snap-to-staff positioning, SMuFL glyph rendering.

using System;
using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Articulation modifier — attached to notes to indicate staccato, tenuto,
    /// accent, fermata, marcato, etc.
    ///
    /// The snap-to-staff algorithm ensures articulations sit cleanly in staff
    /// spaces or outside the staff, never overlapping staff lines.
    ///
    /// Port of VexFlow's Articulation class from articulation.ts.
    /// </summary>
    public class Articulation : Modifier
    {
        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>Category string used by ModifierContext to group articulations.</summary>
        public const string CATEGORY = "articulations";

        /// <inheritdoc/>
        public override string GetCategory() => CATEGORY;

        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>Articulation type key (e.g., "a.", "a>", "a@a").</summary>
        public readonly string Type;

        /// <summary>Articulation struct (glyph codes, between_lines flag).</summary>
        protected ArticulationStruct articulation;

        /// <summary>Font scale used for rendering (matches Tables.NOTATION_FONT_SCALE).</summary>
        private readonly double fontScale = Tables.NOTATION_FONT_SCALE;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a new articulation of the given type.
        /// Type must be a key in Tables.ArticulationCodes, or a raw SMuFL glyph code.
        /// Port of Articulation constructor from articulation.ts.
        /// </summary>
        public Articulation(string type)
        {
            Type = type;
            position = ModifierPosition.Above;

            // Resolve articulation struct — fall back to raw glyph code if not found
            if (Tables.ArticulationCodes.TryGetValue(type, out var art))
            {
                articulation = art;
            }
            else
            {
                // Use type as glyph code directly
                articulation = new ArticulationStruct { Code = type, BetweenLines = false };
                if (type.EndsWith("Above", StringComparison.Ordinal)) position = ModifierPosition.Above;
                if (type.EndsWith("Below", StringComparison.Ordinal)) position = ModifierPosition.Below;
            }

            // Compute width from glyph metrics
            var code = GetGlyphCode();
            if (!string.IsNullOrEmpty(code))
            {
                double w = Glyph.GetWidth(code, fontScale);
                if (w > 0) SetWidth(w);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Get the correct SMuFL glyph code for the current position.</summary>
        private string GetGlyphCode()
        {
            if (position == ModifierPosition.Above)
                return articulation.AboveCode ?? articulation.Code ?? "";
            else
                return articulation.BelowCode ?? articulation.Code ?? "";
        }

        // ── snapLineToStaff ───────────────────────────────────────────────────

        /// <summary>
        /// Snap an articulation's staff-line position to the nearest half-integer
        /// (space) or integer (line), then push it outside the staff if required.
        ///
        /// Port of snapLineToStaff() from articulation.ts.
        /// </summary>
        /// <param name="canSitBetweenLines">
        /// True if this articulation may sit between staff lines.
        /// When true, snapping prefers the nearest space inside the staff.
        /// </param>
        /// <param name="line">Current articulation line position.</param>
        /// <param name="position">ABOVE or BELOW (determines direction of snap preference).</param>
        /// <param name="offsetDirection">+1 = moving down (BELOW), -1 = moving up (ABOVE).</param>
        /// <returns>Snapped staff-line position.</returns>
        private static double SnapLineToStaff(bool canSitBetweenLines, double line,
            ModifierPosition position, int offsetDirection)
        {
            // Round to nearest half-integer using ceiling/floor/round based on position
            double snappedLine = RoundToNearestHalf(GetRoundingFunction(line, position), line);
            bool canSnapToStaffSpace = canSitBetweenLines && IsWithinLines(snappedLine, position);
            bool onStaffLine = snappedLine % 1 == 0;

            if (canSnapToStaffSpace && onStaffLine)
            {
                const double halfStaffSpace = 0.5;
                return snappedLine + halfStaffSpace * -offsetDirection;
            }
            return snappedLine;
        }

        /// <summary>
        /// Round a value to the nearest half-integer (0.5 increments)
        /// using the given rounding function.
        /// </summary>
        private static double RoundToNearestHalf(Func<double, double> mathFn, double value)
            => mathFn(value / 0.5) * 0.5;

        /// <summary>
        /// Select the appropriate rounding function for snapping an articulation
        /// to a staff line/space:
        /// - Inside staff, ABOVE: ceil (push up)
        /// - Inside staff, BELOW: floor (push down)
        /// - Outside staff: round (snap to nearest)
        /// Port of getRoundingFunction() from articulation.ts.
        /// </summary>
        private static Func<double, double> GetRoundingFunction(double line, ModifierPosition position)
        {
            if (IsWithinLines(line, position))
            {
                if (position == ModifierPosition.Above)
                    return Math.Ceiling;
                else
                    return Math.Floor;
            }
            return Math.Round;
        }

        /// <summary>
        /// Whether a staff line position is within the staff region for the given placement.
        /// ABOVE: within staff when line is at most 5 (topmost extended line)
        /// BELOW: within staff when line is at least 1 (bottommost extended line)
        /// Port of isWithinLines() from articulation.ts.
        /// </summary>
        private static bool IsWithinLines(double line, ModifierPosition position)
            => position == ModifierPosition.Above ? line <= 5 : line >= 1;

        // ── Format ────────────────────────────────────────────────────────────

        /// <summary>
        /// Arrange articulations inside a ModifierContext.
        /// Increments TopTextLine (above) or TextLine (below) based on articulation height.
        ///
        /// Port of Articulation.format() from articulation.ts.
        /// </summary>
        public static bool Format(List<Articulation> articulations, ModifierContextState state)
        {
            if (articulations == null || articulations.Count == 0) return false;

            const double margin = 0.5;

            // Compute increment (height in line units + margin)
            double GetIncrement(Articulation art, double line, ModifierPosition pos)
            {
                double heightInLines = 1.0; // fallback: 1 line unit
                var code = art.GetGlyphCode();
                if (!string.IsNullOrEmpty(code))
                {
                    // Height approximation: glyph Ha (ascent) in font units * scale / STAVE_LINE_DISTANCE
                    double w = Glyph.GetWidth(code, art.fontScale);
                    // Use width as proxy if Ha unavailable; VexFlow uses actual metric height
                    // which requires a render pass; we approximate with 1.5 line units for most glyphs
                    if (w > 0) heightInLines = 1.5;
                }
                return RoundToNearestHalf(GetRoundingFunction(line, pos), heightInLines + margin);
            }

            foreach (var art in articulations)
            {
                var note     = (Note)art.GetNote();
                bool hasStem = (note is StemmableNote sn0) && sn0.HasStem();
                int stemDir  = (note is StemmableNote sn1) ? sn1.GetStemDirection() : Stem.UP;
                double lines = 5;

                var stave = note.GetStave();
                if (stave != null) lines = stave.GetNumLines();

                if (art.GetPosition() == ModifierPosition.Above)
                {
                    double noteLine = note.GetLineNumber(isTopNote: true);
                    if (hasStem && stemDir == Stem.UP && note is StemmableNote snUp)
                    {
                        try
                        {
                            var extents = snUp.GetStemExtents();
                            noteLine += Math.Abs(extents.TopY - extents.BaseY) / Tables.STAVE_LINE_DISTANCE;
                        }
                        catch { /* no stem built yet — ignore */ }
                    }

                    double increment = GetIncrement(art, state.TopTextLine, ModifierPosition.Above);
                    double curTop    = noteLine + state.TopTextLine + 0.5;
                    if (!art.articulation.BetweenLines && curTop < lines)
                        increment += lines - curTop;

                    art.SetTextLine(state.TopTextLine);
                    state.TopTextLine += increment;
                }
                else if (art.GetPosition() == ModifierPosition.Below)
                {
                    double noteLine = Math.Max(lines - note.GetLineNumber(), 0);
                    if (hasStem && stemDir == Stem.DOWN && note is StemmableNote snDown)
                    {
                        try
                        {
                            var extents = snDown.GetStemExtents();
                            noteLine += Math.Abs(extents.TopY - extents.BaseY) / Tables.STAVE_LINE_DISTANCE;
                        }
                        catch { /* no stem built yet — ignore */ }
                    }

                    double increment = GetIncrement(art, state.TextLine, ModifierPosition.Below);
                    double curBottom = noteLine + state.TextLine + 0.5;
                    if (!art.articulation.BetweenLines && curBottom < lines)
                        increment += lines - curBottom;

                    art.SetTextLine(state.TextLine);
                    state.TextLine += increment;
                }
            }

            return true;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Render the articulation glyph in position next to its attached note.
        /// Applies snap-to-staff positioning to ensure clean placement.
        /// Port of Articulation.draw() from articulation.ts.
        /// </summary>
        public override void Draw()
        {
            var ctx  = CheckContext();
            var note = (Note)GetNote();
            int idx  = GetIndex() ?? 0;

            var stave        = note.CheckStave();
            double staffSpace = stave.GetSpacingBetweenLines();
            var pos          = GetPosition();
            bool canSitBetweenLines = articulation.BetweenLines;

            // X: centered over/under the note head
            var startXY = note.GetModifierStartXY(pos, idx);
            double x    = startXY.X;

            // Determine initial offset based on position vs stem direction
            bool hasStem   = (note is StemmableNote snCheck) && snCheck.HasStem();
            int stemDir    = (note is StemmableNote snDir) ? snDir.GetStemDirection() : Stem.UP;
            bool isOnStemTip = (pos == ModifierPosition.Above && stemDir == Stem.UP)
                            || (pos == ModifierPosition.Below && stemDir == Stem.DOWN);
            double initialOffset = (hasStem && isOnStemTip) ? 0.5 : 1.0;

            double tl = textLine; // text line set by Format()

            double y;
            int offsetDirection;
            if (pos == ModifierPosition.Above)
            {
                double topY;
                if (hasStem && stemDir == Stem.UP && note is StemmableNote snTop)
                {
                    try { topY = snTop.GetStemExtents().TopY; }
                    catch
                    {
                        topY = note.GetYs().Length > 0 ? note.GetYs()[0] : startXY.Y;
                    }
                }
                else
                {
                    double minY = double.MaxValue;
                    foreach (var noteY in note.GetYs())
                        if (noteY < minY) minY = noteY;
                    topY = minY < double.MaxValue ? minY : startXY.Y;
                }
                y = topY - (tl + initialOffset) * staffSpace;
                offsetDirection = -1;
            }
            else
            {
                double botY;
                if (hasStem && stemDir == Stem.DOWN && note is StemmableNote snBot)
                {
                    try { botY = snBot.GetStemExtents().TopY; }
                    catch
                    {
                        botY = note.GetYs().Length > 0 ? note.GetYs()[note.GetYs().Length - 1] : startXY.Y;
                    }
                }
                else
                {
                    double maxY = double.MinValue;
                    foreach (var noteY in note.GetYs())
                        if (noteY > maxY) maxY = noteY;
                    botY = maxY > double.MinValue ? maxY : startXY.Y;
                }
                y = botY + (tl + initialOffset) * staffSpace;
                offsetDirection = +1;
            }

            // Snap to staff
            var kProps      = note.GetKeyProps();
            double noteLine = (idx < kProps.Count) ? kProps[idx].Line : 3.0;
            double[] noteYs = note.GetYs();
            double distanceFromNote = (noteYs.Length > idx)
                ? (noteYs[idx] - y) / staffSpace
                : 0;
            double articLine  = distanceFromNote + noteLine;
            double snappedLine = SnapLineToStaff(canSitBetweenLines, articLine, pos, offsetDirection);
            y += Math.Abs(snappedLine - articLine) * staffSpace * offsetDirection;

            // Render the glyph
            var code = GetGlyphCode();
            if (!string.IsNullOrEmpty(code))
            {
                var g = new Glyph(code, fontScale);
                g.Render(ctx, x, y);
            }
        }
    }
}

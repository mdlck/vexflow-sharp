// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's Tuplet class (tuplet.ts, 423 lines).
// Tuplet draws a bracket and numerator (optionally with ratio) above or below
// a group of notes. Extends Element — positioned relative to its notes, not
// added to a Voice directly.

using System;
using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Location for tuplet bracket: above (Top = 1) or below (Bottom = -1) the notes.
    /// Port of TupletLocation enum from tuplet.ts.
    /// </summary>
    public enum TupletLocation
    {
        Bottom = -1,
        Top    = +1,
    }

    /// <summary>
    /// Options for constructing a Tuplet.
    /// Port of TupletOptions interface from tuplet.ts.
    /// </summary>
    public class TupletOptions
    {
        /// <summary>Deprecated alias for NotesOccupied.</summary>
        public int? BeatsOccupied  { get; set; }

        /// <summary>Whether to draw a bracket. Defaults to true if notes are not beamed.</summary>
        public bool? Bracketed     { get; set; }

        /// <summary>Location: TupletLocation.Top (above) or TupletLocation.Bottom (below).</summary>
        public int? Location       { get; set; }

        /// <summary>Number of notes the tuplet occupies in time (denominator). Default 2.</summary>
        public int? NotesOccupied  { get; set; }

        /// <summary>Number of notes in the tuplet (numerator). Default = notes.Count.</summary>
        public int? NumNotes       { get; set; }

        /// <summary>Whether to show ratio (e.g. 7:8). Auto-enabled when |notesOccupied - numNotes| > 1.</summary>
        public bool? Ratioed       { get; set; }

        /// <summary>Manual y offset in pixels. Defaults to Metrics.GetDouble("Tuplet.yOffset").</summary>
        public double? YOffset     { get; set; }

        /// <summary>Manual text y offset in pixels. Defaults to Metrics.GetDouble("Tuplet.textYOffset").</summary>
        public double? TextYOffset { get; set; }
    }

    /// <summary>
    /// Draws a tuplet bracket and number (optionally ratio) for a group of notes.
    /// Extends Element — not a Tickable.
    ///
    /// Port of VexFlow's Tuplet class from tuplet.ts.
    /// </summary>
    public class Tuplet : Element
    {
        // ── Category ──────────────────────────────────────────────────────────

        public new const string CATEGORY = "Tuplet";
        public override string GetCategory() => CATEGORY;

        // ── Constants ─────────────────────────────────────────────────────────

        /// <summary>Y-offset increment for nested tuplets.</summary>
        public const double NESTING_OFFSET = 15;

        // ── Fields ────────────────────────────────────────────────────────────

        private readonly List<Note> notes;
        private readonly TupletOptions options;

        private int    numNotes;
        private int    notesOccupied;
        private bool   bracketed;
        private int    location;     // TupletLocation: +1 top, -1 bottom
        private bool   ratioed;
        private double point;        // font size for glyph rendering

        private double xPos  = 100;
        private double yPos  = 16;
        private double tupletWidth = 200;
        private const double BeamedTupletYOffset = 8;

        // Pre-resolved glyph lists for numerator and denominator digits
        private readonly List<Glyph> numeratorGlyphs = new List<Glyph>();
        private readonly List<Glyph> denomGlyphs     = new List<Glyph>();

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Construct a Tuplet for the given notes.
        /// Port of Tuplet constructor from tuplet.ts.
        /// </summary>
        /// <param name="notes">Notes in the tuplet group (must be non-empty).</param>
        /// <param name="options">Optional configuration.</param>
        public Tuplet(List<Note> notes, TupletOptions options = null)
        {
            if (notes == null) throw new ArgumentNullException(nameof(notes));
            if (notes.Count == 0)
                throw new VexFlowException("BadArguments", "No notes provided for tuplet.");

            this.notes   = notes;
            this.options = options ?? new TupletOptions();

            numNotes      = this.options.NumNotes      ?? notes.Count;
            notesOccupied = this.options.NotesOccupied ?? this.options.BeatsOccupied ?? 2;

            // Bracketed: if explicitly set, use that; else bracket unless all notes are beamed
            if (this.options.Bracketed.HasValue)
            {
                bracketed = this.options.Bracketed.Value;
            }
            else
            {
                // Default: bracket if any note is NOT beamed
                bracketed = true;
                foreach (var n in notes)
                {
                    if (n is StemmableNote sn && sn.HasBeam())
                    {
                        bracketed = false;
                        // Only clear if ALL notes are beamed; if any is not beamed, bracket
                    }
                }
                // Re-check: bracket = any note is NOT beamed
                bracketed = false;
                foreach (var n in notes)
                {
                    if (!(n is StemmableNote sn2 && sn2.HasBeam()))
                    {
                        bracketed = true;
                        break;
                    }
                }
            }

            // Ratioed: if explicitly set, use that; else enable when |notesOccupied - numNotes| > 1
            ratioed = this.options.Ratioed
                      ?? (Math.Abs(notesOccupied - numNotes) > 1);

            // Font size for digits: NOTATION_FONT_SCALE * 3 / 5
            point = (Tables.NOTATION_FONT_SCALE * 3.0) / 5.0;

            // Location: default to Top
            location = this.options.Location ?? (int)TupletLocation.Top;
            if (location != (int)TupletLocation.Top && location != (int)TupletLocation.Bottom)
                location = (int)TupletLocation.Top;

            ResolveGlyphs();
            Attach();
        }

        // ── Accessors ─────────────────────────────────────────────────────────

        /// <summary>Get the notes in this tuplet group.</summary>
        public List<Note> GetNotes() => notes;

        /// <summary>Get the numerator note count.</summary>
        public int GetNoteCount() => numNotes;

        /// <summary>Get the denominator notes-occupied count.</summary>
        public int GetNotesOccupied() => notesOccupied;

        /// <summary>Get the current tuplet location.</summary>
        public int GetTupletLocation() => location;

        /// <summary>Whether the tuplet bracket is currently drawn.</summary>
        public bool IsBracketed() => bracketed;

        /// <summary>Get the configured y offset, including the v5 metrics default.</summary>
        public double GetYOffset() => options.YOffset ?? Metrics.GetDouble("Tuplet.yOffset");

        /// <summary>Get the configured text y offset, including the v5 metrics default.</summary>
        public double GetTextYOffset() => options.TextYOffset ?? Metrics.GetDouble("Tuplet.textYOffset");

        /// <summary>Set the location (TupletLocation.Top or TupletLocation.Bottom).</summary>
        public Tuplet SetTupletLocation(int loc)
        {
            if (loc != (int)TupletLocation.Top && loc != (int)TupletLocation.Bottom)
                loc = (int)TupletLocation.Top;
            location = loc;
            return this;
        }

        /// <summary>Set whether the bracket is drawn.</summary>
        public Tuplet SetBracketed(bool b) { bracketed = b; return this; }

        /// <summary>Set whether the ratio is shown.</summary>
        public Tuplet SetRatioed(bool r) { ratioed = r; return this; }

        /// <summary>Attach this tuplet to its notes.</summary>
        public void Attach()
        {
            foreach (var note in notes)
                note.SetTuplet(this);
        }

        /// <summary>Detach this tuplet from its notes.</summary>
        public void Detach()
        {
            foreach (var note in notes)
                note.ResetTuplet(this);
        }

        /// <summary>Update the denominator note count and refresh note attachments.</summary>
        public void SetNotesOccupied(int notesOccupied)
        {
            Detach();
            this.notesOccupied = notesOccupied;
            ResolveGlyphs();
            Attach();
        }

        /// <summary>Count tuplets nested between this tuplet and its notes on the same side.</summary>
        public int GetNestedTupletCount()
        {
            int maxOffset = 0;
            foreach (var note in notes)
            {
                var sameSide = note.GetTupletStack()
                    .FindAll(tuplet => tuplet.location == location);
                int index = sameSide.IndexOf(this);
                if (index > maxOffset) maxOffset = index;
            }
            return maxOffset;
        }

        // ── Glyph resolution ──────────────────────────────────────────────────

        /// <summary>
        /// Resolve digit glyphs for the numerator and denominator.
        /// Uses "timeSig0" through "timeSig9" SMuFL codes.
        /// Port of Tuplet.resolveGlyphs() from tuplet.ts.
        /// </summary>
        private void ResolveGlyphs()
        {
            numeratorGlyphs.Clear();
            int n = numNotes;
            while (n >= 1)
            {
                numeratorGlyphs.Insert(0, new Glyph("timeSig" + (n % 10), point));
                n /= 10;
            }

            denomGlyphs.Clear();
            n = notesOccupied;
            while (n >= 1)
            {
                denomGlyphs.Insert(0, new Glyph("timeSig" + (n % 10), point));
                n /= 10;
            }
        }

        // ── Y position ────────────────────────────────────────────────────────

        /// <summary>
        /// Compute the y position for the tuplet bracket/number.
        /// Port of Tuplet.getYPosition() from tuplet.ts.
        /// Simplified: uses stave line 0 for top, stave line 4 for bottom,
        /// adjusting for stem extents when available.
        /// </summary>
        private double GetYPosition()
        {
            var firstNote = notes[0];
            double yBase;
            double nestedTupletYOffset = GetNestedTupletCount() * NESTING_OFFSET * -location;

            if (location == (int)TupletLocation.Top)
            {
                // Default: top of stave minus offset
                yBase = firstNote.GetStave() != null
                    ? firstNote.CheckStave().GetYForLine(0) - 15
                    : yPos;

                // Adjust for stem extents to avoid collisions
                foreach (var note in notes)
                {
                    if (note is StemmableNote sn && (sn.HasStem() || sn.IsRest()))
                    {
                        double topY;
                        var extents = sn.GetStemExtents();
                        topY = sn.GetStemDirection() == Stem.UP
                            ? extents.TopY  - 5
                            : extents.BaseY - 5;
                        yBase = Math.Min(topY, yBase);
                    }
                }

                if (!bracketed)
                {
                    yBase -= BeamedTupletYOffset;
                }
            }
            else
            {
                // Bottom: use stave line 4 (bottom line) plus offset
                yBase = firstNote.GetStave() != null
                    ? firstNote.CheckStave().GetYForLine(4) + 20
                    : yPos;

                // Adjust for stem extents
                foreach (var note in notes)
                {
                    if (note is StemmableNote sn && (sn.HasStem() || sn.IsRest()))
                    {
                        double botY;
                        var extents = sn.GetStemExtents();
                        botY = sn.GetStemDirection() == Stem.UP
                            ? extents.BaseY + 5
                            : extents.TopY  + 5;
                        if (botY > yBase) yBase = botY;
                    }
                }

                if (!bracketed)
                {
                    yBase += BeamedTupletYOffset;
                }
            }

            return yBase + nestedTupletYOffset + GetYOffset();
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw the tuplet bracket and number onto the render context.
        /// Port of Tuplet.draw() from tuplet.ts.
        /// </summary>
        public override void Draw()
        {
            var ctx = CheckContext();
            rendered = true;
            double bracketPadding = Metrics.GetDouble("Tuplet.bracket.padding");
            double bracketThickness = Metrics.GetDouble("Tuplet.bracket.lineWidth");
            double bracketLegLength = Metrics.GetDouble("Tuplet.bracket.legLength");

            var firstNote = notes[0] as StemmableNote;
            var lastNote  = notes[notes.Count - 1] as StemmableNote;

            // Determine x extents
            if (!bracketed && firstNote != null && lastNote != null)
            {
                xPos       = firstNote.GetStemX();
                tupletWidth = lastNote.GetStemX() - xPos;
            }
            else if (firstNote != null && lastNote != null)
            {
                xPos       = firstNote.GetTieLeftX() - bracketPadding;
                tupletWidth = lastNote.GetTieRightX() - xPos + bracketPadding;
                boundingBox = new BoundingBox(xPos, yPos, tupletWidth + 1, 10);
            }
            else
            {
                // Fallback: use absolute x of first/last note
                xPos       = notes[0].GetAbsoluteX();
                tupletWidth = notes[notes.Count - 1].GetAbsoluteX() + notes[notes.Count - 1].GetWidth() - xPos;
            }

            // Determine y position
            yPos = GetYPosition();
            boundingBox = new BoundingBox(xPos, location == (int)TupletLocation.Top ? yPos : yPos - 9, tupletWidth + (bracketed ? bracketThickness : 0), 10);

            // Calculate visual glyph bounds for centering. Glyph render origins are not
            // necessarily their visual left edges, so account for XMin/XMax bearings.
            var glyphSpan = CalculateGlyphSpan();
            double glyphWidth = glyphSpan.Width;

            double centerX        = xPos + tupletWidth / 2;
            double notationStartX = centerX - glyphSpan.Center;

            // Draw bracket (filled rectangles like VexFlow)
            if (bracketed)
            {
                double lineWidth = tupletWidth / 2 - glyphWidth / 2 - bracketPadding;
                if (lineWidth > 0)
                {
                    // Left segment of horizontal bracket
                    ctx.FillRect(xPos, yPos, lineWidth, bracketThickness);
                    // Right segment of horizontal bracket
                    ctx.FillRect(xPos + tupletWidth / 2 + glyphWidth / 2 + bracketPadding, yPos, lineWidth, bracketThickness);
                    // Left vertical tick
                    double vertOffset = location == (int)TupletLocation.Bottom ? bracketThickness : 0;
                    ctx.FillRect(xPos,              yPos + vertOffset, bracketThickness, location * bracketLegLength);
                    ctx.FillRect(xPos + tupletWidth, yPos + vertOffset, bracketThickness, location * bracketLegLength);
                }
            }

            // Draw numerator glyphs
            double xOffset = 0;
            double textOffsetDirection = location == (int)TupletLocation.Top ? -1 : 1;
            double shiftY  = point / 3 + textOffsetDirection * GetTextYOffset();
            foreach (var glyph in numeratorGlyphs)
            {
                var metrics = glyph.GetMetrics();
                glyph.Render(ctx, notationStartX + xOffset, yPos + shiftY);
                xOffset += metrics.Width;
            }

            // Draw ratio colon + denominator if ratioed
            if (ratioed)
            {
                double colonX      = notationStartX + xOffset + point * 0.16;
                double colonRadius = point * 0.06;

                ctx.BeginPath();
                ctx.Arc(colonX, yPos - point * 0.08, colonRadius, 0, Math.PI * 2, false);
                ctx.ClosePath();
                ctx.Fill();

                ctx.BeginPath();
                ctx.Arc(colonX, yPos + point * 0.12, colonRadius, 0, Math.PI * 2, false);
                ctx.ClosePath();
                ctx.Fill();

                xOffset += point * 0.32;
                foreach (var glyph in denomGlyphs)
                {
                    var metrics = glyph.GetMetrics();
                    glyph.Render(ctx, notationStartX + xOffset, yPos + shiftY);
                    xOffset += metrics.Width;
                }
            }

            DrawPointerRect();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private (double Left, double Right, double Width, double Center) CalculateGlyphSpan()
        {
            double cursor = 0;
            double left = double.PositiveInfinity;
            double right = double.NegativeInfinity;

            AddGlyphSpan(numeratorGlyphs, ref cursor, ref left, ref right);
            if (ratioed)
            {
                cursor += point * 0.32;
                AddGlyphSpan(denomGlyphs, ref cursor, ref left, ref right);
            }

            if (double.IsInfinity(left) || double.IsInfinity(right))
            {
                double width = SumGlyphWidths(numeratorGlyphs)
                    + (ratioed ? SumGlyphWidths(denomGlyphs) + point * 0.32 : 0);
                return (0, width, width, width / 2);
            }

            return (left, right, right - left, (left + right) / 2);
        }

        private static void AddGlyphSpan(List<Glyph> glyphs, ref double cursor, ref double left, ref double right)
        {
            foreach (var glyph in glyphs)
            {
                var metrics = glyph.GetMetrics();
                if (metrics != null)
                {
                    left = Math.Min(left, cursor + metrics.XMin);
                    right = Math.Max(right, cursor + metrics.XMax);
                    cursor += metrics.Width;
                }
            }
        }

        private static double SumGlyphWidths(List<Glyph> glyphs)
        {
            double total = 0;
            foreach (var g in glyphs)
                total += g.GetMetrics().Width;
            return total;
        }

        public override BoundingBox GetBoundingBox()
        {
            if (boundingBox != null) return boundingBox;
            var glyphSpan = CalculateGlyphSpan();
            return new BoundingBox(xPos, yPos - 10, Math.Max(tupletWidth, glyphSpan.Width), 10);
        }
    }
}

#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's GraceNote class (gracenote.ts, 182 lines).
// GraceNote renders at 0.66 scale with LEDGER_LINE_OFFSET=2.

namespace VexFlowSharp
{
    /// <summary>
    /// Input structure for constructing a GraceNote.
    /// Extends StaveNoteStruct with an optional slash flag.
    /// Port of VexFlow's GraceNoteStruct interface from gracenote.ts.
    /// </summary>
    public class GraceNoteStruct : StaveNoteStruct
    {
        /// <summary>Whether to draw a slash through the stem (acciaccatura).</summary>
        public bool? Slash { get; set; }
    }

    /// <summary>
    /// A grace note rendered at 0.66 scale with shorter ledger lines.
    ///
    /// Grace notes are small decorative notes that precede the main note.
    /// They are constructed with a reduced glyph font scale and narrower
    /// ledger line stroke.
    ///
    /// Port of VexFlow's GraceNote class from gracenote.ts.
    /// </summary>
    public class GraceNote : StaveNote
    {
        // ── Constants ──────────────────────────────────────────────────────────

        /// <summary>
        /// Scale factor for grace note rendering (0.66 = 2/3 of standard size).
        /// VexFlow: GraceNote.SCALE = 0.66
        /// </summary>
        public const double SCALE = 0.66;

        /// <summary>
        /// Ledger line extension offset for grace notes (shorter than standard).
        /// VexFlow: GraceNote.LEDGER_LINE_OFFSET = 2
        /// </summary>
        public new const int LEDGER_LINE_OFFSET = 2;

        // ── Fields ─────────────────────────────────────────────────────────────

        /// <summary>Whether to draw a slash through the stem (acciaccatura).</summary>
        protected bool slash;

        /// <summary>Whether to draw a slur from this grace note to the main note.</summary>
        protected bool slur;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Construct a GraceNote from a GraceNoteStruct.
        ///
        /// Sets glyph font scale to 39 * 0.66 = 25.74 and ledger line stroke to 2.
        /// Port of GraceNote constructor from gracenote.ts.
        /// </summary>
        public GraceNote(GraceNoteStruct noteStruct)
            : base(new StaveNoteStruct
            {
                Duration = noteStruct.Duration,
                Keys = noteStruct.Keys,
                Type = noteStruct.Type,
                Dots = noteStruct.Dots,
                AutoStem = noteStruct.AutoStem,
                StemDirection = noteStruct.StemDirection,
                Clef = noteStruct.Clef,
                OctaveShift = noteStruct.OctaveShift,
                GlyphFontScale = Tables.NOTATION_FONT_SCALE * SCALE,
                StrokePx = LEDGER_LINE_OFFSET,
            })
        {
            slash = noteStruct.Slash ?? false;
            slur = true;
            width = 3;
        }

        // ── Scale override ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns 0.66 — grace notes render at 2/3 of standard size.
        /// Port of GraceNote.getStaveNoteScale() from gracenote.ts.
        /// </summary>
        public override double GetStaveNoteScale() => SCALE;

        // ── Draw override ──────────────────────────────────────────────────────

        /// <summary>
        /// Draw the grace note. Calls base StaveNote.Draw(), then if slash=true,
        /// draws a small diagonal line across the stem (acciaccatura slash).
        /// Port of GraceNote.draw() from gracenote.ts.
        /// </summary>
        public override void Draw()
        {
            base.Draw();
            rendered = true;

            if (slash && stem != null)
            {
                DrawSlash();
            }
        }

        /// <summary>
        /// Draw the acciaccatura slash — a diagonal line through the stem.
        ///
        /// For stem-up notes, the slash goes from lower-left to upper-right.
        /// For stem-down notes, the slash goes from upper-left to lower-right.
        /// Port of GraceNote.draw() slash section from gracenote.ts.
        /// </summary>
        private void DrawSlash()
        {
            var ctx = CheckContext();
            double staveNoteScale = GetStaveNoteScale();
            double offsetScale = staveNoteScale / 0.66;

            int stemDirection = GetStemDirection();
            var noteHeadBounds = GetNoteHeadBounds();
            double noteStemHeight = stem!.GetHeight();
            double x = GetAbsoluteX();

            // Compute y starting point based on stem direction
            double y = stemDirection == Stem.DOWN
                ? noteHeadBounds.YTop - noteStemHeight
                : noteHeadBounds.YBottom - noteStemHeight;

            double defaultStemExtension = stemDirection == Stem.DOWN
                ? glyphProps.StemDownExtension
                : glyphProps.StemUpExtension;

            double defaultOffsetY = Tables.STEM_HEIGHT;
            defaultOffsetY -= defaultOffsetY / 2.8;
            defaultOffsetY += defaultStemExtension;
            y += defaultOffsetY * staveNoteScale * stemDirection;

            // Slash offsets (magic numbers based on scale 0.66)
            double x1Offset, y1Offset, x2Offset, y2Offset;
            if (stemDirection == Stem.UP)
            {
                x1Offset = 1;
                y1Offset = 0;
                x2Offset = 13;
                y2Offset = -9;
            }
            else
            {
                x1Offset = -4;
                y1Offset = 1;
                x2Offset = 13;
                y2Offset = 9;
            }

            double x1 = x + x1Offset * offsetScale;
            double y1 = y + y1Offset * offsetScale;
            double x2 = x1 + x2Offset * offsetScale;
            double y2 = y1 + y2Offset * offsetScale;

            ctx.Save();
            ctx.SetLineWidth(1 * offsetScale);
            ctx.BeginPath();
            ctx.MoveTo(x1, y1);
            ctx.LineTo(x2, y2);
            ctx.ClosePath();
            ctx.Stroke();
            ctx.Restore();
        }
    }
}

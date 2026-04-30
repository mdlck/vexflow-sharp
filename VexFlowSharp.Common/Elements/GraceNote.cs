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
        public new const string CATEGORY = "GraceNote";

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

        public override string GetCategory() => CATEGORY;

        public override double GetStemX()
        {
            if (GetNoteType() == "r")
                return GetAbsoluteX() + xShift + GetRenderedNoteHeadWidth() / 2.0;

            double xBegin = GetAbsoluteX() + xShift;
            double xEnd = xBegin + GetRenderedNoteHeadWidth();
            double stemX = GetStemDirection() == Stem.DOWN ? xBegin : xEnd;
            return stemX + Stem.WIDTH / (2.0 * -GetStemDirection());
        }

        private double GetRenderedNoteHeadWidth()
        {
            if (_noteHeads.Count > 0 && _noteHeads[0] != null)
            {
                double width = Glyph.GetWidth(_noteHeads[0].GetGlyphCode(), renderOptions.GlyphFontScale);
                if (width > 0) return width;
            }

            return glyphProps.HeadWidth * GetStaveNoteScale();
        }

        public override double GetStemExtension()
        {
            if (stemExtensionOverride.HasValue)
                return stemExtensionOverride.Value;

            double ret = base.GetStemExtension();
            double scale = GetStaveNoteScale();
            return Stem.HEIGHT * scale - Stem.HEIGHT + ret;
        }

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
            var slashBBox = beam != null
                ? CalcBeamedNotesSlashBBox(8 * staveNoteScale, 8 * staveNoteScale, 6 * staveNoteScale, 5 * staveNoteScale)
                : CalcUnbeamedSlashBBox(staveNoteScale);

            ctx.Save();
            ctx.SetLineWidth(1 * staveNoteScale);
            ctx.BeginPath();
            ctx.MoveTo(slashBBox.X1, slashBBox.Y1);
            ctx.LineTo(slashBBox.X2, slashBBox.Y2);
            ctx.ClosePath();
            ctx.Stroke();
            ctx.Restore();
        }

        private (double X1, double Y1, double X2, double Y2) CalcUnbeamedSlashBBox(double scale)
        {
            int stemDirection = GetStemDirection();
            var noteHeadBounds = GetNoteHeadBounds();
            double noteHeadWidth = GetRenderedNoteHeadWidth();
            double x = stemDirection == Stem.DOWN ? GetAbsoluteX() : GetAbsoluteX() + noteHeadWidth;
            double defaultOffsetY = Tables.STEM_HEIGHT * scale / 2.0;
            double y = stemDirection == Stem.DOWN
                ? noteHeadBounds.YBottom + defaultOffsetY
                : noteHeadBounds.YTop - defaultOffsetY;

            return stemDirection == Stem.DOWN
                ? (x - noteHeadWidth, y - noteHeadWidth, x + noteHeadWidth, y + noteHeadWidth)
                : (x - noteHeadWidth, y + noteHeadWidth, x + noteHeadWidth, y - noteHeadWidth);
        }

        private (double X1, double Y1, double X2, double Y2) CalcBeamedNotesSlashBBox(
            double slashStemOffset,
            double slashBeamOffset,
            double protrusionStem,
            double protrusionBeam)
        {
            if (beam == null)
                throw new VexFlowException("NoBeam", "Can't calculate without a beam.");

            if (!beam.PostFormatted)
                beam.PostFormat();

            double beamSlope = beam.Slope;
            bool isBeamEndNote = beam.Notes[beam.Notes.Count - 1] == this;
            int scaleX = isBeamEndNote ? -1 : 1;
            double beamAngle = System.Math.Atan(beamSlope * scaleX);

            double iPointDx = System.Math.Cos(beamAngle) * slashBeamOffset;
            double iPointDy = System.Math.Sin(beamAngle) * slashBeamOffset;

            slashStemOffset *= GetStemDirection();
            double slashAngle = System.Math.Atan((iPointDy - slashStemOffset) / iPointDx);
            double protrusionStemDeltaX = System.Math.Cos(slashAngle) * protrusionStem * scaleX;
            double protrusionStemDeltaY = System.Math.Sin(slashAngle) * protrusionStem;
            double protrusionBeamDeltaX = System.Math.Cos(slashAngle) * protrusionBeam * scaleX;
            double protrusionBeamDeltaY = System.Math.Sin(slashAngle) * protrusionBeam;

            double stemX = GetStemX();
            double firstStemX = beam.Notes[0].GetStemX();
            double stemY = beam.GetBeamYToDraw() + (stemX - firstStemX) * beamSlope;

            return (
                stemX - protrusionStemDeltaX,
                stemY + slashStemOffset - protrusionStemDeltaY,
                stemX + iPointDx * scaleX + protrusionBeamDeltaX,
                stemY + iPointDy + protrusionBeamDeltaY
            );
        }
    }
}

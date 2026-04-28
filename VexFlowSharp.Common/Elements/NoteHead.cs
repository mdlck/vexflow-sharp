#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    /// <summary>
    /// Input structure for constructing a NoteHead.
    /// Port of VexFlow's NoteHeadStruct interface from notehead.ts.
    /// </summary>
    public class NoteHeadStruct
    {
        /// <summary>Duration string (e.g., "4", "8").</summary>
        public string Duration { get; set; } = "4";

        /// <summary>Note type string (e.g., "n", "r", "x").</summary>
        public string NoteType { get; set; } = "n";

        /// <summary>Staff line number (0 = first line, 0.5 = first space).</summary>
        public double Line { get; set; }

        /// <summary>X coordinate in screen pixels.</summary>
        public double X { get; set; }

        /// <summary>Y coordinate in screen pixels.</summary>
        public double Y { get; set; }

        /// <summary>Optional custom glyph code override.</summary>
        public string? CustomGlyphCode { get; set; }

        /// <summary>Glyph font scale (default: Tables.NOTATION_FONT_SCALE).</summary>
        public double GlyphFontScale { get; set; } = Tables.NOTATION_FONT_SCALE;

        /// <summary>Whether this notehead is displaced (chord collision).</summary>
        public bool Displaced { get; set; }

        /// <summary>Whether this is a rest notehead.</summary>
        public bool IsRest { get; set; }

        /// <summary>Custom element style override.</summary>
        public ElementStyle? CustomStyle { get; set; }

        /// <summary>Stem direction (Stem.UP or Stem.DOWN).</summary>
        public int StemDirection { get; set; } = Stem.UP;

        /// <summary>X shift applied to this notehead.</summary>
        public double XShift { get; set; }

        /// <summary>Optional stem up x offset for custom glyphs.</summary>
        public double StemUpXOffset { get; set; }

        /// <summary>Optional stem down x offset for custom glyphs.</summary>
        public double StemDownXOffset { get; set; }
    }

    /// <summary>
    /// Renders a single notehead glyph at a specified staff position.
    /// Phase 2 implementation: extends Element directly.
    /// Port of VexFlow's NoteHead class from notehead.ts.
    /// </summary>
    public class NoteHead : Element
    {
        public new const string CATEGORY = "NoteHead";

        protected double line;
        protected double x;
        protected double y;
        protected string glyphCode;
        protected Glyph? glyph;
        protected double glyphFontScale;
        protected bool displaced;
        protected string noteType;
        protected string duration;
        protected bool isRest;
        protected ElementStyle? customStyle;
        protected int stemDirection;
        protected double xShift;
        protected bool customGlyph;
        protected double stemUpXOffset;
        protected double stemDownXOffset;
        protected GlyphProps glyphProps;

        public override string GetCategory() => CATEGORY;

        /// <summary>
        /// Construct a NoteHead from the given struct.
        /// </summary>
        public NoteHead(NoteHeadStruct noteHeadStruct)
        {
            duration = Tables.SanitizeDuration(noteHeadStruct.Duration);
            noteType = noteHeadStruct.NoteType;
            line = noteHeadStruct.Line;
            x = noteHeadStruct.X;
            y = noteHeadStruct.Y;
            displaced = noteHeadStruct.Displaced;
            isRest = noteHeadStruct.IsRest;
            customStyle = noteHeadStruct.CustomStyle;
            stemDirection = noteHeadStruct.StemDirection;
            xShift = noteHeadStruct.XShift;
            glyphFontScale = noteHeadStruct.GlyphFontScale > 0
                ? noteHeadStruct.GlyphFontScale
                : Tables.NOTATION_FONT_SCALE;

            // Get glyph properties for this duration and note type
            glyphProps = Tables.GetGlyphProps(duration, noteType);

            // Determine glyph code
            if (noteHeadStruct.CustomGlyphCode != null)
            {
                customGlyph = true;
                glyphCode = noteHeadStruct.CustomGlyphCode;
                stemUpXOffset = noteHeadStruct.StemUpXOffset;
                stemDownXOffset = noteHeadStruct.StemDownXOffset;
            }
            else
            {
                customGlyph = false;
                // Use code from glyphProps if available, otherwise use the code_head
                glyphCode = !string.IsNullOrEmpty(glyphProps.Code)
                    ? glyphProps.Code
                    : (!string.IsNullOrEmpty(glyphProps.CodeHead) ? glyphProps.CodeHead : "noteheadBlack");
                stemUpXOffset = 0;
                stemDownXOffset = 0;
            }

            // Try to create the Glyph object (may fail if font not loaded — tolerate for tests)
            try
            {
                glyph = new Glyph(glyphCode, glyphFontScale);
            }
            catch
            {
                glyph = null;
            }
        }

        /// <summary>Get the staff line number for this notehead.</summary>
        public double GetLine() => line;

        /// <summary>Set the staff line number.</summary>
        public NoteHead SetLine(double newLine)
        {
            line = newLine;
            return this;
        }

        /// <summary>Get the X coordinate.</summary>
        public double GetX() => x;

        /// <summary>Set the X coordinate.</summary>
        public NoteHead SetX(double newX)
        {
            x = newX;
            return this;
        }

        /// <summary>Get the Y coordinate.</summary>
        public double GetY() => y;

        /// <summary>Set the Y coordinate.</summary>
        public NoteHead SetY(double newY)
        {
            y = newY;
            return this;
        }

        /// <summary>
        /// Attach this notehead to a stave.
        /// Computes y from stave.GetYForNote(line) and inherits the stave's render context.
        /// Port of NoteHead.setStave() from notehead.ts.
        /// </summary>
        public NoteHead SetStave(Stave stave)
        {
            SetY(stave.GetYForNote(line));
            try
            {
                SetContext(stave.CheckContext());
            }
            catch
            {
                // Stave has no context yet — y is still set correctly; draw will set context later
            }
            return this;
        }

        /// <summary>Get the glyph code for this notehead.</summary>
        public string GetGlyphCode() => glyphCode;

        /// <summary>Get the SMuFL glyph rendered by this notehead.</summary>
        public string GetText() => glyphCode;

        /// <summary>Get glyph text metrics used by VexFlow 5 layout calculations.</summary>
        public GlyphMetrics GetTextMetrics()
        {
            var metrics = glyph?.GetMetrics();
            if (metrics != null) return metrics;

            double width = GetWidth();
            return new GlyphMetrics
            {
                Width = width,
                Height = Tables.STAVE_LINE_DISTANCE,
                ActualBoundingBoxAscent = Tables.STAVE_LINE_DISTANCE / 2.0,
                ActualBoundingBoxDescent = Tables.STAVE_LINE_DISTANCE / 2.0,
                XMin = 0,
                XMax = width,
                Scale = 1,
            };
        }

        /// <summary>Whether this notehead is displaced.</summary>
        public bool IsDisplaced() => displaced;

        /// <summary>Set the displaced flag.</summary>
        public NoteHead SetDisplaced(bool d)
        {
            displaced = d;
            return this;
        }

        /// <summary>
        /// Get the width of this notehead from the Glyph metrics.
        /// Returns glyphProps.HeadWidth if no glyph is available.
        /// </summary>
        public double GetWidth()
        {
            var metrics = glyph?.GetMetrics();
            if (metrics != null && metrics.Width > 0)
                return metrics.Width;

            return glyphProps.HeadWidth > 0 ? glyphProps.HeadWidth : 8.0;
        }

        /// <summary>Get the absolute X coordinate where the glyph is rendered.</summary>
        public double GetAbsoluteX()
        {
            var displacementStemAdjustment = Stem.WIDTH / 2.0;
            var displacement = displaced
                ? (GetWidth() - displacementStemAdjustment) * stemDirection
                : 0;
            return x + xShift + displacement;
        }

        /// <summary>Return a bounding box for this notehead glyph.</summary>
        public override BoundingBox? GetBoundingBox()
        {
            double headX = GetAbsoluteX();
            if (glyph != null)
            {
                var metrics = glyph.GetMetrics();
                if (metrics != null)
                {
                    return new BoundingBox(
                        headX + metrics.XMin,
                        y - metrics.Height,
                        metrics.Width,
                        metrics.Height);
                }
            }

            double width = GetWidth();
            return new BoundingBox(
                headX,
                y - Tables.STAVE_LINE_DISTANCE / 2.0,
                width,
                Tables.STAVE_LINE_DISTANCE);
        }

        /// <summary>
        /// Draw the notehead glyph at (x, y).
        /// Port of NoteHead.draw() from notehead.ts.
        /// </summary>
        public override void Draw()
        {
            var ctx = CheckContext();
            rendered = true;

            double headX = GetAbsoluteX();

            ctx.Save();
            if (customStyle != null) ApplyStyle(customStyle);
            else ApplyStyle();

            if (glyph != null)
            {
                glyph.SetContext(ctx);
                glyph.Render(ctx, headX, y);
            }

            if (customStyle != null) RestoreStyle(customStyle);
            else RestoreStyle();
            ctx.Restore();
        }
    }
}

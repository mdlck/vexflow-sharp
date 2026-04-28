#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;

namespace VexFlowSharp
{
    /// <summary>
    /// Outline command codes matching VexFlow's OutlineCode enum from glyph.ts.
    /// </summary>
    public enum OutlineCode
    {
        Move = 0,
        Line = 1,
        Quadratic = 2,
        Bezier = 3,
    }

    /// <summary>
    /// Metrics returned after rendering a glyph.
    /// </summary>
    public class GlyphMetrics
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double ActualBoundingBoxAscent { get; set; }
        public double ActualBoundingBoxDescent { get; set; }
        public double XMin { get; set; }
        public double XMax { get; set; }
        public double XShift { get; set; }
        public double YShift { get; set; }
        public double Scale { get; set; }
        public int[]? Outline { get; set; }
    }

    /// <summary>
    /// Renders a single SMuFL glyph as a filled vector path via RenderContext.
    /// Port of VexFlow's Glyph class from glyph.ts.
    ///
    /// CRITICAL CORRECTNESS REQUIREMENTS (REND-05, FONT-02, FONT-03):
    ///   - Y-inversion: yPos - outlineY * scale  (font Y increases up; screen Y increases down)
    ///   - Bezier outline encodes endpoint FIRST, then control points — walker reads endpoint,
    ///     then passes (cp1, cp2, endpoint) to BezierCurveTo.
    ///   - Glyph outlines are ALWAYS rendered as filled paths; NEVER via FillText.
    /// </summary>
    public class Glyph : Element
    {
        public new const string CATEGORY = "Glyph";

        private const int RENDER_PRECISION_PLACES = 3;
        private static readonly double _precision = Math.Pow(10, RENDER_PRECISION_PLACES); // 1000.0

        private readonly string _code;
        private readonly double _scale;
        private readonly FontGlyph? _metrics;

        /// <summary>
        /// Y-shift applied when rendering this glyph (set by StaveModifier.PlaceGlyphOnLine).
        /// </summary>
        private double _yShift = 0;

        /// <summary>Set the y-shift. Returns this for fluent chaining.</summary>
        public Glyph SetYShift(double yShift) { _yShift = yShift; return this; }

        /// <summary>
        /// Construct a Glyph for the given SMuFL glyph code at the given point size.
        /// </summary>
        /// <param name="code">Glyph name key (e.g., "gClef", "noteheadBlack").</param>
        /// <param name="point">Point size. Used to compute scale = point / fontData.Resolution.</param>
        /// <param name="fontData">
        /// Font data to look up the glyph in. If null, uses the active VexFlow font stack.
        /// </param>
        public Glyph(string code, double point, FontData? fontData = null)
        {
            _code = code;
            var data = fontData ?? (Font.HasAnyFonts() ? Font.ResolveGlyphFontData(code) : BravuraGlyphs.Data);
            _scale = GetScale(point, data);
            data.Glyphs.TryGetValue(code, out _metrics);
        }

        public override string GetCategory() => CATEGORY;

        public static double GetScale(double point, FontData data)
        {
            return point * data.GlyphScale / data.Resolution;
        }

        /// <summary>
        /// Walk the pre-parsed integer outline array and draw a filled path via the RenderContext.
        ///
        /// Y-inversion formula: <c>yPos - outline[i] * scale</c> — applied inside NextY().
        /// Bezier: outline stores [3, endX, endY, cp1X, cp1Y, cp2X, cp2Y]; walker reads endX/endY
        /// first, then passes (cp1, cp2, end) to BezierCurveTo — control points before endpoint.
        /// </summary>
        /// <param name="ctx">Render context to draw into.</param>
        /// <param name="outline">Pre-parsed int[] outline array from FontGlyph.CachedOutline.</param>
        /// <param name="scale">Scale factor from <see cref="GetScale(double, FontData)"/>.</param>
        /// <param name="xPos">X origin in screen coordinates.</param>
        /// <param name="yPos">Y origin in screen coordinates (screen-space, Y increases down).</param>
        public static void RenderOutline(RenderContext ctx, int[] outline, double scale, double xPos, double yPos)
        {
            int i = 0;

            double NextX() => Math.Round((xPos + outline[i++] * scale) * _precision) / _precision;
            // CRITICAL Y-INVERSION: yPos - (not +) to flip from font coordinates to screen coordinates
            double NextY() => Math.Round((yPos - outline[i++] * scale) * _precision) / _precision;

            ctx.BeginPath();

            while (i < outline.Length)
            {
                int code = outline[i++];
                switch (code)
                {
                    case (int)OutlineCode.Move:
                        ctx.MoveTo(NextX(), NextY());
                        break;

                    case (int)OutlineCode.Line:
                        ctx.LineTo(NextX(), NextY());
                        break;

                    case (int)OutlineCode.Quadratic:
                        // Outline stores endpoint BEFORE control point: [2, endX, endY, cpX, cpY]
                        // QuadraticCurveTo expects: (cpx, cpy, endX, endY) — control point FIRST
                        double qEndX = NextX(), qEndY = NextY();
                        ctx.QuadraticCurveTo(NextX(), NextY(), qEndX, qEndY);
                        break;

                    case (int)OutlineCode.Bezier:
                        // Outline stores endpoint BEFORE control points: [3, endX, endY, cp1X, cp1Y, cp2X, cp2Y]
                        // BezierCurveTo expects: (cp1x, cp1y, cp2x, cp2y, endX, endY) — control points FIRST
                        double bEndX = NextX(), bEndY = NextY();
                        ctx.BezierCurveTo(NextX(), NextY(), NextX(), NextY(), bEndX, bEndY);
                        break;
                }
            }

            // Glyph outlines are always filled, never stroked (REND-05)
            ctx.Fill();
        }

        /// <summary>
        /// Compute the rendered width of a glyph at the given point size.
        ///
        /// Port of VexFlow's Glyph.getWidth(code, point, category) from glyph.ts.
        /// Legacy VexFlow outline tables compute: bbox.getW() * (point * 72) / (resolution * 100).
        /// Generated raw OTF outline tables compute: bbox.getW() * point / resolution.
        ///
        /// The font data's GlyphScale selects the correct formula for its outline coordinate space.
        /// The bbox width is approximated by scanning all x values in the outline.
        /// </summary>
        /// <param name="code">Glyph name key (e.g., "gClef").</param>
        /// <param name="point">Point size (e.g., Tables.NOTATION_FONT_SCALE = 39).</param>
        /// <param name="fontData">Font data to use (defaults to the active VexFlow font stack).</param>
        public static double GetWidth(string code, double point, FontData? fontData = null)
        {
            var data = fontData ?? (Font.HasAnyFonts() ? Font.ResolveGlyphFontData(code) : BravuraGlyphs.Data);
            if (!data.Glyphs.TryGetValue(code, out var fg) || fg.CachedOutline == null)
            {
                // Fallback: use nominal XMax-XMin with VexFlow scale
                return 0.0;
            }

            double scale = GetScale(point, data);

            // Scan all x coordinates in the outline to find the rendered bounding box.
            // The outline format: [cmd, x, y, ...] where cmd 0=move, 1=line, 3=bezier.
            // For bezier: [3, endX, endY, cp1X, cp1Y, cp2X, cp2Y] — all x values contribute.
            double maxX = double.NegativeInfinity;
            double minX = double.PositiveInfinity;

            int i = 0;
            var outline = fg.CachedOutline;
            while (i < outline.Length)
            {
                int cmd = outline[i++];
                switch (cmd)
                {
                    case 0: // Move: x, y
                    case 1: // Line: x, y
                        if (i + 1 < outline.Length)
                        {
                            double x = outline[i++];
                            i++; // skip y
                            if (x > maxX) maxX = x;
                            if (x < minX) minX = x;
                        }
                        break;
                    case 2: // Quadratic: endX, endY, cpX, cpY
                        if (i + 3 < outline.Length)
                        {
                            double ex = outline[i++]; i++; // end
                            double cx = outline[i++]; i++; // control
                            if (ex > maxX) maxX = ex; if (ex < minX) minX = ex;
                            if (cx > maxX) maxX = cx; if (cx < minX) minX = cx;
                        }
                        break;
                    case 3: // Bezier: endX, endY, cp1X, cp1Y, cp2X, cp2Y
                        if (i + 5 < outline.Length)
                        {
                            double ex = outline[i++]; i++; // end
                            double c1x = outline[i++]; i++; // control 1
                            double c2x = outline[i++]; i++; // control 2
                            if (ex > maxX) maxX = ex; if (ex < minX) minX = ex;
                            if (c1x > maxX) maxX = c1x; if (c1x < minX) minX = c1x;
                            if (c2x > maxX) maxX = c2x; if (c2x < minX) minX = c2x;
                        }
                        break;
                    default:
                        // Unknown command — skip remaining to avoid infinite loop
                        i = outline.Length;
                        break;
                }
            }

            if (double.IsInfinity(maxX) || double.IsInfinity(minX)) return 0.0;

            double bboxWidth = maxX - minX;
            return bboxWidth * scale;
        }

        /// <summary>
        /// Render this glyph at the given position in screen coordinates.
        /// </summary>
        /// <summary>
        /// Get the metrics for this glyph without rendering it.
        /// Returns null if the glyph code was not found in the font data.
        /// Port of VexFlow's Glyph.getMetrics() / metrics property from glyph.ts.
        /// </summary>
        public GlyphMetrics? GetMetrics()
        {
            if (_metrics == null) return null;
            return new GlyphMetrics
            {
                Width  = (_metrics.XMax - _metrics.XMin) * _scale,
                Height = _metrics.Ha * _scale,
                ActualBoundingBoxAscent = Math.Max(0, (_metrics.YMax ?? _metrics.Ha) * _scale),
                ActualBoundingBoxDescent = Math.Max(0, -(_metrics.YMin ?? 0) * _scale),
                XMin   = _metrics.XMin * _scale,
                XMax   = _metrics.XMax * _scale,
                XShift = 0,
                YShift = 0,
                Scale  = _scale,
                Outline = _metrics.CachedOutline,
            };
        }

        /// <param name="ctx">Render context to draw into.</param>
        /// <param name="xPos">X position in screen coordinates.</param>
        /// <param name="yPos">Y position in screen coordinates.</param>
        /// <returns>GlyphMetrics describing the rendered glyph's dimensions and position.</returns>
        public virtual GlyphMetrics Render(RenderContext ctx, double xPos, double yPos)
        {
            if (_metrics?.CachedOutline == null)
                throw new InvalidOperationException($"Glyph '{_code}' has no cached outline data.");

            RenderOutline(ctx, _metrics.CachedOutline, _scale, xPos, yPos);

            return new GlyphMetrics
            {
                Width = (_metrics.XMax - _metrics.XMin) * _scale,
                Height = _metrics.Ha * _scale,
                ActualBoundingBoxAscent = Math.Max(0, (_metrics.YMax ?? _metrics.Ha) * _scale),
                ActualBoundingBoxDescent = Math.Max(0, -(_metrics.YMin ?? 0) * _scale),
                XMin = _metrics.XMin * _scale,
                XMax = _metrics.XMax * _scale,
                XShift = 0,
                YShift = 0,
                Scale = _scale,
                Outline = _metrics.CachedOutline,
            };
        }
    }
}

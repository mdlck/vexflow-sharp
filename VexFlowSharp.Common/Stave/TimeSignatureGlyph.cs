// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// A composite glyph that renders both the top (numerator) and bottom (denominator)
    /// digit rows of a numeric time signature (e.g., 4/4, 3/4, 6/8).
    ///
    /// Each digit is rendered as a separate SMuFL glyph ("timeSig0" through "timeSig9").
    /// Port of VexFlow's TimeSignatureGlyph class from timesigglyph.ts.
    /// </summary>
    public class TimeSignatureGlyph
    {
        private readonly TimeSignature timeSignature;
        private readonly double compositeWidth;
        private readonly double topStartX;
        private readonly double botStartX;
        private readonly int lineShift;

        private readonly List<(FontGlyph Fg, double Scale)> topGlyphs;
        private readonly List<(FontGlyph Fg, double Scale)> botGlyphs;

        /// <summary>Total width of this composite glyph.</summary>
        public double Width => compositeWidth;

        /// <summary>
        /// Construct a TimeSignatureGlyph.
        /// </summary>
        /// <param name="timeSig">Parent TimeSignature that provides point size and line positions.</param>
        /// <param name="topDigits">Top row string (e.g., "4" for 4/4 top).</param>
        /// <param name="botDigits">Bottom row string (e.g., "4" for 4/4 bottom). Empty for standalone.</param>
        /// <param name="point">Point size to render at.</param>
        public TimeSignatureGlyph(TimeSignature timeSig, string topDigits, string botDigits, double point)
        {
            timeSignature = timeSig;
            var musicFont = Font.HasAnyFonts() ? Tables.CurrentMusicFont() : BravuraGlyphs.Data;

            topGlyphs = new List<(FontGlyph, double)>();
            botGlyphs = new List<(FontGlyph, double)>();

            double topWidth = 0;
            double botWidth = 0;
            double height   = 0;

            // Build top digit glyphs
            // Use Glyph.GetWidth() (VexFlow-compatible scale formula) for accurate widths.
            for (int i = 0; i < topDigits.Length; i++)
            {
                string digitCode = MapDigitToCode(topDigits[i], hasBotRow: botDigits.Length > 0);
                var data = Font.HasAnyFonts() ? Font.ResolveGlyphFontData(digitCode) : musicFont;
                double scale = Glyph.GetScale(point, data);
                data.Glyphs.TryGetValue(digitCode, out var fg);
                topGlyphs.Add((fg, scale));
                double gw = Glyph.GetWidth(digitCode, point);
                if (gw <= 0 && fg != null) gw = (fg.XMax - fg.XMin) * scale; // fallback
                double gh = fg != null ? fg.Ha * scale : point;
                topWidth += gw;
                height    = Math.Max(height, gh);
            }

            // Build bottom digit glyphs
            for (int i = 0; i < botDigits.Length; i++)
            {
                string digitCode = MapBotDigitToCode(botDigits[i]);
                var data = Font.HasAnyFonts() ? Font.ResolveGlyphFontData(digitCode) : musicFont;
                double scale = Glyph.GetScale(point, data);
                data.Glyphs.TryGetValue(digitCode, out var fg);
                botGlyphs.Add((fg, scale));
                double gw = Glyph.GetWidth(digitCode, point);
                if (gw <= 0 && fg != null) gw = (fg.XMax - fg.XMin) * scale; // fallback
                double gh = fg != null ? fg.Ha * scale : point;
                botWidth += gw;
                height    = Math.Max(height, gh);
            }

            // If glyphs taller than two staff spaces (20px), shift up/down an extra line
            lineShift = height > 22 ? 1 : 0;

            compositeWidth = Math.Max(topWidth, botWidth);
            topStartX      = (compositeWidth - topWidth) / 2.0;
            botStartX      = (compositeWidth - botWidth) / 2.0;
        }

        private static string MapDigitToCode(char ch, bool hasBotRow)
        {
            return ch switch
            {
                '-' => "timeSigMinus",
                '+' => hasBotRow ? "timeSigPlusSmall" : "timeSigPlus",
                '(' => hasBotRow ? "timeSigParensLeftSmall" : "timeSigParensLeft",
                ')' => hasBotRow ? "timeSigParensRightSmall" : "timeSigParensRight",
                _   => "timeSig" + ch,
            };
        }

        private static string MapBotDigitToCode(char ch)
        {
            return ch switch
            {
                '+' => "timeSigPlusSmall",
                '(' => "timeSigParensLeftSmall",
                ')' => "timeSigParensRightSmall",
                _   => "timeSig" + ch,
            };
        }

        /// <summary>
        /// Render top and bottom digit rows onto the stave at the given x position.
        /// </summary>
        public void RenderToStave(RenderContext ctx, Stave stave, double x)
        {
            // Top row Y
            double topY;
            if (botGlyphs.Count > 0)
                topY = stave.GetYForLine(timeSignature.TopLine - lineShift);
            else
                topY = (stave.GetYForLine(timeSignature.TopLine) + stave.GetYForLine(timeSignature.BottomLine)) / 2;

            double startX = x + topStartX;
            foreach (var (fg, s) in topGlyphs)
            {
                if (fg.CachedOutline != null)
                {
                    Glyph.RenderOutline(ctx, fg.CachedOutline, s, startX, topY);
                    startX += (fg.XMax - fg.XMin) * s;
                }
            }

            // Bottom row Y
            double botY = stave.GetYForLine(timeSignature.BottomLine + lineShift);
            startX = x + botStartX;
            foreach (var (fg, s) in botGlyphs)
            {
                if (fg.CachedOutline != null)
                {
                    Glyph.RenderOutline(ctx, fg.CachedOutline, s, startX, botY);
                    startX += (fg.XMax - fg.XMin) * s;
                }
            }
        }
    }
}

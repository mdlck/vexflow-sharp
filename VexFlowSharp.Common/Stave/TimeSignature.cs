// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Renders a time signature on a stave.
    /// Supports numeric signatures (4/4, 3/4, 6/8), common time (C), and cut time (C|).
    ///
    /// Port of VexFlow's TimeSignature class from timesignature.ts.
    /// </summary>
    public class TimeSignature : StaveModifier
    {
        public new const string CATEGORY = "TimeSignature";

        public override string GetCategory() => CATEGORY;

        /// <summary>Special time signatures mapping to single glyphs.</summary>
        private static readonly Dictionary<string, (string Code, double Line)> SpecialGlyphs
            = new Dictionary<string, (string, double)>
        {
            { "C",  ("timeSigCommon",    2) },
            { "C|", ("timeSigCutCommon", 2) },
        };

        private readonly string timeSpec;
        private readonly bool isNumeric;

        // For numeric time signatures
        private readonly TimeSignatureGlyph numericGlyph;
        // For special (common/cut) time signatures
        private readonly string specialCode;

        // VexFlow 5 measures time signature text through the active Bravura canvas font.
        // The outline-derived width is slightly narrower for numeric signatures.
        private const double NumericCanvasWidthCorrection = 1.1328125;

        /// <summary>Staff line for rendering the glyph (0 = numeric, 2 = common/cut).</summary>
        public double Line { get; private set; }

        /// <summary>
        /// Staff top line used by TimeSignatureGlyph for digit row placement.
        /// VexFlow: 2 + digits.shiftLine(-1) = 1
        /// </summary>
        public double TopLine    { get; } = 1;

        /// <summary>
        /// Staff bottom line used by TimeSignatureGlyph for digit row placement.
        /// VexFlow: 4 + digits.shiftLine(-1) = 3
        /// </summary>
        public double BottomLine { get; } = 3;

        /// <summary>Rendering point size for this time signature.</summary>
        public double Point { get; }

        /// <summary>
        /// Create a time signature from a spec string.
        /// Valid specs: "4/4", "3/4", "6/8", "C", "C|", additive e.g. "+".
        /// </summary>
        public TimeSignature(string timeSpec = "4/4", double customPadding = 15)
        {
            this.timeSpec = timeSpec;
            this.Point    = Tables.NOTATION_FONT_SCALE;

            SetPosition(StaveModifierPosition.Begin);
            SetPadding(customPadding);

            if (SpecialGlyphs.TryGetValue(timeSpec, out var info))
            {
                // Common time or cut time — single glyph
                isNumeric    = false;
                specialCode  = info.Code;
                Line         = info.Line;

                // Width from glyph data
                SetWidth(GetSpecialGlyphWidth(info.Code));
            }
            else
            {
                // Numeric time signature
                isNumeric = true;
                Line      = 0;

                var parts = timeSpec.Split('/');
                string top = parts.Length > 0 ? parts[0] : timeSpec;
                string bot = parts.Length > 1 ? parts[1] : "";

                numericGlyph = new TimeSignatureGlyph(this, top, bot, Point);
                double width = numericGlyph.Width > 0 ? numericGlyph.Width : GetFallbackNumericWidth(top, bot);
                SetWidth(width + NumericCanvasWidthCorrection);
            }
        }

        private double GetSpecialGlyphWidth(string code)
        {
            var data = Font.HasAnyFonts() ? Font.ResolveGlyphFontData(code) : BravuraGlyphs.Data;
            double scale = Glyph.GetScale(Point, data);
            if (data.Glyphs.TryGetValue(code, out var fg))
                return (fg.XMax - fg.XMin) * scale;
            return 20.0;  // fallback
        }

        private double GetFallbackNumericWidth(string top, string bot)
        {
            // Very rough: ~12px per digit
            int maxDigits = Math.Max(top.Length, bot.Length);
            return maxDigits * 12.0;
        }

        /// <summary>Get the time spec string (e.g., "4/4", "C").</summary>
        public string GetTimeSpec() => timeSpec;

        /// <summary>Whether this is a numeric time signature (not common/cut time).</summary>
        public bool GetIsNumeric() => isNumeric;

        /// <summary>
        /// Draw the time signature on the stave.
        /// </summary>
        public override void Draw(Stave stave, double xShift)
        {
            var ctx = stave.CheckContext();
            SetContext(ctx);

            double drawX = x + xShift;

            if (!isNumeric && specialCode != null)
            {
                // Render single common/cut time glyph
                double drawY = stave.GetYForLine(Line);
                var data = Font.HasAnyFonts() ? Font.ResolveGlyphFontData(specialCode) : BravuraGlyphs.Data;
                double scale = Glyph.GetScale(Point, data);

                if (data.Glyphs.TryGetValue(specialCode, out var fg)
                    && fg.CachedOutline != null)
                {
                    Glyph.RenderOutline(ctx, fg.CachedOutline, scale, drawX, drawY);
                }
            }
            else if (numericGlyph != null)
            {
                // Render top and bottom digit rows
                numericGlyph.RenderToStave(ctx, stave, drawX);
            }
        }
    }
}

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Renders a key signature (sharps or flats) on a stave.
    /// Port of VexFlow's KeySignature class from keysignature.ts.
    ///
    /// Calling Tables.KeySignature(spec) returns a list of (type, line) pairs
    /// that describe which accidentals to draw and where on the staff.
    /// </summary>
    public class KeySignature : StaveModifier
    {
        // Default horizontal spacing between adjacent accidentals (pixels)
        private const double AccidentalSpacing = 10.0;

        private readonly string keySpec;
        private string? cancelKeySpec;
        private readonly double glyphFontScale;

        /// <summary>Accidentals to render: (type="#"/"b"/"n", line=staff-line-position).</summary>
        private List<(string Type, double Line)> accList;

        private bool formatted;
        private bool paddingForced;

        /// <summary>Create a key signature for the given key spec (e.g., "G", "Eb", "F#").</summary>
        public KeySignature(string keySpec, string? cancelKeySpec = null)
        {
            this.keySpec       = keySpec;
            this.cancelKeySpec = cancelKeySpec;
            this.glyphFontScale = Tables.NOTATION_FONT_SCALE;
            this.accList       = new List<(string, double)>();
            this.formatted     = false;
            this.paddingForced = false;

            SetPosition(StaveModifierPosition.Begin);
        }

        /// <summary>
        /// Get padding for this modifier.
        /// Key signatures with no accidentals (C major) contribute no padding.
        /// </summary>
        public override double GetPadding(int index)
        {
            if (!formatted) Format();
            return (accList.Count == 0 || (!paddingForced && index < 2)) ? 0 : padding;
        }

        /// <summary>Get total width including all accidental glyphs.</summary>
        public override double GetWidth()
        {
            if (!formatted) Format();
            return width;
        }

        /// <summary>Number of accidentals in this key signature.</summary>
        public int GetAccidentalCount()
        {
            if (!formatted) Format();
            return accList.Count;
        }

        /// <summary>
        /// Force layout calculation based on Tables.KeySignature data and stave clef.
        /// Sets this.width and this.accList.
        /// </summary>
        public void Format()
        {
            width   = 0;
            accList = Tables.KeySignature(keySpec);

            // Width is the sum of per-glyph widths plus spacing
            foreach (var acc in accList)
            {
                var (code, _) = Tables.AccidentalCodes(acc.Type);
                // Estimate glyph width from BravuraGlyphs data if available
                double glyphWidth = GetAccidentalGlyphWidth(code);
                width += glyphWidth + AccidentalSpacing;
            }

            // Remove trailing spacing if any accidentals
            if (accList.Count > 0 && width > 0)
                width -= AccidentalSpacing;

            formatted = true;
        }

        private double GetAccidentalGlyphWidth(string code)
        {
            double scale = (glyphFontScale * 72.0) / (BravuraGlyphs.Data.Resolution * 100.0);
            if (BravuraGlyphs.Data.Glyphs.TryGetValue(code, out var fg))
                return (fg.XMax - fg.XMin) * scale;
            // Fallback widths matching VexFlow defaults
            return code.Contains("Flat") ? 8.0 : 10.0;
        }

        /// <summary>Draw all accidentals for this key signature on the stave.</summary>
        public override void Draw(Stave stave, double xShift)
        {
            var ctx = stave.CheckContext();
            SetContext(ctx);

            if (!formatted) Format();

            double curX  = x + xShift;
            double scale = (glyphFontScale * 72.0) / (BravuraGlyphs.Data.Resolution * 100.0);

            foreach (var acc in accList)
            {
                var (code, _) = Tables.AccidentalCodes(acc.Type);
                double lineY  = stave.GetYForLine(acc.Line);

                if (BravuraGlyphs.Data.Glyphs.TryGetValue(code, out var fg)
                    && fg.CachedOutline != null)
                {
                    Glyph.RenderOutline(ctx, fg.CachedOutline, scale, curX, lineY);
                    double gw = (fg.XMax - fg.XMin) * scale;
                    curX += gw + AccidentalSpacing;
                }
                else
                {
                    // Fallback: advance by a default amount
                    curX += (code.Contains("Flat") ? 8.0 : 10.0) + AccidentalSpacing;
                }
            }
        }
    }
}

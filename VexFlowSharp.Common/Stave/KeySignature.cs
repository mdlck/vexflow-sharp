#nullable enable annotations

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
        public new const string CATEGORY = "KeySignature";

        public override string GetCategory() => CATEGORY;

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

        public string GetKeySpec() => keySpec;
        public string? GetCancelKeySpec() => cancelKeySpec;

        /// <summary>
        /// Force layout calculation based on Tables.KeySignature data and stave clef.
        /// Sets this.width and this.accList.
        /// </summary>
        public void Format()
        {
            width   = 0;
            accList = Tables.KeySignature(keySpec);

            if (!string.IsNullOrEmpty(cancelKeySpec))
            {
                var cancelAccList = Tables.KeySignature(cancelKeySpec);
                bool differentTypes = accList.Count > 0 && cancelAccList.Count > 0
                    && cancelAccList[0].Type != accList[0].Type;
                int naturals = differentTypes ? cancelAccList.Count : cancelAccList.Count - accList.Count;

                if (naturals > 0)
                {
                    var cancelled = new List<(string Type, double Line)>();
                    for (int i = 0; i < naturals; i++)
                    {
                        int index = differentTypes ? i : cancelAccList.Count - naturals + i;
                        cancelled.Add(("n", cancelAccList[index].Line));
                    }
                    cancelled.AddRange(accList);
                    accList = cancelled;
                }
            }

            // Width is the sum of per-glyph widths plus spacing
            (string Type, double Line)? previous = null;
            foreach (var acc in accList)
            {
                var (code, _) = Tables.AccidentalCodes(acc.Type);
                // Estimate glyph width from BravuraGlyphs data if available
                double glyphWidth = GetAccidentalGlyphWidth(code);
                double spacing = GetAccidentalSpacing(previous, acc);
                width += glyphWidth + spacing;
                previous = acc;
            }

            // Remove trailing spacing if any accidentals
            if (accList.Count > 0 && width > 0)
                width -= Metrics.GetDouble("KeySignature.accidentalSpacing");

            formatted = true;
        }

        private static double GetAccidentalSpacing((string Type, double Line)? previous, (string Type, double Line) current)
        {
            if (previous == null) return Metrics.GetDouble("KeySignature.accidentalSpacing");
            bool hasNatural = previous.Value.Type == "n" || current.Type == "n";
            double yDistance = Math.Abs(current.Line - previous.Value.Line) * Tables.STAVE_LINE_DISTANCE;
            return hasNatural && yDistance < Tables.STAVE_LINE_DISTANCE
                ? Metrics.GetDouble("KeySignature.naturalCollisionSpacing")
                : Metrics.GetDouble("KeySignature.accidentalSpacing");
        }

        private double GetAccidentalGlyphWidth(string code)
        {
            double scale = (glyphFontScale * 72.0) / (BravuraGlyphs.Data.Resolution * 100.0);
            if (BravuraGlyphs.Data.Glyphs.TryGetValue(code, out var fg))
                return (fg.XMax - fg.XMin) * scale;
            // Fallback widths matching VexFlow defaults
            return code.Contains("Flat")
                ? Metrics.GetDouble("KeySignature.flatFallbackWidth")
                : Metrics.GetDouble("KeySignature.sharpFallbackWidth");
        }

        /// <summary>Draw all accidentals for this key signature on the stave.</summary>
        public override void Draw(Stave stave, double xShift)
        {
            var ctx = stave.CheckContext();
            SetContext(ctx);

            if (!formatted) Format();

            double curX  = x + xShift;
            double scale = (glyphFontScale * 72.0) / (BravuraGlyphs.Data.Resolution * 100.0);

            (string Type, double Line)? previous = null;
            foreach (var acc in accList)
            {
                var (code, _) = Tables.AccidentalCodes(acc.Type);
                double lineY  = stave.GetYForLine(acc.Line);

                if (BravuraGlyphs.Data.Glyphs.TryGetValue(code, out var fg)
                    && fg.CachedOutline != null)
                {
                    Glyph.RenderOutline(ctx, fg.CachedOutline, scale, curX, lineY);
                    double gw = (fg.XMax - fg.XMin) * scale;
                    curX += gw + GetAccidentalSpacing(previous, acc);
                }
                else
                {
                    // Fallback: advance by a default amount
                    curX += (code.Contains("Flat")
                        ? Metrics.GetDouble("KeySignature.flatFallbackWidth")
                        : Metrics.GetDouble("KeySignature.sharpFallbackWidth")) + GetAccidentalSpacing(previous, acc);
                }
                previous = acc;
            }
        }
    }
}

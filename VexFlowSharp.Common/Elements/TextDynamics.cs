// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's TextDynamics class (textdynamics.ts, 136 lines).
// TextDynamics renders traditional text dynamics markings (p, f, sfz, rfz, ppp etc.)
// as SMuFL glyphs, letter-by-letter. Extends Note so it can be formatted in a Voice.

using System;

namespace VexFlowSharp
{
    /// <summary>
    /// Renders dynamic markings (p, mp, mf, f, ff, pp, sfz, rfz etc.) as SMuFL glyphs.
    /// Extends Note so it participates in Voice/Formatter tick allocation.
    ///
    /// Multi-letter dynamics are rendered letter-by-letter; each letter's width is
    /// taken from Tables.TextDynamicsGlyphs.
    ///
    /// Port of VexFlow's TextDynamics class from textdynamics.ts.
    /// </summary>
    public class TextDynamics : Note
    {
        // ── Category ──────────────────────────────────────────────────────────

        public const string CATEGORY = "textdynamics";
        public override string GetCategory() => CATEGORY;

        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>The dynamics string to render (e.g. "p", "mf", "ff").</summary>
        private readonly string sequence;

        /// <summary>Staff line where dynamics are rendered (default 0; draw() applies -3 offset like VexFlow).</summary>
        private double line = 0;

        /// <summary>Glyph font scale for rendering. Matches Tables.NOTATION_FONT_SCALE default.</summary>
        private double glyphFontSize;

        /// <summary>Whether PreFormat has been called on this TextDynamics note.</summary>
        private bool isPreFormatted = false;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Construct a TextDynamics note from a NoteStruct.
        /// The <c>noteStruct.Duration</c> is used for tick-allocation in the Voice.
        /// The dynamics string comes from the constructor parameter.
        /// </summary>
        /// <param name="noteStruct">NoteStruct (must have Duration set).</param>
        /// <param name="dynamics">Dynamics sequence string e.g. "p", "mf", "ff".</param>
        public TextDynamics(NoteStruct noteStruct, string dynamics) : base(noteStruct)
        {
            sequence = (dynamics ?? "").ToLowerInvariant();
            glyphFontSize = Tables.NOTATION_FONT_SCALE;
        }

        /// <summary>
        /// Convenience constructor: takes dynamics string only; uses duration "q".
        /// </summary>
        /// <param name="dynamics">Dynamics sequence string e.g. "p", "mf", "ff".</param>
        public TextDynamics(string dynamics) : this(new NoteStruct { Duration = "q" }, dynamics)
        {
        }

        // ── Accessors ─────────────────────────────────────────────────────────

        /// <summary>Get the dynamics sequence string.</summary>
        public string GetSequence() => sequence;

        /// <summary>Get the staff line for placement.</summary>
        public double GetLine() => line;

        /// <summary>Set the staff line for placement.</summary>
        public TextDynamics SetLine(double l) { line = l; return this; }

        // ── Width ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Compute total rendered width as sum of each letter's width from TextDynamicsGlyphs.
        /// </summary>
        public new double GetWidth()
        {
            double w = 0;
            foreach (char c in sequence)
            {
                string key = c.ToString();
                if (Tables.TextDynamicsGlyphs.TryGetValue(key, out var g))
                    w += g.Width;
            }
            return w;
        }

        // ── PreFormat ─────────────────────────────────────────────────────────

        /// <summary>
        /// Pre-format: set width to sum of letter widths.
        /// Port of textdynamics.ts preFormat().
        /// </summary>
        public override void PreFormat()
        {
            if (isPreFormatted) return;
            double total_width = 0;
            foreach (char c in sequence)
            {
                string key = c.ToString();
                if (!Tables.TextDynamicsGlyphs.TryGetValue(key, out var g))
                    throw new InvalidOperationException(
                        $"TextDynamics: no glyph for letter '{key}'. Supported: f, p, m, s, z, r.");
                total_width += g.Width;
            }
            SetWidth(total_width);
            isPreFormatted = true;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw each dynamics letter as an SMuFL glyph at the correct staff position.
        /// Port of textdynamics.ts draw().
        /// </summary>
        public override void Draw()
        {
            var ctx   = CheckContext();
            var stave = CheckStave();
            rendered = true;

            double x = GetAbsoluteX();
            // VexFlow: getYForLine(this.line + -3) — offset of -3 places dynamics below the staff
            double y = stave.GetYForLine(line + -3);

            double curX = x;
            foreach (char c in sequence)
            {
                string key = c.ToString();
                if (!Tables.TextDynamicsGlyphs.TryGetValue(key, out var glyphData))
                    throw new InvalidOperationException(
                        $"TextDynamics: no glyph for letter '{key}'. Supported: f, p, m, s, z, r.");

                // Render SMuFL glyph using Glyph infrastructure
                var g = new Glyph(glyphData.Code, glyphFontSize);
                g.Render(ctx, curX, y);
                curX += glyphData.Width;
            }
        }
    }
}

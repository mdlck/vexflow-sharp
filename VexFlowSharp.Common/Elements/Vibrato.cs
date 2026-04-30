// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of vexflow/src/vibrato.ts
// Vibrato modifier — repeated SMuFL wiggle glyph above note head.

using System;
using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Rendering options for a vibrato glyph run.
    /// Port of VexFlow's VibratoRenderOptions interface from vibrato.ts.
    /// </summary>
    public class VibratoRenderOptions
    {
        /// <summary>SMuFL code point for the vibrato segment.</summary>
        public int Code { get; set; } = 0xeab0;

        /// <summary>Total requested width of the vibrato run in pixels.</summary>
        public double Width { get; set; } = Metrics.GetDouble("Vibrato.width");

        /// <summary>Compatibility-only: VexFlow 5 renders vibrato as text glyphs, not harsh zigzags.</summary>
        public bool Harsh { get; set; } = false;
    }

    /// <summary>
    /// Vibrato modifier — renders repeated SMuFL wiggle glyphs above a note.
    ///
    /// Port of VexFlow's Vibrato class from vibrato.ts.
    /// </summary>
    public class Vibrato : Modifier
    {
        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>Category string used by ModifierContext to group vibratos.</summary>
        public new const string CATEGORY = "Vibrato";

        /// <inheritdoc/>
        public override string GetCategory() => CATEGORY;

        // ── Fields ────────────────────────────────────────────────────────────

        private VibratoRenderOptions renderOptions = new VibratoRenderOptions();
        private string text = string.Empty;
        private MetricsFontInfo font = Metrics.GetFontInfo("Vibrato");

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a new Vibrato modifier.
        /// Defaults to RIGHT position (attached to right of note), matching VexFlow.
        /// </summary>
        public Vibrato()
        {
            position = ModifierPosition.Right;
            SetVibratoWidth(renderOptions.Width);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Whether to render a harsh zigzag vibrato.</summary>
        public bool IsHarsh
        {
            get => renderOptions.Harsh;
            set => renderOptions.Harsh = value;
        }

        // ── Setters ───────────────────────────────────────────────────────────

        public string GetText() => text;
        public int GetVibratoCode() => renderOptions.Code;

        /// <summary>Set the vibrato width and update the repeated glyph text.</summary>
        public Vibrato SetVibratoWidth(double w)
        {
            renderOptions.Width = w;
            text = char.ConvertFromUtf32(renderOptions.Code);

            double segmentWidth = GetSegmentWidth();
            if (segmentWidth <= 0)
                throw new VexFlowException("CannotSetVibratoWidth", "Cannot set vibrato width if width is 0");

            int items = (int)Math.Round(renderOptions.Width / segmentWidth);
            for (int i = 1; i < items; i++)
                text += char.ConvertFromUtf32(renderOptions.Code);

            SetWidth(w);
            return this;
        }

        /// <summary>Compatibility shim. VexFlow 5 vibrato no longer has a harsh rendering mode.</summary>
        public Vibrato SetHarsh(bool harsh)
        {
            renderOptions.Harsh = harsh;
            return this;
        }

        public Vibrato SetVibratoCode(int code)
        {
            renderOptions.Code = code;
            return SetVibratoWidth(renderOptions.Width);
        }

        /// <summary>Set all vibrato render options at once.</summary>
        public Vibrato SetVibratoRenderOptions(VibratoRenderOptions opts)
        {
            renderOptions = opts;
            return SetVibratoWidth(opts.Width);
        }

        public double GetSegmentWidth()
        {
            string glyphName = GetGlyphName(renderOptions.Code);
            if (glyphName != null)
            {
                double width = Glyph.GetWidth(glyphName, font.Size);
                if (width > 0) return width;
            }

            return TextFormatter.Create(font.Family, font.Size).GetWidthForTextInPx(char.ConvertFromUtf32(renderOptions.Code));
        }

        public void RenderText(RenderContext ctx, double x, double y)
        {
            string glyphName = GetGlyphName(renderOptions.Code);
            if (glyphName != null)
            {
                double segmentWidth = GetSegmentWidth();
                int items = Math.Max(1, (int)Math.Round(renderOptions.Width / segmentWidth));
                for (int i = 0; i < items; i++)
                    new Glyph(glyphName, font.Size).Render(ctx, x + i * segmentWidth, y);
                return;
            }

            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
            ctx.FillText(text, x, y);
        }

        private static string GetGlyphName(int code)
            => code == 0xeab0 ? "wiggleArpeggiatoUp" : null;

        // ── Format ────────────────────────────────────────────────────────────

        /// <summary>
        /// Arrange vibratos inside a ModifierContext.
        /// Takes THREE arguments — third arg is the ModifierContext (pitfall 3).
        ///
        /// Port of VexFlow's Vibrato.format() static from vibrato.ts (lines 28-57).
        /// </summary>
        public static bool Format(List<Vibrato> vibratos, ModifierContextState state, ModifierContext ctx)
        {
            if (vibratos == null || vibratos.Count == 0) return false;

            double textLine = state.TopTextLine;
            double width = 0;
            double shift = state.RightShift - Metrics.GetDouble("Vibrato.rightShift");

            // Vibratos are always on top.
            state.TopTextLine += Metrics.GetDouble("Vibrato.textLineIncrement");

            // Format each vibrato
            for (int i = 0; i < vibratos.Count; i++)
            {
                var vibrato = vibratos[i];
                vibrato.xShift = shift;
                vibrato.SetTextLine(textLine);
                width += vibrato.GetWidth();
                shift += width;
            }

            state.RightShift += width;
            return true;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw the vibrato glyph run above the attached note.
        /// Port of VexFlow's Vibrato.draw() from vibrato.ts.
        /// </summary>
        public override void Draw()
        {
            var ctx  = CheckContext();
            var note = (Note)GetNote();
            rendered = true;

            var start = note.GetModifierStartXY(ModifierPosition.Right, GetIndex() ?? 0);
            double x = start.X + xShift;
            double y = note.GetYForTopText(textLine) + Metrics.GetDouble("Vibrato.yShift");

            RenderText(ctx, x, y);
        }
    }
}

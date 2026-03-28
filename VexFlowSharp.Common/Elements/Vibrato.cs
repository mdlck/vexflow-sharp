// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of vexflow/src/vibrato.ts (145 lines)
// Vibrato modifier — wave pattern above note head.

using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Rendering options for a vibrato wave.
    /// Port of VexFlow's VibratoRenderOptions interface from vibrato.ts.
    /// </summary>
    public class VibratoRenderOptions
    {
        /// <summary>Total width of the vibrato wave in pixels.</summary>
        public double VibratoWidth { get; set; } = 20;

        /// <summary>Height of each wave cycle in pixels.</summary>
        public double WaveHeight { get; set; } = 6;

        /// <summary>Width of each wave cycle in pixels.</summary>
        public double WaveWidth { get; set; } = 4;

        /// <summary>Vertical girth (bottom-of-wave offset) in pixels.</summary>
        public double WaveGirth { get; set; } = 2;

        /// <summary>Whether to render a harsh zigzag vibrato instead of a smooth wave.</summary>
        public bool Harsh { get; set; } = false;
    }

    /// <summary>
    /// Vibrato modifier — renders a wave pattern above a note.
    /// Shared RenderVibrato() static method is also used by VibratoBracket.
    ///
    /// Port of VexFlow's Vibrato class from vibrato.ts.
    /// </summary>
    public class Vibrato : Modifier
    {
        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>Category string used by ModifierContext to group vibratos.</summary>
        public const string CATEGORY = "vibratos";

        /// <inheritdoc/>
        public override string GetCategory() => CATEGORY;

        // ── Fields ────────────────────────────────────────────────────────────

        private VibratoRenderOptions renderOptions = new VibratoRenderOptions();

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a new Vibrato modifier.
        /// Defaults to RIGHT position (attached to right of note), matching VexFlow.
        /// </summary>
        public Vibrato()
        {
            position = ModifierPosition.Right;
            SetVibratoWidth(renderOptions.VibratoWidth);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Whether to render a harsh zigzag vibrato.</summary>
        public bool IsHarsh
        {
            get => renderOptions.Harsh;
            set => renderOptions.Harsh = value;
        }

        // ── Setters ───────────────────────────────────────────────────────────

        /// <summary>Set the vibrato width and update the modifier width.</summary>
        public Vibrato SetVibratoWidth(double w)
        {
            renderOptions.VibratoWidth = w;
            SetWidth(w);
            return this;
        }

        /// <summary>Set harsh vibrato mode. Returns this for fluent chaining.</summary>
        public Vibrato SetHarsh(bool harsh)
        {
            renderOptions.Harsh = harsh;
            return this;
        }

        /// <summary>Set all vibrato render options at once.</summary>
        public Vibrato SetVibratoRenderOptions(VibratoRenderOptions opts)
        {
            renderOptions = opts;
            SetWidth(opts.VibratoWidth);
            return this;
        }

        // ── Static rendering ──────────────────────────────────────────────────

        /// <summary>
        /// Shared static vibrato wave renderer — used by both Vibrato.Draw() and VibratoBracket.Draw().
        ///
        /// For harsh=false: smooth wave using quadratic curves.
        /// For harsh=true: zigzag wave using straight lines.
        ///
        /// Port of VexFlow's Vibrato.renderVibrato() static from vibrato.ts (lines 105-144).
        /// </summary>
        public static void RenderVibrato(RenderContext ctx, double x, double y, VibratoRenderOptions opts)
        {
            double numWaves = opts.VibratoWidth / opts.WaveWidth;

            ctx.BeginPath();

            if (opts.Harsh)
            {
                // Zigzag wave (harsh vibrato) — straight lines
                ctx.MoveTo(x, y + opts.WaveGirth + 1);
                for (int i = 0; i < (int)(numWaves / 2); i++)
                {
                    ctx.LineTo(x + opts.WaveWidth, y - opts.WaveHeight / 2);
                    x += opts.WaveWidth;
                    ctx.LineTo(x + opts.WaveWidth, y + opts.WaveHeight / 2);
                    x += opts.WaveWidth;
                }
                // Return pass
                for (int i = 0; i < (int)(numWaves / 2); i++)
                {
                    ctx.LineTo(x - opts.WaveWidth, y - opts.WaveHeight / 2 + opts.WaveGirth + 1);
                    x -= opts.WaveWidth;
                    ctx.LineTo(x - opts.WaveWidth, y + opts.WaveHeight / 2 + opts.WaveGirth + 1);
                    x -= opts.WaveWidth;
                }
                ctx.Fill();
            }
            else
            {
                // Smooth wave — quadratic bezier curves
                ctx.MoveTo(x, y + opts.WaveGirth);
                for (int i = 0; i < (int)(numWaves / 2); i++)
                {
                    ctx.QuadraticCurveTo(x + opts.WaveWidth / 2, y - opts.WaveHeight / 2, x + opts.WaveWidth, y);
                    x += opts.WaveWidth;
                    ctx.QuadraticCurveTo(x + opts.WaveWidth / 2, y + opts.WaveHeight / 2, x + opts.WaveWidth, y);
                    x += opts.WaveWidth;
                }
                // Return pass
                for (int i = 0; i < (int)(numWaves / 2); i++)
                {
                    ctx.QuadraticCurveTo(x - opts.WaveWidth / 2, y + opts.WaveHeight / 2 + opts.WaveGirth, x - opts.WaveWidth, y + opts.WaveGirth);
                    x -= opts.WaveWidth;
                    ctx.QuadraticCurveTo(x - opts.WaveWidth / 2, y - opts.WaveHeight / 2 + opts.WaveGirth, x - opts.WaveWidth, y + opts.WaveGirth);
                    x -= opts.WaveWidth;
                }
                ctx.Fill();
            }
        }

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
            double shift = state.RightShift - 7;

            // Vibratos are always on top — increment top_text_line
            state.TopTextLine += 1;

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
        /// Draw the vibrato wave above the attached note.
        /// Port of VexFlow's Vibrato.draw() from vibrato.ts (lines 88-99).
        /// </summary>
        public override void Draw()
        {
            var ctx  = CheckContext();
            var note = (Note)GetNote();
            rendered = true;

            double startX = note.GetAbsoluteX() + xShift;
            double y = note.GetYForTopText(textLine) + 2;

            renderOptions.VibratoWidth = GetWidth();
            RenderVibrato(ctx, startX, y, renderOptions);
        }
    }
}

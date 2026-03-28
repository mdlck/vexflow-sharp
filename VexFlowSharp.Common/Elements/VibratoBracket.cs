// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of vexflow/src/vibratobracket.ts (96 lines)
// VibratoBracket — vibrato wave spanning multiple notes, delegating to Vibrato.RenderVibrato().

namespace VexFlowSharp
{
    /// <summary>
    /// VibratoBracket renders a vibrato wave spanning multiple notes.
    /// Either start or stop may be null:
    ///   - null start: wave begins at stave start
    ///   - null stop: wave extends to stave end
    ///
    /// Port of VexFlow's VibratoBracket class from vibratobracket.ts.
    /// </summary>
    public class VibratoBracket : Element
    {
        // ── Fields ────────────────────────────────────────────────────────────

        private readonly Note? start;
        private readonly Note? stop;

        private int line = 1;

        private readonly VibratoRenderOptions renderOptions = new VibratoRenderOptions
        {
            Harsh       = false,
            WaveHeight  = 6,
            WaveWidth   = 4,
            WaveGirth   = 2,
            VibratoWidth = 0,
        };

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Construct a VibratoBracket with optional start and stop notes.
        /// Port of VexFlow's VibratoBracket(bracket_data) constructor.
        /// </summary>
        /// <param name="start">Note where the bracket begins. Null = from stave start.</param>
        /// <param name="stop">Note where the bracket ends. Null = to stave end.</param>
        public VibratoBracket(Note? start = null, Note? stop = null)
        {
            this.start = start;
            this.stop  = stop;
        }

        // ── Setters ───────────────────────────────────────────────────────────

        /// <summary>Set the staff line position for this bracket.</summary>
        public VibratoBracket SetLine(int l) { line = l; return this; }

        /// <summary>Set harsh zigzag vibrato mode.</summary>
        public VibratoBracket SetHarsh(bool harsh) { renderOptions.Harsh = harsh; return this; }

        /// <summary>Override the vibrato wave width.</summary>
        public VibratoBracket SetVibratoWidth(double w) { renderOptions.VibratoWidth = w; return this; }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw the vibrato bracket from start to stop, delegating to Vibrato.RenderVibrato().
        /// Port of VexFlow's VibratoBracket.draw() from vibratobracket.ts (lines 72-95).
        /// </summary>
        public override void Draw()
        {
            var ctx = CheckContext();
            rendered = true;

            // Compute y from whichever note has a stave
            double y = 0;
            if (start != null)
                y = start.CheckStave().GetYForTopText(line);
            else if (stop != null)
                y = stop.CheckStave().GetYForTopText(line);

            // Compute start x — from start note, or stave tie-start x
            double startX = 0;
            if (start != null)
                startX = start.GetAbsoluteX();
            else if (stop != null)
                startX = stop.CheckStave().GetTieStartX();

            // Compute stop x — from stop note, or stave tie-end x
            double stopX = 0;
            if (stop != null)
                stopX = stop.GetAbsoluteX() - stop.GetWidth() - 5;
            else if (start != null)
                stopX = start.CheckStave().GetTieEndX() - 10;

            renderOptions.VibratoWidth = stopX - startX;

            Vibrato.RenderVibrato(ctx, startX, y, renderOptions);
        }
    }
}

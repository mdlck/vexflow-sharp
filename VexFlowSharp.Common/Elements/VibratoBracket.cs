// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of vexflow/src/vibratobracket.ts (96 lines)
// VibratoBracket — vibrato glyph run spanning multiple notes.

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
        public new const string CATEGORY = "VibratoBracket";

        // ── Fields ────────────────────────────────────────────────────────────

        private readonly Note start;
        private readonly Note stop;

        private int line = 1;
        private readonly Vibrato vibrato = new Vibrato();

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Construct a VibratoBracket with optional start and stop notes.
        /// Port of VexFlow's VibratoBracket(bracket_data) constructor.
        /// </summary>
        /// <param name="start">Note where the bracket begins. Null = from stave start.</param>
        /// <param name="stop">Note where the bracket ends. Null = to stave end.</param>
        public VibratoBracket(Note start = null, Note stop = null)
        {
            this.start = start;
            this.stop  = stop;
        }

        // ── Setters ───────────────────────────────────────────────────────────

        /// <summary>Set the staff line position for this bracket.</summary>
        public VibratoBracket SetLine(int l) { line = l; return this; }

        /// <summary>Compatibility shim. VexFlow 5 vibrato brackets use text glyphs.</summary>
        public VibratoBracket SetHarsh(bool harsh) { vibrato.SetHarsh(harsh); return this; }

        /// <summary>Override the vibrato wave width.</summary>
        public VibratoBracket SetVibratoWidth(double w) { vibrato.SetVibratoWidth(w); return this; }

        public VibratoBracket SetVibratoCode(int code) { vibrato.SetVibratoCode(code); return this; }

        public Note GetStart() => start;
        public Note GetStop() => stop;
        public int GetLine() => line;
        public bool IsHarsh() => vibrato.IsHarsh;
        public int GetVibratoCode() => vibrato.GetVibratoCode();
        public double GetVibratoWidth() => vibrato.GetWidth();

        public override string GetCategory() => CATEGORY;

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
                stopX = stop.GetAbsoluteX() - stop.GetWidth() - Metrics.GetDouble("VibratoBracket.stopNoteOffset");
            else if (start != null)
                stopX = start.CheckStave().GetTieEndX() - Metrics.GetDouble("VibratoBracket.tieEndOffset");

            vibrato.SetVibratoWidth(stopX - startX);

            vibrato.RenderText(ctx, startX, y);
        }
    }
}

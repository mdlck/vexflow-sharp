// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    /// <summary>
    /// Position of a stave modifier on the staff.
    /// Port of VexFlow's StaveModifierPosition enum from stavemodifier.ts.
    /// </summary>
    public enum StaveModifierPosition
    {
        Center = 0,
        Left   = 1,
        Right  = 2,
        Above  = 3,
        Below  = 4,
        Begin  = 5,
        End    = 6,
    }

    /// <summary>
    /// Layout metrics for a stave modifier (used by Barline to specify positioning).
    /// Port of VexFlow's LayoutMetrics interface from stavemodifier.ts.
    /// </summary>
    public class LayoutMetrics
    {
        public double XMin { get; set; }
        public double XMax { get; set; }
        public double PaddingLeft { get; set; }
        public double PaddingRight { get; set; }
    }

    /// <summary>
    /// Abstract base class for all stave modifiers (clefs, key signatures, time signatures, barlines).
    /// Port of VexFlow's StaveModifier class from stavemodifier.ts.
    /// </summary>
    public abstract class StaveModifier : Element
    {
        protected double width = 0;
        protected double x = 0;
        protected double padding = 10;
        protected StaveModifierPosition position = StaveModifierPosition.Above;
        protected Stave? stave;
        protected LayoutMetrics? layoutMetrics;

        // ── Width ──────────────────────────────────────────────────────────────

        /// <summary>Get modifier width.</summary>
        public virtual double GetWidth() => width;

        /// <summary>Set modifier width. Returns this for fluent chaining.</summary>
        public StaveModifier SetWidth(double w) { width = w; return this; }

        // ── X position ────────────────────────────────────────────────────────

        /// <summary>Get the x position of this modifier.</summary>
        public double GetX() => x;

        /// <summary>Set the x position. Returns this for fluent chaining.</summary>
        public StaveModifier SetX(double xPos) { x = xPos; return this; }

        // ── Padding ───────────────────────────────────────────────────────────

        /// <summary>
        /// Get padding for this modifier. index less than 2 returns 0 (no padding for first two modifiers).
        /// Port of VexFlow's getPadding(index) from stavemodifier.ts.
        /// </summary>
        public virtual double GetPadding(int index) => (index < 2) ? 0 : padding;

        /// <summary>Set padding. Returns this for fluent chaining.</summary>
        public StaveModifier SetPadding(double p) { padding = p; return this; }

        // ── Position ──────────────────────────────────────────────────────────

        /// <summary>Get the position of this modifier.</summary>
        public StaveModifierPosition GetPosition() => position;

        /// <summary>Set the position. Returns this for fluent chaining.</summary>
        public StaveModifier SetPosition(StaveModifierPosition pos) { position = pos; return this; }

        // ── Stave ─────────────────────────────────────────────────────────────

        /// <summary>Get the associated stave (may be null).</summary>
        public Stave? GetStave() => stave;

        /// <summary>Set the associated stave. Returns this for fluent chaining.</summary>
        public StaveModifier SetStave(Stave s) { stave = s; return this; }

        /// <summary>Get the associated stave. Throws VexFlowException if not set.</summary>
        public Stave CheckStave()
            => stave ?? throw new VexFlowException("NoStave", "No stave attached to instance.");

        // ── Layout Metrics ────────────────────────────────────────────────────

        /// <summary>Get layout metrics (may be null).</summary>
        public LayoutMetrics? GetLayoutMetrics() => layoutMetrics;

        /// <summary>Set layout metrics. Returns this for fluent chaining.</summary>
        public StaveModifier SetLayoutMetrics(LayoutMetrics lm) { layoutMetrics = lm; return this; }

        // ── Place glyph on line ───────────────────────────────────────────────

        /// <summary>
        /// Set the yShift on a glyph so that it aligns with the given stave line.
        /// Port of VexFlow's placeGlyphOnLine() from stavemodifier.ts.
        /// </summary>
        public void PlaceGlyphOnLine(Glyph glyph, Stave s, double line, double customShift = 0)
        {
            glyph.SetYShift(s.GetYForLine(line) - s.GetYForGlyphs() + customShift);
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>Draw this modifier on the given stave at the given x shift.</summary>
        public abstract void Draw(Stave stave, double xShift);
    }
}

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's Crescendo class (crescendo.ts, 133 lines).
// Crescendo/Decrescendo draws hairpin dynamic marks between two tick positions.
// Extends Note so it participates in Voice/Formatter tick allocation.

using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Draws crescendo (narrow-to-wide) or decrescendo (wide-to-narrow) hairpin marks.
    /// Extends Note so it is formatted as part of a Voice like any other note type.
    ///
    /// Start x comes from GetAbsoluteX(); end x comes from TickContext.GetNextContext()
    /// or from the stave right edge if there is no next context.
    ///
    /// Port of VexFlow's Crescendo class from crescendo.ts.
    /// </summary>
    public class Crescendo : Note
    {
        // ── Category ──────────────────────────────────────────────────────────

        public const string CATEGORY = "crescendo";
        public override string GetCategory() => CATEGORY;

        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>Whether this is a decrescendo (reversed hairpin). Default false.</summary>
        private bool decrescendo;

        /// <summary>Full height at the open end of the hairpin in pixels.</summary>
        private double height = 15;

        /// <summary>Staff line on which the hairpin is centred.</summary>
        private double line = 0;

        /// <summary>Horizontal extension on the left side (expand start by this amount).</summary>
        private double extendLeft  = 0;

        /// <summary>Horizontal extension on the right side (expand end by this amount).</summary>
        private double extendRight = 0;

        /// <summary>Vertical shift applied to the hairpin y position.</summary>
        private double yShift = 0;

        /// <summary>Whether PreFormat has been called on this Crescendo note.</summary>
        private bool isPreFormatted = false;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Construct a Crescendo from a NoteStruct.
        /// Use <c>SetDecrescendo(true)</c> to render a decrescendo instead.
        /// </summary>
        /// <param name="noteStruct">NoteStruct with at least Duration set.</param>
        public Crescendo(NoteStruct noteStruct) : base(noteStruct)
        {
            decrescendo = false;
            line = 0;
        }

        /// <summary>
        /// Convenience constructor: takes a decrescendo flag only; uses duration "q".
        /// </summary>
        /// <param name="isDecrescendo">True for decrescendo (wide-to-narrow), false for crescendo.</param>
        public Crescendo(bool isDecrescendo = false)
            : this(new NoteStruct { Duration = "q" })
        {
            decrescendo = isDecrescendo;
        }

        // ── Accessors ─────────────────────────────────────────────────────────

        /// <summary>Whether this is a decrescendo.</summary>
        public bool IsDecrescendo() => decrescendo;

        /// <summary>Set whether the sign is a decrescendo.</summary>
        public Crescendo SetDecrescendo(bool value) { decrescendo = value; return this; }

        /// <summary>Get the full hairpin height at the open end.</summary>
        public double GetHeight() => height;

        /// <summary>Set the full hairpin height at the open end.</summary>
        public Crescendo SetHeight(double h) { height = h; return this; }

        /// <summary>Set the staff line for placement.</summary>
        public Crescendo SetLine(double l) { line = l; return this; }

        // ── PreFormat ─────────────────────────────────────────────────────────

        /// <summary>
        /// Pre-format stub — marks as formatted.
        /// Port of crescendo.ts preFormat().
        /// </summary>
        public override void PreFormat()
        {
            if (isPreFormatted) return;
            isPreFormatted = true;
        }

        // ── Rendering helper ──────────────────────────────────────────────────

        /// <summary>
        /// Draw the hairpin wedge using two lines from a point to a wide end (or reverse).
        /// Port of crescendo.ts renderHairpin() private function.
        /// </summary>
        private static void RenderHairpin(RenderContext ctx,
                                          double beginX, double endX,
                                          double y, double h,
                                          bool reverse)
        {
            double halfH = h / 2;

            ctx.BeginPath();

            if (reverse)
            {
                // Decrescendo: wide end at begin, narrow (point) at end
                ctx.MoveTo(beginX, y - halfH);
                ctx.LineTo(endX,   y);
                ctx.LineTo(beginX, y + halfH);
            }
            else
            {
                // Crescendo: narrow (point) at begin, wide end at end
                ctx.MoveTo(endX,   y - halfH);
                ctx.LineTo(beginX, y);
                ctx.LineTo(endX,   y + halfH);
            }

            ctx.Stroke();
            ctx.ClosePath();
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw the crescendo/decrescendo hairpin on the render context.
        /// End x uses GetNextContext() if present, otherwise the stave right edge.
        /// Port of crescendo.ts draw().
        /// </summary>
        public override void Draw()
        {
            var ctx   = CheckContext();
            var stave = CheckStave();
            rendered = true;

            var tickContext = GetTickContext();
            var nextContext = tickContext != null
                ? TickContext.GetNextContext(tickContext)
                : null;

            double beginX = GetAbsoluteX() - extendLeft;
            double endX   = nextContext != null
                ? nextContext.GetX() + extendRight
                : stave.GetX() + stave.GetWidth();

            // VexFlow: getYForLine(this.line + -3) + 1
            double y = stave.GetYForLine(line + -3) + 1 + yShift;

            RenderHairpin(ctx, beginX, endX, y, height, decrescendo);
        }
    }
}

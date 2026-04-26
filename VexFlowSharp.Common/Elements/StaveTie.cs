#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's StaveTie class (stavetie.ts, 239 lines).
// StaveTie renders ties, hammer-ons (H), pull-offs (P), and slides between notes.
// It extends Element directly — no ModifierContext involvement.

using System;

namespace VexFlowSharp
{
    /// <summary>
    /// Holds the note references for a StaveTie connection.
    /// Port of VexFlow's TieNotes interface from stavetie.ts.
    /// Either FirstNote or LastNote may be null to indicate a barline boundary.
    /// </summary>
    public class TieNotes
    {
        /// <summary>The start note of the tie (null = starts at stave left boundary).</summary>
        public Note? FirstNote { get; set; }

        /// <summary>The end note of the tie (null = ends at stave right boundary).</summary>
        public Note? LastNote { get; set; }

        /// <summary>Index into the start note's keys array for tie anchor (default 0).</summary>
        public int FirstIndex { get; set; } = 0;

        /// <summary>Index into the end note's keys array for tie anchor (default 0).</summary>
        public int LastIndex { get; set; } = 0;
    }

    /// <summary>
    /// Render options for a StaveTie connection.
    /// Port of VexFlow's StaveTie.render_options from stavetie.ts.
    /// </summary>
    public class StaveTieRenderOptions
    {
        /// <summary>Quadratic bezier control point 1 y-offset (top arc peak).</summary>
        public double Cp1 { get; set; } = 36;

        /// <summary>Quadratic bezier control point 2 y-offset (return arc offset).</summary>
        public double Cp2 { get; set; } = 36;

        /// <summary>X offset applied to the first note anchor point.</summary>
        public double FirstXShift { get; set; } = 0;

        /// <summary>X offset applied to the last note anchor point.</summary>
        public double LastXShift { get; set; } = 0;

        /// <summary>Text x shift for centering the text label.</summary>
        public double TextShiftX { get; set; } = 0;

        /// <summary>Y shift applied to the tie arc (scaled by direction).</summary>
        public double YShift { get; set; } = 7;

        /// <summary>Stroke thickness for the filled tie arc.</summary>
        public double Thickness { get; set; } = 2;
    }

    /// <summary>
    /// Renders a tie, hammer-on, pull-off, or slide arc between two notes.
    /// Port of VexFlow's StaveTie class from stavetie.ts.
    ///
    /// Uses two quadratic bezier curves to form a filled lune shape.
    /// The text field ("H", "P", slide text) is rendered centered between the notes.
    /// Either FirstNote or LastNote in TieNotes may be null for barline-boundary ties.
    /// </summary>
    public class StaveTie : Element
    {
        private readonly TieNotes notes;
        private readonly string? text;
        public StaveTieRenderOptions RenderOptions => renderOptions;
        private readonly StaveTieRenderOptions renderOptions;

        /// <summary>Explicit direction: 1 = tie curves above, -1 = tie curves below. 0 = auto.</summary>
        private int direction = 0;

        /// <summary>
        /// Create a StaveTie between the notes described in the TieNotes struct.
        /// The optional text is rendered as a label (e.g., "H" for hammer-on, "P" for pull-off).
        /// Port of VexFlow's StaveTie constructor from stavetie.ts.
        /// </summary>
        public StaveTie(TieNotes notes, string? text = null, StaveTieRenderOptions? options = null)
        {
            this.notes         = notes;
            this.text          = text;
            this.renderOptions = options ?? new StaveTieRenderOptions();
        }

        /// <summary>
        /// Set the arc direction explicitly.
        ///   1 = above (tie curves up, stem direction down relative to arc)
        ///  -1 = below (tie curves down, stem direction up relative to arc)
        ///   0 = auto (inferred from stem direction)
        /// Port of VexFlow's StaveTie.setDirection() from stavetie.ts.
        /// </summary>
        public StaveTie SetDirection(int dir)
        {
            direction = dir;
            return this;
        }

        /// <summary>
        /// Get the TieNotes structure.
        /// Port of VexFlow's StaveTie.getNotes().
        /// </summary>
        public TieNotes GetNotes() => notes;

        /// <summary>
        /// Returns true if either endpoint is null (barline boundary).
        /// Port of VexFlow's StaveTie.isPartial().
        /// </summary>
        public bool IsPartial() => notes.FirstNote == null || notes.LastNote == null;

        /// <summary>
        /// Render the quadratic bezier tie arc.
        /// Port of VexFlow's StaveTie.renderTie() from stavetie.ts.
        ///
        /// Uses two quadratic bezier passes to create a filled lune:
        ///   Forward:  MoveTo(firstX, firstY) → QuadTo(cpX, top_cpY, lastX, lastY)
        ///   Return:   QuadTo(cpX, bottom_cpY, firstX, firstY) → ClosePath → Fill
        ///
        /// When notes are close (&lt;10px), cp offsets are reduced for a tighter arc.
        /// </summary>
        private void RenderTie(RenderContext ctx,
                                double firstX, double lastX,
                                double firstY, double lastY,
                                int dir)
        {
            double cp1 = renderOptions.Cp1;
            double cp2 = renderOptions.Cp2;

            // For very close notes, use smaller control points (matches VexFlow behavior)
            if (Math.Abs(lastX - firstX) < 10)
            {
                cp1 = 2;
                cp2 = 8;
            }

            double fxShift = renderOptions.FirstXShift;
            double lxShift = renderOptions.LastXShift;

            double cpX      = (lastX + lxShift + (firstX + fxShift)) / 2.0;
            double topCpY   = (firstY + lastY) / 2.0 + cp1 * dir;
            double botCpY   = (firstY + lastY) / 2.0 + cp2 * dir;

            ctx.BeginPath();
            ctx.MoveTo(firstX + fxShift, firstY);
            ctx.QuadraticCurveTo(cpX, topCpY, lastX + lxShift, lastY);
            ctx.QuadraticCurveTo(cpX, botCpY, firstX + fxShift, firstY);
            ctx.ClosePath();
            ctx.Fill();
        }

        /// <summary>
        /// Render the text label (hammer-on, pull-off, or slide indicator).
        /// Centers the text between firstX and lastX at the stave's top text line.
        /// Port of VexFlow's StaveTie.renderText() from stavetie.ts.
        /// </summary>
        private void RenderText(RenderContext ctx, double firstX, double lastX)
        {
            if (string.IsNullOrEmpty(text)) return;

            double centerX = (firstX + lastX) / 2.0;
            var textMeasure = ctx.MeasureText(text);
            centerX -= textMeasure.Width / 2.0;
            centerX += renderOptions.TextShiftX;

            // Get y from stave top text line
            var stave = notes.FirstNote?.GetStave() ?? notes.LastNote?.GetStave();
            if (stave != null)
            {
                double y = stave.GetYForTopText() - 1;
                ctx.FillText(text, centerX, y);
            }
        }

        /// <summary>
        /// Draw the tie arc and optional text label onto the render context.
        /// Port of VexFlow's StaveTie.draw() from stavetie.ts.
        ///
        /// Anchor x positions come from note.GetTieRightX()/GetTieLeftX() (or stave boundaries).
        /// Anchor y positions come from note.GetYs()[index] shifted by YShift * direction.
        /// Direction is inferred from stem direction unless SetDirection() was called.
        /// </summary>
        public override void Draw()
        {
            var ctx = CheckContext();
            rendered = true;

            var firstNote = notes.FirstNote;
            var lastNote  = notes.LastNote;

            double firstX, lastX;
            double firstY, lastY;
            int stemDirection = -1; // default: arc above = -1 in VexFlow convention

            if (firstNote != null)
            {
                firstX        = firstNote.GetTieRightX();
                firstY        = firstNote.GetTieYForBottom();
                try { stemDirection = firstNote.GetStemDirection(); }
                catch { /* no stem — use default */ }
            }
            else if (lastNote != null)
            {
                var stave = lastNote.CheckStave();
                firstX = stave.GetTieStartX();
                firstY = lastNote.GetTieYForBottom();
            }
            else
            {
                // Both null — nothing to draw
                return;
            }

            if (lastNote != null)
            {
                lastX        = lastNote.GetTieLeftX();
                lastY        = lastNote.GetTieYForBottom();
                try { stemDirection = lastNote.GetStemDirection(); }
                catch { /* no stem — use default */ }
            }
            else if (firstNote != null)
            {
                var stave = firstNote.CheckStave();
                lastX = stave.GetTieEndX();
                lastY = firstY;
            }
            else
            {
                return;
            }

            // Explicit direction overrides stem direction
            int dir = direction != 0 ? direction : stemDirection;

            // Apply y shift (scaled by direction)
            double yShift = renderOptions.YShift * dir;
            firstY += yShift;
            lastY  += yShift;

            RenderTie(ctx, firstX, lastX, firstY, lastY, dir);
            RenderText(ctx, firstX, lastX);
        }

        /// <inheritdoc />
        public override string GetCategory() => "StaveTie";
    }
}

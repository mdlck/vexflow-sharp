// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's Curve class (curve.ts, 193 lines).
// Curve renders a bezier slur/tie arc between two notes.
// It extends Element directly — no ModifierContext involvement.

using System;

namespace VexFlowSharp
{
    /// <summary>
    /// Describes where the curve end point anchors relative to the note.
    /// Port of VexFlow's CurvePosition enum from curve.ts.
    /// </summary>
    public enum CurvePosition
    {
        NEAR_HEAD = 1,
        NEAR_TOP  = 2,
    }

    /// <summary>
    /// Options for rendering a Curve (slur).
    /// Port of VexFlow's CurveOptions interface from curve.ts.
    /// </summary>
    public class CurveOptions
    {
        /// <summary>Stroke thickness of the bezier arc fill path.</summary>
        public double Thickness { get; set; } = Metrics.GetDouble("Curve.thickness");

        /// <summary>Horizontal shift applied to both anchor x positions.</summary>
        public double X_Shift { get; set; } = Metrics.GetDouble("Curve.xShift");

        /// <summary>Vertical shift applied to both anchor y positions (scaled by direction).</summary>
        public double Y_Shift { get; set; } = Metrics.GetDouble("Curve.yShift");

        /// <summary>
        /// Control point y-offset for the bezier arc height.
        /// Default cp y value from VexFlow's render_options.cps[n].y = 10.
        /// </summary>
        public double CpHeight { get; set; } = Metrics.GetDouble("Curve.cpHeight");

        /// <summary>Whether to invert the curve direction relative to the stem direction.</summary>
        public bool Invert { get; set; } = false;

        /// <summary>Which part of the start note to anchor the curve.</summary>
        public CurvePosition Position { get; set; } = CurvePosition.NEAR_HEAD;

        /// <summary>Which part of the end note to anchor the curve.</summary>
        public CurvePosition PositionEnd { get; set; } = CurvePosition.NEAR_HEAD;
    }

    /// <summary>
    /// Renders a bezier slur arc between two notes.
    /// Port of VexFlow's Curve class from curve.ts.
    ///
    /// Unlike Modifier subclasses, Curve holds note references directly
    /// and draws itself when Draw() is called with a render context set.
    /// Either from or to can be null to indicate a barline boundary.
    /// </summary>
    public class Curve : Element
    {
        public new const string CATEGORY = "Curve";

        private readonly Note from;
        private readonly Note to;
        private readonly CurveOptions renderOptions;

        /// <summary>
        /// Create a Curve (slur) from one note to another.
        /// Either from or to may be null — null means a barline boundary
        /// (curve starts at stave.GetTieStartX() or ends at stave.GetTieEndX()).
        /// Port of VexFlow's Curve constructor from curve.ts.
        /// </summary>
        public Curve(Note from, Note to, CurveOptions options = null)
        {
            this.from          = from;
            this.to            = to;
            this.renderOptions = options ?? new CurveOptions();
        }

        public Note GetFromNote() => from;
        public Note GetToNote() => to;
        public CurveOptions GetRenderOptions() => renderOptions;

        /// <summary>
        /// Returns true if this is a partial curve (one note is null).
        /// Port of VexFlow's Curve.isPartial().
        /// </summary>
        public bool IsPartial() => from == null || to == null;

        /// <summary>
        /// Render the cubic bezier arc.
        /// Ports VexFlow's Curve.renderCurve() from curve.ts exactly:
        ///   cp_spacing = (lastX - firstX) / (cps.length + 2)  where cps.length = 2
        ///   cp1X = firstX + cp_spacing + cp0x
        ///   cp1Y = firstY + cp0y * direction
        ///   cp2X = lastX  - cp_spacing + cp1x
        ///   cp2Y = lastY  + cp1y * direction
        /// A return pass is added at Thickness offset to give the arc visual weight.
        /// </summary>
        private void RenderCurve(double firstX, double firstY, double lastX, double lastY,
                                  int direction, RenderContext ctx)
        {
            ApplyStyle();
            double cpHeight  = renderOptions.CpHeight;
            double thickness = renderOptions.Thickness;
            bool dashed = !string.IsNullOrWhiteSpace(GetStyle()?.LineDash);

            // cp_spacing = (last_x - first_x) / (cps.length + 2), cps.length = 2 → divide by 4
            double cpSpacing = (lastX - firstX) / 4.0;

            // Forward pass control points
            double cp1X = firstX + cpSpacing;
            double cp1Y = firstY + cpHeight * direction;
            double cp2X = lastX  - cpSpacing;
            double cp2Y = lastY  + cpHeight * direction;

            ctx.BeginPath();
            ctx.MoveTo(firstX, firstY);
            ctx.BezierCurveTo(cp1X, cp1Y, cp2X, cp2Y, lastX, lastY);

            if (!dashed)
            {
                // Return pass (thickness) — offset the y by thickness in the curve direction
                ctx.BezierCurveTo(
                    cp2X, cp2Y + thickness * direction,
                    cp1X, cp1Y + thickness * direction,
                    firstX, firstY
                );
            }
            ctx.Stroke();
            ctx.ClosePath();
            if (!dashed) ctx.Fill();
            RestoreStyle();
        }

        /// <summary>
        /// Draw the curve onto the render context.
        /// Port of VexFlow's Curve.draw() from curve.ts.
        /// Computes anchor x/y from the from/to notes (or stave boundaries for null notes),
        /// determines the curve direction from the stem direction, and calls RenderCurve.
        /// </summary>
        public override void Draw()
        {
            var ctx = CheckContext();
            rendered = true;

            // Stem direction drives the curve arc direction (above or below notes)
            int stemDirection = 1; // default UP
            if (from != null)
            {
                try { stemDirection = from.GetStemDirection(); }
                catch { /* note has no stem — default to UP */ }
            }
            else if (to != null)
            {
                try { stemDirection = to.GetStemDirection(); }
                catch { /* note has no stem — default to UP */ }
            }

            // direction = stem direction * invert factor, matching VexFlow curve.ts:
            //   direction: stem_direction * (this.render_options.invert === true ? -1 : 1)
            int direction = stemDirection * (renderOptions.Invert ? -1 : 1);

            // Compute anchor x/y positions
            double firstX, firstY, lastX, lastY;
            double xShift = renderOptions.X_Shift;

            if (from != null)
            {
                firstX = from.GetTieRightX() + xShift;
                firstY = renderOptions.Position == CurvePosition.NEAR_TOP
                    ? from.GetTieYForTop()
                    : from.GetTieYForBottom();
            }
            else
            {
                // from == null: start at stave left tie-start boundary
                var stave = to!.CheckStave();
                firstX = stave.GetTieStartX();
                firstY = to.GetTieYForBottom();
            }

            if (to != null)
            {
                lastX = to.GetTieLeftX() - xShift;
                lastY  = renderOptions.PositionEnd == CurvePosition.NEAR_TOP
                    ? to.GetTieYForTop()
                    : to.GetTieYForBottom();
            }
            else
            {
                // to == null: end at stave right tie-end boundary
                var stave = from!.CheckStave();
                lastX = stave.GetTieEndX();
                lastY  = firstY;
            }

            // Apply y_shift scaled by direction (matching VexFlow renderCurve params)
            firstY += renderOptions.Y_Shift * direction;
            lastY  += renderOptions.Y_Shift * direction;

            RenderCurve(firstX, firstY, lastX, lastY, direction, ctx);
        }

        /// <inheritdoc />
        public override string GetCategory() => CATEGORY;
    }
}

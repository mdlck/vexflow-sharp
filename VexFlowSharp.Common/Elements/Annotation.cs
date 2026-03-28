// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of vexflow/src/annotation.ts (306 lines)
// Annotation modifier — text above/below notes using TextFormatter for width measurement.

using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Horizontal justification of annotation text relative to the note.
    /// Port of VexFlow's AnnotationHorizontalJustify enum from annotation.ts.
    /// </summary>
    public enum AnnotationHorizontalJustify
    {
        LEFT        = 1,
        CENTER      = 2,
        RIGHT       = 3,
        CENTER_STEM = 4,
    }

    /// <summary>
    /// Vertical justification (placement) of annotation text.
    /// ABOVE/TOP: above the stave. BELOW/BOTTOM: below the stave.
    /// Port of VexFlow's AnnotationVerticalJustify enum from annotation.ts.
    /// </summary>
    public enum AnnotationVerticalJustify
    {
        ABOVE  = 1,
        BELOW  = 2,
        TOP    = 3,
        BOTTOM = 4,
    }

    /// <summary>
    /// Annotation modifier — renders text (lyrics, chord symbols, fingering) above or below notes.
    /// Uses TextFormatter for accurate text width measurement and centering.
    ///
    /// Port of VexFlow's Annotation class from annotation.ts.
    /// </summary>
    public class Annotation : Modifier
    {
        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>Category string used by ModifierContext to group annotations.</summary>
        public const string CATEGORY = "annotations";

        /// <inheritdoc/>
        public override string GetCategory() => CATEGORY;

        // ── Fields ────────────────────────────────────────────────────────────

        private readonly string text;
        private string fontFamily  = "Arial";
        private double fontSize    = 10;
        private string fontStyle   = "";

        private AnnotationHorizontalJustify justify     = AnnotationHorizontalJustify.CENTER;
        private AnnotationVerticalJustify   vertJustify = AnnotationVerticalJustify.ABOVE;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create an annotation with the given text.
        /// Defaults: horizontally centered, positioned above the note.
        /// Port of VexFlow's Annotation(text) constructor.
        /// </summary>
        public Annotation(string text)
        {
            this.text = text;
        }

        // ── Configuration ─────────────────────────────────────────────────────

        /// <summary>Set the text font family, size, and optional style.</summary>
        public Annotation SetFont(string family, double size, string style = "")
        {
            fontFamily = family;
            fontSize   = size;
            fontStyle  = style;
            return this;
        }

        /// <summary>Set horizontal justification of the annotation text.</summary>
        public Annotation SetJustification(AnnotationHorizontalJustify j)
        {
            justify = j;
            return this;
        }

        /// <summary>Get horizontal justification.</summary>
        public AnnotationHorizontalJustify GetJustification() => justify;

        /// <summary>Set vertical justification (above/below note).</summary>
        public Annotation SetVerticalJustification(AnnotationVerticalJustify j)
        {
            vertJustify = j;
            return this;
        }

        /// <summary>Get vertical justification.</summary>
        public AnnotationVerticalJustify GetVerticalJustification() => vertJustify;

        // ── Format ────────────────────────────────────────────────────────────

        /// <summary>
        /// Arrange annotations within a ModifierContext.
        /// Increments TopTextLine for above-note annotations, TextLine for below-note annotations.
        ///
        /// Port of VexFlow's Annotation.format() static from annotation.ts (lines 79-179).
        /// This is a simplified C# port — uses TextFormatter for width but omits stem-height
        /// adjustments (which require runtime glyph measurement not available in unit tests).
        /// </summary>
        public static bool Format(List<Annotation> annotations, ModifierContextState state)
        {
            if (annotations == null || annotations.Count == 0) return false;

            double leftWidth  = 0;
            double rightWidth = 0;

            foreach (var ann in annotations)
            {
                var tf = TextFormatter.Create(ann.fontFamily, ann.fontSize);
                double textWidth = tf.GetWidthForTextInPx(ann.text);

                // Update width tracking based on horizontal justification
                if (ann.justify == AnnotationHorizontalJustify.LEFT)
                    leftWidth = System.Math.Max(leftWidth, textWidth);
                else if (ann.justify == AnnotationHorizontalJustify.RIGHT)
                    rightWidth = System.Math.Max(rightWidth, textWidth);
                else
                {
                    leftWidth  = System.Math.Max(leftWidth, textWidth / 2);
                    rightWidth = System.Math.Max(rightWidth, textWidth / 2);
                }

                // Set text line based on vertical justification
                if (ann.vertJustify == AnnotationVerticalJustify.ABOVE ||
                    ann.vertJustify == AnnotationVerticalJustify.TOP)
                {
                    ann.SetTextLine(state.TopTextLine);
                    state.TopTextLine += 1;
                }
                else
                {
                    ann.SetTextLine(state.TextLine);
                    state.TextLine += 1;
                }
            }

            state.LeftShift  += leftWidth;
            state.RightShift += rightWidth;
            return true;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Render the annotation text near the note, using TextFormatter for centering.
        /// Port of VexFlow's Annotation.draw() from annotation.ts (lines 229-305).
        /// </summary>
        public override void Draw()
        {
            var ctx   = CheckContext();
            var note  = (Note)GetNote();
            var stave = note.CheckStave();
            rendered = true;

            // Use TextFormatter for accurate text width measurement
            var tf        = TextFormatter.Create(fontFamily, fontSize);
            double textWidth = tf.GetWidthForTextInPx(text);

            double noteX = note.GetAbsoluteX();

            // Compute x based on horizontal justification
            double x;
            switch (justify)
            {
                case AnnotationHorizontalJustify.LEFT:
                    x = noteX;
                    break;
                case AnnotationHorizontalJustify.RIGHT:
                    x = noteX - textWidth;
                    break;
                case AnnotationHorizontalJustify.CENTER:
                default:
                    x = noteX - textWidth / 2;
                    break;
            }

            // Compute y based on vertical justification
            double spacing = stave.GetSpacingBetweenLines();
            double y;
            if (vertJustify == AnnotationVerticalJustify.ABOVE ||
                vertJustify == AnnotationVerticalJustify.TOP)
            {
                // Above — use top text position
                y = note.GetYForTopText(textLine) - fontSize / 2;
            }
            else
            {
                // Below — use bottom text position
                y = note.GetYForBottomText(textLine) + fontSize / 2;
            }

            ctx.Save();
            // fontStyle might be "italic" or "" — map to weight/style args
            string weight = fontStyle.Contains("bold")   ? "bold"   : "normal";
            string style  = fontStyle.Contains("italic") ? "italic" : "normal";
            ctx.SetFont(fontFamily, fontSize, weight, style);
            ctx.FillText(text, x, y);
            ctx.Restore();
        }
    }
}

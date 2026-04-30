// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of vexflow/src/annotation.ts (306 lines)
// Annotation modifier — text above/below notes using TextFormatter for width measurement.

using System;
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
        CENTER = 5,
        CENTER_STEM = 6,
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
        public new const string CATEGORY = "Annotation";

        /// <inheritdoc/>
        public override string GetCategory() => CATEGORY;

        /// <summary>Minimum horizontal padding between annotation text and noteheads.</summary>
        public static double MinAnnotationPadding => Metrics.GetDouble("NoteHead.minPadding");

        // ── Fields ────────────────────────────────────────────────────────────

        private readonly string text;
        private string fontFamily  = "Arial";
        private double fontSize    = Metrics.GetDouble("Annotation.fontSize");
        private string fontStyle   = "";

        private AnnotationHorizontalJustify justify     = AnnotationHorizontalJustify.CENTER;
        private AnnotationVerticalJustify   vertJustify = AnnotationVerticalJustify.ABOVE;

        private static readonly Dictionary<string, AnnotationHorizontalJustify> horizontalJustifyStrings =
            new Dictionary<string, AnnotationHorizontalJustify>(StringComparer.Ordinal)
            {
                ["left"] = AnnotationHorizontalJustify.LEFT,
                ["right"] = AnnotationHorizontalJustify.RIGHT,
                ["center"] = AnnotationHorizontalJustify.CENTER,
                ["centerStem"] = AnnotationHorizontalJustify.CENTER_STEM,
            };

        private static readonly Dictionary<string, AnnotationVerticalJustify> verticalJustifyStrings =
            new Dictionary<string, AnnotationVerticalJustify>(StringComparer.Ordinal)
            {
                ["above"] = AnnotationVerticalJustify.TOP,
                ["top"] = AnnotationVerticalJustify.TOP,
                ["below"] = AnnotationVerticalJustify.BOTTOM,
                ["bottom"] = AnnotationVerticalJustify.BOTTOM,
                ["center"] = AnnotationVerticalJustify.CENTER,
                ["centerStem"] = AnnotationVerticalJustify.CENTER_STEM,
            };

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create an annotation with the given text.
        /// Defaults: horizontally centered, positioned above the note.
        /// Port of VexFlow's Annotation(text) constructor.
        /// </summary>
        public Annotation(string text)
        {
            this.text = text;
            UpdateWidth();
        }

        // ── Configuration ─────────────────────────────────────────────────────

        /// <summary>Set the text font family, size, and optional style.</summary>
        public Annotation SetFont(string family, double size, string style = "")
        {
            fontFamily = family;
            fontSize   = size;
            fontStyle  = style;
            UpdateWidth();
            return this;
        }

        /// <summary>Get the annotation text.</summary>
        public string GetText() => text;

        /// <summary>Set horizontal justification of the annotation text.</summary>
        public Annotation SetJustification(AnnotationHorizontalJustify j)
        {
            justify = j;
            return this;
        }

        public Annotation SetJustification(string j)
        {
            if (!horizontalJustifyStrings.TryGetValue(j, out var parsed))
                throw new VexFlowException("BadArgument", $"Invalid annotation horizontal justification: {j}");

            return SetJustification(parsed);
        }

        /// <summary>Get the configured font size in points.</summary>
        public double GetFontSize() => fontSize;

        /// <summary>Get the configured font family.</summary>
        public string GetFontFamily() => fontFamily;

        /// <summary>Get the configured font style string.</summary>
        public string GetFontStyle() => fontStyle;

        /// <summary>Get horizontal justification.</summary>
        public AnnotationHorizontalJustify GetJustification() => justify;

        /// <summary>Set vertical justification (above/below note).</summary>
        public Annotation SetVerticalJustification(AnnotationVerticalJustify j)
        {
            vertJustify = j;
            return this;
        }

        public Annotation SetVerticalJustification(string j)
        {
            if (!verticalJustifyStrings.TryGetValue(j, out var parsed))
                throw new VexFlowException("BadArgument", $"Invalid annotation vertical justification: {j}");

            return SetVerticalJustification(parsed);
        }

        /// <summary>Get vertical justification.</summary>
        public AnnotationVerticalJustify GetVerticalJustification() => vertJustify;

        // ── Format ────────────────────────────────────────────────────────────

        /// <summary>
        /// Arrange annotations within a ModifierContext.
        /// Increments TopTextLine for above-note annotations, TextLine for below-note annotations.
        ///
        /// Port of VexFlow's Annotation.format() static from annotation.ts (lines 79-179).
        /// Uses TextFormatter for width and stem extents for vertical spacing.
        /// </summary>
        public static bool Format(List<Annotation> annotations, ModifierContextState state)
        {
            if (annotations == null || annotations.Count == 0) return false;

            double leftWidth = 0;
            double rightWidth = 0;
            double maxLeftGlyphWidth = 0;
            double maxRightGlyphWidth = 0;

            foreach (var ann in annotations)
            {
                var tf = TextFormatter.Create(ann.fontFamily, ann.fontSize);
                double textWidth = tf.GetWidthForTextInPx(ann.text);
                double textLines = (2 + ann.fontSize) / Tables.STAVE_LINE_DISTANCE;
                double verticalSpaceNeeded = textLines;
                var note = ann.TryGetAttachedNote();
                double glyphWidth = note?.GetMetrics().GlyphWidth ?? 0;

                // Update width tracking based on horizontal justification
                if (ann.justify == AnnotationHorizontalJustify.RIGHT)
                {
                    maxLeftGlyphWidth = System.Math.Max(glyphWidth, maxLeftGlyphWidth);
                    leftWidth = System.Math.Max(leftWidth, textWidth) + MinAnnotationPadding;
                }
                else if (ann.justify == AnnotationHorizontalJustify.LEFT)
                {
                    maxRightGlyphWidth = System.Math.Max(glyphWidth, maxRightGlyphWidth);
                    rightWidth = System.Math.Max(rightWidth, textWidth);
                }
                else
                {
                    leftWidth  = System.Math.Max(leftWidth, textWidth / 2) + MinAnnotationPadding;
                    rightWidth = System.Math.Max(rightWidth, textWidth / 2);
                    maxLeftGlyphWidth = System.Math.Max(glyphWidth / 2, maxLeftGlyphWidth);
                    maxRightGlyphWidth = System.Math.Max(glyphWidth / 2, maxRightGlyphWidth);
                }

                // Set text line based on vertical justification
                if (note == null)
                {
                    if (ann.IsTopJustified())
                    {
                        ann.SetTextLine(state.TopTextLine);
                        state.TopTextLine += 1;
                    }
                    else if (ann.IsBottomJustified())
                    {
                        ann.SetTextLine(state.TextLine);
                        state.TextLine += 1;
                    }
                    else
                    {
                        ann.SetTextLine(state.TextLine);
                    }

                    continue;
                }

                var stave = note.GetStave();
                int lines = stave?.GetNumLines() ?? 5;
                int stemDirection = note.HasStem() ? note.GetStemDirection() : Stem.UP;
                double stemHeight = 0;
                if (note is TabNote tabNote)
                {
                    if (tabNote.RenderOptions.DrawStem)
                    {
                        var stem = tabNote.GetStem();
                        if (stem != null) stemHeight = Math.Abs(stem.GetHeight()) / Tables.STAVE_LINE_DISTANCE;
                    }
                }
                else if (note is StemmableNote stemmable && note.GetNoteType() == "n")
                {
                    var stem = stemmable.GetStem();
                    if (stem != null) stemHeight = Math.Abs(stem.GetHeight()) / Tables.STAVE_LINE_DISTANCE;
                }

                if (ann.IsTopJustified())
                {
                    double noteLine = note.GetLineNumber(true);
                    if (note is TabNote tab)
                        noteLine = lines - (tab.LeastString() - 0.5);

                    if (stemDirection == Stem.UP)
                        noteLine += stemHeight;

                    double curTop = noteLine + state.TopTextLine + 0.5;
                    if (curTop < lines)
                    {
                        ann.SetTextLine(lines - noteLine);
                        verticalSpaceNeeded += lines - noteLine;
                        state.TopTextLine = verticalSpaceNeeded;
                    }
                    else
                    {
                        ann.SetTextLine(state.TopTextLine);
                        state.TopTextLine += verticalSpaceNeeded;
                    }
                }
                else if (ann.IsBottomJustified())
                {
                    double noteLine = lines - note.GetLineNumber();
                    if (note is TabNote tab)
                        noteLine = tab.GreatestString() - 1;

                    if (stemDirection == Stem.DOWN)
                        noteLine += stemHeight;

                    double curBottom = noteLine + state.TextLine + 1;
                    if (curBottom < lines)
                    {
                        ann.SetTextLine(lines - curBottom);
                        verticalSpaceNeeded += lines - curBottom;
                        state.TextLine = verticalSpaceNeeded;
                    }
                    else
                    {
                        ann.SetTextLine(state.TextLine);
                        state.TextLine += verticalSpaceNeeded;
                    }
                }
                else
                {
                    ann.SetTextLine(state.TextLine);
                }
            }

            double rightOverlap = System.Math.Min(
                System.Math.Max(rightWidth - maxRightGlyphWidth, 0),
                System.Math.Max(rightWidth - state.RightShift, 0));
            double leftOverlap = System.Math.Min(
                System.Math.Max(leftWidth - maxLeftGlyphWidth, 0),
                System.Math.Max(leftWidth - state.LeftShift, 0));
            state.LeftShift  += leftOverlap;
            state.RightShift += rightOverlap;
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

            string groupClass = "annotation";
            var classAttribute = GetAttribute("class");
            if (!string.IsNullOrEmpty(classAttribute))
                groupClass += " " + classAttribute;
            ctx.OpenGroup(groupClass, GetId());

            // Use TextFormatter for accurate text width measurement
            var tf        = TextFormatter.Create(fontFamily, fontSize);
            double textWidth = tf.GetWidthForTextInPx(text);

            var start = note.GetModifierStartXY(ModifierPosition.Above, index ?? 0);

            // Compute x based on horizontal justification
            double x;
            switch (justify)
            {
                case AnnotationHorizontalJustify.LEFT:
                    x = start.X;
                    break;
                case AnnotationHorizontalJustify.RIGHT:
                    x = start.X - textWidth;
                    break;
                case AnnotationHorizontalJustify.CENTER_STEM:
                    x = note is StemmableNote stemmable ? stemmable.GetStemX() - textWidth / 2 : start.X - textWidth / 2;
                    break;
                case AnnotationHorizontalJustify.CENTER:
                default:
                    x = start.X - textWidth / 2;
                    break;
            }

            // Compute y based on vertical justification
            int stemDirection = note.HasStem() ? note.GetStemDirection() : Stem.UP;
            double textHeight = fontSize;
            bool hasStem = note.HasStem() && note is StemmableNote;
            (double TopY, double BaseY) stemExt = (0, 0);
            double spacing = 0;
            if (hasStem && note is StemmableNote stemmableForExtents)
            {
                stemExt = stemmableForExtents.CheckStem().GetExtents();
                spacing = stave.GetSpacingBetweenLines();
            }

            double y;
            if (IsBottomJustified())
            {
                var ys = note.GetYs();
                y = Max(ys) + (textLine + 1) * Tables.STAVE_LINE_DISTANCE + textHeight;
                if (hasStem && stemDirection == Stem.DOWN)
                    y = System.Math.Max(y, stemExt.TopY + textHeight + spacing * textLine);
            }
            else if (vertJustify == AnnotationVerticalJustify.CENTER)
            {
                double yt = note.GetYForTopText(textLine) - 1;
                double yb = stave.GetYForBottomText(textLine);
                y = yt + (yb - yt) / 2 + textHeight / 2;
            }
            else if (IsTopJustified())
            {
                double topY = Min(note.GetYs());
                y = topY - (textLine + 1) * Tables.STAVE_LINE_DISTANCE;
                if (hasStem && stemDirection == Stem.UP)
                {
                    double usedSpacing = stemExt.TopY < stave.GetTopLineTopY() ? Tables.STAVE_LINE_DISTANCE : spacing;
                    y = System.Math.Min(y, stemExt.TopY - usedSpacing * (textLine + 1));
                }
            }
            else
            {
                var extents = ((StemmableNote)note).GetStemExtents();
                y = extents.TopY + (extents.BaseY - extents.TopY) / 2 + textHeight / 2;
            }

            ctx.Save();
            // fontStyle might be "italic" or "" — map to weight/style args
            string weight = fontStyle.Contains("bold")   ? "bold"   : "normal";
            string style  = fontStyle.Contains("italic") ? "italic" : "normal";
            ctx.SetFont(fontFamily, fontSize, weight, style);
            ctx.FillText(text, x, y);
            ctx.Restore();
            DrawPointerRect();
            ctx.CloseGroup();
        }

        private bool IsTopJustified()
            => vertJustify == AnnotationVerticalJustify.ABOVE || vertJustify == AnnotationVerticalJustify.TOP;

        private bool IsBottomJustified()
            => vertJustify == AnnotationVerticalJustify.BELOW || vertJustify == AnnotationVerticalJustify.BOTTOM;

        private Note TryGetAttachedNote()
        {
            try { return GetNote() as Note; }
            catch { return null; }
        }

        private void UpdateWidth()
        {
            SetWidth(TextFormatter.Create(fontFamily, fontSize).GetWidthForTextInPx(text));
        }

        private static double Min(double[] values)
        {
            double min = values[0];
            foreach (var value in values) if (value < min) min = value;
            return min;
        }

        private static double Max(double[] values)
        {
            double max = values[0];
            foreach (var value in values) if (value > max) max = value;
            return max;
        }
    }
}

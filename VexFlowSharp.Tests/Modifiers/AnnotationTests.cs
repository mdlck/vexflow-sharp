using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Annotation")]
    [Category("Modifiers")]
    [Category("Phase4")]
    public class AnnotationTests
    {
        [Test]
        public void Lyrics_TextBelowNote()
        {
            // Lyrics are placed below the note using BELOW vertical justification
            var ann = new Annotation("la");
            ann.SetVerticalJustification(AnnotationVerticalJustify.BELOW);
            Assert.AreEqual(AnnotationVerticalJustify.BELOW, ann.GetVerticalJustification());
        }

        [Test]
        public void Simple_ChordSymbolAboveNote()
        {
            // Chord symbols are placed above the note using ABOVE vertical justification
            var ann = new Annotation("C");
            ann.SetVerticalJustification(AnnotationVerticalJustify.ABOVE);
            Assert.AreEqual(AnnotationVerticalJustify.ABOVE, ann.GetVerticalJustification());
        }

        [Test]
        public void Standard_AnnotationAboveNote()
        {
            // Default vertical justification is ABOVE
            var ann = new Annotation("Hello");
            Assert.AreEqual(AnnotationVerticalJustify.ABOVE, ann.GetVerticalJustification());
        }

        [Test]
        public void Styling_CustomFont()
        {
            var ann = new Annotation("C#");
            Assert.DoesNotThrow(() => ann.SetFont("Times New Roman", 14, "italic"));
        }

        [Test]
        public void Styling_DefaultFontSize_ComesFromMetrics()
        {
            var ann = new Annotation("C#");

            Assert.That(ann.GetFontSize(), Is.EqualTo(Metrics.GetDouble("Annotation.fontSize")));
        }

        [Test]
        public void Constructor_SetsMeasuredWidth()
        {
            var ann = new Annotation("C#");
            double expectedWidth = TextFormatter.Create(ann.GetFontFamily(), ann.GetFontSize()).GetWidthForTextInPx("C#");

            Assert.That(ann.GetWidth(), Is.EqualTo(expectedWidth).Within(0.0001));
        }

        [Test]
        public void SetFont_UpdatesMeasuredWidth()
        {
            TextFormatter.ClearRegistry();
            TextFormatter.RegisterInfo(new TextFormatterInfo
            {
                Family = "AnnotationFont",
                Resolution = 1000,
                Glyphs = new Dictionary<string, double>
                {
                    { "A", 900.0 },
                },
            });
            var ann = new Annotation("A");

            ann.SetFont("AnnotationFont", 12);

            Assert.That(ann.GetWidth(), Is.EqualTo(0.9 * 12 * Metrics.GetDouble("TextFormatter.ptToPx")).Within(0.0001));
        }

        [Test]
        public void Harmonic_AnnotationPositioning()
        {
            // Annotation can be centered above note
            var ann = new Annotation("o");
            ann.SetJustification(AnnotationHorizontalJustify.CENTER);
            Assert.AreEqual(AnnotationHorizontalJustify.CENTER, ann.GetJustification());
        }

        [Test]
        public void Picking_FingeringAnnotation()
        {
            // Fingering annotations use LEFT justification
            var ann = new Annotation("1");
            ann.SetJustification(AnnotationHorizontalJustify.LEFT);
            Assert.AreEqual(AnnotationHorizontalJustify.LEFT, ann.GetJustification());
        }

        [Test]
        public void Placement_AboveBelowBoth()
        {
            // Can set both ABOVE and BELOW placements
            var annAbove = new Annotation("chord");
            annAbove.SetVerticalJustification(AnnotationVerticalJustify.ABOVE);

            var annBelow = new Annotation("lyric");
            annBelow.SetVerticalJustification(AnnotationVerticalJustify.BELOW);

            Assert.AreEqual(AnnotationVerticalJustify.ABOVE, annAbove.GetVerticalJustification());
            Assert.AreEqual(AnnotationVerticalJustify.BELOW, annBelow.GetVerticalJustification());
        }

        [Test]
        public void StringAliases_MapToV5JustificationNames()
        {
            var ann = new Annotation("text")
                .SetJustification("centerStem")
                .SetVerticalJustification("center");

            Assert.That(ann.GetJustification(), Is.EqualTo(AnnotationHorizontalJustify.CENTER_STEM));
            Assert.That(ann.GetVerticalJustification(), Is.EqualTo(AnnotationVerticalJustify.CENTER));
        }

        [Test]
        public void Draw_CenterStemJustificationUsesStemX()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            var ann = new Annotation("la").SetJustification("centerStem");
            note.SetStave(stave).SetX(100).AddModifier(ann);
            note.PreFormat();
            ann.SetContext(ctx);

            ann.Draw();

            double textWidth = TextFormatter.Create("Arial", ann.GetFontSize()).GetWidthForTextInPx("la");
            var fill = ctx.GetCall("FillText");
            Assert.That(fill.Args[0], Is.EqualTo(note.GetStemX() - textWidth / 2).Within(0.0001));
        }

        [Test]
        public void Draw_CenterVerticalJustificationPlacesTextBetweenTopAndBottomText()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            var ann = new Annotation("la").SetVerticalJustification("center");
            note.SetStave(stave).SetX(100).AddModifier(ann);
            note.PreFormat();
            ann.SetContext(ctx);

            ann.Draw();

            double yt = note.GetYForTopText(ann.GetTextLine()) - 1;
            double yb = stave.GetYForBottomText(ann.GetTextLine());
            var fill = ctx.GetCall("FillText");
            Assert.That(fill.Args[1], Is.EqualTo(yt + (yb - yt) / 2 + ann.GetFontSize() / 2).Within(0.0001));
        }

        [Test]
        public void Draw_OpensAndClosesV5RenderGroup()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            var ann = new Annotation("la");
            note.SetStave(stave).SetX(100).AddModifier(ann);
            note.PreFormat();
            ann.SetContext(ctx);

            ann.Draw();

            Assert.That(ctx.HasCall("OpenGroup"), Is.True);
            Assert.That(ctx.HasCall("CloseGroup"), Is.True);
            var methods = ctx.Calls.Select(c => c.Method).ToList();
            Assert.That(methods.IndexOf("OpenGroup"), Is.LessThan(methods.IndexOf("CloseGroup")));
        }

        [Test]
        public void Bottom_AnnotationBelowStave()
        {
            // Annotations at BOTTOM position
            var ann = new Annotation("text");
            ann.SetVerticalJustification(AnnotationVerticalJustify.BOTTOM);
            Assert.AreEqual(AnnotationVerticalJustify.BOTTOM, ann.GetVerticalJustification());
        }

        [Test]
        public void HorizontalJustify_CenterIsTwo()
            => Assert.AreEqual(2, (int)AnnotationHorizontalJustify.CENTER);

        [Test]
        public void VerticalJustify_AboveIsOne()
            => Assert.AreEqual(1, (int)AnnotationVerticalJustify.ABOVE);

        [Test]
        public void VerticalJustify_BelowIsTwo()
            => Assert.AreEqual(2, (int)AnnotationVerticalJustify.BELOW);

        [Test]
        public void Format_AboveAnnotation_IncrementsTopTextLine()
        {
            var state = new ModifierContextState { TopTextLine = 0 };
            var ann   = new Annotation("C");
            ann.SetVerticalJustification(AnnotationVerticalJustify.ABOVE);
            var annotations = new List<Annotation> { ann };
            var result = Annotation.Format(annotations, state);
            Assert.IsTrue(result);
            Assert.AreEqual(1.0, state.TopTextLine, 1e-9);
        }

        [Test]
        public void Format_BelowAnnotation_IncrementsTextLine()
        {
            var state = new ModifierContextState { TextLine = 0 };
            var ann   = new Annotation("la");
            ann.SetVerticalJustification(AnnotationVerticalJustify.BELOW);
            var annotations = new List<Annotation> { ann };
            var result = Annotation.Format(annotations, state);
            Assert.IsTrue(result);
            Assert.AreEqual(1.0, state.TextLine, 1e-9);
        }

        [Test]
        public void Format_LeftJustification_ConsumesRightShift()
        {
            var state = new ModifierContextState();
            var ann = new Annotation("la").SetJustification(AnnotationHorizontalJustify.LEFT);

            Annotation.Format(new List<Annotation> { ann }, state);

            Assert.That(state.LeftShift, Is.EqualTo(0));
            Assert.That(state.RightShift, Is.GreaterThan(0));
        }

        [Test]
        public void Format_RightJustification_ConsumesLeftShiftWithPadding()
        {
            var state = new ModifierContextState();
            var ann = new Annotation("la").SetJustification(AnnotationHorizontalJustify.RIGHT);
            double textWidth = TextFormatter.Create("Arial", ann.GetFontSize()).GetWidthForTextInPx("la");

            Annotation.Format(new List<Annotation> { ann }, state);

            Assert.That(state.LeftShift, Is.EqualTo(textWidth + Annotation.MinAnnotationPadding).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(0));
        }

        [Test]
        public void Format_CenterJustification_AddsPaddingOnlyOnLeftShift()
        {
            var state = new ModifierContextState();
            var ann = new Annotation("la");
            double halfTextWidth = TextFormatter.Create("Arial", ann.GetFontSize()).GetWidthForTextInPx("la") / 2;

            Annotation.Format(new List<Annotation> { ann }, state);

            Assert.That(state.LeftShift, Is.EqualTo(halfTextWidth + Annotation.MinAnnotationPadding).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(halfTextWidth).Within(0.0001));
        }

        [Test]
        public void Format_AttachedRightJustificationOnlyReservesOverlapPastNotehead()
        {
            var stave = new Stave(10, 20, 300);
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            var ann = new Annotation("long text").SetJustification(AnnotationHorizontalJustify.RIGHT);
            note.SetStave(stave).AddModifier(ann);
            var state = new ModifierContextState();
            double textWidth = TextFormatter.Create("Arial", ann.GetFontSize()).GetWidthForTextInPx("long text");
            double reservedWidth = textWidth + Annotation.MinAnnotationPadding;
            double expectedOverlap = System.Math.Max(reservedWidth - note.GetMetrics().GlyphWidth, 0);

            Annotation.Format(new List<Annotation> { ann }, state);

            Assert.That(state.LeftShift, Is.EqualTo(expectedOverlap).Within(0.0001));
        }
    }
}

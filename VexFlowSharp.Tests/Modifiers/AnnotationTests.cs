using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;

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
    }
}

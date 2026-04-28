using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Core
{
    [TestFixture]
    [Category("DataStructures")]
    [Category("TypeGuards")]
    public class TypeGuardsTests
    {
        [Test]
        public void IsCategory_ChecksExactCategoryWhenAncestorWalkIsDisabled()
        {
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });

            Assert.That(TypeGuards.IsCategory(note, StaveNote.CATEGORY, checkAncestors: false), Is.True);
            Assert.That(TypeGuards.IsCategory(note, VexFlowSharp.Note.CATEGORY, checkAncestors: false), Is.False);
        }

        [Test]
        public void IsCategory_ChecksAncestorCategoriesByDefault()
        {
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });

            Assert.That(TypeGuards.IsNote(note), Is.True);
            Assert.That(TypeGuards.IsTickable(note), Is.True);
            Assert.That(TypeGuards.IsStemmableNote(note), Is.True);
            Assert.That(TypeGuards.IsStaveNote(note), Is.True);
            Assert.That(TypeGuards.IsTabNote(note), Is.False);
        }

        [Test]
        public void IsCategory_ReturnsFalseForNullAndPrimitiveValues()
        {
            Assert.That(TypeGuards.IsCategory(null, Element.CATEGORY), Is.False);
            Assert.That(TypeGuards.IsCategory(123, Element.CATEGORY), Is.False);
            Assert.That(TypeGuards.IsCategory("not an element", Element.CATEGORY), Is.False);
        }

        [Test]
        public void SpecificHelpers_MatchV5TypeguardSurface()
        {
            Assert.That(TypeGuards.IsAccidental(new Accidental("#")), Is.True);
            Assert.That(TypeGuards.IsModifier(new Accidental("#")), Is.True);
            Assert.That(TypeGuards.IsAnnotation(new Annotation("p")), Is.True);
            Assert.That(TypeGuards.IsBarline(new Barline(BarlineType.Single)), Is.True);
            Assert.That(TypeGuards.IsDot(new Dot()), Is.True);
            Assert.That(TypeGuards.IsGraceNote(new GraceNote(new GraceNoteStruct { Keys = new[] { "d/5" }, Duration = "8" })), Is.True);
            Assert.That(TypeGuards.IsGraceNoteGroup(new GraceNoteGroup(new List<GraceNote> { new GraceNote(new GraceNoteStruct { Keys = new[] { "d/5" }, Duration = "8" }) })), Is.True);
            Assert.That(TypeGuards.IsRenderContext(new RecordingRenderContext()), Is.True);
        }
    }
}

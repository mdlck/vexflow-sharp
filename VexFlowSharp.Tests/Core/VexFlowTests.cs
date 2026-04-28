using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Core
{
    [TestFixture]
    [Category("DataStructures")]
    [Category("VexFlow")]
    public class VexFlowTests
    {
        [TearDown]
        public void TearDown()
        {
            VexFlow.SetFonts("Bravura", "Academico");
        }

        [Test]
        public void BuildMetadata_ExposesV5VersionSurface()
        {
            Assert.That(VexFlow.VERSION, Is.EqualTo("5.0.0"));
            Assert.That(VexFlow.BUILD.VERSION, Is.EqualTo(VexFlow.VERSION));
            Assert.That(VexFlow.BUILD.ID, Is.EqualTo(VexFlow.ID));
            Assert.That(VexFlow.BUILD.DATE, Is.EqualTo(VexFlow.DATE));
            Assert.That(VexFlow.BUILD.INFO, Is.EqualTo(string.Empty));
        }

        [Test]
        public void SetFontsAndGetFonts_UpdateMetricFontFamily()
        {
            VexFlow.SetFonts("Petaluma", "Petaluma Script");

            Assert.That(VexFlow.GetFonts(), Is.EqualTo(new[] { "Petaluma", "Petaluma Script" }));
            Assert.That(Metrics.GetFontInfo("StaveNote").Family, Is.EqualTo("Petaluma,Petaluma Script"));
        }

        [Test]
        public void SetFontsWithoutArguments_RestoresV5DefaultFamily()
        {
            VexFlow.SetFonts("Petaluma");
            VexFlow.SetFonts();

            Assert.That(VexFlow.GetFonts(), Is.EqualTo(new[] { "Bravura", "Academico" }));
        }

        [Test]
        public void FacadeConstants_ExposeTablesValues()
        {
            Assert.That(VexFlow.RENDER_PRECISION_PLACES, Is.EqualTo(Tables.RENDER_PRECISION_PLACES));
            Assert.That(VexFlow.SOFTMAX_FACTOR, Is.EqualTo(Tables.SOFTMAX_FACTOR));
            Assert.That(VexFlow.NOTATION_FONT_SCALE, Is.EqualTo(Tables.NOTATION_FONT_SCALE));
            Assert.That(VexFlow.RESOLUTION, Is.EqualTo(Tables.RESOLUTION));
            Assert.That(VexFlow.SLASH_NOTEHEAD_WIDTH, Is.EqualTo(Tables.SLASH_NOTEHEAD_WIDTH));
            Assert.That(VexFlow.STAVE_LINE_DISTANCE, Is.EqualTo(Tables.STAVE_LINE_DISTANCE));
            Assert.That(VexFlow.STAVE_LINE_THICKNESS, Is.EqualTo(Tables.STAVE_LINE_THICKNESS));
            Assert.That(VexFlow.STEM_HEIGHT, Is.EqualTo(Tables.STEM_HEIGHT));
            Assert.That(VexFlow.STEM_WIDTH, Is.EqualTo(Tables.STEM_WIDTH));
        }

        [Test]
        public void KeySignature_DelegatesToTables()
        {
            Assert.That(VexFlow.KeySignature("D"), Is.EqualTo(Tables.KeySignature("D")));
        }
    }
}

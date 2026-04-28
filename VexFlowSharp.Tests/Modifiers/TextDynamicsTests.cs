using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("TextDynamics")]
    [Category("Modifiers")]
    [Category("Phase4")]
    public class TextDynamicsTests
    {
        [Test]
        public void GetWidth_SingleLetterF_Returns12()
        {
            var td = new TextDynamics("f");
            Assert.AreEqual(12.0, td.GetWidth(), 1e-9);
            Assert.That(td.GetLine(), Is.EqualTo(Metrics.GetDouble("TextDynamics.line")));
        }

        [Test]
        public void GetWidth_Mf_ReturnsSumOfMAndF_29()
        {
            // m=17, f=12 => mf=29
            var td = new TextDynamics("mf");
            Assert.AreEqual(29.0, td.GetWidth(), 1e-9);
        }

        [Test]
        public void GetWidth_Pp_ReturnsSumOfTwoP_28()
        {
            // p=14, p=14 => pp=28
            var td = new TextDynamics("pp");
            Assert.AreEqual(28.0, td.GetWidth(), 1e-9);
        }

        [Test]
        public void GetWidth_Ff_ReturnsSumOfTwoF_24()
        {
            // f=12, f=12 => ff=24
            var td = new TextDynamics("ff");
            Assert.AreEqual(24.0, td.GetWidth(), 1e-9);
        }

        [Test]
        public void GetWidth_Mp_ReturnsSumOfMAndP_31()
        {
            // m=17, p=14 => mp=31
            var td = new TextDynamics("mp");
            Assert.AreEqual(31.0, td.GetWidth(), 1e-9);
        }

        [Test]
        public void TextDynamicsGlyphs_ContainsFPMSZR()
        {
            Assert.IsTrue(Tables.TextDynamicsGlyphs.ContainsKey("f"), "f missing");
            Assert.IsTrue(Tables.TextDynamicsGlyphs.ContainsKey("p"), "p missing");
            Assert.IsTrue(Tables.TextDynamicsGlyphs.ContainsKey("m"), "m missing");
            Assert.IsTrue(Tables.TextDynamicsGlyphs.ContainsKey("s"), "s missing");
            Assert.IsTrue(Tables.TextDynamicsGlyphs.ContainsKey("z"), "z missing");
            Assert.IsTrue(Tables.TextDynamicsGlyphs.ContainsKey("r"), "r missing");
        }

        [Test]
        public void Draw_UnknownLetter_ThrowsInvalidOperation()
        {
            var td = new TextDynamics("x");
            // x is not a valid dynamics letter — GetWidth returns 0, Draw throws
            Assert.AreEqual(0.0, td.GetWidth(), 1e-9);
            Assert.Throws<System.InvalidOperationException>(() => td.PreFormat());
        }

        [Test]
        public void Draw_RendersDynamicsGlyphOutline()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 60, 200);
            stave.SetContext(ctx);
            var td = new TextDynamics("f");
            td.SetX(20).SetStave(stave).SetContext(ctx);

            td.Draw();

            Assert.That(ctx.HasCall("BeginPath"), Is.True);
            Assert.That(ctx.HasCall("Fill"), Is.True);
        }
    }
}

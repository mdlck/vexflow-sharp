// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using NUnit.Framework;
using System.Linq;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.StaveTests
{
    [TestFixture]
    [Category("Clef")]
    public class ClefTests
    {
        // ── Constructor ───────────────────────────────────────────────────────

        [Test]
        public void Clef_Treble_HasPositiveWidth()
        {
            var clef = new Clef("treble");
            Assert.That(clef.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void Clef_Bass_HasPositiveWidth()
        {
            var clef = new Clef("bass");
            Assert.That(clef.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void Clef_Alto_UsesCClef()
        {
            var clef = new Clef("alto");
            Assert.That(clef.GetClefInfo().Code, Is.EqualTo("cClef"));
        }

        [Test]
        public void Clef_Tenor_UsesCClef()
        {
            var clef = new Clef("tenor");
            Assert.That(clef.GetClefInfo().Code, Is.EqualTo("cClef"));
        }

        [Test]
        public void Clef_Treble_UsesGClef()
        {
            var clef = new Clef("treble");
            Assert.That(clef.GetClefInfo().Code, Is.EqualTo("gClef"));
        }

        [Test]
        public void Clef_Bass_UsesFClef()
        {
            var clef = new Clef("bass");
            Assert.That(clef.GetClefInfo().Code, Is.EqualTo("fClef"));
        }

        [Test]
        public void Clef_Percussion_HasPositiveWidth()
        {
            var clef = new Clef("percussion");
            Assert.That(clef.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void Clef_Tab_RendersBravuraGlyphOutline()
        {
            var ctx = new RecordingRenderContext();
            var stave = new TabStave(10, 25, 300);
            stave.SetContext(ctx);
            var clef = new Clef("tab");
            clef.SetX(10);

            clef.Draw(stave, 0);

            Assert.That(clef.GetWidth(), Is.GreaterThan(0));
            Assert.That(ctx.GetCalls("FillText").Any(), Is.False);
            Assert.That(ctx.GetCalls("Fill").Any(), Is.True);
        }

        [Test]
        public void Clef_PetalumaDrawUsesFontSpecificGlyphScale()
        {
            try
            {
                Font.ClearRegistry();
                VexFlow.LoadFonts("Petaluma", "Petaluma Script");
                VexFlow.SetFonts("Petaluma", "Petaluma Script");

                var ctx = new RecordingRenderContext();
                var stave = new Stave(10, 25, 300);
                stave.SetContext(ctx);
                var clef = new Clef("treble");
                clef.SetX(10);

                clef.Draw(stave, 0);

                Assert.That(PetalumaGlyphs.Data.Glyphs.TryGetValue("gClef", out var glyph), Is.True);
                double expectedHeight = GetOutlineYSpan(glyph!.CachedOutline!) * Glyph.GetScale(Clef.GetPoint(), PetalumaGlyphs.Data);
                double legacyBravuraOnlyHeight = GetOutlineYSpan(glyph.CachedOutline!) * Clef.GetPoint() * 0.72 / PetalumaGlyphs.Data.Resolution;
                double actualHeight = GetRecordedPathYSpan(ctx);

                Assert.That(actualHeight, Is.EqualTo(expectedHeight).Within(1.0));
                Assert.That(actualHeight, Is.GreaterThan(legacyBravuraOnlyHeight * 1.2));
            }
            finally
            {
                Font.ClearRegistry();
                Font.Load("Bravura", BravuraGlyphs.Data);
                VexFlow.SetFonts("Bravura", "Academico");
            }
        }

        private static double GetRecordedPathYSpan(RecordingRenderContext ctx)
        {
            var ys = ctx.Calls.SelectMany(call => call.Method switch
            {
                "MoveTo" or "LineTo" => new[] { call.Args[1] },
                "QuadraticCurveTo" => new[] { call.Args[1], call.Args[3] },
                "BezierCurveTo" => new[] { call.Args[1], call.Args[3], call.Args[5] },
                _ => System.Array.Empty<double>(),
            }).ToArray();

            return ys.Max() - ys.Min();
        }

        private static double GetOutlineYSpan(int[] outline)
        {
            double minY = double.PositiveInfinity;
            double maxY = double.NegativeInfinity;
            int i = 0;

            void AddY(double y)
            {
                minY = System.Math.Min(minY, y);
                maxY = System.Math.Max(maxY, y);
            }

            while (i < outline.Length)
            {
                int command = outline[i++];
                switch (command)
                {
                    case 0:
                    case 1:
                        i++;
                        AddY(outline[i++]);
                        break;
                    case 2:
                        i++;
                        AddY(outline[i++]);
                        i++;
                        AddY(outline[i++]);
                        break;
                    case 3:
                        i++;
                        AddY(outline[i++]);
                        i++;
                        AddY(outline[i++]);
                        i++;
                        AddY(outline[i++]);
                        break;
                    default:
                        i = outline.Length;
                        break;
                }
            }

            return maxY - minY;
        }

        // ── Staff line positions ──────────────────────────────────────────────

        [Test]
        public void Clef_Treble_Line_Is3()
        {
            var clef = new Clef("treble");
            Assert.That(clef.GetClefInfo().Line, Is.EqualTo(3));
        }

        [Test]
        public void Clef_Bass_Line_Is1()
        {
            var clef = new Clef("bass");
            Assert.That(clef.GetClefInfo().Line, Is.EqualTo(1));
        }

        [Test]
        public void Clef_Alto_Line_Is2()
        {
            var clef = new Clef("alto");
            Assert.That(clef.GetClefInfo().Line, Is.EqualTo(2));
        }

        // ── Position ─────────────────────────────────────────────────────────

        [Test]
        public void Clef_DefaultPosition_IsBegin()
        {
            var clef = new Clef("treble");
            Assert.That(clef.GetPosition(), Is.EqualTo(StaveModifierPosition.Begin));
        }

        // ── Types map ─────────────────────────────────────────────────────────

        [Test]
        public void Clef_Types_ContainsAllStandardClefs()
        {
            Assert.That(Clef.Types, Contains.Key("treble"));
            Assert.That(Clef.Types, Contains.Key("bass"));
            Assert.That(Clef.Types, Contains.Key("alto"));
            Assert.That(Clef.Types, Contains.Key("tenor"));
            Assert.That(Clef.Types, Contains.Key("percussion"));
        }

        [Test]
        public void Clef_InvalidType_Throws()
        {
            var ex = Assert.Throws<VexFlowException>(() => new Clef("nonexistent"));
            Assert.That(ex!.Code, Is.EqualTo("BadArguments"));
        }

        // ── SetType ───────────────────────────────────────────────────────────

        [Test]
        public void Clef_SetType_ChangesCleInfo()
        {
            var clef = new Clef("treble");
            clef.SetType("bass");
            Assert.That(clef.GetClefInfo().Code, Is.EqualTo("fClef"));
        }

        // ── GetPoint ──────────────────────────────────────────────────────────

        [Test]
        public void GetPoint_Default_IsNotationFontScale()
        {
            Assert.That(Clef.GetPoint("default"), Is.EqualTo(Tables.NOTATION_FONT_SCALE));
        }

        [Test]
        public void GetPoint_Small_IsTwoThirdsOfDefault()
        {
            double expected = (Tables.NOTATION_FONT_SCALE / 3) * 2;
            Assert.That(Clef.GetPoint("small"), Is.EqualTo(expected).Within(0.001));
        }
    }
}

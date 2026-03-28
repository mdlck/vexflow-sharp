// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Skia;

namespace VexFlowSharp.Tests.StaveConnectorTests
{
    [TestFixture]
    [Category("StaveConnector")]
    [Category("Phase5")]
    public class StaveConnectorTests
    {
        [Test]
        [Category("Unit")]
        public void BraceBracketBarlineRenders()
        {
            // Two staves connected with brace, bracket, and single barline connectors.
            // This is a smoke test: create staves, create connectors, draw without exceptions.

            var ctx = SkiaRenderContext.Create(700, 300);

            // Treble stave
            var treble = new Stave(80, 40, 300);
            treble.AddClef("treble");
            treble.SetContext(ctx);
            treble.Draw();

            // Bass stave
            var bass = new Stave(80, 160, 300);
            bass.AddClef("bass");
            bass.SetContext(ctx);
            bass.Draw();

            // Brace connector (left side, between treble and bass)
            var brace = new StaveConnector(treble, bass);
            brace.SetType(StaveConnectorType.Brace);
            brace.SetContext(ctx);
            brace.Draw();

            // Bracket connector
            var bracket = new StaveConnector(treble, bass);
            bracket.SetType("bracket");
            bracket.SetContext(ctx);
            bracket.Draw();

            // Single barline on left
            var single = new StaveConnector(treble, bass);
            single.SetType(StaveConnectorType.Single);
            single.SetContext(ctx);
            single.Draw();

            // Verify enum type string aliases
            Assert.That(brace.GetConnectorType(), Is.EqualTo(StaveConnectorType.Brace));
            Assert.That(bracket.GetConnectorType(), Is.EqualTo(StaveConnectorType.Bracket));
            Assert.That(single.GetConnectorType(), Is.EqualTo(StaveConnectorType.Single));
            Assert.That(single.GetConnectorType(), Is.EqualTo(StaveConnectorType.SingleLeft));
        }

        [Test]
        [Category("Unit")]
        public void AllConnectorTypesDrawWithoutException()
        {
            // Verify all 9 connector types can be drawn without throwing.

            var ctx = SkiaRenderContext.Create(400, 300);

            var top = new Stave(80, 40, 200);
            top.SetContext(ctx);
            top.Draw();

            var bot = new Stave(80, 160, 200);
            bot.SetContext(ctx);
            bot.Draw();

            var types = new[]
            {
                StaveConnectorType.SingleRight,
                StaveConnectorType.SingleLeft,
                StaveConnectorType.Double,
                StaveConnectorType.Brace,
                StaveConnectorType.Bracket,
                StaveConnectorType.BoldDoubleLeft,
                StaveConnectorType.BoldDoubleRight,
                StaveConnectorType.ThinDouble,
                StaveConnectorType.None,
            };

            foreach (var t in types)
            {
                var conn = new StaveConnector(top, bot);
                conn.SetType(t);
                conn.SetContext(ctx);
                Assert.DoesNotThrow(() => conn.Draw(),
                    $"Draw() threw for connector type {t}");
            }
        }

        [Test]
        [Category("Unit")]
        public void SetTypeStringParsesAllValues()
        {
            var top = new Stave(0, 0, 100);
            var bot = new Stave(0, 100, 100);
            var conn = new StaveConnector(top, bot);

            conn.SetType("brace");
            Assert.That(conn.GetConnectorType(), Is.EqualTo(StaveConnectorType.Brace));

            conn.SetType("bracket");
            Assert.That(conn.GetConnectorType(), Is.EqualTo(StaveConnectorType.Bracket));

            conn.SetType("single");
            Assert.That(conn.GetConnectorType(), Is.EqualTo(StaveConnectorType.Single));

            conn.SetType("double");
            Assert.That(conn.GetConnectorType(), Is.EqualTo(StaveConnectorType.Double));

            conn.SetType("boldDoubleLeft");
            Assert.That(conn.GetConnectorType(), Is.EqualTo(StaveConnectorType.BoldDoubleLeft));

            conn.SetType("boldDoubleRight");
            Assert.That(conn.GetConnectorType(), Is.EqualTo(StaveConnectorType.BoldDoubleRight));

            conn.SetType("thinDouble");
            Assert.That(conn.GetConnectorType(), Is.EqualTo(StaveConnectorType.ThinDouble));

            conn.SetType("none");
            Assert.That(conn.GetConnectorType(), Is.EqualTo(StaveConnectorType.None));
        }
    }
}

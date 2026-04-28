// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Skia;
using VexFlowSharp.Tests.Rendering;

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
            Assert.That(brace.GetCategory(), Is.EqualTo(StaveConnector.CATEGORY));
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

        [Test]
        [Category("Unit")]
        public void ThinDoubleConnector_UsesMetricGeometry()
        {
            var ctx = new RecordingRenderContext();
            var top = new Stave(80, 40, 200);
            var bot = new Stave(80, 160, 200);
            var conn = new StaveConnector(top, bot)
                .SetType(StaveConnectorType.ThinDouble)
                .SetContext(ctx);

            conn.Draw();

            var rects = ctx.GetCalls("FillRect").ToArray();
            Assert.That(rects.Length, Is.EqualTo(2));
            Assert.That(rects[0].Args[2], Is.EqualTo(Metrics.GetDouble("StaveConnector.singleLineWidth")).Within(0.0001));
            Assert.That(rects[1].Args[0], Is.EqualTo(rects[0].Args[0] - Metrics.GetDouble("StaveConnector.thinDoubleGap")).Within(0.0001));
        }

        [Test]
        [Category("Unit")]
        public void ConnectorText_UsesMetricOffsets()
        {
            var ctx = new RecordingRenderContext();
            var top = new Stave(80, 40, 200);
            var bot = new Stave(80, 160, 200);
            var conn = new StaveConnector(top, bot)
                .SetType(StaveConnectorType.None)
                .SetText("Piano", shiftX: 2, shiftY: 3)
                .SetContext(ctx);

            conn.Draw();

            Assert.That(ctx.GetCall("SetLineWidth").Args[0], Is.EqualTo(Metrics.GetDouble("StaveConnector.textLineWidth")).Within(0.0001));
            Assert.That(ctx.GetCall("FillText").Args[0], Is.EqualTo(80 - Metrics.GetDouble("StaveConnector.textXOffset") + 2).Within(0.0001));
            Assert.That(
                ctx.GetCall("FillText").Args[1],
                Is.EqualTo((top.GetYForLine(0) + bot.GetYForLine(bot.GetNumLines() - 1)) / 2.0 + 3 + Metrics.GetDouble("StaveConnector.textYOffset")).Within(0.0001));
        }
    }
}

using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("TextBracket")]
    public class TextBracketTests
    {
        private static (RecordingRenderContext Ctx, GhostNote Start, GhostNote Stop) MakeNotes()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 260);
            stave.SetContext(ctx);
            var start = new GhostNote("q");
            var stop = new GhostNote("q");
            start.SetStave(stave).SetX(60);
            stop.SetStave(stave).SetX(180);
            start.SetContext(ctx);
            stop.SetContext(ctx);
            return (ctx, start, stop);
        }

        [Test]
        public void Constructor_StoresParamsAndDefaults()
        {
            var (_, start, stop) = MakeNotes();
            var bracket = new TextBracket(new TextBracketParams
            {
                Start = start,
                Stop = stop,
                Text = "8",
                Superscript = "va",
            });

            Assert.That(bracket.GetStart(), Is.SameAs(start));
            Assert.That(bracket.GetStop(), Is.SameAs(stop));
            Assert.That(bracket.GetText(), Is.EqualTo("8"));
            Assert.That(bracket.GetSuperscript(), Is.EqualTo("va"));
            Assert.That(bracket.GetPosition(), Is.EqualTo(TextBracketPosition.Top));
            Assert.That(bracket.RenderOptions.Dashed, Is.True);
            Assert.That(bracket.RenderOptions.LineWidth, Is.EqualTo(Metrics.GetDouble("TextBracket.lineWidth")));
            Assert.That(bracket.RenderOptions.BracketHeight, Is.EqualTo(Metrics.GetDouble("TextBracket.bracketHeight")));
        }

        [Test]
        public void Draw_TopDashed_RendersTextAndDashedBracket()
        {
            var (ctx, start, stop) = MakeNotes();
            var bracket = new TextBracket(new TextBracketParams
            {
                Start = start,
                Stop = stop,
                Text = "8",
                Superscript = "va",
            });
            bracket.SetContext(ctx);

            bracket.Draw();

            Assert.That(ctx.GetCalls("FillText").Count(), Is.EqualTo(2));
            Assert.That(ctx.GetCall("SetLineWidth").Args[0], Is.EqualTo(Metrics.GetDouble("TextBracket.lineWidth")));
            Assert.That(ctx.GetCalls("SetLineDash").Count(), Is.GreaterThanOrEqualTo(2));
            Assert.That(ctx.GetCalls("Stroke").Count(), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Draw_BottomSolid_RendersSinglePath()
        {
            var (ctx, start, stop) = MakeNotes();
            var bracket = new TextBracket(new TextBracketParams
            {
                Start = start,
                Stop = stop,
                Text = "15",
                Superscript = "vb",
                Position = TextBracketPosition.Bottom,
            });
            bracket.SetDashed(false).SetLine(2).SetContext(ctx);

            bracket.Draw();

            Assert.That(ctx.GetCalls("FillText").Count(), Is.EqualTo(2));
            Assert.That(ctx.GetCalls("SetLineDash").Count(), Is.EqualTo(0));
            Assert.That(ctx.GetCalls("LineTo").Count(), Is.GreaterThanOrEqualTo(2));
            Assert.That(ctx.HasCall("Stroke"), Is.True);
        }
    }
}

using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Tremolo")]
    [Category("Modifiers")]
    public class TremoloTests
    {
        private static GhostNote MakeStemmedNote(int stemDirection = Stem.UP)
        {
            var note = new GhostNote("4");
            note.SetX(20);
            note.BuildStem();
            note.SetStemDirection(stemDirection);
            return note;
        }

        [Test]
        public void Constructor_DefaultsToCenterPosition()
        {
            var tremolo = new Tremolo(3);

            Assert.That(tremolo.GetCategory(), Is.EqualTo(Tremolo.CATEGORY));
            Assert.That(tremolo.GetPosition(), Is.EqualTo(ModifierPosition.Center));
            Assert.That(tremolo.GetNum(), Is.EqualTo(3));
        }

        [Test]
        public void Draw_RendersOneGlyphPerTremoloBar()
        {
            var ctx = new RecordingRenderContext();
            var note = MakeStemmedNote();
            var tremolo = new Tremolo(3);
            note.AddModifier(tremolo, 0);
            tremolo.SetContext(ctx);

            tremolo.Draw();

            Assert.That(ctx.GetCalls("Fill").Count(), Is.EqualTo(3));
        }

        [Test]
        public void FontSize_FallsBackToRootMetric()
            => Assert.That(Metrics.GetDouble("Tremolo.fontSize"), Is.EqualTo(30));

        [Test]
        public void Draw_UsesMetricSpacingFromStemTip()
        {
            var ctx = new RecordingRenderContext();
            var note = MakeStemmedNote(Stem.UP);
            var tremolo = new Tremolo(2);
            note.AddModifier(tremolo, 0);
            tremolo.SetContext(ctx);

            tremolo.Draw();

            var firstMoves = ctx.GetCalls("MoveTo").Take(2).ToArray();
            Assert.That(firstMoves, Has.Length.EqualTo(2));
            double spacing = firstMoves[1].Args[1] - firstMoves[0].Args[1];
            double expected = Metrics.GetDouble("Tremolo.spacing");
            Assert.That(spacing, Is.EqualTo(expected).Within(0.001));
        }

        [Test]
        public void Draw_DownStemSpacesInOppositeDirection()
        {
            var ctx = new RecordingRenderContext();
            var note = MakeStemmedNote(Stem.DOWN);
            var tremolo = new Tremolo(2);
            note.AddModifier(tremolo, 0);
            tremolo.SetContext(ctx);

            tremolo.Draw();

            var firstMoves = ctx.GetCalls("MoveTo").Take(2).ToArray();
            Assert.That(firstMoves, Has.Length.EqualTo(2));
            double spacing = firstMoves[1].Args[1] - firstMoves[0].Args[1];
            double expected = -Metrics.GetDouble("Tremolo.spacing");
            Assert.That(spacing, Is.EqualTo(expected).Within(0.001));
        }
    }
}

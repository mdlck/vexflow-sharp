using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Api;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("FretHandFinger")]
    [Category("Modifiers")]
    public class FretHandFingerTests
    {
        private static StaveNote MakeNote()
        {
            return new StaveNote(new StaveNoteStruct
            {
                Keys = new[] { "c/4" },
                Duration = "4",
            });
        }

        [Test]
        public void Constructor_DefaultsToLeftPosition()
        {
            var fingering = new FretHandFinger("1");

            Assert.That(fingering.GetCategory(), Is.EqualTo(FretHandFinger.CATEGORY));
            Assert.That(fingering.GetFretHandFinger(), Is.EqualTo("1"));
            Assert.That(fingering.GetPosition(), Is.EqualTo(ModifierPosition.Left));
            Assert.That(fingering.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void SetFretHandFinger_UpdatesTextAndWidth()
        {
            var fingering = new FretHandFinger("1");
            var oldWidth = fingering.GetWidth();

            fingering.SetFretHandFinger("123");

            Assert.That(fingering.GetFretHandFinger(), Is.EqualTo("123"));
            Assert.That(fingering.GetWidth(), Is.GreaterThan(oldWidth));
        }

        [Test]
        public void SetOffsets_AreStored()
        {
            var fingering = new FretHandFinger("2")
                .SetOffsetX(4)
                .SetOffsetY(-3);

            Assert.That(fingering.GetOffsetX(), Is.EqualTo(4));
            Assert.That(fingering.GetOffsetY(), Is.EqualTo(-3));
        }

        [Test]
        public void Format_LeftFinger_ConsumesLeftShift()
        {
            var note = MakeNote();
            var fingering = new FretHandFinger("1");
            fingering.SetNote(note);
            fingering.SetIndex(0);

            var state = new ModifierContextState { LeftShift = 3 };

            FretHandFinger.Format(new List<FretHandFinger> { fingering }, state);

            Assert.That(fingering.GetXShift(), Is.EqualTo(-6).Within(0.0001));
            Assert.That(state.LeftShift, Is.EqualTo(6 + fingering.GetWidth() + Metrics.GetDouble("FretHandFinger.numSpacing")).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(0));
        }

        [Test]
        public void Format_RightFinger_ConsumesRightShift()
        {
            var note = MakeNote();
            var fingering = new FretHandFinger("1");
            fingering.SetPosition(ModifierPosition.Right);
            fingering.SetNote(note);
            fingering.SetIndex(0);

            var state = new ModifierContextState { RightShift = 4 };

            FretHandFinger.Format(new List<FretHandFinger> { fingering }, state);

            Assert.That(fingering.GetXShift(), Is.EqualTo(4).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(4 + fingering.GetWidth() + Metrics.GetDouble("FretHandFinger.numSpacing")).Within(0.0001));
            Assert.That(state.LeftShift, Is.EqualTo(0));
        }

        [Test]
        public void Format_AboveAndBelowFinger_ReserveTextLines()
        {
            var note = MakeNote();
            var above = new FretHandFinger("1");
            above.SetPosition(ModifierPosition.Above);
            above.SetNote(note);
            above.SetIndex(0);

            var below = new FretHandFinger("2");
            below.SetPosition(ModifierPosition.Below);
            below.SetNote(note);
            below.SetIndex(0);

            var state = new ModifierContextState();

            FretHandFinger.Format(new List<FretHandFinger> { above, below }, state);

            double expectedLineIncrement = Metrics.GetDouble("FretHandFinger.fontSize") / Tables.STAVE_LINE_DISTANCE + 0.5;
            Assert.That(state.TopTextLine, Is.EqualTo(expectedLineIncrement).Within(0.0001));
            Assert.That(state.TextLine, Is.EqualTo(expectedLineIncrement).Within(0.0001));
        }

        [Test]
        public void Draw_RendersTextWithMetricFont()
        {
            var ctx = new RecordingRenderContext();
            var note = new GhostNote("4").SetX(20);
            var fingering = new FretHandFinger("3");
            note.AddModifier(fingering, 0);
            fingering.SetContext(ctx);

            fingering.Draw();

            Assert.That(ctx.HasCall("SetFont"), Is.True);
            Assert.That(ctx.GetCall("SetFont").Args[0], Is.EqualTo(Metrics.GetDouble("FretHandFinger.fontSize")));
            Assert.That(ctx.HasCall("FillText"), Is.True);
        }

        [TestCase(ModifierPosition.Above, "FretHandFinger.aboveXShift", "FretHandFinger.aboveYShift")]
        [TestCase(ModifierPosition.Below, "FretHandFinger.belowXShift", "FretHandFinger.belowYShift")]
        public void Draw_AboveAndBelowOffsetsComeFromMetrics(ModifierPosition position, string xMetric, string yMetric)
        {
            var ctx = new RecordingRenderContext();
            var note = new GhostNote("4").SetX(20);
            var fingering = new FretHandFinger("3").SetPosition(position);
            note.AddModifier(fingering, 0);
            fingering.SetContext(ctx);
            var start = note.GetModifierStartXY(position, 0);

            fingering.Draw();

            var fillText = ctx.GetCall("FillText");
            Assert.That(fillText.Args[0], Is.EqualTo(start.X + Metrics.GetDouble(xMetric)).Within(0.0001));
            Assert.That(fillText.Args[1], Is.EqualTo(
                start.Y
                + Metrics.GetDouble("FretHandFinger.baseYShift")
                + Metrics.GetDouble(yMetric)).Within(0.0001));
        }

        [Test]
        public void Draw_RightOffsetComesFromMetrics()
        {
            var ctx = new RecordingRenderContext();
            var note = new GhostNote("4").SetX(20);
            var fingering = new FretHandFinger("3").SetPosition(ModifierPosition.Right);
            note.AddModifier(fingering, 0);
            fingering.SetContext(ctx);
            var start = note.GetModifierStartXY(ModifierPosition.Right, 0);

            fingering.Draw();

            var fillText = ctx.GetCall("FillText");
            Assert.That(fillText.Args[0], Is.EqualTo(start.X + Metrics.GetDouble("FretHandFinger.rightXShift")).Within(0.0001));
            Assert.That(fillText.Args[1], Is.EqualTo(start.Y + Metrics.GetDouble("FretHandFinger.baseYShift")).Within(0.0001));
        }

        [Test]
        public void Factory_Fingering_CreatesFretHandFinger()
        {
            var factory = new Factory(new RecordingRenderContext(), 200, 100);

            var fingering = factory.Fingering("4", ModifierPosition.Right);

            Assert.That(fingering.GetFretHandFinger(), Is.EqualTo("4"));
            Assert.That(fingering.GetPosition(), Is.EqualTo(ModifierPosition.Right));
        }
    }
}

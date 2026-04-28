using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Api;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("StringNumber")]
    [Category("Modifiers")]
    public class StringNumberTests
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
        public void Constructor_DefaultsToAbovePosition()
        {
            var stringNumber = new StringNumber("3");

            Assert.That(stringNumber.GetCategory(), Is.EqualTo(StringNumber.CATEGORY));
            Assert.That(stringNumber.GetStringNumber(), Is.EqualTo("3"));
            Assert.That(stringNumber.GetPosition(), Is.EqualTo(ModifierPosition.Above));
            Assert.That(
                stringNumber.GetWidth(),
                Is.EqualTo(Metrics.GetDouble("StringNumber.radius") * 2 + Metrics.GetDouble("StringNumber.circlePadding")));
            Assert.That(stringNumber.GetDrawCircle(), Is.True);
        }

        [Test]
        public void Setters_UpdateStoredValues()
        {
            var stringNumber = new StringNumber("1")
                .SetStringNumber("4")
                .SetOffsetX(3)
                .SetOffsetY(-2)
                .SetStemOffset(5)
                .SetDashed(false)
                .SetDrawCircle(false)
                .SetLineEndType(RendererLineEndType.Down);

            Assert.That(stringNumber.GetStringNumber(), Is.EqualTo("4"));
            Assert.That(stringNumber.GetOffsetX(), Is.EqualTo(3));
            Assert.That(stringNumber.GetOffsetY(), Is.EqualTo(-2));
            Assert.That(stringNumber.GetStemOffset(), Is.EqualTo(5));
            Assert.That(stringNumber.IsDashed(), Is.False);
            Assert.That(stringNumber.GetDrawCircle(), Is.False);
            Assert.That(stringNumber.GetLineEndType(), Is.EqualTo(RendererLineEndType.Down));
        }

        [Test]
        public void Format_LeftNumber_ConsumesLeftShift()
        {
            var note = MakeNote();
            var stringNumber = new StringNumber("1");
            stringNumber.SetPosition(ModifierPosition.Left);
            stringNumber.SetNote(note);
            stringNumber.SetIndex(0);

            var state = new ModifierContextState { LeftShift = 3 };

            StringNumber.Format(new List<StringNumber> { stringNumber }, state);

            Assert.That(stringNumber.GetXShift(), Is.EqualTo(-3).Within(0.0001));
            Assert.That(state.LeftShift, Is.EqualTo(3 + stringNumber.GetWidth() + Metrics.GetDouble("StringNumber.numSpacing")).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(0));
        }

        [Test]
        public void Format_RightNumber_ConsumesRightShift()
        {
            var note = MakeNote();
            var stringNumber = new StringNumber("1");
            stringNumber.SetPosition(ModifierPosition.Right);
            stringNumber.SetNote(note);
            stringNumber.SetIndex(0);

            var state = new ModifierContextState { RightShift = 4 };

            StringNumber.Format(new List<StringNumber> { stringNumber }, state);

            Assert.That(stringNumber.GetXShift(), Is.EqualTo(4).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(4 + stringNumber.GetWidth() + Metrics.GetDouble("StringNumber.numSpacing")).Within(0.0001));
            Assert.That(state.LeftShift, Is.EqualTo(0));
        }

        [Test]
        public void Format_AboveAndBelowNumbers_ReserveTextLinesWhenModifierContextIsAvailable()
        {
            var note = MakeNote();
            var mc = new ModifierContext();
            mc.AddMember(note);

            var above = new StringNumber("1");
            above.SetPosition(ModifierPosition.Above);
            note.AddModifier(above, 0);

            var below = new StringNumber("2");
            below.SetPosition(ModifierPosition.Below);
            note.AddModifier(below, 0);

            var state = new ModifierContextState();

            StringNumber.Format(new List<StringNumber> { above, below }, state);

            double expectedLineIncrement = Metrics.GetDouble("StringNumber.radius") * 2 / Tables.STAVE_LINE_DISTANCE + 0.5;
            Assert.That(state.TopTextLine, Is.EqualTo(expectedLineIncrement).Within(0.0001));
            Assert.That(state.TextLine, Is.EqualTo(expectedLineIncrement).Within(0.0001));
        }

        [Test]
        public void Draw_RendersCircleTextAndExtensionLine()
        {
            var ctx = new RecordingRenderContext();
            var note = new GhostNote("4");
            note.SetX(20);
            note.BuildStem();

            var last = new GhostNote("4");
            last.SetX(70);
            last.SetStemDirection(Stem.UP);

            var stringNumber = new StringNumber("3")
                .SetLastNote(last)
                .SetLineEndType(RendererLineEndType.Up);
            stringNumber.SetPosition(ModifierPosition.Right);
            note.AddModifier(stringNumber, 0);
            stringNumber.SetContext(ctx);

            stringNumber.Draw();

            Assert.That(ctx.HasCall("Arc"), Is.True);
            Assert.That(ctx.GetCall("Arc").Args[2], Is.EqualTo(Metrics.GetDouble("StringNumber.radius")));
            Assert.That(ctx.GetCalls("SetLineWidth").Select(call => call.Args[0]), Does.Contain(Metrics.GetDouble("StringNumber.circleLineWidth")));
            Assert.That(ctx.GetCalls("SetLineWidth").Select(call => call.Args[0]), Does.Contain(Metrics.GetDouble("StringNumber.extensionLineWidth")));
            Assert.That(ctx.HasCall("SetFont"), Is.True);
            Assert.That(ctx.GetCall("SetFont").Args[0], Is.EqualTo(Metrics.GetDouble("StringNumber.fontSize")));
            Assert.That(ctx.HasCall("FillText"), Is.True);
            Assert.That(
                ctx.GetCall("FillText").Args[1],
                Is.EqualTo(ctx.GetCall("Arc").Args[1] + Metrics.GetDouble("StringNumber.textBaselineShift")).Within(0.0001));
            Assert.That(ctx.HasCall("SetLineDash"), Is.True);
            Assert.That(ctx.GetCall("SetLineDash").Args, Is.EqualTo(new[]
            {
                Metrics.GetDouble("StringNumber.extensionDash"),
                Metrics.GetDouble("StringNumber.extensionGap"),
            }));
            Assert.That(ctx.GetCalls("LineTo"), Is.Not.Empty);
            Assert.That(
                ctx.GetCalls("LineTo").Last().Args[1],
                Is.EqualTo(ctx.GetCall("Arc").Args[1] - Metrics.GetDouble("StringNumber.legLength")).Within(0.0001));
        }

        [Test]
        public void Factory_StringNumber_CreatesStringNumber()
        {
            var factory = new Factory(new RecordingRenderContext(), 200, 100);

            var stringNumber = factory.StringNumber("5", ModifierPosition.Below, drawCircle: false);

            Assert.That(stringNumber.GetStringNumber(), Is.EqualTo("5"));
            Assert.That(stringNumber.GetPosition(), Is.EqualTo(ModifierPosition.Below));
            Assert.That(stringNumber.GetDrawCircle(), Is.False);
        }
    }
}

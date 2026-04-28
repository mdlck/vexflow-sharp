using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Parenthesis")]
    [Category("Modifiers")]
    public class ParenthesisTests
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
        public void Constructor_SelectsGlyphForPosition()
        {
            var left = new Parenthesis(ModifierPosition.Left);
            var right = new Parenthesis(ModifierPosition.Right);

            Assert.That(left.GetCategory(), Is.EqualTo(Parenthesis.CATEGORY));
            Assert.That(left.GetGlyphCode(), Is.EqualTo("noteheadParenthesisLeft"));
            Assert.That(right.GetGlyphCode(), Is.EqualTo("noteheadParenthesisRight"));
            Assert.That(left.GetWidth(), Is.GreaterThan(0));
            Assert.That(right.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void BuildAndAttach_AddsLeftAndRightForEachKey()
        {
            var note = new StaveNote(new StaveNoteStruct
            {
                Keys = new[] { "c/4", "e/4" },
                Duration = "4",
            });

            Parenthesis.BuildAndAttach(new List<VexFlowSharp.Note> { note });

            var parentheses = note.GetModifiers().OfType<Parenthesis>().ToArray();
            Assert.That(parentheses, Has.Length.EqualTo(4));
            Assert.That(parentheses.Count(p => p.GetPosition() == ModifierPosition.Left), Is.EqualTo(2));
            Assert.That(parentheses.Count(p => p.GetPosition() == ModifierPosition.Right), Is.EqualTo(2));
        }

        [Test]
        public void Format_LeftParenthesis_ConsumesLeftShiftAndSetsNegativeXShift()
        {
            var note = MakeNote();
            var parenthesis = new Parenthesis(ModifierPosition.Left);
            note.AddModifier(parenthesis, 0);

            var state = new ModifierContextState();

            Parenthesis.Format(new List<Parenthesis> { parenthesis }, state);

            Assert.That(parenthesis.GetXShift(), Is.EqualTo(-parenthesis.GetWidth()).Within(0.0001));
            Assert.That(state.LeftShift, Is.EqualTo(parenthesis.GetWidth() * 2).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(0));
        }

        [Test]
        public void Format_RightParenthesis_ConsumesRightShift()
        {
            var note = MakeNote();
            var parenthesis = new Parenthesis(ModifierPosition.Right);
            note.AddModifier(parenthesis, 0);

            var state = new ModifierContextState();

            Parenthesis.Format(new List<Parenthesis> { parenthesis }, state);

            Assert.That(parenthesis.GetXShift(), Is.EqualTo(0).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(parenthesis.GetWidth()).Within(0.0001));
            Assert.That(state.LeftShift, Is.EqualTo(0));
        }

        [Test]
        public void GetFirstDotPx_IncludesParenthesisWidthWhenRegistered()
        {
            var note = MakeNote();
            var parenthesis = new Parenthesis(ModifierPosition.Right);
            note.AddModifier(parenthesis, 0);

            var mc = new ModifierContext();
            mc.AddMember(note);

            Assert.That(note.GetFirstDotPx(), Is.EqualTo(parenthesis.GetWidth() + 1).Within(0.0001));
        }

        [Test]
        public void Draw_RendersGlyphOutline()
        {
            var ctx = new RecordingRenderContext();
            var note = new GhostNote("4").SetX(20);
            var parenthesis = new Parenthesis(ModifierPosition.Left);
            note.AddModifier(parenthesis, 0);
            parenthesis.SetContext(ctx);

            parenthesis.Draw();

            Assert.That(ctx.HasCall("BeginPath"), Is.True);
            Assert.That(ctx.HasCall("Fill"), Is.True);
        }
    }
}

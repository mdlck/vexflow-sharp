using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("StaveHairpin")]
    [Category("Modifiers")]
    public class StaveHairpinTests
    {
        private static (Stave Stave, StaveNote First, StaveNote Last) MakeNotes()
        {
            var stave = new Stave(10, 40, 320);
            var first = new StaveNote(new StaveNoteStruct { Duration = "4", Keys = new[] { "c/4" } });
            var last = new StaveNote(new StaveNoteStruct { Duration = "4", Keys = new[] { "d/4" } });

            first.SetStave(stave).SetX(60);
            last.SetStave(stave).SetX(160);
            first.PreFormat();
            last.PreFormat();

            return (stave, first, last);
        }

        [Test]
        public void Constructor_UsesV5CategoryAndDefaults()
        {
            var (_, first, last) = MakeNotes();
            var hairpin = new StaveHairpin(new StaveHairpinNotes
            {
                FirstNote = first,
                LastNote = last,
            }, StaveHairpinType.Crescendo);

            Assert.That(hairpin.GetCategory(), Is.EqualTo(StaveHairpin.CATEGORY));
            Assert.That(StaveHairpin.CATEGORY, Is.EqualTo("StaveHairpin"));
            Assert.That(hairpin.GetPosition(), Is.EqualTo(ModifierPosition.Below));
            Assert.That(hairpin.RenderOptions.Height, Is.EqualTo(10));
        }

        [Test]
        public void Draw_Crescendo_RendersNarrowToWideHairpin()
        {
            var (_, first, last) = MakeNotes();
            var ctx = new RecordingRenderContext();
            var hairpin = new StaveHairpin(new StaveHairpinNotes
            {
                FirstNote = first,
                LastNote = last,
            }, StaveHairpin.Type.CRESC);

            hairpin.SetContext(ctx);
            hairpin.Draw();

            Assert.That(hairpin.IsRendered(), Is.True);
            Assert.That(ctx.HasCall("MoveTo"), Is.True);
            Assert.That(ctx.GetCalls("LineTo").Count(), Is.EqualTo(2));
            Assert.That(ctx.HasCall("Stroke"), Is.True);
            Assert.That(ctx.HasCall("ClosePath"), Is.True);
        }

        [Test]
        public void Draw_Decrescendo_AppliesRenderOptionShifts()
        {
            var (_, first, last) = MakeNotes();
            var ctx = new RecordingRenderContext();
            var hairpin = new StaveHairpin(new StaveHairpinNotes
            {
                FirstNote = first,
                LastNote = last,
            }, StaveHairpinType.Decrescendo)
                .SetRenderOptions(new StaveHairpinRenderOptions
                {
                    LeftShiftPx = 3,
                    RightShiftPx = 7,
                    Height = 14,
                    YShift = 2,
                });

            hairpin.SetContext(ctx);
            hairpin.Draw();

            var move = ctx.GetCall("MoveTo").Args;
            var firstStart = first.GetModifierStartXY(ModifierPosition.Below, 0);

            Assert.That(move[0], Is.EqualTo(firstStart.X + 3).Within(0.0001));
        }

        [Test]
        public void SetPosition_AcceptsOnlyAboveAndBelow()
        {
            var (_, first, last) = MakeNotes();
            var hairpin = new StaveHairpin(new StaveHairpinNotes
            {
                FirstNote = first,
                LastNote = last,
            }, StaveHairpinType.Crescendo);

            Assert.That(hairpin.SetPosition(ModifierPosition.Above), Is.SameAs(hairpin));
            Assert.That(hairpin.GetPosition(), Is.EqualTo(ModifierPosition.Above));

            hairpin.SetPosition(ModifierPosition.Left);
            Assert.That(hairpin.GetPosition(), Is.EqualTo(ModifierPosition.Above));
        }

        [Test]
        public void Draw_AbovePosition_UsesAboveStaffY()
        {
            var (stave, first, last) = MakeNotes();
            var ctx = new RecordingRenderContext();
            var hairpin = new StaveHairpin(new StaveHairpinNotes
            {
                FirstNote = first,
                LastNote = last,
            }, StaveHairpin.Type.CRESC)
                .SetPosition(ModifierPosition.Above)
                .SetRenderOptions(new StaveHairpinRenderOptions { Height = 10 });

            hairpin.SetContext(ctx);
            hairpin.Draw();

            var move = ctx.GetCall("MoveTo").Args;
            var firstY = stave.GetY() + stave.GetHeight();
            var expectedY = firstY - stave.GetHeight() + 10;

            Assert.That(move[1], Is.EqualTo(expectedY).Within(0.0001));
        }

        [Test]
        public void Constructor_RequiresAtLeastOneNote()
        {
            Assert.Throws<VexFlowException>(() =>
                new StaveHairpin(new StaveHairpinNotes(), StaveHairpinType.Crescendo));
        }

        [Test]
        public void Draw_RequiresBothNotes()
        {
            var (_, first, _) = MakeNotes();
            var ctx = new RecordingRenderContext();
            var hairpin = new StaveHairpin(new StaveHairpinNotes
            {
                FirstNote = first,
            }, StaveHairpinType.Crescendo);

            hairpin.SetContext(ctx);

            var ex = Assert.Throws<VexFlowException>(() => hairpin.Draw());
            Assert.That(ex!.Code, Is.EqualTo("NoNote"));
            Assert.That(hairpin.IsRendered(), Is.True);
        }

        [Test]
        public void FormatByTicksAndDraw_ConvertsTickShiftsToPixels()
        {
            var (_, first, last) = MakeNotes();
            var ctx = new RecordingRenderContext();

            StaveHairpin.FormatByTicksAndDraw(
                ctx,
                pixelsPerTick: 2,
                notes: new StaveHairpinNotes { FirstNote = first, LastNote = last },
                type: StaveHairpin.Type.DECRESC,
                position: ModifierPosition.Below,
                options: new StaveHairpinRenderOptions
                {
                    LeftShiftTicks = 3,
                    RightShiftTicks = 5,
                    Height = 12,
                    YShift = 1,
                });

            var move = ctx.GetCall("MoveTo").Args;
            var firstStart = first.GetModifierStartXY(ModifierPosition.Below, 0);

            Assert.That(move[0], Is.EqualTo(firstStart.X + 6).Within(0.0001));
        }

        [Test]
        public void FormatByTicksAndDraw_UsesDrawWithStyle()
        {
            var (_, first, last) = MakeNotes();
            var ctx = new RecordingRenderContext();

            StaveHairpin.FormatByTicksAndDraw(
                ctx,
                pixelsPerTick: 1,
                notes: new StaveHairpinNotes { FirstNote = first, LastNote = last },
                type: StaveHairpin.Type.CRESC,
                position: ModifierPosition.Below,
                options: new StaveHairpinRenderOptions
                {
                    Height = 12,
                    YShift = 1,
                });

            Assert.That(ctx.Calls[0].Method, Is.EqualTo("Save"));
            Assert.That(ctx.HasCall("Stroke"), Is.True);
            Assert.That(ctx.Calls[^1].Method, Is.EqualTo("Restore"));
        }

        [Test]
        public void FormatByTicksAndDraw_RequiresPixelsPerTick()
        {
            var (_, first, last) = MakeNotes();
            var ctx = new RecordingRenderContext();

            var ex = Assert.Throws<VexFlowException>(() =>
                StaveHairpin.FormatByTicksAndDraw(
                    ctx,
                    pixelsPerTick: 0,
                    notes: new StaveHairpinNotes { FirstNote = first, LastNote = last },
                    type: StaveHairpin.Type.CRESC,
                    position: ModifierPosition.Below,
                    options: new StaveHairpinRenderOptions()));

            Assert.That(ex!.Code, Is.EqualTo("BadArguments"));
        }
    }
}

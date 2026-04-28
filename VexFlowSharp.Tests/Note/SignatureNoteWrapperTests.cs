using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("SignatureNoteWrappers")]
    public class SignatureNoteWrapperTests
    {
        private static (RecordingRenderContext Ctx, VexFlowSharp.Stave Stave) MakeStave()
        {
            var ctx = new RecordingRenderContext();
            var stave = new VexFlowSharp.Stave(10, 60, 260);
            stave.SetContext(ctx);
            return (ctx, stave);
        }

        [Test]
        public void KeySigNote_PreFormatSetsWidthAndIgnoresTicks()
        {
            var (_, stave) = MakeStave();
            var note = new KeySigNote("D");
            note.SetStave(stave);

            note.PreFormat();

            Assert.That(note.ShouldIgnoreTicks(), Is.True);
            Assert.That(note.GetWidth(), Is.GreaterThan(0));
            Assert.That(note.GetCategory(), Is.EqualTo(KeySigNote.CATEGORY));
        }

        [Test]
        public void KeySigNote_DoesNotRegisterWithModifierContext()
        {
            var note = new KeySigNote("D");
            var context = new ModifierContext();

            note.AddToModifierContext(context);

            Assert.That(context.GetMembers(KeySigNote.CATEGORY).Count, Is.EqualTo(0));
        }

        [Test]
        public void TimeSigNote_DrawRendersTimeSignatureGlyphs()
        {
            var (ctx, stave) = MakeStave();
            var note = new TimeSigNote("4/4");
            note.SetStave(stave).SetX(70);

            note.Draw();

            Assert.That(note.ShouldIgnoreTicks(), Is.True);
            Assert.That(note.GetWidth(), Is.GreaterThan(0));
            Assert.That(ctx.HasCall("Fill"), Is.True);
        }

        [Test]
        public void ClefNote_SetTypeUpdatesClefAndWidth()
        {
            var (ctx, stave) = MakeStave();
            var note = new ClefNote("treble");
            note.SetType("bass", "small");
            note.SetStave(stave).SetX(70);

            note.Draw();

            Assert.That(note.GetClef().GetClefTypeName(), Is.EqualTo("bass"));
            Assert.That(note.GetWidth(), Is.GreaterThan(0));
            Assert.That(ctx.HasCall("Fill"), Is.True);
        }
    }
}

using System.Linq;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Api;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("TabNote")]
    public class TabNoteTests
    {
        private static TabNoteStruct Struct(params TabNotePosition[] positions)
        {
            return new TabNoteStruct
            {
                Duration = "4",
                Positions = positions,
            };
        }

        [Test]
        public void TabStave_UsesSixLinesAndTabSpacing()
        {
            var stave = new TabStave(10, 20, 300);

            Assert.That(stave.GetCategory(), Is.EqualTo(TabStave.CATEGORY));
            Assert.That(stave.GetNumLines(), Is.EqualTo((int)Metrics.GetDouble("TabStave.numLines")));
            Assert.That(stave.GetSpacingBetweenLines(), Is.EqualTo(Metrics.GetDouble("TabStave.spacingBetweenLinesPx")));
        }

        [Test]
        public void Constructor_StoresPositionsAndWidth()
        {
            var note = new TabNote(Struct(
                new TabNotePosition { Str = 2, Fret = 3 },
                new TabNotePosition { Str = 4, Fret = "10" }));

            Assert.That(note.GetCategory(), Is.EqualTo(TabNote.CATEGORY));
            Assert.That(note.GetPositions().Length, Is.EqualTo(2));
            Assert.That(note.GreatestString(), Is.EqualTo(4));
            Assert.That(note.LeastString(), Is.EqualTo(2));
            Assert.That(note.GetWidth(), Is.GreaterThan(0));
        }

        [Test]
        public void SetStave_ComputesStringYs()
        {
            var ctx = new RecordingRenderContext();
            var stave = new TabStave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new TabNote(Struct(new TabNotePosition { Str = 3, Fret = 7 }));

            note.SetStave(stave);

            Assert.That(note.GetYs()[0], Is.EqualTo(stave.GetYForLine(2)));
            Assert.That(note.GetContext(), Is.SameAs(ctx));
        }

        [Test]
        public void Draw_RendersFretTextAndClearsStaveBehindIt()
        {
            var ctx = new RecordingRenderContext();
            var stave = new TabStave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new TabNote(Struct(new TabNotePosition { Str = 1, Fret = "7" }));
            note.SetStave(stave).SetContext(ctx);

            note.Draw();

            Assert.That(ctx.HasCall("ClearRect"), Is.True);
            Assert.That(ctx.HasCall("FillText"), Is.True);
        }

        [Test]
        public void Draw_RendersMutedStringAsGlyph()
        {
            var ctx = new RecordingRenderContext();
            var stave = new TabStave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new TabNote(Struct(new TabNotePosition { Str = 1, Fret = "X" }));
            note.SetStave(stave).SetContext(ctx);

            note.Draw();

            Assert.That(ctx.HasCall("ClearRect"), Is.True);
            Assert.That(ctx.HasCall("Fill"), Is.True);
            Assert.That(ctx.HasCall("FillText"), Is.False);
        }

        [Test]
        public void Draw_UsesV5GroupClassAndElementId()
        {
            var ctx = new RecordingRenderContext();
            var stave = new TabStave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new TabNote(Struct(new TabNotePosition { Str = 1, Fret = "7" }));
            note.SetAttribute("id", "tab-a");
            note.SetAttribute("class", "picked muted");
            note.SetStave(stave);

            note.Draw();

            Assert.That(ctx.Groups[0].Class, Is.EqualTo("tabnote picked muted"));
            Assert.That(ctx.Groups[0].Id, Is.EqualTo("tab-a"));
        }

        [Test]
        public void Draw_BeamedTabNoteSkipsOwnStem()
        {
            var ctx = new RecordingRenderContext();
            var stave = new TabStave(10, 20, 300);
            stave.SetContext(ctx);
            var first = new TabNote(new TabNoteStruct
            {
                Duration = "8",
                Positions = new[] { new TabNotePosition { Str = 2, Fret = 3 } },
            }, drawStem: true);
            var second = new TabNote(new TabNoteStruct
            {
                Duration = "8",
                Positions = new[] { new TabNotePosition { Str = 2, Fret = 5 } },
            }, drawStem: true);
            first.SetStave(stave).SetX(40);
            second.SetStave(stave).SetX(90);
            _ = new Beam(new System.Collections.Generic.List<StemmableNote> { first, second });

            first.Draw();

            Assert.That(ctx.GetCalls("LineTo").Count(), Is.EqualTo(0));
        }

        [Test]
        public void DrawStemThroughStave_SplitsStemAroundOccupiedStrings()
        {
            var ctx = new RecordingRenderContext();
            var stave = new TabStave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new TabNote(Struct(
                new TabNotePosition { Str = 2, Fret = 3 },
                new TabNotePosition { Str = 4, Fret = 5 }), true);
            note.RenderOptions.DrawStemThroughStave = true;
            note.SetStave(stave).SetContext(ctx);

            note.Draw();

            Assert.That(ctx.GetCalls("LineTo").Count(), Is.EqualTo(3));
        }

        [Test]
        public void GraceTabNote_UsesGraceCategoryAndYOffset()
        {
            var note = new GraceTabNote(Struct(new TabNotePosition { Str = 1, Fret = 5 }));

            Assert.That(note.GetCategory(), Is.EqualTo(GraceTabNote.CATEGORY));
            Assert.That(note.RenderOptions.YShift, Is.EqualTo(Metrics.GetDouble("GraceTabNote.yShift")));
        }

        [Test]
        public void TabSlide_InfersDirectionAndDrawsLine()
        {
            var ctx = new RecordingRenderContext();
            var stave = new TabStave(10, 20, 300);
            stave.SetContext(ctx);
            var first = new TabNote(Struct(new TabNotePosition { Str = 2, Fret = 3 }));
            var last = new TabNote(Struct(new TabNotePosition { Str = 2, Fret = 5 }));
            first.SetStave(stave).SetX(40);
            last.SetStave(stave).SetX(120);

            var slide = new TabSlide(new TieNotes { FirstNote = first, LastNote = last });
            slide.SetContext(ctx);
            slide.Draw();

            Assert.That(slide.GetDirection(), Is.EqualTo(TabSlide.SLIDE_UP));
            Assert.That(slide.RenderOptions.Cp1, Is.EqualTo(Metrics.GetDouble("TabSlide.cp1")));
            Assert.That(slide.RenderOptions.Cp2, Is.EqualTo(Metrics.GetDouble("TabSlide.cp2")));
            Assert.That(slide.RenderOptions.YShift, Is.EqualTo(Metrics.GetDouble("TabSlide.yShift")));
            Assert.That(ctx.HasCall("LineTo"), Is.True);
        }

        [Test]
        public void TabTie_UsesMetricCurveDefaults()
        {
            var tie = new TabTie(new TieNotes());

            Assert.That(tie.RenderOptions.Cp1, Is.EqualTo(Metrics.GetDouble("TabTie.cp1")));
            Assert.That(tie.RenderOptions.Cp2, Is.EqualTo(Metrics.GetDouble("TabTie.cp2")));
            Assert.That(tie.RenderOptions.YShift, Is.EqualTo(Metrics.GetDouble("TabTie.yShift")));
        }

        [Test]
        public void TabSlide_DrawsOneLineForEachFirstIndex()
        {
            var ctx = new RecordingRenderContext();
            var stave = new TabStave(10, 20, 300);
            stave.SetContext(ctx);
            var first = new TabNote(Struct(
                new TabNotePosition { Str = 2, Fret = 3 },
                new TabNotePosition { Str = 4, Fret = 5 }));
            var last = new TabNote(Struct(
                new TabNotePosition { Str = 2, Fret = 5 },
                new TabNotePosition { Str = 4, Fret = 7 }));
            first.SetStave(stave).SetX(40);
            last.SetStave(stave).SetX(120);

            var slide = new TabSlide(new TieNotes { FirstNote = first, LastNote = last, FirstIndexes = new[] { 0, 1 } });
            slide.SetContext(ctx);
            slide.Draw();

            Assert.That(ctx.GetCalls("LineTo").Count(), Is.EqualTo(2));
            Assert.That(ctx.HasCall("FillText"), Is.True);
        }

        [Test]
        public void Factory_CreatesTabStaveAndTabNote()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 400, 200);

            var stave = factory.TabStave(10, 20, 300);
            var note = factory.TabNote(Struct(new TabNotePosition { Str = 1, Fret = 3 }));

            Assert.That(stave, Is.SameAs(note.GetStave()));
            Assert.That(note.GetCategory(), Is.EqualTo(TabNote.CATEGORY));
        }
    }
}

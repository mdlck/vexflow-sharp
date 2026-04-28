using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("StaveLine")]
    [Category("Phase5")]
    public class StaveLineTests
    {
        private static StaveNote MakeNote(double x, params double[] ys)
        {
            var note = new StaveNote(new StaveNoteStruct
            {
                Keys = new[] { "c/4", "e/4" },
                Duration = "4",
            });
            note.SetX(x);
            note.SetYs(ys);
            return note;
        }

        [Test]
        public void Constructor_StoresV5Params()
        {
            var from = MakeNote(20, 40, 30);
            var to = MakeNote(80, 42, 32);
            var font = new MetricsFontInfo { Family = "Arial", Size = 10 };

            var line = new StaveLine(new StaveLineParams
            {
                From = from,
                To = to,
                FirstIndexes = new List<int> { 1 },
                LastIndexes = new List<int> { 0 },
                Options = new StaveLineTextOptions { Text = "gliss.", Font = font },
            });

            Assert.That(line.GetCategory(), Is.EqualTo(StaveLine.CATEGORY));
            Assert.That(line.GetStart(), Is.SameAs(from));
            Assert.That(line.GetStop(), Is.SameAs(to));
            Assert.That(line.GetFirstIndexes(), Is.EqualTo(new[] { 1 }));
            Assert.That(line.GetLastIndexes(), Is.EqualTo(new[] { 0 }));
            Assert.That(line.GetText(), Is.EqualTo("gliss."));
            Assert.That(line.GetFontInfo(), Is.SameAs(font));
        }

        [Test]
        public void Draw_RendersLineAndOptionalText()
        {
            var ctx = new RecordingRenderContext();
            var from = MakeNote(20, 40, 30);
            var to = MakeNote(80, 42, 32);

            var line = new StaveLine(new StaveLineParams
            {
                From = from,
                To = to,
                FirstIndexes = new List<int> { 1 },
                LastIndexes = new List<int> { 0 },
                Options = new StaveLineTextOptions { Text = "gliss.", Font = new MetricsFontInfo { Size = 9 } },
            });
            line.SetContext(ctx);

            line.Draw();

            Assert.That(ctx.HasCall("MoveTo"), Is.True);
            Assert.That(ctx.GetCall("MoveTo").Args[1], Is.EqualTo(30));
            Assert.That(ctx.HasCall("LineTo"), Is.True);
            Assert.That(ctx.GetCall("LineTo").Args[1], Is.EqualTo(42));
            Assert.That(ctx.HasCall("Stroke"), Is.True);
            Assert.That(ctx.HasCall("SetFont"), Is.True);
            Assert.That(ctx.GetCall("SetFont").Args[0], Is.EqualTo(9));
            Assert.That(ctx.HasCall("FillText"), Is.True);
        }
    }
}

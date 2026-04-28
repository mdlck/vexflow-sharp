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
    [Category("ChordSymbol")]
    public class ChordSymbolTests
    {
        private static GhostNote Note(double x = 50)
        {
            var note = new GhostNote("q");
            note.SetX(x);
            note.SetYs(new[] { 80.0 });
            var stave = new VexFlowSharp.Stave(10, 60, 300);
            stave.SetContext(new RecordingRenderContext());
            note.SetStave(stave);
            return note;
        }

        [Test]
        public void AddTextAndGlyphOrText_BuildsSymbolBlocks()
        {
            var chord = new ChordSymbol()
                .AddText("F7")
                .AddGlyphOrText("(#11b9)", SymbolModifiers.Superscript);

            Assert.That(chord.GetSymbolBlocks().Count, Is.EqualTo(2));
            Assert.That(chord.GetSymbolBlocks()[0].GetText(), Is.EqualTo("F7"));
            Assert.That(chord.GetSymbolBlocks()[1].IsSuperscript(), Is.True);
        }

        [Test]
        public void Format_StacksSuperscriptAndSubscriptAndReportsWidth()
        {
            var note = Note();
            var chord = new ChordSymbol()
                .AddText("F7")
                .AddGlyphOrText("#11", SymbolModifiers.Superscript)
                .AddGlyphOrText("b9", SymbolModifiers.Subscript);
            chord.SetNote(note);

            var state = new ModifierContextState();
            var formatted = ChordSymbol.Format(new List<ChordSymbol> { chord }, state);

            Assert.That(formatted, Is.True);
            Assert.That(chord.GetWidth(), Is.GreaterThan(0));
            Assert.That(state.TopTextLine, Is.EqualTo(2));
            Assert.That(chord.GetSymbolBlocks()[2].VAlign, Is.True);
        }

        [Test]
        public void SuperSubscriptOffsets_UseVexFlowPixelFontSize()
        {
            var chord = new ChordSymbol().SetFontSize(12)
                .AddTextSuperscript("9")
                .AddTextSubscript("11");
            var superscript = chord.GetSymbolBlocks()[0];
            var subscript = chord.GetSymbolBlocks()[1];
            double fontSizeInPixels = 12 * Metrics.GetDouble("TextFormatter.ptToPx");

            Assert.That(superscript.GetYShift(), Is.EqualTo(ChordSymbol.SuperscriptOffset * fontSizeInPixels).Within(0.001));
            Assert.That(subscript.GetYShift(), Is.EqualTo(ChordSymbol.SubscriptOffset * fontSizeInPixels).Within(0.001));
        }

        [Test]
        public void Draw_RendersEachBlockAsText()
        {
            var ctx = new RecordingRenderContext();
            var note = Note();
            note.SetContext(ctx);
            var chord = new ChordSymbol()
                .AddText("C")
                .AddGlyphSuperscript("majorSeventh");
            chord.SetNote(note);
            ChordSymbol.Format(new List<ChordSymbol> { chord }, new ModifierContextState());
            chord.SetContext(ctx);

            chord.Draw();

            Assert.That(ctx.GetCalls("FillText").Count(), Is.EqualTo(2));
            Assert.That(chord.GetBoundingBox(), Is.Not.Null);
        }

        [Test]
        public void FactoryChordSymbol_AppliesJustificationAndFontSize()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 400, 200);

            var chord = factory.ChordSymbol(fontSize: 16, hJustify: "centerStem", vJustify: "bottom")
                .AddText("C")
                .AddTextSuperscript("Maj.");

            Assert.That(chord.GetHorizontal(), Is.EqualTo(ChordSymbolHorizontalJustify.CenterStem));
            Assert.That(chord.GetVertical(), Is.EqualTo(ChordSymbolVerticalJustify.Bottom));
            Assert.That(chord.GetSymbolBlocks()[0].GetWidth(), Is.GreaterThan(0));
        }
    }
}

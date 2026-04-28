using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Vibrato")]
    [Category("Modifiers")]
    [Category("Phase4")]
    public class VibratoTests
    {
        [Test]
        public void Constructor_UsesV5WiggleGlyphDefaults()
        {
            var vibrato = new Vibrato();

            Assert.That(vibrato.GetVibratoCode(), Is.EqualTo(0xeab0));
            Assert.That(vibrato.GetWidth(), Is.EqualTo(Metrics.GetDouble("Vibrato.width")));
            Assert.That(vibrato.GetText(), Is.Not.Empty);
        }

        [Test]
        public void Harsh_ZigzagVibratoWaveAboveNote()
        {
            var vibrato = new Vibrato();
            vibrato.SetHarsh(true);
            Assert.IsTrue(vibrato.IsHarsh);
        }

        [Test]
        public void Format_ThreeArgSignature_Compiles()
        {
            // Verify the 3-arg signature exists (pitfall 3 guard)
            var state   = new ModifierContextState();
            var mc      = new ModifierContext();
            var vibrato = new Vibrato();
            var vibratos = new List<Vibrato> { vibrato };
            // Should not throw — textLine assignment only, no note access in Format()
            Assert.DoesNotThrow(() => Vibrato.Format(vibratos, state, mc));
        }

        [Test]
        public void Format_EmptyList_ReturnsFalse()
        {
            var state  = new ModifierContextState();
            var mc     = new ModifierContext();
            var result = Vibrato.Format(new List<Vibrato>(), state, mc);
            Assert.IsFalse(result);
        }

        [Test]
        public void VibratoRenderOptions_DefaultWidthIsTwenty()
        {
            var opts = new VibratoRenderOptions();
            Assert.AreEqual(Metrics.GetDouble("Vibrato.width"), opts.Width, 1e-9);
        }

        [Test]
        public void SetVibratoCode_RebuildsText()
        {
            var vibrato = new Vibrato();
            vibrato.SetVibratoCode(0xeab1);

            Assert.That(vibrato.GetVibratoCode(), Is.EqualTo(0xeab1));
            Assert.That(vibrato.GetText(), Is.Not.Empty);
        }

        [Test]
        public void Draw_RendersTextGlyphRun()
        {
            var ctx = new RecordingRenderContext();
            var note = new GhostNote("q").SetX(40);
            note.SetYs(new[] { 80.0 });

            var vibrato = new Vibrato();
            vibrato.SetNote(note);
            vibrato.SetContext(ctx);

            vibrato.Draw();

            Assert.That(ctx.HasCall("FillText"), Is.True);
            Assert.That(ctx.GetCall("FillText").Args[1], Is.EqualTo(note.GetYForTopText(0) + Metrics.GetDouble("Vibrato.yShift")).Within(0.0001));
            Assert.That(ctx.HasCall("QuadraticCurveTo"), Is.False);
        }

        [Test]
        public void Format_UsesMetricRightShiftAndTextLineIncrement()
        {
            var state = new ModifierContextState { RightShift = 12 };
            var mc = new ModifierContext();
            var vibrato = new Vibrato();

            Vibrato.Format(new List<Vibrato> { vibrato }, state, mc);

            Assert.That(vibrato.GetXShift(), Is.EqualTo(12 - Metrics.GetDouble("Vibrato.rightShift")).Within(0.0001));
            Assert.That(state.TopTextLine, Is.EqualTo(Metrics.GetDouble("Vibrato.textLineIncrement")).Within(0.0001));
        }

        [Test]
        public void VibratoRenderOptions_DefaultNotHarsh()
        {
            var opts = new VibratoRenderOptions();
            Assert.IsFalse(opts.Harsh);
        }
    }
}

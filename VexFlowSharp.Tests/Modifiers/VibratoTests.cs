using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Vibrato")]
    [Category("Modifiers")]
    [Category("Phase4")]
    public class VibratoTests
    {
        [Test]
        public void Simple_SmoothVibratoWaveAboveNote()
        {
            // Smooth vibrato is default (Harsh=false)
            var vibrato = new Vibrato();
            Assert.IsFalse(vibrato.IsHarsh);
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
        public void VibratoRenderOptions_DefaultWaveWidthIsFour()
        {
            var opts = new VibratoRenderOptions();
            Assert.AreEqual(4.0, opts.WaveWidth, 1e-9);
        }

        [Test]
        public void VibratoRenderOptions_DefaultNotHarsh()
        {
            var opts = new VibratoRenderOptions();
            Assert.IsFalse(opts.Harsh);
        }
    }
}

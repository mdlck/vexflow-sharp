using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.TableTests
{
    [TestFixture]
    [Category("Tables")]
    [Category("Tuning")]
    public class TuningTests
    {
        [Test]
        public void Constructor_DefaultsToV5EightStringTuning()
        {
            var tuning = new Tuning();

            Assert.That(tuning.GetValueForString(1), Is.EqualTo(64));
            Assert.That(tuning.GetValueForString(6), Is.EqualTo(40));
            Assert.That(tuning.GetValueForString(8), Is.EqualTo(28));
        }

        [Test]
        public void SetTuning_ExpandsV5NamedTunings()
        {
            var tuning = new Tuning("standard");

            Assert.That(tuning.GetValueForString(1), Is.EqualTo(64));
            Assert.That(tuning.GetValueForString(6), Is.EqualTo(40));

            tuning.SetTuning("dropd");
            Assert.That(tuning.GetValueForString(6), Is.EqualTo(38));

            tuning.SetTuning("standardBanjo");
            Assert.That(tuning.GetValueForString(5), Is.EqualTo(67));
        }

        [Test]
        public void CustomTuningString_AcceptsCommaSeparatedNotesWithWhitespace()
        {
            var tuning = new Tuning("D/5, A/4, G/4, D/4");

            Assert.That(tuning.GetValueForString("1"), Is.EqualTo(62));
            Assert.That(tuning.GetValueForString("4"), Is.EqualTo(50));
        }

        [Test]
        public void GetValueForFret_AddsFretToOpenStringValue()
        {
            var tuning = new Tuning("standard");

            Assert.That(tuning.GetValueForFret(3, 6), Is.EqualTo(43));
            Assert.That(tuning.GetValueForFret("12", "1"), Is.EqualTo(76));
        }

        [Test]
        public void GetNoteForFret_ConvertsTabPositionToNoteName()
        {
            var tuning = new Tuning("standard");

            Assert.That(tuning.GetNoteForFret(3, 6), Is.EqualTo("G/3"));
            Assert.That(tuning.GetNoteForFret("1", "2"), Is.EqualTo("C/5"));
        }

        [Test]
        public void InvalidStringOrFret_ThrowsBadArguments()
        {
            var tuning = new Tuning("standard");

            var stringEx = Assert.Throws<VexFlowException>(() => tuning.GetValueForString(7));
            var fretEx = Assert.Throws<VexFlowException>(() => tuning.GetValueForFret(-1, 1));

            Assert.That(stringEx!.Code, Is.EqualTo("BadArguments"));
            Assert.That(fretEx!.Code, Is.EqualTo("BadArguments"));
        }
    }
}

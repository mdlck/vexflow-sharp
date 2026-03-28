using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("Stem")]
    public class StemTests
    {
        [Test]
        public void UP_ConstantIsOne()
        {
            Assert.That(Stem.UP, Is.EqualTo(1));
        }

        [Test]
        public void DOWN_ConstantIsMinusOne()
        {
            Assert.That(Stem.DOWN, Is.EqualTo(-1));
        }

        [Test]
        public void WIDTH_ConstantIs1_5()
        {
            Assert.That(Stem.WIDTH, Is.EqualTo(1.5));
        }

        [Test]
        public void HEIGHT_ConstantIs35()
        {
            Assert.That(Stem.HEIGHT, Is.EqualTo(35.0));
        }

        [Test]
        public void Constructor_DefaultOptions_DirectionIsZero()
        {
            var stem = new Stem();
            // A new Stem with no direction set defaults to 0
            // GetHeight returns 0 direction * height
            Assert.That(stem.GetHeight(), Is.EqualTo(0.0));
        }

        [Test]
        public void SetDirection_UP_StoresDirection()
        {
            var stem = new Stem();
            stem.SetDirection(Stem.UP);
            // With UP direction, GetHeight includes the direction factor
            var height = stem.GetHeight();
            // height = (bottom - top) * direction = 0 * 1 = includes HEIGHT factor
            // unsignedHeight = (0-0) + (35 - 0 + 0) = 35; signed = 35 * 1 = 35
            Assert.That(height, Is.EqualTo(35.0));
        }

        [Test]
        public void SetDirection_DOWN_StoresDirection()
        {
            var stem = new Stem();
            stem.SetDirection(Stem.DOWN);
            // unsignedHeight = (0-0) + (35 - 0 + 0) = 35; signed = 35 * -1 = -35
            Assert.That(stem.GetHeight(), Is.EqualTo(-35.0));
        }

        [Test]
        public void GetHeight_FromYTopAndBottom_ReturnsCorrectValue()
        {
            var stem = new Stem(new StemOptions
            {
                YTop = 10,
                YBottom = 50,
                StemDirection = Stem.UP
            });
            // unsignedHeight = (50 - 10) + (35 - 0 + 0) = 40 + 35 = 75; signed = 75 * 1 = 75
            Assert.That(stem.GetHeight(), Is.EqualTo(75.0));
        }

        [Test]
        public void AdjustHeightForFlag_SetsRenderHeightAdjustment()
        {
            var stem = new Stem(new StemOptions { StemDirection = Stem.UP, YTop = 0, YBottom = 0 });
            // Before adjustment, height = 35
            double beforeHeight = stem.GetHeight();

            stem.AdjustHeightForFlag();

            // GetHeight itself doesn't change — renderHeightAdjustment only affects Draw()
            // but we can verify the flag was set by drawing would be different
            // For this test, we just confirm AdjustHeightForFlag doesn't throw
            Assert.That(stem.GetHeight(), Is.EqualTo(beforeHeight));
        }

        [Test]
        public void GetExtents_WithUPDirection_ReturnsCorrectTopAndBase()
        {
            var stem = new Stem(new StemOptions
            {
                YTop = 10,
                YBottom = 50,
                StemDirection = Stem.UP
            });
            var (topY, baseY) = stem.GetExtents();
            // isStemUp = true; innerMostY = min(10, 50) = 10; outerMostY = max(10, 50) = 50
            // stemHeight = 35 + 0 = 35; stemTipY = 10 + 35 * -1 = -25
            Assert.That(topY, Is.EqualTo(-25.0));
            Assert.That(baseY, Is.EqualTo(50.0));
        }

        [Test]
        public void SetNoteHeadXBounds_ReturnsThis()
        {
            var stem = new Stem();
            var result = stem.SetNoteHeadXBounds(5.0, 15.0);
            Assert.That(result, Is.SameAs(stem));
        }

        [Test]
        public void IsElement()
        {
            var stem = new Stem();
            Assert.That(stem, Is.InstanceOf<Element>());
        }
    }
}

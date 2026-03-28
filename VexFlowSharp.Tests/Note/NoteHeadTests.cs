using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Note
{
    [TestFixture]
    [Category("NoteHead")]
    public class NoteHeadTests
    {
        [Test]
        public void Constructor_WithLine3_StoresLine()
        {
            // Line 3 = B4 in treble clef (middle line)
            var nh = new NoteHead(new NoteHeadStruct
            {
                Duration = "4",
                NoteType = "n",
                Line = 3.0,
                X = 10.0,
                Y = 50.0,
            });
            Assert.That(nh.GetLine(), Is.EqualTo(3.0));
        }

        [Test]
        public void GetX_ReturnsConstructedX()
        {
            var nh = new NoteHead(new NoteHeadStruct
            {
                Duration = "4",
                NoteType = "n",
                X = 42.5,
                Y = 100.0,
            });
            Assert.That(nh.GetX(), Is.EqualTo(42.5));
        }

        [Test]
        public void SetX_GetX_RoundTrip()
        {
            var nh = new NoteHead(new NoteHeadStruct
            {
                Duration = "4",
                NoteType = "n",
            });
            nh.SetX(75.0);
            Assert.That(nh.GetX(), Is.EqualTo(75.0));
        }

        [Test]
        public void GetY_ReturnsConstructedY()
        {
            var nh = new NoteHead(new NoteHeadStruct
            {
                Duration = "4",
                NoteType = "n",
                X = 0,
                Y = 99.0,
            });
            Assert.That(nh.GetY(), Is.EqualTo(99.0));
        }

        [Test]
        public void SetLine_GetLine_RoundTrip()
        {
            var nh = new NoteHead(new NoteHeadStruct { Duration = "4", NoteType = "n" });
            nh.SetLine(2.5);
            Assert.That(nh.GetLine(), Is.EqualTo(2.5));
        }

        [Test]
        public void GetWidth_ReturnsNonZero_ForStandardNotehead()
        {
            var nh = new NoteHead(new NoteHeadStruct
            {
                Duration = "4",
                NoteType = "n",
            });
            // Standard notehead should have a non-zero width
            Assert.That(nh.GetWidth(), Is.GreaterThan(0.0));
        }

        [Test]
        public void IsDisplaced_DefaultsFalse()
        {
            var nh = new NoteHead(new NoteHeadStruct { Duration = "4", NoteType = "n" });
            Assert.That(nh.IsDisplaced(), Is.False);
        }

        [Test]
        public void SetDisplaced_SetsValue()
        {
            var nh = new NoteHead(new NoteHeadStruct { Duration = "4", NoteType = "n" });
            nh.SetDisplaced(true);
            Assert.That(nh.IsDisplaced(), Is.True);
        }

        [Test]
        public void GetGlyphCode_ReturnsNonEmptyString()
        {
            var nh = new NoteHead(new NoteHeadStruct
            {
                Duration = "4",
                NoteType = "n",
            });
            Assert.That(nh.GetGlyphCode(), Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void IsElement()
        {
            var nh = new NoteHead(new NoteHeadStruct { Duration = "4", NoteType = "n" });
            Assert.That(nh, Is.InstanceOf<Element>());
        }
    }
}

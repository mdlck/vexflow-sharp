using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Core
{
    [TestFixture]
    [Category("DataStructures")]
    public class BoundingBoxTests
    {
        [Test]
        public void Constructor_StoresXYWH()
        {
            var box = new BoundingBox(10, 20, 100, 50);
            Assert.That(box.X, Is.EqualTo(10));
            Assert.That(box.Y, Is.EqualTo(20));
            Assert.That(box.W, Is.EqualTo(100));
            Assert.That(box.H, Is.EqualTo(50));
        }

        [Test]
        public void GettersReturnConstructorValues()
        {
            var box = new BoundingBox(10, 20, 100, 50);
            Assert.That(box.GetX(), Is.EqualTo(10));
            Assert.That(box.GetY(), Is.EqualTo(20));
            Assert.That(box.GetW(), Is.EqualTo(100));
            Assert.That(box.GetH(), Is.EqualTo(50));
        }

        [Test]
        public void Setters_UpdateValues()
        {
            var box = new BoundingBox(0, 0, 0, 0);
            box.SetX(5).SetY(10).SetW(20).SetH(30);
            Assert.That(box.X, Is.EqualTo(5));
            Assert.That(box.Y, Is.EqualTo(10));
            Assert.That(box.W, Is.EqualTo(20));
            Assert.That(box.H, Is.EqualTo(30));
        }

        [Test]
        public void Copy_CreatesIndependentCopy()
        {
            var original = new BoundingBox(10, 20, 100, 50);
            var copy = BoundingBox.Copy(original);
            Assert.That(copy.X, Is.EqualTo(10));
            Assert.That(copy.Y, Is.EqualTo(20));
            Assert.That(copy.W, Is.EqualTo(100));
            Assert.That(copy.H, Is.EqualTo(50));
            // Independence check: modifying copy should not affect original
            copy.SetX(99);
            Assert.That(original.X, Is.EqualTo(10));
        }

        [Test]
        public void Clone_CreatesIndependentCopy()
        {
            var original = new BoundingBox(10, 20, 100, 50);
            var clone = original.Clone();
            Assert.That(clone.X, Is.EqualTo(10));
            Assert.That(clone.Y, Is.EqualTo(20));
            Assert.That(clone.W, Is.EqualTo(100));
            Assert.That(clone.H, Is.EqualTo(50));
            clone.SetX(99);
            Assert.That(original.X, Is.EqualTo(10));
        }

        [Test]
        public void Move_ShiftsPositionByDelta()
        {
            var box = new BoundingBox(10, 20, 100, 50);
            box.Move(5, 10);
            Assert.That(box.X, Is.EqualTo(15));
            Assert.That(box.Y, Is.EqualTo(30));
            // Size should not change
            Assert.That(box.W, Is.EqualTo(100));
            Assert.That(box.H, Is.EqualTo(50));
        }

        [Test]
        public void MergeWith_AdjacentBoxes_ExpandsCorrectly()
        {
            // box1: (10, 20, 100, 50) => right edge at 110, bottom at 70
            // box2: (50, 30, 80, 40) => right edge at 130, bottom at 70
            var box1 = new BoundingBox(10, 20, 100, 50);
            var box2 = new BoundingBox(50, 30, 80, 40);
            box1.MergeWith(box2);
            Assert.That(box1.X, Is.EqualTo(10));
            Assert.That(box1.Y, Is.EqualTo(20));
            Assert.That(box1.W, Is.EqualTo(120)); // max(110, 130) - 10 = 120
            Assert.That(box1.H, Is.EqualTo(50));  // max(70, 70) - 20 = 50
        }

        [Test]
        public void MergeWith_NonOverlappingBoxes_EncompassesBoth()
        {
            var box1 = new BoundingBox(0, 0, 10, 10);
            var box2 = new BoundingBox(20, 20, 10, 10);
            box1.MergeWith(box2);
            Assert.That(box1.X, Is.EqualTo(0));
            Assert.That(box1.Y, Is.EqualTo(0));
            Assert.That(box1.W, Is.EqualTo(30));
            Assert.That(box1.H, Is.EqualTo(30));
        }

        [Test]
        public void MergeWith_ReturnsThis()
        {
            var box1 = new BoundingBox(0, 0, 10, 10);
            var box2 = new BoundingBox(5, 5, 10, 10);
            var result = box1.MergeWith(box2);
            Assert.That(result, Is.SameAs(box1));
        }
    }
}

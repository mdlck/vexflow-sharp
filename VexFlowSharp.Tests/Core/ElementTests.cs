using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Core
{
    [TestFixture]
    [Category("DataStructures")]
    public class ElementTests
    {
        /// <summary>Concrete subclass for testing the abstract Element.</summary>
        private class TestElement : Element
        {
            public bool DrawCalled { get; private set; }
            public override void Draw() { DrawCalled = true; }
        }

        [Test]
        public void Children_InitiallyEmpty()
        {
            var el = new TestElement();
            Assert.That(el.GetChildren(), Is.Empty);
        }

        [Test]
        public void AddChild_AddsToChildren()
        {
            var parent = new TestElement();
            var child = new TestElement();
            var result = parent.AddChild(child);
            Assert.That(result, Is.SameAs(parent), "AddChild should return the parent");
            Assert.That(parent.GetChildren(), Has.Count.EqualTo(1));
            Assert.That(parent.GetChildren()[0], Is.SameAs(child));
        }

        [Test]
        public void AddChild_MultipleChildren()
        {
            var parent = new TestElement();
            parent.AddChild(new TestElement());
            parent.AddChild(new TestElement());
            parent.AddChild(new TestElement());
            Assert.That(parent.GetChildren(), Has.Count.EqualTo(3));
        }

        [Test]
        public void SetStyle_GetStyle_RoundTrips()
        {
            var el = new TestElement();
            var style = new ElementStyle { FillStyle = "#ff0000" };
            el.SetStyle(style);
            Assert.That(el.GetStyle(), Is.SameAs(style));
        }

        [Test]
        public void SetStyle_FillStyle_StoredCorrectly()
        {
            var el = new TestElement();
            el.SetStyle(new ElementStyle { FillStyle = "#blue" });
            Assert.That(el.GetStyle()!.FillStyle, Is.EqualTo("#blue"));
        }

        [Test]
        public void GetBoundingBox_InitiallyNull()
        {
            var el = new TestElement();
            Assert.That(el.GetBoundingBox(), Is.Null);
        }

        [Test]
        public void Draw_CallsOverriddenMethod()
        {
            var el = new TestElement();
            Assert.That(el.DrawCalled, Is.False);
            el.Draw();
            Assert.That(el.DrawCalled, Is.True);
        }

        [Test]
        public void GetStyle_InitiallyNull()
        {
            var el = new TestElement();
            Assert.That(el.GetStyle(), Is.Null);
        }

        [Test]
        public void SetId_GetId_RoundTrips()
        {
            var el = new TestElement();
            el.SetId("myId");
            Assert.That(el.GetId(), Is.EqualTo("myId"));
        }

        [Test]
        public void GetId_InitiallyNull()
        {
            var el = new TestElement();
            Assert.That(el.GetId(), Is.Null);
        }
    }
}

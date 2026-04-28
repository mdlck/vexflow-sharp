using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Tests.Rendering;

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
        public void AddChild_RejectsChildWithExistingParent()
        {
            var firstParent = new TestElement();
            var secondParent = new TestElement();
            var child = new TestElement();

            firstParent.AddChild(child);

            var ex = Assert.Throws<VexFlowException>(() => secondParent.AddChild(child));
            Assert.That(ex!.Code, Is.EqualTo("Element"));
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
        public void SetGroupStyle_AppliesStyleToElementAndChildren()
        {
            var parent = new TestElement();
            var child = new TestElement();
            var grandchild = new TestElement();
            var style = new ElementStyle { StrokeStyle = "#333333" };

            child.AddChild(grandchild);
            parent.AddChild(child);
            var returned = parent.SetGroupStyle(style);

            Assert.That(returned, Is.SameAs(parent));
            Assert.That(parent.GetStyle(), Is.SameAs(style));
            Assert.That(child.GetStyle(), Is.SameAs(style));
            Assert.That(grandchild.GetStyle(), Is.SameAs(style));
        }

        [Test]
        public void ApplyStyle_LineDash_SetsDashPattern()
        {
            var ctx = new RecordingRenderContext();
            var el = new TestElement()
                .SetStyle(new ElementStyle { LineDash = "4 2", LineWidth = 3 })
                .SetContext(ctx);

            el.ApplyStyle();

            var dash = ctx.GetCall("SetLineDash").Args;
            Assert.That(dash, Is.EqualTo(new[] { 4.0, 2.0 }));
            Assert.That(ctx.HasCall("Save"), Is.True);
        }

        [Test]
        public void DrawWithStyle_WrapsDrawInSaveRestoreAndAppliesStyle()
        {
            var ctx = new RecordingRenderContext();
            var el = new TestElement();
            el.SetStyle(new ElementStyle { FillStyle = "#ff0000", LineDash = "2,1" });
            el.SetContext(ctx);

            var returned = el.DrawWithStyle();

            Assert.That(returned, Is.SameAs(el));
            Assert.That(el.DrawCalled, Is.True);
            Assert.That(ctx.Calls[0].Method, Is.EqualTo("Save"));
            Assert.That(ctx.HasCall("SetFillStyle"), Is.True);
            Assert.That(ctx.GetCall("SetLineDash").Args, Is.EqualTo(new[] { 2.0, 1.0 }));
            Assert.That(ctx.Calls[^1].Method, Is.EqualTo("Restore"));
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
        public void GetId_InitializesV5StyleAutoId()
        {
            var el = new TestElement();
            Assert.That(el.GetId(), Does.StartWith("auto"));
            Assert.That(el.GetAttribute("type"), Is.EqualTo(Element.CATEGORY));
            Assert.That(el.GetCategory(), Is.EqualTo(Element.CATEGORY));
        }

        [Test]
        public void SetRendered_RoundTripsAndDefaultsToTrue()
        {
            var el = new TestElement();

            Assert.That(el.IsRendered(), Is.False);
            Assert.That(el.SetRendered(), Is.SameAs(el));
            Assert.That(el.IsRendered(), Is.True);

            el.SetRendered(false);
            Assert.That(el.IsRendered(), Is.False);
        }

        [Test]
        public void GetContext_ReturnsAssignedRenderContext()
        {
            var ctx = new RecordingRenderContext();
            var el = new TestElement();

            Assert.That(el.GetContext(), Is.Null);
            Assert.That(el.SetContext(ctx), Is.SameAs(el));
            Assert.That(el.GetContext(), Is.SameAs(ctx));
        }

        [Test]
        public void SetAttribute_StoresDynamicAttributesAndBaseTypeCategory()
        {
            var el = new TestElement();

            el.SetAttribute("data-row", "3");
            el.SetAttribute("type", "CustomElement");

            Assert.That(el.GetAttribute("data-row"), Is.EqualTo("3"));
            Assert.That(el.GetAttribute("type"), Is.EqualTo("CustomElement"));
            Assert.That(el.GetCategory(), Is.EqualTo("CustomElement"));
            Assert.That(el.GetAttributes().Type, Is.EqualTo("CustomElement"));
        }
    }
}

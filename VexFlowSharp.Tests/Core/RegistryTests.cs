using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Core
{
    [TestFixture]
    [Category("DataStructures")]
    [Category("Registry")]
    public class RegistryTests
    {
        private class TestElement : Element
        {
            public override void Draw() { }
        }

        [TearDown]
        public void TearDown()
        {
            Registry.DisableDefaultRegistry();
        }

        [Test]
        public void Register_IndexesElementByIdTypeAndClass()
        {
            var registry = new Registry();
            var element = new TestElement()
                .SetId("n1")
                .AddClass("selected");

            registry.Register(element);

            Assert.That(registry.GetElementById("n1"), Is.SameAs(element));
            Assert.That(registry.GetElementsByType(Element.CATEGORY), Has.Member(element));
            Assert.That(registry.GetElementsByClass("selected"), Has.Member(element));
        }

        [Test]
        public void AttributeUpdates_RefreshIndexes()
        {
            var registry = new Registry();
            var element = new TestElement().SetId("old");
            registry.Register(element);

            element.SetId("new");
            element.AddClass("hot");
            element.RemoveClass("hot");

            Assert.That(registry.GetElementById("old"), Is.Null);
            Assert.That(registry.GetElementById("new"), Is.SameAs(element));
            Assert.That(registry.GetElementsByClass("hot"), Is.Empty);
        }

        [Test]
        public void AttributeUpdates_RefreshTypeAndClassListIndexes()
        {
            var registry = new Registry();
            var element = new TestElement().SetId("n1");
            registry.Register(element);

            element.SetAttribute("type", "CustomElement");
            element.SetAttribute("class", "alpha beta");
            element.SetAttribute("class", "beta gamma");

            Assert.That(registry.GetElementsByType(Element.CATEGORY), Is.Empty);
            Assert.That(registry.GetElementsByType("CustomElement"), Has.Member(element));
            Assert.That(registry.GetElementsByClass("alpha"), Is.Empty);
            Assert.That(registry.GetElementsByClass("beta"), Has.Member(element));
            Assert.That(registry.GetElementsByClass("gamma"), Has.Member(element));
        }

        [Test]
        public void DefaultRegistry_AutoRegistersFutureElements()
        {
            var registry = new Registry();
            Registry.EnableDefaultRegistry(registry);

            var element = new TestElement();

            Assert.That(registry.GetElementById(element.GetId()!), Is.SameAs(element));
            Assert.That(registry.GetElementsByType(Element.CATEGORY), Has.Member(element));
        }

        [Test]
        public void Clear_RemovesAllIndexes()
        {
            var registry = new Registry();
            var element = new TestElement().SetId("n1").AddClass("selected");
            registry.Register(element);

            registry.Clear();

            Assert.That(registry.GetElementById("n1"), Is.Null);
            Assert.That(registry.GetElementsByClass("selected"), Is.Empty);
        }
    }
}

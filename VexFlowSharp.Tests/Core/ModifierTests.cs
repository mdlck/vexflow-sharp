using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Core
{
    [TestFixture]
    [Category("DataStructures")]
    [Category("Modifier")]
    public class ModifierTests
    {
        private class TestModifier : Modifier
        {
            public override void Draw() { }
        }

        [Test]
        public void Category_IsV5Modifier()
        {
            var modifier = new TestModifier();

            Assert.That(Modifier.CATEGORY, Is.EqualTo("Modifier"));
            Assert.That(modifier.GetCategory(), Is.EqualTo(Modifier.CATEGORY));
            Assert.That(TypeGuards.IsModifier(modifier), Is.True);
        }
    }
}

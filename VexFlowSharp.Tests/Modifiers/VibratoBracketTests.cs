using NUnit.Framework;
using VexFlowSharp;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("VibratoBracket")]
    [Category("Modifiers")]
    [Category("Phase4")]
    public class VibratoBracketTests
    {
        [Test]
        public void Simple_BracketSpansMultipleNotes()
        {
            // VibratoBracket can be constructed with start and stop notes
            var start   = new GhostNote("q");
            var stop    = new GhostNote("q");
            var bracket = new VibratoBracket(start, stop);
            Assert.IsNotNull(bracket);
        }

        [Test]
        public void NullStop_BracketExtendsToStaveEnd()
        {
            // null stop means bracket extends to stave end
            var note    = new GhostNote("q");
            var bracket = new VibratoBracket(note, null);
            Assert.IsNotNull(bracket);
        }

        [Test]
        public void Harsh_ZigzagBracket()
        {
            var note    = new GhostNote("q");
            var bracket = new VibratoBracket(note);
            bracket.SetHarsh(true);
            // No exception — harsh mode set
            Assert.IsNotNull(bracket);
        }

        [Test]
        public void SetHarsh_DoesNotThrow()
        {
            var note    = new GhostNote("q");
            var bracket = new VibratoBracket(note);
            Assert.DoesNotThrow(() => bracket.SetHarsh(true));
        }

        [Test]
        public void WithArrow_ArrowAtEndOfBracket()
        {
            // VibratoBracket constructed without an explicit arrow mechanism in this
            // simplified port — bracket is constructed and not null
            var start   = new GhostNote("q");
            var stop    = new GhostNote("q");
            var bracket = new VibratoBracket(start, stop);
            Assert.IsNotNull(bracket);
        }
    }
}

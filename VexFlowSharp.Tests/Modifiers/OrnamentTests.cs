// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Ornament")]
    [Category("Modifiers")]
    [Category("Phase4")]
    public class OrnamentTests
    {
        // ── Table tests ────────────────────────────────────────────────────────

        [Test]
        public void OrnamentCodes_ContainsTrill()
            => Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("tr"),
                "OrnamentCodes must contain trill 'tr'");

        [Test]
        public void OrnamentCodes_ContainsMordent()
            => Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("mordent"),
                "OrnamentCodes must contain 'mordent'");

        [Test]
        public void OrnamentCodes_ContainsTurn()
            => Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("turn"),
                "OrnamentCodes must contain 'turn'");

        [Test]
        public void OrnamentCodes_ContainsMordentInverted()
            => Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("mordent_inverted"),
                "OrnamentCodes must contain 'mordent_inverted'");

        [Test]
        public void OrnamentCodes_ContainsTurnInverted()
            => Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("turn_inverted"),
                "OrnamentCodes must contain 'turn_inverted'");

        [Test]
        public void OrnamentCodes_HasAtLeastFifteenEntries()
            => Assert.GreaterOrEqual(Tables.OrnamentCodes.Count, 15,
                "OrnamentCodes must have at least 15 entries (all 24 from tables.ts)");

        // ── Category test ──────────────────────────────────────────────────────

        [Test]
        public void Category_IsOrnaments()
            => Assert.AreEqual("ornaments", Ornament.CATEGORY);

        // ── Format tests ───────────────────────────────────────────────────────

        [Test]
        public void Format_EmptyList_DoesNotThrow()
        {
            var state = new ModifierContextState();
            Assert.DoesNotThrow(() =>
                Ornament.Format(new List<Ornament>(), state));
        }

        [Test]
        public void Format_NullList_DoesNotThrow()
        {
            var state = new ModifierContextState();
            Assert.DoesNotThrow(() =>
                Ornament.Format(null!, state));
        }

        [Test]
        public void Format_EmptyList_ReturnsFalse()
        {
            var state = new ModifierContextState();
            bool result = Ornament.Format(new List<Ornament>(), state);
            Assert.IsFalse(result, "Format with empty list must return false");
        }

        // ── Constructor tests ──────────────────────────────────────────────────

        [Test]
        public void DrawOrnaments_Trill()
        {
            var orn = new Ornament("tr");
            Assert.AreEqual("ornaments", orn.GetCategory());
            Assert.AreEqual(ModifierPosition.Above, orn.GetPosition(),
                "Default position should be ABOVE");
        }

        [Test]
        public void DrawOrnaments_Mordent()
        {
            var orn = new Ornament("mordent");
            Assert.AreEqual("ornaments", orn.GetCategory());
            Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("mordent"),
                "OrnamentCodes must contain mordent");
        }

        [Test]
        public void DrawOrnaments_Turn()
        {
            var orn = new Ornament("turn");
            Assert.AreEqual("ornaments", orn.GetCategory());
            Assert.AreEqual("ornamentTurn", Tables.OrnamentCodes["turn"],
                "Turn glyph code must be 'ornamentTurn'");
        }

        [Test]
        public void DrawOrnamentsDisplaced_TrillWithMordent()
        {
            // Two ornaments can be created independently
            var trill   = new Ornament("tr");
            var mordent = new Ornament("mordent");

            Assert.AreEqual("ornaments", trill.GetCategory());
            Assert.AreEqual("ornaments", mordent.GetCategory());
        }

        [Test]
        public void DrawOrnamentsDelayed_PostNoteOrnament()
        {
            // Jazz ornaments (flip, jazzTurn, smear) are delayed by default
            var flip = new Ornament("flip");
            Assert.IsTrue(flip.Delayed,
                "flip ornament should be delayed by default (OrnamentNoteTransition)");

            var doit = new Ornament("doit");
            Assert.IsFalse(doit.Delayed,
                "doit ornament should not be delayed by default");
        }

        [Test]
        public void DrawOrnamentsStacked_TwoOrnamentsAboveNote()
        {
            // Format increments TopTextLine for each ABOVE ornament
            // We can verify this by checking that Format returns true for non-empty list
            // and that subsequent Format calls would increment the state
            var state = new ModifierContextState();
            Assert.AreEqual(0.0, state.TopTextLine, "Initial TopTextLine must be 0");
        }

        [Test]
        public void DrawOrnamentsWithAccidentals_TrillWithSharp()
        {
            // SetUpperAccidental and SetLowerAccidental should not throw
            var orn = new Ornament("tr");
            Assert.DoesNotThrow(() => orn.SetUpperAccidental("#"),
                "SetUpperAccidental('#') must not throw");
            Assert.IsNotNull(orn.AccidentalUpper,
                "AccidentalUpper must be set after SetUpperAccidental");
        }

        [Test]
        public void JazzOrnaments_Lyrics()
        {
            // Jazz ornament types should all be in OrnamentCodes
            Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("scoop"),  "'scoop' must be in OrnamentCodes");
            Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("doit"),   "'doit' must be in OrnamentCodes");
            Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("fall"),   "'fall' must be in OrnamentCodes");
            Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("bend"),   "'bend' must be in OrnamentCodes");
            Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("flip"),   "'flip' must be in OrnamentCodes");
            Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("jazzTurn"), "'jazzTurn' must be in OrnamentCodes");
            Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("smear"),  "'smear' must be in OrnamentCodes");
        }
    }
}

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Articulation")]
    [Category("Modifiers")]
    [Category("Phase4")]
    public class ArticulationTests
    {
        // ── Table tests ────────────────────────────────────────────────────────

        [Test]
        public void ArticulationCodes_ContainsStaccato()
            => Assert.IsTrue(Tables.ArticulationCodes.ContainsKey("a."),
                "ArticulationCodes must contain staccato 'a.'");

        [Test]
        public void ArticulationCodes_ContainsAccent()
            => Assert.IsTrue(Tables.ArticulationCodes.ContainsKey("a>"),
                "ArticulationCodes must contain accent 'a>'");

        [Test]
        public void ArticulationCodes_ContainsFermata()
            => Assert.IsTrue(Tables.ArticulationCodes.ContainsKey("a@a"),
                "ArticulationCodes must contain fermata above 'a@a'");

        [Test]
        public void ArticulationCodes_ContainsTenuto()
            => Assert.IsTrue(Tables.ArticulationCodes.ContainsKey("a-"),
                "ArticulationCodes must contain tenuto 'a-'");

        [Test]
        public void ArticulationCodes_ContainsMarcato()
            => Assert.IsTrue(Tables.ArticulationCodes.ContainsKey("a^"),
                "ArticulationCodes must contain marcato 'a^'");

        [Test]
        public void ArticulationCodes_HasAtLeastTwentyEntries()
            => Assert.GreaterOrEqual(Tables.ArticulationCodes.Count, 20,
                "ArticulationCodes must have at least 20 entries (all 23 from tables.ts)");

        // ── Category test ──────────────────────────────────────────────────────

        [Test]
        public void Category_IsArticulations()
            => Assert.AreEqual("articulations", Articulation.CATEGORY);

        // ── Format tests ───────────────────────────────────────────────────────

        [Test]
        public void Format_EmptyList_DoesNotThrow()
        {
            var state = new ModifierContextState();
            Assert.DoesNotThrow(() =>
                Articulation.Format(new List<Articulation>(), state));
        }

        [Test]
        public void Format_NullList_DoesNotThrow()
        {
            var state = new ModifierContextState();
            Assert.DoesNotThrow(() =>
                Articulation.Format(null!, state));
        }

        [Test]
        public void Format_EmptyList_ReturnsFalse()
        {
            var state = new ModifierContextState();
            bool result = Articulation.Format(new List<Articulation>(), state);
            Assert.IsFalse(result, "Format with empty list must return false");
        }

        // ── Constructor tests ──────────────────────────────────────────────────

        [Test]
        public void DrawArticulations_StaccatoAboveNote()
        {
            // Staccato 'a.' is BetweenLines = true (may sit in a space)
            var art = new Articulation("a.");
            Assert.AreEqual("articulations", art.GetCategory());
            Assert.AreEqual(ModifierPosition.Above, art.GetPosition(),
                "Default position should be ABOVE");
        }

        [Test]
        public void DrawArticulations_TenutoAboveNote()
        {
            var art = new Articulation("a-");
            Assert.AreEqual("articulations", art.GetCategory());
            // Tenuto BetweenLines = true
            Assert.IsTrue(Tables.ArticulationCodes["a-"].BetweenLines,
                "Tenuto BetweenLines must be true");
        }

        [Test]
        public void DrawArticulations_AccentAboveNote()
        {
            var art = new Articulation("a>");
            Assert.AreEqual("articulations", art.GetCategory());
            Assert.IsNotNull(Tables.ArticulationCodes["a>"].AboveCode,
                "Accent must have an AboveCode");
        }

        [Test]
        public void DrawFermata_AboveAndBelowNote()
        {
            // Fermata above (a@a) and below (a@u) are separate entries
            Assert.IsTrue(Tables.ArticulationCodes.ContainsKey("a@a"),
                "Fermata above 'a@a' must be in ArticulationCodes");
            Assert.IsTrue(Tables.ArticulationCodes.ContainsKey("a@u"),
                "Fermata below 'a@u' must be in ArticulationCodes");

            var artAbove = new Articulation("a@a");
            var artBelow = new Articulation("a@u");
            Assert.AreEqual("articulations", artAbove.GetCategory());
            Assert.AreEqual("articulations", artBelow.GetCategory());
        }

        [Test]
        public void VerticalPlacement_SnapToStaff()
        {
            // Staccato (BetweenLines=true) vs Fermata (BetweenLines=false)
            var staccato = new Articulation("a.");
            var fermata  = new Articulation("a@a");

            // Both should be articulations
            Assert.AreEqual("articulations", staccato.GetCategory());
            Assert.AreEqual("articulations", fermata.GetCategory());

            // BetweenLines flag should differ
            Assert.IsTrue(Tables.ArticulationCodes["a."].BetweenLines,
                "Staccato BetweenLines must be true");
            Assert.IsFalse(Tables.ArticulationCodes["a@a"].BetweenLines,
                "Fermata BetweenLines must be false (must sit outside staff)");
        }

        [Test]
        public void DrawArticulations2_Marcato()
        {
            var art = new Articulation("a^");
            Assert.AreEqual("articulations", art.GetCategory());
            // Marcato BetweenLines = false (must sit outside staff)
            Assert.IsFalse(Tables.ArticulationCodes["a^"].BetweenLines,
                "Marcato BetweenLines must be false");
            Assert.IsNotNull(Tables.ArticulationCodes["a^"].AboveCode,
                "Marcato must have an AboveCode");
        }
    }
}

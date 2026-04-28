// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Tests.Rendering;

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
            => Assert.AreEqual("Articulation", Articulation.CATEGORY);

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
            Assert.AreEqual("Articulation", art.GetCategory());
            Assert.AreEqual(ModifierPosition.Above, art.GetPosition(),
                "Default position should be ABOVE");
        }

        [Test]
        public void DrawArticulations_TenutoAboveNote()
        {
            var art = new Articulation("a-");
            Assert.AreEqual("Articulation", art.GetCategory());
            // Tenuto BetweenLines = true
            Assert.IsTrue(Tables.ArticulationCodes["a-"].BetweenLines,
                "Tenuto BetweenLines must be true");
        }

        [Test]
        public void DrawArticulations_AccentAboveNote()
        {
            var art = new Articulation("a>");
            Assert.AreEqual("Articulation", art.GetCategory());
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
            Assert.AreEqual("Articulation", artAbove.GetCategory());
            Assert.AreEqual("Articulation", artBelow.GetCategory());
        }

        [Test]
        public void VerticalPlacement_SnapToStaff()
        {
            // Staccato (BetweenLines=true) vs Fermata (BetweenLines=false)
            var staccato = new Articulation("a.");
            var fermata  = new Articulation("a@a");

            // Both should be articulations
            Assert.AreEqual("Articulation", staccato.GetCategory());
            Assert.AreEqual("Articulation", fermata.GetCategory());

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
            Assert.AreEqual("Articulation", art.GetCategory());
            // Marcato BetweenLines = false (must sit outside staff)
            Assert.IsFalse(Tables.ArticulationCodes["a^"].BetweenLines,
                "Marcato BetweenLines must be false");
            Assert.IsNotNull(Tables.ArticulationCodes["a^"].AboveCode,
                "Marcato must have an AboveCode");
        }

        [Test]
        public void SetBetweenLines_OverridesTableValue()
        {
            var art = new Articulation("a^").SetBetweenLines(true);
            var state = new ModifierContextState();
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            note.SetStave(new Stave(10, 20, 300)).AddModifier(art);

            Assert.DoesNotThrow(() => Articulation.Format(new List<Articulation> { art }, state));
        }

        [Test]
        public void Format_UsesGlyphHeightForTextLineIncrement()
        {
            var state = new ModifierContextState();
            var art = new Articulation("a.");
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            note.SetStave(new Stave(10, 20, 300)).AddModifier(art);
            string code = Tables.ArticulationCodes["a."].AboveCode ?? Tables.ArticulationCodes["a."].Code!;
            double expectedIncrement = System.Math.Ceiling(((new Glyph(code, Tables.NOTATION_FONT_SCALE).GetMetrics()!.Height / Tables.STAVE_LINE_DISTANCE) + 0.5) / 0.5) * 0.5;

            Articulation.Format(new List<Articulation> { art }, state);

            Assert.That(state.TopTextLine, Is.EqualTo(expectedIncrement).Within(0.0001));
        }

        [Test]
        public void Format_AddsSymmetricHorizontalOverlapBeyondNotehead()
        {
            var state = new ModifierContextState();
            var art = new Articulation("a@a");
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            note.SetStave(new Stave(10, 20, 300)).AddModifier(art);
            double expectedOverlap = System.Math.Min(
                System.Math.Max(art.GetWidth() - note.GetMetrics().GlyphWidth, 0),
                System.Math.Max(art.GetWidth() - (state.LeftShift + state.RightShift), 0));

            Articulation.Format(new List<Articulation> { art }, state);

            Assert.That(state.LeftShift, Is.EqualTo(expectedOverlap / 2).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(expectedOverlap / 2).Within(0.0001));
        }

        [Test]
        public void Draw_TabNoteArticulationRendersWithoutStaffSnap()
        {
            var ctx = new RecordingRenderContext();
            var stave = new TabStave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new TabNote(new TabNoteStruct
            {
                Positions = new[] { new TabNotePosition { Str = 3, Fret = 7 } },
                Duration = "4",
            });
            var art = new Articulation("a.");
            note.SetStave(stave);
            note.SetContext(ctx);
            note.AddModifier(art);
            note.PreFormat();
            art.SetContext(ctx);

            art.Draw();

            Assert.That(ctx.HasCall("Fill"), Is.True);
        }
    }
}

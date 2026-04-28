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
        public void OrnamentCodes_ContainsV5CamelCaseMordentInverted()
            => Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("mordentInverted"),
                "OrnamentCodes must contain v5 'mordentInverted'");

        [Test]
        public void OrnamentCodes_ContainsTurnInverted()
            => Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("turn_inverted"),
                "OrnamentCodes must contain 'turn_inverted'");

        [Test]
        public void OrnamentCodes_ContainsV5CamelCaseTurnInverted()
            => Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("turnInverted"),
                "OrnamentCodes must contain v5 'turnInverted'");

        [Test]
        public void OrnamentCodes_HasAtLeastFifteenEntries()
            => Assert.GreaterOrEqual(Tables.OrnamentCodes.Count, 15,
                "OrnamentCodes must have at least 15 entries (all 24 from tables.ts)");

        // ── Category test ──────────────────────────────────────────────────────

        [Test]
        public void Category_IsOrnaments()
            => Assert.AreEqual("Ornament", Ornament.CATEGORY);

        [Test]
        public void MinPadding_ComesFromMetrics()
            => Assert.That(Ornament.MinPadding, Is.EqualTo(Metrics.GetDouble("NoteHead.minPadding")));

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

        [Test]
        public void Format_ReleaseOrnament_ConsumesRightShiftWithPadding()
        {
            var state = new ModifierContextState { RightShift = 4 };
            var ornament = new Ornament("doit");

            Ornament.Format(new List<Ornament> { ornament }, state);

            Assert.That(ornament.GetXShift(), Is.EqualTo(4 + Metrics.GetDouble("Ornament.sideShift")).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(4 + ornament.GetWidth() + Ornament.MinPadding).Within(0.0001));
            Assert.That(state.LeftShift, Is.EqualTo(0));
            Assert.That(state.TopTextLine, Is.EqualTo(0));
        }

        [Test]
        public void Format_AttackOrnament_ConsumesLeftShiftWithPadding()
        {
            var state = new ModifierContextState { LeftShift = 3 };
            var ornament = new Ornament("scoop");

            Ornament.Format(new List<Ornament> { ornament }, state);

            Assert.That(
                ornament.GetXShift(),
                Is.EqualTo(-(3 + ornament.GetWidth() + Metrics.GetDouble("Ornament.sideShift"))).Within(0.0001));
            Assert.That(state.LeftShift, Is.EqualTo(3 + ornament.GetWidth() + Ornament.MinPadding).Within(0.0001));
            Assert.That(state.RightShift, Is.EqualTo(0));
            Assert.That(state.TopTextLine, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_JazzLeftAndRightOrnamentsUseV5ModifierPositions()
        {
            Assert.That(new Ornament("scoop").GetPosition(), Is.EqualTo(ModifierPosition.Left));
            Assert.That(new Ornament("doit").GetPosition(), Is.EqualTo(ModifierPosition.Right));
            Assert.That(new Ornament("flip").GetPosition(), Is.EqualTo(ModifierPosition.Above));
        }

        [Test]
        public void SetNote_ArticulationOrnamentsChooseSideFromNoteLine()
        {
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            var ornament = new Ornament("bend");

            note.AddModifier(ornament);

            var expected = note.GetLineNumber() >= 3 ? ModifierPosition.Above : ModifierPosition.Below;
            Assert.That(ornament.GetPosition(), Is.EqualTo(expected));
        }

        [Test]
        public void Format_StackedArticulationOrnamentsUseGlyphHeightForYOffset()
        {
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            var first = new Ornament("bend");
            var second = new Ornament("bend");
            note.AddModifier(first);
            note.AddModifier(second);
            var state = new ModifierContextState();

            Ornament.Format(new List<Ornament> { first, second }, state);

            var expectedHeight = new Glyph(Tables.OrnamentCode("bend"), Tables.NOTATION_FONT_SCALE).GetMetrics()!.Height;
            var expectedSecondShift = second.GetPosition() == ModifierPosition.Above ? -expectedHeight : expectedHeight;
            Assert.That(first.GetYShift(), Is.EqualTo(0).Within(0.0001));
            Assert.That(second.GetYShift(), Is.EqualTo(expectedSecondShift).Within(0.0001));
            if (second.GetPosition() == ModifierPosition.Above)
                Assert.That(state.TopTextLine, Is.EqualTo(Metrics.GetDouble("Ornament.textLineIncrement") * 2).Within(0.0001));
            else
                Assert.That(state.TextLine, Is.EqualTo(Metrics.GetDouble("Ornament.textLineIncrement") * 2).Within(0.0001));
        }

        // ── Constructor tests ──────────────────────────────────────────────────

        [Test]
        public void DrawOrnaments_Trill()
        {
            var orn = new Ornament("tr");
            Assert.AreEqual("Ornament", orn.GetCategory());
            Assert.AreEqual(ModifierPosition.Above, orn.GetPosition(),
                "Default position should be ABOVE");
        }

        [Test]
        public void DrawOrnaments_Mordent()
        {
            var orn = new Ornament("mordent");
            Assert.AreEqual("Ornament", orn.GetCategory());
            Assert.IsTrue(Tables.OrnamentCodes.ContainsKey("mordent"),
                "OrnamentCodes must contain mordent");
        }

        [Test]
        public void DrawOrnaments_Turn()
        {
            var orn = new Ornament("turn");
            Assert.AreEqual("Ornament", orn.GetCategory());
            Assert.AreEqual("ornamentTurn", Tables.OrnamentCodes["turn"],
                "Turn glyph code must be 'ornamentTurn'");
        }

        [Test]
        public void DrawOrnaments_CustomGlyphName_PassesThrough()
        {
            var orn = new Ornament("ornamentTurn");
            Assert.AreEqual("Ornament", orn.GetCategory());
            Assert.That(orn.GetWidth(), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void DrawOrnamentsDisplaced_TrillWithMordent()
        {
            // Two ornaments can be created independently
            var trill   = new Ornament("tr");
            var mordent = new Ornament("mordent");

            Assert.AreEqual("Ornament", trill.GetCategory());
            Assert.AreEqual("Ornament", mordent.GetCategory());
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
            var trill = new Ornament("tr");
            var mordent = new Ornament("mordent");
            var state = new ModifierContextState();

            Ornament.Format(new List<Ornament> { trill, mordent }, state);

            Assert.That(state.TopTextLine, Is.EqualTo(Metrics.GetDouble("Ornament.textLineIncrement") * 2).Within(0.0001));
            Assert.That(trill.GetTextLine(), Is.EqualTo(0));
            Assert.That(mordent.GetTextLine(), Is.EqualTo(Metrics.GetDouble("Ornament.textLineIncrement")));
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
        public void DrawOrnamentsWithAccidentals_RendersOrnamentAndBothAccidentals()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            var ornament = new Ornament("tr")
                .SetUpperAccidental("#")
                .SetLowerAccidental("b");
            note.SetStave(stave).SetX(100).AddModifier(ornament);
            note.PreFormat();
            ornament.SetContext(ctx);

            ornament.Draw();

            Assert.That(ctx.GetCalls("Fill").Count(), Is.EqualTo(3));
        }

        [Test]
        public void Draw_OpensAndClosesV5RenderGroup()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            var ornament = new Ornament("tr");
            note.SetStave(stave).SetX(100).AddModifier(ornament);
            note.PreFormat();
            ornament.SetContext(ctx);

            ornament.Draw();

            Assert.That(ctx.HasCall("OpenGroup"), Is.True);
            Assert.That(ctx.HasCall("CloseGroup"), Is.True);
            var methods = ctx.Calls.Select(c => c.Method).ToList();
            Assert.That(methods.IndexOf("OpenGroup"), Is.LessThan(methods.IndexOf("CloseGroup")));
        }

        [Test]
        public void DrawDelayedOrnament_UsesStaveEndFallbackWhenNoNextTickContext()
        {
            var ctx = new RecordingRenderContext();
            var stave = new Stave(10, 20, 300);
            stave.SetContext(ctx);
            var note = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "4" });
            var ornament = new Ornament("flip");
            note.SetStave(stave).SetX(100).AddModifier(ornament);
            note.PreFormat();
            ornament.SetContext(ctx);

            ornament.Draw();

            Assert.That(ornament.Delayed, Is.True);
            Assert.That(ctx.HasCall("Fill"), Is.True);
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

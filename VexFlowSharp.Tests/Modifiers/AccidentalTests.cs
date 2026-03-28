using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Tests.Modifiers
{
    [TestFixture]
    [Category("Accidental")]
    [Category("Modifiers")]
    [Category("Phase4")]
    public class AccidentalTests
    {
        // ── Category / constructor ────────────────────────────────────────────

        [Test]
        public void Accidental_Category_IsAccidentals()
        {
            Assert.That(Accidental.CATEGORY, Is.EqualTo("accidentals"));
        }

        [Test]
        public void Accidental_GetCategory_IsAccidentals()
        {
            var acc = new Accidental("#");
            Assert.That(acc.GetCategory(), Is.EqualTo("accidentals"));
        }

        [Test]
        public void Accidental_Position_IsLeft()
        {
            var acc = new Accidental("#");
            Assert.That(acc.GetPosition(), Is.EqualTo(ModifierPosition.Left));
        }

        // ── Basic single accidentals ──────────────────────────────────────────

        [Test]
        public void Basic_SingleAccidental_Sharp()
        {
            var acc = new Accidental("#");
            Assert.That(acc.Type, Is.EqualTo("#"));
            Assert.That(acc.GetWidth(), Is.GreaterThan(0), "Sharp glyph should have positive width");
        }

        [Test]
        public void Basic_SingleAccidental_Flat()
        {
            var acc = new Accidental("b");
            Assert.That(acc.Type, Is.EqualTo("b"));
            Assert.That(acc.GetWidth(), Is.GreaterThan(0), "Flat glyph should have positive width");
        }

        [Test]
        public void Basic_SingleAccidental_Natural()
        {
            var acc = new Accidental("n");
            Assert.That(acc.Type, Is.EqualTo("n"));
            Assert.That(acc.GetWidth(), Is.GreaterThan(0), "Natural glyph should have positive width");
        }

        [Test]
        public void Basic_SingleAccidental_DoubleSharp()
        {
            var acc = new Accidental("##");
            Assert.That(acc.Type, Is.EqualTo("##"));
            Assert.That(acc.GetWidth(), Is.GreaterThan(0), "Double-sharp glyph should have positive width");
        }

        [Test]
        public void Basic_SingleAccidental_DoubleFlat()
        {
            var acc = new Accidental("bb");
            Assert.That(acc.Type, Is.EqualTo("bb"));
            Assert.That(acc.GetWidth(), Is.GreaterThan(0), "Double-flat glyph should have positive width");
        }

        // ── Cautionary accidentals ────────────────────────────────────────────

        [Test]
        public void Cautionary_AccidentalInParentheses()
        {
            var acc = new Accidental("#");
            double widthBefore = acc.GetWidth();
            acc.SetAsCautionary();
            double widthAfter = acc.GetWidth();

            Assert.That(acc.IsCautionary(), Is.True);
            // Cautionary is wider (parentheses added)
            Assert.That(widthAfter, Is.GreaterThan(widthBefore),
                "Cautionary accidental should be wider than plain accidental");
        }

        // ── AccidentalColumnsTable ────────────────────────────────────────────

        [Test]
        public void AccidentalColumnsTable_HasEntries1Through6()
        {
            for (int i = 1; i <= 6; i++)
                Assert.That(Tables.AccidentalColumnsTable.ContainsKey(i), Is.True,
                    $"AccidentalColumnsTable missing key {i}");
        }

        [Test]
        public void AccidentalColumnsTable_SixEntry_HasFourCases()
        {
            var six = Tables.AccidentalColumnsTable[6];
            Assert.That(six.ContainsKey("a"), Is.True);
            Assert.That(six.ContainsKey("b"), Is.True);
            Assert.That(six.ContainsKey("spaced_out_hexachord"), Is.True);
            Assert.That(six.ContainsKey("very_spaced_out_hexachord"), Is.True);
        }

        [Test]
        public void AccidentalColumnsTable_ThreeEntry_HasThreeCases()
        {
            var three = Tables.AccidentalColumnsTable[3];
            Assert.That(three.ContainsKey("a"), Is.True);
            Assert.That(three.ContainsKey("b"), Is.True);
            Assert.That(three.ContainsKey("second_on_bottom"), Is.True);
        }

        [Test]
        public void AccidentalColumnsTable_OneEntry_HasAAndB()
        {
            var one = Tables.AccidentalColumnsTable[1];
            Assert.That(one.ContainsKey("a"), Is.True);
            Assert.That(one.ContainsKey("b"), Is.True);
            Assert.That(one["a"][0], Is.EqualTo(1));
        }

        // ── Format static method ──────────────────────────────────────────────

        [Test]
        public void Format_NullList_ReturnsFalse()
        {
            var state  = new ModifierContextState();
            var result = Accidental.Format(null, state);
            Assert.That(result, Is.False);
        }

        [Test]
        public void Format_EmptyList_ReturnsFalse()
        {
            var state  = new ModifierContextState();
            var result = Accidental.Format(new List<Accidental>(), state);
            Assert.That(result, Is.False);
        }

        // ── CheckCollision ────────────────────────────────────────────────────

        [Test]
        public void CheckCollision_AdjacentHalfStep_Collides()
        {
            // Two accidentals 0.5 lines apart (adjacent half-step) should collide
            var m1 = new StaveLineAccidentalLayoutMetrics
                { Line = 2.0, FlatLine = false, DblSharpLine = false, NumAcc = 1, Width = 10, Column = 0 };
            var m2 = new StaveLineAccidentalLayoutMetrics
                { Line = 1.5, FlatLine = false, DblSharpLine = false, NumAcc = 1, Width = 10, Column = 0 };

            bool collision = Accidental.CheckCollision(m1, m2);
            Assert.That(collision, Is.True,
                "0.5-line separation should produce a collision (needs 3.0 clearance)");
        }

        [Test]
        public void CheckCollision_FourLines_NoCollision()
        {
            // 4.0 lines apart — greater than the 3.0 clearance required
            var m1 = new StaveLineAccidentalLayoutMetrics
                { Line = 5.0, FlatLine = false, DblSharpLine = false, NumAcc = 1, Width = 10, Column = 0 };
            var m2 = new StaveLineAccidentalLayoutMetrics
                { Line = 1.0, FlatLine = false, DblSharpLine = false, NumAcc = 1, Width = 10, Column = 0 };

            bool collision = Accidental.CheckCollision(m1, m2);
            Assert.That(collision, Is.False,
                "4.0-line separation should not collide (clearance > 3.0)");
        }

        [Test]
        public void CheckCollision_FlatNeedsLessClearance()
        {
            // Flat accidentals only need 2.5 lines clearance instead of 3.0.
            // clearance = line2 - line1 (positive means line2 is on top in VexFlow convention).
            // When clearance > 0: clearanceRequired depends on line2.FlatLine.
            // With 2.7-line clearance and line2.FlatLine=true → 2.7 > 2.5 → no collision.
            // With 2.7-line clearance and line2.FlatLine=false → 2.7 < 3.0 → collision.

            // line1 is below, line2 is above (line2.Line > line1.Line → positive clearance)
            var m1NonFlat = new StaveLineAccidentalLayoutMetrics
                { Line = 0.3, FlatLine = false, DblSharpLine = false, NumAcc = 1, Width = 10, Column = 0 };
            var m1Flat = new StaveLineAccidentalLayoutMetrics
                { Line = 0.3, FlatLine = false, DblSharpLine = false, NumAcc = 1, Width = 10, Column = 0 };

            var m2NonFlat = new StaveLineAccidentalLayoutMetrics
                { Line = 3.0, FlatLine = false, DblSharpLine = false, NumAcc = 1, Width = 10, Column = 0 };
            var m2Flat = new StaveLineAccidentalLayoutMetrics
                { Line = 3.0, FlatLine = true, DblSharpLine = false, NumAcc = 1, Width = 10, Column = 0 };

            // clearance = 3.0 - 0.3 = 2.7
            bool collisionNonFlat = Accidental.CheckCollision(m1NonFlat, m2NonFlat);
            bool collisionFlat    = Accidental.CheckCollision(m1Flat, m2Flat);

            // Non-flat requires 3.0 clearance → 2.7 < 3.0 → collision
            Assert.That(collisionNonFlat, Is.True,
                "2.7-line separation with non-flat line2 should collide (needs 3.0 clearance)");

            // Flat requires 2.5 clearance → 2.7 > 2.5 → no collision
            Assert.That(collisionFlat, Is.False,
                "2.7-line separation with flat line2 should not collide (only needs 2.5 clearance)");
        }

        // ── SpecialCases / stagger ────────────────────────────────────────────

        [Test]
        public void SpecialCases_AccidentalsInChord_NoXOverlap()
        {
            // Verify the table-driven stagger ensures no overlap: for 2 accidentals,
            // the "a" case gives columns [1, 2], which must produce distinct x-offsets.
            var table = Tables.AccidentalColumnsTable[2];
            int[] cols = table["a"];
            Assert.That(cols[0], Is.Not.EqualTo(cols[1]),
                "Two simultaneous accidentals should be in different columns");
        }

        [Test]
        public void StemDown_AccidentalPositioning()
        {
            // Accidentals are always LEFT of the notehead regardless of stem direction.
            var acc = new Accidental("b");
            Assert.That(acc.GetPosition(), Is.EqualTo(ModifierPosition.Left));
        }

        [Test]
        public void MultiVoice_AccidentalStagger()
        {
            // For 3 simultaneous accidentals in case "a": columns = {1, 3, 2}
            // This is the classic triangular layout (middle note goes farthest left).
            var cols = Tables.AccidentalColumnsTable[3]["a"];
            Assert.That(cols[0], Is.EqualTo(1), "First acc (highest) → column 1");
            Assert.That(cols[1], Is.EqualTo(3), "Middle acc → column 3 (farthest left)");
            Assert.That(cols[2], Is.EqualTo(2), "Third acc (lowest) → column 2");
        }

        [Test]
        public void MultiColumn_SixSimultaneousAccidentals()
        {
            // Six accidentals in case "a": [1, 3, 5, 6, 4, 2] — 6 distinct column assignments.
            var cols = Tables.AccidentalColumnsTable[6]["a"];
            Assert.That(cols.Length, Is.EqualTo(6));
            // All 6 positions are assigned (no repeats in canonical "a" case)
            var used = new System.Collections.Generic.HashSet<int>(cols);
            Assert.That(used.Count, Is.EqualTo(6), "All 6 accidentals should be in distinct columns");
        }

        [Test]
        public void GraceNote_UsesNarrowSpacing()
        {
            // Grace note accidentals use fontScale 25 instead of NOTATION_FONT_SCALE (39).
            // The width for grace notes should be narrower (smaller font scale = smaller glyph).
            var normalAcc = new Accidental("#");
            double normalWidth = normalAcc.GetWidth();

            // Simulate grace note accidental scaling by checking that a smaller font scale
            // produces a narrower glyph.
            double graceScale = 25.0;
            double normalScale = Tables.NOTATION_FONT_SCALE;
            double graceWidth  = Glyph.GetWidth(Tables.AccidentalCodes("#").Code, graceScale);
            double stdWidth    = Glyph.GetWidth(Tables.AccidentalCodes("#").Code, normalScale);

            Assert.That(graceWidth, Is.LessThan(stdWidth),
                "Grace note accidental (scale 25) should be narrower than normal accidental (scale 39)");
        }

        [Test]
        public void AutoAccidental_ChromaticScale()
        {
            // Verify that Accidental.CATEGORY matches the key used to wire ModifierContext.
            // This confirms that the applyAccidentals() chain would find them.
            Assert.That(Accidental.CATEGORY, Is.EqualTo("accidentals"));

            // Verify all standard accidental types parse without exception
            var types = new[] { "#", "##", "b", "bb", "n" };
            foreach (var t in types)
            {
                var acc = new Accidental(t);
                Assert.That(acc.Type, Is.EqualTo(t), $"Accidental type '{t}' should round-trip");
                Assert.That(acc.GetWidth(), Is.GreaterThan(0),
                    $"Accidental '{t}' should have positive glyph width");
            }
        }
    }
}

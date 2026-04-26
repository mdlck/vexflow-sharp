#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of vexflow/src/ornament.ts (347 lines)
// Ornament modifier — trill, mordent, turn, and jazz ornament glyph rendering.

using System;
using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Ornament modifier — attached to notes to indicate trills, mordents,
    /// turns, and jazz ornaments (bends, doits, falls, etc.).
    ///
    /// Port of VexFlow's Ornament class from ornament.ts.
    /// </summary>
    public class Ornament : Modifier
    {
        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>Category string used by ModifierContext to group ornaments.</summary>
        public const string CATEGORY = "ornaments";

        /// <inheritdoc/>
        public override string GetCategory() => CATEGORY;

        // ── Static ornament type groups ───────────────────────────────────────

        /// <summary>
        /// Jazz ornaments that represent an effect from one note to another.
        /// These are generally placed at the top of the staff.
        /// Port of Ornament.ornamentNoteTransition from ornament.ts.
        /// </summary>
        public static readonly string[] OrnamentNoteTransition = { "flip", "jazzTurn", "smear" };

        /// <summary>
        /// Ornaments that happen on the attack, placed before the note.
        /// Port of Ornament.ornamentAttack from ornament.ts.
        /// </summary>
        public static readonly string[] OrnamentAttack = { "scoop" };

        /// <summary>
        /// Ornaments aligned based on the note head without regard to stem direction.
        /// Port of Ornament.ornamentAlignWithNoteHead from ornament.ts.
        /// </summary>
        public static readonly string[] OrnamentAlignWithNoteHead =
        {
            "doit", "fall", "fallLong", "doitLong", "bend",
            "plungerClosed", "plungerOpen", "scoop",
        };

        /// <summary>
        /// Ornaments that happen on the release of the note (placed after).
        /// Port of Ornament.ornamentRelease from ornament.ts.
        /// </summary>
        public static readonly string[] OrnamentRelease =
        {
            "doit", "fall", "fallLong", "doitLong", "jazzTurn", "smear", "flip",
        };

        /// <summary>
        /// Ornaments that go above/below the note based on space availability.
        /// Port of Ornament.ornamentArticulation from ornament.ts.
        /// </summary>
        public static readonly string[] OrnamentArticulation =
        {
            "bend", "plungerClosed", "plungerOpen",
        };

        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>Ornament type string (e.g., "tr", "mordent", "turn").</summary>
        public readonly string Type;

        /// <summary>SMuFL glyph code for this ornament (from Tables.OrnamentCodes).</summary>
        private readonly string glyphCode;

        /// <summary>Font scale for rendering.</summary>
        private readonly double fontScale = Tables.NOTATION_FONT_SCALE;

        /// <summary>Whether the ornament is delayed (placed after the note).</summary>
        public bool Delayed { get; private set; }

        /// <summary>Whether to align with note head rather than stem.</summary>
        private readonly bool alignWithNoteHead;

        /// <summary>Accidental glyph above the ornament (e.g., trill with sharp).</summary>
        public Glyph? AccidentalUpper { get; private set; }

        /// <summary>Accidental glyph below the ornament.</summary>
        public Glyph? AccidentalLower { get; private set; }

        /// <summary>Padding between ornament and upper accidental (px).</summary>
        private const double AccidentalUpperPadding = 3.0;

        /// <summary>Padding between ornament and lower accidental (px).</summary>
        private const double AccidentalLowerPadding = 3.0;

        /// <summary>Reported width override for jazz ornaments that overlap the next bar.</summary>
        private readonly double reportedWidth;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a new ornament of the given type.
        /// Type must be a key in Tables.OrnamentCodes.
        /// Port of Ornament constructor from ornament.ts.
        /// </summary>
        public Ornament(string type)
        {
            Type = type;
            position = ModifierPosition.Above;

            if (!Tables.OrnamentCodes.TryGetValue(type, out var code))
                throw new VexFlowException("ArgumentError", $"Ornament not found: '{type}'");

            glyphCode = code;

            // Jazz ornaments that release after the note are delayed by default
            Delayed = Array.IndexOf(OrnamentNoteTransition, type) >= 0;

            alignWithNoteHead = Array.IndexOf(OrnamentAlignWithNoteHead, type) >= 0;

            // Jazz ornaments that "release" shift right; attack ornaments shift left
            // (xShift is set during Format based on state shifts)
            reportedWidth = 0; // simplified: no hard-coded per-ornament metrics

            // Set width from glyph
            double w = Glyph.GetWidth(glyphCode, fontScale);
            if (w > 0) SetWidth(w);
        }

        // ── Setters ───────────────────────────────────────────────────────────

        /// <summary>Set whether the ornament should be delayed (jazz ornaments).</summary>
        public Ornament SetDelayed(bool delayed) { Delayed = delayed; return this; }

        /// <summary>SMuFL code of the upper accidental (set via SetUpperAccidental).</summary>
        private string? accidentalUpperCode;

        /// <summary>SMuFL code of the lower accidental (set via SetLowerAccidental).</summary>
        private string? accidentalLowerCode;

        /// <summary>
        /// Set an accidental glyph above the ornament.
        /// Port of Ornament.setUpperAccidental() from ornament.ts.
        /// </summary>
        public Ornament SetUpperAccidental(string accid)
        {
            double scale = fontScale / 1.3;
            var (accCode, _) = Tables.AccidentalCodes(accid);
            accidentalUpperCode = accCode;
            AccidentalUpper = new Glyph(accCode, scale);
            return this;
        }

        /// <summary>
        /// Set an accidental glyph below the ornament.
        /// Port of Ornament.setLowerAccidental() from ornament.ts.
        /// </summary>
        public Ornament SetLowerAccidental(string accid)
        {
            double scale = fontScale / 1.3;
            var (accCode, _) = Tables.AccidentalCodes(accid);
            accidentalLowerCode = accCode;
            AccidentalLower = new Glyph(accCode, scale);
            return this;
        }

        // ── Format ────────────────────────────────────────────────────────────

        /// <summary>
        /// Arrange ornaments inside a ModifierContext.
        /// Handles both classical ornaments (text-line based) and jazz ornaments
        /// (shift-based for attack/release placement).
        ///
        /// Port of Ornament.format() from ornament.ts.
        /// </summary>
        public static bool Format(List<Ornament> ornaments, ModifierContextState state)
        {
            if (ornaments == null || ornaments.Count == 0) return false;

            double width       = 0; // centered ornaments use this
            double rightShift  = state.RightShift;
            double leftShift   = state.LeftShift;
            double yOffset     = 0;
            const int increment = 2;

            foreach (var ornament in ornaments)
            {
                bool isRelease      = Array.IndexOf(OrnamentRelease, ornament.Type) >= 0;
                bool isAttack       = Array.IndexOf(OrnamentAttack, ornament.Type) >= 0;
                bool isArticulation = Array.IndexOf(OrnamentArticulation, ornament.Type) >= 0;

                if (isRelease)
                    ornament.xShift += rightShift + 2;

                if (isAttack)
                    ornament.xShift -= leftShift + 2;

                if (ornament.reportedWidth != 0 && ornament.xShift < 0)
                    leftShift += ornament.reportedWidth;
                else if (ornament.reportedWidth != 0 && ornament.xShift >= 0)
                    rightShift += ornament.reportedWidth;
                else
                    width = Math.Max(ornament.GetWidth(), width);

                if (isArticulation)
                {
                    // Articulation-style ornaments go above or below based on line number
                    var ornNote = (Note)ornament.GetNote();
                    bool goesAbove = ornNote.GetLineNumber() >= 3
                        || ornament.GetPosition() == ModifierPosition.Above;

                    if (goesAbove)
                    {
                        state.TopTextLine += increment;
                        ornament.yShift   += yOffset;
                        yOffset           -= Tables.STAVE_LINE_DISTANCE; // approximate glyph height
                    }
                    else
                    {
                        state.TextLine  += increment;
                        ornament.yShift += yOffset;
                        yOffset         += Tables.STAVE_LINE_DISTANCE;
                    }
                }
                else
                {
                    if (ornament.GetPosition() == ModifierPosition.Above)
                    {
                        ornament.SetTextLine(state.TopTextLine);
                        state.TopTextLine += increment;
                    }
                    else
                    {
                        ornament.SetTextLine(state.TextLine);
                        state.TextLine += increment;
                    }
                }
            }

            state.LeftShift  = leftShift + width / 2;
            state.RightShift = rightShift + width / 2;
            return true;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Render the ornament glyph in position next to its attached note.
        /// Port of Ornament.draw() from ornament.ts.
        /// </summary>
        public override void Draw()
        {
            var ctx  = CheckContext();
            var note = (Note)GetNote();

            var stave    = note.CheckStave();
            double spacing = stave.GetSpacingBetweenLines();

            int stemDir = (note is StemmableNote snD) ? snD.GetStemDirection() : Stem.UP;
            bool hasStem = note.HasStem();

            // Base Y: from stem extents
            double y;
            if (hasStem && note is StemmableNote snExt)
            {
                try
                {
                    var extents = snExt.GetStemExtents();
                    y = stemDir == Stem.DOWN ? extents.BaseY : extents.TopY;
                }
                catch
                {
                    y = stave.GetYForTopText(textLine);
                }
            }
            else
            {
                y = stave.GetYForTopText(textLine);
            }

            bool isPlacedOnNoteheadSide = stemDir == Stem.DOWN;
            double lineSpacing = 1;

            // Beamed stems are longer — adjust spacing
            if (!isPlacedOnNoteheadSide && note is StaveNote snBeam)
            {
                try { if (snBeam.GetBeam() != null) lineSpacing += 0.5; }
                catch { /* no beam */ }
            }

            double totalSpacing   = spacing * (textLine + lineSpacing);
            double glyphYBetweenLines = y - totalSpacing;

            // Get modifier start position
            var start  = note.GetModifierStartXY(position, GetIndex() ?? 0);
            double glyphX = start.X + xShift;

            double glyphY;
            if (alignWithNoteHead)
                glyphY = start.Y;
            else
                glyphY = Math.Min(stave.GetYForTopText(textLine), glyphYBetweenLines);

            glyphY += yShift;

            // Adjust for delayed ornaments (jazz ornaments placed after the note)
            if (Delayed)
            {
                // Simple offset: push right by half the glyph width
                double w = Glyph.GetWidth(glyphCode, fontScale);
                glyphX += w / 2;
            }

            // Render lower accidental first
            if (AccidentalLower != null && accidentalLowerCode != null)
            {
                try
                {
                    AccidentalLower.Render(ctx, glyphX, glyphY);
                    double lowerH = Glyph.GetWidth(accidentalLowerCode, fontScale / 1.3);
                    glyphY -= lowerH + AccidentalLowerPadding;
                }
                catch { /* glyph not available — skip */ }
            }

            // Render ornament glyph
            try
            {
                var g = new Glyph(glyphCode, fontScale);
                g.Render(ctx, glyphX, glyphY);

                // Render upper accidental above the ornament
                if (AccidentalUpper != null && accidentalUpperCode != null)
                {
                    try
                    {
                        double ornH = Glyph.GetWidth(glyphCode, fontScale);
                        glyphY -= ornH + AccidentalUpperPadding;
                        AccidentalUpper.Render(ctx, glyphX, glyphY);
                    }
                    catch { /* glyph not available — skip */ }
                }
            }
            catch (Exception ex) when (ex.Message.Contains("no cached outline"))
            {
                // Glyph not available in font data — skip rendering silently
            }
        }
    }
}

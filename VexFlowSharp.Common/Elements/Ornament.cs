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
        public new const string CATEGORY = "Ornament";

        /// <inheritdoc/>
        public override string GetCategory() => CATEGORY;

        /// <summary>Minimum horizontal padding between ornaments and neighboring noteheads/modifiers.</summary>
        public static double MinPadding => Metrics.GetDouble("NoteHead.minPadding");

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
            "doit", "fall", "fallLong", "doitLong", "scoop",
        };

        /// <summary>
        /// Ornaments that happen on the release of the note (placed after).
        /// Port of Ornament.ornamentRelease from ornament.ts.
        /// </summary>
        public static readonly string[] OrnamentRelease =
        {
            "doit", "fall", "fallLong", "doitLong", "jazzTurn", "smear", "flip",
        };

        public static readonly string[] OrnamentLeft = { "scoop" };

        public static readonly string[] OrnamentRight = { "doit", "fall", "fallLong", "doitLong" };

        public static readonly string[] OrnamentYShift = { "fallLong" };

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

        private double? delayXShift;

        /// <summary>Accidental glyph above the ornament (e.g., trill with sharp).</summary>
        public Glyph? AccidentalUpper { get; private set; }

        /// <summary>Accidental glyph below the ornament.</summary>
        public Glyph? AccidentalLower { get; private set; }

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
            if (Array.IndexOf(OrnamentRight, type) >= 0)
                position = ModifierPosition.Right;
            if (Array.IndexOf(OrnamentLeft, type) >= 0)
                position = ModifierPosition.Left;

            glyphCode = Tables.OrnamentCode(type);

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

        public override Modifier SetNote(Element n)
        {
            base.SetNote(n);
            if (Array.IndexOf(OrnamentArticulation, Type) >= 0 && n is Note note)
                position = note.GetLineNumber() >= 3 ? ModifierPosition.Above : ModifierPosition.Below;
            return this;
        }

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
            double increment = Metrics.GetDouble("Ornament.textLineIncrement");
            double sideShift = Metrics.GetDouble("Ornament.sideShift");

            foreach (var ornament in ornaments)
            {
                bool isArticulation = Array.IndexOf(OrnamentArticulation, ornament.Type) >= 0;

                if (ornament.GetPosition() == ModifierPosition.Right)
                {
                    ornament.xShift += rightShift + sideShift;
                    rightShift += ornament.GetWidth() + MinPadding;
                    continue;
                }

                if (ornament.GetPosition() == ModifierPosition.Left)
                {
                    ornament.xShift -= leftShift + ornament.GetWidth() + sideShift;
                    leftShift += ornament.GetWidth() + MinPadding;
                    continue;
                }

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
                        yOffset           -= ornament.GetGlyphHeight();
                    }
                    else
                    {
                        state.TextLine  += increment;
                        ornament.yShift += yOffset;
                        yOffset         += ornament.GetGlyphHeight();
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

        private double GetGlyphHeight()
        {
            return new Glyph(glyphCode, fontScale).GetMetrics()?.Height ?? Tables.STAVE_LINE_DISTANCE;
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
            rendered = true;

            string groupClass = "ornament";
            var classAttribute = GetAttribute("class");
            if (!string.IsNullOrEmpty(classAttribute))
                groupClass += " " + classAttribute;
            ctx.OpenGroup(groupClass, GetId());

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
            double lineSpacing = Metrics.GetDouble("Ornament.lineSpacing");

            // Beamed stems are longer — adjust spacing
            if (!isPlacedOnNoteheadSide && note is StaveNote snBeam)
            {
                try { if (snBeam.GetBeam() != null) lineSpacing += Metrics.GetDouble("Ornament.beamedLineSpacing"); }
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

            if (Delayed)
            {
                double computedDelay = delayXShift ?? ComputeDelayXShift(note, stave, glyphX);
                delayXShift = computedDelay;
                glyphX += computedDelay;
            }

            // Render lower accidental first
            if (AccidentalLower != null && accidentalLowerCode != null)
            {
                try
                {
                    var metrics = AccidentalLower.GetMetrics();
                    double lowerX = glyphX + xShift - (metrics?.Width ?? 0) * 0.5;
                    AccidentalLower.Render(ctx, lowerX, glyphY);
                    glyphY -= (metrics?.Height ?? Glyph.GetWidth(accidentalLowerCode, fontScale / 1.3))
                        + Metrics.GetDouble("Ornament.accidentalLowerPadding");
                }
                catch { /* glyph not available — skip */ }
            }

            if (Array.IndexOf(OrnamentYShift, Type) >= 0)
                yShift += new Glyph(glyphCode, fontScale).GetMetrics()?.Height ?? GetWidth();

            // Render ornament glyph
            try
            {
                var g = new Glyph(glyphCode, fontScale);
                double renderX = glyphX + xShift;
                if (position == ModifierPosition.Above || position == ModifierPosition.Below)
                    renderX -= GetWidth() * 0.5;
                g.Render(ctx, renderX, glyphY + yShift);

                // Render upper accidental above the ornament
                if (AccidentalUpper != null && accidentalUpperCode != null)
                {
                    try
                    {
                        double ornH = g.GetMetrics()?.Height ?? GetWidth();
                        glyphY -= ornH + Metrics.GetDouble("Ornament.accidentalUpperPadding");
                        var upperMetrics = AccidentalUpper.GetMetrics();
                        double upperX = glyphX + xShift - (upperMetrics?.Width ?? 0) * 0.5;
                        AccidentalUpper.Render(ctx, upperX, glyphY + yShift);
                    }
                    catch { /* glyph not available — skip */ }
                }
            }
            catch (Exception ex) when (ex.Message.Contains("no cached outline"))
            {
                // Glyph not available in font data — skip rendering silently
            }

            DrawPointerRect();
            ctx.CloseGroup();
        }

        private double ComputeDelayXShift(Note note, Stave stave, double glyphX)
        {
            double startX = note.GetTickContext()?.GetX() ?? note.GetAbsoluteX();
            var voice = note.GetVoice();
            if (voice != null)
            {
                var tickables = voice.GetTickables();
                int index = tickables.IndexOf(note);
                if (index >= 0 && index + 1 < tickables.Count)
                {
                    var nextContext = tickables[index + 1].GetTickContext();
                    if (nextContext != null)
                        return (nextContext.GetX() - startX) * 0.5;
                }
            }

            return (stave.GetX() + stave.GetWidth() - glyphX) * 0.5;
        }
    }
}

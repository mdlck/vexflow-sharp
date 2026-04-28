#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of vexflow/src/dot.ts (182 lines)
// Dot augmentation modifier — extends Modifier, placed to the RIGHT of noteheads.

using System;
using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Augmentation dot modifier. Attached to notes to indicate dotted duration.
    /// Dots are positioned to the RIGHT of the notehead, shifted off staff lines
    /// onto adjacent spaces.
    ///
    /// Port of VexFlow's Dot class from dot.ts.
    /// </summary>
    public class Dot : Modifier
    {
        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>Category string used by ModifierContext to group dots.</summary>
        public new const string CATEGORY = "Dot";

        /// <inheritdoc/>
        public override string GetCategory() => CATEGORY;

        // ── Fields ────────────────────────────────────────────────────────────

        private double radius;
        private double dotShiftY;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a new Dot at the RIGHT position.
        /// Port of Dot constructor from dot.ts.
        /// </summary>
        public Dot()
        {
            position = ModifierPosition.Right;
            radius   = Metrics.GetDouble("Dot.radius");
            dotShiftY = 0;
            SetWidth(Metrics.GetDouble("Dot.width"));
        }

        // ── Accessors ─────────────────────────────────────────────────────────

        /// <summary>Get the dot render radius.</summary>
        public double GetRadius() => radius;

        /// <summary>Set the dot render radius.</summary>
        public void SetRadius(double r) { radius = r; }

        /// <summary>Get the vertical shift applied to this dot (in pixels).</summary>
        public double GetDotShiftY() => dotShiftY;

        /// <summary>Set the vertical shift for this dot (in fractional line units — converted to px at draw).</summary>
        public void SetDotShiftY(double y) { dotShiftY = y; }

        /// <summary>
        /// When the note is a grace note, shrink the dot radius and width.
        /// Port of Dot.setNote() from dot.ts.
        /// </summary>
        public new Dot SetNote(Element n)
        {
            base.SetNote(n);
            if (n is GraceNote)
            {
                radius *= Metrics.GetDouble("Dot.graceScale");
                SetWidth(Metrics.GetDouble("Dot.graceWidth"));
            }
            return this;
        }

        // ── BuildAndAttach ────────────────────────────────────────────────────

        /// <summary>
        /// Create and attach dots to the given notes.
        /// When allNotes is true, attaches one dot per key in each note.
        /// When index is specified, attaches dot to that key index only.
        /// Otherwise attaches dot to key index 0.
        ///
        /// Port of VexFlow's Dot.buildAndAttach() from dot.ts lines 26-44.
        /// </summary>
        public static void BuildAndAttach(List<Note> notes, bool allNotes = false, int? index = null)
        {
            foreach (var note in notes)
            {
                if (allNotes)
                {
                    for (int i = 0; i < note.GetKeys().Length; i++)
                    {
                        var dot = new Dot();
                        dot.SetDotShiftY(note.GetGlyphProps().DotShiftY);
                        note.AddModifier(dot, i);
                    }
                }
                else if (index.HasValue)
                {
                    var dot = new Dot();
                    dot.SetDotShiftY(note.GetGlyphProps().DotShiftY);
                    note.AddModifier(dot, index.Value);
                }
                else
                {
                    var dot = new Dot();
                    dot.SetDotShiftY(note.GetGlyphProps().DotShiftY);
                    note.AddModifier(dot, 0);
                }
            }
        }

        // ── Format ────────────────────────────────────────────────────────────

        /// <summary>
        /// Arrange dots inside a ModifierContext.
        /// Port of Dot.format() from dot.ts — handles staff-line detection and
        /// half-space vertical shift when dots fall on a staff line.
        /// </summary>
        public static bool Format(List<Dot>? dots, ModifierContextState state)
        {
            if (dots == null || dots.Count == 0) return false;

            double rightShift   = state.RightShift;
            double dotSpacing   = Metrics.GetDouble("Dot.spacing");

            // Build list enriched with line position info
            var dotList = new List<(double Line, Note Note, string NoteId, Dot Dot)>();
            var maxShiftMap = new Dictionary<string, double>();

            for (int i = 0; i < dots.Count; i++)
            {
                var dot  = dots[i];
                var note = (Note)dot.GetNote();

                double line;
                double shift;

                if (note is StaveNote sn)
                {
                    int idx = dot.GetIndex() ?? 0;
                    line  = sn.GetKeyProps()[idx].Line;
                    shift = sn.GetFirstDotPx();
                }
                else
                {
                    // Default: place on a space
                    line  = 0.5;
                    shift = rightShift;
                }

                string noteId = note.GetId() ?? i.ToString();
                dotList.Add((line, note, noteId, dot));

                if (!maxShiftMap.TryGetValue(noteId, out var existing) || shift > existing)
                    maxShiftMap[noteId] = shift;
            }

            // Sort by descending line (top of staff first, matching VexFlow)
            dotList.Sort((a, b) => b.Line.CompareTo(a.Line));

            double dotShift          = rightShift;
            double xWidth            = 0;
            double? lastLine         = null;
            Note?   lastNote         = null;
            double? prevDottedSpace  = null;
            double  halfShiftY       = 0;

            for (int i = 0; i < dotList.Count; i++)
            {
                var (line, note, noteId, dot) = dotList[i];

                // Reset dot_shift whenever the line or note changes
                if (line != lastLine || note != lastNote)
                    dotShift = maxShiftMap[noteId];

                if (!note.IsRest() && line != lastLine)
                {
                    if (Math.Abs(line % 1) == 0.5)
                    {
                        // Note is on a space — no vertical adjustment needed
                        halfShiftY = 0;
                    }
                    else
                    {
                        // Note is on a staff line — shift dot up to the space above
                        halfShiftY = 0.5;
                        if (lastNote != null && !lastNote.IsRest()
                            && lastLine != null && lastLine - line == 0.5)
                        {
                            // Previous note was on a space — shift down instead
                            halfShiftY = -0.5;
                        }
                        else if (prevDottedSpace.HasValue && line + halfShiftY == prevDottedSpace.Value)
                        {
                            // Previous space is already dotted — shift down
                            halfShiftY = -0.5;
                        }
                    }
                }

                // Convert half_shiftY multiplier (matches VexFlow dot.ts draw(): dot_shiftY * lineSpace)
                if (note.IsRest())
                    dot.dotShiftY += -halfShiftY;
                else
                    dot.dotShiftY = -halfShiftY;

                prevDottedSpace = line + halfShiftY;

                dot.SetXShift(dotShift);
                dotShift += dot.GetWidth() + dotSpacing;
                if (dotShift > xWidth) xWidth = dotShift;

                lastLine = line;
                lastNote = note;
            }

            // Update state
            state.RightShift += xWidth;
            return true;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Render the dot onto the canvas.
        /// Port of Dot.draw() from dot.ts.
        /// </summary>
        public override void Draw()
        {
            var ctx  = CheckContext();
            var note = (Note)GetNote();
            SetRendered();
            note.CheckStave(); // validates stave is set; we read lineSpace from it
            int idx  = GetIndex() ?? 0;

            var stave = note.CheckStave();
            var start = note.GetModifierStartXY(ModifierPosition.Right, idx, new { forceFlagRight = true });
            if (note is TabNote tabNote)
                start.Y = tabNote.GetStemExtents().BaseY;

            double lineSpace = stave.GetSpacingBetweenLines();

            double x = start.X + xShift + width - radius;
            double y = start.Y + yShift + dotShiftY * lineSpace;

            ctx.BeginPath();
            ctx.Arc(x, y, radius, 0, Math.PI * 2, false);
            ctx.Fill();
        }
    }
}

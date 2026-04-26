#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's GraceNoteGroup class (gracenotegroup.ts, 194 lines).
// GraceNoteGroup collects grace notes and positions them before the main note.

using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// GraceNoteGroup is a modifier that positions and renders grace notes
    /// immediately before their associated main note.
    ///
    /// Phase 2 limitations:
    /// - PreFormat() is a stub; full Voice/Formatter integration is Phase 3.
    /// - Format() is a no-op stub (Phase 3 responsibility).
    /// - Slur rendering is simplified (no StaveTie/TabTie in Phase 2).
    ///
    /// Port of VexFlow's GraceNoteGroup class from gracenotegroup.ts.
    /// </summary>
    public class GraceNoteGroup : Modifier
    {
        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>The grace notes belonging to this group.</summary>
        protected List<GraceNote> graceNotes;

        /// <summary>Whether to draw a slur connecting the grace notes to the main note.</summary>
        protected bool showSlur;

        /// <summary>Y shift for the slur arc.</summary>
        protected double slurY;

        /// <summary>Whether this group has been pre-formatted.</summary>
        protected bool preFormatted;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Construct a GraceNoteGroup with the given grace notes.
        /// Position is LEFT — grace notes are placed before the main note.
        /// Port of GraceNoteGroup constructor from gracenotegroup.ts.
        /// </summary>
        /// <param name="graceNotes">The grace notes in this group.</param>
        /// <param name="showSlur">Whether to render a slur to the main note (default true).</param>
        public GraceNoteGroup(List<GraceNote> graceNotes, bool showSlur = true)
        {
            this.graceNotes = graceNotes;
            this.showSlur = showSlur;
            this.slurY = 0;
            this.preFormatted = false;
            this.width = 0;
            position = ModifierPosition.Left;
        }

        // ── Width ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Get the total width of this grace note group, including minimum padding.
        /// Port of GraceNoteGroup.getWidth() from gracenotegroup.ts.
        /// </summary>
        public new double GetWidth()
        {
            // Include a minimum notehead padding so the group does not crowd the main note
            return width + StaveNote.minNoteheadPadding;
        }

        // ── Accessor ──────────────────────────────────────────────────────────

        /// <summary>Get the grace notes in this group.</summary>
        public List<GraceNote> GetGraceNotes() => graceNotes;

        // ── PreFormat stub ────────────────────────────────────────────────────

        /// <summary>
        /// Pre-format stub. Computes width as the sum of all grace note widths.
        ///
        /// PHASE 2 STUB: Does NOT use Voice or Formatter (Pitfall 6).
        /// Full formatting using Voice and Formatter is a Phase 3 responsibility.
        /// Port of GraceNoteGroup.preFormat() from gracenotegroup.ts.
        /// </summary>
        public void PreFormat()
        {
            if (preFormatted) return;

            // Compute total width from grace note widths (no Formatter in Phase 2)
            double totalWidth = 0;
            foreach (var gn in graceNotes)
            {
                totalWidth += gn.GetWidth();
            }

            SetWidth(totalWidth);
            preFormatted = true;
        }

        // ── Format stub ───────────────────────────────────────────────────────

        /// <summary>
        /// Format stub — no-op in Phase 2.
        /// Full implementation using Voice and Formatter arrives in Phase 3.
        /// </summary>
        public void Format()
        {
            // Phase 3: use Voice and Formatter to arrange grace notes
        }

        // ── Draw override ─────────────────────────────────────────────────────

        /// <summary>
        /// Draw all grace notes and optionally a slur arc to the main note.
        /// Uses a Voice and Formatter to position grace notes on the main note's stave.
        /// Port of GraceNoteGroup.draw() from gracenotegroup.ts.
        /// </summary>
        public override void Draw()
        {
            var ctx = CheckContext();
            rendered = true;

            // Retrieve the stave from the main note — required for positioning
            var mainNote = note as Note;
            var mainStave = mainNote?.GetStave();
            if (mainStave == null) return;

            // Build a SOFT voice for the grace notes and format them on the stave.
            // SOFT mode doesn't enforce beat count, suitable for grace note groups.
            var graceVoice = new Voice(new VoiceTime { NumBeats = 4, BeatValue = 4 });
            graceVoice.SetMode(VoiceMode.SOFT);
            var tickables = new List<Tickable>();
            foreach (var gn in graceNotes) tickables.Add(gn);
            graceVoice.AddTickables(tickables);

            // Format the grace notes into a small space just before the main note.
            // We use a tight width equal to the group's own computed width.
            double formatWidth = System.Math.Max(GetWidth(), 30.0);
            new Formatter()
                .JoinVoices(new List<Voice> { graceVoice })
                .Format(new List<Voice> { graceVoice }, formatWidth);

            // Set the stave on each grace note so Y values are computed correctly.
            foreach (var gn in graceNotes)
                gn.SetStave(mainStave);

            // Position grace notes to the left of the main note.
            // Strategy: set each grace note's tick context X directly so that
            // GetNoteHeadBeginX() lands at mainNote - groupWidth + gnRelativeX.
            //
            // We cannot use SetXShift() here because GetNoteHeadBeginX() =
            // GetAbsoluteX() + xShift and GetAbsoluteX() already includes xShift,
            // causing double-counting. VexFlow avoids this by adjusting tc.GetX()
            // directly in GraceNoteGroup.format().
            double groupWidth = GetWidth();
            double mainTcX = mainNote!.GetTickContext()?.GetX() ?? 0;
            double gnRelX0 = graceNotes[0].GetTickContext()?.GetX() ?? 0;

            foreach (var gn in graceNotes)
            {
                var gnTc = gn.GetTickContext();
                if (gnTc == null) continue;
                double gnRelX = gnTc.GetX() - gnRelX0; // position within the group
                gnTc.SetX(mainTcX - groupWidth + gnRelX);
            }

            // Draw each grace note
            foreach (var gn in graceNotes)
            {
                gn.SetContext(ctx).Draw();
            }

            // Draw a simple slur arc if showSlur is true
            if (showSlur && graceNotes.Count > 0 && mainNote != null)
            {
                DrawSimpleSlur(ctx);
            }
        }

        /// <summary>
        /// Draw a simplified slur arc from the last grace note to the main note.
        /// Phase 2 simplified version — uses a basic quadratic bezier curve.
        /// Phase 3 will replace this with a StaveTie or TabTie.
        /// </summary>
        private void DrawSimpleSlur(RenderContext ctx)
        {
            var lastGrace = graceNotes[graceNotes.Count - 1];
            double x1 = lastGrace.GetAbsoluteX();
            // note is typed as Element? in Modifier base class; cast to Note for GetAbsoluteX()
            double x2 = (note as Note)?.GetAbsoluteX() ?? lastGrace.GetAbsoluteX() + 10;

            // Use a basic arc above/below the notes
            double[] graceYs;
            try { graceYs = lastGrace.GetYs(); }
            catch { return; } // No y-values set — skip slur

            double y1 = graceYs[0];
            double midX = (x1 + x2) / 2.0;
            double midY = y1 - 8 + slurY; // Arc above the notes

            ctx.Save();
            ctx.BeginPath();
            ctx.MoveTo(x1, y1);
            ctx.QuadraticCurveTo(midX, midY, x2, y1);
            ctx.Stroke();
            ctx.Restore();
        }
    }
}

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's NoteSubGroup class (notesubgroup.ts, 88 lines).
// NoteSubGroup wraps a list of sub-notes as a left-positioned Modifier with an
// embedded Formatter and Voice. Used for inline note groupings such as mid-measure
// clef changes (ClefNote), time signature changes (TimeSigNote), and bar notes.

using System;
using System.Collections.Generic;
using System.Linq;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Modifier that contains a sub-group of notes formatted inline with the parent note.
    /// Each sub-note is formatted via an embedded Formatter+Voice pair, then drawn
    /// to the left of the parent note's position.
    ///
    /// Port of VexFlow's NoteSubGroup class from notesubgroup.ts.
    /// </summary>
    public class NoteSubGroup : Modifier
    {
        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>Category string used by ModifierContext for dispatch.</summary>
        public static string CATEGORY = "notesubgroups";

        // ── Fields ────────────────────────────────────────────────────────────

        private readonly List<Note> subNotes;
        private readonly Formatter  formatter;
        private readonly Voice      voice;
        private bool preFormatted = false;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a NoteSubGroup containing the given sub-notes.
        /// Position defaults to Left. Uses a SOFT-mode Voice so tick totals are not enforced.
        /// </summary>
        public NoteSubGroup(List<Note> subNotes)
        {
            this.subNotes = subNotes ?? throw new ArgumentNullException(nameof(subNotes));
            position = ModifierPosition.Left;
            width    = 0;

            formatter = new Formatter();

            // Voice uses a 4/4 time signature with default resolution.
            // SOFT mode allows sub-note tick counts that do not fill the measure.
            voice = new Voice(new VoiceTime { NumBeats = 4, BeatValue = 4,
                                              Resolution = Tables.RESOLUTION })
                        .SetMode(VoiceMode.SOFT);

            // Sub-notes do not enforce tick boundaries (match VexFlow setIgnoreTicks(false) ↔
            // already false by default, but we still use SOFT voice mode to be safe)
            voice.AddTickables(subNotes.Cast<Tickable>().ToList());
        }

        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>Return the category string for ModifierContext dispatch.</summary>
        public override string GetCategory() => CATEGORY;

        // ── Width ─────────────────────────────────────────────────────────────

        /// <summary>Set width explicitly (also used by PreFormat after formatting).</summary>
        public new NoteSubGroup SetWidth(double w) { width = w; return this; }

        /// <summary>Get the formatted width (valid after PreFormat).</summary>
        public new double GetWidth() => width;

        // ── PreFormat ─────────────────────────────────────────────────────────

        /// <summary>
        /// Format the sub-notes via the embedded Formatter+Voice.
        /// Sets width to the minimum total width required for the group.
        /// Idempotent — only runs once.
        ///
        /// Port of VexFlow's NoteSubGroup.preFormat() from notesubgroup.ts.
        /// </summary>
        public void PreFormat()
        {
            if (preFormatted) return;

            formatter.JoinVoices(new List<Voice> { voice });
            formatter.Format(new List<Voice> { voice }, 0);
            width = formatter.GetMinTotalWidth();
            preFormatted = true;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw the sub-notes, aligning each to the parent note's absolute x position.
        /// Port of VexFlow's NoteSubGroup.draw() from notesubgroup.ts.
        /// </summary>
        public override void Draw()
        {
            var ctx = CheckContext();

            var parentNote = (Note)GetNote();

            // Align sub-note tick contexts to the parent note's position
            AlignSubNotesWithNote(subNotes, parentNote);

            // Draw each sub-note
            foreach (var subNote in subNotes)
            {
                subNote.SetContext(ctx);
                subNote.Draw();
            }

            rendered = true;
        }

        // ── Static Format for ModifierContext ─────────────────────────────────

        /// <summary>
        /// Static format function dispatched by ModifierContext at slot 8.
        /// Calls PreFormat on each group and accumulates the total width into
        /// the left-shift of the ModifierContextState.
        ///
        /// Port of VexFlow's NoteSubGroup.format() static method from notesubgroup.ts.
        /// </summary>
        public static bool Format(List<NoteSubGroup> groups, ModifierContextState state)
        {
            if (groups == null || groups.Count == 0) return false;

            double totalWidth = 0;
            foreach (var group in groups)
            {
                group.PreFormat();
                totalWidth += group.GetWidth();
            }

            state.LeftShift += totalWidth;
            return true;
        }
    }
}

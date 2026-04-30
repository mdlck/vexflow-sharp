// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's GraceNoteGroup class (gracenotegroup.ts, 194 lines).
// GraceNoteGroup collects grace notes and positions them before the main note.

using System.Collections.Generic;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public class GraceNoteGroupRenderOptions
    {
        public double SlurYShift { get; set; }
    }

    /// <summary>
    /// GraceNoteGroup is a modifier that positions and renders grace notes
    /// immediately before their associated main note.
    ///
    /// Port of VexFlow's GraceNoteGroup class from gracenotegroup.ts.
    /// </summary>
    public class GraceNoteGroup : Modifier
    {
        public new const string CATEGORY = "GraceNoteGroup";

        public override string GetCategory() => CATEGORY;

        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>The grace notes belonging to this group.</summary>
        protected List<GraceNote> graceNotes;

        /// <summary>Whether to draw a slur connecting the grace notes to the main note.</summary>
        protected bool showSlur;

        /// <summary>Whether this group has been pre-formatted.</summary>
        protected bool preFormatted;

        protected readonly Voice voice;
        protected Formatter formatter;
        private readonly List<Beam> beams;
        private StaveTie slur;

        public GraceNoteGroupRenderOptions RenderOptions { get; }

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
            this.preFormatted = false;
            this.width = 0;
            this.beams = new List<Beam>();
            this.voice = new Voice(new VoiceTime
            {
                NumBeats = 4,
                BeatValue = 4,
                Resolution = Tables.RESOLUTION,
            }).SetStrict(false);
            this.RenderOptions = new GraceNoteGroupRenderOptions { SlurYShift = 0 };
            position = ModifierPosition.Left;

            var tickables = new List<Tickable>();
            foreach (var gn in graceNotes) tickables.Add(gn);
            voice.AddTickables(tickables);
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

        /// <summary>Whether this group renders a slur to the main note.</summary>
        public bool GetShowSlur() => showSlur;

        public GraceNoteGroup BeamNotes(List<GraceNote> notes = null)
        {
            notes ??= graceNotes;
            if (notes.Count > 1)
            {
                var beamNotes = new List<StemmableNote>();
                foreach (var note in notes)
                    beamNotes.Add(note);

                var beam = new Beam(beamNotes);
                beam.RenderOptions.BeamWidth = 3;
                beam.RenderOptions.PartialBeamLength = 4;
                beams.Add(beam);
            }

            return this;
        }

        public static bool Format(List<GraceNoteGroup> gracenoteGroups, ModifierContextState state)
        {
            const double groupSpacingStave = 4;
            const double groupSpacingTab = 0;

            if (gracenoteGroups == null || gracenoteGroups.Count == 0) return false;

            var groupList = new List<(double Shift, GraceNoteGroup Group, double Spacing)>();
            Note prevNote = null;
            double shift = 0;

            foreach (var gracenoteGroup in gracenoteGroups)
            {
                var note = gracenoteGroup.TryGetAttachedNote();
                bool isStaveNote = note is StaveNote;
                double spacing = isStaveNote ? groupSpacingStave : groupSpacingTab;

                if (isStaveNote && note != prevNote)
                {
                    shift = System.Math.Max(note!.GetLeftDisplacedHeadPx(), shift);
                    prevNote = note;
                }

                groupList.Add((shift, gracenoteGroup, spacing));
            }

            double groupShift = groupList[0].Shift;
            bool right = false;
            bool left = false;
            foreach (var item in groupList)
            {
                if (item.Group.GetPosition() == ModifierPosition.Right) right = true;
                else left = true;

                item.Group.PreFormat();
                double formatWidth = item.Group.GetWidth() + item.Spacing;
                groupShift = System.Math.Max(formatWidth, groupShift);
            }

            foreach (var item in groupList)
            {
                double formatWidth = item.Group.GetWidth() + item.Spacing;
                item.Group.SetSpacingFromNextModifier(
                    groupShift - System.Math.Min(formatWidth, groupShift) + StaveNote.minNoteheadPadding);
            }

            if (right) state.RightShift += groupShift;
            if (left) state.LeftShift += groupShift;
            return true;
        }

        // ── PreFormat ─────────────────────────────────────────────────────────

        /// <summary>
        /// Pre-format the grace-note voice and record the formatter's minimum total width.
        /// Port of GraceNoteGroup.preFormat() from gracenotegroup.ts.
        /// </summary>
        public void PreFormat()
        {
            if (preFormatted) return;

            formatter ??= new Formatter();
            formatter.JoinVoices(new List<Voice> { voice });
            formatter.Format(new List<Voice> { voice }, 0, new FormatParams());
            SetWidth(formatter.GetMinTotalWidth());
            preFormatted = true;
        }

        // ── Format ────────────────────────────────────────────────────────────

        /// <summary>
        /// Instance compatibility wrapper; modifier-context layout is handled by the static Format().
        /// </summary>
        public void Format()
        {
            PreFormat();
        }

        private Note TryGetAttachedNote()
        {
            try { return GetNote() as Note; }
            catch { return null; }
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
            var mainStave = mainNote.GetStave();
            if (mainStave == null) return;

            PreFormat();

            if (mainNote!.GetTickContext() != null)
            {
                AlignSubNotesWithNote(new List<Note>(graceNotes), mainNote);
            }
            else
            {
                // Direct unit-test rendering can draw an attached note without a parent TickContext.
                // Keep that path positioned from the parent x while formatted voices provide v5 offsets.
                foreach (var gn in graceNotes)
                    gn.SetStave(mainStave);

                double groupWidth = GetWidth();
                double mainX = mainNote.GetAbsoluteX();
                double gnRelX0 = graceNotes.Count > 0 ? graceNotes[0].GetTickContext()?.GetX() ?? 0 : 0;

                foreach (var gn in graceNotes)
                {
                    var gnTc = gn.GetTickContext();
                    if (gnTc == null) continue;
                    double gnRelX = gnTc.GetX() - gnRelX0;
                    gnTc.SetX(mainX - groupWidth + gnRelX);
                }
            }

            // Draw each grace note
            foreach (var gn in graceNotes)
            {
                gn.SetContext(ctx).Draw();
            }

            foreach (var beam in beams)
            {
                beam.SetContext(ctx).Draw();
            }

            if (showSlur && graceNotes.Count > 0 && mainNote != null)
            {
                DrawSlur(ctx, mainNote);
            }
        }

        private void DrawSlur(RenderContext ctx, Note mainNote)
        {
            var tieNotes = new TieNotes
            {
                LastNote = graceNotes[0],
                FirstNote = mainNote,
                FirstIndexes = new[] { 0 },
                LastIndexes = new[] { 0 },
            };

            slur = mainNote is StaveNote
                ? new StaveTie(tieNotes)
                : new TabTie(tieNotes);
            slur.RenderOptions.Cp2 = 12;
            slur.RenderOptions.YShift = (mainNote is StaveNote ? 7 : 5) + RenderOptions.SlurYShift;
            slur.SetContext(ctx).Draw();
        }
    }
}

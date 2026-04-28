// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's GhostNote class (ghostnote.ts, 75 lines).
// GhostNote is an invisible spacer note: width=0, no-op Draw(), ticks still counted.
// Used to create spacing in a voice without rendering any notation.

namespace VexFlowSharp
{
    /// <summary>
    /// An invisible spacer note that takes up no visual space but still counts ticks.
    ///
    /// Used to create spacing in a Voice without rendering notation.
    /// Draw() is a no-op — nothing is rendered.
    ///
    /// Port of VexFlow's GhostNote class from ghostnote.ts.
    /// </summary>
    public class GhostNote : StemmableNote
    {
        public new const string CATEGORY = "GhostNote";

        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>Whether this note has been pre-formatted.</summary>
        private bool preFormatted = false;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Construct a GhostNote from a duration string.
        /// Convenience factory for the common case.
        /// Port of GhostNote(parameter: string) from ghostnote.ts.
        /// </summary>
        public GhostNote(string duration)
            : this(new NoteStruct { Duration = duration })
        {
        }

        /// <summary>
        /// Construct a GhostNote from a NoteStruct.
        /// Sets width to 0; note is invisible but still occupies time.
        /// Port of GhostNote(parameter: NoteStruct) from ghostnote.ts.
        /// </summary>
        public GhostNote(NoteStruct noteStruct) : base(noteStruct)
        {
            // Ghost notes have zero visual width
            SetWidth(0);
        }

        // ── Rest override ─────────────────────────────────────────────────────

        /// <summary>
        /// GhostNote is considered a rest — it has no pitch.
        /// Port of GhostNote.isRest() from ghostnote.ts.
        /// </summary>
        public override bool IsRest() => true;

        public override string GetCategory() => CATEGORY;

        // ── SetStave override ─────────────────────────────────────────────────

        /// <summary>
        /// Attach to a stave. Delegates to Note.SetStave() but does NOT compute ys
        /// (ghost notes have no visual position on the staff).
        /// Port of GhostNote.setStave() from ghostnote.ts.
        /// </summary>
        public override Note SetStave(Stave newStave)
        {
            // Call Note.SetStave directly (skip StaveNote's y-computation)
            base.SetStave(newStave);
            return this;
        }

        // ── PreFormat override ────────────────────────────────────────────────

        /// <summary>
        /// Pre-format stub. Sets preFormatted = true; width remains 0.
        /// Port of GhostNote.preFormat() from ghostnote.ts.
        /// </summary>
        public override void PreFormat()
        {
            if (preFormatted) return;
            preFormatted = true;
        }

        // ── Draw override ─────────────────────────────────────────────────────

        /// <summary>
        /// Draw is a no-op for ghost notes. Validates context exists, then marks rendered.
        /// Port of GhostNote.draw() from ghostnote.ts.
        /// </summary>
        public override void Draw()
        {
            CheckContext();
            rendered = true;
        }
    }
}

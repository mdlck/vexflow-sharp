// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Position of a modifier relative to a note.
    /// Port of VexFlow's ModifierPosition enum from modifier.ts.
    /// </summary>
    public enum ModifierPosition
    {
        Center = 0,
        Left   = 1,
        Right  = 2,
        Above  = 3,
        Below  = 4,
    }

    /// <summary>
    /// Abstract base class for notational elements that modify a Note.
    /// Examples: Accidental, Annotation, Stroke, etc.
    ///
    /// Port of VexFlow's Modifier class from modifier.ts.
    /// </summary>
    public abstract class Modifier : Element
    {
        public new const string CATEGORY = "Modifier";

        // The note this modifier is attached to. Typed as Element because Note.cs
        // does not exist yet; plan 02-03 will narrow this when Note is defined.
        protected Element note;

        /// <summary>Index of the note head within a chord that this modifier targets.</summary>
        protected int? index;

        protected double width = 0;
        protected double textLine = 0;
        protected ModifierPosition position = ModifierPosition.Left;
        protected double yShift = 0;
        protected double xShift = 0;

        private double spacingFromNextModifier = 0;

        protected Modifier()
        {
            width = 0;
            textLine = 0;
            position = ModifierPosition.Left;
            xShift = 0;
            yShift = 0;
            spacingFromNextModifier = 0;
        }

        public override string GetCategory() => CATEGORY;

        /// <summary>Called when position changes. Override to react to position updates.</summary>
        protected virtual void Reset() { }

        // ── Width ──────────────────────────────────────────────────────────────

        /// <summary>Get modifier width.</summary>
        public double GetWidth() => width;

        /// <summary>Set modifier width. Returns this for fluent chaining.</summary>
        public Modifier SetWidth(double w) { width = w; return this; }

        // ── Note attachment ────────────────────────────────────────────────────

        /// <summary>Get the attached note element. Throws if not set.</summary>
        public Element GetNote()
            => note ?? throw new VexFlowException("NoNote", "Modifier has no note.");

        /// <summary>Set the attached note element. Returns this for fluent chaining.</summary>
        public virtual Modifier SetNote(Element n) { note = n; return this; }

        // ── Index ──────────────────────────────────────────────────────────────

        /// <summary>Get the note head index (may be null).</summary>
        public int? GetIndex() => index;

        /// <summary>Set the note head index. Returns this for fluent chaining.</summary>
        public Modifier SetIndex(int i) { index = i; return this; }

        // ── Position ───────────────────────────────────────────────────────────

        /// <summary>Get the modifier position.</summary>
        public ModifierPosition GetPosition() => position;

        /// <summary>Set the modifier position and call Reset(). Returns this for fluent chaining.</summary>
        public Modifier SetPosition(ModifierPosition p) { position = p; Reset(); return this; }

        // ── X/Y shifts ────────────────────────────────────────────────────────

        /// <summary>
        /// Shift modifier x pixels in the direction of the modifier.
        /// Negative values shift reverse. Mirrors VexFlow setXShift behaviour.
        /// </summary>
        public Modifier SetXShift(double x)
        {
            xShift = 0;
            if (position == ModifierPosition.Left)
                xShift -= x;
            else
                xShift += x;
            return this;
        }

        /// <summary>Get the x shift value.</summary>
        public double GetXShift() => xShift;

        /// <summary>Shift modifier down y pixels (negative shifts up). Returns this.</summary>
        public Modifier SetYShift(double y) { yShift = y; return this; }

        /// <summary>Get the y shift value.</summary>
        public double GetYShift() => yShift;

        // ── Text line ─────────────────────────────────────────────────────────

        /// <summary>Get the text line reserved above/below the stave.</summary>
        public double GetTextLine() => textLine;

        /// <summary>Set the text line. Returns this for fluent chaining.</summary>
        public Modifier SetTextLine(double tl) { textLine = tl; return this; }

        // ── Spacing ───────────────────────────────────────────────────────────

        /// <summary>Get spacing from the next modifier.</summary>
        public double GetSpacingFromNextModifier() => spacingFromNextModifier;

        /// <summary>Set spacing from the next modifier.</summary>
        public void SetSpacingFromNextModifier(double x) { spacingFromNextModifier = x; }

        // ── Sub-note alignment ────────────────────────────────────────────────

        /// <summary>
        /// Align sub-notes of a NoteSubGroup (or GraceNoteGroup) to the parent note's
        /// absolute x position by setting each sub-note's tick context xOffset.
        ///
        /// Port of VexFlow's Modifier.alignSubNotesWithNote() from modifier.ts lines 218-231.
        /// Logic: compute subNoteXOffset from the parent's tick context metrics, then
        /// call SetXOffset() on each sub-note's tick context.
        /// </summary>
        protected void AlignSubNotesWithNote(List<Note> subNotes, Note parentNote)
        {
            var tickContext = parentNote.GetTickContext();
            if (tickContext == null) return;

            var metrics = tickContext.GetMetrics();
            var stave   = parentNote.GetStave();

            double subNoteXOffset =
                tickContext.GetX()
                - metrics.ModLeftPx
                - metrics.ModRightPx
                + GetSpacingFromNextModifier();

            foreach (var subNote in subNotes)
            {
                var subTickContext = subNote.GetTickContext();
                if (stave != null) subNote.SetStave(stave);
                if (subTickContext != null)
                    subTickContext.SetXOffset(subNoteXOffset);
            }
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Render the modifier. Subclasses must override to provide rendering logic.
        /// Throws VexFlowException if called on the base class.
        /// </summary>
        public override void Draw()
        {
            CheckContext();
            throw new VexFlowException("NotImplemented", "Draw() not implemented for this modifier.");
        }
    }
}

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's ModifierContext class (modifiercontext.ts, 177 lines).
// ModifierContext groups all tickables and modifiers at the same tick position
// to ensure they don't collide during the pre-format pass.
//
// In Phase 3, only StaveNote.Format is a real implementation.
// All other category dispatchers (Dots, Accidentals, GraceNoteGroups, etc.)
// are no-ops that Phase 4 will replace.

using System;
using System.Collections.Generic;
using VexFlowSharp;

namespace VexFlowSharp.Common.Formatting
{
    /// <summary>
    /// State object passed to category formatters during PreFormat.
    /// Each formatter reads and accumulates shifts into this shared object.
    /// Port of VexFlow's ModifierContextState interface from modifiercontext.ts.
    /// </summary>
    public class ModifierContextState
    {
        /// <summary>Accumulated left shift in pixels (space added to the left of notes).</summary>
        public double LeftShift { get; set; } = 0;

        /// <summary>Accumulated right shift in pixels (space added to the right of notes).</summary>
        public double RightShift { get; set; } = 0;

        /// <summary>Current text line below the stave (for articulations, etc.).</summary>
        public double TextLine { get; set; } = 0;

        /// <summary>Current text line above the stave (for annotations, chord symbols, etc.).</summary>
        public double TopTextLine { get; set; } = 0;
    }

    /// <summary>
    /// ModifierContext groups all tickables and modifiers at the same tick position
    /// to resolve collisions before the Formatter runs.
    ///
    /// The key concept: when multiple voices share a stave, StaveNotes at the same
    /// tick may overlap. PreFormat() calls category-specific format functions
    /// (in VexFlow's exact dispatch order) to shift notes left or right as needed.
    ///
    /// Port of VexFlow's ModifierContext class from modifiercontext.ts.
    /// </summary>
    public class ModifierContext
    {
        // ── Member registry ───────────────────────────────────────────────────

        /// <summary>
        /// Dictionary mapping category string to the list of members in that category.
        /// Port of VexFlow's ModifierContext.members field.
        /// </summary>
        private readonly Dictionary<string, List<Element>> members =
            new Dictionary<string, List<Element>>(StringComparer.Ordinal);

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>Accumulated format state from the last PreFormat call.</summary>
        private ModifierContextState state = new ModifierContextState();

        private bool preFormatted = false;
        private bool postFormatted = false;

        /// <summary>Total width consumed by all members after PreFormat (left + right shift).</summary>
        private double width = 0;

        // ── Category dispatchers ──────────────────────────────────────────────

        /// <summary>
        /// Ordered list of category format dispatchers.
        /// Port of VexFlow's preFormat() call sequence from modifiercontext.ts lines 148-163.
        ///
        /// Phase 3: Only StaveNote.Format is a real implementation.
        /// All other entries are no-ops; Phase 4 will replace them with real formatters.
        /// </summary>
        private readonly List<Action<ModifierContextState>> formatters;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a new ModifierContext with category dispatchers registered
        /// in VexFlow's exact dispatch order.
        /// </summary>
        public ModifierContext()
        {
            formatters = new List<Action<ModifierContextState>>
            {
                // 1. StaveNote — real implementation (Phase 3)
                s => StaveNote.Format(GetStaveNotes(), s),

                // 2. Parenthesis — stub (Phase 4)
                _ => { },

                // 3. Dots — real implementation (Phase 4)
                s => Dot.Format(GetModifiers<Dot>(Dot.CATEGORY), s),

                // 4. FretHandFinger — stub (Phase 4)
                _ => { },

                // 5. Accidentals — real implementation (Phase 4)
                s => Accidental.Format(GetModifiers<Accidental>(Accidental.CATEGORY), s),

                // 6. Stroke — stub (Phase 4)
                _ => { },

                // 7. GraceNoteGroups — stub (Phase 4)
                _ => { },

                // 8. NoteSubGroup — real implementation (Phase 5)
                s => NoteSubGroup.Format(GetModifiers<NoteSubGroup>(NoteSubGroup.CATEGORY), s),

                // 9. StringNumber — stub (Phase 4)
                _ => { },

                // 10. Articulation — real implementation (Phase 4, plan 04-02)
                s => Articulation.Format(GetModifiers<Articulation>(Articulation.CATEGORY), s),

                // 11. Ornament — real implementation (Phase 4, plan 04-02)
                s => Ornament.Format(GetModifiers<Ornament>(Ornament.CATEGORY), s),

                // 12. Annotation — real implementation (Phase 4, plan 04-05)
                s => Annotation.Format(GetModifiers<Annotation>(Annotation.CATEGORY), s),

                // 13. ChordSymbol — stub (Phase 4)
                _ => { },

                // 14. Bend — stub (Phase 4)
                _ => { },

                // 15. Vibrato — real implementation (Phase 4, plan 04-05)
                s => Vibrato.Format(GetModifiers<Vibrato>(Vibrato.CATEGORY), s, this),
            };
        }

        // ── Member registry ───────────────────────────────────────────────────

        /// <summary>
        /// Add a member to this context under its category key.
        /// Also calls AddToModifierContext on the member if it is a Tickable.
        /// Port of VexFlow's ModifierContext.addMember() from modifiercontext.ts.
        /// </summary>
        /// <param name="member">Any Element (StaveNote, Modifier, GhostNote, etc.).</param>
        public void AddMember(Element member)
        {
            string category = member.GetCategory();
            if (!members.TryGetValue(category, out var list))
            {
                list = new List<Element>();
                members[category] = list;
            }
            list.Add(member);

            // Wire the back-reference so the member knows its context
            if (member is Tickable tickable)
                tickable.AddToModifierContext(this);

            preFormatted = false;
        }

        /// <summary>
        /// Register a member in this context without calling back into AddToModifierContext.
        /// Used by Tickable.AddToModifierContext to avoid circular callbacks.
        /// Idempotent: won't add the same element twice to the same category list.
        /// </summary>
        internal void RegisterMember(Element member)
        {
            string category = member.GetCategory();
            if (!members.TryGetValue(category, out var list))
            {
                list = new List<Element>();
                members[category] = list;
            }
            if (!list.Contains(member))
                list.Add(member);
            preFormatted = false;
        }

        /// <summary>
        /// Get all members registered under the given category.
        /// Returns an empty list (never null) if the category is unknown.
        /// Port of VexFlow's ModifierContext.getMembers() from modifiercontext.ts.
        /// </summary>
        /// <param name="category">Category string (e.g. StaveNote.CATEGORY).</param>
        public List<Element> GetMembers(string category)
        {
            return members.TryGetValue(category, out var list) ? list : new List<Element>();
        }

        // ── Metrics ───────────────────────────────────────────────────────────

        /// <summary>
        /// Get the total pixel width accumulated by all modifiers.
        /// Valid after PreFormat() has been called.
        /// Port of VexFlow's ModifierContext.getWidth() from modifiercontext.ts.
        /// </summary>
        public double GetWidth() => width;

        /// <summary>Get the left shift accumulated in the last PreFormat pass.</summary>
        public double GetLeftShift() => state.LeftShift;

        /// <summary>Get the right shift accumulated in the last PreFormat pass.</summary>
        public double GetRightShift() => state.RightShift;

        /// <summary>Get the full ModifierContextState from the last PreFormat pass.</summary>
        public ModifierContextState GetState() => state;

        // ── PreFormat ─────────────────────────────────────────────────────────

        /// <summary>
        /// Run the pre-format pass: call each category's format dispatcher in order.
        /// Idempotent — calling multiple times has no effect after the first call.
        ///
        /// Port of VexFlow's ModifierContext.preFormat() from modifiercontext.ts.
        /// </summary>
        public void PreFormat()
        {
            if (preFormatted) return;

            state = new ModifierContextState();

            // Run each category formatter in VexFlow's exact dispatch order.
            foreach (var fmt in formatters)
                fmt(state);

            width = state.LeftShift + state.RightShift;
            preFormatted = true;
        }

        // ── PostFormat ────────────────────────────────────────────────────────

        /// <summary>
        /// Run the post-format pass.
        /// Calls StaveNote.PostFormat for StaveNote category members.
        /// Port of VexFlow's ModifierContext.postFormat() from modifiercontext.ts.
        /// </summary>
        public void PostFormat()
        {
            if (postFormatted) return;

            StaveNote.PostFormat(GetStaveNotes());

            postFormatted = true;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Extract typed members from the member registry.
        /// Returns an empty list if no members of the given category are registered.
        /// </summary>
        private List<T> GetModifiers<T>(string category) where T : Element
        {
            var raw    = GetMembers(category);
            var result = new List<T>(raw.Count);
            foreach (var m in raw)
                if (m is T t) result.Add(t);
            return result;
        }

        /// <summary>
        /// Extract the StaveNote list from the member registry.
        /// Returns an empty list if no StaveNotes have been registered.
        /// </summary>
        private List<StaveNote> GetStaveNotes()
        {
            var raw = GetMembers(StaveNote.CATEGORY);
            var result = new List<StaveNote>(raw.Count);
            foreach (var m in raw)
            {
                if (m is StaveNote sn)
                    result.Add(sn);
            }
            return result;
        }
    }
}

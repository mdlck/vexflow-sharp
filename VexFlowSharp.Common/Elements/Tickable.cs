// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Formatter metrics — space allocation information for a tickable.
    /// Port of VexFlow's FormatterMetrics interface from tickable.ts.
    /// </summary>
    public class FormatterMetrics
    {
        /// <summary>Simplified rational duration string used as a map key.</summary>
        public string Duration { get; set; } = "";

        /// <summary>Freedom to move left without colliding with a neighbor (pixels).</summary>
        public double FreedomLeft { get; set; }

        /// <summary>Freedom to move right without colliding with a neighbor (pixels).</summary>
        public double FreedomRight { get; set; }

        /// <summary>Number of formatting iterations undergone.</summary>
        public int Iterations { get; set; }

        /// <summary>Pixels allocated by this formatter.</summary>
        public double SpaceUsed { get; set; }

        /// <summary>Mean space for tickables of this duration.</summary>
        public double SpaceMean { get; set; }

        /// <summary>Deviation from the mean space.</summary>
        public double SpaceDeviation { get; set; }
    }

    /// <summary>
    /// Abstract base for elements that occupy time on a musical score.
    /// Port of VexFlow's Tickable class from tickable.ts.
    /// </summary>
    public abstract class Tickable : Element
    {
        /// <summary>Whether the formatter should ignore this tickable.</summary>
        protected bool ignoreTicks = false;

        /// <summary>Duration in ticks.</summary>
        protected Fraction ticks;

        /// <summary>Multiplier applied to the intrinsic ticks (e.g., for tuplets).</summary>
        protected Fraction tickMultiplier = new Fraction(1, 1);

        /// <summary>Width in pixels.</summary>
        protected double width = 0;

        /// <summary>Shift from tick context (pixels).</summary>
        protected double xShift = 0;

        /// <summary>Shift from tick context when centre-aligned (pixels).</summary>
        protected double centerXShift = 0;

        /// <summary>Whether this tickable is centre-aligned within its tick context.</summary>
        protected bool alignCenter = false;

        /// <summary>Base tick count before any multiplier is applied.</summary>
        protected int intrinsicTicks = 0;

        /// <summary>Formatter metrics for this tickable.</summary>
        protected FormatterMetrics formatterMetrics;

        /// <summary>
        /// Modifier list. Typed as List&lt;Modifier&gt; as of Phase 2.
        /// </summary>
        protected List<Modifier> modifiers = new List<Modifier>();

        /// <summary>The voice this tickable belongs to (set by Voice.AddTickable).</summary>
        protected Voice? voice;

        /// <summary>The tick context this tickable is aligned within (set by TickContext.AddTickable).</summary>
        protected TickContext? tickContext;

        /// <summary>Whether this tickable should be center-aligned within its tick context.</summary>
        protected bool centerAligned = false;

        /// <summary>The modifier context this tickable is grouped within (set by ModifierContext.AddMember).</summary>
        protected ModifierContext? modifierContext;

        protected Tickable()
        {
            ticks = new Fraction(0, 1);
            formatterMetrics = new FormatterMetrics();
        }

        /// <summary>Get the tick duration of this element.</summary>
        public Fraction GetTicks() => ticks;

        /// <summary>Set the tick duration. Returns this for fluent chaining.</summary>
        public Tickable SetTicks(Fraction t)
        {
            ticks = t;
            return this;
        }

        /// <summary>Get the rendered width in pixels.</summary>
        public double GetWidth() => width;

        /// <summary>Set the rendered width. Returns this for fluent chaining.</summary>
        public Tickable SetWidth(double w)
        {
            width = w;
            return this;
        }

        /// <summary>Get the x shift from the tick context.</summary>
        public double GetXShift() => xShift;

        /// <summary>Set the x shift.</summary>
        public Tickable SetXShift(double x)
        {
            xShift = x;
            return this;
        }

        /// <summary>Whether the formatter should skip this tickable.</summary>
        public bool ShouldIgnoreTicks() => ignoreTicks;

        /// <summary>
        /// Get the intrinsic tick count for this tickable.
        /// Returns the integer tick value of the duration (including dots).
        /// Used by Beam to determine beamability (notes shorter than a quarter note).
        /// Port of Tickable.getIntrinsicTicks() from tickable.ts.
        /// </summary>
        public virtual int GetIntrinsicTicks() => (int)System.Math.Round(ticks.Value());

        /// <summary>
        /// Get the tuplet this tickable belongs to, or null if none.
        /// Port of Tickable.getTuplet() from tickable.ts.
        /// Tuplets are a Phase 4+ concern; returns null for now.
        /// </summary>
        public virtual object? GetTuplet() => null;

        /// <summary>Get formatter metrics for this tickable.</summary>
        public FormatterMetrics GetMetrics() => formatterMetrics;

        /// <summary>Get the tick multiplier.</summary>
        public Fraction GetTickMultiplier() => tickMultiplier;

        /// <summary>Apply a tick multiplier (e.g., for tuplets).</summary>
        public Tickable ApplyTickMultiplier(int numerator, int denominator)
        {
            tickMultiplier = tickMultiplier.Multiply(new Fraction(numerator, denominator));
            return this;
        }

        /// <summary>Get whether this element is centre-aligned.</summary>
        public bool ShouldAlignCenter() => alignCenter;

        /// <summary>Get the centre x shift.</summary>
        public double GetCenterXShift() => centerXShift;

        /// <summary>Get the modifier list for this tickable.</summary>
        public List<Modifier> GetModifiers() => modifiers;

        /// <summary>Add a modifier to this tickable. Returns this for fluent chaining.</summary>
        public Tickable AddModifier(Modifier modifier)
        {
            modifiers.Add(modifier);
            return this;
        }

        // ── Voice wiring ──────────────────────────────────────────────────────

        /// <summary>Get the voice this tickable belongs to.</summary>
        public Voice? GetVoice() => voice;

        /// <summary>Set the associated voice. Returns this for fluent chaining.</summary>
        public Tickable SetVoice(Voice v) { voice = v; return this; }

        // ── TickContext wiring ────────────────────────────────────────────────

        /// <summary>Get the tick context this tickable is aligned within.</summary>
        public TickContext? GetTickContext() => tickContext;

        /// <summary>Set the associated tick context. Returns this for fluent chaining.</summary>
        public Tickable SetTickContext(TickContext tc) { tickContext = tc; return this; }

        // ── Center alignment ──────────────────────────────────────────────────

        /// <summary>Returns true if this tickable should be center-aligned in its tick context.</summary>
        public bool IsCenterAligned() => centerAligned;

        /// <summary>Set center alignment. Returns this for fluent chaining.</summary>
        public Tickable SetCenterAligned(bool align) { centerAligned = align; return this; }

        /// <summary>Set the center x shift.</summary>
        public void SetCenterXShift(double shift) { centerXShift = shift; }

        // ── ModifierContext wiring ─────────────────────────────────────────────

        /// <summary>
        /// Store the ModifierContext reference and register all existing modifiers.
        /// Port of Tickable.addToModifierContext() from tickable.ts.
        /// VexFlow registers all existing modifiers into the context, then adds the note itself.
        /// </summary>
        public virtual void AddToModifierContext(ModifierContext mc)
        {
            modifierContext = mc;
            // Register each modifier already attached to this tickable (one-way — no recursion)
            foreach (var mod in modifiers)
                mc.RegisterMember(mod);
            // Register the tickable itself via one-way registration (avoids circular AddMember loop)
            mc.RegisterMember(this as VexFlowSharp.Element ?? throw new System.InvalidOperationException("Tickable must be an Element"));
        }

        /// <summary>Get the modifier context this tickable belongs to.</summary>
        public ModifierContext? GetModifierContext() => modifierContext;

        // ── Formatter metrics access ──────────────────────────────────────────

        /// <summary>Get the formatter metrics for this tickable.</summary>
        public FormatterMetrics GetFormatterMetrics() => formatterMetrics;

        // ── Pre/Post format ───────────────────────────────────────────────────

        /// <summary>Pre-format this tickable (called by TickContext.PreFormat). Override in subclasses.</summary>
        public virtual void PreFormat() { }

        /// <summary>Post-format this tickable (called after formatting pass). Override in subclasses.</summary>
        public virtual void PostFormat() { }

        // ── Note metrics ──────────────────────────────────────────────────────

        /// <summary>
        /// Get note metrics for this tickable.
        /// Port of Tickable.getMetrics() from tickable.ts.
        /// </summary>
        public virtual NoteMetrics GetNoteMetrics()
        {
            return new NoteMetrics
            {
                Width                 = width,
                GlyphWidth            = 0,
                NotePx                = width,
                ModLeftPx             = 0,
                ModRightPx            = 0,
                LeftDisplacedHeadPx   = 0,
                RightDisplacedHeadPx  = 0,
                GlyphPx               = 0,
            };
        }
    }
}

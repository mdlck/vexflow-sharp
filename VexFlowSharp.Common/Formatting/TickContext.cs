// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's TickContext class (tickcontext.ts, 280 lines).
// TickContext aligns notes at the same tick position across multiple voices.
// It accumulates the maximum metrics from all tickables at a given position.

using System;
using System.Collections.Generic;

namespace VexFlowSharp.Common.Formatting
{
    /// <summary>
    /// Metrics aggregate returned by TickContext.GetMetrics().
    /// Port of VexFlow's TickContextMetrics interface from tickcontext.ts.
    /// </summary>
    public class TickContextMetrics
    {
        /// <summary>Width of the widest note in context (notePx + totalLeftPx + totalRightPx).</summary>
        public double Width { get; set; }

        /// <summary>Width of the widest glyph (note head) in context.</summary>
        public double GlyphPx { get; set; }

        /// <summary>Width of the widest note head + stem.</summary>
        public double NotePx { get; set; }

        /// <summary>Max left displaced head pixels across all tickables.</summary>
        public double LeftDisplacedHeadPx { get; set; }

        /// <summary>Max right displaced head pixels across all tickables.</summary>
        public double RightDisplacedHeadPx { get; set; }

        /// <summary>Max left modifier pixels across all tickables.</summary>
        public double ModLeftPx { get; set; }

        /// <summary>Max right modifier pixels across all tickables.</summary>
        public double ModRightPx { get; set; }

        /// <summary>Total left pixels (modLeftPx + leftDisplacedHeadPx).</summary>
        public double TotalLeftPx { get; set; }

        /// <summary>Total right pixels (modRightPx + rightDisplacedHeadPx).</summary>
        public double TotalRightPx { get; set; }
    }

    /// <summary>
    /// Aligns tickables at the same tick position across multiple voices.
    /// Port of VexFlow's TickContext class from tickcontext.ts.
    /// </summary>
    public class TickContext
    {
        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>All tickables aligned at this tick position.</summary>
        protected readonly List<VexFlowSharp.Tickable> tickables = new List<VexFlowSharp.Tickable>();

        /// <summary>Tickables indexed by voice index (0-based).</summary>
        protected readonly Dictionary<int, VexFlowSharp.Tickable> tickablesByVoice = new Dictionary<int, VexFlowSharp.Tickable>();

        /// <summary>Tick position of this context.</summary>
        protected Fraction currentTick = new Fraction(0, 1);

        /// <summary>Maximum tick count among all tickables.</summary>
        protected Fraction maxTicks = new Fraction(0, 1);

        /// <summary>The absolute X position assigned by the Formatter.</summary>
        protected double x = 0;

        /// <summary>Base X position (before xOffset).</summary>
        protected double xBase = 0;

        /// <summary>Offset from xBase (x = xBase + xOffset).</summary>
        protected double xOffset = 0;

        /// <summary>Padding on each side (width += padding * 2).</summary>
        protected double padding = 1;

        // ── Accumulated metrics ───────────────────────────────────────────────

        protected double notePx              = 0;
        protected double glyphPx             = 0;
        protected double leftDisplacedHeadPx = 0;
        protected double rightDisplacedHeadPx= 0;
        protected double modLeftPx           = 0;
        protected double modRightPx          = 0;
        protected double totalLeftPx         = 0;
        protected double totalRightPx        = 0;
        protected double width               = 0;

        /// <summary>Whether PreFormat has been called.</summary>
        protected bool preFormatted  = false;

        /// <summary>Whether PostFormat has been called.</summary>
        protected bool postFormatted = false;

        /// <summary>
        /// Formatter freedom/position metrics for this context.
        /// Mirrors the FormatterMetrics type used on Tickable, but for the context itself.
        /// </summary>
        protected VexFlowSharp.FormatterMetrics formatterMetrics = new VexFlowSharp.FormatterMetrics();

        /// <summary>
        /// Parent array of all TickContexts in the formatted measure.
        /// The Formatter sets this so GetNextContext() can walk the list.
        /// </summary>
        public List<TickContext> tContexts = new List<TickContext>();

        // ── Formatter metrics ─────────────────────────────────────────────────

        /// <summary>
        /// Get formatter metrics for this context (tracks freedom of movement).
        /// Used by Formatter.Evaluate() and Formatter.Tune() to track gap freedom.
        /// </summary>
        public VexFlowSharp.FormatterMetrics GetFormatterMetrics() => formatterMetrics;

        // ── Static helpers ────────────────────────────────────────────────────

        /// <summary>Return the next TickContext in the parent tContexts list, or null.</summary>
        public static TickContext? GetNextContext(TickContext tc)
        {
            var list  = tc.tContexts;
            int index = list.IndexOf(tc);
            if (index + 1 < list.Count) return list[index + 1];
            return null;
        }

        // ── X positioning ─────────────────────────────────────────────────────

        /// <summary>Get the absolute X position.</summary>
        public double GetX() => x;

        /// <summary>Set X; also resets xBase to x and clears xOffset.</summary>
        public TickContext SetX(double value)
        {
            x       = value;
            xBase   = value;
            xOffset = 0;
            return this;
        }

        /// <summary>Get xBase.</summary>
        public double GetXBase() => xBase;

        /// <summary>Set xBase; x is recalculated as xBase + xOffset.</summary>
        public void SetXBase(double value)
        {
            xBase = value;
            x     = xBase + xOffset;
        }

        /// <summary>Get xOffset.</summary>
        public double GetXOffset() => xOffset;

        /// <summary>Set xOffset; x is recalculated as xBase + xOffset.</summary>
        public void SetXOffset(double value)
        {
            xOffset = value;
            x       = xBase + xOffset;
        }

        // ── Width and padding ─────────────────────────────────────────────────

        /// <summary>Get the total context width including padding on each side.</summary>
        public double GetWidth() => width + padding * 2;

        /// <summary>Set padding.</summary>
        public TickContext SetPadding(double p) { padding = p; return this; }

        // ── Tick fractions ────────────────────────────────────────────────────

        /// <summary>Get the maximum tick count among all tickables.</summary>
        public Fraction GetMaxTicks() => maxTicks;

        /// <summary>Get the current (position) tick.</summary>
        public Fraction GetCurrentTick() => currentTick;

        /// <summary>Set the current tick position and reset preFormatted flag.</summary>
        public void SetCurrentTick(Fraction tick)
        {
            currentTick  = tick;
            preFormatted = false;
        }

        // ── Tickable management ───────────────────────────────────────────────

        /// <summary>Get all tickables in this context.</summary>
        public List<VexFlowSharp.Tickable> GetTickables() => tickables;

        /// <summary>Get tickables dictionary keyed by voice index.</summary>
        public Dictionary<int, VexFlowSharp.Tickable> GetTickablesByVoice() => tickablesByVoice;

        /// <summary>Get the tickable for a specific voice index.</summary>
        public VexFlowSharp.Tickable? GetTickableForVoice(int voiceIndex)
        {
            tickablesByVoice.TryGetValue(voiceIndex, out var t);
            return t;
        }

        /// <summary>Get tickables that are center-aligned.</summary>
        public List<VexFlowSharp.Tickable> GetCenterAlignedTickables()
        {
            var result = new List<VexFlowSharp.Tickable>();
            foreach (var t in tickables)
                if (t.IsCenterAligned()) result.Add(t);
            return result;
        }

        /// <summary>
        /// Add a tickable to this context.
        /// Sets the tickContext back-reference, tracks maxTicks.
        /// Port of TickContext.addTickable() from tickcontext.ts.
        /// </summary>
        public TickContext AddTickable(VexFlowSharp.Tickable tickable, int voiceIndex = 0)
        {
            if (tickable == null)
                throw new VexFlowSharp.VexFlowException("BadArgument", "Invalid tickable added.");

            if (!tickable.ShouldIgnoreTicks())
            {
                var ticks = tickable.GetTicks();
                if (ticks > maxTicks)
                    maxTicks = new Fraction(ticks.Numerator, ticks.Denominator);
            }

            tickable.SetTickContext(this);
            tickables.Add(tickable);
            tickablesByVoice[voiceIndex] = tickable;
            preFormatted = false;
            return this;
        }

        // ── Format passes ─────────────────────────────────────────────────────

        /// <summary>
        /// Pre-format: iterate tickables, call PreFormat() on each, accumulate max metrics.
        /// Port of TickContext.preFormat() from tickcontext.ts.
        /// </summary>
        public TickContext PreFormat()
        {
            if (preFormatted) return this;

            foreach (var tickable in tickables)
            {
                tickable.PreFormat();
                var m = tickable.GetNoteMetrics();

                leftDisplacedHeadPx  = Math.Max(leftDisplacedHeadPx,  m.LeftDisplacedHeadPx);
                rightDisplacedHeadPx = Math.Max(rightDisplacedHeadPx, m.RightDisplacedHeadPx);
                notePx               = Math.Max(notePx,               m.NotePx);
                glyphPx              = Math.Max(glyphPx,              m.GlyphWidth);
                modLeftPx            = Math.Max(modLeftPx,            m.ModLeftPx);
                modRightPx           = Math.Max(modRightPx,           m.ModRightPx);
                totalLeftPx          = Math.Max(totalLeftPx,          m.ModLeftPx + m.LeftDisplacedHeadPx);
                totalRightPx         = Math.Max(totalRightPx,         m.ModRightPx + m.RightDisplacedHeadPx);

                width = notePx + totalLeftPx + totalRightPx;
            }

            preFormatted = true;
            return this;
        }

        /// <summary>
        /// Post-format pass: mark as post-formatted.
        /// Port of TickContext.postFormat() from tickcontext.ts.
        /// </summary>
        public TickContext PostFormat()
        {
            if (postFormatted) return this;
            postFormatted = true;
            return this;
        }

        // ── Metrics snapshot ──────────────────────────────────────────────────

        /// <summary>
        /// Get a snapshot of the accumulated metrics.
        /// Port of TickContext.getMetrics() from tickcontext.ts.
        /// </summary>
        public TickContextMetrics GetMetrics()
        {
            return new TickContextMetrics
            {
                Width                = width,
                GlyphPx              = glyphPx,
                NotePx               = notePx,
                LeftDisplacedHeadPx  = leftDisplacedHeadPx,
                RightDisplacedHeadPx = rightDisplacedHeadPx,
                ModLeftPx            = modLeftPx,
                ModRightPx           = modRightPx,
                TotalLeftPx          = totalLeftPx,
                TotalRightPx         = totalRightPx,
            };
        }
    }
}

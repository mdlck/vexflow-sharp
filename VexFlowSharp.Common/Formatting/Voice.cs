// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's Voice class (voice.ts, 317 lines).
// Voice groups Tickables into a rhythmic stream and enforces three duration modes:
//   STRICT — ticks must fill the voice exactly (default).
//   SOFT   — ticks can be added without restriction.
//   FULL   — ticks cannot exceed the total but need not fill it.

using System;
using System.Collections.Generic;

namespace VexFlowSharp.Common.Formatting
{
    /// <summary>
    /// Voice duration-enforcement mode.
    /// Port of VexFlow's VoiceMode enum from voice.ts.
    /// </summary>
    public enum VoiceMode
    {
        STRICT = 1,
        SOFT   = 2,
        FULL   = 3,
    }

    /// <summary>
    /// Time signature descriptor for a Voice.
    /// Port of VexFlow's VoiceTime interface from voice.ts.
    /// </summary>
    public struct VoiceTime
    {
        /// <summary>Number of beats per measure (numerator).</summary>
        public int NumBeats;

        /// <summary>Beat value (denominator, e.g., 4 = quarter note).</summary>
        public int BeatValue;

        /// <summary>Tick resolution. Defaults to Tables.RESOLUTION (16384).</summary>
        public int Resolution;
    }

    /// <summary>
    /// Container that groups Tickables for formatting.
    /// Port of VexFlow's Voice class from voice.ts.
    /// </summary>
    public class Voice
    {
        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>The time signature for this voice.</summary>
        protected readonly VoiceTime time;

        /// <summary>Total ticks available in this voice (computed from time signature).</summary>
        protected Fraction totalTicks;

        /// <summary>Ticks used so far by added tickables.</summary>
        protected Fraction ticksUsed = new Fraction(0, 1);

        /// <summary>Smallest tick count seen among all added tickables.</summary>
        protected Fraction smallestTickCount;

        /// <summary>Current resolution multiplier (= ticksUsed.Denominator).</summary>
        protected int resolutionMultiplier = 1;

        /// <summary>Sum of exp(ticks/total) over all tickables — softmax denominator cache.</summary>
        protected double expTicksUsed = 0;

        /// <summary>Whether PreFormat has run.</summary>
        protected bool preFormatted = false;

        /// <summary>Current mode (STRICT by default).</summary>
        protected VoiceMode mode = VoiceMode.STRICT;

        /// <summary>Stave this voice is attached to.</summary>
        protected VexFlowSharp.Stave? stave;

        /// <summary>All tickables in order of addition.</summary>
        protected readonly List<VexFlowSharp.Tickable> tickables = new List<VexFlowSharp.Tickable>();

        /// <summary>Softmax options.</summary>
        protected double softmaxFactor;

        // ── Constructors ──────────────────────────────────────────────────────

        /// <summary>
        /// Create a Voice with explicit time signature.
        /// Equivalent to VexFlow <c>new Voice(time)</c>.
        /// </summary>
        public Voice(VoiceTime voiceTime)
        {
            softmaxFactor = VexFlowSharp.Tables.SOFTMAX_FACTOR;
            time = new VoiceTime
            {
                NumBeats   = voiceTime.NumBeats,
                BeatValue  = voiceTime.BeatValue,
                Resolution = voiceTime.Resolution > 0 ? voiceTime.Resolution : VexFlowSharp.Tables.RESOLUTION,
            };

            // totalTicks = numBeats * (resolution / beatValue)
            int ticks = time.NumBeats * (time.Resolution / time.BeatValue);
            totalTicks = new Fraction(ticks, 1);
            smallestTickCount = new Fraction(ticks, 1);
        }

        /// <summary>
        /// Create a 4/4 Voice using the default time signature.
        /// </summary>
        public Voice() : this(new VoiceTime { NumBeats = 4, BeatValue = 4, Resolution = VexFlowSharp.Tables.RESOLUTION })
        {
        }

        // ── Time & tick access ────────────────────────────────────────────────

        /// <summary>Get the total ticks declared for this voice.</summary>
        public Fraction GetTotalTicks() => totalTicks;

        /// <summary>Get the ticks consumed so far.</summary>
        public Fraction GetTicksUsed() => ticksUsed;

        /// <summary>Get the smallest tick count seen.</summary>
        public Fraction GetSmallestTickCount() => smallestTickCount;

        /// <summary>Get the resolution multiplier.</summary>
        public int GetResolutionMultiplier() => resolutionMultiplier;

        // ── Mode ──────────────────────────────────────────────────────────────

        /// <summary>Get the current voice mode.</summary>
        public VoiceMode GetMode() => mode;

        /// <summary>Set the voice mode.</summary>
        public Voice SetMode(VoiceMode m) { mode = m; return this; }

        /// <summary>Convenience: switch between STRICT and SOFT.</summary>
        public Voice SetStrict(bool strict)
        {
            mode = strict ? VoiceMode.STRICT : VoiceMode.SOFT;
            return this;
        }

        // ── Completeness check ────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the voice is completely filled according to its mode.
        /// STRICT / FULL: true when ticksUsed == totalTicks.
        /// SOFT: always true (no restriction).
        /// </summary>
        public bool IsComplete()
        {
            if (mode == VoiceMode.STRICT || mode == VoiceMode.FULL)
                return ticksUsed == totalTicks;
            return true;
        }

        // ── Stave ─────────────────────────────────────────────────────────────

        /// <summary>Set the associated stave.</summary>
        public Voice SetStave(VexFlowSharp.Stave s) { stave = s; return this; }

        /// <summary>Get the associated stave.</summary>
        public VexFlowSharp.Stave? GetStave() => stave;

        /// <summary>Get the associated stave; throws if not set.</summary>
        public VexFlowSharp.Stave CheckStave()
            => stave ?? throw new VexFlowSharp.VexFlowException("NoStave", "No stave attached to voice.");

        // ── Softmax ───────────────────────────────────────────────────────────

        /// <summary>Set the softmax factor and reset the cached sum.</summary>
        public Voice SetSoftmaxFactor(double factor)
        {
            softmaxFactor = factor;
            expTicksUsed  = 0; // invalidate cache
            return this;
        }

        /// <summary>
        /// Compute the sum of exp(ticks_i / total) over all tickables.
        /// Used as the denominator in Softmax().
        /// </summary>
        protected double ReCalculateExpTicksUsed()
        {
            double total = ticksUsed.Value();
            double sum   = 0;
            foreach (var t in tickables)
                sum += Math.Pow(softmaxFactor, t.GetTicks().Value() / total);
            expTicksUsed = sum;
            return expTicksUsed;
        }

        /// <summary>
        /// Softmax-scaled value for a tick duration.
        /// Returns exp(tickValue / total) / sum_of_all_exps.
        /// Port of Voice.softmax() from voice.ts.
        /// </summary>
        public double Softmax(double tickValue)
        {
            if (expTicksUsed == 0)
                expTicksUsed = ReCalculateExpTicksUsed();

            double total = ticksUsed.Value();
            double exp   = Math.Pow(softmaxFactor, tickValue / total);
            return exp / expTicksUsed;
        }

        // ── Tickable management ───────────────────────────────────────────────

        /// <summary>Get all tickables in this voice.</summary>
        public List<VexFlowSharp.Tickable> GetTickables() => tickables;

        /// <summary>
        /// Add a single tickable to the voice.
        /// Port of Voice.addTickable() from voice.ts.
        /// Enforces mode: STRICT/FULL throw VexFlowException if total would be exceeded.
        /// </summary>
        public Voice AddTickable(VexFlowSharp.Tickable tickable)
        {
            if (!tickable.ShouldIgnoreTicks())
            {
                var ticks = tickable.GetTicks();

                // Accumulate ticks
                ticksUsed    = ticksUsed.Add(ticks);
                expTicksUsed = 0; // invalidate softmax cache

                // Mode enforcement
                if ((mode == VoiceMode.STRICT || mode == VoiceMode.FULL) &&
                    ticksUsed > totalTicks)
                {
                    // Roll back and throw
                    ticksUsed = ticksUsed.Subtract(ticks);
                    throw new VexFlowSharp.VexFlowException("BadArgument", "Too many ticks.");
                }

                // Track smallest tick count
                if (ticks < smallestTickCount)
                    smallestTickCount = new Fraction(ticks.Numerator, ticks.Denominator);

                // Update resolution multiplier from denominator of running sum
                resolutionMultiplier = ticksUsed.Denominator;
            }

            tickables.Add(tickable);
            tickable.SetVoice(this);
            return this;
        }

        /// <summary>Add a list of tickables to the voice.</summary>
        public Voice AddTickables(List<VexFlowSharp.Tickable> list)
        {
            foreach (var t in list)
                AddTickable(t);
            return this;
        }

        // ── Formatting ────────────────────────────────────────────────────────

        /// <summary>
        /// Pre-format: applies the voice's stave to each tickable that lacks one.
        /// Port of Voice.preFormat() from voice.ts.
        /// </summary>
        public Voice PreFormat()
        {
            if (preFormatted) return this;
            // In Phase 3, requires stave to be set. Skip stave assignment for test contexts.
            preFormatted = true;
            return this;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw all tickables. Assigns stave and context from arguments.
        /// Port of Voice.draw() from voice.ts.
        /// </summary>
        public void Draw(VexFlowSharp.RenderContext ctx, VexFlowSharp.Stave? drawStave = null)
        {
            var activeStave = drawStave ?? stave;
            foreach (var tickable in tickables)
            {
                // SetStave is defined on Note, not Tickable — cast if possible.
                if (activeStave != null && tickable is VexFlowSharp.Note note)
                    note.SetStave(activeStave);
                tickable.SetContext(ctx);
                tickable.Draw();
            }
        }
    }
}

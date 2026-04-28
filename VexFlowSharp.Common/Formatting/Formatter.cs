#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's Formatter class (formatter.ts, 1108 lines).
// The Formatter is the central layout engine that assigns X positions to notes.
// It creates TickContexts and ModifierContexts from voices, then runs a
// softmax-based justification algorithm to distribute notes proportionally.

using System;
using System.Collections.Generic;
using System.Linq;
using VexFlowSharp.Common.Elements;

namespace VexFlowSharp.Common.Formatting
{
    /// <summary>
    /// Options for configuring the Formatter's spacing algorithm.
    /// Port of VexFlow's FormatterOptions interface from formatter.ts.
    /// </summary>
    public class FormatterOptions
    {
        /// <summary>Softmax factor for proportional spacing. Defaults to Tables.SOFTMAX_FACTOR (10).</summary>
        public double SoftmaxFactor { get; set; } = Metrics.GetDouble("Formatter.softmaxFactor");

        /// <summary>Use global softmax across all staves. Defaults to false.</summary>
        public bool GlobalSoftmax { get; set; } = false;

        /// <summary>Maximum justify iterations. Defaults to 5.</summary>
        public int MaxIterations { get; set; } = (int)Metrics.GetDouble("Formatter.maxIterations");
    }

    /// <summary>
    /// Parameters for FormatAndDraw and related methods.
    /// Port of VexFlow's FormatParams interface from formatter.ts.
    /// </summary>
    public class FormatParams
    {
        /// <summary>Align rests with adjacent notes. Defaults to false.</summary>
        public bool AlignRests { get; set; } = false;

        /// <summary>Automatically beam notes. Defaults to false. (Requires plan 03-04 Beam class.)</summary>
        public bool AutoBeam { get; set; } = false;

        /// <summary>Stave to use for FormatToStave justification.</summary>
        public VexFlowSharp.Stave? Stave { get; set; }
    }

    /// <summary>
    /// Alignment context data for tick or modifier contexts.
    /// Port of VexFlow's AlignmentContexts interface from formatter.ts.
    /// </summary>
    public class AlignmentContexts
    {
        /// <summary>Sorted list of integer tick positions.</summary>
        public List<long> List { get; set; } = new List<long>();

        /// <summary>Map from integer tick position to TickContext.</summary>
        public Dictionary<long, TickContext> Map { get; set; } = new Dictionary<long, TickContext>();

        /// <summary>All TickContexts in order of creation.</summary>
        public List<TickContext> Array { get; set; } = new List<TickContext>();

        /// <summary>Resolution multiplier (LCM of all voice resolution multipliers).</summary>
        public int ResolutionMultiplier { get; set; } = 0;
    }

    /// <summary>
    /// The Formatter is the heart of VexFlow's layout engine.
    ///
    /// It creates TickContexts and ModifierContexts from voices, runs a multi-iteration
    /// softmax justification loop, and assigns final x-positions to all notes.
    ///
    /// Port of VexFlow's Formatter class from formatter.ts.
    /// </summary>
    public class Formatter
    {
        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>Whether minTotalWidth has been calculated.</summary>
        protected bool hasMinTotalWidth;

        /// <summary>Minimum total width required to render all voices.</summary>
        protected double minTotalWidth;

        /// <summary>Width to justify the voices to.</summary>
        protected double justifyWidth;

        /// <summary>Total cost of the current formatting (sum of squared deviations).</summary>
        protected double totalCost;

        /// <summary>Total shift applied during the last tune() call.</summary>
        protected double totalShift;

        /// <summary>Tick contexts for all voices.</summary>
        protected AlignmentContexts tickContexts;

        /// <summary>Modifier contexts for all joined voices.</summary>
        protected readonly List<List<ModifierContext>> modifierContexts;

        /// <summary>All voices being formatted.</summary>
        protected List<Voice> voices;

        /// <summary>History of total costs over formatting iterations.</summary>
        protected readonly List<double> lossHistory;

        /// <summary>Gap tracking for debug/evaluate.</summary>
        protected double contextGapsTotal;

        /// <summary>Formatter options (softmax factor, iterations, etc.).</summary>
        protected readonly FormatterOptions formatterOptions;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a new Formatter with optional options.
        /// Port of Formatter constructor from formatter.ts.
        /// </summary>
        public Formatter(FormatterOptions? options = null)
        {
            formatterOptions = options ?? new FormatterOptions();
            justifyWidth     = 0;
            totalCost        = 0;
            totalShift       = 0;
            minTotalWidth    = 0;
            hasMinTotalWidth = false;
            contextGapsTotal = 0;

            tickContexts = new AlignmentContexts();
            modifierContexts = new List<List<ModifierContext>>();
            voices       = new List<Voice>();
            lossHistory  = new List<double>();
        }

        // ── Static helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Get the resolution multiplier for a list of voices.
        /// Uses LCM of all voice resolution multipliers.
        /// Port of Formatter.getResolutionMultiplier() from formatter.ts.
        /// </summary>
        public static int GetResolutionMultiplier(List<Voice> voices)
        {
            if (voices == null || voices.Count == 0)
                throw new VexFlowSharp.VexFlowException("BadArgument", "No voices to format");

            int resolutionMultiplier = 1;
            foreach (var voice in voices)
            {
                int rm = voice.GetResolutionMultiplier();
                resolutionMultiplier = Math.Max(resolutionMultiplier,
                    Fraction.LCM(resolutionMultiplier, rm));
            }
            return resolutionMultiplier;
        }

        /// <summary>
        /// Simple left-to-right layout without full justify. Useful for tests and debugging.
        /// Port of Formatter.SimpleFormat() from formatter.ts.
        /// </summary>
        public static void SimpleFormat(List<VexFlowSharp.Tickable> notes, double x = 0,
            double paddingBetween = 10)
        {
            double accumulator = x;
            foreach (var note in notes)
            {
                note.AddToModifierContext(new ModifierContext());
                var tick = new TickContext();
                tick.AddTickable(note);
                tick.PreFormat();
                var metrics = tick.GetMetrics();
                tick.SetX(accumulator + metrics.TotalLeftPx);
                accumulator += tick.GetWidth() + metrics.TotalRightPx + paddingBetween;
            }
        }

        /// <summary>
        /// Helper to format and draw a single voice onto a stave.
        /// Port of Formatter.FormatAndDraw() from formatter.ts.
        /// </summary>
        public static VexFlowSharp.BoundingBox? FormatAndDraw(
            VexFlowSharp.RenderContext ctx,
            VexFlowSharp.Stave stave,
            List<VexFlowSharp.StemmableNote> notes,
            FormatParams? p = null)
        {
            var opts = p ?? new FormatParams();

            // Create a SOFT voice and add all notes
            var voice = new Voice();
            voice.SetMode(VoiceMode.SOFT);
            foreach (var n in notes)
                voice.AddTickable(n);

            // Generate auto-beams before formatting (stem directions may change)
            var beams = opts.AutoBeam
                ? Beam.ApplyAndGetBeams(voice)
                : new List<Beam>();

            // Format and draw
            new Formatter()
                .JoinVoices(new List<Voice> { voice })
                .FormatToStave(new List<Voice> { voice }, stave,
                    new FormatParams { AlignRests = opts.AlignRests, Stave = stave });

            voice.SetStave(stave).Draw(ctx, stave);

            // Draw beams after voice (notes must have stave y-values before PostFormat)
            foreach (var beam in beams)
            {
                beam.SetContext(ctx);
                beam.Draw();
            }

            return voice.GetBoundingBox();
        }

        /// <summary>
        /// Convenience overload matching VexFlow's boolean autoBeam parameter.
        /// </summary>
        public static VexFlowSharp.BoundingBox? FormatAndDraw(
            VexFlowSharp.RenderContext ctx,
            VexFlowSharp.Stave stave,
            List<VexFlowSharp.StemmableNote> notes,
            bool autoBeam)
            => FormatAndDraw(ctx, stave, notes, new FormatParams { AutoBeam = autoBeam });

        // ── Context creation ──────────────────────────────────────────────────

        /// <summary>
        /// Create a TickContext for each unique tick position across all voices.
        /// Port of Formatter.createTickContexts() from formatter.ts.
        ///
        /// Uses long keys (Pitfall 7) to avoid int overflow with complex rhythms.
        /// </summary>
        public AlignmentContexts CreateTickContexts(List<Voice> inputVoices)
        {
            var result = CreateContexts(inputVoices);
            tickContexts = result;

            // Wire tContexts back-reference so GetNextContext() works
            var contextArray = tickContexts.Array;
            foreach (var ctx in contextArray)
                ctx.tContexts = contextArray;

            return result;
        }

        /// <summary>
        /// Internal: create tick contexts from voices.
        /// Port of the createContexts() free function from formatter.ts.
        /// </summary>
        private static AlignmentContexts CreateContexts(List<Voice> inputVoices)
        {
            var result = new AlignmentContexts();

            if (inputVoices == null || inputVoices.Count == 0)
                return result;

            int resolutionMultiplier = GetResolutionMultiplier(inputVoices);
            result.ResolutionMultiplier = resolutionMultiplier;

            // Dictionary<long, TickContext> — use long keys to avoid int overflow (Pitfall 7)
            var tickToContextMap = new Dictionary<long, TickContext>();
            var tickList = new List<long>();
            var contextArray = new List<TickContext>();

            for (int voiceIndex = 0; voiceIndex < inputVoices.Count; voiceIndex++)
            {
                var voice = inputVoices[voiceIndex];

                // ticksUsed fraction uses resolutionMultiplier as denominator
                // so numerator == integerTicks directly (no further expansion needed)
                var ticksUsed = new Fraction(0, resolutionMultiplier);

                foreach (var tickable in voice.GetTickables())
                {
                    long integerTicks = ticksUsed.Numerator;

                    if (!tickToContextMap.ContainsKey(integerTicks))
                    {
                        var newContext = new TickContext();
                        contextArray.Add(newContext);
                        tickToContextMap[integerTicks] = newContext;
                        tickList.Add(integerTicks);
                    }

                    // Add tickable to the TickContext for this position
                    tickToContextMap[integerTicks].AddTickable(tickable, voiceIndex);

                    // Advance ticksUsed by this tickable's duration
                    ticksUsed = ticksUsed.Add(tickable.GetTicks());
                }
            }

            // Sort tick positions
            tickList.Sort();

            result.List  = tickList;
            result.Map   = tickToContextMap;
            result.Array = contextArray;

            return result;
        }

        /// <summary>
        /// Create a ModifierContext for each unique tick position per stave.
        /// Port of Formatter.createModifierContexts() from formatter.ts.
        /// </summary>
        public void CreateModifierContexts(List<Voice> inputVoices)
        {
            if (inputVoices == null || inputVoices.Count == 0) return;

            int resolutionMultiplier = GetResolutionMultiplier(inputVoices);

            // Map from stave-index → (tick → ModifierContext)
            // Stave is keyed by object identity index to avoid null-key issues in Dictionary.
            // null stave (no stave assigned) maps to index -1.
            var staveIndex    = new Dictionary<int, Dictionary<long, ModifierContext>>();
            var staveRegistry = new List<VexFlowSharp.Stave>();
            var contexts      = new List<ModifierContext>();

            int GetStaveId(VexFlowSharp.Stave? stave)
            {
                if (stave == null) return -1;
                int idx = staveRegistry.IndexOf(stave);
                if (idx < 0)
                {
                    staveRegistry.Add(stave);
                    idx = staveRegistry.Count - 1;
                }
                return idx;
            }

            foreach (var voice in inputVoices)
            {
                var ticksUsed = new Fraction(0, resolutionMultiplier);

                foreach (var tickable in voice.GetTickables())
                {
                    long integerTicks = ticksUsed.Numerator;

                    // Get the stave for this tickable (null if not set)
                    VexFlowSharp.Stave? tickableStave = null;
                    if (tickable is VexFlowSharp.Note note)
                        tickableStave = note.GetStave();

                    int sid = GetStaveId(tickableStave);
                    if (!staveIndex.ContainsKey(sid))
                        staveIndex[sid] = new Dictionary<long, ModifierContext>();

                    var staveTickMap = staveIndex[sid];

                    if (!staveTickMap.ContainsKey(integerTicks))
                    {
                        var newContext = new ModifierContext();
                        contexts.Add(newContext);
                        staveTickMap[integerTicks] = newContext;
                    }

                    tickable.AddToModifierContext(staveTickMap[integerTicks]);

                    ticksUsed = ticksUsed.Add(tickable.GetTicks());
                }
            }

            modifierContexts.Add(contexts);
        }

        // ── Format passes ─────────────────────────────────────────────────────

        /// <summary>
        /// Pre-format: assign initial X positions based on minimum widths.
        /// Port of Formatter.preFormat() from formatter.ts.
        /// </summary>
        public double PreFormat(double justifyWidth = 0, List<Voice>? voicesParam = null,
            VexFlowSharp.Stave? stave = null)
        {
            var contexts = tickContexts;

            lossHistory.Clear();

            // If voices and stave were provided, set stave and preformat voices
            if (voicesParam != null && stave != null)
            {
                foreach (var v in voicesParam)
                {
                    v.SetStave(stave);
                    v.PreFormat();
                }
            }

            var contextList = contexts.List;
            var contextMap  = contexts.Map;

            double x = 0;
            double shift = 0;
            this.minTotalWidth = 0;
            double totalTicks = 0;

            // Pass 1: give each context the maximum width it requests
            foreach (var tick in contextList)
            {
                var context = contextMap[tick];
                context.PreFormat();

                double width = context.GetWidth();
                this.minTotalWidth += width;

                double maxTicks = context.GetMaxTicks().Value();
                totalTicks += maxTicks;

                var metrics = context.GetMetrics();
                x = x + shift + metrics.TotalLeftPx;
                context.SetX(x);

                shift = width - metrics.TotalLeftPx;
            }

            this.minTotalWidth = x + shift;
            this.hasMinTotalWidth = true;

            // No justification needed
            if (justifyWidth <= 0) return Evaluate();

            if (contextList.Count == 0) return 0;

            var firstContext = contextMap[contextList[0]];
            var lastContext  = contextMap[contextList[contextList.Count - 1]];

            // Compute softmax denominator for global softmax mode
            double softmaxFactor = formatterOptions.SoftmaxFactor;
            double expTicksUsed = 0;
            foreach (var tick in contextList)
                expTicksUsed += Math.Pow(softmaxFactor,
                    contextMap[tick].GetMaxTicks().Value() / totalTicks);

            bool globalSoftmax = formatterOptions.GlobalSoftmax;

            // Adjusted justify width: subtract the last note's right metrics and first note's left metrics
            var lastMetrics  = lastContext.GetMetrics();
            var firstMetrics = firstContext.GetMetrics();
            double adjustedJustifyWidth = Math.Round(
                justifyWidth - lastMetrics.NotePx - lastMetrics.TotalRightPx - firstMetrics.TotalLeftPx,
                6, MidpointRounding.AwayFromZero);

            // Closure-captured arrays for intermediate values — declared before local functions
            double[] lastMaxNegShifts  = new double[contextList.Count];
            VexFlowSharp.Tickable?[] lastBackTickables = new VexFlowSharp.Tickable?[contextList.Count];

            // Calculate ideal distances between adjacent tick contexts
            double[] CalculateIdealDistances(double targetWidth)
            {
                var distances = new double[contextList.Count];
                var maxNegShifts = new double[contextList.Count];
                var backTickables = new VexFlowSharp.Tickable?[contextList.Count];

                for (int i = 0; i < contextList.Count; i++)
                {
                    if (i == 0)
                    {
                        distances[i] = 0;
                        maxNegShifts[i] = 0;
                        continue;
                    }

                    long tick = contextList[i];
                    var context = contextMap[tick];
                    var contextVoices = context.GetTickablesByVoice();

                    double expectedDistance = 0;
                    double maxNegativeShiftPx = double.PositiveInfinity;
                    VexFlowSharp.Tickable? backTickable = null;

                    // Search backwards for a matching voice in a previous context
                    for (int j = i - 1; j >= 0; j--)
                    {
                        long backTick = contextList[j];
                        var backContext = contextMap[backTick];
                        var backVoices = backContext.GetTickablesByVoice();

                        // Find matching voices between current and back context
                        var matchingVoices = new List<int>();
                        foreach (var v in contextVoices.Keys)
                        {
                            if (backVoices.ContainsKey(v))
                                matchingVoices.Add(v);
                        }

                        if (matchingVoices.Count > 0)
                        {
                            double maxTicks = 0;

                            foreach (int v in matchingVoices)
                            {
                                double ticks = backVoices[v].GetTicks().Value();
                                if (ticks > maxTicks)
                                {
                                    backTickable = backVoices[v];
                                    maxTicks = ticks;
                                }

                                // Calculate collision limit
                                var thisTickable = contextVoices[v];
                                var thisMet = thisTickable.GetNoteMetrics();
                                double insideLeftEdge = thisTickable.GetTickContext()!.GetX()
                                    - (thisMet.ModLeftPx + thisMet.LeftDisplacedHeadPx);

                                var backMet = backVoices[v].GetNoteMetrics();
                                double insideRightEdge = backVoices[v].GetTickContext()!.GetX()
                                    + backMet.NotePx + backMet.ModRightPx + backMet.RightDisplacedHeadPx;

                                maxNegativeShiftPx = Math.Min(maxNegativeShiftPx,
                                    insideLeftEdge - insideRightEdge);
                            }

                            // Don't shift further left than 5% from prev context
                            var prevContext = contextMap[contextList[i - 1]];
                            maxNegativeShiftPx = Math.Min(maxNegativeShiftPx,
                                context.GetX() - (prevContext.GetX() + targetWidth * 0.05));

                            if (globalSoftmax)
                            {
                                double t = totalTicks;
                                expectedDistance = (Math.Pow(softmaxFactor, maxTicks / t)
                                    / expTicksUsed) * targetWidth;
                            }
                            else if (backTickable != null)
                            {
                                var v2 = backTickable.GetVoice();
                                if (v2 != null)
                                    expectedDistance = v2.Softmax(maxTicks) * targetWidth;
                            }

                            backTickables[i] = backTickable;
                            distances[i] = expectedDistance;
                            maxNegShifts[i] = double.IsInfinity(maxNegativeShiftPx) ? 0 : maxNegativeShiftPx;
                            break;
                        }
                    }

                    if (backTickable == null)
                    {
                        distances[i] = 0;
                        maxNegShifts[i] = 0;
                    }
                }

                // Store intermediate data in closure-captured arrays for ShiftToIdealDistances
                Array.Copy(maxNegShifts,  lastMaxNegShifts,  maxNegShifts.Length);
                Array.Copy(backTickables, lastBackTickables, backTickables.Length);

                return distances;
            }

            double ShiftToIdealDistances(double[] idealDistances)
            {
                double centerX = adjustedJustifyWidth / 2.0;
                double spaceAccum = 0;

                for (int index = 0; index < contextList.Count; index++)
                {
                    long tick = contextList[index];
                    var context = contextMap[tick];

                    if (index > 0)
                    {
                        double contextX = context.GetX();
                        double expectedDist = idealDistances[index];
                        var fromTickable = lastBackTickables[index];

                        double errorPx = 0;
                        if (fromTickable != null)
                        {
                            var fromTc = fromTickable.GetTickContext();
                            if (fromTc != null)
                                errorPx = fromTc.GetX() + expectedDist - (contextX + spaceAccum);
                        }

                        double negativeShiftPx = 0;
                        if (errorPx > 0)
                        {
                            spaceAccum += errorPx;
                        }
                        else if (errorPx < 0)
                        {
                            negativeShiftPx = Math.Min(lastMaxNegShifts[index], Math.Abs(errorPx));
                            spaceAccum += -negativeShiftPx;
                        }

                        context.SetX(contextX + spaceAccum);
                    }

                    // Center-aligned tickables
                    foreach (var tickable in context.GetCenterAlignedTickables())
                        tickable.SetCenterXShift(centerX - context.GetX());
                }

                return lastContext.GetX() - firstContext.GetX();
            }

            double targetWidth = adjustedJustifyWidth;
            double[] distances = CalculateIdealDistances(targetWidth);
            double actualWidth = ShiftToIdealDistances(distances);

            if (contextList.Count == 1) return 0;

            // Determine padding bounds from common metrics.
            double configMinPadding = Metrics.GetDouble("Stave.endPaddingMin");
            double configMaxPadding = Metrics.GetDouble("Stave.endPaddingMax");
            double leftPadding      = Metrics.GetDouble("Stave.padding");

            // Calculate minimum distance between contexts
            double CalcMinDistance(double tW, double[] dists)
            {
                double mdCalc = tW / 2.0;
                for (int di = 1; di < dists.Length; di++)
                    mdCalc = Math.Min(dists[di] > 0 ? dists[di] / 2.0 : mdCalc, mdCalc);
                return mdCalc;
            }

            double minDistance = CalcMinDistance(targetWidth, distances);

            double PaddingMaxCalc(double curTargetWidth)
            {
                double lastTickablePadding = 0;
                var lastTickable = lastContext.GetTickables().Count > 0
                    ? lastContext.GetTickables()[0]
                    : null;

                if (lastTickable != null)
                {
                    var v = lastTickable.GetVoice();
                    if (v != null)
                    {
                        if (v.GetTicksUsed().Value() > v.GetTotalTicks().Value())
                            return configMaxPadding * 2 < minDistance ? minDistance : configMaxPadding;

                        double tickWidth = lastTickable.GetWidth();
                        lastTickablePadding = v.Softmax(lastContext.GetMaxTicks().Value())
                            * curTargetWidth - (tickWidth + leftPadding);
                    }
                }

                return configMaxPadding * 2 < lastTickablePadding ? lastTickablePadding : configMaxPadding;
            }

            double paddingMax = PaddingMaxCalc(targetWidth);
            double paddingMin = paddingMax - (configMaxPadding - configMinPadding);
            double maxX = adjustedJustifyWidth - paddingMin;

            int iterations = formatterOptions.MaxIterations;
            while ((actualWidth > maxX && iterations > 0) ||
                   (actualWidth + paddingMax < maxX && iterations > 1))
            {
                targetWidth -= actualWidth - maxX;
                paddingMax = PaddingMaxCalc(targetWidth);
                paddingMin = paddingMax - (configMaxPadding - configMinPadding);
                distances  = CalculateIdealDistances(targetWidth);
                actualWidth = ShiftToIdealDistances(distances);
                iterations--;
            }

            this.justifyWidth = justifyWidth;
            return Evaluate();
        }

        /// <summary>
        /// Post-format all modifier and tick contexts.
        /// Port of Formatter.postFormat() from formatter.ts.
        /// </summary>
        public Formatter PostFormat()
        {
            foreach (var mcList in modifierContexts)
                foreach (var mc in mcList)
                    mc.PostFormat();

            foreach (var tick in tickContexts.List)
                tickContexts.Map[tick].PostFormat();

            return this;
        }

        /// <summary>
        /// Evaluate the current formatting cost (sum of squared deviations from mean).
        /// Port of Formatter.evaluate() from formatter.ts.
        /// </summary>
        public double Evaluate()
        {
            var contexts = tickContexts;
            contextGapsTotal = 0;

            // Calculate gaps between adjacent tick contexts
            for (int index = 1; index < contexts.List.Count; index++)
            {
                long prevTick = contexts.List[index - 1];
                long tick     = contexts.List[index];
                var prevContext = contexts.Map[prevTick];
                var context     = contexts.Map[tick];

                var prevMet = prevContext.GetMetrics();
                var currMet = context.GetMetrics();

                double insideRightEdge = prevContext.GetX() + prevMet.NotePx + prevMet.TotalRightPx;
                double insideLeftEdge  = context.GetX() - currMet.TotalLeftPx;
                double gap = insideLeftEdge - insideRightEdge;

                contextGapsTotal += gap;

                // Update freedom metrics
                var contextFm = context.GetFormatterMetrics();
                var prevFm    = prevContext.GetFormatterMetrics();
                contextFm.FreedomLeft = gap;
                prevFm.FreedomRight   = gap;
            }

            // Compute duration stats (mean space used per duration type)
            var durationStats = new Dictionary<string, (double Mean, int Count, double Total)>(StringComparer.Ordinal);

            void UpdateStats(string dur, double space)
            {
                if (!durationStats.ContainsKey(dur))
                    durationStats[dur] = (space, 1, space);
                else
                {
                    var (_, count, total) = durationStats[dur];
                    total += space;
                    count += 1;
                    durationStats[dur] = (total / count, count, total);
                }
            }

            foreach (var voice in voices)
            {
                var tickables = voice.GetTickables();
                for (int i = 0; i < tickables.Count; i++)
                {
                    var note = tickables[i];
                    var noteTicks = note.GetTicks().Simplify();
                    string duration = noteTicks.ToString();
                    var fm = note.GetFormatterMetrics();
                    var met = note.GetNoteMetrics();

                    double leftNoteEdge = note.GetTickContext()?.GetX() ?? 0;
                    leftNoteEdge += met.NotePx + met.ModRightPx + met.RightDisplacedHeadPx;

                    double space = 0;
                    if (i < tickables.Count - 1)
                    {
                        var rightNote = tickables[i + 1];
                        var rightMet  = rightNote.GetNoteMetrics();
                        double rightNoteEdge = (rightNote.GetTickContext()?.GetX() ?? 0)
                            - rightMet.ModLeftPx - rightMet.LeftDisplacedHeadPx;

                        space = rightNoteEdge - leftNoteEdge;
                        double noteX = note.GetTickContext()?.GetX() ?? 0;
                        double rightX = rightNote.GetTickContext()?.GetX() ?? 0;
                        fm.SpaceUsed = rightX - noteX;
                        rightNote.GetFormatterMetrics().FreedomLeft = space;
                    }
                    else
                    {
                        space = justifyWidth - leftNoteEdge;
                        double noteX = note.GetTickContext()?.GetX() ?? 0;
                        fm.SpaceUsed = justifyWidth - noteX;
                    }

                    fm.FreedomRight = space;
                    UpdateStats(duration, fm.SpaceUsed);
                }
            }

            // Compute deviations from mean space per duration
            double totalDeviation = 0;
            foreach (var voice in voices)
            {
                foreach (var note in voice.GetTickables())
                {
                    var noteTicks = note.GetTicks().Simplify();
                    string duration = noteTicks.ToString();
                    var fm = note.GetFormatterMetrics();

                    if (durationStats.TryGetValue(duration, out var stats))
                    {
                        fm.SpaceMean      = stats.Mean;
                        fm.Duration       = duration;
                        fm.Iterations    += 1;
                        fm.SpaceDeviation = fm.SpaceUsed - fm.SpaceMean;
                        totalDeviation   += fm.SpaceDeviation * fm.SpaceDeviation;
                    }
                }
            }

            totalCost = Math.Sqrt(totalDeviation);
            lossHistory.Add(totalCost);
            return totalCost;
        }

        /// <summary>
        /// Run a single tuning iteration to reduce layout cost.
        /// Port of Formatter.tune() from formatter.ts.
        /// </summary>
        public double Tune(double alpha = 0.5)
        {
            var contexts = tickContexts;
            if (contexts.List.Count == 0) return 0;

            double shift = 0;
            totalShift = 0;

            for (int index = 0; index < contexts.List.Count; index++)
            {
                long tick    = contexts.List[index];
                var context  = contexts.Map[tick];
                TickContext? prevContext = index > 0 ? contexts.Map[contexts.List[index - 1]] : null;
                TickContext? nextContext = index < contexts.List.Count - 1
                    ? contexts.Map[contexts.List[index + 1]] : null;

                context.Move(shift, prevContext, nextContext);

                double cost = -context.GetDeviationCost();

                if (cost > 0)
                {
                    double freedom = context.GetFormatterMetrics().FreedomRight;
                    shift = -Math.Min(freedom, Math.Abs(cost));
                }
                else if (cost < 0 && nextContext != null)
                {
                    double freedom = nextContext.GetFormatterMetrics().FreedomRight;
                    shift = Math.Min(freedom, Math.Abs(cost));
                }
                else
                {
                    shift = 0;
                }

                shift *= alpha;
                totalShift += shift;
            }

            return Evaluate();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Get the minimum total width required to render all voices.
        /// Either Format or PreCalculateMinTotalWidth must be called first.
        /// Port of Formatter.getMinTotalWidth() from formatter.ts.
        /// </summary>
        public double GetMinTotalWidth()
        {
            if (!hasMinTotalWidth)
                throw new VexFlowSharp.VexFlowException(
                    "NoMinTotalWidth",
                    "Call 'Format' or 'PreCalculateMinTotalWidth' before calling 'GetMinTotalWidth'");
            return minTotalWidth;
        }

        /// <summary>
        /// Join voices so they share ModifierContexts (notes at the same tick
        /// can see each other for collision avoidance).
        /// Port of Formatter.joinVoices() from formatter.ts.
        /// </summary>
        public Formatter JoinVoices(List<Voice> voicesToJoin)
        {
            CreateModifierContexts(voicesToJoin);
            hasMinTotalWidth = false;
            return this;
        }

        /// <summary>
        /// Align rests in each voice to neighboring notes.
        /// Port of Formatter.alignRests() from formatter.ts.
        /// </summary>
        public void AlignRests(List<Voice> voicesToAlign, bool alignAllNotes)
        {
            if (voicesToAlign == null || voicesToAlign.Count == 0)
                throw new VexFlowSharp.VexFlowException("BadArgument", "No voices to format rests");

            foreach (var voice in voicesToAlign)
                AlignRestsToNotes(voice.GetTickables(), alignAllNotes);
        }

        private static double MidLine(double top, double bottom) => (top + bottom) / 2.0;

        private static double GetRestLineForNextNoteGroup(
            List<VexFlowSharp.Tickable> tickables,
            double currRestLine,
            int currNoteIndex,
            bool compare)
        {
            double nextRestLine = currRestLine;

            for (int noteIndex = currNoteIndex + 1; noteIndex < tickables.Count; noteIndex++)
            {
                if (tickables[noteIndex] is Note note && !note.IsRest() && !note.ShouldIgnoreTicks())
                {
                    nextRestLine = note.GetLineForRest();
                    break;
                }
            }

            if (compare && Math.Abs(currRestLine - nextRestLine) > double.Epsilon)
            {
                double top = Math.Max(currRestLine, nextRestLine);
                double bot = Math.Min(currRestLine, nextRestLine);
                nextRestLine = MidLine(top, bot);
            }

            return nextRestLine;
        }

        /// <summary>
        /// Align rests to neighboring notes within a list of tickables.
        /// Port of Formatter.AlignRestsToNotes() from formatter.ts.
        /// </summary>
        public static void AlignRestsToNotes(List<VexFlowSharp.Tickable> tickables, bool alignAllNotes,
            bool alignTuplets = false)
        {
            for (int index = 0; index < tickables.Count; index++)
            {
                if (tickables[index] is not StaveNote currTickable || !currTickable.IsRest())
                    continue;

                if (currTickable.GetTuplet() != null && !alignTuplets)
                    continue;

                double line = currTickable.GetLineForRest();
                if (Math.Abs(line - 3) > double.Epsilon)
                    continue;

                if (!alignAllNotes && currTickable.GetBeam() == null)
                    continue;

                double newLine = currTickable.GetKeyLine(0);
                if (index == 0)
                {
                    newLine = GetRestLineForNextNoteGroup(tickables, newLine, index, compare: false);
                }
                else if (index > 0 && index < tickables.Count)
                {
                    var prevTickable = tickables[index - 1];
                    if (prevTickable is StaveNote prevStaveNote)
                    {
                        if (prevStaveNote.IsRest())
                        {
                            newLine = prevStaveNote.GetKeyLine(0);
                        }
                        else
                        {
                            double restLine = prevStaveNote.GetLineForRest();
                            newLine = GetRestLineForNextNoteGroup(tickables, restLine, index, compare: true);
                        }
                    }
                }

                currTickable.SetKeyLine(0, newLine);
            }
        }

        /// <summary>
        /// Format voices and optionally justify them to justifyWidth pixels.
        /// Port of Formatter.format() from formatter.ts.
        /// </summary>
        public void Format(List<Voice> voicesToFormat, double? justifyWidthParam = null,
            FormatParams? options = null)
        {
            var opts = options ?? new FormatParams();
            voices = voicesToFormat;

            // Apply softmax factor to each voice
            double smFactor = formatterOptions.SoftmaxFactor;
            foreach (var v in voices)
                v.SetSoftmaxFactor(smFactor);

            AlignRests(voices, opts.AlignRests);
            CreateTickContexts(voices);
            PreFormat(justifyWidthParam ?? 0, voices, opts.Stave);

            // PostFormat only when stave is provided (y-values are set)
            if (opts.Stave != null) PostFormat();
        }

        /// <summary>
        /// Format voices using the stave dimensions to determine justify width.
        /// Port of Formatter.formatToStave() from formatter.ts.
        /// </summary>
        public Formatter FormatToStave(List<Voice> voicesToFormat, VexFlowSharp.Stave stave,
            FormatParams? optionsParam = null)
        {
            var options = optionsParam ?? new FormatParams();
            options.Stave = stave;

            // Justify to stave note area minus defaultPadding.
            // VexFlow: justifyWidth = stave.getNoteEndX() - stave.getNoteStartX() - Stave.defaultPadding
            // where defaultPadding = stave.padding + stave.endPaddingMax = 12 + 10 = 22.
            double justifyW = stave.GetNoteEndX()
                - stave.GetNoteStartX()
                - VexFlowSharp.Stave.DefaultPadding;

            Format(voicesToFormat, justifyW, options);
            return this;
        }

        /// <summary>
        /// Get the AlignmentContexts of TickContexts.
        /// Port of Formatter.getTickContexts() from formatter.ts.
        /// </summary>
        public AlignmentContexts GetTickContexts() => tickContexts;

        /// <summary>
        /// Get a specific TickContext by tick integer.
        /// </summary>
        public TickContext? GetTickContext(long tick)
        {
            tickContexts.Map.TryGetValue(tick, out var tc);
            return tc;
        }

        /// <summary>
        /// Get the loss history (totalCost per iteration).
        /// </summary>
        public List<double> GetLossHistory() => lossHistory;

        /// <summary>
        /// Estimate the minimum total width needed to render voices.
        /// Also creates tick contexts as a side effect.
        /// Port of Formatter.preCalculateMinTotalWidth() from formatter.ts.
        /// </summary>
        public double PreCalculateMinTotalWidth(List<Voice> voicesToCalc)
        {
            if (hasMinTotalWidth) return minTotalWidth;

            CreateTickContexts(voicesToCalc);

            var contextList = tickContexts.List;
            var contextMap  = tickContexts.Map;
            minTotalWidth = 0;

            foreach (var tick in contextList)
            {
                var context = contextMap[tick];
                context.PreFormat();
                double width = context.GetWidth();
                minTotalWidth += width;
            }

            hasMinTotalWidth = true;
            return minTotalWidth;
        }

        // ── Helpers used by GetFormatterMetrics ───────────────────────────────

        /// <summary>
        /// Get the TickContext formatter metrics for a given context.
        /// Returns the context's FormatterMetrics (via the tickables).
        /// This exists for API symmetry with the TypeScript version.
        /// </summary>
        public FormatterMetrics? GetTickContextFormatterMetrics(TickContext tc)
        {
            var tickables = tc.GetTickables();
            if (tickables.Count == 0) return null;
            return tickables[0].GetFormatterMetrics();
        }
    }
}

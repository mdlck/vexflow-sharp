// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's Beam class (beam.ts, 994 lines).
// Beam spans a group of StemmableNotes, computing slope via 20-iteration cost minimisation,
// extending stems to meet the beam line, and drawing primary and secondary beam lines.
//
// Usage:
//   var beam = new Beam(notes, autoStem: true);
//   beam.SetContext(ctx).Draw();
//
// Auto-grouping:
//   var beams = Beam.GenerateBeams(notes);
//   var beams = Beam.ApplyAndGetBeams(voice);

using System;
using System.Collections.Generic;
using System.Linq;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Common.Elements
{
    // ── Partial beam direction ─────────────────────────────────────────────────

    /// <summary>
    /// Direction of a partial (secondary-level) beam stub.
    /// Port of VexFlow's PartialBeamDirection type from beam.ts.
    /// </summary>
    public enum PartialBeamDirection
    {
        Left,
        Right,
        Both,
    }

    // ── BeamConfig ────────────────────────────────────────────────────────────

    /// <summary>
    /// Configuration options for <see cref="Beam.GenerateBeams"/>.
    /// Port of VexFlow's beam generateBeams config object from beam.ts.
    /// </summary>
    public class BeamConfig
    {
        /// <summary>Custom beat groupings for beam formation. Defaults to [2/8].</summary>
        public List<Fraction> Groups { get; set; }

        /// <summary>Override stem direction for all generated beams.</summary>
        public int? StemDirection { get; set; }

        /// <summary>Include rests inside beam groups.</summary>
        public bool BeamRests { get; set; } = false;

        /// <summary>Only include rests in the middle of beat groups.</summary>
        public bool BeamMiddleOnly { get; set; } = false;

        /// <summary>Maintain existing stem directions (do not auto-calc).</summary>
        public bool MaintainStemDirections { get; set; } = false;

        /// <summary>Show stemlets for rests inside beams.</summary>
        public bool ShowStemlets { get; set; } = false;

        /// <summary>Duration string for secondary beam breaks (e.g. "16").</summary>
        public string SecondaryBreaks { get; set; }

        /// <summary>Force flat (horizontal) beams.</summary>
        public bool FlatBeams { get; set; } = false;

        /// <summary>Y-offset for flat beams (null = auto-calculate).</summary>
        public double? FlatBeamOffset { get; set; }
    }

    // ── Render options ────────────────────────────────────────────────────────

    /// <summary>
    /// Render tunables for a Beam instance.
    /// Port of VexFlow's Beam.render_options from beam.ts.
    /// </summary>
    public class BeamRenderOptions
    {
        /// <summary>Width (height in pixels) of each beam line.</summary>
        public double BeamWidth { get; set; } = 5;

        /// <summary>Maximum allowed slope.</summary>
        public double MaxSlope { get; set; } = 0.25;

        /// <summary>Minimum allowed slope.</summary>
        public double MinSlope { get; set; } = -0.25;

        /// <summary>Number of slope candidates tested. Default 20.</summary>
        public int SlopeIterations { get; set; } = 20;

        /// <summary>Weight applied to slope distance from ideal. Default 100.</summary>
        public double SlopeCost { get; set; } = 100;

        /// <summary>Draw stemlets for rests.</summary>
        public bool ShowStemlets { get; set; } = false;

        /// <summary>Extra pixels beyond the beam for stemlet tips.</summary>
        public double StemletExtension { get; set; } = 7;

        /// <summary>Length in px of a partial (stub) beam. Default 10.</summary>
        public double PartialBeamLength { get; set; } = 10;

        /// <summary>Force flat (horizontal) beams.</summary>
        public bool FlatBeams { get; set; } = false;

        /// <summary>Minimum offset from outermost notehead for flat beams. Default 15.</summary>
        public double MinFlatBeamOffset { get; set; } = 15;

        /// <summary>Y-offset for flat beams. Null = auto-calculate.</summary>
        public double? FlatBeamOffset { get; set; }

        /// <summary>Tick count at which to break secondary beams. Null = never.</summary>
        public int? SecondaryBreakTicks { get; set; }
    }

    // ── Beam ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spans over a set of StemmableNotes, rendering beam lines with slope and secondary beams.
    /// Port of VexFlow's Beam class from beam.ts (994 lines).
    ///
    /// Call sequence:
    ///   1. new Beam(notes)               — attach beam to notes, compute beam_count
    ///   2. voice.Draw(ctx, stave)        — notes get stem y-values set (stave must be attached)
    ///   3. beam.SetContext(ctx).Draw()   — PostFormat() + DrawBeamLines()
    /// </summary>
    public class Beam : VexFlowSharp.Element
    {
        // ── Category ──────────────────────────────────────────────────────────

        public new const string CATEGORY = "Beam";
        public override string GetCategory() => CATEGORY;

        // ── Static beam-direction constants ───────────────────────────────────

        public const string BEAM_LEFT  = "L";
        public const string BEAM_RIGHT = "R";
        public const string BEAM_BOTH  = "B";

        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>The notes grouped under this beam.</summary>
        public List<VexFlowSharp.StemmableNote> Notes { get; }

        /// <summary>Whether PostFormat has already run.</summary>
        public bool PostFormatted { get; private set; }

        /// <summary>The best slope found by CalculateSlope (pixels/pixel).</summary>
        public double Slope { get; private set; }

        /// <summary>Render options for this beam.</summary>
        public BeamRenderOptions RenderOptions { get; }

        private int stemDirection;
        private readonly int ticks;
        private double yShift;
        private readonly List<int> breakOnIndices;
        private readonly int beamCount;
        /// <summary>Forced partial-beam sides by note index.</summary>
        private readonly Dictionary<int, PartialBeamDirection> forcedPartialDirections;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a Beam spanning the given notes.
        /// Port of Beam constructor from beam.ts.
        /// </summary>
        /// <param name="notes">At least 2 stemmable notes, all shorter than a quarter.</param>
        /// <param name="autoStem">Automatically compute stem direction from note positions.</param>
        public Beam(List<VexFlowSharp.StemmableNote> notes, bool autoStem = false)
        {
            if (notes == null || notes.Count == 0)
                throw new VexFlowSharp.VexFlowException("BadArguments", "No notes provided for beam.");
            if (notes.Count == 1)
                throw new VexFlowSharp.VexFlowException("BadArguments", "Too few notes for beam.");

            ticks = notes[0].GetIntrinsicTicks();

            // Beams can only be applied to notes shorter than a quarter note
            if (ticks >= Tables.DurationToTicks("4"))
                throw new VexFlowSharp.VexFlowException(
                    "BadArguments", "Beams can only be applied to notes shorter than a quarter note.");

            Notes = notes;
            stemDirection = notes[0].GetStemDirection();

            int computedStemDir = stemDirection;
            if (autoStem)
                computedStemDir = CalculateStemDirectionForNotes(notes);

            for (int i = 0; i < notes.Count; i++)
            {
                var note = notes[i];
                if (autoStem)
                {
                    note.SetStemDirection(computedStemDir);
                    stemDirection = computedStemDir;
                }
                note.SetBeam(this);
            }

            PostFormatted = false;
            Slope = 0;
            yShift = 0;
            breakOnIndices = new List<int>();
            forcedPartialDirections = new Dictionary<int, PartialBeamDirection>();
            RenderOptions = new BeamRenderOptions();
            beamCount = GetBeamCountFromNotes();
        }

        // ── Static helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Convert a duration string to its numeric value (e.g. "8" => 8, "4" => 4).
        /// Port of Tables.durationToNumber() from tables.ts.
        /// "1/2" => 0.5, "1" => 1, "2" => 2, "4" => 4, "8" => 8, "16" => 16, etc.
        /// A note is unbeamable if its number is less than 8.
        /// </summary>
        private static double DurationToNumber(string duration)
        {
            // The duration string, as-is, represents the denominator of the note fraction.
            // "4" means 1/4 note; "8" means 1/8 note. The "number" is the integer part.
            // "1/2" is a special case = 0.5.
            duration = duration.Trim();
            if (duration == "1/2") return 0.5;
            if (double.TryParse(duration, out double d)) return d;
            return 4; // fallback
        }

        /// <summary>
        /// Get default beam groups for a time signature string.
        /// Port of Beam.getDefaultBeamGroups() from beam.ts.
        /// </summary>
        public static List<Fraction> GetDefaultBeamGroups(string timeSig)
        {
            if (string.IsNullOrEmpty(timeSig) || timeSig == "c")
                timeSig = "4/4";

            var defaults = new Dictionary<string, string>
            {
                ["1/2"] = "1/2", ["2/2"] = "1/2", ["3/2"] = "1/2", ["4/2"] = "1/2",
                ["1/4"] = "1/4", ["2/4"] = "1/4", ["3/4"] = "1/4", ["4/4"] = "1/4",
                ["1/8"] = "1/8", ["2/8"] = "2/8", ["3/8"] = "3/8", ["4/8"] = "2/8",
                ["1/16"] = "1/16", ["2/16"] = "2/16", ["3/16"] = "3/16", ["4/16"] = "2/16",
            };

            if (defaults.TryGetValue(timeSig, out var groupStr))
                return new List<Fraction> { ParseGroupFraction(groupStr) };

            // Naive fallback from time signature
            var parts = timeSig.Split('/');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int beatTotal) &&
                int.TryParse(parts[1], out int beatValue))
            {
                bool triple = beatTotal % 3 == 0;
                if (triple)
                    return new List<Fraction> { new Fraction(3, beatValue) };
                if (beatValue > 4)
                    return new List<Fraction> { new Fraction(2, beatValue) };
                return new List<Fraction> { new Fraction(1, beatValue) };
            }

            return new List<Fraction> { new Fraction(1, 4) };
        }

        private static Fraction ParseGroupFraction(string s)
        {
            var parts = s.Split('/');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int n) &&
                int.TryParse(parts[1], out int d))
                return new Fraction(n, d);
            return new Fraction(1, 4);
        }

        /// <summary>
        /// Calculate stem direction for a group of notes based on line position.
        /// Port of the free function calculateStemDirection() from beam.ts.
        /// </summary>
        private static int CalculateStemDirectionForNotes(List<VexFlowSharp.StemmableNote> notes)
        {
            double lineSum = 0;
            foreach (var note in notes)
            {
                var kps = note.GetKeyProps();
                foreach (var kp in kps)
                    lineSum += kp.Line - 3;
            }
            return lineSum >= 0 ? VexFlowSharp.Stem.DOWN : VexFlowSharp.Stem.UP;
        }

        /// <summary>
        /// Get the initial slope between first and last note stem tips.
        /// Port of the free function getStemSlope() from beam.ts.
        /// </summary>
        private static double GetStemSlope(
            VexFlowSharp.StemmableNote firstNote,
            VexFlowSharp.StemmableNote lastNote)
        {
            var (firstTopY, _) = firstNote.GetStemExtents();
            var (lastTopY, _)  = lastNote.GetStemExtents();
            double firstX = firstNote.GetStemX();
            double lastX  = lastNote.GetStemX();
            if (Math.Abs(lastX - firstX) < 0.001) return 0;
            return (lastTopY - firstTopY) / (lastX - firstX);
        }

        // ── Auto-beam public API ───────────────────────────────────────────────

        /// <summary>
        /// Generate beams automatically from a list of notes and a config.
        /// Port of Beam.generateBeams() from beam.ts.
        /// </summary>
        public static List<Beam> GenerateBeams(
            List<VexFlowSharp.StemmableNote> notes,
            BeamConfig config = null)
        {
            config = config ?? new BeamConfig();
            if (config.Groups == null || config.Groups.Count == 0)
                config.Groups = new List<Fraction> { new Fraction(2, 8) };

            // Convert group fractions to tick amounts
            var tickGroups = config.Groups
                .Select(g => g.Multiply(new Fraction(Tables.RESOLUTION, 1)))
                .ToList();

            int currentTickGroup = 0;
            var noteGroups = new List<List<VexFlowSharp.StemmableNote>>();
            var currentGroup = new List<VexFlowSharp.StemmableNote>();

            Fraction GetTotalTicks(List<VexFlowSharp.StemmableNote> grpNotes)
            {
                var total = new Fraction(0, 1);
                foreach (var n in grpNotes)
                    total = total.Add(n.GetTicks());
                return total;
            }

            void NextTickGroup()
            {
                if (tickGroups.Count - 1 > currentTickGroup)
                    currentTickGroup++;
                else
                    currentTickGroup = 0;
            }

            // ── createGroups ──────────────────────────────────────────────────
            var currentGroupTotalTicks = new Fraction(0, 1);
            foreach (var note in notes)
            {
                var nextGroup = new List<VexFlowSharp.StemmableNote>();
                if (note.ShouldIgnoreTicks())
                {
                    noteGroups.Add(new List<VexFlowSharp.StemmableNote>(currentGroup));
                    currentGroup = nextGroup;
                    continue;
                }

                currentGroup.Add(note);
                var ticksPerGroup = tickGroups[currentTickGroup];
                var totalTicks = GetTotalTicks(currentGroup).Add(currentGroupTotalTicks);

                // Quarter notes and longer are unbeamable
                bool isUnbeamable = DurationToNumber(note.GetDuration()) < 8;
                if (isUnbeamable && note.GetTuplet() != null)
                {
                    // Double the amount of ticks in a group for unbeamable tuplets
                    ticksPerGroup = new Fraction(ticksPerGroup.Numerator * 2, ticksPerGroup.Denominator);
                }

                if (totalTicks > ticksPerGroup)
                {
                    if (!isUnbeamable)
                    {
                        currentGroup.RemoveAt(currentGroup.Count - 1);
                        nextGroup.Add(note);
                    }
                    noteGroups.Add(new List<VexFlowSharp.StemmableNote>(currentGroup));

                    do
                    {
                        currentGroupTotalTicks = totalTicks.Subtract(tickGroups[currentTickGroup]);
                        NextTickGroup();
                    }
                    while (currentGroupTotalTicks >= tickGroups[currentTickGroup]);

                    currentGroup = nextGroup;
                }
                else if (totalTicks == ticksPerGroup)
                {
                    noteGroups.Add(new List<VexFlowSharp.StemmableNote>(currentGroup));
                    currentGroupTotalTicks = new Fraction(0, 1);
                    currentGroup = nextGroup;
                    NextTickGroup();
                }
            }

            if (currentGroup.Count > 0)
                noteGroups.Add(currentGroup);

            // ── sanitizeGroups ────────────────────────────────────────────────
            var sanitizedGroups = new List<List<VexFlowSharp.StemmableNote>>();
            foreach (var group in noteGroups)
            {
                var tempGroup = new List<VexFlowSharp.StemmableNote>();
                for (int i = 0; i < group.Count; i++)
                {
                    var note = group[i];
                    bool isFirstOrLast = i == 0 || i == group.Count - 1;
                    var prevNote = i > 0 ? group[i - 1] : null;

                    bool breaksOnEachRest       = !config.BeamRests && note.IsRest();
                    bool breaksOnFirstOrLastRest = config.BeamRests && config.BeamMiddleOnly
                                                    && note.IsRest() && isFirstOrLast;
                    bool breakOnStemChange = false;
                    if (config.MaintainStemDirections && prevNote != null
                        && !note.IsRest() && !prevNote.IsRest())
                    {
                        breakOnStemChange = note.GetStemDirection() != prevNote.GetStemDirection();
                    }

                    bool isUnbeamableDuration = DurationToNumber(note.GetDuration()) < 8;
                    bool shouldBreak = breaksOnEachRest || breaksOnFirstOrLastRest
                                       || breakOnStemChange || isUnbeamableDuration;

                    if (shouldBreak)
                    {
                        if (tempGroup.Count > 0)
                            sanitizedGroups.Add(new List<VexFlowSharp.StemmableNote>(tempGroup));
                        tempGroup = breakOnStemChange
                            ? new List<VexFlowSharp.StemmableNote> { note }
                            : new List<VexFlowSharp.StemmableNote>();
                    }
                    else
                    {
                        tempGroup.Add(note);
                    }
                }
                if (tempGroup.Count > 0)
                    sanitizedGroups.Add(tempGroup);
            }
            noteGroups = sanitizedGroups;

            // ── formatStems ───────────────────────────────────────────────────
            foreach (var group in noteGroups)
            {
                int stemDir;
                if (config.MaintainStemDirections)
                {
                    var firstNote = group.FirstOrDefault(n => !n.IsRest());
                    stemDir = firstNote != null ? firstNote.GetStemDirection() : VexFlowSharp.Stem.UP;
                }
                else
                {
                    stemDir = config.StemDirection.HasValue
                        ? config.StemDirection.Value
                        : CalculateStemDirectionForNotes(group);
                }
                foreach (var note in group)
                    note.SetStemDirection(stemDir);
            }

            // ── getTuplets ───────────────────────────────────────────────────
            var allTuplets = new List<Tuplet>();
            foreach (var group in noteGroups)
            {
                Tuplet tuplet = null;
                foreach (var note in group)
                {
                    if (note.GetTuplet() is Tuplet noteTuplet && tuplet != noteTuplet)
                    {
                        tuplet = noteTuplet;
                        if (!allTuplets.Contains(tuplet))
                            allTuplets.Add(tuplet);
                    }
                }
            }

            // ── getBeamGroups — filter to groups > 1 note, all beamable ──────
            var beamedNoteGroups = noteGroups
                .Where(group =>
                {
                    if (group.Count <= 1) return false;
                    return group.All(n => n.GetIntrinsicTicks() < Tables.DurationToTicks("4"));
                })
                .ToList();

            // ── Create Beam instances ─────────────────────────────────────────
            var beams = new List<Beam>();
            foreach (var group in beamedNoteGroups)
            {
                var beam = new Beam(group);
                if (config.ShowStemlets)
                    beam.RenderOptions.ShowStemlets = true;
                if (config.SecondaryBreaks != null)
                    beam.RenderOptions.SecondaryBreakTicks = Tables.DurationToTicks(config.SecondaryBreaks);
                if (config.FlatBeams)
                {
                    beam.RenderOptions.FlatBeams = true;
                    beam.RenderOptions.FlatBeamOffset = config.FlatBeamOffset;
                }
                beams.Add(beam);
            }

            // Reformat tuplets after beam attachment, matching v5 beam.ts.
            foreach (var tuplet in allTuplets)
            {
                var tupletNotes = tuplet.GetNotes();
                if (tupletNotes.Count == 0) continue;

                if (tupletNotes[0] is VexFlowSharp.StemmableNote first)
                {
                    int direction = first.GetStemDirection() == VexFlowSharp.Stem.DOWN
                        ? (int)TupletLocation.Bottom
                        : (int)TupletLocation.Top;
                    tuplet.SetTupletLocation(direction);
                }

                bool bracketed = false;
                foreach (var note in tupletNotes)
                {
                    if (note is not VexFlowSharp.StemmableNote stemmable || !stemmable.HasBeam())
                    {
                        bracketed = true;
                        break;
                    }
                }
                tuplet.SetBracketed(bracketed);
            }

            return beams;
        }

        /// <summary>
        /// Convenience wrapper: generate beams from a Voice's tickables.
        /// Port of Beam.applyAndGetBeams() from beam.ts.
        /// </summary>
        public static List<Beam> ApplyAndGetBeams(
            Voice voice,
            int? stemDirection = null,
            List<Fraction> groups = null)
        {
            var stemmableNotes = voice.GetTickables()
                .OfType<VexFlowSharp.StemmableNote>()
                .ToList();

            return GenerateBeams(stemmableNotes, new BeamConfig
            {
                StemDirection = stemDirection,
                Groups = groups,
            });
        }

        // ── Instance: stem direction & slope ──────────────────────────────────

        /// <summary>Get the stem direction for this beam group.</summary>
        public int GetStemDirection() => stemDirection;

        /// <summary>Get the notes in this beam.</summary>
        public List<VexFlowSharp.StemmableNote> GetNotes() => Notes;

        /// <summary>
        /// Get the maximum beam count across all notes.
        /// Port of Beam.getBeamCount() from beam.ts.
        /// </summary>
        private int GetBeamCountFromNotes()
        {
            int max = 0;
            foreach (var n in Notes)
            {
                int bc = n.GetBeamCount();
                if (bc > max) max = bc;
            }
            return max;
        }

        /// <summary>Set which note indices should break the secondary beam.</summary>
        public Beam BreakSecondaryAt(List<int> indices)
        {
            breakOnIndices.Clear();
            breakOnIndices.AddRange(indices);
            return this;
        }

        /// <summary>Force the partial beam direction at a specific note index.</summary>
        public Beam SetPartialBeamSideAt(int noteIndex, PartialBeamDirection side)
        {
            forcedPartialDirections[noteIndex] = side;
            return this;
        }

        /// <summary>Clear a forced partial beam direction.</summary>
        public Beam UnsetPartialBeamSideAt(int noteIndex)
        {
            forcedPartialDirections.Remove(noteIndex);
            return this;
        }

        // ── Slope calculation ─────────────────────────────────────────────────

        /// <summary>
        /// Return the Y coordinate on the beam line for a given X.
        /// Port of Beam.getSlopeY() from beam.ts.
        /// </summary>
        public double GetSlopeY(double x, double firstXPx, double firstYPx, double slope)
            => firstYPx + (x - firstXPx) * slope;

        /// <summary>
        /// Calculate the best slope using a 20-iteration cost-minimisation loop.
        /// Port of Beam.calculateSlope() from beam.ts.
        /// </summary>
        public void CalculateSlope()
        {
            var firstNote = Notes[0];
            double initialSlope = GetStemSlope(firstNote, Notes[Notes.Count - 1]);

            double minSlope  = RenderOptions.MinSlope;
            double maxSlope  = RenderOptions.MaxSlope;
            int    iters     = RenderOptions.SlopeIterations;
            double slopeCost = RenderOptions.SlopeCost;

            double increment = (maxSlope - minSlope) / iters;
            double minCost   = double.MaxValue;
            double bestSlope = 0;
            double bestYShift = 0;

            for (double slope = minSlope; slope <= maxSlope; slope += increment)
            {
                double totalStemExtension = 0;
                double yShiftTemp = 0;

                for (int i = 1; i < Notes.Count; i++)
                {
                    var note = Notes[i];
                    if (!note.HasStem() && !note.IsRest()) continue;

                    var (stemTipY, _) = note.GetStemExtents();
                    double stemX = note.GetStemX();

                    var (firstTopY, _) = firstNote.GetStemExtents();
                    double adjustedStemTipY =
                        GetSlopeY(stemX, firstNote.GetStemX(), firstTopY, slope) + yShiftTemp;

                    if (stemTipY * stemDirection < adjustedStemTipY * stemDirection)
                    {
                        double diff = Math.Abs(stemTipY - adjustedStemTipY);
                        yShiftTemp += diff * -stemDirection;
                        totalStemExtension += diff * i;
                    }
                    else
                    {
                        totalStemExtension += (stemTipY - adjustedStemTipY) * stemDirection;
                    }
                }

                double idealSlope      = initialSlope / 2.0;
                double distFromIdeal   = Math.Abs(idealSlope - slope);
                double cost            = slopeCost * distFromIdeal + Math.Abs(totalStemExtension);

                if (cost < minCost)
                {
                    minCost    = cost;
                    bestSlope  = slope;
                    bestYShift = yShiftTemp;
                }
            }

            Slope  = bestSlope;
            yShift = bestYShift;
        }

        /// <summary>
        /// Calculate a flat (zero-slope) beam with a y-offset tuned to clear all noteheads.
        /// Port of Beam.calculateFlatSlope() from beam.ts.
        /// </summary>
        public void CalculateFlatSlope()
        {
            double beamWidth = RenderOptions.BeamWidth;
            double minFlatOff = RenderOptions.MinFlatBeamOffset;
            double? flatOffset = RenderOptions.FlatBeamOffset;

            double total = 0;
            double extremeY = 0;
            double extremeBeamCount = 0;
            double currentExtreme = 0;

            foreach (var note in Notes)
            {
                var (stemTipY, _) = note.GetStemExtents();
                total += stemTipY;

                double[] ys;
                try { ys = note.GetYs(); }
                catch { ys = new double[0]; }

                if (stemDirection == VexFlowSharp.Stem.DOWN && currentExtreme < stemTipY)
                {
                    currentExtreme = stemTipY;
                    extremeY = ys.Length > 0 ? ys.Max() : stemTipY;
                    extremeBeamCount = note.GetBeamCount();
                }
                else if (stemDirection == VexFlowSharp.Stem.UP
                    && (currentExtreme == 0 || currentExtreme > stemTipY))
                {
                    currentExtreme = stemTipY;
                    extremeY = ys.Length > 0 ? ys.Min() : stemTipY;
                    extremeBeamCount = note.GetBeamCount();
                }
            }

            double offset = total / Notes.Count;
            double bw = beamWidth * 1.5;
            double extremeTest = minFlatOff + extremeBeamCount * bw;
            double newOffset = extremeY + extremeTest * -stemDirection;

            if (stemDirection == VexFlowSharp.Stem.DOWN && offset < newOffset)
                offset = extremeY + extremeTest;
            else if (stemDirection == VexFlowSharp.Stem.UP && offset > newOffset)
                offset = extremeY - extremeTest;

            if (!flatOffset.HasValue)
                RenderOptions.FlatBeamOffset = offset;
            else if (stemDirection == VexFlowSharp.Stem.DOWN && offset > flatOffset.Value)
                RenderOptions.FlatBeamOffset = offset;
            else if (stemDirection == VexFlowSharp.Stem.UP && offset < flatOffset.Value)
                RenderOptions.FlatBeamOffset = offset;

            Slope  = 0;
            yShift = 0;
        }

        /// <summary>
        /// Return the Y at which the primary beam should be drawn for the first note.
        /// Port of Beam.getBeamYToDraw() from beam.ts.
        /// </summary>
        public double GetBeamYToDraw()
        {
            var (firstStemTipY, _) = Notes[0].GetStemExtents();
            if (RenderOptions.FlatBeams && RenderOptions.FlatBeamOffset.HasValue)
                return RenderOptions.FlatBeamOffset.Value;
            return firstStemTipY;
        }

        // ── Stem extensions ───────────────────────────────────────────────────

        /// <summary>
        /// Extend stems so each note's stem tip reaches the beam line.
        /// Port of Beam.applyStemExtensions() from beam.ts.
        /// </summary>
        public void ApplyStemExtensions()
        {
            var firstNote = Notes[0];
            double firstStemTipY = GetBeamYToDraw();
            double firstStemX = firstNote.GetStemX();

            for (int i = 0; i < Notes.Count; i++)
            {
                var note = Notes[i];
                var stem = note.GetStem();
                if (stem == null) continue;

                double stemX = note.GetStemX();
                var (stemTipY, _) = note.GetStemExtents();
                double beamedStemTipY = GetSlopeY(stemX, firstStemX, firstStemTipY, Slope) + yShift;

                double preBeamExtension = stem.GetExtension();
                double beamExtension = note.GetStemDirection() == VexFlowSharp.Stem.UP
                    ? stemTipY - beamedStemTipY
                    : beamedStemTipY - stemTipY;

                double crossStemExtension = 0;
                if (note.GetStemDirection() != stemDirection)
                {
                    int bc = note.GetBeamCount();
                    crossStemExtension = (1 + (bc - 1) * 1.5) * RenderOptions.BeamWidth;
                }

                stem.SetExtension(preBeamExtension + beamExtension + crossStemExtension);
                stem.AdjustHeightForBeam();

                if (note.IsRest() && RenderOptions.ShowStemlets)
                {
                    double bw = RenderOptions.BeamWidth;
                    double totalBeamW = (beamCount - 1) * bw * 1.5 + bw;
                    stem.SetVisibility(true).SetStemlet(true, totalBeamW + RenderOptions.StemletExtension);
                }
            }
        }

        // ── Beam-line geometry ────────────────────────────────────────────────

        /// <summary>
        /// Determine the partial beam direction for a secondary beam at a given note.
        /// Port of Beam.lookupBeamDirection() from beam.ts.
        /// </summary>
        public string LookupBeamDirection(
            string duration,
            int prevTick, int tick, int nextTick,
            int noteIndex)
        {
            if (duration == "4") return BEAM_LEFT;

            if (forcedPartialDirections.TryGetValue(noteIndex, out var forced))
            {
                return forced == PartialBeamDirection.Left  ? BEAM_LEFT  :
                       forced == PartialBeamDirection.Right ? BEAM_RIGHT : BEAM_BOTH;
            }

            double num = DurationToNumber(duration);
            if (num <= 0) return BEAM_LEFT;

            // Look up the "halved" duration
            string lookupDuration = ((int)(num / 2)).ToString();
            int lookupTicks;
            try { lookupTicks = Tables.DurationToTicks(lookupDuration); }
            catch { return BEAM_LEFT; }

            bool prevGetsBeam = prevTick < lookupTicks;
            bool nextGetsBeam = nextTick < lookupTicks;
            bool thisGetsBeam = tick < lookupTicks;

            if (prevGetsBeam && nextGetsBeam && thisGetsBeam) return BEAM_BOTH;
            if (prevGetsBeam && !nextGetsBeam && thisGetsBeam) return BEAM_LEFT;
            if (!prevGetsBeam && nextGetsBeam && thisGetsBeam) return BEAM_RIGHT;

            // Recurse with halved duration
            return LookupBeamDirection(lookupDuration, prevTick, tick, nextTick, noteIndex);
        }

        /// <summary>
        /// Compute beam-line segments for a specific beam level (duration).
        /// Returns a list of (start, end) x-coordinates.
        /// Port of Beam.getBeamLines() from beam.ts.
        /// </summary>
        public List<(double Start, double? End)> GetBeamLines(string duration)
        {
            int tickOfDuration = Tables.DurationToTicks(duration);
            bool beamStarted = false;
            var beamLines = new List<(double Start, double? End)>();
            int currentIndex = -1;
            double partialBeamLength = RenderOptions.PartialBeamLength;
            bool previousShouldBreak = false;
            int tickTally = 0;

            for (int i = 0; i < Notes.Count; i++)
            {
                var note = Notes[i];
                int ticks = note.GetTicks().Numerator;  // already integer ticks (denom = 1 for simple notes)
                // More robust: use Value() * resolution
                int noteTicks = (int)Math.Round(note.GetTicks().Value());
                tickTally += noteTicks;

                bool shouldBreak = false;

                // 8th note beams are always drawn; secondary beams may break
                int durNum = (int)DurationToNumber(duration);
                if (durNum >= 8)
                {
                    shouldBreak = breakOnIndices.Contains(i);
                    if (RenderOptions.SecondaryBreakTicks.HasValue
                        && tickTally >= RenderOptions.SecondaryBreakTicks.Value)
                    {
                        tickTally = 0;
                        shouldBreak = true;
                    }
                }

                bool noteGetsBeam = note.GetIntrinsicTicks() < tickOfDuration;
                double stemX = note.GetStemX() - VexFlowSharp.Stem.WIDTH / 2.0;

                var prevNote = i > 0 ? Notes[i - 1] : null;
                var nextNote = i < Notes.Count - 1 ? Notes[i + 1] : null;
                bool nextNoteGetsBeam = nextNote != null && nextNote.GetIntrinsicTicks() < tickOfDuration;
                bool prevNoteGetsBeam = prevNote != null && prevNote.GetIntrinsicTicks() < tickOfDuration;
                bool beamAlone = prevNote != null && nextNote != null
                                 && noteGetsBeam && !prevNoteGetsBeam && !nextNoteGetsBeam;

                if (noteGetsBeam)
                {
                    if (beamStarted)
                    {
                        // Extend current beam to this stem
                        beamLines[currentIndex] = (beamLines[currentIndex].Start, stemX);

                        if (shouldBreak)
                        {
                            beamStarted = false;
                            // If next note won't get a beam, we need a left partial
                            if (nextNote != null && !nextNoteGetsBeam && beamLines[currentIndex].End == null)
                                beamLines[currentIndex] = (beamLines[currentIndex].Start,
                                    beamLines[currentIndex].Start - partialBeamLength);
                        }
                    }
                    else
                    {
                        // Start a new beam segment
                        double? endX = null;
                        beamStarted = true;

                        if (beamAlone)
                        {
                            int prevTk = prevNote!.GetIntrinsicTicks();
                            int nextTk = nextNote!.GetIntrinsicTicks();
                            int thisTk = note.GetIntrinsicTicks();
                            string dir = LookupBeamDirection(duration, prevTk, thisTk, nextTk, i);

                            if (dir == BEAM_LEFT || dir == BEAM_BOTH)
                                endX = stemX - partialBeamLength;
                            else
                                endX = stemX + partialBeamLength;
                        }
                        else if (!nextNoteGetsBeam)
                        {
                            if ((previousShouldBreak || i == 0) && nextNote != null)
                                endX = stemX + partialBeamLength;
                            else
                                endX = stemX - partialBeamLength;
                        }
                        else if (shouldBreak)
                        {
                            endX = stemX - partialBeamLength;
                            beamStarted = false;
                        }

                        beamLines.Add((stemX, endX));
                        currentIndex = beamLines.Count - 1;
                    }
                }
                else
                {
                    beamStarted = false;
                }

                previousShouldBreak = shouldBreak;
            }

            // Close any dangling beam
            if (beamLines.Count > 0)
            {
                var last = beamLines[beamLines.Count - 1];
                if (last.End == null)
                    beamLines[beamLines.Count - 1] = (last.Start, last.Start - partialBeamLength);
            }

            return beamLines;
        }

        // ── Stems ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Draw each note's stem. Called from Draw().
        /// Port of Beam.drawStems() from beam.ts.
        /// </summary>
        protected void DrawStems(VexFlowSharp.RenderContext ctx)
        {
            foreach (var note in Notes)
            {
                var stem = note.GetStem();
                if (stem != null)
                {
                    double stemX = note.GetStemX();
                    stem.SetNoteHeadXBounds(stemX, stemX);
                    stem.SetContext(ctx).Draw();
                }
            }
        }

        // ── Beam lines ────────────────────────────────────────────────────────

        /// <summary>
        /// Draw primary and secondary beam lines.
        /// Port of Beam.drawBeamLines() from beam.ts.
        /// </summary>
        protected void DrawBeamLines(VexFlowSharp.RenderContext ctx)
        {
            var validDurations = new[] { "4", "8", "16", "32", "64", "128" };

            var firstNote = Notes[0];
            double beamY = GetBeamYToDraw();
            double firstStemX = firstNote.GetStemX();
            double beamThickness = RenderOptions.BeamWidth * stemDirection;

            foreach (var duration in validDurations)
            {
                var lines = GetBeamLines(duration);

                foreach (var (start, endNullable) in lines)
                {
                    double startX = start;
                    double startY = GetSlopeY(startX, firstStemX, beamY, Slope);

                    if (endNullable.HasValue)
                    {
                        double endX = endNullable.Value;
                        double endY = GetSlopeY(endX, firstStemX, beamY, Slope);

                        ctx.BeginPath();
                        ctx.MoveTo(startX, startY);
                        ctx.LineTo(startX, startY + beamThickness);
                        ctx.LineTo(endX + 1, endY + beamThickness);
                        ctx.LineTo(endX + 1, endY);
                        ctx.ClosePath();
                        ctx.Fill();
                    }
                    else
                    {
                        throw new VexFlowSharp.VexFlowException("NoLastBeamX", "lastBeamX undefined.");
                    }
                }

                beamY += beamThickness * 1.5;
            }
        }

        // ── Format / Draw ─────────────────────────────────────────────────────

        /// <summary>
        /// Pre-format: no-op for beams (all work happens in PostFormat after notes have positions).
        /// Port of Beam.preFormat() from beam.ts.
        /// </summary>
        public Beam PreFormat() => this;

        /// <summary>
        /// Post-format: calculate slope and extend stems.
        /// Must be called after notes have stave y-values set (i.e., after voice.Draw()).
        /// Port of Beam.postFormat() from beam.ts.
        /// </summary>
        public void PostFormat()
        {
            if (PostFormatted) return;

            if (Notes[0] is VexFlowSharp.TabNote || RenderOptions.FlatBeams)
                CalculateFlatSlope();
            else
                CalculateSlope();

            ApplyStemExtensions();
            PostFormatted = true;
        }

        /// <summary>
        /// Render the beam: stems + beam lines.
        /// Calls PostFormat() if not already done.
        /// Port of Beam.draw() from beam.ts.
        /// </summary>
        public override void Draw()
        {
            var ctx = CheckContext();
            rendered = true;

            if (!PostFormatted)
                PostFormat();

            DrawStems(ctx);
            ApplyStyle();
            ctx.OpenGroup("beam", GetId());
            DrawBeamLines(ctx);
            ctx.CloseGroup();
            RestoreStyle();
        }
    }
}

#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's StaveNote class (stavenote.ts, 1275 lines).
// StaveNote is the central rendering class for standard music notation.
// It renders noteheads, stems, ledger lines, and flags for all durations and rest types.
//
// Formatter-dependent paths are stubs/no-ops (Phase 3 will integrate ModifierContext/TickContext).
// Draw() call order mirrors VexFlow's exact ordering: ledger lines, stem, noteheads, flag.

using System;
using System.Collections.Generic;
using System.Linq;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    // ── Data structures ───────────────────────────────────────────────────────

    /// <summary>
    /// Input structure for constructing a StaveNote.
    /// Extends NoteStruct with clef, octave shift, and stem options.
    /// Port of VexFlow's StaveNoteStruct interface from stavenote.ts.
    /// </summary>
    public class StaveNoteStruct : NoteStruct
    {
        /// <summary>Clef for this note (default "treble").</summary>
        public new string? Clef { get; set; }

        /// <summary>Octave shift (positive = higher, negative = lower).</summary>
        public new int? OctaveShift { get; set; }

        /// <summary>Whether to automatically determine stem direction.</summary>
        public new bool? AutoStem { get; set; }

        /// <summary>Manual stem direction: Stem.UP or Stem.DOWN.</summary>
        public new int? StemDirection { get; set; }

        /// <summary>Glyph font scale override.</summary>
        public new double? GlyphFontScale { get; set; }

        /// <summary>Stroke pixels for ledger lines.</summary>
        public new double? StrokePx { get; set; }
    }

    /// <summary>
    /// Render options for a StaveNote.
    /// Port of VexFlow's render_options fields in stavenote.ts.
    /// </summary>
    public class StaveNoteRenderOptions
    {
        /// <summary>Glyph font scale for noteheads and rests.</summary>
        public double GlyphFontScale { get; set; } = Tables.NOTATION_FONT_SCALE;

        /// <summary>Number of stroke pixels to the left and right of the note head (ledger line extent).</summary>
        public double StrokePx { get; set; } = StaveNote.LEDGER_LINE_OFFSET;

        /// <summary>Whether to draw this note at all.</summary>
        public bool Draw { get; set; } = true;
    }

    /// <summary>
    /// Head bounds returned by GetNoteHeadBounds().
    /// Port of VexFlow's StaveNoteHeadBounds interface.
    /// </summary>
    public class StaveNoteHeadBounds
    {
        public double YTop { get; set; } = double.PositiveInfinity;
        public double YBottom { get; set; } = double.NegativeInfinity;
        public double? DisplacedX { get; set; }
        public double? NonDisplacedX { get; set; }
        public double HighestLine { get; set; }
        public double LowestLine { get; set; }
        public double? HighestDisplacedLine { get; set; }
        public double? LowestDisplacedLine { get; set; }
        public double HighestNonDisplacedLine { get; set; }
        public double LowestNonDisplacedLine { get; set; }
    }

    /// <summary>
    /// Sorted key prop entry — key props plus original index for mapping back to keys[].
    /// </summary>
    public class SortedKeyPropEntry
    {
        public KeyProps KeyProps { get; set; } = null!;
        public int Index { get; set; }
    }

    // ── StaveNote ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Full port of VexFlow's StaveNote class (stavenote.ts).
    ///
    /// Renders standard music notation notes including:
    /// - One or more noteheads at staff positions
    /// - An optional stem (up or down)
    /// - Ledger lines for notes above or below the staff
    /// - A flag for eighth notes and shorter when not beamed
    ///
    /// Formatter-dependent paths (ModifierContext, TickContext) are stubs.
    /// Phase 3 will integrate full formatting.
    ///
    /// Draw() call order exactly mirrors VexFlow:
    ///   1. Position noteheads at xBegin
    ///   2. Position stem x bounds
    ///   3. DrawLedgerLines (behind noteheads)
    ///   4. DrawStem (if has stem and not beamed)
    ///   5. DrawNoteHeads
    ///   6. DrawFlag
    /// </summary>
    public class StaveNote : StemmableNote
    {
        // ── Constants ──────────────────────────────────────────────────────────

        /// <summary>
        /// Category string for ModifierContext member dispatch.
        /// Port of VexFlow's Category.StaveNote from typeguard.ts.
        /// </summary>
        public const string CATEGORY = "stavenotes";

        /// <summary>
        /// Ledger line extension beyond notehead edge, in pixels.
        /// VexFlow default is 3; plan specifies 5.0 for this port.
        /// </summary>
        public const double LEDGER_LINE_OFFSET = 5.0;

        /// <summary>
        /// Minimum padding in pixels added to a note's width when it has no modifiers.
        /// Prevents notes from being packed too tightly when there are no accidentals/dots.
        /// Port of StaveNote.minNoteheadPadding from stavenote.ts — value from font metric 'noteHead.minPadding' = 2.
        /// </summary>
        public const double minNoteheadPadding = 2.0;

        /// <summary>Index of the outermost notehead for stem-up notes.</summary>
        public const int STEM_UP_UNIQUE_INDEX = 0;

        /// <summary>Index of the outermost notehead for stem-down notes.</summary>
        public const int STEM_DOWN_UNIQUE_INDEX = -1;

        // ── Fields ─────────────────────────────────────────────────────────────

        /// <summary>All noteheads for this note/chord, in key-index order.</summary>
        protected List<NoteHead> _noteHeads = new List<NoteHead>();

        /// <summary>Clef for this note (default "treble").</summary>
        protected string clef;

        /// <summary>Octave shift amount (positive = higher, negative = lower).</summary>
        protected int octaveShift;

        /// <summary>Key props sorted by line (ascending), with original indices for mapping.</summary>
        protected List<SortedKeyPropEntry> sortedKeyProps = new List<SortedKeyPropEntry>();

        /// <summary>Whether PreFormat() has run for this note.</summary>
        protected bool preFormatted = false;

        /// <summary>Whether any notehead is displaced due to chord collision.</summary>
        protected bool displaced;

        /// <summary>Dot shift for augmentation dots (in staff line units).</summary>
        protected double dotShiftY;

        /// <summary>Whether to use a default head x (for displaced ledger lines).</summary>
        protected bool useDefaultHeadX;

        /// <summary>Render options for this note.</summary>
        protected StaveNoteRenderOptions renderOptions;

        /// <summary>Custom style for ledger lines.</summary>
        protected ElementStyle? ledgerLineStyle;

        /// <summary>Minimum staff line across all noteheads (set in CalculateOptimalStemDirection).</summary>
        public double MinLine { get; private set; }

        /// <summary>Maximum staff line across all noteheads (set in CalculateOptimalStemDirection).</summary>
        public double MaxLine { get; private set; }

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Construct a StaveNote from the given struct.
        /// Port of StaveNote constructor from stavenote.ts.
        /// </summary>
        public StaveNote(StaveNoteStruct noteStruct) : base(noteStruct)
        {
            clef = noteStruct.Clef ?? "treble";
            octaveShift = noteStruct.OctaveShift ?? 0;

            displaced = false;
            dotShiftY = 0;
            useDefaultHeadX = false;

            renderOptions = new StaveNoteRenderOptions
            {
                GlyphFontScale = noteStruct.GlyphFontScale ?? Tables.NOTATION_FONT_SCALE,
                StrokePx = noteStruct.StrokePx ?? LEDGER_LINE_OFFSET,
                Draw = true,
            };

            // Build key properties from the key strings
            CalculateKeyProps();

            // Build stem (hidden for rests)
            SetStem(new Stem(new StemOptions { Hide = IsRest() }));

            // Set stem direction
            if (noteStruct.AutoStem == true)
            {
                AutoStem();
            }
            else
            {
                SetStemDirection(noteStruct.StemDirection ?? Stem.UP);
            }

            // Reset builds note heads and updates stem y bounds if stave is set
            Reset();

            // Build flag after stem direction is established
            BuildFlag();
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reset the note by rebuilding noteheads.
        /// Called from constructor and when key lines change.
        /// Port of StaveNote.reset() from stavenote.ts.
        /// </summary>
        protected void Reset()
        {
            BuildNoteHeads();

            if (stave != null)
            {
                SetStave(stave);
            }

            CalcNoteDisplacements();
        }

        // ── Key calculation ───────────────────────────────────────────────────

        /// <summary>
        /// Calculate key properties for each key string.
        /// Handles rest position overrides, octave shift, and adjacent-note displacement.
        /// Port of StaveNote.calculateKeyProps() from stavenote.ts.
        /// </summary>
        public void CalculateKeyProps()
        {
            double? lastLine = null;

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];

                // For rests, override position from glyphProps
                if (glyphProps.Rest)
                    glyphProps.Position = key;

                // Compute key properties including clef offset
                var options = new Dictionary<string, string>
                {
                    { "octave_shift", octaveShift.ToString() },
                    { "duration", duration }
                };
                var props = Tables.KeyProperties(key, clef, options);

                // Override rest line positions per VexFlow convention
                if (props.Key == "R")
                {
                    props.Line = (duration == "1" || duration == "w") ? 4.0 : 3.0;
                }

                // Track displacement for adjacent noteheads (half-step apart)
                double line = props.Line;
                if (lastLine == null)
                {
                    lastLine = line;
                }
                else
                {
                    if (Math.Abs(lastLine.Value - line) == 0.5)
                    {
                        displaced = true;
                        props.Displaced = true;

                        // Mark previous note as displaced too
                        if (keyProps.Count > 0)
                            keyProps[i - 1].Displaced = true;
                    }
                }

                lastLine = line;
                keyProps.Add(props);
            }

            // Build sorted key props (ascending by line)
            sortedKeyProps.Clear();
            for (int i = 0; i < keyProps.Count; i++)
                sortedKeyProps.Add(new SortedKeyPropEntry { KeyProps = keyProps[i], Index = i });
            sortedKeyProps.Sort((a, b) => a.KeyProps.Line.CompareTo(b.KeyProps.Line));
        }

        // ── Stem direction ─────────────────────────────────────────────────────

        /// <summary>
        /// Calculate the optimal stem direction from the average of min/max notehead lines.
        /// Port of StaveNote.calculateOptimalStemDirection() from stavenote.ts.
        /// </summary>
        public int CalculateOptimalStemDirection()
        {
            if (sortedKeyProps.Count == 0) return Stem.UP;

            MinLine = sortedKeyProps[0].KeyProps.Line;
            MaxLine = sortedKeyProps[sortedKeyProps.Count - 1].KeyProps.Line;

            const double MIDDLE_LINE = 3.0;
            double decider = (MinLine + MaxLine) / 2.0;
            return decider < MIDDLE_LINE ? Stem.UP : Stem.DOWN;
        }

        /// <summary>
        /// Auto-stem: choose stem direction based on note positions.
        /// Overrides StemmableNote.AutoStem() to use CalculateOptimalStemDirection().
        /// </summary>
        public new void AutoStem()
        {
            SetStemDirection(CalculateOptimalStemDirection());
        }

        // ── NoteHead building ─────────────────────────────────────────────────

        /// <summary>
        /// Build NoteHead objects for each key.
        /// Uses sortedKeyProps so that adjacent-note displacement is computed in
        /// the correct order (bottom-to-top for stem-up, top-to-bottom for stem-down).
        /// Port of StaveNote.buildNoteHeads() from stavenote.ts.
        /// </summary>
        public void BuildNoteHeads()
        {
            _noteHeads.Clear();
            // Pre-size to hold all note heads (indexed by original key index)
            for (int i = 0; i < keys.Length; i++)
                _noteHeads.Add(null!);

            int stemDir = GetStemDirection();

            double? lastLine = null;
            bool displacedHead = false;

            // For stem-up, iterate sortedKeyProps low-to-high; stem-down, high-to-low
            int start, end, step;
            if (stemDir == Stem.UP)
            {
                start = 0;
                end = sortedKeyProps.Count;
                step = 1;
            }
            else
            {
                start = sortedKeyProps.Count - 1;
                end = -1;
                step = -1;
            }

            for (int i = start; i != end; i += step)
            {
                var entry = sortedKeyProps[i];
                var noteProps = entry.KeyProps;
                double line = noteProps.Line;

                if (lastLine == null)
                {
                    lastLine = line;
                }
                else
                {
                    double lineDiff = Math.Abs(lastLine.Value - line);
                    if (lineDiff == 0.0 || lineDiff == 0.5)
                    {
                        displacedHead = !displacedHead;
                    }
                    else
                    {
                        displacedHead = false;
                        useDefaultHeadX = true;
                    }
                }
                lastLine = line;

                var notehead = new NoteHead(new NoteHeadStruct
                {
                    Duration = duration,
                    NoteType = noteType,
                    Displaced = displacedHead,
                    StemDirection = stemDir,
                    CustomGlyphCode = noteProps.Code,
                    GlyphFontScale = renderOptions.GlyphFontScale,
                    XShift = noteProps.ShiftRight ?? 0,
                    Line = noteProps.Line,
                });

                // Store in original-index slot
                _noteHeads[entry.Index] = notehead;
                AddChild(notehead);
            }
        }

        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>
        /// Return the category string for ModifierContext dispatch.
        /// Port of VexFlow's StaveNote.getCategory() from element.ts (via CATEGORY static).
        /// </summary>
        public override string GetCategory() => CATEGORY;

        // ── IsRest override ───────────────────────────────────────────────────

        /// <summary>
        /// True if this note is a rest. Checks glyphProps.Rest for StaveNote.
        /// Port of StaveNote.isRest() from stavenote.ts.
        /// </summary>
        public override bool IsRest() => glyphProps.Rest;

        /// <summary>True if this note is a chord (multiple keys, not a rest).</summary>
        public bool IsChord() => !IsRest() && keys.Length > 1;

        /// <summary>True if this note is displaced.</summary>
        public bool IsDisplaced() => displaced;

        /// <summary>Set whether this note is displaced.</summary>
        public StaveNote SetNoteDisplaced(bool d)
        {
            displaced = d;
            return this;
        }

        // ── Stem direction override ───────────────────────────────────────────

        /// <summary>
        /// Override SetStemDirection to also rebuild noteheads when direction changes.
        /// VexFlow's setStemDirection calls reset() which rebuilds noteheads with correct displacement.
        /// Port of StaveNote.setStemDirection() implicit behavior from stavenote.ts.
        /// </summary>
        public new StaveNote SetStemDirection(int direction)
        {
            base.SetStemDirection(direction);
            // Rebuild noteheads so displacement is recalculated for new stem direction
            Reset();
            return this;
        }

        // ── HasFlag override ──────────────────────────────────────────────────

        /// <summary>
        /// True if this note has a flag. Rests never have flags.
        /// Port of StaveNote.hasFlag() from stavenote.ts.
        /// </summary>
        public override bool HasFlag() => base.HasFlag() && !IsRest();

        // ── Stem X ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Get the x coordinate for the stem.
        /// For rests, returns the center glyph x.
        /// Port of StaveNote.getStemX() from stavenote.ts.
        /// </summary>
        public override double GetStemX()
        {
            if (noteType == "r")
                return GetCenterGlyphX();

            int dir = stemDirection;
            return base.GetStemX() + (dir != 0 ? Stem.WIDTH / (2.0 * -dir) : 0);
        }

        // ── Stem extension ────────────────────────────────────────────────────

        /// <summary>
        /// Get the stem extension adjusted for notes far from the middle line.
        /// Port of StaveNote.getStemExtension() from stavenote.ts.
        /// </summary>
        public override double GetStemExtension()
        {
            double superExtension = base.GetStemExtension();
            if (!glyphProps.Stem) return superExtension;

            int dir = GetStemDirection();
            if (dir != CalculateOptimalStemDirection()) return superExtension;

            const double MIDDLE_LINE = 3.0;
            double midLineDistance;
            if (dir == Stem.UP)
                midLineDistance = MIDDLE_LINE - MaxLine;
            else
                midLineDistance = MinLine - MIDDLE_LINE;

            double linesOverOctaveFromMidLine = midLineDistance - 3.5;
            if (linesOverOctaveFromMidLine <= 0) return superExtension;

            double spacingBetweenLines = 10.0;
            if (stave != null)
                spacingBetweenLines = stave.GetSpacingBetweenLines();

            return superExtension + linesOverOctaveFromMidLine * spacingBetweenLines;
        }

        // ── Stave attachment ──────────────────────────────────────────────────

        /// <summary>
        /// Attach this note to a stave, computing y positions for each notehead.
        /// Port of StaveNote.setStave() from stavenote.ts.
        /// </summary>
        public override Note SetStave(Stave newStave)
        {
            base.SetStave(newStave);

            // Compute y positions from stave for each notehead
            ys = new double[_noteHeads.Count];
            for (int i = 0; i < _noteHeads.Count; i++)
            {
                var nh = _noteHeads[i];
                if (nh == null) continue;

                nh.SetStave(newStave);
                ys[i] = nh.GetY();
            }

            // Set stem y bounds from head bounds
            if (stem != null)
            {
                var bounds = GetNoteHeadBounds();
                stem.SetYBounds(bounds.YTop, bounds.YBottom);
            }

            return this;
        }

        // ── Displacement calculation ──────────────────────────────────────────

        /// <summary>
        /// Calculate and store left/right displaced head pixel offsets.
        /// Port of StaveNote.calcNoteDisplacements() from stavenote.ts.
        /// </summary>
        public void CalcNoteDisplacements()
        {
            SetLeftDisplacedHeadPx(displaced && stemDirection == Stem.DOWN ? glyphProps.HeadWidth : 0);
            // For upstems with flags, extra space is unnecessary (taken up by flag)
            SetRightDisplacedHeadPx(!HasFlag() && displaced && stemDirection == Stem.UP ? glyphProps.HeadWidth : 0);
        }

        // ── PreFormat stub ────────────────────────────────────────────────────

        /// <summary>
        /// Pre-format stub. Sets preFormatted=true and computes basic width.
        /// No ModifierContext call per plan Pitfall 3 — formatter stubs for Phase 2.
        /// Port of StaveNote.preFormat() from stavenote.ts.
        /// </summary>
        public override void PreFormat()
        {
            // Call modifierContext.PreFormat() so it computes its left/right shift
            // (accidentals, dots, etc.). Only then can we determine noteHeadPadding.
            // Port of StaveNote.preFormat() from stavenote.ts.
            double noteHeadPadding = 0;
            if (modifierContext != null)
            {
                modifierContext.PreFormat();
                // If there are no modifiers (width == 0), add minimum padding to
                // prevent notes from being packed too tightly.
                if (modifierContext.GetWidth() == 0)
                    noteHeadPadding = minNoteheadPadding;
            }
            else
            {
                // No modifier context: always add minimum padding.
                noteHeadPadding = minNoteheadPadding;
            }

            double w = glyphProps.HeadWidth + leftDisplacedHeadPx + rightDisplacedHeadPx + noteHeadPadding;

            // For upward flagged notes, add extra width for the flag
            if (ShouldDrawFlag() && stemDirection == Stem.UP)
                w += glyphProps.HeadWidth;

            SetWidth(w);
            preFormatted = true;
        }

        /// <summary>Whether a flag should be drawn (has stem, has flag prop, no beam, not a rest).</summary>
        public bool ShouldDrawFlag()
        {
            return stem != null && HasFlag() && beam == null;
        }

        // ── Notehead x bounds ─────────────────────────────────────────────────

        /// <summary>
        /// Get the starting x coordinate for noteheads.
        /// Port of StaveNote.getNoteHeadBeginX() from stavenote.ts.
        /// </summary>
        public double GetNoteHeadBeginX()
        {
            return GetAbsoluteX() + xShift;
        }

        /// <summary>
        /// Get the ending x coordinate for noteheads.
        /// Port of StaveNote.getNoteHeadEndX() from stavenote.ts.
        /// </summary>
        public double GetNoteHeadEndX()
        {
            return GetNoteHeadBeginX() + glyphProps.HeadWidth;
        }

        // ── NoteHead bounds ───────────────────────────────────────────────────

        /// <summary>
        /// Get bounds for the highest and lowest noteheads.
        /// Used for stem y positioning and ledger line rendering.
        /// Port of StaveNote.getNoteHeadBounds() from stavenote.ts.
        /// </summary>
        public StaveNoteHeadBounds GetNoteHeadBounds()
        {
            var bounds = new StaveNoteHeadBounds
            {
                YTop = double.PositiveInfinity,
                YBottom = double.NegativeInfinity,
                HighestLine = stave?.GetNumLines() ?? 5,
                LowestLine = 1,
            };
            bounds.HighestNonDisplacedLine = bounds.HighestLine;
            bounds.LowestNonDisplacedLine = bounds.LowestLine;

            foreach (var nh in _noteHeads)
            {
                if (nh == null) continue;

                double line = nh.GetLine();
                double y = nh.GetY();

                if (y < bounds.YTop) bounds.YTop = y;
                if (y > bounds.YBottom) bounds.YBottom = y;

                if (bounds.DisplacedX == null && nh.IsDisplaced())
                    bounds.DisplacedX = nh.GetX();

                if (bounds.NonDisplacedX == null && !nh.IsDisplaced())
                    bounds.NonDisplacedX = nh.GetX();

                if (line > bounds.HighestLine) bounds.HighestLine = line;
                if (line < bounds.LowestLine) bounds.LowestLine = line;

                if (nh.IsDisplaced())
                {
                    bounds.HighestDisplacedLine = bounds.HighestDisplacedLine == null
                        ? line : Math.Max(line, bounds.HighestDisplacedLine.Value);
                    bounds.LowestDisplacedLine = bounds.LowestDisplacedLine == null
                        ? line : Math.Min(line, bounds.LowestDisplacedLine.Value);
                }
                else
                {
                    if (line > bounds.HighestNonDisplacedLine) bounds.HighestNonDisplacedLine = line;
                    if (line < bounds.LowestNonDisplacedLine) bounds.LowestNonDisplacedLine = line;
                }
            }

            return bounds;
        }

        // ── Line numbers ──────────────────────────────────────────────────────

        /// <summary>
        /// Get the line number for the bottom (or top) note in the chord.
        /// Port of StaveNote.getLineNumber() from stavenote.ts.
        /// </summary>
        public override double GetLineNumber(bool isTopNote = false)
        {
            if (keyProps.Count == 0)
                throw new VexFlowException("NoKeyProps", "Can't get bottom note line, note not initialized.");

            double resultLine = keyProps[0].Line;
            foreach (var kp in keyProps)
            {
                double line = kp.Line;
                if (isTopNote ? (line > resultLine) : (line < resultLine))
                    resultLine = line;
            }
            return resultLine;
        }

        /// <summary>
        /// Get the line for rests — midpoint between lowest and highest key lines.
        /// Port of StaveNote.getLineForRest() from stavenote.ts.
        /// </summary>
        public override double GetLineForRest()
        {
            if (keyProps.Count == 0) return 0;

            double restLine = keyProps[0].Line;
            if (keyProps.Count > 1)
            {
                double lastLine = keyProps[keyProps.Count - 1].Line;
                double top = Math.Max(restLine, lastLine);
                double bot = Math.Min(restLine, lastLine);
                restLine = (top + bot) / 2.0;
            }
            return restLine;
        }

        // ── Ledger line style ─────────────────────────────────────────────────

        /// <summary>Set the style for ledger lines.</summary>
        public void SetLedgerLineStyle(ElementStyle s) => ledgerLineStyle = s;

        /// <summary>Get the style for ledger lines (may be null).</summary>
        public ElementStyle? GetLedgerLineStyle() => ledgerLineStyle;

        // ── Beam access ───────────────────────────────────────────────────────

        /// <summary>Get the beam reference (null if not beamed).</summary>
        public VexFlowSharp.Common.Elements.Beam? GetBeam() => beam;

        /// <summary>
        /// Attach a beam and update note displacements and stem extension.
        /// Port of StaveNote.setBeam() from stavenote.ts.
        /// Overrides StemmableNote.SetBeam() to also call CalcNoteDisplacements.
        /// </summary>
        public new void SetBeam(VexFlowSharp.Common.Elements.Beam b)
        {
            beam = b;
            CalcNoteDisplacements();
            if (stem != null)
                stem.SetExtension(GetStemExtension());
        }

        // ── Key line get/set ──────────────────────────────────────────────────

        /// <summary>Get the staff line for the key at the given index.</summary>
        public override double GetKeyLine(int index) => keyProps[index].Line;

        /// <summary>
        /// Set the staff line for the key at the given index and reset.
        /// Port of StaveNote.setKeyLine() from stavenote.ts.
        /// </summary>
        public StaveNote SetKeyLine(int index, double line)
        {
            keyProps[index].Line = line;
            Reset();
            return this;
        }

        // ── Modifier start XY ─────────────────────────────────────────────────

        /// <summary>
        /// Returns the {x, y} start coordinates for a modifier at the given position and index.
        /// Port of VexFlow stavenote.ts getModifierStartXY().
        /// </summary>
        public override (double X, double Y) GetModifierStartXY(ModifierPosition position, int index, object? options = null)
        {
            if (!preFormatted) throw new System.InvalidOperationException("GetModifierStartXY: Note must be preformatted.");
            if (IsRest()) return (GetAbsoluteX(), _noteHeads.Count > 0 ? _noteHeads[0].GetY() : 0);

            double x = 0;
            if (position == ModifierPosition.Left)
                x = -2 + (_noteHeads.Count > 0 ? _noteHeads[0].GetX() : GetAbsoluteX());
            else if (position == ModifierPosition.Right)
                x = 2 + (_noteHeads.Count > 0 ? _noteHeads[_noteHeads.Count - 1].GetX() : GetAbsoluteX());
            else
                x = GetAbsoluteX();

            double y;
            if (position == ModifierPosition.Above)
                y = _noteHeads.Count > 0 ? _noteHeads[0].GetY() : 0;
            else if (position == ModifierPosition.Below)
                y = _noteHeads.Count > 0 ? _noteHeads[_noteHeads.Count - 1].GetY() : 0;
            else if (index < _noteHeads.Count)
                y = _noteHeads[index] != null ? _noteHeads[index].GetY() : 0;
            else
                y = _noteHeads.Count > 0 ? _noteHeads[_noteHeads.Count - 1].GetY() : 0;

            return (x, y);
        }

        // ── Tie anchor positions ──────────────────────────────────────────────

        /// <summary>
        /// Get the right x position for tie attachment (end of note head).
        /// Port of VexFlow stavenote.ts getTieRightX().
        /// </summary>
        public override double GetTieRightX()
        {
            double tieStartX = GetAbsoluteX();
            double xShiftPx  = GetXShift();
            return tieStartX + xShiftPx + glyphProps.HeadWidth + stave?.GetSpacingBetweenLines() / 4.0 ?? 0;
        }

        /// <summary>
        /// Get the left x position for tie attachment (start of note head).
        /// Port of VexFlow stavenote.ts getTieLeftX().
        /// </summary>
        public override double GetTieLeftX()
        {
            double tieStartX = GetAbsoluteX();
            double xShiftPx  = GetXShift();
            return tieStartX + xShiftPx;
        }

        /// <summary>
        /// Get the y position for a tie arc above the note (uses top notehead y).
        /// Port of VexFlow note.ts getTieYForTop() concept.
        /// </summary>
        public override double GetTieYForTop()
        {
            if (ys.Length == 0) return 0;
            double minY = ys[0];
            foreach (var y in ys) if (y < minY) minY = y;
            return minY;
        }

        /// <summary>
        /// Get the y position for a tie arc below the note (uses bottom notehead y).
        /// Port of VexFlow note.ts getTieYForBottom() concept.
        /// </summary>
        public override double GetTieYForBottom()
        {
            if (ys.Length == 0) return 0;
            double maxY = ys[0];
            foreach (var y in ys) if (y > maxY) maxY = y;
            return maxY;
        }

        // ── Voice shift width ─────────────────────────────────────────────────

        /// <summary>
        /// Get the voice shift width for formatter use.
        /// Port of StaveNote.getVoiceShiftWidth() from stavenote.ts.
        /// </summary>
        public double GetVoiceShiftWidth()
        {
            return glyphProps.HeadWidth * (displaced ? 2.0 : 1.0);
        }

        // ── Drawing helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Draw ledger lines for notes above or below the staff.
        ///
        /// Ledger lines are drawn before noteheads so they appear behind them.
        /// Port of StaveNote.drawLedgerLines() from stavenote.ts.
        /// </summary>
        public void DrawLedgerLines()
        {
            if (IsRest()) return;

            var staveRef = CheckStave();
            var ctx = CheckContext();

            var bounds = GetNoteHeadBounds();
            double highestLine = bounds.HighestLine;
            double lowestLine = bounds.LowestLine;

            // Early out if all noteheads are within the staff
            if (highestLine < 6 && lowestLine > 0) return;

            double strokePx = renderOptions.StrokePx;
            double headWidth = glyphProps.HeadWidth;
            double width = headWidth + strokePx * 2;
            double doubleWidth = 2.0 * (headWidth + strokePx) - Stem.WIDTH / 2.0;

            double minX = Math.Min(bounds.DisplacedX ?? 0, bounds.NonDisplacedX ?? 0);

            // Apply ledger line style (merge stave's default + this note's override)
            ElementStyle? mergedStyle = null;
            if (ledgerLineStyle != null)
                mergedStyle = ledgerLineStyle;

            if (mergedStyle != null) ApplyStyle(mergedStyle);

            void DrawLedgerLine(double y, bool normal, bool isDisplaced)
            {
                double lineX;
                if (isDisplaced && normal)
                    lineX = minX - strokePx;
                else if (normal)
                    lineX = (bounds.NonDisplacedX ?? 0) - strokePx;
                else
                    lineX = (bounds.DisplacedX ?? 0) - strokePx;

                double ledgerWidth = (normal && isDisplaced) ? doubleWidth : width;

                ctx.BeginPath();
                ctx.SetLineWidth(Tables.STAVE_LINE_THICKNESS);
                ctx.MoveTo(lineX, y);
                ctx.LineTo(lineX + ledgerWidth, y);
                ctx.Stroke();
            }

            // Ledger lines below staff: line >= 6 (below bottom line)
            for (double line = 6.0; line <= highestLine; line += 1.0)
            {
                bool normal = bounds.NonDisplacedX != null && line <= bounds.HighestNonDisplacedLine;
                bool isDisplaced = bounds.HighestDisplacedLine != null && line <= bounds.HighestDisplacedLine.Value;
                DrawLedgerLine(staveRef.GetYForNote(line), normal, isDisplaced);
            }

            // Ledger lines above staff: line <= 0 (above top line)
            for (double line = 0.0; line >= lowestLine; line -= 1.0)
            {
                bool normal = bounds.NonDisplacedX != null && line >= bounds.LowestNonDisplacedLine;
                bool isDisplaced = bounds.LowestDisplacedLine != null && line >= bounds.LowestDisplacedLine.Value;
                DrawLedgerLine(staveRef.GetYForNote(line), normal, isDisplaced);
            }

            if (mergedStyle != null) RestoreStyle(mergedStyle);
        }

        /// <summary>
        /// Draw all noteheads. Called after DrawLedgerLines so noteheads appear on top.
        /// Port of StaveNote.drawNoteHeads() from stavenote.ts.
        /// </summary>
        public void DrawNoteHeads()
        {
            var ctx = CheckContext();
            foreach (var nh in _noteHeads)
            {
                if (nh == null) continue;
                nh.SetContext(ctx).Draw();
            }
        }

        /// <summary>
        /// Draw the stem, adjusting its height if a flag will be drawn over it.
        /// Port of StaveNote.drawStem() from stavenote.ts.
        /// </summary>
        public new void DrawStem(StemOptions? stemOptions = null)
        {
            var ctx = CheckContext();

            if (stemOptions != null)
                SetStem(new Stem(stemOptions));

            // Shorten stem so the tip doesn't poke through the flag
            if (ShouldDrawFlag() && stem != null)
                stem.AdjustHeightForFlag();

            if (stem != null)
                stem.SetContext(ctx).Draw();
        }

        /// <summary>
        /// Draw the flag at the stem tip.
        /// Overrides StemmableNote.DrawFlag() to use VexFlow's exact flag y-position formula.
        /// Port of StaveNote.drawFlag() from stavenote.ts.
        /// </summary>
        public new void DrawFlag()
        {
            if (!ShouldDrawFlag()) return;
            if (flag == null || stem == null) return;

            var ctx = CheckContext();
            var bounds = GetNoteHeadBounds();
            double noteStemHeight = stem.GetHeight();
            double flagX = GetStemX();

            // The +/- 2 pushes the flag glyph outward so it covers the stem tip entirely.
            double flagY;
            if (GetStemDirection() == Stem.DOWN)
            {
                flagY = bounds.YTop - noteStemHeight + 2
                    - glyphProps.StemDownExtension * GetStaveNoteScale();
            }
            else
            {
                flagY = bounds.YBottom - noteStemHeight - 2
                    + glyphProps.StemUpExtension * GetStaveNoteScale();
            }

            flag.SetContext(ctx);
            flag.Render(ctx, flagX, flagY);
        }

        // ── Draw (main entry point) ────────────────────────────────────────────

        /// <summary>
        /// Draw the full StaveNote: noteheads, stem, ledger lines, flag.
        ///
        /// Call order EXACTLY mirrors VexFlow's stavenote.ts draw():
        ///   1. Validate (Draw option, Y values, context)
        ///   2. Position noteheads at xBegin
        ///   3. Set stem x bounds
        ///   4. DrawLedgerLines
        ///   5. DrawStem (if shouldRenderStem)
        ///   6. DrawNoteHeads
        ///   7. DrawFlag
        ///
        /// Port of StaveNote.draw() from stavenote.ts.
        /// </summary>
        public override void Draw()
        {
            if (!renderOptions.Draw) return;

            if (ys == null || ys.Length == 0)
                throw new VexFlowException("NoYValues", "Can't draw note without Y values.");

            var ctx = CheckContext();
            double xBegin = GetNoteHeadBeginX();
            bool shouldRenderStem = HasStem() && beam == null;

            // 1. Position all noteheads at xBegin
            foreach (var nh in _noteHeads)
            {
                if (nh != null) nh.SetX(xBegin);
            }

            // 2. Position stem x bounds
            if (stem != null)
            {
                double stemX = GetStemX();
                stem.SetNoteHeadXBounds(stemX, stemX);
            }

            // 3. Apply overall style, draw ledger lines (behind noteheads)
            ApplyStyle();
            DrawLedgerLines();

            // 4. Draw stem
            if (shouldRenderStem) DrawStem();

            // 5. Draw noteheads
            DrawNoteHeads();

            // 6. Draw flag
            DrawFlag();

            // 7. Draw modifiers (accidentals, dots, articulations, etc.)
            // Port of VexFlow stavenote.ts drawModifiers(): iterates this.modifiers
            // and calls modifier.setContext(ctx).draw() for each one.
            foreach (var modifier in modifiers)
            {
                modifier.SetContext(ctx);
                modifier.Draw();
            }

            RestoreStyle();
            rendered = true;
        }

        // ── Static Format (ModifierContext entry point) ───────────────────────

        /// <summary>
        /// Format notes inside a ModifierContext.
        /// Handles voice collision detection: shifts notes or rests when two voices
        /// share a stave and their noteheads overlap.
        ///
        /// Port of StaveNote.format() static method from stavenote.ts.
        /// Called by ModifierContext.PreFormat for the "stavenotes" member category.
        /// </summary>
        /// <param name="notes">List of StaveNote members in this modifier context.</param>
        /// <param name="state">Shared ModifierContextState to accumulate shifts into.</param>
        /// <returns>True if formatting was applied; false if fewer than 2 notes.</returns>
        public static bool Format(List<StaveNote>? notes, ModifierContextState state)
        {
            if (notes == null || notes.Count < 2) return false;

            var notesList = new List<StaveNoteFormatSettings>();

            for (int i = 0; i < notes.Count; i++)
            {
                var sortedProps = notes[i].sortedKeyProps;
                double line = sortedProps[0].KeyProps.Line;
                double minL = sortedProps[sortedProps.Count - 1].KeyProps.Line;
                int stemDir = notes[i].GetStemDirection();
                double stemMax = notes[i].GetStemLength() / 10.0;

                double maxL;
                if (notes[i].IsRest())
                {
                    maxL = line + notes[i].glyphProps.LineAbove;
                    minL = line - notes[i].glyphProps.LineBelow;
                }
                else
                {
                    maxL = stemDir == Stem.UP
                        ? sortedProps[sortedProps.Count - 1].KeyProps.Line + stemMax
                        : sortedProps[sortedProps.Count - 1].KeyProps.Line;
                    minL = stemDir == Stem.UP
                        ? sortedProps[0].KeyProps.Line
                        : sortedProps[0].KeyProps.Line - stemMax;
                }

                notesList.Add(new StaveNoteFormatSettings
                {
                    Line = line,
                    MaxLine = maxL,
                    MinLine = minL,
                    IsRest = notes[i].IsRest(),
                    StemDirection = stemDir,
                    VoiceShift = notes[i].GetVoiceShiftWidth(),
                    IsDisplaced = notes[i].IsDisplaced(),
                    Note = notes[i],
                });
            }

            // Determine how many visible notes we have (up to 3)
            var draw = new bool[3];
            for (int i = 0; i < Math.Min(3, notesList.Count); i++)
                draw[i] = notesList[i].Note.renderOptions.Draw;

            StaveNoteFormatSettings? noteU = null;
            StaveNoteFormatSettings? noteM = null;
            StaveNoteFormatSettings? noteL = null;
            int voices = 0;

            if (draw[0] && draw[1] && notesList.Count >= 3 && draw[2])
            {
                voices = 3;
                noteU = notesList[0];
                noteM = notesList[1];
                noteL = notesList[2];
            }
            else if (draw[0] && draw[1])
            {
                voices = 2;
                noteU = notesList[0];
                noteL = notesList[1];
            }
            else if (draw[0] && notesList.Count >= 3 && draw[2])
            {
                voices = 2;
                noteU = notesList[0];
                noteL = notesList[2];
            }
            else if (notesList.Count >= 3 && draw[1] && draw[2])
            {
                voices = 2;
                noteU = notesList[1];
                noteL = notesList[2];
            }
            else
            {
                // Fewer than 2 visible notes — no shift needed
                return true;
            }

            // For two voices: ensure upper voice is stem-up for backward compatibility
            if (voices == 2 && noteU!.StemDirection == Stem.DOWN && noteL!.StemDirection == Stem.UP)
            {
                var tmp = noteU;
                noteU = noteL;
                noteL = tmp;
            }

            double voiceXShift = Math.Max(noteU!.VoiceShift, noteL!.VoiceShift);
            double xShift = 0;

            if (voices == 2)
            {
                double lineSpacing = (noteU.Note.HasStem() && noteL.Note.HasStem() &&
                    noteU.StemDirection == noteL.StemDirection) ? 0.0 : 0.5;

                if (noteL.IsRest && noteU.IsRest && noteU.Note.duration == noteL.Note.duration)
                {
                    // Same-duration rests: hide lower voice rest
                    noteL.Note.renderOptions.Draw = false;
                }
                else if (noteU.MinLine <= noteL.MaxLine + lineSpacing)
                {
                    if (noteU.IsRest)
                    {
                        ShiftRestVertical(noteU, noteL, 1);
                    }
                    else if (noteL.IsRest)
                    {
                        ShiftRestVertical(noteL, noteU, -1);
                    }
                    else
                    {
                        double lineDiff = Math.Abs(noteU.Line - noteL.Line);
                        if (noteU.Note.HasStem() && noteL.Note.HasStem())
                        {
                            // Notes with stems: offset one if they collide
                            if (noteU.StemDirection == noteL.StemDirection)
                            {
                                xShift = voiceXShift + 2;
                                noteU.Note.SetXShift(xShift);
                            }
                            else
                            {
                                xShift = voiceXShift + 2;
                                noteL.Note.SetXShift(xShift);
                            }
                        }
                        else if (lineDiff < 1)
                        {
                            xShift = voiceXShift + 2;
                            if (string.Compare(noteU.Note.duration, noteL.Note.duration, StringComparison.Ordinal) < 0)
                                noteU.Note.SetXShift(xShift);
                            else
                                noteL.Note.SetXShift(xShift);
                        }
                        else if (noteU.Note.HasStem())
                        {
                            int newDir = -noteU.Note.GetStemDirection();
                            noteU.StemDirection = newDir;
                            noteU.Note.SetStemDirection(newDir);
                        }
                        else if (noteL.Note.HasStem())
                        {
                            int newDir = -noteL.Note.GetStemDirection();
                            noteL.StemDirection = newDir;
                            noteL.Note.SetStemDirection(newDir);
                        }
                    }
                }

                state.RightShift += xShift;
                return true;
            }

            // Three voices
            if (noteM == null)
                throw new VexFlowException("InvalidState", "noteM not defined for three-voice format.");

            // Special case 1: middle voice rest between two notes
            if (noteM.IsRest && !noteU.IsRest && !noteL.IsRest)
            {
                if (noteU.MinLine <= noteM.MaxLine || noteM.MinLine <= noteL.MaxLine)
                {
                    double restHeight = noteM.MaxLine - noteM.MinLine;
                    double space = noteU.MinLine - noteL.MaxLine;
                    if (restHeight < space)
                    {
                        CenterRest(noteM, noteU, noteL);
                    }
                    else
                    {
                        xShift = voiceXShift + 2;
                        noteM.Note.SetXShift(xShift);
                        if (noteL.Note.GetBeam() == null)
                        {
                            noteL.StemDirection = Stem.DOWN;
                            noteL.Note.SetStemDirection(Stem.DOWN);
                        }
                        if (noteU.MinLine <= noteL.MaxLine && noteU.Note.GetBeam() == null)
                        {
                            noteU.StemDirection = Stem.UP;
                            noteU.Note.SetStemDirection(Stem.UP);
                        }
                    }
                    state.RightShift += xShift;
                    return true;
                }
            }

            // Special case 2: all voices are rests
            if (noteU.IsRest && noteM.IsRest && noteL.IsRest)
            {
                noteU.Note.renderOptions.Draw = false;
                noteL.Note.renderOptions.Draw = false;
                state.RightShift += xShift;
                return true;
            }

            // Handle remaining rest repositioning
            if (noteM.IsRest && noteU.IsRest && noteM.MinLine <= noteL.MaxLine)
                noteM.Note.renderOptions.Draw = false;
            if (noteM.IsRest && noteL.IsRest && noteU.MinLine <= noteM.MaxLine)
                noteM.Note.renderOptions.Draw = false;
            if (noteU.IsRest && noteU.MinLine <= noteM.MaxLine)
                ShiftRestVertical(noteU, noteM, 1);
            if (noteL.IsRest && noteM.MinLine <= noteL.MaxLine)
                ShiftRestVertical(noteL, noteM, -1);

            // If middle voice intersects upper or lower
            if (noteU.MinLine <= noteM.MaxLine + 0.5 || noteM.MinLine <= noteL.MaxLine)
            {
                xShift = voiceXShift + 2;
                noteM.Note.SetXShift(xShift);
                if (noteL.Note.GetBeam() == null)
                {
                    noteL.StemDirection = Stem.DOWN;
                    noteL.Note.SetStemDirection(Stem.DOWN);
                }
                if (noteU.MinLine <= noteL.MaxLine && noteU.Note.GetBeam() == null)
                {
                    noteU.StemDirection = Stem.UP;
                    noteU.Note.SetStemDirection(Stem.UP);
                }
            }

            state.RightShift += xShift;
            return true;
        }

        /// <summary>
        /// Post-format all notes in the list by calling each note's PostFormat().
        /// Port of StaveNote.postFormat() static method from stavenote.ts.
        /// </summary>
        public static bool PostFormat(List<StaveNote> notes)
        {
            if (notes == null) return false;
            foreach (var note in notes)
                note.PostFormat();
            return true;
        }

        // ── Format helpers (file-private) ─────────────────────────────────────

        private static void ShiftRestVertical(StaveNoteFormatSettings rest, StaveNoteFormatSettings note, int dir)
        {
            rest.Line += dir;
            rest.MaxLine += dir;
            rest.MinLine += dir;
            rest.Note.SetKeyLine(0, rest.Note.GetKeyLine(0) + dir);
        }

        private static double MidLine(double a, double b) => (a + b) / 2.0;

        private static void CenterRest(StaveNoteFormatSettings rest,
            StaveNoteFormatSettings noteU, StaveNoteFormatSettings noteL)
        {
            double delta = rest.Line - MidLine(noteU.MinLine, noteL.MaxLine);
            rest.Note.SetKeyLine(0, rest.Note.GetKeyLine(0) - delta);
            rest.Line -= delta;
            rest.MaxLine -= delta;
            rest.MinLine -= delta;
        }

        // ── NoteHead accessor ─────────────────────────────────────────────────

        /// <summary>Get a copy of the notehead list.</summary>
        public List<NoteHead> GetNoteHeads() => new List<NoteHead>(_noteHeads);
    }

    // ── StaveNoteFormatSettings (internal helper) ────────────────────────────

    /// <summary>
    /// Format settings for a single StaveNote within a ModifierContext.
    /// Port of VexFlow's StaveNoteFormatSettings interface from stavenote.ts.
    /// </summary>
    internal class StaveNoteFormatSettings
    {
        public double Line { get; set; }
        public double MaxLine { get; set; }
        public double MinLine { get; set; }
        public bool IsRest { get; set; }
        public int StemDirection { get; set; }
        public double VoiceShift { get; set; }
        public bool IsDisplaced { get; set; }
        public StaveNote Note { get; set; } = null!;
    }
}

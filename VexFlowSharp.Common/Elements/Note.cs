#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using System.Linq;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Properties describing width and offset metrics for a note.
    /// Port of VexFlow's NoteMetrics interface from note.ts.
    /// </summary>
    public class NoteMetrics
    {
        /// <summary>Total width including modifiers.</summary>
        public double Width { get; set; }

        /// <summary>Glyph width (note head only).</summary>
        public double GlyphWidth { get; set; }

        /// <summary>Width of note head pixels.</summary>
        public double NotePx { get; set; }

        /// <summary>Start X for left modifiers.</summary>
        public double ModLeftPx { get; set; }

        /// <summary>Start X for right modifiers.</summary>
        public double ModRightPx { get; set; }

        /// <summary>Extra space on left for displaced note head.</summary>
        public double LeftDisplacedHeadPx { get; set; }

        /// <summary>Extra space on right for displaced note head.</summary>
        public double RightDisplacedHeadPx { get; set; }

        /// <summary>Glyph pixels.</summary>
        public double GlyphPx { get; set; }
    }

    /// <summary>
    /// Input structure for constructing a Note.
    /// Port of VexFlow's NoteStruct interface from note.ts.
    /// </summary>
    public class NoteStruct
    {
        /// <summary>Duration string (e.g., "4" = quarter, "8" = eighth, "4r" = quarter rest).</summary>
        public string Duration { get; set; } = "4";

        /// <summary>Array of pitch strings (e.g., ["c/4", "e/4", "g/4"]).</summary>
        public string[]? Keys { get; set; }

        /// <summary>Note type override (e.g., "r" = rest, "n" = normal, "x" = X-notehead).</summary>
        public string? Type { get; set; }

        /// <summary>Number of augmentation dots.</summary>
        public int? Dots { get; set; }

        /// <summary>Whether to automatically determine stem direction.</summary>
        public bool? AutoStem { get; set; }

        /// <summary>Manual stem direction: Stem.UP or Stem.DOWN.</summary>
        public int? StemDirection { get; set; }

        /// <summary>Clef for this note (default "treble").</summary>
        public string? Clef { get; set; }

        /// <summary>Octave shift (positive = higher, negative = lower).</summary>
        public int? OctaveShift { get; set; }

        /// <summary>Glyph font scale override.</summary>
        public double? GlyphFontScale { get; set; }

        /// <summary>Stroke pixels for ledger lines.</summary>
        public double? StrokePx { get; set; }
    }

    /// <summary>
    /// Abstract base class for all notes and chords rendered on a stave.
    /// Port of VexFlow's Note class from note.ts.
    ///
    /// Inherits from Tickable, which provides tick/duration, width, modifiers, and xShift.
    /// </summary>
    public abstract class Note : Tickable
    {
        public new const string CATEGORY = "Note";

        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>Glyph properties for this note's duration and type.</summary>
        protected GlyphProps glyphProps;

        /// <summary>Raw key strings (e.g., "c/4", "f#/5").</summary>
        protected string[] keys;

        /// <summary>Computed per-key properties (line, octave, accidental, etc.).</summary>
        protected List<KeyProps> keyProps;

        /// <summary>Associated stave (null until SetStave is called).</summary>
        protected Stave? stave;

        /// <summary>Duration base string (e.g., "4", "8", "2").</summary>
        protected string duration;

        /// <summary>Note type string (e.g., "n" = normal, "r" = rest, "x" = X-notehead).</summary>
        protected string noteType;

        /// <summary>Number of augmentation dots.</summary>
        protected int dots;

        public override string GetCategory() => CATEGORY;

        /// <summary>Y coordinates for each note head in this note/chord.</summary>
        protected double[] ys;

        /// <summary>Tuplets this note belongs to, ordered from outermost to innermost.</summary>
        protected List<Tuplet> tupletStack = new List<Tuplet>();

        /// <summary>The innermost tuplet this note belongs to, if any.</summary>
        protected Tuplet? tuplet;

        /// <summary>Manual X position (used when tickContext is null).</summary>
        protected double x = 0;

        /// <summary>Extra pixels on the left side for displaced note heads.</summary>
        protected double leftDisplacedHeadPx = 0;

        /// <summary>Extra pixels on the right side for displaced note heads.</summary>
        protected double rightDisplacedHeadPx = 0;

        /// <summary>
        /// Constructor: parse the NoteStruct and initialize all fields.
        /// Port of Note constructor from note.ts.
        /// </summary>
        protected Note(NoteStruct noteStruct)
        {
            // 1. Parse duration — strip type suffix if present (e.g., "4r" → "4")
            string rawDuration = noteStruct.Duration ?? "4";
            string parsedType = "n";
            int parsedDots = noteStruct.Dots ?? 0;

            // Parse duration string: optional digits/fraction, optional dots 'd', optional type char
            // Format: <duration>[d*][type]  e.g. "4", "4r", "8", "8d", "2dd"
            string durStr = rawDuration;
            // Extract type suffix (last non-digit character that is a valid type)
            // NOTE: 'd' is intentionally excluded — in a duration string, trailing 'd's mean dots,
            // not the diamond note type. Diamond type must be set via NoteStruct.Type = "d".
            string validTypes = "nrhmsxg";
            if (durStr.Length > 0)
            {
                char lastChar = durStr[durStr.Length - 1];
                if (validTypes.IndexOf(lastChar) >= 0)
                {
                    parsedType = lastChar.ToString();
                    durStr = durStr.Substring(0, durStr.Length - 1);
                }
            }

            // Count and remove trailing 'd' characters (dot notation)
            while (durStr.Length > 0 && durStr[durStr.Length - 1] == 'd')
            {
                parsedDots++;
                durStr = durStr.Substring(0, durStr.Length - 1);
            }

            // Type override from NoteStruct
            if (noteStruct.Type != null) parsedType = noteStruct.Type;

            // Sanitize and validate duration
            duration = Tables.SanitizeDuration(durStr.Length > 0 ? durStr : "4");

            noteType = parsedType;

            // Store keys
            keys = noteStruct.Keys ?? System.Array.Empty<string>();
            keyProps = new List<KeyProps>();

            // Compute ticks: base ticks + dotted additions
            int baseTicks = Tables.DurationToTicks(duration);
            int totalTicks = baseTicks;
            int currentTicks = baseTicks;
            dots = parsedDots;
            for (int i = 0; i < dots; i++)
            {
                currentTicks = currentTicks / 2;
                totalTicks += currentTicks;
            }
            ticks = new Fraction(totalTicks, 1);
            intrinsicTicks = totalTicks;

            // Get glyph props
            glyphProps = Tables.GetGlyphProps(duration, noteType);

            // Initialize ys
            ys = System.Array.Empty<double>();
        }

        // ── Key access ────────────────────────────────────────────────────────

        /// <summary>Get the raw key strings for this note.</summary>
        public string[] GetKeys() => keys;

        /// <summary>Get the computed KeyProps for each pitch.</summary>
        public List<KeyProps> GetKeyProps() => keyProps;

        // ── Duration access ───────────────────────────────────────────────────

        /// <summary>Get the duration string (e.g., "4", "8").</summary>
        public string GetDuration() => duration;

        public override int GetIntrinsicTicks() => intrinsicTicks;

        /// <summary>Get the note type (e.g., "n", "r", "x").</summary>
        public string GetNoteType() => noteType;

        /// <summary>Get the number of augmentation dots.</summary>
        public int GetDots() => dots;

        /// <summary>Returns true if this is a rest (noteType == "r").</summary>
        public virtual bool IsRest() => noteType == "r";

        /// <summary>Get the glyph properties for this note.</summary>
        public GlyphProps GetGlyphProps() => glyphProps;

        // ── Y positions ───────────────────────────────────────────────────────

        /// <summary>Get the Y positions for each note head.</summary>
        public double[] GetYs()
        {
            if (ys.Length == 0)
                throw new VexFlowException("NoYValues", "No Y-values calculated for this note.");
            return ys;
        }

        /// <summary>Set the Y positions for each note head.</summary>
        public Note SetYs(double[] newYs)
        {
            ys = newYs;
            return this;
        }

        /// <summary>
        /// Get the Y position for top text at the given text line.
        /// Uses the minimum Y value minus one line distance per text level.
        /// </summary>
        public virtual double GetYForTopText(double textLine)
        {
            if (ys.Length == 0) return 0;
            double minY = ys[0];
            foreach (var y in ys) if (y < minY) minY = y;
            return minY - Tables.STAVE_LINE_DISTANCE * (textLine + 1);
        }

        /// <summary>
        /// Get the Y position for bottom text at the given text line.
        /// Uses the maximum Y value plus one line distance per text level.
        /// </summary>
        public virtual double GetYForBottomText(double textLine)
        {
            if (ys.Length == 0) return 0;
            double maxY = ys[0];
            foreach (var y in ys) if (y > maxY) maxY = y;
            return maxY + Tables.STAVE_LINE_DISTANCE * (textLine + 1);
        }

        // ── X positioning ─────────────────────────────────────────────────────

        /// <summary>Set the manual X position.</summary>
        public Note SetX(double newX)
        {
            x = newX;
            return this;
        }

        /// <summary>Get the manual X position.</summary>
        public double GetX() => x;

        /// <summary>
        /// Get the absolute X position of this note.
        /// When a TickContext is set, returns tickContext.GetX() + stave.getNoteStartX() + xShift
        /// (matching VexFlow's note.ts getAbsoluteX() which adds the stave note-start offset).
        /// Without a TickContext, falls back to the manual x position.
        /// </summary>
        public virtual double GetAbsoluteX()
        {
            var tc = GetTickContext();
            if (tc != null)
            {
                double absX = tc.GetX();
                // Add stave noteStartX + stave padding (matches VexFlow note.ts getAbsoluteX)
                if (stave != null)
                    absX += stave.GetNoteStartX() + Metrics.GetDouble("Stave.padding");
                return absX + xShift;
            }
            return x;
        }

        // ── Stave attachment ──────────────────────────────────────────────────

        /// <summary>
        /// Set the associated stave. Updates render context if stave has one.
        /// Port of Note.setStave() from note.ts.
        /// </summary>
        public virtual Note SetStave(Stave newStave)
        {
            stave = newStave;
            // Only inherit context if the stave has one (stave may be context-less in unit tests)
            try { SetContext(newStave.CheckContext()); }
            catch { /* stave has no context yet — note will get context on Draw() */ }
            return this;
        }

        /// <summary>Get the associated stave (may be null).</summary>
        public Stave? GetStave() => stave;

        /// <summary>Get the associated stave; throws if not set.</summary>
        public Stave CheckStave()
            => stave ?? throw new VexFlowException("NoStave", "No stave attached to instance.");

        // ── Modifier management ───────────────────────────────────────────────

        /// <summary>
        /// Add a modifier to this note at the given key index.
        /// Sets the note reference on the modifier, and registers it with the
        /// ModifierContext if the note already has one (matches VexFlow note.ts addModifier()).
        /// </summary>
        public Note AddModifier(Modifier mod, int index = 0)
        {
            mod.SetNote(this);
            mod.SetIndex(index);
            modifiers.Add(mod);
            // Register with the modifier context if already assigned (matches VexFlow note.ts addModifier)
            modifierContext?.RegisterMember(mod);
            return this;
        }

        /// <summary>Associate this note with a tuplet, preserving insertion order.</summary>
        public Note SetTuplet(Tuplet tuplet)
        {
            if (!tupletStack.Contains(tuplet))
            {
                tupletStack.Add(tuplet);
                ApplyTickMultiplier(tuplet.GetNotesOccupied(), tuplet.GetNoteCount());
            }
            this.tuplet = tuplet;
            return this;
        }

        /// <summary>Remove this note's association with a tuplet.</summary>
        public Note ResetTuplet(Tuplet? tuplet = null)
        {
            if (tuplet != null)
            {
                if (tupletStack.Remove(tuplet))
                    ApplyTickMultiplier(tuplet.GetNoteCount(), tuplet.GetNotesOccupied());
            }
            else
            {
                while (tupletStack.Count > 0)
                {
                    var current = tupletStack[tupletStack.Count - 1];
                    tupletStack.RemoveAt(tupletStack.Count - 1);
                    ApplyTickMultiplier(current.GetNoteCount(), current.GetNotesOccupied());
                }
            }
            this.tuplet = tupletStack.Count > 0 ? tupletStack[tupletStack.Count - 1] : null;
            return this;
        }

        /// <summary>Get a copy of the tuplets this note belongs to.</summary>
        public List<Tuplet> GetTupletStack() => new List<Tuplet>(tupletStack);

        /// <summary>Get the innermost tuplet this note belongs to.</summary>
        public override object? GetTuplet() => tuplet;

        // ── Note metrics ──────────────────────────────────────────────────────

        /// <summary>
        /// Get note metrics for this note (used by TickContext.PreFormat).
        /// Override of Tickable.GetNoteMetrics().
        /// Port of note.ts getMetrics() — includes modifier context left/right shift.
        /// </summary>
        public override NoteMetrics GetNoteMetrics()
        {
            // Include modifier context state so TickContext allocates space for accidentals/dots.
            double modLeft  = modifierContext?.GetState().LeftShift  ?? 0;
            double modRight = modifierContext?.GetState().RightShift ?? 0;
            double formattedWidth = GetWidth();
            double notePx = formattedWidth - modLeft - modRight - leftDisplacedHeadPx - rightDisplacedHeadPx;

            return new NoteMetrics
            {
                Width                = formattedWidth,
                GlyphWidth           = glyphProps.HeadWidth,
                NotePx               = notePx,
                ModLeftPx            = modLeft,
                ModRightPx           = modRight,
                LeftDisplacedHeadPx  = leftDisplacedHeadPx,
                RightDisplacedHeadPx = rightDisplacedHeadPx,
                GlyphPx              = glyphProps.HeadWidth,
            };
        }

        /// <summary>
        /// Get metrics for this note.
        /// Port of note.ts getMetrics() — includes modifier context left/right shift.
        /// </summary>
        public new NoteMetrics GetMetrics()
        {
            double modLeft  = modifierContext?.GetState().LeftShift  ?? 0;
            double modRight = modifierContext?.GetState().RightShift ?? 0;
            double formattedWidth = GetWidth();
            double notePx = formattedWidth - modLeft - modRight - leftDisplacedHeadPx - rightDisplacedHeadPx;

            return new NoteMetrics
            {
                Width                = formattedWidth,
                GlyphWidth           = glyphProps.HeadWidth,
                NotePx               = notePx,
                ModLeftPx            = modLeft,
                ModRightPx           = modRight,
                LeftDisplacedHeadPx  = leftDisplacedHeadPx,
                RightDisplacedHeadPx = rightDisplacedHeadPx,
                GlyphPx              = glyphProps.HeadWidth,
            };
        }

        // ── Displaced head pixels ─────────────────────────────────────────────

        /// <summary>Get left displaced head pixels.</summary>
        public double GetLeftDisplacedHeadPx() => leftDisplacedHeadPx;

        /// <summary>Set left displaced head pixels.</summary>
        public Note SetLeftDisplacedHeadPx(double px) { leftDisplacedHeadPx = px; return this; }

        /// <summary>Get right displaced head pixels.</summary>
        public double GetRightDisplacedHeadPx() => rightDisplacedHeadPx;

        /// <summary>Set right displaced head pixels.</summary>
        public Note SetRightDisplacedHeadPx(double px) { rightDisplacedHeadPx = px; return this; }

        // ── Stem direction (base implementation — throws) ─────────────────────

        /// <summary>
        /// Returns true if this note has a stem.
        /// Base returns false — StemmableNote overrides to return true when a stem is present.
        /// Port of VexFlow's Note.hasStem() from note.ts.
        /// </summary>
        public virtual bool HasStem() => false;

        /// <summary>Get the stem direction. Throws NoStem for notes without stems.</summary>
        public virtual int GetStemDirection()
        {
            throw new VexFlowException("NoStem", "No stem attached to this note.");
        }

        // ── Line numbers ──────────────────────────────────────────────────────

        /// <summary>Get the stave line number for this note (default 0).</summary>
        public virtual double GetLineNumber(bool isTopNote = false) => 0;

        /// <summary>Get the stave line for rests (default 0).</summary>
        public virtual double GetLineForRest() => 0;

        /// <summary>
        /// Get the staff line for the key at the given index.
        /// Returns 0 by default; override in StaveNote to return keyProps[index].Line.
        /// </summary>
        public virtual double GetKeyLine(int index) => 0;

        // ── Dot positioning ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the x position (relative to note left) where the first dot should start.
        /// Port of VexFlow note.ts getFirstDotPx().
        /// Returns rightDisplacedHeadPx by default; override for parenthesis padding.
        /// </summary>
        public virtual double GetRightParenthesisPx(int index)
        {
            var props = GetKeyProps()[index];
            return props.Displaced ? GetRightDisplacedHeadPx() : 0;
        }

        public virtual double GetLeftParenthesisPx(int index)
        {
            var props = GetKeyProps()[index];
            return props.Displaced ? GetLeftDisplacedHeadPx() - GetXShift() : -GetXShift();
        }

        public virtual double GetFirstDotPx()
        {
            double px = rightDisplacedHeadPx;
            var parentheses = GetModifierContext()?.GetMembers(Parenthesis.CATEGORY);

            if (parentheses != null && parentheses.Count != 0 && parentheses[0] is Parenthesis parenthesis)
                px += parenthesis.GetWidth() + 1;

            return px;
        }

        // ── Modifier start XY ─────────────────────────────────────────────────

        /// <summary>
        /// Returns the {x, y} start coordinates for a modifier at the given position and index.
        /// Port of VexFlow note.ts getModifierStartXY().
        /// Override in StaveNote for position-aware coordinates.
        /// </summary>
        public virtual (double X, double Y) GetModifierStartXY(ModifierPosition position, int index, object? options = null)
        {
            return (GetAbsoluteX(), 0);
        }

        // ── Tie anchor positions ──────────────────────────────────────────────

        /// <summary>
        /// Get the right x position for tie attachment (end of note head).
        /// Virtual stub — StaveNote overrides for accurate notehead positions.
        /// Port of VexFlow note.ts getTieRightX().
        /// </summary>
        public virtual double GetTieRightX() => GetAbsoluteX();

        /// <summary>
        /// Get the left x position for tie attachment (start of note head).
        /// Virtual stub — StaveNote overrides for accurate notehead positions.
        /// Port of VexFlow note.ts getTieLeftX().
        /// </summary>
        public virtual double GetTieLeftX() => GetAbsoluteX();

        /// <summary>
        /// Get the y position for a tie arc above the note.
        /// Virtual stub — StaveNote overrides using stave line positions.
        /// </summary>
        public virtual double GetTieYForTop() => ys.Length > 0 ? ys[0] : 0;

        /// <summary>
        /// Get the y position for a tie arc below the note.
        /// Virtual stub — StaveNote overrides using stave line positions.
        /// </summary>
        public virtual double GetTieYForBottom() => ys.Length > 0 ? ys[ys.Length - 1] : 0;
    }
}

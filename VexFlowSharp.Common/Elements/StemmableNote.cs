#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Linq;
using VexFlowSharp.Common.Elements;

namespace VexFlowSharp
{
    /// <summary>
    /// Abstract base class for notes with optional stems and flags.
    /// Extends Note with stem management, flag rendering, and auto-stemming logic.
    ///
    /// Examples of stemmable notes: StaveNote, TabNote, GraceNote.
    /// Port of VexFlow's StemmableNote class from stemmablenote.ts.
    /// </summary>
    public abstract class StemmableNote : Note
    {
        public new const string CATEGORY = "StemmableNote";

        public override string GetCategory() => CATEGORY;

        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>The stem for this note (null until BuildStem is called).</summary>
        protected Stem? stem;

        /// <summary>The flag glyph for this note (null until BuildFlag is called).</summary>
        protected Glyph? flag;

        /// <summary>Current stem direction: Stem.UP (1) or Stem.DOWN (-1).</summary>
        protected int stemDirection = Stem.UP;

        /// <summary>Override for the stem extension length (null = use glyphProps value).</summary>
        protected double? stemExtensionOverride;

        /// <summary>Whether this note can be beamed.</summary>
        protected bool beamable;

        /// <summary>The Beam this note belongs to (null if not beamed).</summary>
        protected Beam? beam;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Construct a StemmableNote from the given NoteStruct.
        /// </summary>
        protected StemmableNote(NoteStruct noteStruct) : base(noteStruct)
        {
        }

        // ── Stem management ───────────────────────────────────────────────────

        /// <summary>Whether this note has a stem (based on glyphProps).</summary>
        public override bool HasStem() => glyphProps.Stem;

        /// <summary>Get the stem (may be null if BuildStem not called yet).</summary>
        public Stem? GetStem() => stem;

        /// <summary>Get the stem; throw if not built.</summary>
        public Stem CheckStem()
            => stem ?? throw new VexFlowException("NoStem", "No stem attached to instance.");

        /// <summary>Set the stem and add it as a child element.</summary>
        public StemmableNote SetStem(Stem s)
        {
            stem = s;
            AddChild(s);
            return this;
        }

        /// <summary>Build and attach a new Stem to this note.</summary>
        public StemmableNote BuildStem()
        {
            SetStem(new Stem());
            return this;
        }

        /// <summary>
        /// Build the flag glyph for this note if it has a flag.
        /// (Eighth notes and shorter that are not beamed.)
        /// </summary>
        public void BuildFlag()
        {
            if (HasFlag())
            {
                string flagCode = stemDirection == Stem.DOWN
                    ? glyphProps.CodeFlagDownStem
                    : glyphProps.CodeFlagUpStem;

                if (!string.IsNullOrEmpty(flagCode))
                {
                    double scale = Tables.NOTATION_FONT_SCALE;
                    try
                    {
                        flag = new Flag(flagCode, scale);
                    }
                    catch
                    {
                        flag = null;
                    }
                }
            }
        }

        /// <summary>Whether this note has a flag (has flag props and no beam).</summary>
        public virtual bool HasFlag() => glyphProps.Flag;

        // ── Stem direction ────────────────────────────────────────────────────

        /// <summary>Get the current stem direction.</summary>
        public override int GetStemDirection() => stemDirection;

        /// <summary>
        /// Set the stem direction (Stem.UP or Stem.DOWN).
        /// Also updates the attached stem if it exists.
        /// </summary>
        public StemmableNote SetStemDirection(int direction)
        {
            if (direction != Stem.UP && direction != Stem.DOWN)
                throw new VexFlowException("BadArgument", $"Invalid stem direction: {direction}");

            stemDirection = direction;

            if (stem != null)
            {
                stem.SetDirection(direction);
                stem.SetExtension(GetStemExtension());
            }

            BuildFlag();
            return this;
        }

        // ── Stem geometry ─────────────────────────────────────────────────────

        /// <summary>
        /// Get the x coordinate for the stem.
        /// UP stem attaches at the right edge of the glyph; DOWN at the left edge.
        /// </summary>
        public virtual double GetStemX()
        {
            double xBegin = GetAbsoluteX() + xShift;
            double xEnd = xBegin + glyphProps.HeadWidth;
            // xShift already included in xBegin; adjust to absolute
            return stemDirection == Stem.DOWN ? xBegin : xEnd;
        }

        /// <summary>Get the centre of the glyph's x extent.</summary>
        public double GetCenterGlyphX()
        {
            return GetAbsoluteX() + xShift + glyphProps.HeadWidth / 2.0;
        }

        /// <summary>
        /// Get the stem extension for the current direction.
        /// Returns the override if set, otherwise reads from glyphProps.
        /// </summary>
        public virtual double GetStemExtension()
        {
            if (stemExtensionOverride.HasValue)
                return stemExtensionOverride.Value;

            if (beam != null)
                return glyphProps.StemBeamExtension * GetStaveNoteScale();

            return stemDirection == Stem.UP
                ? glyphProps.StemUpExtension
                : glyphProps.StemDownExtension;
        }

        /// <summary>
        /// Override the stem extension to achieve a specific total stem length.
        /// </summary>
        public StemmableNote SetStemLength(double height)
        {
            stemExtensionOverride = height - Stem.HEIGHT;
            return this;
        }

        /// <summary>Get the full stem length: Stem.HEIGHT + extension.</summary>
        public double GetStemLength() => Stem.HEIGHT + GetStemExtension();

        /// <summary>Get the stem extents (topY and baseY) from the attached stem.</summary>
        public (double TopY, double BaseY) GetStemExtents()
        {
            if (stem == null)
                throw new VexFlowException("NoStem", "No stem attached to this note.");
            return stem.GetExtents();
        }

        // ── Auto-stemming ─────────────────────────────────────────────────────

        /// <summary>
        /// Automatically determine stem direction based on the average staff line of all note heads.
        /// Notes with average line below 3 (median line) get stem UP; above 3 get stem DOWN.
        ///
        /// Port of VexFlow's StemmableNote.autoStem() — uses median line 3.0.
        /// </summary>
        public void AutoStem()
        {
            if (keyProps.Count == 0)
            {
                SetStemDirection(Stem.UP);
                return;
            }

            double avgLine = 0;
            foreach (var kp in keyProps) avgLine += kp.Line;
            avgLine /= keyProps.Count;

            // If average line is at or above 3 (middle of treble staff), stem DOWN; else UP
            SetStemDirection(avgLine >= 3 ? Stem.DOWN : Stem.UP);
        }

        // ── Drawing helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Draw a stem using the given options.
        /// Creates a new Stem, attaches it, and renders it.
        /// </summary>
        public void DrawStem(StemOptions options)
        {
            CheckContext();
            rendered = true;

            var s = new Stem(options);
            SetStem(s);
            s.SetContext(CheckContext()).Draw();
        }

        /// <summary>
        /// Draw the flag glyph at the stem tip.
        /// Only call after BuildFlag() and when stem Y positions are set.
        /// </summary>
        public void DrawFlag()
        {
            if (flag == null || stem == null) return;

            var ctx = CheckContext();
            var (topY, _) = stem.GetExtents();
            double flagX = GetStemX();

            flag.SetContext(ctx);
            flag.Render(ctx, flagX, topY);
        }

        /// <summary>
        /// Get the scale for rendering note heads.
        /// Returns 1.0 for normal notes; overridden to 0.66 for GraceNote.
        /// </summary>
        public virtual double GetStaveNoteScale() => 1.0;

        /// <summary>Get the minimum required stem length for this duration.</summary>
        public double GetStemMinimumLength()
        {
            switch (duration)
            {
                case "1": case "1/2": return 0;
                case "8":  return 35;
                case "16": return 35;
                case "32": return 45;
                case "64": return 50;
                case "128": return 55;
                default: return 20;
            }
        }

        /// <summary>Get the beam count for this note's duration.</summary>
        public int GetBeamCount() => glyphProps.BeamCount;

        // ── Beam attachment ───────────────────────────────────────────────────

        /// <summary>
        /// Attach a Beam to this note. Called by Beam constructor.
        /// Port of StemmableNote.setBeam() from stemmablenote.ts.
        /// </summary>
        public void SetBeam(Beam b)
        {
            beam = b;
        }

        /// <summary>
        /// Returns true if this note is part of a beam.
        /// Port of StemmableNote.hasBeam() from stemmablenote.ts.
        /// </summary>
        public bool HasBeam() => beam != null;
    }
}

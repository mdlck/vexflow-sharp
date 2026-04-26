#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Properties for a rendered glyph (note head, rest, etc.).
    /// Port of VexFlow's GlyphProps interface from glyph.ts.
    /// </summary>
    public class GlyphProps
    {
        /// <summary>Note head width in pixels.</summary>
        public double HeadWidth { get; set; } = 10.0;

        /// <summary>SMuFL glyph code for the note head.</summary>
        public string CodeHead { get; set; } = "";

        /// <summary>SMuFL glyph code for this entry.</summary>
        public string Code { get; set; } = "";

        /// <summary>SMuFL glyph code for an up-stem flag.</summary>
        public string CodeFlagUpStem { get; set; } = "";

        /// <summary>SMuFL glyph code for a down-stem flag.</summary>
        public string CodeFlagDownStem { get; set; } = "";

        /// <summary>Whether this entry has a stem.</summary>
        public bool Stem { get; set; } = false;

        /// <summary>Whether this entry has a flag.</summary>
        public bool Flag { get; set; } = false;

        /// <summary>Whether this entry represents a rest.</summary>
        public bool Rest { get; set; } = false;

        /// <summary>Number of beams for beamed notes.</summary>
        public int BeamCount { get; set; } = 0;

        /// <summary>Extension of stem above the note (pixels).</summary>
        public double StemUpExtension { get; set; } = 0;

        /// <summary>Extension of stem below the note (pixels).</summary>
        public double StemDownExtension { get; set; } = 0;

        /// <summary>Vertical shift for dots (staff lines).</summary>
        public double DotShiftY { get; set; } = 0;

        /// <summary>Number of ledger lines above the note to stroke.</summary>
        public double LineAbove { get; set; } = 0;

        /// <summary>Number of ledger lines below the note to stroke.</summary>
        public double LineBelow { get; set; } = 0;

        /// <summary>Default staff position string (e.g., "B/4").</summary>
        public string Position { get; set; } = "";

        /// <summary>Horizontal shift for specially positioned noteheads.</summary>
        public double ShiftRight { get; set; } = 0;
    }

    /// <summary>
    /// Properties computed for a specific key/octave/clef combination.
    /// Port of VexFlow's KeyProps interface from note.ts.
    /// </summary>
    public class KeyProps
    {
        /// <summary>Uppercase note name (e.g., "C", "F#").</summary>
        public string Key { get; set; } = "";

        /// <summary>Octave number.</summary>
        public int Octave { get; set; }

        /// <summary>Staff line number (0 = first line, 0.5 = first space).</summary>
        public double Line { get; set; }

        /// <summary>Chromatic integer value (octave * 12 + semitone). Null for non-pitched notes.</summary>
        public int? IntValue { get; set; }

        /// <summary>Accidental string (e.g., "#", "b", "n") or null.</summary>
        public string? Accidental { get; set; }

        /// <summary>SMuFL code override (for note types like X-noteheads).</summary>
        public string? Code { get; set; }

        /// <summary>Stroke direction: 1 = up, -1 = down, 0 = none.</summary>
        public int Stroke { get; set; }

        /// <summary>Right-shift amount for special noteheads.</summary>
        public double? ShiftRight { get; set; }

        /// <summary>Whether this note head is displaced (chord collision avoidance).</summary>
        public bool Displaced { get; set; }
    }

    /// <summary>
    /// Properties for an articulation SMuFL glyph.
    /// Port of VexFlow's ArticulationStruct interface from tables.ts.
    /// </summary>
    public class ArticulationStruct
    {
        /// <summary>SMuFL glyph code (used when a single glyph covers both above/below).</summary>
        public string? Code { get; set; }

        /// <summary>SMuFL glyph code used when the articulation is placed above the note.</summary>
        public string? AboveCode { get; set; }

        /// <summary>SMuFL glyph code used when the articulation is placed below the note.</summary>
        public string? BelowCode { get; set; }

        /// <summary>Whether this articulation may sit between staff lines (true) or must sit outside (false).</summary>
        public bool BetweenLines { get; set; }
    }

    /// <summary>
    /// Central lookup table for all VexFlow music notation constants and mappings.
    /// Port of VexFlow's Tables class (tables.ts).
    ///
    /// All dictionaries are initialized in the static constructor to avoid
    /// C# static field initialization order pitfalls.
    /// </summary>
    public static class Tables
    {
        // ── Constants ─────────────────────────────────────────────────────────

        /// <summary>Ticks per whole note.</summary>
        public const int RESOLUTION = 16384;

        /// <summary>Stem width in pixels.</summary>
        public const double STEM_WIDTH = 1.5;

        /// <summary>Default stem height in pixels.</summary>
        public const double STEM_HEIGHT = 35;

        /// <summary>Stave line thickness in pixels.</summary>
        public const double STAVE_LINE_THICKNESS = 1;

        /// <summary>Vertical distance between stave lines in pixels.</summary>
        public const double STAVE_LINE_DISTANCE = 10;

        /// <summary>Scale factor for notation font rendering.</summary>
        public const double NOTATION_FONT_SCALE = 39;

        /// <summary>Width of a slash notehead in pixels.</summary>
        public const double SLASH_NOTEHEAD_WIDTH = 15;

        /// <summary>Number of chromatic tones in an octave.</summary>
        public const int NUM_TONES = 12;

        /// <summary>
        /// Softmax factor for voice layout proportionality.
        /// Port of Tables.SOFTMAX_FACTOR from tables.ts.
        /// </summary>
        public const double SOFTMAX_FACTOR = 10;

        /// <summary>
        /// Default 4/4 time signature descriptor.
        /// Port of Tables.TIME4_4 from tables.ts.
        /// </summary>
        public static readonly Dictionary<string, int> TIME4_4 = new Dictionary<string, int>
        {
            { "numBeats",   4 },
            { "beatValue",  4 },
            { "resolution", RESOLUTION },
        };

        // ── Accidental modifier spacing constants (from common_metrics.ts) ──────

        /// <summary>Padding between a notehead and accidentals to its left.</summary>
        public const double ACCIDENTAL_NOTEHEAD_PADDING = 1.0;

        /// <summary>Additional padding to the left of all accidentals in a chord cluster.</summary>
        public const double ACCIDENTAL_LEFT_PADDING = 2.0;

        /// <summary>Horizontal spacing between adjacent accidental columns.</summary>
        public const double ACCIDENTAL_SPACING = 3.0;

        // ── Stave padding constants (from common_metrics.ts) ──────────────────

        /// <summary>Left padding inside the stave before the first note.</summary>
        public const double STAVE_PADDING = 12;

        /// <summary>Minimum end padding inside the stave after the last note.</summary>
        public const double STAVE_END_PADDING_MIN = 5;

        /// <summary>Maximum end padding inside the stave after the last note.</summary>
        public const double STAVE_END_PADDING_MAX = 10;

        /// <summary>Padding added for notes that are not part of the main formatter pass.</summary>
        public const double UNALIGNED_NOTE_PADDING = 10;

        // ── Static dictionaries ───────────────────────────────────────────────

        private static readonly Dictionary<string, int> _durations;
        private static readonly Dictionary<string, string> _durationAliases;
        private static readonly Dictionary<string, (string? Acc, int Num)> _keySignatures;
        private static readonly Dictionary<string, int> _clefs;
        private static readonly Dictionary<string, (int Index, int? IntVal, string? Accidental, bool IsRest, int? Octave, string? Code, double? ShiftRight)> _notesInfo;
        private static readonly Dictionary<int, string> _integerToNoteMap;

        // ── AccidentalColumnsTable (port of vexflow/src/tables.ts lines 436–466) ─────

        /// <summary>
        /// Layout table for accidental stagger columns.
        /// Key: number of simultaneous accidentals (1–6).
        /// Value: dictionary of case-name to column-assignment arrays.
        /// Port of VexFlow's accidentalColumnsTable from tables.ts.
        /// </summary>
        public static readonly Dictionary<int, Dictionary<string, int[]>> AccidentalColumnsTable =
            new Dictionary<int, Dictionary<string, int[]>>
            {
                [1] = new Dictionary<string, int[]>
                {
                    ["a"] = new[] { 1 },
                    ["b"] = new[] { 1 },
                },
                [2] = new Dictionary<string, int[]>
                {
                    ["a"] = new[] { 1, 2 },
                },
                [3] = new Dictionary<string, int[]>
                {
                    ["a"]                = new[] { 1, 3, 2 },
                    ["b"]                = new[] { 1, 2, 1 },
                    ["second_on_bottom"] = new[] { 1, 2, 3 },
                },
                [4] = new Dictionary<string, int[]>
                {
                    ["a"]                     = new[] { 1, 3, 4, 2 },
                    ["b"]                     = new[] { 1, 2, 3, 1 },
                    ["spaced_out_tetrachord"] = new[] { 1, 2, 1, 2 },
                },
                [5] = new Dictionary<string, int[]>
                {
                    ["a"]                          = new[] { 1, 3, 5, 4, 2 },
                    ["b"]                          = new[] { 1, 2, 4, 3, 1 },
                    ["spaced_out_pentachord"]      = new[] { 1, 2, 3, 2, 1 },
                    ["very_spaced_out_pentachord"] = new[] { 1, 2, 1, 2, 1 },
                },
                [6] = new Dictionary<string, int[]>
                {
                    ["a"]                         = new[] { 1, 3, 5, 6, 4, 2 },
                    ["b"]                         = new[] { 1, 2, 4, 5, 3, 1 },
                    ["spaced_out_hexachord"]      = new[] { 1, 3, 2, 1, 3, 2 },
                    ["very_spaced_out_hexachord"] = new[] { 1, 2, 1, 2, 1, 2 },
                },
            };

        // Duration code tables — each duration maps to common + type-specific GlyphProps
        private static readonly Dictionary<string, GlyphProps> _durationCommon;
        private static readonly Dictionary<string, GlyphProps> _durationRest;

        /// <summary>Static constructor initializes all lookup dictionaries.</summary>
        static Tables()
        {
            // ── Durations ──────────────────────────────────────────────────────
            _durations = new Dictionary<string, int>
            {
                { "1/2", RESOLUTION * 2 },
                { "1",   RESOLUTION / 1 },
                { "2",   RESOLUTION / 2 },
                { "4",   RESOLUTION / 4 },
                { "8",   RESOLUTION / 8 },
                { "16",  RESOLUTION / 16 },
                { "32",  RESOLUTION / 32 },
                { "64",  RESOLUTION / 64 },
                { "128", RESOLUTION / 128 },
                { "256", RESOLUTION / 256 },
            };

            _durationAliases = new Dictionary<string, string>
            {
                { "w", "1" },
                { "h", "2" },
                { "q", "4" },
                { "b", "256" },  // bar note, no-op
            };

            // ── Key signatures ─────────────────────────────────────────────────
            _keySignatures = new Dictionary<string, (string?, int)>
            {
                { "C",   (null, 0) },
                { "Am",  (null, 0) },
                { "F",   ("b", 1) },
                { "Dm",  ("b", 1) },
                { "Bb",  ("b", 2) },
                { "Gm",  ("b", 2) },
                { "Eb",  ("b", 3) },
                { "Cm",  ("b", 3) },
                { "Ab",  ("b", 4) },
                { "Fm",  ("b", 4) },
                { "Db",  ("b", 5) },
                { "Bbm", ("b", 5) },
                { "Gb",  ("b", 6) },
                { "Ebm", ("b", 6) },
                { "Cb",  ("b", 7) },
                { "Abm", ("b", 7) },
                { "G",   ("#", 1) },
                { "Em",  ("#", 1) },
                { "D",   ("#", 2) },
                { "Bm",  ("#", 2) },
                { "A",   ("#", 3) },
                { "F#m", ("#", 3) },
                { "E",   ("#", 4) },
                { "C#m", ("#", 4) },
                { "B",   ("#", 5) },
                { "G#m", ("#", 5) },
                { "F#",  ("#", 6) },
                { "D#m", ("#", 6) },
                { "C#",  ("#", 7) },
                { "A#m", ("#", 7) },
            };

            // ── Clefs ─────────────────────────────────────────────────────────
            // line_shift values from tables.ts
            _clefs = new Dictionary<string, int>
            {
                { "treble",      0 },
                { "bass",        6 },
                { "tenor",       4 },
                { "alto",        3 },
                { "soprano",     1 },
                { "percussion",  0 },
                { "mezzo-soprano", 2 },
                { "baritone-c",  5 },
                { "baritone-f",  5 },
                { "subbass",     7 },
                { "french",     -1 },
            };

            // ── Notes info ────────────────────────────────────────────────────
            // (index, int_val?, accidental?, rest, octave?, code?, shift_right?)
            _notesInfo = new Dictionary<string, (int, int?, string?, bool, int?, string?, double?)>(StringComparer.Ordinal)
            {
                { "C",   (0, 0,    null, false, null, null, null) },
                { "CN",  (0, 0,    "n",  false, null, null, null) },
                { "C#",  (0, 1,    "#",  false, null, null, null) },
                { "C##", (0, 2,    "##", false, null, null, null) },
                { "CB",  (0, 11,   "b",  false, null, null, null) },
                { "CBB", (0, 10,   "bb", false, null, null, null) },
                { "D",   (1, 2,    null, false, null, null, null) },
                { "DN",  (1, 2,    "n",  false, null, null, null) },
                { "D#",  (1, 3,    "#",  false, null, null, null) },
                { "D##", (1, 4,    "##", false, null, null, null) },
                { "DB",  (1, 1,    "b",  false, null, null, null) },
                { "DBB", (1, 0,    "bb", false, null, null, null) },
                { "E",   (2, 4,    null, false, null, null, null) },
                { "EN",  (2, 4,    "n",  false, null, null, null) },
                { "E#",  (2, 5,    "#",  false, null, null, null) },
                { "E##", (2, 6,    "##", false, null, null, null) },
                { "EB",  (2, 3,    "b",  false, null, null, null) },
                { "EBB", (2, 2,    "bb", false, null, null, null) },
                { "F",   (3, 5,    null, false, null, null, null) },
                { "FN",  (3, 5,    "n",  false, null, null, null) },
                { "F#",  (3, 6,    "#",  false, null, null, null) },
                { "F##", (3, 7,    "##", false, null, null, null) },
                { "FB",  (3, 4,    "b",  false, null, null, null) },
                { "FBB", (3, 3,    "bb", false, null, null, null) },
                { "G",   (4, 7,    null, false, null, null, null) },
                { "GN",  (4, 7,    "n",  false, null, null, null) },
                { "G#",  (4, 8,    "#",  false, null, null, null) },
                { "G##", (4, 9,    "##", false, null, null, null) },
                { "GB",  (4, 6,    "b",  false, null, null, null) },
                { "GBB", (4, 5,    "bb", false, null, null, null) },
                { "A",   (5, 9,    null, false, null, null, null) },
                { "AN",  (5, 9,    "n",  false, null, null, null) },
                { "A#",  (5, 10,   "#",  false, null, null, null) },
                { "A##", (5, 11,   "##", false, null, null, null) },
                { "AB",  (5, 8,    "b",  false, null, null, null) },
                { "ABB", (5, 7,    "bb", false, null, null, null) },
                { "B",   (6, 11,   null, false, null, null, null) },
                { "BN",  (6, 11,   "n",  false, null, null, null) },
                { "B#",  (6, 12,   "#",  false, null, null, null) },
                { "B##", (6, 13,   "##", false, null, null, null) },
                { "BB",  (6, 10,   "b",  false, null, null, null) },
                { "BBB", (6, 9,    "bb", false, null, null, null) },
                { "R",   (6, null, null, true,  null, null, null) },
                { "X",   (6, null, "",   false, 4,    "noteheadXBlack", 5.5) },
            };

            // ── Integer-to-note map ────────────────────────────────────────────
            _integerToNoteMap = new Dictionary<int, string>
            {
                { 0,  "C"  },
                { 1,  "C#" },
                { 2,  "D"  },
                { 3,  "D#" },
                { 4,  "E"  },
                { 5,  "F"  },
                { 6,  "F#" },
                { 7,  "G"  },
                { 8,  "G#" },
                { 9,  "A"  },
                { 10, "A#" },
                { 11, "B"  },
            };

            // ── Duration glyph data (common properties) ───────────────────────
            _durationCommon = new Dictionary<string, GlyphProps>
            {
                { "1/2", new GlyphProps { Stem = false, Flag = false, StemUpExtension = -STEM_HEIGHT, StemDownExtension = -STEM_HEIGHT, DotShiftY = 0, LineAbove = 0, LineBelow = 0 } },
                { "1",   new GlyphProps { Stem = false, Flag = false, StemUpExtension = -STEM_HEIGHT, StemDownExtension = -STEM_HEIGHT, DotShiftY = 0, LineAbove = 0, LineBelow = 0 } },
                { "2",   new GlyphProps { Stem = true,  Flag = false, StemUpExtension = 0, StemDownExtension = 0, DotShiftY = 0, LineAbove = 0, LineBelow = 0 } },
                { "4",   new GlyphProps { Stem = true,  Flag = false, StemUpExtension = 0, StemDownExtension = 0, DotShiftY = 0, LineAbove = 0, LineBelow = 0 } },
                { "8",   new GlyphProps { Stem = true,  Flag = true,  BeamCount = 1, CodeFlagUpStem = "flag8thUp",   CodeFlagDownStem = "flag8thDown",   StemUpExtension = 0, StemDownExtension = 0, DotShiftY = 0, LineAbove = 0, LineBelow = 0 } },
                { "16",  new GlyphProps { Stem = true,  Flag = true,  BeamCount = 2, CodeFlagUpStem = "flag16thUp",  CodeFlagDownStem = "flag16thDown",  StemUpExtension = 0, StemDownExtension = 0, DotShiftY = 0, LineAbove = 0, LineBelow = 0 } },
                { "32",  new GlyphProps { Stem = true,  Flag = true,  BeamCount = 3, CodeFlagUpStem = "flag32ndUp",  CodeFlagDownStem = "flag32ndDown",  StemUpExtension = 9,  StemDownExtension = 9,  DotShiftY = 0, LineAbove = 0, LineBelow = 0 } },
                { "64",  new GlyphProps { Stem = true,  Flag = true,  BeamCount = 4, CodeFlagUpStem = "flag64thUp",  CodeFlagDownStem = "flag64thDown",  StemUpExtension = 13, StemDownExtension = 13, DotShiftY = 0, LineAbove = 0, LineBelow = 0 } },
                { "128", new GlyphProps { Stem = true,  Flag = true,  BeamCount = 5, CodeFlagUpStem = "flag128thUp", CodeFlagDownStem = "flag128thDown", StemUpExtension = 22, StemDownExtension = 22, DotShiftY = 0, LineAbove = 0, LineBelow = 0 } },
                { "256", new GlyphProps { Stem = true,  Flag = true,  BeamCount = 6, CodeFlagUpStem = "flag256thUp", CodeFlagDownStem = "flag256thDown", StemUpExtension = 24, StemDownExtension = 24, DotShiftY = 0, LineAbove = 0, LineBelow = 0 } },
            };

            // Rest code_head lookup per duration
            _durationRest = new Dictionary<string, GlyphProps>
            {
                { "1/2", new GlyphProps { CodeHead = "restDoubleWhole", Rest = true, Position = "B/5", DotShiftY = 0 } },
                { "1",   new GlyphProps { CodeHead = "restWhole",        Rest = true, Position = "D/5", DotShiftY = 0.5 } },
                { "2",   new GlyphProps { CodeHead = "restHalf",         Rest = true, Stem = false, Position = "B/4", DotShiftY = -0.5 } },
                { "4",   new GlyphProps { CodeHead = "restQuarter",      Rest = true, Stem = false, Position = "B/4", DotShiftY = -0.5, LineAbove = 1.5, LineBelow = 1.5 } },
                { "8",   new GlyphProps { CodeHead = "rest8th",          Rest = true, Stem = false, Position = "B/4", DotShiftY = -0.5, LineAbove = 1.0, LineBelow = 1.0 } },
                { "16",  new GlyphProps { CodeHead = "rest16th",         Rest = true, Stem = false, Position = "B/4", DotShiftY = -0.5, LineAbove = 1.0, LineBelow = 2.0 } },
                { "32",  new GlyphProps { CodeHead = "rest32nd",         Rest = true, Stem = false, Position = "B/4", DotShiftY = -1.5, LineAbove = 2.0, LineBelow = 2.0 } },
                { "64",  new GlyphProps { CodeHead = "rest64th",         Rest = true, Stem = false, Position = "B/4", DotShiftY = -1.5, LineAbove = 2.0, LineBelow = 3.0 } },
                { "128", new GlyphProps { CodeHead = "rest128th",        Rest = true, Stem = false, Position = "B/4", DotShiftY = -2.5, LineAbove = 3.0, LineBelow = 3.0 } },
                { "256", new GlyphProps { CodeHead = "rest256th",        Rest = true, Stem = false, Position = "B/4", DotShiftY = -2.5, LineAbove = 3.0, LineBelow = 3.0 } },
            };
        }

        // ── Duration methods ──────────────────────────────────────────────────

        /// <summary>
        /// Resolve duration aliases and validate. Returns canonical duration string.
        /// "q" -> "4", "w" -> "1", etc.
        /// </summary>
        public static string SanitizeDuration(string duration)
        {
            if (_durationAliases.TryGetValue(duration, out var alias))
                duration = alias;
            if (!_durations.ContainsKey(duration))
                throw new VexFlowException("BadArguments", $"The provided duration is not valid: {duration}");
            return duration;
        }

        /// <summary>
        /// Convert a duration string to its tick count.
        /// </summary>
        public static int DurationToTicks(string duration)
        {
            duration = SanitizeDuration(duration);
            return _durations[duration];
        }

        // ── Clef methods ──────────────────────────────────────────────────────

        /// <summary>
        /// Return the line_shift for a clef name.
        /// </summary>
        public static int ClefLineShift(string clef)
        {
            if (!_clefs.TryGetValue(clef, out var shift))
                throw new VexFlowException("BadArgument", $"Invalid clef: {clef}");
            return shift;
        }

        // ── Key signature methods ─────────────────────────────────────────────

        /// <summary>
        /// Return the list of accidentals for a key signature.
        /// Each entry is (Type: "#" or "b", Line: staff line position).
        /// Returns an empty list for C major / A minor.
        /// </summary>
        public static List<(string Type, double Line)> KeySignature(string spec)
        {
            if (!_keySignatures.TryGetValue(spec, out var keySpec))
                throw new VexFlowException("BadKeySignature", $"Bad key signature spec: '{spec}'");

            var result = new List<(string, double)>();
            if (keySpec.Acc == null) return result;

            // Accidental line positions from tables.ts
            var accidentalLines = new Dictionary<string, double[]>
            {
                { "b",  new double[] { 2, 0.5, 2.5, 1, 3, 1.5, 3.5 } },
                { "#",  new double[] { 0, 1.5, -0.5, 1, 2.5, 0.5, 2 } },
            };

            var notes = accidentalLines[keySpec.Acc];
            for (int i = 0; i < keySpec.Num; i++)
                result.Add((keySpec.Acc, notes[i]));
            return result;
        }

        /// <summary>Whether the given key signature spec is known.</summary>
        public static bool HasKeySignature(string spec) => _keySignatures.ContainsKey(spec);

        // ── Key properties method ─────────────────────────────────────────────

        /// <summary>
        /// Compute note layout properties from a "key/octave" string (e.g., "c/4").
        /// Port of Tables.keyProperties() from tables.ts.
        /// </summary>
        public static KeyProps KeyProperties(string keyOctaveGlyph, string clef = "treble",
            Dictionary<string, string>? params_ = null)
        {
            var pieces = keyOctaveGlyph.Split('/');
            if (pieces.Length < 2)
                throw new VexFlowException("BadArguments",
                    $"First argument must be note/octave or note/octave/glyph-code: {keyOctaveGlyph}");

            var key = pieces[0].ToUpperInvariant();
            if (!_notesInfo.TryGetValue(key, out var value))
                throw new VexFlowException("BadArguments", $"Invalid key name: {key}");

            // If the note overrides its own octave (e.g., X noteheads), use that.
            if (value.Octave.HasValue) pieces[1] = value.Octave.Value.ToString();

            if (!int.TryParse(pieces[1], out var octave))
                throw new VexFlowException("BadArguments", $"Invalid octave: {pieces[1]}");

            int octaveShift = 0;
            if (params_ != null && params_.TryGetValue("octave_shift", out var shiftStr))
                int.TryParse(shiftStr, out octaveShift);
            octave -= octaveShift;

            var baseIndex = octave * 7 - 4 * 7;
            double line = (baseIndex + value.Index) / 2.0;
            line += ClefLineShift(clef);

            int stroke = 0;
            if (line <= 0 && (line * 2) % 2 == 0) stroke = 1;
            if (line >= 6 && (line * 2) % 2 == 0) stroke = -1;

            int? intValue = value.IntVal.HasValue ? octave * 12 + value.IntVal.Value : (int?)null;

            // Custom notehead from third piece of the key string
            string? codeOverride = null;
            if (pieces.Length > 2 && !string.IsNullOrEmpty(pieces[2]))
            {
                var duration = params_ != null && params_.TryGetValue("duration", out var d) ? d : "4";
                duration = SanitizeDuration(duration);
                codeOverride = CodeNoteHead(pieces[2].ToUpperInvariant(), duration);
                if (codeOverride == "") codeOverride = null;
            }

            return new KeyProps
            {
                Key        = key,
                Octave     = octave,
                Line       = line,
                IntValue   = intValue,
                Accidental = value.Accidental,
                Code       = codeOverride ?? value.Code,
                Stroke     = stroke,
                ShiftRight = value.ShiftRight,
                Displaced  = false,
            };
        }

        // ── Note head code method ─────────────────────────────────────────────

        /// <summary>
        /// Return the SMuFL glyph code for a given note head type and duration.
        /// Port of Tables.codeNoteHead() from tables.ts.
        /// type is already uppercased by callers.
        /// </summary>
        public static string CodeNoteHead(string type, string duration)
        {
            switch (type)
            {
                case "D0": return "noteheadDiamondWhole";
                case "D1": return "noteheadDiamondHalf";
                case "D2": return "noteheadDiamondBlack";
                case "D3": return "noteheadDiamondBlack";

                case "T0": return "noteheadTriangleUpWhole";
                case "T1": return "noteheadTriangleUpHalf";
                case "T2": return "noteheadTriangleUpBlack";
                case "T3": return "noteheadTriangleUpBlack";

                case "X0": return "noteheadXWhole";
                case "X1": return "noteheadXHalf";
                case "X2": return "noteheadXBlack";
                case "X3": return "noteheadCircleX";

                case "S1": return "noteheadSquareWhite";
                case "S2": return "noteheadSquareBlack";

                case "R1": return "vexNoteHeadRectWhite";
                case "R2": return "vexNoteHeadRectBlack";

                case "DO":   return "noteheadTriangleUpBlack";
                case "RE":   return "noteheadMoonBlack";
                case "MI":   return "noteheadDiamondBlack";
                case "FA":   return "noteheadTriangleLeftBlack";
                case "FAUP": return "noteheadTriangleRightBlack";
                case "SO":   return "noteheadBlack";
                case "LA":   return "noteheadSquareBlack";
                case "TI":   return "noteheadTriangleRoundDownBlack";

                case "D":
                case "H":
                    switch (duration)
                    {
                        case "1/2": return "noteheadDiamondDoubleWhole";
                        case "1":   return "noteheadDiamondWhole";
                        case "2":   return "noteheadDiamondHalf";
                        default:    return "noteheadDiamondBlack";
                    }

                case "N":
                case "G":
                    switch (duration)
                    {
                        case "1/2": return "noteheadDoubleWhole";
                        case "1":   return "noteheadWhole";
                        case "2":   return "noteheadHalf";
                        default:    return "noteheadBlack";
                    }

                case "M":
                case "X":
                    switch (duration)
                    {
                        case "1/2": return "noteheadXDoubleWhole";
                        case "1":   return "noteheadXWhole";
                        case "2":   return "noteheadXHalf";
                        default:    return "noteheadXBlack";
                    }

                case "CX":
                    switch (duration)
                    {
                        case "1/2": return "noteheadCircleXDoubleWhole";
                        case "1":   return "noteheadCircleXWhole";
                        case "2":   return "noteheadCircleXHalf";
                        default:    return "noteheadCircleX";
                    }

                case "CI":
                    switch (duration)
                    {
                        case "1/2": return "noteheadCircledDoubleWhole";
                        case "1":   return "noteheadCircledWhole";
                        case "2":   return "noteheadCircledHalf";
                        default:    return "noteheadCircledBlack";
                    }

                case "SQ":
                    switch (duration)
                    {
                        case "1/2": return "noteheadDoubleWholeSquare";
                        case "1":
                        case "2":   return "noteheadSquareWhite";
                        default:    return "noteheadSquareBlack";
                    }

                case "TU":
                    switch (duration)
                    {
                        case "1/2": return "noteheadTriangleUpDoubleWhole";
                        case "1":   return "noteheadTriangleUpWhole";
                        case "2":   return "noteheadTriangleUpHalf";
                        default:    return "noteheadTriangleUpBlack";
                    }

                case "TD":
                    switch (duration)
                    {
                        case "1/2": return "noteheadTriangleDownDoubleWhole";
                        case "1":   return "noteheadTriangleDownWhole";
                        case "2":   return "noteheadTriangleDownHalf";
                        default:    return "noteheadTriangleDownBlack";
                    }

                case "SF":
                    switch (duration)
                    {
                        case "1/2": return "noteheadSlashedDoubleWhole1";
                        case "1":   return "noteheadSlashedWhole1";
                        case "2":   return "noteheadSlashedHalf1";
                        default:    return "noteheadSlashedBlack1";
                    }

                case "SB":
                    switch (duration)
                    {
                        case "1/2": return "noteheadSlashedDoubleWhole2";
                        case "1":   return "noteheadSlashedWhole2";
                        case "2":   return "noteheadSlashedHalf2";
                        default:    return "noteheadSlashedBlack2";
                    }

                default:
                    return "";
            }
        }

        // ── GlyphProps lookup ─────────────────────────────────────────────────

        /// <summary>
        /// Return glyph properties for a given duration and type.
        /// Port of Tables.getGlyphProps() from tables.ts.
        /// </summary>
        public static GlyphProps GetGlyphProps(string duration, string type = "n")
        {
            duration = SanitizeDuration(duration);

            // Start with common duration properties
            GlyphProps props;
            if (_durationCommon.TryGetValue(duration, out var common))
            {
                props = new GlyphProps
                {
                    Stem              = common.Stem,
                    Flag              = common.Flag,
                    BeamCount         = common.BeamCount,
                    CodeFlagUpStem    = common.CodeFlagUpStem,
                    CodeFlagDownStem  = common.CodeFlagDownStem,
                    StemUpExtension   = common.StemUpExtension,
                    StemDownExtension = common.StemDownExtension,
                    DotShiftY         = common.DotShiftY,
                    LineAbove         = common.LineAbove,
                    LineBelow         = common.LineBelow,
                };
            }
            else
            {
                props = new GlyphProps();
            }

            // Overlay rest properties for type "r"
            if (type == "r" && _durationRest.TryGetValue(duration, out var rest))
            {
                props.CodeHead  = rest.CodeHead;
                props.Rest      = true;
                props.Stem      = false;
                props.Position  = rest.Position;
                props.DotShiftY = rest.DotShiftY;
                if (rest.LineAbove != 0) props.LineAbove = rest.LineAbove;
                if (rest.LineBelow != 0) props.LineBelow = rest.LineBelow;
            }

            // Try custom note head code
            var codeHead = CodeNoteHead(type.ToUpperInvariant(), duration);
            if (!string.IsNullOrEmpty(codeHead))
            {
                props.CodeHead = codeHead;
                props.Code     = codeHead;
            }

            // Compute actual head width from glyph data using VexFlow's scale formula.
            // VexFlow: getWidth = () => Glyph.getWidth(code_head, NOTATION_FONT_SCALE)
            // Port: Glyph.GetWidth(code, NOTATION_FONT_SCALE) uses (point*72)/(resolution*100) * bbox.getW()
            if (!string.IsNullOrEmpty(props.CodeHead))
            {
                double w = Glyph.GetWidth(props.CodeHead, NOTATION_FONT_SCALE);
                if (w > 0) props.HeadWidth = w;
            }
            else if (!string.IsNullOrEmpty(props.Code))
            {
                double w = Glyph.GetWidth(props.Code, NOTATION_FONT_SCALE);
                if (w > 0) props.HeadWidth = w;
            }

            return props;
        }

        // ── Integer-to-note ───────────────────────────────────────────────────

        /// <summary>
        /// Convert a semitone integer [0, 11] to a note name.
        /// </summary>
        public static string IntegerToNote(int integer)
        {
            if (!_integerToNoteMap.TryGetValue(integer, out var name))
                throw new VexFlowException("BadArguments",
                    $"integerToNote() requires an integer in the range [0, 11]: {integer}");
            return name;
        }

        // ── Accidentals ───────────────────────────────────────────────────────

        private static readonly Dictionary<string, (string Code, int PaddingAdj)> _accidentals
            = new Dictionary<string, (string, int)>
        {
            { "#",   ("accidentalSharp",       -1) },
            { "##",  ("accidentalDoubleSharp", -1) },
            { "b",   ("accidentalFlat",        -2) },
            { "bb",  ("accidentalDoubleFlat",  -2) },
            { "n",   ("accidentalNatural",     -1) },
            { "{",   ("accidentalParensLeft",  -1) },
            { "}",   ("accidentalParensRight", -1) },
            { "db",  ("accidentalThreeQuarterTonesFlatZimmermann", -1) },
            { "d",   ("accidentalQuarterToneFlatStein",  0) },
            { "++",  ("accidentalThreeQuarterTonesSharpStein", -1) },
            { "+",   ("accidentalQuarterToneSharpStein", -1) },
            { "+-",  ("accidentalKucukMucennebSharp",    -1) },
            { "bs",  ("accidentalBakiyeFlat",  -1) },
            { "bss", ("accidentalBuyukMucennebFlat", -1) },
        };

        /// <summary>
        /// Return the SMuFL glyph code for a given accidental type string ("#", "b", "n", etc.).
        /// Port of VexFlow's Tables.accidentalCodes() from tables.ts.
        /// </summary>
        public static (string Code, int PaddingAdj) AccidentalCodes(string acc)
        {
            if (_accidentals.TryGetValue(acc, out var result))
                return result;
            throw new VexFlowException("BadArguments", $"Unknown accidental: {acc}");
        }

        // ── Articulation codes ────────────────────────────────────────────────

        /// <summary>
        /// Lookup table mapping articulation type strings to their SMuFL glyph codes.
        /// Port of VexFlow's tables.ts articulations dictionary (lines 468-512).
        /// All 22 entries, verbatim from source.
        /// </summary>
        public static readonly Dictionary<string, ArticulationStruct> ArticulationCodes =
            new Dictionary<string, ArticulationStruct>
            {
                ["a."]    = new ArticulationStruct { Code = "augmentationDot", BetweenLines = true },  // Staccato
                ["av"]    = new ArticulationStruct { AboveCode = "articStaccatissimoAbove", BelowCode = "articStaccatissimoBelow", BetweenLines = true },  // Staccatissimo
                ["a>"]    = new ArticulationStruct { AboveCode = "articAccentAbove", BelowCode = "articAccentBelow", BetweenLines = true },  // Accent
                ["a-"]    = new ArticulationStruct { AboveCode = "articTenutoAbove", BelowCode = "articTenutoBelow", BetweenLines = true },  // Tenuto
                ["a^"]    = new ArticulationStruct { AboveCode = "articMarcatoAbove", BelowCode = "articMarcatoBelow", BetweenLines = false },  // Marcato
                ["a+"]    = new ArticulationStruct { Code = "pluckedLeftHandPizzicato", BetweenLines = false },  // Left hand pizzicato
                ["ao"]    = new ArticulationStruct { AboveCode = "pluckedSnapPizzicatoAbove", BelowCode = "pluckedSnapPizzicatoBelow", BetweenLines = false },  // Snap pizzicato
                ["ah"]    = new ArticulationStruct { Code = "stringsHarmonic", BetweenLines = false },  // Natural harmonic or open note
                ["a@"]    = new ArticulationStruct { AboveCode = "fermataAbove", BelowCode = "fermataBelow", BetweenLines = false },  // Fermata
                ["a@a"]   = new ArticulationStruct { Code = "fermataAbove", BetweenLines = false },  // Fermata above staff
                ["a@u"]   = new ArticulationStruct { Code = "fermataBelow", BetweenLines = false },  // Fermata below staff
                ["a@s"]   = new ArticulationStruct { AboveCode = "fermataShortAbove", BelowCode = "fermataShortBelow", BetweenLines = false },  // Fermata short
                ["a@as"]  = new ArticulationStruct { Code = "fermataShortAbove", BetweenLines = false },  // Fermata short above staff
                ["a@us"]  = new ArticulationStruct { Code = "fermataShortBelow", BetweenLines = false },  // Fermata short below staff
                ["a@l"]   = new ArticulationStruct { AboveCode = "fermataLongAbove", BelowCode = "fermataLongBelow", BetweenLines = false },  // Fermata long
                ["a@al"]  = new ArticulationStruct { Code = "fermataLongAbove", BetweenLines = false },  // Fermata long above staff
                ["a@ul"]  = new ArticulationStruct { Code = "fermataLongBelow", BetweenLines = false },  // Fermata long below staff
                ["a@vl"]  = new ArticulationStruct { AboveCode = "fermataVeryLongAbove", BelowCode = "fermataVeryLongBelow", BetweenLines = false },  // Fermata very long
                ["a@avl"] = new ArticulationStruct { Code = "fermataVeryLongAbove", BetweenLines = false },  // Fermata very long above staff
                ["a@uvl"] = new ArticulationStruct { Code = "fermataVeryLongBelow", BetweenLines = false },  // Fermata very long below staff
                ["a|"]    = new ArticulationStruct { Code = "stringsUpBow", BetweenLines = false },  // Bow up - up stroke
                ["am"]    = new ArticulationStruct { Code = "stringsDownBow", BetweenLines = false },  // Bow down - down stroke
                ["a,"]    = new ArticulationStruct { Code = "pictChokeCymbal", BetweenLines = false },  // Choked
            };

        // ── TextDynamics glyphs ───────────────────────────────────────────────

        /// <summary>
        /// Lookup table mapping individual dynamic letters to SMuFL glyph codes and widths.
        /// Port of vexflow/src/textdynamics.ts GLYPHS constant.
        /// Multi-letter dynamics ("mp", "ff") are rendered letter-by-letter.
        /// </summary>
        public static readonly Dictionary<string, (string Code, int Width)> TextDynamicsGlyphs =
            new Dictionary<string, (string Code, int Width)>
            {
                ["f"] = ("dynamicForte",       12),
                ["p"] = ("dynamicPiano",       14),
                ["m"] = ("dynamicMezzo",       17),
                ["s"] = ("dynamicSforzando",   10),
                ["z"] = ("dynamicZ",           12),
                ["r"] = ("dynamicRinforzando", 12),
            };

        // ── Ornament codes ────────────────────────────────────────────────────

        /// <summary>
        /// Lookup table mapping ornament type strings to their SMuFL glyph codes.
        /// Port of VexFlow's tables.ts ornaments dictionary (lines 514-535).
        /// All entries, verbatim from source.
        /// </summary>
        public static readonly Dictionary<string, string> OrnamentCodes =
            new Dictionary<string, string>
            {
                ["mordent"]          = "ornamentShortTrill",
                ["mordent_inverted"] = "ornamentMordent",
                ["turn"]             = "ornamentTurn",
                ["turn_inverted"]    = "ornamentTurnSlash",
                ["tr"]               = "ornamentTrill",
                ["upprall"]          = "ornamentPrecompSlideTrillDAnglebert",
                ["downprall"]        = "ornamentPrecompDoubleCadenceUpperPrefix",
                ["prallup"]          = "ornamentPrecompTrillSuffixDandrieu",
                ["pralldown"]        = "ornamentPrecompTrillLowerSuffix",
                ["upmordent"]        = "ornamentPrecompSlideTrillBach",
                ["downmordent"]      = "ornamentPrecompDoubleCadenceUpperPrefixTurn",
                ["lineprall"]        = "ornamentPrecompAppoggTrill",
                ["prallprall"]       = "ornamentTremblement",
                ["scoop"]            = "brassScoop",
                ["doit"]             = "brassDoitMedium",
                ["fall"]             = "brassFallLipShort",
                ["doitLong"]         = "brassLiftMedium",
                ["fallLong"]         = "brassFallRoughMedium",
                ["bend"]             = "brassBend",
                ["plungerClosed"]    = "brassMuteClosed",
                ["plungerOpen"]      = "brassMuteOpen",
                ["flip"]             = "brassFlip",
                ["jazzTurn"]         = "brassJazzTurn",
                ["smear"]            = "brassSmear",
            };

        // ── Font ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Return the current music font data (Bravura).
        /// </summary>
        public static FontData CurrentMusicFont()
        {
            if (!Font.HasFont("Bravura"))
                throw new VexFlowException("NoFonts", "Bravura font not loaded. Call Font.Load(\"Bravura\", ...) first.");
            return Font.GetData("Bravura");
        }
    }
}

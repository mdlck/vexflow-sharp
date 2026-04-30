// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VexFlowSharp
{
    /// <summary>
    /// Return value of GetNoteParts: root letter and optional accidental.
    /// </summary>
    public class NoteParts
    {
        public string Root        { get; set; } = "";
        public string Accidental  { get; set; } = "";
    }

    /// <summary>
    /// Return value of GetKeyParts: root, optional accidental, and scale type.
    /// </summary>
    public class KeyParts
    {
        public string Root        { get; set; } = "";
        public string Accidental  { get; set; } = "";
        public string Type        { get; set; } = "";
    }

    /// <summary>
    /// Standard music theory utilities.
    /// Port of VexFlow's Music class from music.ts.
    /// </summary>
    public class Music
    {
        // ── Static data ───────────────────────────────────────────────────────

        public static readonly string[] Roots = { "c", "d", "e", "f", "g", "a", "b" };

        public static readonly int[] RootValues = { 0, 2, 4, 5, 7, 9, 11 };

        public static readonly Dictionary<string, int> RootIndices = new Dictionary<string, int>
        {
            { "c", 0 }, { "d", 1 }, { "e", 2 }, { "f", 3 },
            { "g", 4 }, { "a", 5 }, { "b", 6 },
        };

        public static readonly string[] CanonicalNotes =
            { "c", "c#", "d", "d#", "e", "f", "f#", "g", "g#", "a", "a#", "b" };

        public static readonly string[] DiatonicIntervals =
            { "unison", "m2", "M2", "m3", "M3", "p4", "dim5", "p5", "m6", "M6", "b7", "M7", "octave" };

        public static readonly string[] Accidentals = { "bb", "b", "n", "#", "##" };

        public static readonly Dictionary<string, int[]> Scales = new Dictionary<string, int[]>
        {
            { "major",      new[] { 2, 2, 1, 2, 2, 2, 1 } },
            { "minor",      new[] { 2, 1, 2, 2, 1, 2, 2 } },
            { "ionian",     new[] { 2, 2, 1, 2, 2, 2, 1 } },
            { "dorian",     new[] { 2, 1, 2, 2, 2, 1, 2 } },
            { "phyrgian",   new[] { 1, 2, 2, 2, 1, 2, 2 } },
            { "lydian",     new[] { 2, 2, 2, 1, 2, 2, 1 } },
            { "mixolydian", new[] { 2, 2, 1, 2, 2, 1, 2 } },
            { "aeolian",    new[] { 2, 1, 2, 2, 1, 2, 2 } },
            { "locrian",    new[] { 1, 2, 2, 1, 2, 2, 2 } },
        };

        public static readonly Dictionary<string, int[]> ScaleTypes = new Dictionary<string, int[]>
        {
            { "M", Scales["major"] },
            { "m", Scales["minor"] },
        };

        public static readonly Dictionary<string, int> Intervals = new Dictionary<string, int>
        {
            { "u",       0 }, { "unison", 0 },
            { "m2",      1 }, { "b2",  1 }, { "min2", 1 }, { "S", 1 }, { "H", 1 },
            { "2",       2 }, { "M2",  2 }, { "maj2", 2 }, { "T", 2 }, { "W",  2 },
            { "m3",      3 }, { "b3",  3 }, { "min3", 3 },
            { "M3",      4 }, { "3",   4 }, { "maj3", 4 },
            { "4",       5 }, { "p4",  5 },
            { "#4",      6 }, { "b5",  6 }, { "aug4", 6 }, { "dim5", 6 },
            { "5",       7 }, { "p5",  7 },
            { "#5",      8 }, { "b6",  8 }, { "aug5", 8 },
            { "6",       9 }, { "M6",  9 }, { "maj6", 9 },
            { "b7",     10 }, { "m7", 10 }, { "min7", 10 }, { "dom7", 10 },
            { "M7",     11 }, { "maj7", 11 },
            { "8",      12 }, { "octave", 12 },
        };

        /// <summary>Maps note names (with accidentals) to chromatic semitone value [0-11].</summary>
        public static readonly Dictionary<string, (int RootIndex, int IntVal)> NoteValues =
            new Dictionary<string, (int, int)>(StringComparer.Ordinal)
        {
            { "c",    (0, 0)  }, { "cn",   (0, 0)  },
            { "c#",   (0, 1)  }, { "c##",  (0, 2)  },
            { "cb",   (0, 11) }, { "cbb",  (0, 10) },
            { "d",    (1, 2)  }, { "dn",   (1, 2)  },
            { "d#",   (1, 3)  }, { "d##",  (1, 4)  },
            { "db",   (1, 1)  }, { "dbb",  (1, 0)  },
            { "e",    (2, 4)  }, { "en",   (2, 4)  },
            { "e#",   (2, 5)  }, { "e##",  (2, 6)  },
            { "eb",   (2, 3)  }, { "ebb",  (2, 2)  },
            { "f",    (3, 5)  }, { "fn",   (3, 5)  },
            { "f#",   (3, 6)  }, { "f##",  (3, 7)  },
            { "fb",   (3, 4)  }, { "fbb",  (3, 3)  },
            { "g",    (4, 7)  }, { "gn",   (4, 7)  },
            { "g#",   (4, 8)  }, { "g##",  (4, 9)  },
            { "gb",   (4, 6)  }, { "gbb",  (4, 5)  },
            { "a",    (5, 9)  }, { "an",   (5, 9)  },
            { "a#",   (5, 10) }, { "a##",  (5, 11) },
            { "ab",   (5, 8)  }, { "abb",  (5, 7)  },
            { "b",    (6, 11) }, { "bn",   (6, 11) },
            { "b#",   (6, 0)  }, { "b##",  (6, 1)  },
            { "bb",   (6, 10) }, { "bbb",  (6, 9)  },
        };

        public static int NumTones => CanonicalNotes.Length;

        // ── Validation helpers ─────────────────────────────────────────────────

        protected bool IsValidNoteValue(int note) => note >= 0 && note < NumTones;
        protected bool IsValidIntervalValue(int interval) => interval >= 0 && interval < DiatonicIntervals.Length;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Parse "c#" into { Root="c", Accidental="#" }.
        /// Does NOT include the octave — use the full "c#/4" parsing separately.
        /// </summary>
        public NoteParts GetNoteParts(string noteString)
        {
            if (string.IsNullOrEmpty(noteString) || noteString.Length < 1)
                throw new VexFlowException("BadArguments", $"Invalid note name: {noteString}");
            if (noteString.Length > 3)
                throw new VexFlowException("BadArguments", $"Invalid note name: {noteString}");

            var note = noteString.ToLowerInvariant();
            var match = Regex.Match(note, @"^([cdefgab])(b|bb|n|#|##)?$");
            if (!match.Success)
                throw new VexFlowException("BadArguments", $"Invalid note name: {noteString}");

            return new NoteParts
            {
                Root       = match.Groups[1].Value,
                Accidental = match.Groups[2].Value,
            };
        }

        /// <summary>
        /// Parse "Cm" into { Root="c", Accidental="", Type="m" }.
        /// Unspecified type defaults to "M" (major).
        /// </summary>
        public KeyParts GetKeyParts(string keyString)
        {
            if (string.IsNullOrEmpty(keyString))
                throw new VexFlowException("BadArguments", $"Invalid key: {keyString}");

            var key = keyString.ToLowerInvariant();
            // Supports M, m, mel, harm suffixes
            var match = Regex.Match(key, @"^([cdefgab])(b|#)?(mel|harm|m|M)?$");
            if (!match.Success)
                throw new VexFlowException("BadArguments", $"Invalid key: {keyString}");

            var type = match.Groups[3].Value;
            if (string.IsNullOrEmpty(type)) type = "M";

            return new KeyParts
            {
                Root       = match.Groups[1].Value,
                Accidental = match.Groups[2].Value,
                Type       = type,
            };
        }

        /// <summary>
        /// Return the semitone value [0-11] for a note name like "c#".
        /// </summary>
        public int GetNoteValue(string noteString)
        {
            if (!NoteValues.TryGetValue(noteString.ToLowerInvariant(), out var val))
                throw new VexFlowException("BadArguments", $"Invalid note name: {noteString}");
            return val.IntVal;
        }

        /// <summary>
        /// Return the semitone count for an interval name like "M3".
        /// </summary>
        public int GetIntervalValue(string intervalString)
        {
            if (!Intervals.TryGetValue(intervalString, out var val))
                throw new VexFlowException("BadArguments", $"Invalid interval name: {intervalString}");
            return val;
        }

        /// <summary>
        /// Return canonical note name for a semitone integer [0-11].
        /// </summary>
        public string GetCanonicalNoteName(int noteValue)
        {
            if (!IsValidNoteValue(noteValue))
                throw new VexFlowException("BadArguments", $"Invalid note value: {noteValue}");
            return CanonicalNotes[noteValue];
        }

        /// <summary>
        /// Return canonical interval name for a diatonic interval index.
        /// </summary>
        public string GetCanonicalIntervalName(int intervalValue)
        {
            if (!IsValidIntervalValue(intervalValue))
                throw new VexFlowException("BadArguments", $"Invalid interval value: {intervalValue}");
            return DiatonicIntervals[intervalValue];
        }

        /// <summary>
        /// Compute note value + direction * interval, wrapped to [0, 11].
        /// direction must be 1 or -1.
        /// </summary>
        public int GetRelativeNoteValue(int noteValue, int intervalValue, int direction = 1)
        {
            if (direction != 1 && direction != -1)
                throw new VexFlowException("BadArguments", $"Invalid direction: {direction}");
            int sum = (noteValue + direction * intervalValue) % NumTones;
            if (sum < 0) sum += NumTones;
            return sum;
        }

        /// <summary>
        /// Given a root note name and a target semitone value, return the note name with
        /// appropriate sharps/flats added (e.g., root="c", noteValue=1 -> "c#").
        /// </summary>
        public string GetRelativeNoteName(string root, int noteValue)
        {
            var parts    = GetNoteParts(root);
            var rootValue = GetNoteValue(parts.Root);
            var interval  = noteValue - rootValue;

            if (Math.Abs(interval) > NumTones - 3)
            {
                int multiplier = interval > 0 ? -1 : 1;
                int reverseInterval = ((noteValue + 1 + (rootValue + 1)) % NumTones) * multiplier;
                if (Math.Abs(reverseInterval) > 2)
                    throw new VexFlowException("BadArguments", $"Notes not related: {root}, {noteValue}");
                interval = reverseInterval;
            }

            if (Math.Abs(interval) > 2)
                throw new VexFlowException("BadArguments", $"Notes not related: {root}, {noteValue}");

            var name = parts.Root;
            if (interval > 0)
                for (int i = 1; i <= interval; i++) name += "#";
            else if (interval < 0)
                for (int i = -1; i >= interval; i--) name += "b";
            return name;
        }

        /// <summary>
        /// Build the scale tone list from a root semitone value and an interval array.
        /// For example, key=0 (C) and major intervals returns [0,2,4,5,7,9,11].
        /// </summary>
        public int[] GetScaleTones(int key, int[] intervals)
        {
            var tones = new List<int> { key };
            int next = key;
            foreach (var interval in intervals)
            {
                next = GetRelativeNoteValue(next, interval);
                if (next != key) tones.Add(next);
            }
            return tones.ToArray();
        }

        /// <summary>
        /// Return the interval (in semitones) between two notes.
        /// direction: 1 = ascending from note1 to note2; -1 = descending.
        /// </summary>
        public int GetIntervalBetween(int note1, int note2, int direction = 1)
        {
            if (direction != 1 && direction != -1)
                throw new VexFlowException("BadArguments", $"Invalid direction: {direction}");
            if (!IsValidNoteValue(note1) || !IsValidNoteValue(note2))
                throw new VexFlowException("BadArguments", $"Invalid notes: {note1}, {note2}");

            int difference = direction == 1 ? note2 - note1 : note1 - note2;
            if (difference < 0) difference += NumTones;
            return difference;
        }

        /// <summary>
        /// Build a scale map (root letter -> note name with accidental) for a key signature.
        /// e.g., "G" returns { c:"cn", d:"dn", e:"en", f:"f#", g:"gn", a:"an", b:"bn" }.
        /// </summary>
        public Dictionary<string, string> CreateScaleMap(string keySignature)
        {
            var keySigParts = GetKeyParts(keySignature);
            if (!ScaleTypes.TryGetValue(keySigParts.Type, out var scaleName))
                throw new VexFlowException("BadArguments", $"Unsupported key type: {keySignature}");

            var keySigString = keySigParts.Root;
            if (!string.IsNullOrEmpty(keySigParts.Accidental)) keySigString += keySigParts.Accidental;

            var scale = GetScaleTones(GetNoteValue(keySigString), scaleName);
            var noteLocation = RootIndices[keySigParts.Root];

            var scaleMap = new Dictionary<string, string>();
            for (int i = 0; i < Roots.Length; i++)
            {
                var index    = (noteLocation + i) % Roots.Length;
                var rootName = Roots[index];
                var noteName = GetRelativeNoteName(rootName, scale[i]);
                if (noteName.Length == 1) noteName += "n";
                scaleMap[rootName] = noteName;
            }
            return scaleMap;
        }
    }
}

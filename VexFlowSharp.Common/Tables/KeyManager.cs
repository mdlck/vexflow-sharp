// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Result of GetAccidental / SelectNote.
    /// </summary>
    public class AccidentalResult
    {
        /// <summary>Full note name in the current scale map (e.g., "f#").</summary>
        public string Note        { get; set; } = "";

        /// <summary>Accidental string, or null/empty if natural.</summary>
        public string? Accidental { get; set; }

        /// <summary>Whether the scale map was mutated by this call.</summary>
        public bool Change        { get; set; }
    }

    /// <summary>
    /// Diatonic key manager — tracks active accidentals within a measure.
    /// Port of VexFlow's KeyManager class from keymanager.ts.
    /// </summary>
    public class KeyManager
    {
        private readonly Music _music;

        private KeyParts _keyParts   = null!;
        private string   _keyString  = "";
        private string   _key        = "";

        private int[]    _scale      = System.Array.Empty<int>();
        private Dictionary<string, string> _scaleMap             = new();
        private Dictionary<int, string>    _scaleMapByValue      = new();
        private Dictionary<int, string>    _originalScaleMapByValue = new();

        /// <summary>
        /// Construct a KeyManager for the given key (e.g., "G", "Cm").
        /// </summary>
        public KeyManager(string key)
        {
            _music = new Music();
            SetKey(key);
        }

        /// <summary>Set a new key and reset the scale map.</summary>
        public KeyManager SetKey(string key)
        {
            _key = key;
            Reset();
            return this;
        }

        /// <summary>Return the current key string.</summary>
        public string GetKey() => _key;

        /// <summary>
        /// Rebuild the scale map from the current key.
        /// Resets any mid-measure accidental changes.
        /// </summary>
        public KeyManager Reset()
        {
            _keyParts = _music.GetKeyParts(_key);

            _keyString = _keyParts.Root;
            if (!string.IsNullOrEmpty(_keyParts.Accidental)) _keyString += _keyParts.Accidental;

            if (!Music.ScaleTypes.ContainsKey(_keyParts.Type))
                throw new VexFlowException("BadArguments", $"Unsupported key type: {_key}");

            _scale = _music.GetScaleTones(
                _music.GetNoteValue(_keyString),
                Music.ScaleTypes[_keyParts.Type]
            );

            _scaleMap             = new Dictionary<string, string>();
            _scaleMapByValue      = new Dictionary<int, string>();
            _originalScaleMapByValue = new Dictionary<int, string>();

            var noteLocation = Music.RootIndices[_keyParts.Root];
            for (int i = 0; i < Music.Roots.Length; i++)
            {
                var index    = (noteLocation + i) % Music.Roots.Length;
                var rootName = Music.Roots[index];
                var noteName = _music.GetRelativeNoteName(rootName, _scale[i]);

                _scaleMap[rootName]                  = noteName;
                _scaleMapByValue[_scale[i]]          = noteName;
                _originalScaleMapByValue[_scale[i]]  = noteName;
            }
            return this;
        }

        /// <summary>
        /// Return the accidental status of a note in the current key context.
        /// Does NOT mutate the scale map.
        /// </summary>
        public AccidentalResult GetAccidental(string key)
        {
            var root   = _music.GetKeyParts(key).Root;
            var parts  = _music.GetNoteParts(_scaleMap[root]);
            return new AccidentalResult
            {
                Note       = _scaleMap[root],
                Accidental = parts.Accidental,
                Change     = false,
            };
        }

        /// <summary>
        /// Select a note within the current key context, updating the scale map
        /// when an accidental change is detected.
        /// Port of VexFlow's KeyManager.selectNote() from keymanager.ts.
        /// </summary>
        public AccidentalResult SelectNote(string note)
        {
            note = note.ToLowerInvariant();
            var parts     = _music.GetNoteParts(note);
            var scaleNote = _scaleMap[parts.Root];
            var modparts  = _music.GetNoteParts(scaleNote);

            // 1. Exact match — note is already in the scale map as-is.
            if (scaleNote == note)
            {
                return new AccidentalResult
                {
                    Note       = scaleNote,
                    Accidental = parts.Accidental,
                    Change     = false,
                };
            }

            // 2. Equivalent value found in the (possibly altered) scale map.
            var noteValue = _music.GetNoteValue(note);
            if (_scaleMapByValue.TryGetValue(noteValue, out var valueNote))
            {
                return new AccidentalResult
                {
                    Note       = valueNote,
                    Accidental = _music.GetNoteParts(valueNote).Accidental,
                    Change     = false,
                };
            }

            // 3. Equivalent value found in the original (unaltered) scale map.
            if (_originalScaleMapByValue.TryGetValue(noteValue, out var origValueNote))
            {
                _scaleMap[modparts.Root] = origValueNote;
                var scaleNoteValue = _music.GetNoteValue(scaleNote);
                _scaleMapByValue.Remove(scaleNoteValue);
                _scaleMapByValue[noteValue] = origValueNote;
                return new AccidentalResult
                {
                    Note       = origValueNote,
                    Accidental = _music.GetNoteParts(origValueNote).Accidental,
                    Change     = true,
                };
            }

            // 4. Naturalize: the note matches the root of the current scale entry.
            if (modparts.Root == note)
            {
                var modNoteValue = _music.GetNoteValue(_scaleMap[parts.Root]);
                _scaleMapByValue.Remove(modNoteValue);
                _scaleMapByValue[_music.GetNoteValue(modparts.Root)] = modparts.Root;
                _scaleMap[modparts.Root] = modparts.Root;
                return new AccidentalResult
                {
                    Note       = modparts.Root,
                    Accidental = null,
                    Change     = true,
                };
            }

            // 5. Last resort — accept the note and update the maps.
            var existingValue = _music.GetNoteValue(_scaleMap[parts.Root]);
            _scaleMapByValue.Remove(existingValue);
            _scaleMapByValue[noteValue] = note;
            _scaleMap[modparts.Root]    = note;
            return new AccidentalResult
            {
                Note       = note,
                Accidental = parts.Accidental,
                Change     = true,
            };
        }
    }
}

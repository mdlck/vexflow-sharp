// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace VexFlowSharp
{
    /// <summary>
    /// Tablature tuning helper. Port of VexFlow's Tuning class from tuning.ts.
    /// </summary>
    public class Tuning
    {
        private readonly List<int> tuningValues = new List<int>();

        public static readonly IReadOnlyDictionary<string, string> Names =
            new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "standard", "E/5,B/4,G/4,D/4,A/3,E/3" },
            { "dagdad", "D/5,A/4,G/4,D/4,A/3,D/3" },
            { "dropd", "E/5,B/4,G/4,D/4,A/3,D/3" },
            { "eb", "Eb/5,Bb/4,Gb/4,Db/4,Ab/3,Db/3" },
            { "standardBanjo", "D/5,B/4,G/4,D/4,G/5" },
        };

        public Tuning(string tuningString = "E/5,B/4,G/4,D/4,A/3,E/3,B/2,E/2")
        {
            SetTuning(tuningString);
        }

        public int NoteToInteger(string noteString)
            => Tables.KeyProperties(noteString).IntValue ?? -1;

        public void SetTuning(string tuningString)
        {
            if (Names.TryGetValue(tuningString, out var namedTuning))
                tuningString = namedTuning;

            tuningValues.Clear();

            var keys = Regex.Split(tuningString, @"\s*,\s*");
            if (keys.Length == 0)
                throw new VexFlowException("BadArguments", $"Invalid tuning string: {tuningString}");

            foreach (var key in keys)
                tuningValues.Add(NoteToInteger(key));
        }

        public int GetValueForString(string stringNum)
        {
            if (!double.TryParse(stringNum, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                throw new VexFlowException("BadArguments", $"String number must be between 1 and {tuningValues.Count}:{stringNum}");

            return GetValueForString(parsed);
        }

        public int GetValueForString(int stringNum) => GetValueForString((double)stringNum);

        public int GetValueForString(double stringNum)
        {
            if (stringNum < 1 || stringNum > tuningValues.Count || Math.Abs(stringNum - Math.Round(stringNum)) > 0.0000001)
                throw new VexFlowException("BadArguments", $"String number must be between 1 and {tuningValues.Count}:{stringNum}");

            return tuningValues[(int)stringNum - 1];
        }

        public int GetValueForFret(string fretNum, string stringNum)
        {
            if (!double.TryParse(fretNum, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFret))
                throw new VexFlowException("BadArguments", $"Fret number must be 0 or higher: {fretNum}");

            return GetValueForFret(parsedFret, stringNum);
        }

        public int GetValueForFret(int fretNum, int stringNum)
            => GetValueForFret((double)fretNum, (double)stringNum);

        public int GetValueForFret(double fretNum, string stringNum)
        {
            if (!double.TryParse(stringNum, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedString))
                throw new VexFlowException("BadArguments", $"String number must be between 1 and {tuningValues.Count}:{stringNum}");

            return GetValueForFret(fretNum, parsedString);
        }

        public int GetValueForFret(double fretNum, double stringNum)
        {
            var stringValue = GetValueForString(stringNum);
            if (fretNum < 0 || Math.Abs(fretNum - Math.Round(fretNum)) > 0.0000001)
                throw new VexFlowException("BadArguments", $"Fret number must be 0 or higher: {fretNum}");

            return stringValue + (int)fretNum;
        }

        public string GetNoteForFret(string fretNum, string stringNum)
        {
            if (!double.TryParse(fretNum, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFret))
                throw new VexFlowException("BadArguments", $"Fret number must be 0 or higher: {fretNum}");

            return GetNoteForFret(parsedFret, stringNum);
        }

        public string GetNoteForFret(int fretNum, int stringNum)
            => GetNoteForFret((double)fretNum, (double)stringNum);

        public string GetNoteForFret(double fretNum, string stringNum)
        {
            if (!double.TryParse(stringNum, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedString))
                throw new VexFlowException("BadArguments", $"String number must be between 1 and {tuningValues.Count}:{stringNum}");

            return GetNoteForFret(fretNum, parsedString);
        }

        public string GetNoteForFret(double fretNum, double stringNum)
        {
            var noteValue = GetValueForFret(fretNum, stringNum);
            var octave = (int)Math.Floor(noteValue / 12.0);
            var value = noteValue % 12;

            return $"{Tables.IntegerToNote(value)}/{octave}";
        }
    }
}

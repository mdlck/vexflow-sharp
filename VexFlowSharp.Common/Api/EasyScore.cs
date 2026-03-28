// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Full port of VexFlow's EasyScore and EasyScoreGrammar (easyscore.ts, 538 lines).
// EasyScore implements a DSL parser for concise score notation:
//   "C4/q, D4, E4, F4"  →  four StaveNote instances with quarter duration
//   "(C4 E4 G4)/q"      →  one chord StaveNote with 3 keys

using System;
using System.Collections.Generic;
using System.Linq;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Api
{
    // ── NoteOptions ───────────────────────────────────────────────────────────

    /// <summary>
    /// Options passed to EasyScore.Notes() to override defaults.
    /// Port of VexFlow's BuilderOptions from easyscore.ts.
    /// </summary>
    public class NoteOptions
    {
        /// <summary>Stem direction: "auto", "up", or "down".</summary>
        public string? Stem { get; set; }

        /// <summary>Clef: "treble", "bass", etc.</summary>
        public string? Clef { get; set; }
    }

    /// <summary>
    /// Options passed to EasyScore.Voice().
    /// </summary>
    public class VoiceOptions
    {
        public int? NumBeats { get; set; }
        public int? BeatValue { get; set; }
    }

    // ── NotePiece ─────────────────────────────────────────────────────────────

    /// <summary>
    /// One note within a chord or single note, accumulated by the Builder during parsing.
    /// Port of VexFlow's NotePiece interface from easyscore.ts.
    /// </summary>
    internal class NotePiece
    {
        /// <summary>Note letter name (e.g. "c", "d", "g").</summary>
        public string Key { get; set; } = "";

        /// <summary>Accidental string (e.g. "#", "b", "##", null if none).</summary>
        public string? Accid { get; set; }

        /// <summary>Octave string (e.g. "4").</summary>
        public string Octave { get; set; } = "4";
    }

    // ── Piece ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Accumulates note data for one piece (note or chord) during parsing.
    /// Port of VexFlow's Piece class from easyscore.ts.
    /// </summary>
    internal class Piece
    {
        public List<NotePiece> Chord { get; } = new List<NotePiece>();
        public string Duration { get; set; }
        public int Dots { get; set; } = 0;
        public string? Type { get; set; }
        public Dictionary<string, string> Options { get; } = new Dictionary<string, string>();

        public Piece(string duration)
        {
            Duration = duration;
        }
    }

    // ── Builder ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Accumulates note data during parsing and commits StaveNote instances.
    /// Port of VexFlow's Builder class from easyscore.ts.
    /// </summary>
    public class Builder
    {
        // ── Fields ────────────────────────────────────────────────────────────

        private readonly Factory factory;
        private string rollingDuration = "8";
        private Piece piece;
        private string optionStem = "auto";
        private string optionClef = "treble";

        /// <summary>All StaveNote elements created during a parse.</summary>
        public List<StaveNote> Elements { get; private set; } = new List<StaveNote>();

        /// <summary>Commit hooks called after each note is committed.</summary>
        public List<Action<Builder, StaveNote>> CommitHooks { get; set; } = new List<Action<Builder, StaveNote>>();

        // ── Constructor ───────────────────────────────────────────────────────

        public Builder(Factory factory)
        {
            this.factory = factory;
            piece = new Piece(rollingDuration);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        /// <summary>Reset builder state for a new parse. Port of Builder.reset() from easyscore.ts.</summary>
        public void Reset(NoteOptions? options = null)
        {
            optionStem = options?.Stem ?? "auto";
            optionClef = options?.Clef ?? "treble";
            Elements = new List<StaveNote>();
            rollingDuration = "8";
            ResetPiece();
        }

        private void ResetPiece()
        {
            piece = new Piece(rollingDuration);
        }

        // ── Builder methods called by grammar triggers ─────────────────────────

        /// <summary>
        /// Called by SINGLENOTE trigger: adds a single note to the chord list.
        /// Port of Builder.addSingleNote() from easyscore.ts.
        /// </summary>
        public void AddSingleNote(string key, string? accid, string octave)
        {
            piece.Chord.Add(new NotePiece { Key = key, Accid = accid, Octave = octave });
        }

        /// <summary>
        /// Called by CHORD trigger: chord notes were already accumulated via SINGLENOTE (NOTE rule).
        /// Port of Builder.addChord() from easyscore.ts — the chord notes arrive via NOTE rule calls.
        /// </summary>
        public void AddChord(List<object?> notes)
        {
            // Notes already accumulated into piece.Chord by individual NOTE/SINGLENOTE triggers.
            // This is a no-op in the C# port — CHORD's inner NOTE rules already called AddSingleNote.
        }

        /// <summary>
        /// Called by DURATION trigger. Sets rolling duration.
        /// Port of Builder.setNoteDuration() from easyscore.ts.
        /// </summary>
        public void SetNoteDuration(string? duration)
        {
            rollingDuration = piece.Duration = duration ?? rollingDuration;
        }

        /// <summary>
        /// Called by TYPE trigger. Sets note type override.
        /// Port of Builder.setNoteType() from easyscore.ts.
        /// </summary>
        public void SetNoteType(string? type)
        {
            if (type != null) piece.Type = type;
        }

        /// <summary>
        /// Called by DOTS trigger. Counts number of dots.
        /// Port of Builder.setNoteDots() from easyscore.ts.
        /// </summary>
        public void SetNoteDots(List<object?> dots)
        {
            if (dots != null) piece.Dots = dots.Count(d => d != null);
        }

        /// <summary>
        /// Called by KEYVAL trigger. Stores a key=value option for this piece.
        /// Port of Builder.addNoteOption() from easyscore.ts.
        /// </summary>
        public void AddNoteOption(string key, string value)
        {
            piece.Options[key] = value;
        }

        /// <summary>
        /// Commits the current piece into a StaveNote and resets for the next piece.
        /// This is the core method of the Builder.
        /// Port of Builder.commitPiece() from easyscore.ts lines 334-391.
        /// </summary>
        public void CommitPiece()
        {
            // Merge global options with per-piece options
            string stem = piece.Options.TryGetValue("stem", out var ps) ? ps.ToLower() : optionStem.ToLower();
            string clef = piece.Options.TryGetValue("clef", out var pc) ? pc.ToLower() : optionClef.ToLower();

            string duration = piece.Duration ?? rollingDuration;
            int dots = piece.Dots;
            string? type = piece.Type;

            // Build keys array: "c#/4", "e/4" etc.
            // Only standard accidentals (bb, b, n, #, ##) are included in the key string.
            // Microtonal accidentals are NOT included in the key (they're attached as modifiers only).
            var standardAccidentals = Music.Accidentals;
            var keys = piece.Chord.Select(np =>
                np.Key.ToLower()
                + (np.Accid != null && standardAccidentals.Contains(np.Accid) ? np.Accid : "")
                + "/"
                + np.Octave
            ).ToArray();

            bool autoStem = stem == "auto";

            // Create note
            StaveNote note;
            if (type?.ToLower() == "g")
            {
                // Ghost note — use duration from piece
                var ghostNote = factory.GhostNote(new StaveNoteStruct
                {
                    Duration = duration,
                    Dots = dots,
                });
                if (!autoStem)
                    ghostNote.SetStemDirection(stem == "up" ? Stem.UP : Stem.DOWN);
                // Ghost notes don't accumulate as StaveNote in Elements — skip
                ResetPiece();
                return;
            }

            note = factory.StaveNote(new StaveNoteStruct
            {
                Keys = keys,
                Duration = duration,
                Dots = dots,
                Type = type,
                Clef = clef,
                AutoStem = autoStem,
            });

            if (!autoStem)
                note.SetStemDirection(stem == "up" ? Stem.UP : Stem.DOWN);

            // Attach accidentals for each chord note
            for (int i = 0; i < piece.Chord.Count; i++)
            {
                var accid = piece.Chord[i].Accid;
                if (accid != null)
                {
                    var accidental = factory.Accidental(accid);
                    note.AddModifier(accidental, i);
                }
            }

            // Attach dots
            for (int i = 0; i < dots; i++)
            {
                Dot.BuildAndAttach(new List<Note> { note }, allNotes: true);
            }

            // Run commit hooks
            foreach (var hook in CommitHooks)
                hook(this, note);

            Elements.Add(note);
            ResetPiece();
        }
    }

    // ── EasyScoreGrammar ─────────────────────────────────────────────────────

    /// <summary>
    /// Grammar for the EasyScore DSL. Implements IGrammar.
    /// Port of VexFlow's EasyScoreGrammar class from easyscore.ts lines 29-212.
    /// </summary>
    public class EasyScoreGrammar : IGrammar
    {
        private readonly Builder builder;

        public EasyScoreGrammar(Builder builder)
        {
            this.builder = builder;
        }

        // ── IGrammar entry point ───────────────────────────────────────────────

        /// <inheritdoc/>
        public RuleFunction Begin() => LINE;

        // ── Grammar rules ─────────────────────────────────────────────────────

        // LINE: PIECE PIECES EOL
        private Rule LINE() => new Rule
        {
            Expect = new RuleFunction[] { PIECE, PIECES, EOL },
        };

        // PIECE: CHORDORNOTE PARAMS  (trigger: commitPiece)
        private Rule PIECE() => new Rule
        {
            Expect = new RuleFunction[] { CHORDORNOTE, PARAMS },
            Run = (state) => builder.CommitPiece(),
        };

        // PIECES: (COMMA PIECE)*
        private Rule PIECES() => new Rule
        {
            Expect = new RuleFunction[] { COMMA, PIECE },
            ZeroOrMore = true,
        };

        // PARAMS: DURATION TYPE DOTS OPTS  (all optional via their own Maybe flags)
        private Rule PARAMS() => new Rule
        {
            Expect = new RuleFunction[] { DURATION, TYPE, DOTS, OPTS },
        };

        // CHORDORNOTE: CHORD | SINGLENOTE
        private Rule CHORDORNOTE() => new Rule
        {
            Expect = new RuleFunction[] { CHORD, SINGLENOTE },
            Or = true,
        };

        // CHORD: LPAREN NOTES RPAREN  (trigger: addChord with matches[1])
        private Rule CHORD() => new Rule
        {
            Expect = new RuleFunction[] { LPAREN, NOTES, RPAREN },
            Run = (state) => builder.AddChord(state.Matches),
        };

        // NOTES: NOTE+
        private Rule NOTES() => new Rule
        {
            Expect = new RuleFunction[] { NOTE },
            OneOrMore = true,
        };

        // NOTE: NOTENAME ACCIDENTAL OCTAVE  (no trigger — used inside CHORD)
        private Rule NOTE() => new Rule
        {
            Expect = new RuleFunction[] { NOTENAME, ACCIDENTAL, OCTAVE },
            Run = (state) =>
            {
                // When parsing chord notes via NOTE rule, we still need to add each note.
                // The SINGLENOTE trigger does the same for single notes.
                // state.Matches: [noteName, accidental|null, octave]
                string key = (state.Matches.Count > 0 ? state.Matches[0] as string : null) ?? "";
                string? accid = state.Matches.Count > 1 ? state.Matches[1] as string : null;
                string octave = (state.Matches.Count > 2 ? state.Matches[2] as string : null) ?? "4";
                builder.AddSingleNote(key, accid, octave);
            },
        };

        // SINGLENOTE: NOTENAME ACCIDENTAL OCTAVE  (trigger: addSingleNote)
        private Rule SINGLENOTE() => new Rule
        {
            Expect = new RuleFunction[] { NOTENAME, ACCIDENTAL, OCTAVE },
            Run = (state) =>
            {
                string key = (state.Matches.Count > 0 ? state.Matches[0] as string : null) ?? "";
                string? accid = state.Matches.Count > 1 ? state.Matches[1] as string : null;
                string octave = (state.Matches.Count > 2 ? state.Matches[2] as string : null) ?? "4";
                builder.AddSingleNote(key, accid, octave);
            },
        };

        // ACCIDENTAL: (MICROTONES | ACCIDENTALS)?  — optional, or-alternatives
        private Rule ACCIDENTAL() => new Rule
        {
            Expect = new RuleFunction[] { MICROTONES, ACCIDENTALS },
            Maybe = true,
            Or = true,
        };

        // DOTS: DOT*  (trigger: setNoteDots with matches)
        private Rule DOTS() => new Rule
        {
            Expect = new RuleFunction[] { DOT },
            ZeroOrMore = true,
            Run = (state) => builder.SetNoteDots(state.Matches),
        };

        // TYPE: SLASH MAYBESLASH TYPES  (optional; trigger: setNoteType with matches[2])
        private Rule TYPE() => new Rule
        {
            Expect = new RuleFunction[] { SLASH, MAYBESLASH, TYPES },
            Maybe = true,
            Run = (state) =>
            {
                // matches[2] is the type letter (after two slashes)
                string? t = state.Matches.Count > 2 ? state.Matches[2] as string : null;
                builder.SetNoteType(t);
            },
        };

        // DURATION: SLASH DURATIONS  (optional; trigger: setNoteDuration with matches[1])
        private Rule DURATION() => new Rule
        {
            Expect = new RuleFunction[] { SLASH, DURATIONS },
            Maybe = true,
            Run = (state) =>
            {
                // matches[1] is the duration value (after the slash)
                string? d = state.Matches.Count > 1 ? state.Matches[1] as string : null;
                builder.SetNoteDuration(d);
            },
        };

        // OPTS: LBRACKET KEYVAL KEYVALS RBRACKET  (optional)
        private Rule OPTS() => new Rule
        {
            Expect = new RuleFunction[] { LBRACKET, KEYVAL, KEYVALS, RBRACKET },
            Maybe = true,
        };

        // KEYVALS: (COMMA KEYVAL)*
        private Rule KEYVALS() => new Rule
        {
            Expect = new RuleFunction[] { COMMA, KEYVAL },
            ZeroOrMore = true,
        };

        // KEYVAL: KEY EQUALS VAL  (trigger: addNoteOption)
        private Rule KEYVAL() => new Rule
        {
            Expect = new RuleFunction[] { KEY, EQUALS, VAL },
            Run = (state) =>
            {
                string key = (state.Matches.Count > 0 ? state.Matches[0] as string : null) ?? "";
                string raw = (state.Matches.Count > 2 ? state.Matches[2] as string : null) ?? "";
                // Strip surrounding quotes (both ' and ")
                string value = raw.Length >= 2 ? raw.Substring(1, raw.Length - 2) : raw;
                builder.AddNoteOption(key, value);
            },
        };

        // VAL: SVAL | DVAL
        private Rule VAL() => new Rule
        {
            Expect = new RuleFunction[] { SVAL, DVAL },
            Or = true,
        };

        // ── Token rules (lexer) ───────────────────────────────────────────────

        private Rule NOTENAME()    => new Rule { Token = "[a-gA-G]" };
        private Rule OCTAVE()      => new Rule { Token = "[0-9]+" };
        private Rule ACCIDENTALS() => new Rule { Token = "bb|b|##|#|n" };
        private Rule MICROTONES()  => new Rule { Token = @"bbs|bss|bs|db|d|\+\+-|\+-|\+\+|\+|k|o" };
        private Rule DURATIONS()   => new Rule { Token = "[0-9whq]+" };
        private Rule TYPES()       => new Rule { Token = "[rRsSmMhHgG]" };
        private Rule LPAREN()      => new Rule { Token = "[(]" };
        private Rule RPAREN()      => new Rule { Token = "[)]" };
        private Rule COMMA()       => new Rule { Token = "[,]" };
        private Rule DOT()         => new Rule { Token = "[.]" };
        private Rule SLASH()       => new Rule { Token = "[/]" };
        private Rule MAYBESLASH()  => new Rule { Token = "[/]?" };
        private Rule EQUALS()      => new Rule { Token = "[=]" };
        private Rule LBRACKET()    => new Rule { Token = @"\[" };
        private Rule RBRACKET()    => new Rule { Token = @"\]" };
        private Rule KEY()         => new Rule { Token = "[a-zA-Z][a-zA-Z0-9]*" };
        private Rule SVAL()        => new Rule { Token = "'[^']*'" };
        private Rule DVAL()        => new Rule { Token = "\"[^\"]*\"" };
        private Rule EOL()         => new Rule { Token = "$" };
    }

    // ── EasyScore ─────────────────────────────────────────────────────────────

    /// <summary>
    /// EasyScore implements a parser for VexFlow's concise notation language.
    /// Converts notation strings like "C4/q, D4, E4" into StaveNote arrays.
    ///
    /// Port of VexFlow's EasyScore class from easyscore.ts.
    /// </summary>
    public class EasyScore
    {
        // ── Fields ────────────────────────────────────────────────────────────

        /// <summary>The factory that owns this EasyScore instance.</summary>
        public Factory Factory { get; }

        private readonly Builder builder;
        private readonly EasyScoreGrammar grammar;
        private readonly Parser parser;

        /// <summary>Default clef for notes parsed without explicit clef option.</summary>
        public string DefaultClef { get; set; } = "treble";

        /// <summary>Default stem direction for notes: "auto", "up", or "down".</summary>
        public string DefaultStem { get; set; } = "auto";

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create an EasyScore with a bidirectional factory reference.
        /// Port of VexFlow's EasyScore constructor from easyscore.ts.
        /// </summary>
        public EasyScore(Factory factory)
        {
            Factory = factory;
            builder = new Builder(factory);
            grammar = new EasyScoreGrammar(builder);
            parser = new Parser(grammar);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Parse a notation string and return the resulting StaveNote list.
        ///
        /// Examples:
        ///   score.Notes("C4/q, D4, E4, F4")           → 4 quarter notes
        ///   score.Notes("(C4 E4 G4)/q")                → 1 chord with 3 keys
        ///   score.Notes("C4/q, D4, E4")                → 3 notes, D4+E4 inherit /q
        ///   score.Notes("C3/q", new NoteOptions { Clef = "bass" })  → bass clef
        ///
        /// Port of VexFlow's EasyScore.notes() from easyscore.ts line 524.
        /// </summary>
        public List<StaveNote> Notes(string noteStr, NoteOptions? options = null)
        {
            var opts = new NoteOptions
            {
                Stem = options?.Stem ?? DefaultStem,
                Clef = options?.Clef ?? DefaultClef,
            };
            builder.Reset(opts);
            var result = parser.Parse(noteStr);
            if (!result.Success)
                throw new VexFlowException("EasyScoreParseError",
                    $"EasyScore parse error at position {result.ErrorPos}: '{noteStr}'");
            return builder.Elements;
        }

        /// <summary>
        /// Create a Voice containing the given notes.
        /// Port of VexFlow's EasyScore.voice() from easyscore.ts line 530.
        /// </summary>
        public Voice Voice(List<StaveNote> notes, VoiceOptions? options = null)
        {
            var voice = Factory.Voice(options?.NumBeats, options?.BeatValue);
            voice.AddTickables(notes.Cast<Tickable>().ToList());
            return voice;
        }
    }
}

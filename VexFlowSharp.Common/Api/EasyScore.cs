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
        public string Stem { get; set; }

        /// <summary>Clef: "treble", "bass", etc.</summary>
        public string Clef { get; set; }
    }

    /// <summary>Default EasyScore options matching VexFlow's clef/time/stem defaults.</summary>
    public class EasyScoreDefaults : NoteOptions
    {
        public string Time { get; set; }
    }

    /// <summary>
    /// Options passed to EasyScore.Voice().
    /// </summary>
    public class VoiceOptions
    {
        public int? NumBeats { get; set; }
        public int? BeatValue { get; set; }
        public string Time { get; set; }
        public double? SoftmaxFactor { get; set; }
        public VoiceFormattingOptions Options { get; set; }
    }

    /// <summary>
    /// Nested voice formatting options matching VexFlow 5 EasyScore.voice(..., { options: ... }).
    /// </summary>
    public class VoiceFormattingOptions
    {
        public double? SoftmaxFactor { get; set; }
    }

    /// <summary>
    /// Options used when constructing EasyScore through Factory.EasyScore().
    /// Mirrors the v5 construction-time configuration for defaults, parse errors, and commit hooks.
    /// </summary>
    public class EasyScoreOptions
    {
        public EasyScoreDefaults Defaults { get; set; }
        public bool? ThrowOnError { get; set; }
        public List<Action<Builder, StemmableNote>> CommitHooks { get; set; }
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
        public string Accid { get; set; }

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
        public string Type { get; set; }
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

        private static readonly Dictionary<string, string> ArticulationNameToCode = new Dictionary<string, string>
        {
            ["staccato"] = "a.",
            ["tenuto"] = "a-",
            ["accent"] = "a>",
        };

        /// <summary>All stemmable note elements created during a parse.</summary>
        public List<StemmableNote> Elements { get; private set; } = new List<StemmableNote>();

        /// <summary>Commit hooks called after each note is committed.</summary>
        public List<Action<Builder, StemmableNote>> CommitHooks { get; set; } = new List<Action<Builder, StemmableNote>>();

        // ── Constructor ───────────────────────────────────────────────────────

        public Builder(Factory factory)
        {
            this.factory = factory;
            piece = new Piece(rollingDuration);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        /// <summary>Reset builder state for a new parse. Port of Builder.reset() from easyscore.ts.</summary>
        public void Reset(NoteOptions options = null)
        {
            optionStem = options.Stem ?? "auto";
            optionClef = options.Clef ?? "treble";
            Elements = new List<StemmableNote>();
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
        public void AddSingleNote(string key, string accid, string octave)
        {
            piece.Chord.Add(new NotePiece { Key = key, Accid = accid, Octave = octave });
        }

        /// <summary>
        /// Called by CHORD trigger: chord notes were already accumulated via SINGLENOTE (NOTE rule).
        /// Port of Builder.addChord() from easyscore.ts — the chord notes arrive via NOTE rule calls.
        /// </summary>
        public void AddChord(List<object> notes)
        {
            // Notes already accumulated into piece.Chord by individual NOTE/SINGLENOTE triggers.
            // This is a no-op in the C# port — CHORD's inner NOTE rules already called AddSingleNote.
        }

        /// <summary>
        /// Called by DURATION trigger. Sets rolling duration.
        /// Port of Builder.setNoteDuration() from easyscore.ts.
        /// </summary>
        public void SetNoteDuration(string duration)
        {
            rollingDuration = piece.Duration = duration ?? rollingDuration;
        }

        /// <summary>
        /// Called by TYPE trigger. Sets note type override.
        /// Port of Builder.setNoteType() from easyscore.ts.
        /// </summary>
        public void SetNoteType(string type)
        {
            if (type != null) piece.Type = type;
        }

        /// <summary>
        /// Called by DOTS trigger. Counts number of dots.
        /// Port of Builder.setNoteDots() from easyscore.ts.
        /// </summary>
        public void SetNoteDots(List<object> dots)
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
            string type = piece.Type;

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
            if (type.ToLower() == "g")
            {
                // Ghost note — use duration from piece
                var ghostNote = factory.GhostNote(new StaveNoteStruct
                {
                    Duration = duration,
                    Dots = dots,
                });
                if (!autoStem)
                    ghostNote.SetStemDirection(stem == "up" ? Stem.UP : Stem.DOWN);
                foreach (var hook in CommitHooks)
                    hook(this, ghostNote);

                Elements.Add(ghostNote);
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

            if (piece.Options.TryGetValue("id", out var id))
                note.SetId(id);
            if (piece.Options.TryGetValue("class", out var classList))
            {
                foreach (var className in classList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    note.AddClass(className.Trim());
            }

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

            AttachEasyScoreArticulations(note);
            AttachEasyScoreFingerings(note);
            AttachEasyScoreOrnaments(note);
            AttachEasyScoreAnnotations(note);

            // Run commit hooks
            foreach (var hook in CommitHooks)
                hook(this, note);

            Elements.Add(note);
            ResetPiece();
        }

        private void AttachEasyScoreArticulations(StaveNote note)
        {
            if (!piece.Options.TryGetValue("articulations", out var articulations) ||
                string.IsNullOrWhiteSpace(articulations))
                return;

            foreach (var articulationString in articulations.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var split = articulationString.Trim().Split(new[] { '.' }, 2);
                var name = split[0].Trim();
                if (name.Length == 0) continue;

                var type = ArticulationNameToCode.TryGetValue(name, out var code) ? code : name;
                var position = split.Length > 1 ? ParseModifierPosition(split[1]) : null;
                note.AddModifier(factory.Articulation(type, position), 0);
            }
        }

        private void AttachEasyScoreFingerings(StaveNote note)
        {
            if (!piece.Options.TryGetValue("fingerings", out var fingerings) ||
                string.IsNullOrWhiteSpace(fingerings))
                return;

            var index = 0;
            foreach (var fingeringString in fingerings.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var split = fingeringString.Trim().Split(new[] { '.' }, 2);
                var number = split[0].Trim();
                if (number.Length == 0) continue;

                var position = split.Length > 1 ? ParseModifierPosition(split[1]) : null;
                note.AddModifier(factory.Fingering(number, position), index);
                index++;
            }
        }

        private void AttachEasyScoreOrnaments(StaveNote note)
        {
            if (!piece.Options.TryGetValue("ornaments", out var ornaments) ||
                string.IsNullOrWhiteSpace(ornaments))
                return;

            foreach (var ornamentString in ornaments.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = ornamentString.Trim().Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                var options = new FactoryOrnamentOptions { Type = parts[0].Trim() };
                if (options.Type.Length == 0) continue;

                for (int i = 1; i < parts.Length; i++)
                {
                    var token = parts[i].Trim();
                    if (token.Length == 0) continue;
                    if (string.Equals(token, "delayed", StringComparison.OrdinalIgnoreCase))
                    {
                        options.Delayed = true;
                        continue;
                    }

                    options.Position ??= ParseModifierPosition(token);
                }

                note.AddModifier(factory.Ornament(options), 0);
            }
        }

        private void AttachEasyScoreAnnotations(StaveNote note)
        {
            var annotations = GetOptionValue("annotations") ?? GetOptionValue("annotation");
            if (string.IsNullOrWhiteSpace(annotations))
                return;

            foreach (var annotationString in annotations.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = annotationString.Trim().Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                var options = new FactoryAnnotationOptions { Text = parts[0].Trim() };
                if (options.Text.Length == 0) continue;

                for (int i = 1; i < parts.Length; i++)
                {
                    var token = parts[i].Trim();
                    if (token.Length == 0) continue;

                    if (IsAnnotationHorizontalJustify(token))
                    {
                        options.HJustifyString = token;
                        continue;
                    }

                    if (IsAnnotationVerticalJustify(token))
                    {
                        options.VJustifyString = token;
                    }
                }

                note.AddModifier(factory.Annotation(options), 0);
            }
        }

        private string GetOptionValue(string key)
        {
            return piece.Options.TryGetValue(key, out var value) ? value : null;
        }

        private static bool IsAnnotationHorizontalJustify(string token)
        {
            switch (token)
            {
                case "left":
                case "right":
                case "center":
                case "centerStem":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAnnotationVerticalJustify(string token)
        {
            switch (token)
            {
                case "above":
                case "top":
                case "below":
                case "bottom":
                case "center":
                case "centerStem":
                    return true;
                default:
                    return false;
            }
        }

        private static ModifierPosition? ParseModifierPosition(string position)
        {
            switch (position.Trim().ToLowerInvariant())
            {
                case "center":
                    return ModifierPosition.Center;
                case "left":
                    return ModifierPosition.Left;
                case "right":
                    return ModifierPosition.Right;
                case "above":
                    return ModifierPosition.Above;
                case "below":
                    return ModifierPosition.Below;
                default:
                    return null;
            }
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
                string accid = state.Matches.Count > 1 ? state.Matches[1] as string : null;
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
                string accid = state.Matches.Count > 1 ? state.Matches[1] as string : null;
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
                string t = state.Matches.Count > 2 ? state.Matches[2] as string : null;
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
                string d = state.Matches.Count > 1 ? state.Matches[1] as string : null;
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
        private Rule TYPES()       => new Rule { Token = "[rRsSmMhHgGxX]" };
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

        /// <summary>Default time signature used by <see cref="Voice(List{Note}, VoiceOptions)"/>.</summary>
        public string DefaultTime { get; set; } = "4/4";

        /// <summary>When true, <see cref="Parse"/> throws on parse failure, matching VexFlow's throwOnError option.</summary>
        public bool ThrowOnError { get; set; } = false;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create an EasyScore with a bidirectional factory reference.
        /// Port of VexFlow's EasyScore constructor from easyscore.ts.
        /// </summary>
        public EasyScore(Factory factory, EasyScoreOptions options = null)
        {
            Factory = factory;
            builder = new Builder(factory);
            grammar = new EasyScoreGrammar(builder);
            parser = new Parser(grammar);

            if (options != null) SetOptions(options);
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
        public List<StemmableNote> Notes(string noteStr, NoteOptions options = null)
        {
            var result = Parse(noteStr, options);
            if (!result.Success)
                throw new VexFlowException("EasyScoreParseError",
                    $"EasyScore parse error at position {result.ErrorPos}: '{noteStr}'");
            return builder.Elements;
        }

        /// <summary>
        /// Parse notation and return only concrete stave notes. This is a C# convenience
        /// for callers that do not expect v5 EasyScore ghost notes in the result list.
        /// </summary>
        public List<StaveNote> StaveNotes(string noteStr, NoteOptions options = null)
        {
            return Notes(noteStr, options).OfType<StaveNote>().ToList();
        }

        /// <summary>
        /// Parse a notation string and return the raw parser result.
        /// This mirrors VexFlow's EasyScore.parse() non-throwing path unless <see cref="ThrowOnError"/> is enabled.
        /// </summary>
        public ParseResult Parse(string noteStr, NoteOptions options = null)
        {
            var opts = new NoteOptions
            {
                Stem = options.Stem ?? DefaultStem,
                Clef = options.Clef ?? DefaultClef,
            };
            builder.Reset(opts);
            var result = parser.Parse(noteStr);
            if (!result.Success && ThrowOnError)
                throw new VexFlowException("EasyScoreParseError",
                    $"EasyScore parse error at position {result.ErrorPos}: '{noteStr}'");
            return result;
        }

        /// <summary>Add a callback that runs after each note is committed by the EasyScore builder.</summary>
        public EasyScore AddCommitHook(Action<Builder, StemmableNote> commitHook)
        {
            builder.CommitHooks.Add(commitHook);
            return this;
        }

        /// <summary>Set the render context on the underlying factory.</summary>
        public EasyScore SetContext(RenderContext context)
        {
            Factory.SetContext(context);
            return this;
        }

        /// <summary>Set EasyScore defaults for subsequent Notes() and Voice() calls.</summary>
        public EasyScore Set(EasyScoreDefaults defaults)
        {
            if (defaults.Clef != null) DefaultClef = defaults.Clef;
            if (defaults.Stem != null) DefaultStem = defaults.Stem;
            if (defaults.Time != null) DefaultTime = defaults.Time;
            return this;
        }

        /// <summary>Apply EasyScore construction options after creation, mirroring VexFlow 5 setOptions().</summary>
        public EasyScore SetOptions(EasyScoreOptions options)
        {
            if (options.Defaults != null) Set(options.Defaults);
            if (options.ThrowOnError != null) ThrowOnError = options.ThrowOnError.Value;
            if (options.CommitHooks != null)
            {
                foreach (var hook in options.CommitHooks)
                    AddCommitHook(hook);
            }
            return this;
        }

        /// <summary>Create a Beam for the given notes and return the same note list for fluent EasyScore composition.</summary>
        public List<StemmableNote> Beam(List<StemmableNote> notes, FactoryBeamOptions options = null)
        {
            Factory.Beam(notes, options);
            return notes;
        }

        /// <summary>Create a Tuplet for the given notes and return the same note list for fluent EasyScore composition.</summary>
        public List<StemmableNote> Tuplet(List<StemmableNote> notes, TupletOptions options = null)
        {
            Factory.Tuplet(notes.Cast<Note>().ToList(), options);
            return notes;
        }

        /// <summary>
        /// Create a Voice containing the given notes.
        /// Port of VexFlow's EasyScore.voice() from easyscore.ts line 530.
        /// </summary>
        public Voice Voice(List<StaveNote> notes, VoiceOptions options = null)
            => Voice(notes.Cast<Note>().ToList(), options);

        /// <summary>Create a Voice containing the given stemmable notes.</summary>
        public Voice Voice(List<StemmableNote> notes, VoiceOptions options = null)
            => Voice(notes.Cast<Note>().ToList(), options);

        /// <summary>Create a Voice containing the given notes.</summary>
        public Voice Voice(List<Note> notes, VoiceOptions options = null)
        {
            Voice voice;
            if (!string.IsNullOrWhiteSpace(options.Time))
                voice = Factory.Voice(options.Time!);
            else if (options.NumBeats != null || options.BeatValue != null)
                voice = Factory.Voice(options.NumBeats, options.BeatValue);
            else
                voice = Factory.Voice(DefaultTime);

            var softmaxFactor = options.SoftmaxFactor ?? options.Options?.SoftmaxFactor;
            if (softmaxFactor.HasValue)
                voice.SetSoftmaxFactor(softmaxFactor.Value);

            voice.AddTickables(notes.Cast<Tickable>().ToList());
            return voice;
        }
    }
}

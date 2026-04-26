#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's Parser class (parser.ts, 263 lines).
// A generic recursive-descent parser framework used by EasyScore.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VexFlowSharp.Api
{
    // ── Delegate types ────────────────────────────────────────────────────────

    /// <summary>
    /// A function that returns a grammar Rule.
    /// Port of VexFlow's RuleFunction type from parser.ts.
    /// </summary>
    public delegate Rule RuleFunction();

    /// <summary>
    /// A trigger function called when a grammar rule matches successfully.
    /// Receives the parser state (with accumulated matches) for the matched rule.
    /// Port of VexFlow's TriggerFunction type from parser.ts.
    /// </summary>
    public delegate void TriggerFunction(ParserState state);

    // ── IGrammar ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Interface that a grammar must implement.
    /// Port of VexFlow's Grammar interface from parser.ts.
    /// </summary>
    public interface IGrammar
    {
        /// <summary>Return the entry-point RuleFunction for this grammar.</summary>
        RuleFunction Begin();
    }

    // ── ParserState ───────────────────────────────────────────────────────────

    /// <summary>
    /// Mutable state passed to TriggerFunction callbacks.
    /// Contains the flattened list of string matches for the current rule.
    /// Port of VexFlow's implicit state object from parser.ts (state.matches).
    /// </summary>
    public class ParserState
    {
        /// <summary>Flattened list of matched strings (nulls for optional non-matches).</summary>
        public List<object?> Matches { get; set; } = new List<object?>();
    }

    // ── Rule ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes a grammar rule — either a lexer rule (Token) or parser rule (Expect).
    /// Port of VexFlow's Rule interface from parser.ts.
    /// </summary>
    public class Rule
    {
        // ── Lexer rule fields ─────────────────────────────────────────────────

        /// <summary>
        /// Regex pattern string for a lexer rule.
        /// When set, the parser matches this token at the current position.
        /// </summary>
        public string? Token { get; set; }

        /// <summary>When true, do not skip trailing whitespace after matching the token.</summary>
        public bool NoSpace { get; set; } = false;

        // ── Parser rule fields ────────────────────────────────────────────────

        /// <summary>Array of sub-rule functions to match in sequence (or as alternatives with Or).</summary>
        public RuleFunction[]? Expect { get; set; }

        /// <summary>Match zero or more times (always succeeds; position reset if zero matches).</summary>
        public bool ZeroOrMore { get; set; } = false;

        /// <summary>Match one or more times (fails if no match found).</summary>
        public bool OneOrMore { get; set; } = false;

        /// <summary>Optional match — always succeeds; position reset if not matched.</summary>
        public bool Maybe { get; set; } = false;

        /// <summary>When true, try each Expect alternative and stop at first success.</summary>
        public bool Or { get; set; } = false;

        /// <summary>Trigger function called after a successful rule match.</summary>
        public TriggerFunction? Run { get; set; }
    }

    // ── ParseResult ───────────────────────────────────────────────────────────

    /// <summary>
    /// Result of matching a rule or token.
    /// Port of VexFlow's Result interface from parser.ts.
    /// </summary>
    public class ParseResult
    {
        /// <summary>Whether the match succeeded.</summary>
        public bool Success { get; set; }

        // Lexer result fields
        /// <summary>Character position in the string where this match started.</summary>
        public int Pos { get; set; }

        /// <summary>How many characters to advance the position after this match.</summary>
        public int IncrementPos { get; set; }

        /// <summary>The captured token string (for lexer matches). Null for parser matches.</summary>
        public string? MatchedString { get; set; }

        // Parser result fields
        /// <summary>Flattened match list built by the expect() method.</summary>
        public List<object?> Matches { get; set; } = new List<object?>();

        /// <summary>Number of successful sub-matches.</summary>
        public int NumMatches { get; set; }

        /// <summary>Grouped sub-results (list of ParseResult or List&lt;object?&gt;).</summary>
        public List<object?> Results { get; set; } = new List<object?>();

        /// <summary>Position of the first parse error. -1 if no error.</summary>
        public int ErrorPos { get; set; } = -1;
    }

    // ── Parser ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generic recursive-descent parser. Given a grammar (IGrammar), it parses a
    /// line and executes trigger functions as rules match.
    ///
    /// Port of VexFlow's Parser class from parser.ts.
    /// </summary>
    public class Parser
    {
        private const int NO_ERROR_POS = -1;

        private readonly IGrammar grammar;
        private string line = "";
        private int pos = 0;
        private int errorPos = NO_ERROR_POS;

        /// <summary>
        /// Create a parser with the given grammar.
        /// Port of VexFlow's Parser constructor from parser.ts.
        /// </summary>
        public Parser(IGrammar grammar)
        {
            this.grammar = grammar;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void MatchFail(int returnPos)
        {
            if (errorPos == NO_ERROR_POS) errorPos = pos;
            pos = returnPos;
        }

        private void MatchSuccess()
        {
            errorPos = NO_ERROR_POS;
        }

        /// <summary>
        /// Try to match a token regex at the current position.
        /// Port of Parser.matchToken() from parser.ts lines 114-128.
        /// CRITICAL: slices from current position before regex matching.
        /// </summary>
        private ParseResult MatchToken(string token, bool noSpace = false)
        {
            string pattern = noSpace
                ? $"^(({token}))"
                : $"^(({token})\\s*)";
            string workingLine = line.Substring(pos);
            var m = Regex.Match(workingLine, pattern);
            if (m.Success)
            {
                return new ParseResult
                {
                    Success = true,
                    MatchedString = m.Groups[2].Value,
                    IncrementPos = m.Groups[1].Length,
                    Pos = pos,
                };
            }
            return new ParseResult { Success = false, Pos = pos };
        }

        /// <summary>
        /// Flattens the grouped parser result tree into a flat list for trigger functions.
        /// Leaves are MatchedString values (strings), missing optionals become null.
        ///
        /// Port of VexFlow's flattenMatches() from parser.ts lines 57-64.
        /// </summary>
        private static List<object?> FlattenMatches(List<object?> results)
        {
            var flat = new List<object?>();
            foreach (var r in results)
            {
                if (r is ParseResult pr)
                {
                    if (pr.MatchedString != null)
                    {
                        // Leaf: a lexer token match
                        flat.Add(pr.MatchedString);
                    }
                    else if (pr.Results != null && pr.Results.Count > 0)
                    {
                        // Parser result with sub-results: recurse
                        flat.AddRange(FlattenMatches(pr.Results));
                    }
                    else
                    {
                        // Empty parser result (e.g. maybe with no match)
                        flat.Add(null);
                    }
                }
                else if (r is List<object?> list)
                {
                    // Nested list from oneOrMore/zeroOrMore
                    flat.AddRange(FlattenMatches(list));
                }
                else
                {
                    flat.Add(r);
                }
            }
            return flat;
        }

        /// <summary>
        /// Execute rule to match a sequence of tokens (or rules). If maybe is set,
        /// return success even if the token is not found, but reset the position.
        /// Port of Parser.expectOne() from parser.ts lines 134-174.
        /// </summary>
        private ParseResult ExpectOne(Rule rule, bool maybe = false)
        {
            var results = new List<object?>();
            int savedPos = pos;

            bool allMatches = true;
            bool oneMatch = false;
            maybe = maybe || rule.Maybe;

            if (rule.Expect != null)
            {
                foreach (var next in rule.Expect)
                {
                    int localPos = pos;
                    var result = Expect(next);

                    if (result.Success)
                    {
                        results.Add(result);
                        oneMatch = true;
                        if (rule.Or) break;
                    }
                    else
                    {
                        allMatches = false;
                        if (!rule.Or)
                        {
                            pos = localPos;
                            break;
                        }
                    }
                }
            }

            bool gotOne = (rule.Or && oneMatch) || allMatches;
            bool success = gotOne || maybe;
            int numMatches = gotOne ? 1 : 0;

            if (maybe && !gotOne) pos = savedPos;

            if (success)
                MatchSuccess();
            else
                MatchFail(savedPos);

            return new ParseResult { Success = success, Results = results, NumMatches = numMatches };
        }

        /// <summary>
        /// Try to match one or more instances of the rule.
        /// Port of Parser.expectOneOrMore() from parser.ts lines 178-202.
        /// </summary>
        private ParseResult ExpectOneOrMore(Rule rule, bool maybe = false)
        {
            var results = new List<object?>();
            int savedPos = pos;
            int numMatches = 0;
            bool more = true;

            do
            {
                var result = ExpectOne(rule);
                if (result.Success)
                {
                    numMatches++;
                    results.Add(result.Results);
                }
                else
                {
                    more = false;
                }
            } while (more);

            bool success = numMatches > 0 || maybe;
            if (maybe && !(numMatches > 0)) pos = savedPos;

            if (success)
                MatchSuccess();
            else
                MatchFail(savedPos);

            return new ParseResult { Success = success, Results = results, NumMatches = numMatches };
        }

        /// <summary>
        /// Match zero or more instances of rule. Offloads to ExpectOneOrMore.
        /// Port of Parser.expectZeroOrMore() from parser.ts line 205-207.
        /// </summary>
        private ParseResult ExpectZeroOrMore(Rule rule)
        {
            return ExpectOneOrMore(rule, maybe: true);
        }

        /// <summary>
        /// Execute the rule produced by the provided ruleFunc.
        /// Handles both lexer rules (token) and parser rules (expect).
        /// Fires trigger functions on success.
        ///
        /// Port of Parser.expect() from parser.ts lines 212-262.
        /// </summary>
        private ParseResult Expect(RuleFunction ruleFunc)
        {
            if (ruleFunc == null)
                throw new VexFlowException("BadArguments", "Invalid rule function");

            ParseResult result;

            // Bind rule function to grammar (mirrors ruleFunc.bind(this.grammar)() in TS)
            Rule rule = ruleFunc();

            if (rule.Token != null)
            {
                // Lexer rule: match regex token
                result = MatchToken(rule.Token, rule.NoSpace);
                if (result.Success)
                {
                    pos += result.IncrementPos;
                }
            }
            else if (rule.Expect != null)
            {
                // Parser rule
                if (rule.OneOrMore)
                    result = ExpectOneOrMore(rule);
                else if (rule.ZeroOrMore)
                    result = ExpectZeroOrMore(rule);
                else
                    result = ExpectOne(rule);
            }
            else
            {
                throw new VexFlowException("BadArguments", "Bad grammar! No token or expect property");
            }

            // Build the flat matches list from sub-results
            var matches = new List<object?>();
            result.Matches = matches;
            if (result.Results != null && result.Results.Count > 0)
            {
                matches.AddRange(FlattenMatches(result.Results));
            }

            // Fire trigger if rule matched
            if (rule.Run != null && result.Success)
            {
                var state = new ParserState { Matches = matches };
                rule.Run(state);
            }

            return result;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Parse a line using the grammar. Returns success=true if fully parsed.
        /// Sets ErrorPos to the position of the first error on failure.
        ///
        /// Port of Parser.parse() from parser.ts lines 94-101.
        /// </summary>
        public ParseResult Parse(string line)
        {
            this.line = line;
            this.pos = 0;
            this.errorPos = NO_ERROR_POS;
            var result = Expect(grammar.Begin());
            result.ErrorPos = this.errorPos;
            return result;
        }
    }
}

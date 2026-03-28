// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using NUnit.Framework;
using VexFlowSharp.Api;

namespace VexFlowSharp.Tests.Api
{
    [TestFixture]
    [Category("Parser")]
    [Category("Phase5")]
    public class ParserTests
    {
        // ── Minimal grammar for testing ────────────────────────────────────────

        /// <summary>
        /// Simple grammar that matches: TOKEN TOKEN? TOKEN*
        /// Used to verify Parser infrastructure works correctly.
        /// </summary>
        private class TestGrammar : IGrammar
        {
            public RuleFunction Begin() => LINE;

            private Rule LINE() => new Rule
            {
                Expect = new RuleFunction[] { WORD, WORDS, EOL },
            };

            private Rule WORDS() => new Rule
            {
                Expect = new RuleFunction[] { WORD },
                ZeroOrMore = true,
            };

            private Rule WORD() => new Rule { Token = "[a-z]+" };
            private Rule EOL()  => new Rule { Token = "$" };
        }

        /// <summary>Grammar with Or combinator for alternative matching.</summary>
        private class OrGrammar : IGrammar
        {
            public string? LastMatch;

            public RuleFunction Begin() => LINE;

            private Rule LINE() => new Rule
            {
                Expect = new RuleFunction[] { EITHER, EOL },
            };

            private Rule EITHER() => new Rule
            {
                Expect = new RuleFunction[] { AAA, BBB },
                Or = true,
                Run = (state) => LastMatch = state.Matches.Count > 0 ? state.Matches[0] as string : null,
            };

            private Rule AAA() => new Rule { Token = "aaa" };
            private Rule BBB() => new Rule { Token = "bbb" };
            private Rule EOL() => new Rule { Token = "$" };
        }

        /// <summary>Grammar with a trigger to capture match list.</summary>
        private class TriggerGrammar : IGrammar
        {
            public List<object?> CapturedMatches = new List<object?>();

            public RuleFunction Begin() => LINE;

            private Rule LINE() => new Rule
            {
                Expect = new RuleFunction[] { PAIR, EOL },
                Run = (state) => CapturedMatches = new List<object?>(state.Matches),
            };

            private Rule PAIR() => new Rule
            {
                Expect = new RuleFunction[] { WORD, WORD },
            };

            private Rule WORD() => new Rule { Token = "[a-z]+" };
            private Rule EOL()  => new Rule { Token = "$" };
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void MatchesSimpleToken()
        {
            var grammar = new TestGrammar();
            var parser = new Parser(grammar);

            var result = parser.Parse("hello");
            Assert.IsTrue(result.Success, "Simple word should parse successfully");
            Assert.AreEqual(-1, result.ErrorPos, "No error on successful parse");
        }

        [Test]
        public void MatchesMultipleTokens()
        {
            var grammar = new TestGrammar();
            var parser = new Parser(grammar);

            var result = parser.Parse("hello world foo");
            Assert.IsTrue(result.Success, "Multiple words should parse successfully");
        }

        [Test]
        public void OrCombinatorSelectsFirst()
        {
            var grammar = new OrGrammar();
            var parser = new Parser(grammar);

            var result = parser.Parse("aaa");
            Assert.IsTrue(result.Success, "Or combinator should match 'aaa'");
            Assert.AreEqual("aaa", grammar.LastMatch, "LastMatch should be 'aaa'");
        }

        [Test]
        public void OrCombinatorSelectsSecond()
        {
            var grammar = new OrGrammar();
            var parser = new Parser(grammar);

            var result = parser.Parse("bbb");
            Assert.IsTrue(result.Success, "Or combinator should match 'bbb'");
            Assert.AreEqual("bbb", grammar.LastMatch, "LastMatch should be 'bbb'");
        }

        [Test]
        public void ZeroOrMoreAcceptsEmpty()
        {
            var grammar = new TestGrammar();
            var parser = new Parser(grammar);

            // LINE = WORD WORDS EOL where WORDS = WORD* (zeroOrMore)
            // Single word: WORDS matches zero times and succeeds
            var result = parser.Parse("hello");
            Assert.IsTrue(result.Success, "ZeroOrMore should succeed even with no additional matches");
        }

        [Test]
        public void FlattenMatchesProducesCorrectList()
        {
            var grammar = new TriggerGrammar();
            var parser = new Parser(grammar);

            var result = parser.Parse("hello world");
            Assert.IsTrue(result.Success, "Two words should parse successfully");
            // LINE trigger captures PAIR + EOL: PAIR flattens to [hello, world], EOL adds ""
            // Total: ["hello", "world", ""] — 3 matches
            Assert.AreEqual(3, grammar.CapturedMatches.Count, "Should have 3 captured matches (2 words + EOL)");
            Assert.AreEqual("hello", grammar.CapturedMatches[0] as string);
            Assert.AreEqual("world", grammar.CapturedMatches[1] as string);
            Assert.AreEqual("", grammar.CapturedMatches[2] as string, "EOL match is empty string");
        }

        [Test]
        public void FailsOnUnrecognizedInput()
        {
            var grammar = new TestGrammar();
            var parser = new Parser(grammar);

            // Numbers are not in [a-z]+
            var result = parser.Parse("123");
            Assert.IsFalse(result.Success, "Input with only numbers should fail");
            Assert.GreaterOrEqual(result.ErrorPos, 0, "ErrorPos should be set on failure");
        }
    }
}

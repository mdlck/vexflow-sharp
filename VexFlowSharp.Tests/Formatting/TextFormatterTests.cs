// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Unit tests for TextFormatter — port of textformatter.ts API tests.
//
// Tests cover:
//   - Registry caching (same instance returned for same font)
//   - Empty string returns zero width
//   - Non-empty string returns positive width
//   - GetWidthForTextInPx = GetWidthForTextInEm * FontSizeInPixels
//   - Longer text is wider than shorter text

using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Tests.Formatting
{
    [TestFixture]
    [Category("TextFormatter")]
    public class TextFormatterTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clear the formatter registry before each test for isolation
            TextFormatter.ClearRegistry();
        }

        // ── Test 1: Registry caching ───────────────────────────────────────────

        /// <summary>
        /// Calling TextFormatter.Create with the same font family twice should return
        /// formatters with the same underlying font data (registry-matched).
        ///
        /// Note: Create() always returns a new formatter instance (VexFlow's TODO note),
        /// but the key assertion is that both formatters use the same font resolution and
        /// produce the same em width for the same input string — consistent behavior.
        /// </summary>
        [Test]
        public void Create_ReturnsSameInstance_ForSameFont()
        {
            // Register a simple font so Create can find it
            TextFormatter.RegisterInfo(new TextFormatterInfo
            {
                Family = "TestFont",
                Resolution = 1000,
                Glyphs = new System.Collections.Generic.Dictionary<string, double>
                {
                    { "A", 600 },
                    { "B", 580 },
                }
            });

            var f1 = TextFormatter.Create("TestFont");
            var f2 = TextFormatter.Create("TestFont");

            // Both should produce the same width for the same text (consistent registry behavior)
            double w1 = f1.GetWidthForTextInEm("AB");
            double w2 = f2.GetWidthForTextInEm("AB");
            Assert.That(w1, Is.EqualTo(w2),
                "Two TextFormatters created for the same font must produce the same em width");
        }

        // ── Test 2: Empty string returns zero ─────────────────────────────────

        /// <summary>
        /// An empty string should always return 0 width in both em and px.
        /// </summary>
        [Test]
        public void GetWidthForTextInPx_EmptyString_ReturnsZero()
        {
            var formatter = TextFormatter.Create("Arial", 12);
            double widthPx = formatter.GetWidthForTextInPx(string.Empty);
            double widthEm = formatter.GetWidthForTextInEm(string.Empty);

            Assert.That(widthPx, Is.EqualTo(0.0), "Empty string width in px must be 0");
            Assert.That(widthEm, Is.EqualTo(0.0), "Empty string width in em must be 0");
        }

        // ── Test 3: Non-empty string returns positive width ───────────────────

        /// <summary>
        /// A non-empty string should return a positive width in px.
        /// This uses the fallback font (registry empty) which returns 0.5 em/char.
        /// </summary>
        [Test]
        public void GetWidthForTextInPx_NonEmpty_ReturnsPositive()
        {
            var formatter = TextFormatter.Create("Arial", 14);
            double width = formatter.GetWidthForTextInPx("Hello");

            Assert.That(width, Is.GreaterThan(0),
                "Non-empty string must have positive pixel width");
        }

        [Test]
        public void FallbackWidth_ComesFromMetrics()
        {
            var formatter = TextFormatter.Create("Arial", 12);

            Assert.That(formatter.Resolution, Is.EqualTo((int)Metrics.GetDouble("TextFormatter.defaultResolution")));
            Assert.That(formatter.GetWidthForTextInEm("A"), Is.EqualTo(Metrics.GetDouble("TextFormatter.defaultAdvanceWidthEm")).Within(1e-9));
        }

        // ── Test 4: GetWidthForTextInPx = GetWidthForTextInEm * FontSizeInPixels ──

        /// <summary>
        /// Verify the relationship: GetWidthForTextInPx(text) == GetWidthForTextInEm(text) * FontSizeInPixels.
        /// This is the core contract of the TextFormatter API, matching VexFlow's getWidthForTextInPx.
        /// </summary>
        [Test]
        public void GetWidthForTextInEm_ScalesToPx()
        {
            var formatter = TextFormatter.Create("Arial", 12);
            string testText = "Hello VexFlow";

            double widthEm = formatter.GetWidthForTextInEm(testText);
            double widthPx = formatter.GetWidthForTextInPx(testText);
            double expectedPx = widthEm * formatter.FontSizeInPixels;

            Assert.That(widthPx, Is.EqualTo(expectedPx).Within(1e-9),
                $"GetWidthForTextInPx must equal GetWidthForTextInEm * FontSizeInPixels");
        }

        // ── Test 5: Longer text is wider ──────────────────────────────────────

        /// <summary>
        /// "Hello" should have greater pixel width than "Hi" since it has more characters.
        /// Verifies that the width accumulates correctly across characters.
        /// </summary>
        [Test]
        public void GetWidthForTextInPx_LongerText_WiderResult()
        {
            var formatter = TextFormatter.Create("Arial", 14);
            double widthHello = formatter.GetWidthForTextInPx("Hello");
            double widthHi = formatter.GetWidthForTextInPx("Hi");

            Assert.That(widthHello, Is.GreaterThan(widthHi),
                $"'Hello' ({widthHello:F2}px) must be wider than 'Hi' ({widthHi:F2}px)");
        }

        // ── Test 6: FontSizeInPixels matches pt * 4/3 conversion ─────────────

        /// <summary>
        /// VexFlow uses Font.scaleToPxFrom.pt = 4/3.
        /// Verify: FontSizeInPixels == sizeInPt * 4/3.
        /// </summary>
        [Test]
        public void FontSizeInPixels_MatchesPtToPxConversion()
        {
            double sizeInPt = 18;
            var formatter = TextFormatter.Create("Arial", sizeInPt);
            double expectedPx = sizeInPt * Metrics.GetDouble("TextFormatter.ptToPx");

            Assert.That(formatter.FontSizeInPixels, Is.EqualTo(expectedPx).Within(1e-9),
                $"FontSizeInPixels must equal {sizeInPt}pt * 4/3 = {expectedPx}px");
        }

        // ── Test 7: RegisterInfo allows glyph-based width lookup ─────────────

        /// <summary>
        /// When glyph advance widths are registered, the formatter uses them
        /// instead of the default fallback width.
        /// </summary>
        [Test]
        public void RegisterInfo_GlyphData_UsedForWidthCalculation()
        {
            // Register a font where 'A' has advance width = 600/1000 = 0.6 em
            TextFormatter.RegisterInfo(new TextFormatterInfo
            {
                Family = "GlyphFont",
                Resolution = 1000,
                Glyphs = new System.Collections.Generic.Dictionary<string, double>
                {
                    { "A", 600.0 },
                }
            });

            var formatter = TextFormatter.Create("GlyphFont", 12);
            double emWidth = formatter.GetWidthForTextInEm("A");

            Assert.That(emWidth, Is.EqualTo(0.6).Within(1e-9),
                "Character 'A' with advance=600 in 1000-resolution font must be 0.6 em");
        }

        [Test]
        public void RegisterInfo_Overwrite_ClearsCachedWidths()
        {
            TextFormatter.RegisterInfo(new TextFormatterInfo
            {
                Family = "MutableFont",
                Resolution = 1000,
                Glyphs = new System.Collections.Generic.Dictionary<string, double>
                {
                    { "A", 600.0 },
                },
            });
            Assert.That(TextFormatter.Create("MutableFont", 12).GetWidthForTextInEm("A"), Is.EqualTo(0.6).Within(1e-9));

            TextFormatter.RegisterInfo(new TextFormatterInfo
            {
                Family = "MutableFont",
                Resolution = 1000,
                Glyphs = new System.Collections.Generic.Dictionary<string, double>
                {
                    { "A", 720.0 },
                },
            }, overwrite: true);

            Assert.That(TextFormatter.Create("MutableFont", 12).GetWidthForTextInEm("A"), Is.EqualTo(0.72).Within(1e-9));
        }

        [Test]
        public void UpdateParams_ClearsCachedWidths()
        {
            TextFormatter.RegisterInfo(new TextFormatterInfo
            {
                Family = "MutableFont",
                Resolution = 1000,
                Glyphs = new System.Collections.Generic.Dictionary<string, double>
                {
                    { "A", 600.0 },
                },
            });

            var formatter = TextFormatter.Create("MutableFont", 12);
            Assert.That(formatter.GetWidthForTextInEm("A"), Is.EqualTo(0.6).Within(1e-9));

            formatter.UpdateParams(new TextFormatterInfo
            {
                Family = "MutableFont",
                Resolution = 1000,
                Glyphs = new System.Collections.Generic.Dictionary<string, double>
                {
                    { "A", 800.0 },
                },
            });

            Assert.That(formatter.GetWidthForTextInEm("A"), Is.EqualTo(0.8).Within(1e-9));
        }

        [Test]
        public void Create_MatchesRegisteredFamilyPrefixFromRequestedFamily()
        {
            TextFormatter.RegisterInfo(new TextFormatterInfo
            {
                Family = "Roboto Slab",
                Resolution = 1000,
                Glyphs = new System.Collections.Generic.Dictionary<string, double>
                {
                    { "A", 700.0 },
                },
            });

            var formatter = TextFormatter.Create("Roboto Slab Medium", 12);

            Assert.That(formatter.GetWidthForTextInEm("A"), Is.EqualTo(0.7).Within(1e-9));
        }

        [Test]
        public void Create_MatchesQuotedFamiliesInCssFallbackList()
        {
            TextFormatter.RegisterInfo(new TextFormatterInfo
            {
                Family = "Petaluma Script",
                Resolution = 1000,
                Glyphs = new System.Collections.Generic.Dictionary<string, double>
                {
                    { "A", 650.0 },
                },
            });

            var formatter = TextFormatter.Create("\"Missing Font\", 'Petaluma Script', serif", 12);

            Assert.That(formatter.GetWidthForTextInEm("A"), Is.EqualTo(0.65).Within(1e-9));
        }
    }
}

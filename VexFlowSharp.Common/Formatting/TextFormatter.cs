#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's TextFormatter class (textformatter.ts, 346 lines).
// TextFormatter measures the pixel width of text strings for layout purposes.
// It uses per-font glyph advance width data (if available) and falls back
// to a default advance width when glyph metrics are not registered.

using System;
using System.Collections.Generic;

namespace VexFlowSharp.Common.Formatting
{
    /// <summary>
    /// Information record for a registered text font, describing its metrics.
    /// Port of VexFlow's TextFormatterInfo interface from textformatter.ts.
    /// </summary>
    public class TextFormatterInfo
    {
        /// <summary>Font family name (used as registry key).</summary>
        public string Family { get; set; } = string.Empty;

        /// <summary>
        /// Units-per-em resolution.
        /// Text fonts typically use 2048; music fonts (Bravura) use 1000.
        /// </summary>
        public int Resolution { get; set; } = 1000;

        /// <summary>
        /// Per-character glyph advance widths keyed by the character string.
        /// The advanceWidth value is in font units (relative to Resolution).
        /// </summary>
        public Dictionary<string, double> Glyphs { get; set; } = new Dictionary<string, double>();

        /// <summary>Whether the font has serifs.</summary>
        public bool Serifs { get; set; } = false;

        /// <summary>Whether the font is monospaced.</summary>
        public bool Monospaced { get; set; } = false;

        /// <summary>Whether the font is italic.</summary>
        public bool Italic { get; set; } = false;

        /// <summary>Whether the font is bold.</summary>
        public bool Bold { get; set; } = false;

        /// <summary>Character used to determine the max height glyph.</summary>
        public string MaxSizeGlyph { get; set; } = "@";

        /// <summary>Description of the font.</summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Measures text width in em units and pixels for annotation layout.
    ///
    /// Port of VexFlow's TextFormatter class from textformatter.ts.
    ///
    /// Usage:
    ///   var formatter = TextFormatter.Create("Arial");
    ///   double widthPx = formatter.GetWidthForTextInPx("Hello");
    ///
    /// The registry pattern allows the same formatter instance to be reused
    /// for the same font family (reducing allocation and caching computed widths).
    ///
    /// If no glyph metric data is registered for the requested font, a fallback
    /// formatter is returned that uses a default advance width of 0.5 em per character.
    /// TODO (Phase 4/5): Register actual text font glyph data (RobotoSlab, PetalumaScript, etc.)
    /// </summary>
    public class TextFormatter
    {
        // ── Static registry ────────────────────────────────────────────────────

        /// <summary>Registry mapping font family names to TextFormatterInfo records.</summary>
        private static readonly Dictionary<string, TextFormatterInfo> _registry =
            new Dictionary<string, TextFormatterInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Two-level width cache: [cacheKey][textString] → width in em.
        /// Same structure as VexFlow's textWidthCache.
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string, double>> _textWidthCache =
            new Dictionary<string, Dictionary<string, double>>();

        // ── Static API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Register font metrics so they are available to <see cref="Create"/>.
        /// </summary>
        /// <param name="info">Font information record.</param>
        /// <param name="overwrite">If true, overwrite an existing registration.</param>
        public static void RegisterInfo(TextFormatterInfo info, bool overwrite = false)
        {
            if (!_registry.ContainsKey(info.Family) || overwrite)
            {
                _registry[info.Family] = info;
                _textWidthCache.Clear();
            }
        }

        /// <summary>
        /// Return all registered font families.
        /// </summary>
        public static IEnumerable<TextFormatterInfo> GetFontFamilies()
        {
            return _registry.Values;
        }

        /// <summary>
        /// Create a TextFormatter for the given font family.
        ///
        /// Port of VexFlow's TextFormatter.create(requestedFont).
        /// Looks up the registry for a matching font. If no match is found,
        /// returns a fallback formatter using the first registered font,
        /// or a synthetic fallback if the registry is empty.
        /// </summary>
        /// <param name="fontFamily">
        /// Font family name, e.g. "Arial". May be a comma-separated CSS font-family list.
        /// </param>
        /// <param name="fontSizeInPt">Optional font size in points. Default: 14pt.</param>
        public static TextFormatter Create(string fontFamily = "sans-serif", double fontSizeInPt = 14)
        {
            // Support comma-separated CSS font-family strings (e.g. "PetalumaScript, Arial, sans-serif")
            var requestedFamilies = fontFamily.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            TextFormatterInfo? matched = null;
            foreach (var requested in requestedFamilies)
            {
                var trimmed = NormalizeFamilyName(requested);
                foreach (var registeredFamily in _registry.Keys)
                {
                    // VexFlow startsWith matching: "Roboto Slab Medium" matches "Roboto Slab"
                    if (trimmed.StartsWith(registeredFamily, StringComparison.OrdinalIgnoreCase))
                    {
                        matched = _registry[registeredFamily];
                        break;
                    }
                }
                if (matched != null) break;
            }

            TextFormatterInfo info;
            if (matched != null)
            {
                info = matched;
            }
            else if (_registry.Count > 0)
            {
                // VexFlow fallback: use first registered font
                info = _syntheticFallback;
                foreach (var v in _registry.Values) { info = v; break; }
            }
            else
            {
                // Registry is empty — use synthetic fallback info
                // Default advance width 0.5 em per character (reasonable sans-serif approximation)
                info = _syntheticFallback;
            }

            return new TextFormatter(info, fontSizeInPt);
        }

        /// <summary>
        /// Clear the registry and width caches. Primarily for testing.
        /// </summary>
        public static void ClearRegistry()
        {
            _registry.Clear();
            _textWidthCache.Clear();
        }

        private static string NormalizeFamilyName(string family)
        {
            var trimmed = family.Trim();
            if (trimmed.Length >= 2
                && ((trimmed[0] == '\'' && trimmed[trimmed.Length - 1] == '\'')
                    || (trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')))
            {
                return trimmed.Substring(1, trimmed.Length - 2).Trim();
            }

            return trimmed;
        }

        // ── Synthetic fallback font ────────────────────────────────────────────

        /// <summary>
        /// Fallback font info used when no fonts are registered.
        /// Uses 0.5 em as the default advance width per character (average sans-serif estimate).
        /// TODO (Phase 4/5): Replace with actual registered font data.
        /// </summary>
        private static readonly TextFormatterInfo _syntheticFallback = new TextFormatterInfo
        {
            Family = "_fallback",
            Resolution = (int)VexFlowSharp.Metrics.GetDouble("TextFormatter.defaultResolution"),
            Glyphs = new Dictionary<string, double>(), // empty → uses DefaultAdvanceWidth
            Serifs = false,
            Monospaced = false,
            Italic = false,
            Bold = false,
            Description = "Synthetic fallback — 0.5 em per character",
        };

        /// <summary>
        /// Default advance width (in em) used when no glyph data is available.
        /// Matches VexFlow's fallback of ~0.65 em per character for '#' and '5'.
        /// Using 0.5 em as a reasonable average for sans-serif fonts.
        /// </summary>
        private static double DefaultAdvanceWidthEm => VexFlowSharp.Metrics.GetDouble("TextFormatter.defaultAdvanceWidthEm");

        // ── Instance fields ────────────────────────────────────────────────────

        /// <summary>Font family name.</summary>
        private string _family;

        /// <summary>Font size in points.</summary>
        private double _sizeInPt;

        /// <summary>Units-per-em resolution for this font's glyph data.</summary>
        private int _resolution;

        /// <summary>Per-character advance widths in font units. May be empty (uses DefaultAdvanceWidthEm).</summary>
        private Dictionary<string, double> _glyphs;

        /// <summary>Whether the font is bold.</summary>
        private bool _bold;

        /// <summary>Whether the font is italic.</summary>
        private bool _italic;

        /// <summary>Cache key for this formatter instance.</summary>
        private string _cacheKey;

        // ── Constructor ────────────────────────────────────────────────────────

        private TextFormatter(TextFormatterInfo info, double fontSizeInPt)
        {
            _family = info.Family;
            _sizeInPt = fontSizeInPt;
            _resolution = info.Resolution;
            _glyphs = info.Glyphs;
            _bold = info.Bold;
            _italic = info.Italic;
            _cacheKey = BuildCacheKey();
        }

        // ── Properties ─────────────────────────────────────────────────────────

        /// <summary>
        /// Font size in pixels.
        /// Port of VexFlow's fontSizeInPixels getter: size(pt) * (4/3).
        /// VexFlow's Font.scaleToPxFrom.pt = 4/3 (96 dpi / 72 pt-per-inch).
        /// </summary>
        public double FontSizeInPixels => _sizeInPt * VexFlowSharp.Metrics.GetDouble("TextFormatter.ptToPx");

        /// <summary>Font size in points.</summary>
        public double FontSizeInPt => _sizeInPt;

        /// <summary>Units-per-em resolution of the registered font metrics.</summary>
        public int Resolution => _resolution;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Set the font size in points and update the internal cache key.
        /// Port of VexFlow's setFontSize(size).
        /// </summary>
        public TextFormatter SetFontSize(double sizeInPt)
        {
            _sizeInPt = sizeInPt;
            _cacheKey = BuildCacheKey();
            return this;
        }

        /// <summary>
        /// Get the advance width of a single character in em units.
        /// Port of VexFlow's getWidthForCharacterInEm(c).
        ///
        /// If no glyph data is registered for the character, falls back to
        /// DefaultAdvanceWidthEm (0.5 em) — reasonable for sans-serif fonts.
        /// </summary>
        public double GetGlyphWidth(string character)
        {
            if (_glyphs.TryGetValue(character, out double advanceWidth))
            {
                return advanceWidth / _resolution;
            }
            // VexFlow fallback: ~0.65 em for '#' and '5'; we use 0.5 as a general average
            return DefaultAdvanceWidthEm;
        }

        /// <summary>
        /// Get the total width of <paramref name="text"/> in em units.
        /// Port of VexFlow's getWidthForTextInEm(text).
        ///
        /// Results are cached per (font, size, weight, style, text) combination.
        /// </summary>
        public double GetWidthForTextInEm(string text)
        {
            if (text.Length == 0) return 0.0;

            if (!_textWidthCache.TryGetValue(_cacheKey, out var perFontCache))
            {
                perFontCache = new Dictionary<string, double>();
                _textWidthCache[_cacheKey] = perFontCache;
            }

            if (perFontCache.TryGetValue(text, out double cached))
            {
                return cached;
            }

            double width = 0.0;
            foreach (char c in text)
            {
                width += GetGlyphWidth(c.ToString());
            }

            perFontCache[text] = width;
            return width;
        }

        /// <summary>
        /// Get the total width of <paramref name="text"/> in pixels.
        /// Port of VexFlow's getWidthForTextInPx(text).
        ///
        /// Width = GetWidthForTextInEm(text) * FontSizeInPixels.
        /// </summary>
        public double GetWidthForTextInPx(string text)
        {
            return GetWidthForTextInEm(text) * FontSizeInPixels;
        }

        /// <summary>
        /// Update font parameters from a new info record and rebuild the cache key.
        /// Port of VexFlow's updateParams(params).
        /// </summary>
        public void UpdateParams(TextFormatterInfo info)
        {
            _textWidthCache.Remove(_cacheKey);
            if (!string.IsNullOrEmpty(info.Family)) _family = info.Family;
            if (info.Resolution > 0) _resolution = info.Resolution;
            if (info.Glyphs != null && info.Glyphs.Count > 0) _glyphs = info.Glyphs;
            _bold = info.Bold;
            _italic = info.Italic;
            _cacheKey = BuildCacheKey();
            _textWidthCache.Remove(_cacheKey);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Build the cache key for this formatter.
        /// Port of VexFlow's updateCacheKey().
        /// Key format: {family}%{size}%{weight}%{style}
        /// </summary>
        private string BuildCacheKey()
        {
            var family = _family.Replace(" ", "_");
            var weight = _bold ? "bold" : "normal";
            var style = _italic ? "italic" : "normal";
            return $"{family}%{_sizeInPt}%{weight}%{style}";
        }
    }
}

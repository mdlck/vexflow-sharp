// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using System.Linq;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Static registry mapping font names to their glyph metric data.
    /// Port of the relevant static portions of VexFlow's Font class from font.ts.
    /// </summary>
    public class Font
    {
        private static readonly Dictionary<string, FontData> _registry = new Dictionary<string, FontData>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Register a font under the given name.
        /// </summary>
        /// <param name="name">Font name used as the registry key (e.g., "Bravura").</param>
        /// <param name="data">Glyph metric data for the font.</param>
        public static void Load(string name, FontData data)
        {
            _registry[name] = data;
            RegisterTextMetrics(name, data);
        }

        /// <summary>
        /// Register one of the font packages that VexFlow exposes through Font.FILES.
        /// </summary>
        public static void LoadBuiltIn(string name)
        {
            if (!BuiltInFonts.All.TryGetValue(name, out var builtIn))
                throw new VexFlowException("BadFont", $"Font '{name}' is not a built-in VexFlow font.");

            Load(builtIn.Family, builtIn.Data);
        }

        /// <summary>
        /// Register all built-in VexFlow fonts that are embedded as generated C# data.
        /// </summary>
        public static void LoadAllBuiltIns()
        {
            foreach (var builtIn in BuiltInFonts.All.Values)
                Load(builtIn.Family, builtIn.Data);
        }

        /// <summary>
        /// Retrieve font data for the given name.
        /// </summary>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">
        /// Thrown when no font with that name has been loaded.
        /// </exception>
        public static FontData GetData(string name)
        {
            return _registry[name];
        }

        /// <summary>
        /// Check whether a font with the given name has been loaded.
        /// </summary>
        public static bool HasFont(string name)
        {
            return _registry.ContainsKey(name);
        }

        public static bool HasAnyFonts() => _registry.Count > 0;

        /// <summary>
        /// Return registered font names in registry order.
        /// </summary>
        public static IEnumerable<string> GetRegisteredFontNames() => _registry.Keys;

        /// <summary>
        /// Resolve the active music font from Metrics.fontFamily. The first loaded
        /// font in the current font stack that is marked as a music font wins.
        /// </summary>
        public static FontData ResolveMusicFont()
        {
            foreach (var family in Metrics.GetFontFamily().Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                var name = family.Trim();
                if (!_registry.TryGetValue(name, out var data)) continue;
                if ((BuiltInFonts.All.TryGetValue(name, out var builtIn) && builtIn.IsMusicFont) || IsLikelyMusicFont(data))
                    return data;
            }

            if (_registry.TryGetValue("Bravura", out var bravura))
                return bravura;

            throw new VexFlowException("NoFonts", "No music font loaded. Call VexFlow.LoadFonts(...) or Font.Load(\"Bravura\", BravuraGlyphs.Data) first.");
        }

        /// <summary>
        /// Resolve the font that should draw a glyph, following the active font
        /// stack and falling back to Bravura when available.
        /// </summary>
        public static FontData ResolveGlyphFontData(string glyphCode)
        {
            foreach (var family in Metrics.GetFontFamily().Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                var name = family.Trim();
                if (!_registry.TryGetValue(name, out var data)) continue;
                if (data.Glyphs.ContainsKey(glyphCode))
                    return data;
            }

            if (_registry.TryGetValue("Bravura", out var bravura) && bravura.Glyphs.ContainsKey(glyphCode))
                return bravura;

            return ResolveMusicFont();
        }

        /// <summary>
        /// Remove all registered fonts. Primarily useful for testing.
        /// </summary>
        public static void ClearRegistry()
        {
            _registry.Clear();
            TextFormatter.ClearRegistry();
        }

        private static void RegisterTextMetrics(string name, FontData data)
        {
            var glyphs = data.Glyphs
                .Where(kvp => kvp.Key.Length == 1)
                .ToDictionary(kvp => kvp.Key, kvp => (double)(kvp.Value.Ha != 0 ? kvp.Value.Ha : kvp.Value.XMax - kvp.Value.XMin));

            if (glyphs.Count == 0) return;

            TextFormatter.RegisterInfo(new TextFormatterInfo
            {
                Family = name,
                Resolution = data.Resolution,
                Glyphs = glyphs,
                Description = $"Generated metrics for {name}",
            }, overwrite: true);
        }

        private static bool IsLikelyMusicFont(FontData data)
            => data.Glyphs.ContainsKey("gClef") || data.Glyphs.ContainsKey("noteheadBlack") || data.Glyphs.ContainsKey("timeSig4");
    }
}

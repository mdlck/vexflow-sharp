#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Glyph metrics entry for a single character in a font.
    /// Port of VexFlow's FontGlyph interface from font.ts.
    /// </summary>
    public class FontGlyph
    {
        /// <summary>Minimum x extent of the glyph outline.</summary>
        public int XMin { get; set; }

        /// <summary>Maximum x extent of the glyph outline.</summary>
        public int XMax { get; set; }

        /// <summary>Minimum y extent of the glyph outline (optional).</summary>
        public int? YMin { get; set; }

        /// <summary>Maximum y extent of the glyph outline (optional).</summary>
        public int? YMax { get; set; }

        /// <summary>Height above baseline.</summary>
        public int Ha { get; set; }

        /// <summary>Raw outline string (the 'o' field in VexFlow JSON data).</summary>
        public string? OutlineString { get; set; }

        /// <summary>Pre-parsed integer outline commands (cached_outline in VexFlow).</summary>
        public int[]? CachedOutline { get; set; }
    }

    /// <summary>
    /// Container for all glyph metrics in a single music/text font.
    /// Port of VexFlow's FontData interface from font.ts.
    /// </summary>
    public class FontData
    {
        /// <summary>
        /// Dictionary mapping glyph code/name to its metrics.
        /// Corresponds to VexFlow's FontData.glyphs record.
        /// </summary>
        public Dictionary<string, FontGlyph> Glyphs { get; set; } = new Dictionary<string, FontGlyph>();

        /// <summary>CSS font-family name (e.g., "Bravura").</summary>
        public string? FontFamily { get; set; }

        /// <summary>
        /// Units-per-em resolution for this font.
        /// Bravura SMuFL fonts use 1000; text fonts typically use 2048.
        /// </summary>
        public int Resolution { get; set; }

        /// <summary>ISO datetime string recording when the data file was generated.</summary>
        public string? GeneratedOn { get; set; }
    }
}

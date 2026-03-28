// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Static registry mapping font names to their glyph metric data.
    /// Port of the relevant static portions of VexFlow's Font class from font.ts.
    /// </summary>
    public class Font
    {
        private static readonly Dictionary<string, FontData> _registry = new Dictionary<string, FontData>();

        /// <summary>
        /// Register a font under the given name.
        /// </summary>
        /// <param name="name">Font name used as the registry key (e.g., "Bravura").</param>
        /// <param name="data">Glyph metric data for the font.</param>
        public static void Load(string name, FontData data)
        {
            _registry[name] = data;
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

        /// <summary>
        /// Remove all registered fonts. Primarily useful for testing.
        /// </summary>
        public static void ClearRegistry()
        {
            _registry.Clear();
        }
    }
}

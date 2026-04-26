#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Clef type descriptor: glyph code and staff line position.
    /// Port of VexFlow's ClefType interface from clef.ts.
    /// </summary>
    public class ClefType
    {
        public string Code { get; set; } = "";
        public double Line { get; set; }
    }

    /// <summary>
    /// Renders a clef glyph on a stave.
    /// Port of VexFlow's Clef class from clef.ts.
    ///
    /// Every clef name maps to a SMuFL glyph code and a default staff line number.
    /// "treble" → gClef at line 3, "bass" → fClef at line 1, etc.
    /// </summary>
    public class Clef : StaveModifier
    {
        /// <summary>
        /// Map of clef type names to glyph codes and staff-line positions.
        /// Port of VexFlow's Clef.types getter from clef.ts.
        /// </summary>
        public static readonly Dictionary<string, ClefType> Types = new Dictionary<string, ClefType>
        {
            { "treble",        new ClefType { Code = "gClef",                    Line = 3   } },
            { "bass",          new ClefType { Code = "fClef",                    Line = 1   } },
            { "alto",          new ClefType { Code = "cClef",                    Line = 2   } },
            { "tenor",         new ClefType { Code = "cClef",                    Line = 1   } },
            { "percussion",    new ClefType { Code = "unpitchedPercussionClef1", Line = 2   } },
            { "soprano",       new ClefType { Code = "cClef",                    Line = 4   } },
            { "mezzo-soprano", new ClefType { Code = "cClef",                    Line = 3   } },
            { "baritone-c",    new ClefType { Code = "cClef",                    Line = 0   } },
            { "baritone-f",    new ClefType { Code = "fClef",                    Line = 2   } },
            { "subbass",       new ClefType { Code = "fClef",                    Line = 0   } },
            { "french",        new ClefType { Code = "gClef",                    Line = 4   } },
            { "tab",           new ClefType { Code = "6stringTabClef",           Line = 2.5 } },
        };

        private string clefTypeName;
        private string size;
        private string? annotation;
        private ClefType clefInfo;

        /// <summary>
        /// Get rendering point size for a clef.
        /// Default size uses NOTATION_FONT_SCALE; other sizes use 2/3 of that.
        /// Port of VexFlow's Clef.getPoint() from clef.ts.
        /// </summary>
        public static double GetPoint(string sz = "default")
            => sz == "default" ? Tables.NOTATION_FONT_SCALE : (Tables.NOTATION_FONT_SCALE / 3) * 2;

        /// <summary>Create a clef of the given type.</summary>
        public Clef(string type, string size = "default", string? annotation = null)
        {
            this.clefTypeName = type;
            this.size         = size;
            this.annotation   = annotation;

            SetPosition(StaveModifierPosition.Begin);

            if (!Types.TryGetValue(type, out clefInfo!))
                throw new VexFlowException("BadArguments", $"Unknown clef type: {type}");

            // Compute actual glyph width using VexFlow's scale formula:
            // bbox.getW() * (point * 72) / (resolution * 100)
            // This matches Glyph.getWidth(code, point, category) in VexFlow glyph.ts.
            SetWidth(Glyph.GetWidth(clefInfo.Code, GetPoint(size)));
        }

        private static double GetDefaultWidth(string sz)
        {
            // Fallback width when Glyph.GetWidth cannot compute from font data.
            return sz == "default" ? 27.0 : 20.0;
        }

        /// <summary>Get the clef type name (e.g., "treble", "bass").</summary>
        public string GetClefTypeName() => clefTypeName;

        /// <summary>Get the ClefType descriptor (code and line).</summary>
        public ClefType GetClefInfo() => clefInfo;

        /// <summary>Change the clef type after construction.</summary>
        public Clef SetType(string type, string? newSize = null, string? newAnnotation = null)
        {
            clefTypeName = type;
            if (newSize != null) size = newSize;
            annotation   = newAnnotation;

            if (!Types.TryGetValue(type, out clefInfo!))
                throw new VexFlowException("BadArguments", $"Unknown clef type: {type}");

            SetWidth(Glyph.GetWidth(clefInfo.Code, GetPoint(size)));
            return this;
        }

        /// <summary>
        /// Draw the clef glyph onto the stave at its assigned x position.
        /// Renders the SMuFL glyph at (x, stave.GetYForLine(clefLine)).
        /// </summary>
        public override void Draw(Stave stave, double xShift)
        {
            var ctx = stave.CheckContext();
            SetContext(ctx);

            double drawX = x + xShift;
            double drawY = stave.GetYForLine(clefInfo.Line);
            double point = GetPoint(size);
            double scale = (point * 72.0) / (BravuraGlyphs.Data.Resolution * 100.0);

            if (BravuraGlyphs.Data.Glyphs.TryGetValue(clefInfo.Code, out var fg)
                && fg.CachedOutline != null)
            {
                Glyph.RenderOutline(ctx, fg.CachedOutline, scale, drawX, drawY);
            }
        }
    }
}

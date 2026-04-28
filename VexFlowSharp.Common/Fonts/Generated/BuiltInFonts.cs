// AUTO-GENERATED from tools/gen-vexflow-fonts.mjs - do not edit manually.
// Regenerate with: node tools/gen-vexflow-fonts.mjs

using System;
using System.Collections.Generic;

namespace VexFlowSharp
{
    public sealed class BuiltInFont
    {
        public BuiltInFont(string family, string path, FontData data, bool isMusicFont)
        {
            Family = family;
            Path = path;
            Data = data;
            IsMusicFont = isMusicFont;
        }

        public string Family { get; }
        public string Path { get; }
        public FontData Data { get; }
        public bool IsMusicFont { get; }
    }

    public static class BuiltInFonts
    {
        public static readonly IReadOnlyDictionary<string, BuiltInFont> All =
            new Dictionary<string, BuiltInFont>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bravura"] = new BuiltInFont("Bravura", "bravura/bravura.otf", BravuraGlyphs.Data, true),
                ["Academico"] = new BuiltInFont("Academico", "academico/academico.otf", AcademicoGlyphs.Data, false),
                ["Bravura Text"] = new BuiltInFont("Bravura Text", "bravuratext/bravuratext.otf", BravuraTextGlyphs.Data, false),
                ["Edwin"] = new BuiltInFont("Edwin", "edwin/edwin-roman.otf", EdwinGlyphs.Data, false),
                ["Finale Ash"] = new BuiltInFont("Finale Ash", "finaleash/finaleash.otf", FinaleAshGlyphs.Data, true),
                ["Finale Ash Text"] = new BuiltInFont("Finale Ash Text", "finaleashtext/finaleashtext.otf", FinaleAshTextGlyphs.Data, false),
                ["Finale Broadway"] = new BuiltInFont("Finale Broadway", "finalebroadway/finalebroadway.otf", FinaleBroadwayGlyphs.Data, true),
                ["Finale Broadway Text"] = new BuiltInFont("Finale Broadway Text", "finalebroadwaytext/finalebroadwaytext.otf", FinaleBroadwayTextGlyphs.Data, false),
                ["Finale Jazz"] = new BuiltInFont("Finale Jazz", "finalejazz/finalejazz.otf", FinaleJazzGlyphs.Data, true),
                ["Finale Jazz Text"] = new BuiltInFont("Finale Jazz Text", "finalejazztext/finalejazztext.otf", FinaleJazzTextGlyphs.Data, false),
                ["Finale Maestro"] = new BuiltInFont("Finale Maestro", "finalemaestro/finalemaestro.otf", FinaleMaestroGlyphs.Data, true),
                ["Finale Maestro Text"] = new BuiltInFont("Finale Maestro Text", "finalemaestrotext/finalemaestrotext-regular.otf", FinaleMaestroTextGlyphs.Data, false),
                ["Gonville"] = new BuiltInFont("Gonville", "gonville/gonville.otf", GonvilleGlyphs.Data, true),
                ["Gootville"] = new BuiltInFont("Gootville", "gootville/gootville.otf", GootvilleGlyphs.Data, true),
                ["Gootville Text"] = new BuiltInFont("Gootville Text", "gootvilletext/gootvilletext.otf", GootvilleTextGlyphs.Data, false),
                ["Leipzig"] = new BuiltInFont("Leipzig", "leipzig/leipzig.otf", LeipzigGlyphs.Data, true),
                ["Leland"] = new BuiltInFont("Leland", "leland/leland.otf", LelandGlyphs.Data, true),
                ["Leland Text"] = new BuiltInFont("Leland Text", "lelandtext/lelandtext.otf", LelandTextGlyphs.Data, false),
                ["MuseJazz"] = new BuiltInFont("MuseJazz", "musejazz/musejazz.otf", MuseJazzGlyphs.Data, true),
                ["MuseJazz Text"] = new BuiltInFont("MuseJazz Text", "musejazztext/musejazztext.otf", MuseJazzTextGlyphs.Data, false),
                ["Nepomuk"] = new BuiltInFont("Nepomuk", "nepomuk/nepomuk-regular.otf", NepomukGlyphs.Data, false),
                ["Petaluma"] = new BuiltInFont("Petaluma", "petaluma/petaluma.otf", PetalumaGlyphs.Data, true),
                ["Petaluma Script"] = new BuiltInFont("Petaluma Script", "petalumascript/petalumascript.otf", PetalumaScriptGlyphs.Data, false),
                ["Petaluma Text"] = new BuiltInFont("Petaluma Text", "petalumatext/petalumatext.otf", PetalumaTextGlyphs.Data, false),
                ["Roboto Slab"] = new BuiltInFont("Roboto Slab", "robotoslab/robotoslab-regular-400.otf", RobotoSlabGlyphs.Data, false),
                ["Sebastian"] = new BuiltInFont("Sebastian", "sebastian/sebastian.otf", SebastianGlyphs.Data, true),
                ["Sebastian Text"] = new BuiltInFont("Sebastian Text", "sebastiantext/sebastiantext.otf", SebastianTextGlyphs.Data, false),
            };
    }
}

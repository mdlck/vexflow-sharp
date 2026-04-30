// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    /// <summary>
    /// Bar-repeat glyph note. Port of VexFlow's RepeatNote class from repeatnote.ts.
    /// </summary>
    public class RepeatNote : GlyphNote
    {
        public new const string CATEGORY = "RepeatNote";

        private static string CodeForType(string type)
        {
            switch (type)
            {
                case "1": return "repeat1Bar";
                case "2": return "repeat2Bars";
                case "4": return "repeat4Bars";
                case "slash": return "repeatBarSlash";
                default: return "repeat1Bar";
            }
        }

        public RepeatNote(string type, NoteStruct noteStruct = null, GlyphNoteOptions options = null)
            : base(CodeForType(type), MakeNoteStruct(type, noteStruct), options)
        {
            SetCenterAligned(type != "slash");
        }

        private static NoteStruct MakeNoteStruct(string type, NoteStruct noteStruct)
        {
            var source = noteStruct ?? new NoteStruct();
            return new NoteStruct
            {
                Duration = source.Duration ?? "q",
                Keys = source.Keys,
                Type = source.Type,
                Dots = source.Dots,
                AutoStem = source.AutoStem,
                StemDirection = source.StemDirection,
                Clef = source.Clef,
                OctaveShift = source.OctaveShift,
                GlyphFontScale = source.GlyphFontScale,
                StrokePx = source.StrokePx,
            };
        }

        public override string GetCategory() => CATEGORY;
    }
}

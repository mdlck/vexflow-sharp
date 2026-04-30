// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    public class GlyphNoteOptions
    {
        public bool IgnoreTicks { get; set; }
        public double Line { get; set; } = 2;
    }

    /// <summary>
    /// A time-bearing note represented by a single SMuFL glyph on the stave.
    /// Port of VexFlow's GlyphNote class from glyphnote.ts.
    /// </summary>
    public class GlyphNote : Note
    {
        public new const string CATEGORY = "GlyphNote";

        private string glyphCode = string.Empty;
        private readonly GlyphNoteOptions options;
        private bool preFormatted;

        public GlyphNote(string glyph, NoteStruct noteStruct, GlyphNoteOptions options = null)
            : base(noteStruct)
        {
            this.options = options ?? new GlyphNoteOptions();
            ignoreTicks = this.options.IgnoreTicks;
            SetGlyph(glyph);
        }

        public string GetGlyph() => glyphCode;
        public GlyphNoteOptions GetOptions() => options;

        public GlyphNote SetGlyph(string glyph)
        {
            glyphCode = glyph;
            SetWidth(Glyph.GetWidth(glyphCode, Metrics.GetDouble("fontSize")));
            return this;
        }

        public override string GetCategory() => CATEGORY;

        public override void PreFormat()
        {
            if (preFormatted) return;
            modifierContext.PreFormat();
            preFormatted = true;
        }

        public void DrawModifiers()
        {
            var ctx = CheckContext();
            foreach (var modifier in modifiers)
            {
                modifier.SetContext(ctx);
                modifier.Draw();
            }
        }

        public override void Draw()
        {
            var stave = CheckStave();
            var ctx = stave.CheckContext();
            SetContext(ctx);
            rendered = true;

            double drawX = IsCenterAligned()
                ? GetAbsoluteX() - GetWidth() / 2
                : GetAbsoluteX();
            double drawY = stave.GetYForLine(options.Line);

            var glyph = new Glyph(glyphCode, Metrics.GetDouble("fontSize"));
            glyph.Render(ctx, drawX, drawY);
            DrawModifiers();
        }
    }
}

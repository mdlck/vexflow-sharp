#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    /// <summary>
    /// A zero-tick note wrapper around Clef for mid-measure clef changes.
    /// Port of VexFlow's ClefNote class from clefnote.ts.
    /// </summary>
    public class ClefNote : Note
    {
        public new const string CATEGORY = "ClefNote";

        private Clef clef;

        public ClefNote(string type, string size = "default", string? annotation = null)
            : base(new NoteStruct { Duration = "b" })
        {
            clef = new Clef(type, size, annotation);
            SetWidth(clef.GetWidth());
            ignoreTicks = true;
        }

        public ClefNote SetType(string type, string size = "default", string? annotation = null)
        {
            clef = new Clef(type, size, annotation);
            SetWidth(clef.GetWidth());
            return this;
        }

        public Clef GetClef() => clef;

        public override void PreFormat()
        {
            // No modifier preformat work is needed; width comes from Clef.
        }

        public override void Draw()
        {
            var stave = CheckStave();
            var ctx = stave.CheckContext();
            SetContext(ctx);
            rendered = true;

            clef.SetX(GetAbsoluteX());
            clef.SetContext(ctx);
            clef.Draw(stave, 0);
        }

        public override BoundingBox? GetBoundingBox() => clef.GetBoundingBox();

        public override string GetCategory() => CATEGORY;
    }
}

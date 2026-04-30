// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    public class GraceTabNote : TabNote
    {
        public new const string CATEGORY = "GraceTabNote";

        public GraceTabNote(TabNoteStruct noteStruct) : base(noteStruct, false)
        {
            RenderOptions.YShift = Metrics.GetDouble("GraceTabNote.yShift");
            UpdateWidth();
        }

        public override string GetCategory() => CATEGORY;
    }
}

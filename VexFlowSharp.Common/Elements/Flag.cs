// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    /// <summary>
    /// Note flag glyph element used by StemmableNote/StaveNote.
    /// Port of VexFlow's Flag class from flag.ts.
    /// </summary>
    public class Flag : Glyph
    {
        public new const string CATEGORY = "Flag";

        public Flag(string code, double point, FontData fontData = null)
            : base(code, point, fontData)
        {
        }

        public override string GetCategory() => CATEGORY;

        public override GlyphMetrics Render(RenderContext ctx, double xPos, double yPos)
        {
            var cls = GetAttribute("class");
            OpenFlagGroup(ctx, cls);
            var metrics = base.Render(ctx, xPos, yPos);
            ctx.CloseGroup();
            return metrics;
        }

        private void OpenFlagGroup(RenderContext ctx, string cls)
        {
            var groupClass = string.IsNullOrEmpty(cls) ? "flag" : "flag " + cls;
            ctx.OpenGroup(groupClass, GetAttribute("id"));
        }
    }
}

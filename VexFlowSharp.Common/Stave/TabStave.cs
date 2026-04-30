// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    /// <summary>
    /// Six-line tablature stave.
    /// Port of VexFlow 5's TabStave class.
    /// </summary>
    public class TabStave : Stave
    {
        public new const string CATEGORY = "TabStave";

        public TabStave(double x, double y, double width, StaveOptions options = null)
            : base(x, y, width, MergeOptions(options))
        {
        }

        private static StaveOptions MergeOptions(StaveOptions options)
        {
            options ??= new StaveOptions();
            options.SpacingBetweenLinesPx = Metrics.GetDouble("TabStave.spacingBetweenLinesPx");
            options.NumLines = (int)Metrics.GetDouble("TabStave.numLines");
            options.TopTextPosition = Metrics.GetDouble("TabStave.topTextPosition");
            return options;
        }

        public override string GetCategory() => CATEGORY;

        public double GetYForTabGlyphs() => GetYForLine(2.5);

        public TabStave AddTabGlyph()
        {
            AddClef("tab");
            return this;
        }
    }
}

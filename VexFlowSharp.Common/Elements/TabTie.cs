#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    public class TabTie : StaveTie
    {
        public new const string CATEGORY = "TabTie";

        public static TabTie CreateHammeron(TieNotes notes) => new TabTie(notes, "H");
        public static TabTie CreatePulloff(TieNotes notes) => new TabTie(notes, "P");

        public TabTie(TieNotes notes, string? text = null) : base(notes, text)
        {
            RenderOptions.Cp1 = Metrics.GetDouble("TabTie.cp1");
            RenderOptions.Cp2 = Metrics.GetDouble("TabTie.cp2");
            RenderOptions.YShift = Metrics.GetDouble("TabTie.yShift");
            SetDirection(-1);
        }

        public override string GetCategory() => CATEGORY;
    }
}

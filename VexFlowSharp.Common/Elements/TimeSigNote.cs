// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// A zero-tick note wrapper around TimeSignature for mid-measure time changes.
    /// Port of VexFlow's TimeSigNote class from timesignote.ts.
    /// </summary>
    public class TimeSigNote : Note
    {
        public new const string CATEGORY = "TimeSigNote";

        private readonly TimeSignature timeSignature;

        public TimeSigNote(string timeSpec, double customPadding = 15)
            : base(new NoteStruct { Duration = "b" })
        {
            timeSignature = new TimeSignature(timeSpec, customPadding);
            SetWidth(timeSignature.GetWidth());
            ignoreTicks = true;
        }

        public TimeSignature GetTimeSignature() => timeSignature;

        public override void AddToModifierContext(ModifierContext mc)
        {
            // TimeSigNote is itself a timed wrapper and does not register as a modifier.
        }

        public override void PreFormat()
        {
            // No modifier preformat work is needed; width comes from TimeSignature.
        }

        public override void Draw()
        {
            var stave = CheckStave();
            var ctx = stave.CheckContext();
            SetContext(ctx);
            rendered = true;

            timeSignature.SetX(GetAbsoluteX());
            timeSignature.SetContext(ctx);
            timeSignature.Draw(stave, 0);
        }

        public override string GetCategory() => CATEGORY;
    }
}

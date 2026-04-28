#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// A zero-tick note wrapper around KeySignature for mid-measure key changes.
    /// Port of VexFlow's KeySigNote class from keysignote.ts.
    /// </summary>
    public class KeySigNote : Note
    {
        public new const string CATEGORY = "KeySigNote";

        private readonly KeySignature keySignature;
        private bool preFormatted;

        public KeySigNote(string keySpec, string? cancelKeySpec = null, string[]? alterKeySpec = null)
            : base(new NoteStruct { Duration = "b" })
        {
            keySignature = new KeySignature(keySpec, cancelKeySpec);
            ignoreTicks = true;
        }

        public KeySignature GetKeySignature() => keySignature;

        public override void AddToModifierContext(ModifierContext mc)
        {
            // KeySigNote is itself a timed wrapper and does not register as a modifier.
        }

        public override void PreFormat()
        {
            if (preFormatted) return;
            keySignature.SetStave(CheckStave());
            SetWidth(keySignature.GetWidth());
            preFormatted = true;
        }

        public override void Draw()
        {
            var stave = CheckStave();
            var ctx = stave.CheckContext();
            SetContext(ctx);
            rendered = true;

            keySignature.SetX(GetAbsoluteX());
            keySignature.SetContext(ctx);
            keySignature.Draw(stave, 0);
        }

        public override string GetCategory() => CATEGORY;
    }
}

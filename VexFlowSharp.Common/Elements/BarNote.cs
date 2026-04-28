// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// A zero-duration Note that renders a single barline at its formatted X position.
    /// Used to insert visual bar lines between measures within a single-stave voice layout.
    ///
    /// Port of VexFlow's BarNote class from barnote.ts.
    /// </summary>
    public class BarNote : Note
    {
        public new const string CATEGORY = "BarNote";

        private readonly BarlineType _barlineType;

        /// <summary>
        /// Create a BarNote that renders a barline of the given type.
        /// </summary>
        public BarNote(BarlineType barlineType = BarlineType.Single)
            : base(new NoteStruct { Duration = "b", Keys = System.Array.Empty<string>() })
        {
            _barlineType = barlineType;
            ignoreTicks  = true;
            SetWidth(8.0);
        }

        public BarlineType GetBarlineType() => _barlineType;

        public override string GetCategory() => CATEGORY;

        /// <summary>
        /// No-op — BarNote has no modifiers and does not participate in modifier layout.
        /// </summary>
        public override void AddToModifierContext(ModifierContext mc)
        {
            // Intentionally empty — BarNote has no modifiers.
        }

        /// <summary>
        /// No-op pre-format — BarNote does not require glyph layout.
        /// </summary>
        public override void PreFormat()
        {
            // Nothing to pre-format.
        }

        /// <summary>
        /// Draw a barline at this note's formatted absolute X position.
        /// </summary>
        public override void Draw()
        {
            var stave   = CheckStave();
            var ctx     = stave.CheckContext();
            SetContext(ctx);

            double absX    = GetAbsoluteX();
            var    barline = new Barline(_barlineType, absX);
            barline.Draw(stave, 0);

            rendered = true;
        }
    }
}

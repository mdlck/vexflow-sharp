#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    /// <summary>
    /// Tremolo notation modifier.
    /// Port of VexFlow's Tremolo class from tremolo.ts.
    /// </summary>
    public class Tremolo : Modifier
    {
        public new const string CATEGORY = "Tremolo";
        public override string GetCategory() => CATEGORY;

        private readonly int num;

        public Tremolo(int num)
        {
            this.num = num;
            position = ModifierPosition.Center;
        }

        public int GetNum() => num;

        public override void Draw()
        {
            var ctx = CheckContext();
            var note = GetNote() as StemmableNote
                ?? throw new VexFlowException("NoStem", "Tremolo must be attached to a stemmable note.");
            rendered = true;

            int stemDirection = note.GetStemDirection();
            double fontScale = 1;
            double ySpacing = Metrics.GetDouble("Tremolo.spacing") * stemDirection * fontScale;
            double glyphWidth = note.GetGlyphProps().HeadWidth;

            double x = note.GetAbsoluteX()
                + (stemDirection == Stem.UP ? glyphWidth - Stem.WIDTH / 2 : Stem.WIDTH / 2);
            double y = note.GetStemExtents().TopY + (num <= 3 ? ySpacing : 0);
            double fontSize = Metrics.GetDouble("Tremolo.fontSize") * fontScale;

            for (int i = 0; i < num; ++i)
            {
                new Glyph("tremolo1", fontSize).Render(ctx, x, y);
                y += ySpacing;
            }
        }
    }
}

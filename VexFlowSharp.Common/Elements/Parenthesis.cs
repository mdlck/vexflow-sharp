// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Parenthesis modifier for noteheads.
    /// Port of VexFlow's Parenthesis class from parenthesis.ts.
    /// </summary>
    public class Parenthesis : Modifier
    {
        public new const string CATEGORY = "Parenthesis";
        public override string GetCategory() => CATEGORY;

        private readonly string glyphCode;

        public Parenthesis(ModifierPosition position = ModifierPosition.Left)
        {
            this.position = position;
            glyphCode = position == ModifierPosition.Right
                ? "noteheadParenthesisRight"
                : "noteheadParenthesisLeft";
            SetWidth(Glyph.GetWidth(glyphCode, Tables.NOTATION_FONT_SCALE));
        }

        public string GetGlyphCode() => glyphCode;

        public static void BuildAndAttach(List<Note> notes)
        {
            foreach (var note in notes)
            {
                for (int i = 0; i < note.GetKeyProps().Count; i++)
                {
                    note.AddModifier(new Parenthesis(ModifierPosition.Left), i);
                    note.AddModifier(new Parenthesis(ModifierPosition.Right), i);
                }
            }
        }

        public static bool Format(List<Parenthesis> parentheses, ModifierContextState state)
        {
            if (parentheses == null || parentheses.Count == 0) return false;

            double xWidthL = 0;
            double xWidthR = 0;

            foreach (var parenthesis in parentheses)
            {
                var note = parenthesis.GetNote() as Note
                    ?? throw new VexFlowException("NoNote", "Parenthesis must be attached to a note.");
                var pos = parenthesis.GetPosition();
                int index = parenthesis.GetIndex() ?? 0;
                double shift = 0;

                if (pos == ModifierPosition.Right)
                {
                    shift = note.GetRightParenthesisPx(index);
                    xWidthR = xWidthR > shift + parenthesis.GetWidth() ? xWidthR : shift + parenthesis.GetWidth();
                }
                else if (pos == ModifierPosition.Left)
                {
                    shift = note.GetLeftParenthesisPx(index) + parenthesis.GetWidth();
                    xWidthL = xWidthL > shift + parenthesis.GetWidth() ? xWidthL : shift + parenthesis.GetWidth();
                }

                parenthesis.SetXShift(shift);
            }

            state.LeftShift += xWidthL;
            state.RightShift += xWidthR;
            return true;
        }

        public override void Draw()
        {
            var ctx = CheckContext();
            var note = (Note)GetNote();
            rendered = true;

            var start = note.GetModifierStartXY(position, GetIndex() ?? 0, new { forceFlagRight = true });
            new Glyph(glyphCode, Tables.NOTATION_FONT_SCALE).Render(ctx, start.X + xShift, start.Y);
        }
    }
}

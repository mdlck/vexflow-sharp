#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public enum StrokeType
    {
        BrushDown = 1,
        BrushUp = 2,
        RollDown = 3,
        RollUp = 4,
        RasgueadoDown = 5,
        RasgueadoUp = 6,
        ArpeggioDirectionless = 7,
    }

    /// <summary>
    /// Chord stroke modifier for brushed and arpeggiated chords.
    /// Port of VexFlow's Stroke class from strokes.ts.
    /// </summary>
    public class Stroke : Modifier
    {
        public new const string CATEGORY = "Stroke";
        public override string GetCategory() => CATEGORY;

        private readonly bool allVoices;
        private readonly StrokeType type;
        private Note? noteEnd;
        private readonly double fontScale;

        public Stroke(StrokeType type, bool allVoices = true)
        {
            this.type = type;
            this.allVoices = allVoices;
            position = ModifierPosition.Left;
            fontScale = Metrics.GetDouble("Stroke.fontSize");
            SetXShift(0);
            SetWidth(10);
        }

        public StrokeType GetStrokeType() => type;
        public bool GetAllVoices() => allVoices;

        public Stroke AddEndNote(Note note)
        {
            noteEnd = note;
            return this;
        }

        public Note? GetEndNote() => noteEnd;

        public static bool Format(List<Stroke>? strokes, ModifierContextState state)
        {
            if (strokes == null || strokes.Count == 0) return false;

            double leftShift = state.LeftShift;
            double xShift = 0;

            foreach (var stroke in strokes)
            {
                var note = stroke.GetNote() as Note
                    ?? throw new VexFlowException("Internal", "Unexpected stroke note instance.");
                int index = stroke.GetIndex() ?? 0;
                _ = note.GetKeyProps()[index].Line;
                double shift = note.GetLeftDisplacedHeadPx();

                stroke.SetXShift(leftShift + shift);
                xShift = Math.Max(stroke.GetWidth() + Metrics.GetDouble("Stroke.spacing"), xShift);
            }

            state.LeftShift += xShift;
            return true;
        }

        public override void Draw()
        {
            var ctx = CheckContext();
            var note = GetNote() as Note
                ?? throw new VexFlowException("NoNote", "Stroke must be attached to a note.");
            rendered = true;

            var start = note.GetModifierStartXY(position, GetIndex() ?? 0);
            var yPositions = note.GetYs();
            double topY = start.Y;
            double botY = start.Y;
            double x = start.X - 5;
            double lineSpace = note.CheckStave().GetSpacingBetweenLines();

            var notes = note.GetModifierContext()?.GetMembers(note.GetCategory()) ?? new List<Element> { note };
            foreach (var member in notes)
            {
                if (member is Note contextNote && (ReferenceEquals(note, contextNote) || allVoices))
                {
                    yPositions = contextNote.GetYs();
                    foreach (var y in yPositions)
                    {
                        topY = Math.Min(topY, y);
                        botY = Math.Max(botY, y);
                    }
                }
            }

            string arrow = "";
            double arrowY = 0;
            double textY = 0;

            switch (type)
            {
                case StrokeType.BrushDown:
                case StrokeType.RollDown:
                case StrokeType.RasgueadoDown:
                    arrow = "arrowheadBlackUp";
                    arrowY = topY;
                    topY -= lineSpace / 2;
                    botY += lineSpace / 2;
                    break;
                case StrokeType.BrushUp:
                case StrokeType.RollUp:
                case StrokeType.RasgueadoUp:
                    arrow = "arrowheadBlackDown";
                    arrowY = botY + lineSpace;
                    topY -= lineSpace / 2;
                    break;
                case StrokeType.ArpeggioDirectionless:
                    topY -= lineSpace / 2;
                    botY += lineSpace / 2;
                    break;
                default:
                    throw new VexFlowException("InvalidType", $"The stroke type {type} does not exist");
            }

            if (type == StrokeType.BrushDown || type == StrokeType.BrushUp)
            {
                ctx.FillRect(x + xShift, topY, 1, botY - topY);
            }
            else
            {
                string lineGlyph = arrow == "arrowheadBlackDown" ? "wiggleArpeggiatoDown" : "wiggleArpeggiatoUp";
                if (Glyph.GetWidth(lineGlyph, fontScale) <= 0)
                    lineGlyph = "wiggleArpeggiatoUp";

                double glyphWidth = Math.Max(Glyph.GetWidth(lineGlyph, fontScale), 1);
                int glyphCount = Math.Max(1, (int)Math.Ceiling((botY - topY) / glyphWidth));
                double renderX = x + xShift;

                if (type == StrokeType.RasgueadoDown || type == StrokeType.RollDown || type == StrokeType.ArpeggioDirectionless)
                {
                    ctx.OpenRotation(90, renderX, topY);
                    for (int i = 0; i < glyphCount; i++)
                        new Glyph(lineGlyph, fontScale).Render(ctx, renderX + i * glyphWidth, topY);
                    ctx.CloseRotation();
                    textY = topY + glyphCount * glyphWidth + 5;
                }
                else
                {
                    ctx.OpenRotation(-90, renderX, botY);
                    for (int i = 0; i < glyphCount; i++)
                        new Glyph(lineGlyph, fontScale).Render(ctx, renderX + i * glyphWidth, botY);
                    ctx.CloseRotation();
                    textY = botY - glyphCount * glyphWidth - 5;
                }
            }

            if (arrowY != 0)
                new Glyph(arrow, fontScale).Render(ctx, x + xShift, arrowY);

            if (type == StrokeType.RasgueadoDown || type == StrokeType.RasgueadoUp)
            {
                var font = Metrics.GetFontInfo("Stroke.text");
                ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
                ctx.FillText("R", x + xShift, textY + (type == StrokeType.RasgueadoDown ? font.Size : 0));
            }
        }
    }
}

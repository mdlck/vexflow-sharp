// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;
using System.Linq;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// End cap used by StringNumber extension lines.
    /// Port of VexFlow's Renderer.LineEndType values.
    /// </summary>
    public enum RendererLineEndType
    {
        None = 1,
        Up   = 2,
        Down = 3,
    }

    /// <summary>
    /// String number annotation modifier.
    /// Port of VexFlow's StringNumber class from stringnumber.ts.
    /// </summary>
    public class StringNumber : Modifier
    {
        public new const string CATEGORY = "StringNumber";
        public override string GetCategory() => CATEGORY;

        private readonly double radius;
        private bool drawCircle;
        private Note lastNote;
        private string stringNumber;
        private double xOffset;
        private double yOffset;
        private double stemOffset;
        private bool dashed;
        private RendererLineEndType leg;

        public StringNumber(string number)
        {
            stringNumber = number;
            position = ModifierPosition.Above;
            xShift = 0;
            yShift = 0;
            textLine = 0;
            stemOffset = 0;
            xOffset = 0;
            yOffset = 0;
            dashed = true;
            leg = RendererLineEndType.None;
            radius = Metrics.GetDouble("StringNumber.radius");
            drawCircle = true;
            SetWidth(radius * 2 + Metrics.GetDouble("StringNumber.circlePadding"));
        }

        public StringNumber SetLineEndType(RendererLineEndType lineEndType)
        {
            leg = lineEndType;
            return this;
        }

        public RendererLineEndType GetLineEndType() => leg;

        public StringNumber SetStringNumber(string number)
        {
            stringNumber = number;
            return this;
        }

        public string GetStringNumber() => stringNumber;

        public StringNumber SetOffsetX(double x)
        {
            xOffset = x;
            return this;
        }

        public StringNumber SetOffsetY(double y)
        {
            yOffset = y;
            return this;
        }

        public double GetOffsetX() => xOffset;
        public double GetOffsetY() => yOffset;

        public StringNumber SetStemOffset(double offset)
        {
            stemOffset = offset;
            return this;
        }

        public double GetStemOffset() => stemOffset;

        public StringNumber SetLastNote(Note note)
        {
            lastNote = note;
            return this;
        }

        public Note GetLastNote() => lastNote;

        public StringNumber SetDashed(bool isDashed)
        {
            dashed = isDashed;
            return this;
        }

        public bool IsDashed() => dashed;

        public StringNumber SetDrawCircle(bool shouldDrawCircle)
        {
            drawCircle = shouldDrawCircle;
            return this;
        }

        public bool GetDrawCircle() => drawCircle;

        public static bool Format(List<StringNumber> nums, ModifierContextState state)
        {
            if (nums == null || nums.Count == 0) return false;

            double leftShift = state.LeftShift;
            double rightShift = state.RightShift;
            var numsList = new List<(Note Note, StringNumber Num, ModifierPosition Pos, double Line, double ShiftL, double ShiftR)>();

            Note prevNote = null;
            double extraXSpaceForDisplacedNotehead = 0;
            double shiftRight = 0;

            foreach (var num in nums)
            {
                var note = num.GetNote() as Note
                    ?? throw new VexFlowException("NoStaveNote", "StringNumber must be attached to a note.");
                var pos = num.GetPosition();
                int index = num.GetIndex() ?? 0;
                var props = note.GetKeyProps()[index];
                double verticalSpaceNeeded = (num.radius * 2) / Tables.STAVE_LINE_DISTANCE + 0.5;

                var mc = note.GetModifierContext();
                if (mc != null)
                {
                    if (pos == ModifierPosition.Above)
                    {
                        num.textLine = mc.GetState().TopTextLine;
                        state.TopTextLine += verticalSpaceNeeded;
                    }
                    else if (pos == ModifierPosition.Below)
                    {
                        num.textLine = mc.GetState().TextLine;
                        state.TextLine += verticalSpaceNeeded;
                    }
                }

                if (!ReferenceEquals(note, prevNote))
                {
                    if (pos == ModifierPosition.Left)
                        extraXSpaceForDisplacedNotehead = Math.Max(note.GetLeftDisplacedHeadPx(), extraXSpaceForDisplacedNotehead);
                    if (rightShift == 0)
                        shiftRight = Math.Max(note.GetRightDisplacedHeadPx(), shiftRight);
                    prevNote = note;
                }

                numsList.Add((note, num, pos, props.Line, extraXSpaceForDisplacedNotehead, shiftRight));
            }

            numsList.Sort((a, b) => b.Line.CompareTo(a.Line));

            double numShiftR = 0;
            double xWidthL = 0;
            double xWidthR = 0;
            double? lastLine = null;
            Note lastNote = null;

            foreach (var item in numsList)
            {
                if (lastLine == null || Math.Abs(item.Line - lastLine.Value) > double.Epsilon || !ReferenceEquals(item.Note, lastNote))
                    numShiftR = rightShift + item.ShiftR;

                double numWidth = item.Num.GetWidth() + Metrics.GetDouble("StringNumber.numSpacing");

                if (item.Pos == ModifierPosition.Left)
                {
                    item.Num.SetXShift(leftShift + extraXSpaceForDisplacedNotehead);
                    xWidthL = Math.Max(numWidth, xWidthL);
                }
                else if (item.Pos == ModifierPosition.Right)
                {
                    item.Num.SetXShift(numShiftR);
                    xWidthR = Math.Max(numWidth, xWidthR);
                }

                lastLine = item.Line;
                lastNote = item.Note;
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

            var start = note.GetModifierStartXY(position, GetIndex() ?? 0);
            int stemDirection = note.HasStem() ? note.GetStemDirection() : Stem.UP;
            double dotX = start.X + xShift + xOffset;
            double dotY = start.Y + yShift + yOffset;

            (double TopY, double BaseY)? stemExtents = null;
            if (note.HasStem() && note is StemmableNote stemmable)
                stemExtents = stemmable.CheckStem().GetExtents();

            switch (position)
            {
                case ModifierPosition.Above:
                    {
                        var ys = note.GetYs();
                        dotY = ys.Min();
                        if (note.HasStem() && stemDirection == Stem.UP && stemExtents.HasValue)
                            dotY = stemExtents.Value.TopY + Metrics.GetDouble("StringNumber.stemPadding") + stemOffset;
                        dotY -= radius + Metrics.GetDouble("StringNumber.verticalPadding") + textLine * Tables.STAVE_LINE_DISTANCE;
                    }
                    break;
                case ModifierPosition.Below:
                    {
                        var ys = note.GetYs();
                        dotY = ys.Max();
                        if (note.HasStem() && stemDirection == Stem.DOWN && stemExtents.HasValue)
                            dotY = stemExtents.Value.TopY - Metrics.GetDouble("StringNumber.stemPadding") - stemOffset;
                        dotY += radius + Metrics.GetDouble("StringNumber.verticalPadding") + textLine * Tables.STAVE_LINE_DISTANCE;
                    }
                    break;
                case ModifierPosition.Left:
                    dotX -= radius / 2 + Metrics.GetDouble("StringNumber.leftPadding");
                    break;
                case ModifierPosition.Right:
                    dotX += radius / 2 + Metrics.GetDouble("StringNumber.rightPadding");
                    break;
                default:
                    throw new VexFlowException("InvalidPosition", $"The position {position} is invalid");
            }

            ctx.Save();
            if (drawCircle)
            {
                ctx.BeginPath();
                ctx.Arc(dotX, dotY, radius, 0, Math.PI * 2, false);
                ctx.SetLineWidth(Metrics.GetDouble("StringNumber.circleLineWidth"));
                ctx.Stroke();
            }

            var font = Metrics.GetFontInfo("StringNumber");
            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
            double measuredWidth = ctx.MeasureText(stringNumber).Width;
            ctx.FillText(stringNumber, dotX - measuredWidth / 2, dotY + Metrics.GetDouble("StringNumber.textBaselineShift"));

            if (lastNote is StemmableNote lastStemmable)
            {
                double end = lastStemmable.GetStemX() - note.GetX() + Metrics.GetDouble("StringNumber.extensionStemOffset");
                ctx.SetStrokeStyle("#000000");
                ctx.SetLineCap("round");
                ctx.SetLineWidth(Metrics.GetDouble("StringNumber.extensionLineWidth"));

                double dash = Metrics.GetDouble("StringNumber.extensionDash");
                double gap = dashed ? Metrics.GetDouble("StringNumber.extensionGap") : 0.0;
                double[] pattern = new[] { dash, gap };
                DrawLine(ctx, dotX + Metrics.GetDouble("StringNumber.extensionStartOffset"), dotY, dotX + end, dotY, pattern);

                if (leg == RendererLineEndType.Up)
                    DrawLine(ctx, dotX + end, dotY, dotX + end, dotY - Metrics.GetDouble("StringNumber.legLength"), pattern);
                else if (leg == RendererLineEndType.Down)
                    DrawLine(ctx, dotX + end, dotY, dotX + end, dotY + Metrics.GetDouble("StringNumber.legLength"), pattern);
            }

            ctx.Restore();
        }

        private static void DrawLine(RenderContext ctx, double fromX, double fromY, double toX, double toY, double[] pattern)
        {
            ctx.SetLineDash(pattern);
            ctx.BeginPath();
            ctx.MoveTo(fromX, fromY);
            ctx.LineTo(toX, toY);
            ctx.Stroke();
            ctx.SetLineDash(Array.Empty<double>());
        }
    }
}

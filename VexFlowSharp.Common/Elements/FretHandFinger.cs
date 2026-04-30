// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    /// <summary>
    /// Fret-hand fingering modifier.
    /// Port of VexFlow's FretHandFinger class from frethandfinger.ts.
    /// </summary>
    public class FretHandFinger : Modifier
    {
        public new const string CATEGORY = "FretHandFinger";
        public override string GetCategory() => CATEGORY;

        private string finger;
        private double xOffset;
        private double yOffset;

        public FretHandFinger(string finger)
        {
            this.finger = finger;
            position = ModifierPosition.Left;
            xOffset = 0;
            yOffset = 0;
            UpdateWidth();
        }

        public FretHandFinger SetFretHandFinger(string finger)
        {
            this.finger = finger;
            UpdateWidth();
            return this;
        }

        public string GetFretHandFinger() => finger;

        public FretHandFinger SetOffsetX(double x)
        {
            xOffset = x;
            return this;
        }

        public FretHandFinger SetOffsetY(double y)
        {
            yOffset = y;
            return this;
        }

        public double GetOffsetX() => xOffset;
        public double GetOffsetY() => yOffset;

        private void UpdateWidth()
        {
            var font = Metrics.GetFontInfo("FretHandFinger");
            SetWidth(TextFormatter.Create(font.Family, font.Size).GetWidthForTextInPx(finger));
        }

        public static bool Format(List<FretHandFinger> nums, ModifierContextState state)
        {
            if (nums == null || nums.Count == 0) return false;

            double leftShift = state.LeftShift;
            double rightShift = state.RightShift;
            var numsList = new List<(Note Note, FretHandFinger Num, ModifierPosition Pos, double Line, double ShiftL, double ShiftR)>();

            Note prevNote = null;
            double shiftLeft = 0;
            double shiftRight = 0;
            double textHeight = Metrics.GetDouble("FretHandFinger.fontSize");

            foreach (var num in nums)
            {
                var note = (Note)num.GetNote();
                var pos = num.GetPosition();
                int index = num.GetIndex() ?? 0;
                var props = note.GetKeyProps()[index];

                if (pos == ModifierPosition.Above)
                    state.TopTextLine += textHeight / Tables.STAVE_LINE_DISTANCE + 0.5;
                if (pos == ModifierPosition.Below)
                    state.TextLine += textHeight / Tables.STAVE_LINE_DISTANCE + 0.5;

                if (!ReferenceEquals(note, prevNote))
                {
                    if (leftShift == 0)
                        shiftLeft = Math.Max(note.GetLeftDisplacedHeadPx(), shiftLeft);
                    if (rightShift == 0)
                        shiftRight = Math.Max(note.GetRightDisplacedHeadPx(), shiftRight);
                    prevNote = note;
                }

                numsList.Add((note, num, pos, props.Line, shiftLeft, shiftRight));
            }

            numsList.Sort((a, b) => b.Line.CompareTo(a.Line));

            double numShiftL = 0;
            double numShiftR = 0;
            double xWidthL = 0;
            double xWidthR = 0;
            double? lastLine = null;
            Note lastNote = null;

            foreach (var item in numsList)
            {
                if (lastLine == null || Math.Abs(item.Line - lastLine.Value) > double.Epsilon || !ReferenceEquals(item.Note, lastNote))
                {
                    numShiftL = leftShift + item.ShiftL;
                    numShiftR = rightShift + item.ShiftR;
                }

                double numWidth = item.Num.GetWidth() + Metrics.GetDouble("FretHandFinger.numSpacing");
                if (item.Pos == ModifierPosition.Left)
                {
                    item.Num.SetXShift(leftShift + numShiftL);
                    double numShift = leftShift + numWidth;
                    if (numShift > xWidthL) xWidthL = numShift;
                }
                else if (item.Pos == ModifierPosition.Right)
                {
                    item.Num.SetXShift(numShiftR);
                    double numShift = shiftRight + numWidth;
                    if (numShift > xWidthR) xWidthR = numShift;
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
            double dotX = start.X + xOffset;
            double dotY = start.Y + yOffset + Metrics.GetDouble("FretHandFinger.baseYShift");

            switch (position)
            {
                case ModifierPosition.Above:
                    dotX += Metrics.GetDouble("FretHandFinger.aboveXShift");
                    dotY += Metrics.GetDouble("FretHandFinger.aboveYShift");
                    break;
                case ModifierPosition.Below:
                    dotX += Metrics.GetDouble("FretHandFinger.belowXShift");
                    dotY += Metrics.GetDouble("FretHandFinger.belowYShift");
                    break;
                case ModifierPosition.Left:
                    dotX -= width;
                    break;
                case ModifierPosition.Right:
                    dotX += Metrics.GetDouble("FretHandFinger.rightXShift");
                    break;
                default:
                    throw new VexFlowException("InvalidPosition", $"The position {position} does not exist");
            }

            var font = Metrics.GetFontInfo("FretHandFinger");
            ctx.Save();
            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
            ctx.FillText(finger, dotX, dotY);
            ctx.Restore();
        }
    }
}

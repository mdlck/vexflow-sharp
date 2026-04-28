#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    public enum StaveHairpinType
    {
        Crescendo = 1,
        Decrescendo = 2,
    }

    public class StaveHairpinNotes
    {
        public Note? FirstNote { get; set; }
        public Note? LastNote { get; set; }
    }

    public class StaveHairpinRenderOptions
    {
        public double? RightShiftTicks { get; set; }
        public double? LeftShiftTicks { get; set; }
        public double LeftShiftPx { get; set; }
        public double RightShiftPx { get; set; }
        public double Height { get; set; } = 10;
        public double YShift { get; set; }
    }

    /// <summary>
    /// Draws a crescendo or decrescendo hairpin between two notes.
    /// Port of VexFlow's StaveHairpin class from stavehairpin.ts.
    /// </summary>
    public class StaveHairpin : Element
    {
        public new const string CATEGORY = "StaveHairpin";

        public static class Type
        {
            public const int CRESC = 1;
            public const int DECRESC = 2;
        }

        private int hairpin;
        private ModifierPosition position;
        private StaveHairpinNotes notes = new StaveHairpinNotes();
        private Note? firstNote;
        private Note? lastNote;

        public StaveHairpinRenderOptions RenderOptions { get; private set; }

        public StaveHairpin(StaveHairpinNotes notes, int type)
        {
            RenderOptions = new StaveHairpinRenderOptions();
            hairpin = type;
            position = ModifierPosition.Below;
            SetNotes(notes);
        }

        public StaveHairpin(StaveHairpinNotes notes, StaveHairpinType type)
            : this(notes, (int)type)
        {
        }

        public static void FormatByTicksAndDraw(
            RenderContext ctx,
            double pixelsPerTick,
            StaveHairpinNotes notes,
            int type,
            ModifierPosition position,
            StaveHairpinRenderOptions options)
        {
            if (pixelsPerTick == 0)
                throw new VexFlowException("BadArguments", "A valid Formatter must be provide to draw offsets by ticks.");

            var hairpinOptions = new StaveHairpinRenderOptions
            {
                Height = options.Height,
                YShift = options.YShift,
                LeftShiftPx = pixelsPerTick * (options.LeftShiftTicks ?? 0),
                RightShiftPx = pixelsPerTick * (options.RightShiftTicks ?? 0),
                LeftShiftTicks = 0,
                RightShiftTicks = 0,
            };

            var hairpin = new StaveHairpin(notes, type)
                .SetRenderOptions(hairpinOptions)
                .SetPosition(position);
            hairpin.SetContext(ctx);
            hairpin.DrawWithStyle();
        }

        public override string GetCategory() => CATEGORY;

        public StaveHairpin SetPosition(ModifierPosition newPosition)
        {
            if (newPosition == ModifierPosition.Above || newPosition == ModifierPosition.Below)
                position = newPosition;
            return this;
        }

        public ModifierPosition GetPosition() => position;

        public StaveHairpin SetRenderOptions(StaveHairpinRenderOptions options)
        {
            RenderOptions = options;
            return this;
        }

        public StaveHairpin SetNotes(StaveHairpinNotes newNotes)
        {
            if (newNotes.FirstNote == null && newNotes.LastNote == null)
                throw new VexFlowException("BadArguments", "Hairpin needs to have either firstNote or lastNote set.");

            notes = newNotes;
            firstNote = notes.FirstNote;
            lastNote = notes.LastNote;
            return this;
        }

        public StaveHairpinNotes GetNotes() => notes;

        public void RenderHairpin(double firstX, double lastX, double firstY, double lastY, double staffHeight)
        {
            var ctx = CheckContext();
            double dis = RenderOptions.YShift + 20;
            double yShift = firstY;

            if (position == ModifierPosition.Above)
            {
                dis = -dis + 30;
                yShift = firstY - staffHeight;
            }

            double leftShiftPx = RenderOptions.LeftShiftPx;
            double rightShiftPx = RenderOptions.RightShiftPx;

            ctx.BeginPath();

            switch (hairpin)
            {
                case Type.CRESC:
                    ctx.MoveTo(lastX + rightShiftPx, yShift + dis);
                    ctx.LineTo(firstX + leftShiftPx, yShift + RenderOptions.Height / 2 + dis);
                    ctx.LineTo(lastX + rightShiftPx, yShift + RenderOptions.Height + dis);
                    break;
                case Type.DECRESC:
                    ctx.MoveTo(firstX + leftShiftPx, yShift + dis);
                    ctx.LineTo(lastX + rightShiftPx, yShift + RenderOptions.Height / 2 + dis);
                    ctx.LineTo(firstX + leftShiftPx, yShift + RenderOptions.Height + dis);
                    break;
            }

            ctx.Stroke();
            ctx.ClosePath();
        }

        public override void Draw()
        {
            CheckContext();
            SetRendered();

            if (firstNote == null || lastNote == null)
                throw new VexFlowException("NoNote", "Notes required to draw");

            var start = firstNote.GetModifierStartXY(position, 0);
            var end = lastNote.GetModifierStartXY(position, 0);
            var stave = firstNote.CheckStave();

            RenderHairpin(
                start.X,
                end.X,
                stave.GetY() + stave.GetHeight(),
                lastNote.CheckStave().GetY() + lastNote.CheckStave().GetHeight(),
                stave.GetHeight());
        }
    }
}

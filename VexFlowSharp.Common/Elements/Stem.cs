// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

namespace VexFlowSharp
{
    /// <summary>
    /// Options for constructing a Stem.
    /// Port of VexFlow's StemOptions interface from stem.ts.
    /// </summary>
    public class StemOptions
    {
        public double? XBegin { get; set; }
        public double? XEnd { get; set; }
        public double? YTop { get; set; }
        public double? YBottom { get; set; }
        public double? StemExtension { get; set; }
        public int? StemDirection { get; set; }
        public bool? Hide { get; set; }
        public bool? IsStemlet { get; set; }
        public double? StemletHeight { get; set; }
        public double StemUpYOffset { get; set; }
        public double StemDownYOffset { get; set; }
        public double StemUpYBaseOffset { get; set; }
        public double StemDownYBaseOffset { get; set; }
    }

    /// <summary>
    /// Renders a vertical stem line for stemmable notes.
    /// Port of VexFlow's Stem class from stem.ts.
    /// </summary>
    public class Stem : Element
    {
        public new const string CATEGORY = "Stem";

        // Stem directions
        public const int UP = 1;
        public const int DOWN = -1;

        // Theme — matches Tables.STEM_WIDTH and Tables.STEM_HEIGHT
        public const double WIDTH = 1.5;
        public const double HEIGHT = 35;

        protected double xBegin;
        protected double xEnd;
        protected double yTop;
        protected double yBottom;
        protected double stemExtension;
        protected int stemDirection;
        protected bool hide;
        protected bool isStemlet;
        protected double stemletHeight;
        protected double stemUpYOffset;
        protected double stemDownYOffset;
        protected double stemUpYBaseOffset;
        protected double stemDownYBaseOffset;
        protected double renderHeightAdjustment;

        public override string GetCategory() => CATEGORY;

        /// <summary>
        /// Construct a Stem from the given options. All fields default to zero/false.
        /// </summary>
        public Stem(StemOptions options = null)
        {
            options ??= new StemOptions();
            xBegin = options.XBegin ?? 0;
            xEnd = options.XEnd ?? 0;
            yTop = options.YTop ?? 0;
            yBottom = options.YBottom ?? 0;
            stemExtension = options.StemExtension ?? 0;
            stemDirection = options.StemDirection ?? 0;
            hide = options.Hide ?? false;
            isStemlet = options.IsStemlet ?? false;
            stemletHeight = options.StemletHeight ?? 0;
            renderHeightAdjustment = 0;
            SetOptions(options);
        }

        /// <summary>Update configurable per-options fields (y offsets).</summary>
        public void SetOptions(StemOptions options)
        {
            if (options == null) return;
            stemUpYOffset = options.StemUpYOffset;
            stemDownYOffset = options.StemDownYOffset;
            stemUpYBaseOffset = options.StemUpYBaseOffset;
            stemDownYBaseOffset = options.StemDownYBaseOffset;
        }

        /// <summary>Set the x bounds of the notehead.</summary>
        public Stem SetNoteHeadXBounds(double xBegin, double xEnd)
        {
            this.xBegin = xBegin;
            this.xEnd = xEnd;
            return this;
        }

        /// <summary>Set the direction of the stem: Stem.UP (1) or Stem.DOWN (-1).</summary>
        public void SetDirection(int direction)
        {
            stemDirection = direction;
        }

        /// <summary>Set the stem extension amount.</summary>
        public void SetExtension(double ext)
        {
            stemExtension = ext;
        }

        /// <summary>Get the current stem extension.</summary>
        public double GetExtension() => stemExtension;

        /// <summary>Whether the stem is currently visible.</summary>
        public bool IsVisible() => !hide;

        /// <summary>Whether this stem is currently configured as a stemlet.</summary>
        public bool IsStemlet() => isStemlet;

        /// <summary>Get the configured stemlet height.</summary>
        public double GetStemletHeight() => stemletHeight;

        /// <summary>Get the render-only height adjustment applied for flags or beams.</summary>
        public double GetRenderHeightAdjustment() => renderHeightAdjustment;

        /// <summary>Set the y bounds for the top/bottom noteheads.</summary>
        public void SetYBounds(double yTop, double yBottom)
        {
            this.yTop = yTop;
            this.yBottom = yBottom;
        }

        /// <summary>
        /// Get the full height of the stem (signed, positive = upward for UP stems).
        /// </summary>
        public double GetHeight()
        {
            double yOffset = stemDirection == UP ? stemUpYOffset : stemDownYOffset;
            double unsignedHeight = yBottom - yTop + (HEIGHT - yOffset + stemExtension);
            return unsignedHeight * stemDirection;
        }

        /// <summary>
        /// Get the extents: topY is the stem tip, baseY is the outermost notehead.
        /// </summary>
        public (double TopY, double BaseY) GetExtents()
        {
            bool isStemUp = stemDirection == UP;
            double stemHeightTotal = HEIGHT + stemExtension;

            double innerMostY = isStemUp ? System.Math.Min(yTop, yBottom) : System.Math.Max(yTop, yBottom);
            double outerMostY = isStemUp ? System.Math.Max(yTop, yBottom) : System.Math.Min(yTop, yBottom);
            double stemTipY = innerMostY + stemHeightTotal * -stemDirection;

            return (TopY: stemTipY, BaseY: outerMostY);
        }

        /// <summary>Set whether the stem is visible.</summary>
        public Stem SetVisibility(bool isVisible)
        {
            hide = !isVisible;
            return this;
        }

        /// <summary>Configure this stem as a stemlet.</summary>
        public Stem SetStemlet(bool isStemlet, double stemletHeight)
        {
            this.isStemlet = isStemlet;
            this.stemletHeight = stemletHeight;
            return this;
        }

        /// <summary>
        /// Adjusts renderHeightAdjustment to trim the stem tip for a flag.
        /// </summary>
        public void AdjustHeightForFlag()
        {
            renderHeightAdjustment = Metrics.GetDouble("Stem.heightAdjustmentForFlag", -3);
        }

        /// <summary>
        /// Adjusts renderHeightAdjustment for beam attachment.
        /// </summary>
        public void AdjustHeightForBeam()
        {
            renderHeightAdjustment = -(WIDTH / 2);
        }

        /// <summary>
        /// Render the stem onto the canvas using the current render context.
        /// Port of Stem.draw() from stem.ts.
        /// </summary>
        public override void Draw()
        {
            rendered = true;
            if (hide) return;

            var ctx = CheckContext();

            double stemX;
            double stemY;
            double yBaseOffset;

            if (stemDirection == DOWN)
            {
                // Down stems are rendered to the left of the head
                stemX = xBegin;
                stemY = yTop + stemDownYOffset;
                yBaseOffset = stemDownYBaseOffset;
            }
            else
            {
                // Up stems are rendered to the right of the head
                stemX = xEnd;
                stemY = yBottom - stemUpYOffset;
                yBaseOffset = stemUpYBaseOffset;
            }

            double stemHeight = GetHeight();

            // Offset from the stem's base which satisfies the stemlet height
            double stemletYOffset = isStemlet ? stemHeight - stemletHeight * stemDirection : 0;

            ctx.Save();
            ApplyStyle();
            ctx.BeginPath();
            ctx.SetLineWidth(WIDTH);
            ctx.MoveTo(stemX, stemY - stemletYOffset + yBaseOffset);
            ctx.LineTo(stemX, stemY - stemHeight - renderHeightAdjustment * stemDirection);
            ctx.Stroke();
            RestoreStyle();
            ctx.Restore();
        }
    }
}

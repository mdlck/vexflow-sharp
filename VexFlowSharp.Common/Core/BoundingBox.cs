// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;

namespace VexFlowSharp
{
    /// <summary>
    /// Represents a bounding box with X, Y, width, and height.
    /// Port of VexFlow's BoundingBox class from boundingbox.ts.
    /// </summary>
    public class BoundingBox
    {
        /// <summary>X position (left edge).</summary>
        public double X { get; set; }

        /// <summary>Y position (top edge).</summary>
        public double Y { get; set; }

        /// <summary>Width.</summary>
        public double W { get; set; }

        /// <summary>Height.</summary>
        public double H { get; set; }

        /// <summary>
        /// Create a new BoundingBox.
        /// </summary>
        public BoundingBox(double x, double y, double w, double h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        /// <summary>Create an independent copy of <paramref name="that"/>.</summary>
        public static BoundingBox Copy(BoundingBox that)
        {
            return new BoundingBox(that.X, that.Y, that.W, that.H);
        }

        /// <summary>Clone this bounding box.</summary>
        public BoundingBox Clone() => Copy(this);

        // VexFlow API compatibility getters
        public double GetX() => X;
        public double GetY() => Y;
        public double GetW() => W;
        public double GetH() => H;

        // VexFlow API compatibility setters (fluent)
        public BoundingBox SetX(double x) { X = x; return this; }
        public BoundingBox SetY(double y) { Y = y; return this; }
        public BoundingBox SetW(double w) { W = w; return this; }
        public BoundingBox SetH(double h) { H = h; return this; }

        /// <summary>
        /// Move this bounding box by delta (dx, dy).
        /// </summary>
        public BoundingBox Move(double dx, double dy)
        {
            X += dx;
            Y += dy;
            return this;
        }

        /// <summary>
        /// Expand this box to encompass both this box and <paramref name="other"/>.
        /// </summary>
        public BoundingBox MergeWith(BoundingBox other)
        {
            double x = Math.Min(X, other.X);
            double y = Math.Min(Y, other.Y);
            double w = Math.Max(X + W, other.X + other.W) - x;
            double h = Math.Max(Y + H, other.Y + other.H) - y;
            X = x;
            Y = y;
            W = w;
            H = h;
            return this;
        }
    }
}

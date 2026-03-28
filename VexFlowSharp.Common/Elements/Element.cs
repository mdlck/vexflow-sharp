// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Style properties for an element. Maps to VexFlow's ElementStyle interface.
    /// </summary>
    public class ElementStyle
    {
        /// <summary>CSS shadow color.</summary>
        public string? ShadowColor { get; set; }

        /// <summary>Shadow blur level.</summary>
        public double? ShadowBlur { get; set; }

        /// <summary>CSS fill color.</summary>
        public string? FillStyle { get; set; }

        /// <summary>CSS stroke color.</summary>
        public string? StrokeStyle { get; set; }

        /// <summary>Line width (default 1.0).</summary>
        public double? LineWidth { get; set; }
    }

    /// <summary>
    /// Attribute bag for an element (id, type, class).
    /// </summary>
    public class ElementAttributes
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
    }

    /// <summary>
    /// Abstract base class for all VexFlow renderable elements.
    /// Port of VexFlow's Element class from element.ts.
    /// </summary>
    public abstract class Element
    {
        // All Element objects keep a list of children that inherit parent style.
        protected List<Element> children = new List<Element>();

        protected ElementStyle? style;
        protected BoundingBox? boundingBox;
        protected bool rendered = false;

        private string? id;

        /// <summary>
        /// Render context for this element. Typed as RenderContext.
        /// </summary>
        protected RenderContext? context;

        /// <summary>
        /// Draw this element. Subclasses must override to provide rendering logic.
        /// </summary>
        public virtual void Draw() { }

        /// <summary>
        /// Get the render context, throwing VexFlowException if not set.
        /// Maps to VexFlow's checkContext().
        /// </summary>
        public RenderContext CheckContext()
            => context ?? throw new VexFlowException("NoContext", "No render context set.");

        /// <summary>
        /// Set the render context for this element. Returns this for fluent chaining.
        /// Maps to VexFlow's setContext().
        /// </summary>
        public Element SetContext(RenderContext ctx)
        {
            context = ctx;
            return this;
        }

        /// <summary>
        /// Set the element style.
        /// </summary>
        public Element SetStyle(ElementStyle s)
        {
            style = s;
            return this;
        }

        /// <summary>
        /// Get the element style.
        /// </summary>
        public ElementStyle? GetStyle() => style;

        /// <summary>
        /// Apply style to the render context if one is set.
        /// </summary>
        public Element ApplyStyle(ElementStyle? overrideStyle = null)
        {
            var s = overrideStyle ?? style;
            if (s == null || context == null) return this;
            if (s.FillStyle != null) context.SetFillStyle(s.FillStyle);
            if (s.StrokeStyle != null) context.SetStrokeStyle(s.StrokeStyle);
            if (s.ShadowColor != null) context.SetShadowColor(s.ShadowColor);
            if (s.ShadowBlur.HasValue) context.SetShadowBlur(s.ShadowBlur.Value);
            if (s.LineWidth.HasValue) context.SetLineWidth(s.LineWidth.Value);
            return this;
        }

        /// <summary>
        /// Restore context state after ApplyStyle. Calls context.Restore() if a style was set.
        /// </summary>
        public Element RestoreStyle(ElementStyle? overrideStyle = null)
        {
            var s = overrideStyle ?? style;
            if (s == null || context == null) return this;
            context.Restore();
            return this;
        }

        /// <summary>
        /// Get the bounding box for this element. May be null if not yet calculated.
        /// </summary>
        public BoundingBox? GetBoundingBox() => boundingBox;

        /// <summary>
        /// Get the list of child elements.
        /// </summary>
        public List<Element> GetChildren() => children;

        /// <summary>
        /// Add a child element to this element's children list.
        /// </summary>
        public Element AddChild(Element child)
        {
            children.Add(child);
            return this;
        }

        /// <summary>
        /// Get element id. Returns null if not set.
        /// </summary>
        public string? GetId() => id;

        /// <summary>
        /// Set element id.
        /// </summary>
        public Element SetId(string newId)
        {
            id = newId;
            return this;
        }

        /// <summary>
        /// Whether this element has been rendered.
        /// </summary>
        public bool IsRendered() => rendered;

        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>
        /// Get the category string for this element.
        /// Port of VexFlow's Element.getCategory() from element.ts.
        /// Subclasses override to return their own category (e.g. "StaveNote").
        /// ModifierContext uses the category to group members.
        /// </summary>
        public virtual string GetCategory() => "";
    }
}

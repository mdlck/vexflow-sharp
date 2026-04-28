#nullable enable annotations

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

        /// <summary>Stroke dash pattern, matching SVG stroke-dasharray syntax (e.g. "4 2").</summary>
        public string? LineDash { get; set; }
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
        public const string CATEGORY = "Element";

        // All Element objects keep a list of children that inherit parent style.
        protected Element? parent;
        protected List<Element> children = new List<Element>();

        protected ElementStyle? style;
        protected BoundingBox? boundingBox;
        protected bool rendered = false;

        private static int nextId = 1000;
        private string? id = NewId();
        private string? type;
        private readonly List<string> classes = new List<string>();
        private readonly Dictionary<string, string?> attributes = new Dictionary<string, string?>();
        private Registry? registry;

        /// <summary>
        /// Render context for this element. Typed as RenderContext.
        /// </summary>
        protected RenderContext? context;

        protected Element()
        {
            Registry.GetDefaultRegistry()?.Register(this);
        }

        private static string NewId() => $"auto{nextId++}";

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
        /// Return the render context if one has been assigned.
        /// </summary>
        public RenderContext? GetContext() => context;

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

        /// <summary>Set this element and all child elements to the same style.</summary>
        public Element SetGroupStyle(ElementStyle s)
        {
            style = s;
            foreach (var child in children)
                child.SetGroupStyle(s);
            return this;
        }

        private void ApplyStyleProperties(RenderContext targetContext, ElementStyle s)
        {
            if (s.FillStyle != null) targetContext.SetFillStyle(s.FillStyle);
            if (s.StrokeStyle != null) targetContext.SetStrokeStyle(s.StrokeStyle);
            if (s.ShadowColor != null) targetContext.SetShadowColor(s.ShadowColor);
            if (s.ShadowBlur.HasValue) targetContext.SetShadowBlur(s.ShadowBlur.Value);
            if (s.LineWidth.HasValue) targetContext.SetLineWidth(s.LineWidth.Value);
            if (!string.IsNullOrWhiteSpace(s.LineDash))
            {
                var parts = s.LineDash.Split(new[] { ' ', ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                var dash = new double[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    dash[i] = double.Parse(parts[i], System.Globalization.CultureInfo.InvariantCulture);
                targetContext.SetLineDash(dash);
            }
        }

        /// <summary>
        /// Apply style to the render context if one is set.
        /// </summary>
        public Element ApplyStyle(ElementStyle? overrideStyle = null)
        {
            var s = overrideStyle ?? style;
            if (s == null || context == null) return this;
            context.Save();
            ApplyStyleProperties(context, s);
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

        /// <summary>Draw this element with its style wrapped in a save/restore pair.</summary>
        public Element DrawWithStyle()
        {
            var ctx = CheckContext();
            ctx.Save();
            if (style != null)
                ApplyStyleProperties(ctx, style);
            Draw();
            ctx.Restore();
            return this;
        }

        /// <summary>
        /// Get the bounding box for this element. May be null if not yet calculated.
        /// </summary>
        public virtual BoundingBox? GetBoundingBox() => boundingBox;

        /// <summary>
        /// Draw the pointer rectangle used by v5 SVG interaction surfaces when enabled by metrics.
        /// </summary>
        protected void DrawPointerRect()
        {
            if (context == null || !Metrics.GetBool($"{GetType().Name}.pointerRect"))
                return;

            var box = GetBoundingBox();
            if (box != null)
                context.PointerRect(box.GetX(), box.GetY(), box.GetW(), box.GetH());
        }

        /// <summary>
        /// Get the list of child elements.
        /// </summary>
        public List<Element> GetChildren() => children;

        /// <summary>
        /// Add a child element to this element's children list.
        /// </summary>
        public Element AddChild(Element child)
        {
            if (child.parent != null)
                throw new VexFlowException("Element", "Parent already defined");

            child.parent = this;
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
            SetAttribute("id", newId);
            return this;
        }

        /// <summary>Add a CSS-style class name to this element.</summary>
        public Element AddClass(string className)
        {
            if (!string.IsNullOrWhiteSpace(className) && !classes.Contains(className))
            {
                classes.Add(className);
                registry?.OnUpdate(new RegistryUpdate
                {
                    Id = id ?? string.Empty,
                    Name = "class",
                    Value = className,
                    OldValue = null,
                });
            }
            return this;
        }

        /// <summary>Remove a CSS-style class name from this element.</summary>
        public Element RemoveClass(string className)
        {
            if (classes.Remove(className))
            {
                registry?.OnUpdate(new RegistryUpdate
                {
                    Id = id ?? string.Empty,
                    Name = "class",
                    Value = null,
                    OldValue = className,
                });
            }
            return this;
        }

        /// <summary>Return whether this element has the given class name.</summary>
        public bool HasClass(string className) => classes.Contains(className);

        /// <summary>Get a copy of this element's class list.</summary>
        public List<string> GetClasses() => new List<string>(classes);

        /// <summary>Set a VexFlow-style string attribute.</summary>
        public Element SetAttribute(string name, string? value)
        {
            var oldId = id ?? string.Empty;
            var oldValue = GetAttribute(name);

            switch (name)
            {
                case "id":
                    id = value;
                    break;
                case "type":
                    type = value;
                    break;
                case "class":
                    foreach (var className in classes)
                    {
                        registry?.OnUpdate(new RegistryUpdate
                        {
                            Id = oldId,
                            Name = "class",
                            Value = null,
                            OldValue = className,
                        });
                    }
                    classes.Clear();
                    if (value != null)
                    {
                        foreach (var className in value.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
                            AddClass(className);
                    }
                    break;
                default:
                    attributes[name] = value;
                    break;
            }

            if (name != "class")
            {
                registry?.OnUpdate(new RegistryUpdate
                {
                    Id = oldId,
                    Name = name,
                    Value = value,
                    OldValue = oldValue,
                });
            }

            return this;
        }

        /// <summary>Get a VexFlow-style string attribute.</summary>
        public string? GetAttribute(string name)
        {
            return name switch
            {
                "id" => id,
                "type" => type ?? GetCategory(),
                "class" => classes.Count == 0 ? null : string.Join(" ", classes),
                _ => attributes.TryGetValue(name, out var value) ? value : null,
            };
        }

        /// <summary>Return a copy of this element's VexFlow-style attributes.</summary>
        public ElementAttributes GetAttributes()
            => new ElementAttributes
            {
                Id = id ?? string.Empty,
                Type = GetAttribute("type") ?? string.Empty,
                Class = GetAttribute("class") ?? string.Empty,
            };

        /// <summary>Called by Registry after this element is registered.</summary>
        public Element OnRegister(Registry newRegistry)
        {
            registry = newRegistry;
            return this;
        }

        /// <summary>
        /// Whether this element has been rendered.
        /// </summary>
        public bool IsRendered() => rendered;

        /// <summary>
        /// Set the rendered status.
        /// </summary>
        public Element SetRendered(bool rendered = true)
        {
            this.rendered = rendered;
            return this;
        }

        // ── Category ──────────────────────────────────────────────────────────

        /// <summary>
        /// Get the category string for this element.
        /// Port of VexFlow's Element.getCategory() from element.ts.
        /// Subclasses override to return their own category (e.g. "StaveNote").
        /// ModifierContext uses the category to group members.
        /// </summary>
        public virtual string GetCategory() => type ?? CATEGORY;
    }
}

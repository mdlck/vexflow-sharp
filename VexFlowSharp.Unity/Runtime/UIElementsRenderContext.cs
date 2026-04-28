using System;
using System.Collections.Generic;
using System.Linq;
using SysNumerics = System.Numerics;
using UnityEngine;
using UnityEngine.UIElements;
using UnityFont = UnityEngine.Font;
using UnityFontStyle = UnityEngine.FontStyle;

namespace VexFlowSharp.Unity
{
    /// <summary>
    /// Unity UI Toolkit Painter2D-backed implementation of RenderContext.
    /// Maps every RenderContext abstract method to its Painter2D equivalent.
    /// Transform stack is maintained manually via System.Numerics.Matrix3x2
    /// because Painter2D has no built-in Save/Restore transform API.
    /// </summary>
    public class UIElementsRenderContext : RenderContext
    {
        private Painter2D _p;
        private readonly VexFlowElement _owner;

        // Paint state -- stored and applied immediately before Fill()/Stroke()
        private Color _fillColor = Color.black;
        private Color _strokeColor = Color.black;
        private float _lineWidth = 1f;
        private LineCap _lineCap = LineCap.Butt;
        private string _backgroundFillStyle = "#FFFFFF";

        // Font state
        private string _currentFont = "Arial";
        private double _fontSize = 10;
        private string _fontWeight = "normal";
        private string _fontStyle = "normal";
        private UnityFont? _unityFont;
        private UnityFontStyle _unityFontStyle = UnityFontStyle.Normal;
        private static readonly Dictionary<string, UnityFont?> _fontCache =
            new Dictionary<string, UnityFont?>(StringComparer.OrdinalIgnoreCase);

        // Transform + paint state stack (Painter2D has no Save/Restore -- maintained manually)
        private readonly Stack<(SysNumerics.Matrix3x2 transform,
                                Color fillColor,
                                Color strokeColor,
                                float lineWidth,
                                LineCap lineCap,
                                string fontFamily,
                                double fontSize,
                                string fontWeight,
                                string fontStyle,
                                UnityFont? unityFont,
                                UnityFontStyle unityFontStyle)> _stateStack
            = new Stack<(SysNumerics.Matrix3x2, Color, Color, float, LineCap, string, double, string, string, UnityFont?, UnityFontStyle)>();
        private SysNumerics.Matrix3x2 _currentTransform = SysNumerics.Matrix3x2.Identity;

        /// <summary>
        /// Constructor for direct usage with an existing Painter2D (e.g. testing or one-shot rendering).
        /// </summary>
        public UIElementsRenderContext(Painter2D painter, VexFlowElement owner)
        {
            _p = painter;
            _owner = owner;
        }

        /// <summary>
        /// Constructor for long-lived context usage (VexFlowElement pattern).
        /// Painter2D is set later via SetPainter() during each generateVisualContent callback.
        /// </summary>
        internal UIElementsRenderContext(VexFlowElement owner)
        {
            _p = null!;
            _owner = owner;
        }

        /// <summary>
        /// Update the Painter2D reference. Called by VexFlowElement.OnGenerateVisualContent
        /// at the start of each repaint (Painter2D is only valid inside the callback).
        /// Pass null after Draw() completes to prevent stale access.
        /// </summary>
        internal void SetPainter(Painter2D painter)
        {
            _p = painter;
        }

        // ── State ──────────────────────────────────────────────────────────────

        public override void Clear()
        {
            // No-op: VisualElement canvas is cleared by the UIElements framework
            // before each generateVisualContent callback.
        }

        public override RenderContext Save()
        {
            _stateStack.Push((_currentTransform,
                              _fillColor,
                              _strokeColor,
                              _lineWidth,
                              _lineCap,
                              _currentFont,
                              _fontSize,
                              _fontWeight,
                              _fontStyle,
                              _unityFont,
                              _unityFontStyle));
            return this;
        }

        public override RenderContext Restore()
        {
            if (_stateStack.Count > 0)
            {
                var s = _stateStack.Pop();
                _currentTransform = s.transform;
                _fillColor = s.fillColor;
                _strokeColor = s.strokeColor;
                _lineWidth = s.lineWidth;
                _lineCap = s.lineCap;
                _currentFont = s.fontFamily;
                _fontSize = s.fontSize;
                _fontWeight = s.fontWeight;
                _fontStyle = s.fontStyle;
                _unityFont = s.unityFont;
                _unityFontStyle = s.unityFontStyle;
            }
            return this;
        }

        // ── Styles ─────────────────────────────────────────────────────────────

        public override RenderContext SetFillStyle(string style)
        {
            _fillColor = ParseColor(style);
            return this;
        }

        public override RenderContext SetBackgroundFillStyle(string style)
        {
            _backgroundFillStyle = style;
            return this;
        }

        public override RenderContext SetStrokeStyle(string style)
        {
            _strokeColor = ParseColor(style);
            return this;
        }

        public override RenderContext SetShadowColor(string color)
        {
            // No-op: Painter2D has no shadow support
            return this;
        }

        public override RenderContext SetShadowBlur(double blur)
        {
            // No-op: Painter2D has no shadow support
            return this;
        }

        public override RenderContext SetLineWidth(double width)
        {
            _lineWidth = (float)width;
            return this;
        }

        public override RenderContext SetLineCap(string capType)
        {
            _lineCap = capType switch
            {
                "round" => LineCap.Round,
                "square" => LineCap.Butt, // Painter2D has no Square cap; Butt is the closest flat alternative
                _ => LineCap.Butt,
            };
            return this;
        }

        public override RenderContext SetLineDash(double[] dashPattern)
        {
            // No-op: Painter2D has no dashed line support in Unity 2022.3
            return this;
        }

        // ── Transform ──────────────────────────────────────────────────────────

        public override RenderContext Scale(double x, double y)
        {
            _currentTransform *= SysNumerics.Matrix3x2.CreateScale((float)x, (float)y);
            return this;
        }

        public override RenderContext Resize(double width, double height)
        {
            // No-op: VisualElement size is controlled by UIElements layout engine
            return this;
        }

        // ── Rectangles ─────────────────────────────────────────────────────────

        public override RenderContext Rect(double x, double y, double width, double height)
        {
            _p.MoveTo(TransformPoint(x, y));
            _p.LineTo(TransformPoint(x + width, y));
            _p.LineTo(TransformPoint(x + width, y + height));
            _p.LineTo(TransformPoint(x, y + height));
            _p.ClosePath();
            return this;
        }

        public override RenderContext FillRect(double x, double y, double width, double height)
        {
            // Painter2D's GPU tessellator silently discards sub-pixel filled paths.
            // Pad thin dimensions to 2px (centered on the original rect) so the
            // fill geometry is always rasterised.
            const double minDim = 2.0;
            if (width < minDim)
            {
                x -= (minDim - width) * 0.5;
                width = minDim;
            }
            if (height < minDim)
            {
                y -= (minDim - height) * 0.5;
                height = minDim;
            }

            _p.BeginPath();
            _p.MoveTo(TransformPoint(x, y));
            _p.LineTo(TransformPoint(x + width, y));
            _p.LineTo(TransformPoint(x + width, y + height));
            _p.LineTo(TransformPoint(x, y + height));
            _p.ClosePath();
            _p.fillColor = _fillColor;
            _p.Fill();
            return this;
        }

        public override RenderContext ClearRect(double x, double y, double width, double height)
        {
            // Fill with background (white) to simulate a clear
            var savedFill = _fillColor;
            _fillColor = ParseColor(_backgroundFillStyle);
            FillRect(x, y, width, height);
            _fillColor = savedFill;
            return this;
        }

        // ── Path ───────────────────────────────────────────────────────────────

        public override RenderContext BeginPath()
        {
            _p.BeginPath();
            return this;
        }

        public override RenderContext MoveTo(double x, double y)
        {
            _p.MoveTo(TransformPoint(x, y));
            return this;
        }

        public override RenderContext LineTo(double x, double y)
        {
            _p.LineTo(TransformPoint(x, y));
            return this;
        }

        public override RenderContext BezierCurveTo(
            double cp1x, double cp1y,
            double cp2x, double cp2y,
            double x, double y)
        {
            _p.BezierCurveTo(
                TransformPoint(cp1x, cp1y),
                TransformPoint(cp2x, cp2y),
                TransformPoint(x, y));
            return this;
        }

        public override RenderContext QuadraticCurveTo(double cpx, double cpy, double x, double y)
        {
            _p.QuadraticCurveTo(TransformPoint(cpx, cpy), TransformPoint(x, y));
            return this;
        }

        public override RenderContext Arc(
            double x, double y, double radius,
            double startAngle, double endAngle,
            bool counterclockwise)
        {
            _p.Arc(
                TransformPoint(x, y),
                (float)radius,
                Angle.Radians((float)startAngle),
                Angle.Radians((float)endAngle),
                counterclockwise ? ArcDirection.CounterClockwise : ArcDirection.Clockwise);
            return this;
        }

        public override RenderContext ClosePath()
        {
            _p.ClosePath();
            return this;
        }

        public override RenderContext Fill()
        {
            _p.fillColor = _fillColor;
            _p.Fill();
            return this;
        }

        public override RenderContext Stroke()
        {
            _p.strokeColor = _strokeColor;
            _p.lineWidth = _lineWidth;
            _p.lineCap = _lineCap;
            _p.Stroke();
            return this;
        }

        // ── Text ───────────────────────────────────────────────────────────────

        public override RenderContext FillText(string text, double x, double y)
        {
            var label = _owner.GetOrCreateLabel();
            var position = TransformPoint(x, y);
            label.text = text;
            label.style.left = position.x;
            label.style.top = position.y;
            label.style.color = _fillColor;
            label.style.fontSize = FontSizeToPixels(_fontSize);
            label.style.unityFont = _unityFont;
            label.style.unityFontStyleAndWeight = _unityFontStyle;
            return this;
        }

        public override RenderContext SetFont(
            string family, double size,
            string weight = "normal", string style = "normal")
        {
            _currentFont = family;
            _fontSize = size;
            _fontWeight = weight;
            _fontStyle = style;
            _unityFont = ResolveUnityFont(family);
            _unityFontStyle = ResolveFontStyle(weight, style);
            return this;
        }

        public override string GetFont()
        {
            return $"{_fontStyle} {_fontWeight} {_fontSize}px {_currentFont}";
        }

        public override TextMeasure MeasureText(string text)
        {
            // Heuristic approximation -- Painter2D has no text measurement API.
            // Mirrors the TextFormatter fallback used for non-measurable backends.
            double fontSizeInPixels = FontSizeToPixels(_fontSize);
            return new TextMeasure
            {
                X = 0,
                Y = 0,
                Width = text.Length * fontSizeInPixels * 0.6,
                Height = fontSizeInPixels,
            };
        }

        // ── Private Helpers ────────────────────────────────────────────────────

        private Vector2 TransformPoint(double x, double y)
        {
            var pt = SysNumerics.Vector2.Transform(
                new SysNumerics.Vector2((float)x, (float)y),
                _currentTransform);
            return new Vector2(pt.X, pt.Y);
        }

        private static float FontSizeToPixels(double sizeInPoints)
            => (float)(sizeInPoints * Metrics.GetDouble("TextFormatter.ptToPx"));

        private static UnityFont? ResolveUnityFont(string familyList)
        {
            var families = familyList
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeFamilyName)
                .ToArray();

            if (families.Length > 1)
            {
                families = families
                    .OrderBy(IsLikelyMusicFontFamily)
                    .ToArray();
            }

            foreach (var family in families)
            {
                if (_fontCache.TryGetValue(family, out var cached))
                    return cached;

                var font = LoadUnityFont(family);
                _fontCache[family] = font;
                if (font != null) return font;
            }

            return null;
        }

        private static UnityFont? LoadUnityFont(string family)
        {
            foreach (var resourceName in GetResourceFontNames(family))
            {
                var font = Resources.Load<UnityFont>($"Fonts/{resourceName}");
                if (font != null) return font;
            }

            try
            {
                return UnityFont.CreateDynamicFontFromOSFont(family, 16);
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<string> GetResourceFontNames(string family)
        {
            switch (family.ToLowerInvariant())
            {
                case "academico":
                    yield return "Academico";
                    break;
                case "bravura":
                    yield return "Bravura";
                    break;
            }
        }

        private static string NormalizeFamilyName(string family)
        {
            var trimmed = family.Trim();
            if (trimmed.Length >= 2
                && ((trimmed[0] == '\'' && trimmed[trimmed.Length - 1] == '\'')
                    || (trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')))
            {
                return trimmed.Substring(1, trimmed.Length - 2).Trim();
            }

            return trimmed;
        }

        private static bool IsLikelyMusicFontFamily(string family)
        {
            var lower = family.ToLowerInvariant();
            return lower == "bravura"
                || lower == "petaluma"
                || lower == "gonville"
                || lower == "gootville"
                || lower == "leland"
                || lower == "leipzig"
                || lower == "musejazz"
                || lower == "sebastian"
                || lower.StartsWith("finale", StringComparison.OrdinalIgnoreCase);
        }

        private static UnityFontStyle ResolveFontStyle(string weight, string style)
        {
            bool bold = weight.IndexOf("bold", StringComparison.OrdinalIgnoreCase) >= 0;
            bool italic = style.IndexOf("italic", StringComparison.OrdinalIgnoreCase) >= 0;
            if (bold && italic) return UnityFontStyle.BoldAndItalic;
            if (bold) return UnityFontStyle.Bold;
            if (italic) return UnityFontStyle.Italic;
            return UnityFontStyle.Normal;
        }

        private static Color ParseColor(string style)
        {
            if (string.IsNullOrEmpty(style)) return Color.black;
            if (style.StartsWith("#") && ColorUtility.TryParseHtmlString(style, out var c))
                return c;
            return style.ToLowerInvariant() switch
            {
                "black" => Color.black,
                "white" => Color.white,
                "red" => Color.red,
                "blue" => Color.blue,
                "green" => Color.green,
                _ => Color.black,
            };
        }
    }
}

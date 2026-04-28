using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;

namespace VexFlowSharp.Skia
{
    /// <summary>
    /// SkiaSharp-backed implementation of RenderContext for PNG rendering.
    /// Used to verify glyph rendering output and smoke-test the rendering pipeline.
    /// </summary>
    public class SkiaRenderContext : RenderContext, IDisposable
    {
        private readonly SKCanvas _canvas;
        private readonly SKSurface _surface;
        private SKPath _currentPath = new SKPath();
        private SKPaint _fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = SKColors.Black };
        private SKPaint _strokePaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, Color = SKColors.Black, StrokeWidth = 1 };
        private string _fillStyle = "black";
        private string _strokeStyle = "black";
        private string _backgroundFillStyle = "#FFFFFF";
        private string _currentFont = "Arial";
        private double _fontSize = 10;
        private string _fontWeight = "normal";
        private string _fontStyle = "normal";
        private string _shadowColor = "transparent";
        private double _shadowBlur = 0;

        // Paint state stack — SKCanvas.Save/Restore only saves transform/clip,
        // not paint colors. We maintain our own stack to mirror HTML5 canvas semantics.
        private readonly Stack<(SKColor fillColor,
                                string fillStyle,
                                SKColor strokeColor,
                                string strokeStyle,
                                float strokeWidth,
                                SKStrokeCap strokeCap,
                                SKPathEffect? pathEffect,
                                string fontFamily,
                                double fontSize,
                                string fontWeight,
                                string fontStyle,
                                string shadowColor,
                                double shadowBlur)> _paintStack
            = new Stack<(SKColor, string, SKColor, string, float, SKStrokeCap, SKPathEffect?, string, double, string, string, string, double)>();

        public SkiaRenderContext(int width, int height)
        {
            var info = new SKImageInfo(width, height);
            _surface = SKSurface.Create(info);
            _canvas = _surface.Canvas;
            _canvas.Clear(SKColors.Transparent);
        }

        public static SkiaRenderContext Create(int width = 640, int height = 480)
            => new SkiaRenderContext(width, height);

        // State

        public override void Clear()
        {
            _canvas.Clear(SKColors.Transparent);
        }

        public override RenderContext Save()
        {
            _canvas.Save();
            _paintStack.Push((_fillPaint.Color,
                              _fillStyle,
                              _strokePaint.Color,
                              _strokeStyle,
                              _strokePaint.StrokeWidth,
                              _strokePaint.StrokeCap,
                              _strokePaint.PathEffect,
                              _currentFont,
                              _fontSize,
                              _fontWeight,
                              _fontStyle,
                              _shadowColor,
                              _shadowBlur));
            return this;
        }

        public override RenderContext Restore()
        {
            _canvas.Restore();
            if (_paintStack.Count > 0)
            {
                var (fillColor,
                     fillStyle,
                     strokeColor,
                     strokeStyle,
                     strokeWidth,
                     strokeCap,
                     pathEffect,
                     fontFamily,
                     fontSize,
                     fontWeight,
                     fontStyle,
                     shadowColor,
                     shadowBlur) = _paintStack.Pop();
                _fillPaint.Color = fillColor;
                _fillStyle = fillStyle;
                _strokePaint.Color = strokeColor;
                _strokeStyle = strokeStyle;
                _strokePaint.StrokeWidth = strokeWidth;
                _strokePaint.StrokeCap = strokeCap;
                _strokePaint.PathEffect = pathEffect;
                _currentFont = fontFamily;
                _fontSize = fontSize;
                _fontWeight = fontWeight;
                _fontStyle = fontStyle;
                _fillPaint.TextSize = (float)fontSize;
                _shadowColor = shadowColor;
                _shadowBlur = shadowBlur;
            }
            return this;
        }

        // Styles

        public override RenderContext SetFillStyle(string style)
        {
            _fillStyle = style;
            _fillPaint.Color = ParseColor(style);
            return this;
        }

        public override RenderContext SetBackgroundFillStyle(string style)
        {
            _backgroundFillStyle = style;
            return this;
        }

        public override RenderContext SetStrokeStyle(string style)
        {
            _strokeStyle = style;
            _strokePaint.Color = ParseColor(style);
            return this;
        }

        public override string FillStyle
        {
            get => _fillStyle;
            set => SetFillStyle(value);
        }

        public override string StrokeStyle
        {
            get => _strokeStyle;
            set => SetStrokeStyle(value);
        }

        public override RenderContext SetShadowColor(string color)
        {
            // SkiaSharp shadow requires SKMaskFilter or manual blurring; stored for future use
            _shadowColor = color;
            return this;
        }

        public override RenderContext SetShadowBlur(double blur)
        {
            // Stored for future use
            _shadowBlur = blur;
            return this;
        }

        public override RenderContext SetLineWidth(double width)
        {
            _strokePaint.StrokeWidth = (float)width;
            return this;
        }

        public override RenderContext SetLineCap(string capType)
        {
            _strokePaint.StrokeCap = capType switch
            {
                "round" => SKStrokeCap.Round,
                "square" => SKStrokeCap.Square,
                _ => SKStrokeCap.Butt,
            };
            return this;
        }

        public override RenderContext SetLineDash(double[] dashPattern)
        {
            if (dashPattern == null || dashPattern.Length == 0)
            {
                _strokePaint.PathEffect = null;
            }
            else
            {
                var normalizedDashPattern = dashPattern.Length % 2 == 0
                    ? dashPattern
                    : dashPattern.Concat(dashPattern).ToArray();
                _strokePaint.PathEffect = SKPathEffect.CreateDash(normalizedDashPattern.Select(d => (float)d).ToArray(), 0);
            }
            return this;
        }

        // Transform

        public override RenderContext Scale(double x, double y)
        {
            _canvas.Scale((float)x, (float)y);
            return this;
        }

        public override RenderContext OpenRotation(double degrees, double x, double y)
        {
            Save();
            _canvas.Translate((float)x, (float)y);
            _canvas.RotateDegrees((float)degrees);
            _canvas.Translate((float)-x, (float)-y);
            return this;
        }

        public override RenderContext CloseRotation()
        {
            Restore();
            return this;
        }

        public override RenderContext Resize(double width, double height)
        {
            // No-op for fixed surface; surface size is set at construction time
            return this;
        }

        // Rectangles

        public override RenderContext Rect(double x, double y, double width, double height)
        {
            _currentPath.AddRect(new SKRect((float)x, (float)y, (float)(x + width), (float)(y + height)));
            return this;
        }

        public override RenderContext FillRect(double x, double y, double width, double height)
        {
            _canvas.DrawRect(new SKRect((float)x, (float)y, (float)(x + width), (float)(y + height)), _fillPaint);
            return this;
        }

        public override RenderContext ClearRect(double x, double y, double width, double height)
        {
            using var paint = new SKPaint { Color = SKColors.Transparent, Style = SKPaintStyle.Fill, BlendMode = SKBlendMode.Clear };
            _canvas.DrawRect(new SKRect((float)x, (float)y, (float)(x + width), (float)(y + height)), paint);
            return this;
        }

        // Path

        public override RenderContext BeginPath()
        {
            _currentPath.Reset();
            return this;
        }

        public override RenderContext MoveTo(double x, double y)
        {
            _currentPath.MoveTo((float)x, (float)y);
            return this;
        }

        public override RenderContext LineTo(double x, double y)
        {
            _currentPath.LineTo((float)x, (float)y);
            return this;
        }

        public override RenderContext BezierCurveTo(double cp1x, double cp1y, double cp2x, double cp2y, double x, double y)
        {
            // NOTE: SkiaSharp CubicTo argument order matches BezierCurveTo exactly: (cp1, cp2, endpoint)
            _currentPath.CubicTo((float)cp1x, (float)cp1y, (float)cp2x, (float)cp2y, (float)x, (float)y);
            return this;
        }

        public override RenderContext QuadraticCurveTo(double cpx, double cpy, double x, double y)
        {
            _currentPath.QuadTo((float)cpx, (float)cpy, (float)x, (float)y);
            return this;
        }

        public override RenderContext Arc(double x, double y, double radius, double startAngle, double endAngle, bool counterclockwise)
        {
            float startDeg = (float)(startAngle * 180.0 / Math.PI);
            float endDeg = (float)(endAngle * 180.0 / Math.PI);
            float sweepDeg = counterclockwise ? -(endDeg - startDeg) : (endDeg - startDeg);
            if (counterclockwise && sweepDeg > 0) sweepDeg -= 360;
            if (!counterclockwise && sweepDeg < 0) sweepDeg += 360;
            var oval = new SKRect((float)(x - radius), (float)(y - radius), (float)(x + radius), (float)(y + radius));
            // ArcTo with exactly ±360° sweep is unreliable in SkiaSharp — use AddOval for full circles.
            if (Math.Abs(sweepDeg) >= 360f)
                _currentPath.AddOval(oval);
            else
                _currentPath.ArcTo(oval, startDeg, sweepDeg, false);
            return this;
        }

        public override RenderContext ClosePath()
        {
            _currentPath.Close();
            return this;
        }

        public override RenderContext Fill()
        {
            _canvas.DrawPath(_currentPath, _fillPaint);
            return this;
        }

        public override RenderContext Stroke()
        {
            _canvas.DrawPath(_currentPath, _strokePaint);
            return this;
        }

        // Text

        public override RenderContext FillText(string text, double x, double y)
        {
            _canvas.DrawText(text, (float)x, (float)y, _fillPaint);
            return this;
        }

        public override RenderContext SetFont(string family, double size, string weight = "normal", string style = "normal")
        {
            _currentFont = family;
            _fontSize = size;
            _fontWeight = weight;
            _fontStyle = style;
            _fillPaint.TextSize = (float)size;
            return this;
        }

        public override string GetFont()
        {
            return $"{_fontStyle} {_fontWeight} {_fontSize}px {_currentFont}";
        }

        public override TextMeasure MeasureText(string text)
        {
            var bounds = new SKRect();
            _fillPaint.MeasureText(text, ref bounds);
            return new TextMeasure
            {
                Width = bounds.Width,
                Height = bounds.Height,
                X = bounds.Left,
                Y = bounds.Top
            };
        }

        // PNG output helpers (not part of RenderContext abstract interface)

        public byte[] ToPng()
        {
            using var image = _surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        public void SavePng(string filePath)
        {
            File.WriteAllBytes(filePath, ToPng());
        }

        // Color parsing helper

        private static SKColor ParseColor(string style)
        {
            if (string.IsNullOrEmpty(style)) return SKColors.Black;
            if (style.StartsWith("#"))
            {
                return SKColor.Parse(style);
            }
            return style.ToLowerInvariant() switch
            {
                "black" => SKColors.Black,
                "white" => SKColors.White,
                "red" => SKColors.Red,
                "blue" => SKColors.Blue,
                "green" => SKColors.Green,
                _ => SKColors.Black,
            };
        }

        public void Dispose()
        {
            _currentPath?.Dispose();
            _fillPaint?.Dispose();
            _strokePaint?.Dispose();
            _surface?.Dispose();
        }
    }
}

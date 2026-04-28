#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;

namespace VexFlowSharp
{
    /// <summary>
    /// Font settings returned by Metrics.GetFontInfo().
    /// Port of VexFlow's FontInfo shape used by metrics.ts.
    /// </summary>
    public class MetricsFontInfo
    {
        public string Family { get; set; } = "Bravura,Academico";
        public double Size { get; set; } = 30;
        public string Weight { get; set; } = "normal";
        public string Style { get; set; } = "normal";
    }

    /// <summary>
    /// Common engraving metrics lookup.
    /// Port of VexFlow 5's Metrics class and MetricsDefaults.
    /// </summary>
    public static class Metrics
    {
        private static readonly Dictionary<string, ElementStyle> styleCache = new Dictionary<string, ElementStyle>();
        private static readonly Dictionary<string, MetricsFontInfo> fontCache = new Dictionary<string, MetricsFontInfo>();

        private static readonly Dictionary<string, object?> defaults = new Dictionary<string, object?>
        {
            ["pointerRect"] = false,
            ["fontFamily"] = "Bravura,Academico",
            ["fontSize"] = 30.0,
            ["fontScale"] = 1.0,
            ["fontWeight"] = "normal",
            ["fontStyle"] = "normal",

            ["Accidental"] = new Dictionary<string, object?>
            {
                ["cautionary"] = new Dictionary<string, object?> { ["fontSize"] = 20.0 },
                ["grace"] = new Dictionary<string, object?> { ["fontSize"] = 20.0 },
                ["noteheadAccidentalPadding"] = 1.0,
                ["leftPadding"] = 2.0,
                ["accidentalSpacing"] = 3.0,
                ["parenLeftPadding"] = 2.0,
                ["parenRightPadding"] = 2.0,
            },

            ["Annotation"] = new Dictionary<string, object?> { ["fontSize"] = 10.0 },
            ["Barline"] = new Dictionary<string, object?>
            {
                ["repeat"] = new Dictionary<string, object?>
                {
                    ["thinBarShiftBegin"] = 3.0,
                    ["thinBarShiftEnd"] = -5.0,
                    ["dotOffset"] = 4.0,
                    ["dotRadius"] = 2.0,
                },
            },
            ["Bend"] = new Dictionary<string, object?>
            {
                ["fontSize"] = 10.0,
                ["line"] = new Dictionary<string, object?>
                {
                    ["strokeStyle"] = "#777777",
                    ["lineWidth"] = 1.0,
                },
            },
            ["ChordSymbol"] = new Dictionary<string, object?>
            {
                ["fontSize"] = 12.0,
                ["spacing"] = 0.05,
                ["subscriptOffset"] = 0.2,
                ["superscriptOffset"] = -0.4,
                ["superSubRatio"] = 0.6,
            },
            ["Dot"] = new Dictionary<string, object?>
            {
                ["radius"] = 2.0,
                ["width"] = 5.0,
                ["spacing"] = 1.0,
                ["graceScale"] = 0.5,
                ["graceWidth"] = 3.0,
            },
            ["Curve"] = new Dictionary<string, object?>
            {
                ["thickness"] = 2.0,
                ["xShift"] = 0.0,
                ["yShift"] = 10.0,
                ["cpHeight"] = 10.0,
            },
            ["Crescendo"] = new Dictionary<string, object?>
            {
                ["height"] = 15.0,
                ["line"] = 0.0,
                ["lineOffset"] = -3.0,
                ["yOffset"] = 1.0,
            },
            ["FretHandFinger"] = new Dictionary<string, object?>
            {
                ["fontSize"] = 9.0,
                ["fontWeight"] = "bold",
                ["numSpacing"] = 1.0,
                ["baseYShift"] = 5.0,
                ["aboveXShift"] = -4.0,
                ["aboveYShift"] = -12.0,
                ["belowXShift"] = -2.0,
                ["belowYShift"] = 10.0,
                ["rightXShift"] = 1.0,
            },
            ["Formatter"] = new Dictionary<string, object?>
            {
                ["softmaxFactor"] = Tables.SOFTMAX_FACTOR,
                ["maxIterations"] = 5.0,
            },
            ["GraceNote"] = new Dictionary<string, object?> { ["fontScale"] = 2.0 / 3.0 },
            ["GraceTabNote"] = new Dictionary<string, object?>
            {
                ["fontScale"] = 2.0 / 3.0,
                ["yShift"] = 0.3,
            },
            ["KeySignature"] = new Dictionary<string, object?>
            {
                ["accidentalSpacing"] = 1.0,
                ["naturalCollisionSpacing"] = 2.0,
                ["flatFallbackWidth"] = 8.0,
                ["sharpFallbackWidth"] = 10.0,
            },
            ["MultiMeasureRest"] = new Dictionary<string, object?>
            {
                ["fontSize"] = Tables.NOTATION_FONT_SCALE,
                ["numberLine"] = -0.5,
                ["line"] = 2.0,
                ["spacingBetweenLinesPx"] = Tables.STAVE_LINE_DISTANCE,
                ["semibreveRestGlyphScale"] = 30.0,
                ["lineThickness"] = 5.0,
                ["serifThickness"] = 2.0,
                ["linePaddingRatio"] = 0.1,
                ["centerRatio"] = 0.5,
                ["symbolLineOffset"] = -1.0,
                ["numberBaselineRatio"] = 0.5,
            },
            ["NoteHead"] = new Dictionary<string, object?> { ["minPadding"] = 2.0 },
            ["Ornament"] = new Dictionary<string, object?>
            {
                ["accidentalUpperPadding"] = 3.0,
                ["accidentalLowerPadding"] = 3.0,
                ["sideShift"] = 2.0,
                ["textLineIncrement"] = 2.0,
                ["lineSpacing"] = 1.0,
                ["beamedLineSpacing"] = 0.5,
            },
            ["PedalMarking"] = new Dictionary<string, object?>
            {
                ["bracketHeight"] = 10.0,
                ["textMarginRight"] = 6.0,
                ["bracketLineWidth"] = 1.0,
                ["text"] = new Dictionary<string, object?>
                {
                    ["fontSize"] = 12.0,
                    ["fontStyle"] = "italic",
                },
            },
            ["Repetition"] = new Dictionary<string, object?>
            {
                ["text"] = new Dictionary<string, object?>
                {
                    ["fontSize"] = 12.0,
                    ["fontWeight"] = "bold",
                    ["offsetX"] = 12.0,
                    ["offsetY"] = 25.0,
                    ["spacing"] = 5.0,
                },
                ["coda"] = new Dictionary<string, object?> { ["offsetY"] = 25.0 },
                ["segno"] = new Dictionary<string, object?> { ["offsetY"] = 10.0 },
            },
            ["Stave"] = new Dictionary<string, object?>
            {
                ["strokeStyle"] = "black",
                ["fontSize"] = 8.0,
                ["numLines"] = 5.0,
                ["spacingBetweenLinesPx"] = Tables.STAVE_LINE_DISTANCE,
                ["spaceAboveStaffLn"] = 4.0,
                ["spaceBelowStaffLn"] = 4.0,
                ["topTextPosition"] = 1.0,
                ["bottomTextPosition"] = 4.0,
                ["verticalBarWidth"] = 10.0,
                ["padding"] = 12.0,
                ["endPaddingMax"] = 10.0,
                ["endPaddingMin"] = 5.0,
                ["unalignedNotePadding"] = 10.0,
            },
            ["StaveConnector"] = new Dictionary<string, object?>
            {
                ["width"] = 3.0,
                ["singleLineWidth"] = 1.0,
                ["doubleXOffset"] = 2.0,
                ["doubleHeightAdjustment"] = 0.5,
                ["thinDoubleGap"] = 3.0,
                ["textLineWidth"] = 2.0,
                ["textXOffset"] = 24.0,
                ["textYOffset"] = 4.0,
                ["boldDoubleLeftXShift"] = 3.0,
                ["boldDoubleRightXShift"] = -5.0,
                ["boldDoubleLeftWidth"] = 3.5,
                ["boldDoubleRightWidth"] = 3.0,
                ["boldDoubleThickLineOffset"] = 2.0,
                ["text"] = new Dictionary<string, object?> { ["fontSize"] = 16.0 },
            },
            ["StaveLine"] = new Dictionary<string, object?> { ["fontSize"] = 10.0 },
            ["StaveSection"] = new Dictionary<string, object?>
            {
                ["fontSize"] = 10.0,
                ["fontWeight"] = "bold",
                ["lineWidth"] = 2.0,
                ["padding"] = 2.0,
                ["strokeStyle"] = "black",
            },
            ["StaveNote"] = new Dictionary<string, object?> { ["pointerRect"] = true },
            ["StaveTempo"] = new Dictionary<string, object?>
            {
                ["fontSize"] = 14.0,
                ["xShift"] = 10.0,
                ["spacing"] = 3.0,
                ["dotOffsetY"] = 2.0,
                ["glyph"] = new Dictionary<string, object?> { ["fontSize"] = 25.0 },
                ["name"] = new Dictionary<string, object?> { ["fontWeight"] = "bold" },
            },
            ["StaveText"] = new Dictionary<string, object?> { ["fontSize"] = 16.0 },
            ["StaveTie"] = new Dictionary<string, object?>
            {
                ["fontSize"] = 10.0,
                ["cp1"] = 8.0,
                ["cp2"] = 12.0,
                ["firstXShift"] = 0.0,
                ["lastXShift"] = 0.0,
                ["textShiftX"] = 0.0,
                ["yShift"] = 7.0,
                ["thickness"] = 2.0,
                ["closeNoteCp1"] = 2.0,
                ["closeNoteCp2"] = 8.0,
            },
            ["Stem"] = new Dictionary<string, object?> { ["strokeStyle"] = "black" },
            ["StringNumber"] = new Dictionary<string, object?>
            {
                ["fontSize"] = 10.0,
                ["fontWeight"] = "bold",
                ["radius"] = 8.0,
                ["circlePadding"] = 4.0,
                ["numSpacing"] = 1.0,
                ["verticalPadding"] = 8.0,
                ["stemPadding"] = 2.0,
                ["leftPadding"] = 5.0,
                ["rightPadding"] = 6.0,
                ["circleLineWidth"] = 1.5,
                ["textBaselineShift"] = 4.5,
                ["extensionStemOffset"] = 5.0,
                ["extensionStartOffset"] = 10.0,
                ["extensionLineWidth"] = 0.6,
                ["extensionDash"] = 3.0,
                ["extensionGap"] = 3.0,
                ["legLength"] = 10.0,
            },
            ["System"] = new Dictionary<string, object?>
            {
                ["x"] = 10.0,
                ["y"] = 10.0,
                ["width"] = 500.0,
                ["spaceBetweenStaves"] = 12.0,
                ["formatIterations"] = 0.0,
                ["alpha"] = 0.5,
            },
            ["Stroke"] = new Dictionary<string, object?>
            {
                ["spacing"] = 5.0,
                ["text"] = new Dictionary<string, object?>
                {
                    ["fontSize"] = 10.0,
                    ["fontStyle"] = "italic",
                    ["fontWeight"] = "bold",
                },
            },
            ["TabNote"] = new Dictionary<string, object?>
            {
                ["text"] = new Dictionary<string, object?> { ["fontSize"] = 9.0 },
            },
            ["TabStave"] = new Dictionary<string, object?>
            {
                ["strokeStyle"] = "#999999",
                ["fontSize"] = 8.0,
                ["spacingBetweenLinesPx"] = 13.0,
                ["numLines"] = 6.0,
                ["topTextPosition"] = 1.0,
            },
            ["TabTie"] = new Dictionary<string, object?>
            {
                ["fontSize"] = 10.0,
                ["cp1"] = 9.0,
                ["cp2"] = 11.0,
                ["yShift"] = 3.0,
            },
            ["TextBracket"] = new Dictionary<string, object?>
            {
                ["fontSize"] = 15.0,
                ["fontStyle"] = "italic",
                ["lineWidth"] = 1.0,
                ["bracketHeight"] = 8.0,
                ["textHeightOffsetHack"] = 1.0,
            },
            ["TextDynamics"] = new Dictionary<string, object?>
            {
                ["glyphFontSize"] = Tables.NOTATION_FONT_SCALE,
                ["line"] = 0.0,
                ["lineOffset"] = -3.0,
            },
            ["TextNote"] = new Dictionary<string, object?>
            {
                ["text"] = new Dictionary<string, object?> { ["fontSize"] = 12.0 },
            },
            ["TextFormatter"] = new Dictionary<string, object?>
            {
                ["defaultAdvanceWidthEm"] = 0.5,
                ["defaultResolution"] = 1000.0,
                ["ptToPx"] = 4.0 / 3.0,
            },
            ["TickContext"] = new Dictionary<string, object?>
            {
                ["padding"] = 1.0,
            },
            ["TabSlide"] = new Dictionary<string, object?>
            {
                ["fontSize"] = 10.0,
                ["fontStyle"] = "italic",
                ["fontWeight"] = "bold",
                ["cp1"] = 11.0,
                ["cp2"] = 14.0,
                ["yShift"] = 0.5,
                ["labelYShift"] = -1.0,
                ["slideEndpointOffset"] = 3.0,
            },
            ["Tremolo"] = new Dictionary<string, object?> { ["spacing"] = 7.0 },
            ["Tuplet"] = new Dictionary<string, object?>
            {
                ["pointerRect"] = true,
                ["yOffset"] = 0.0,
                ["textYOffset"] = 2.0,
                ["bracket"] = new Dictionary<string, object?>
                {
                    ["padding"] = 5.0,
                    ["lineWidth"] = 1.0,
                    ["legLength"] = 10.0,
                },
            },
            ["Vibrato"] = new Dictionary<string, object?>
            {
                ["width"] = 20.0,
                ["rightShift"] = 7.0,
                ["textLineIncrement"] = 1.0,
                ["yShift"] = 5.0,
            },
            ["VibratoBracket"] = new Dictionary<string, object?>
            {
                ["stopNoteOffset"] = 5.0,
                ["tieEndOffset"] = 10.0,
            },
            ["Volta"] = new Dictionary<string, object?>
            {
                ["fontSize"] = 9.0,
                ["fontWeight"] = "bold",
                ["verticalHeightLines"] = 1.5,
                ["lineWidth"] = 1.0,
                ["endAdjustment"] = 5.0,
                ["beginEndAdjustment"] = 3.0,
                ["textXOffset"] = 5.0,
                ["textYOffset"] = 15.0,
            },
        };

        public static void Clear(string? key = null)
        {
            if (key == null)
            {
                fontCache.Clear();
                styleCache.Clear();
                return;
            }

            fontCache.Remove(key);
            styleCache.Remove(key);
        }

        public static void SetFontFamily(string fontFamily)
        {
            defaults["fontFamily"] = fontFamily;
            Clear();
        }

        public static string GetFontFamily() => GetString("fontFamily", "Bravura,Academico");

        public static object? Get(string key, object? defaultValue = null)
        {
            var keyParts = new List<string>(key.Split('.'));
            var lastKeyPart = keyParts[keyParts.Count - 1];
            keyParts.RemoveAt(keyParts.Count - 1);

            Dictionary<string, object?>? current = defaults;
            object? result = defaultValue;
            var index = 0;

            while (current != null)
            {
                if (current.TryGetValue(lastKeyPart, out var value))
                    result = value;

                if (index >= keyParts.Count) break;

                var keyPart = keyParts[index++];
                current = current.TryGetValue(keyPart, out var child) ? child as Dictionary<string, object?> : null;
            }

            return result;
        }

        public static double GetDouble(string key, double defaultValue = 0)
        {
            var value = Get(key, defaultValue);
            return value == null ? defaultValue : Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string GetString(string key, string defaultValue = "")
        {
            return Get(key, defaultValue)?.ToString() ?? defaultValue;
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            var value = Get(key, defaultValue);
            return value == null ? defaultValue : Convert.ToBoolean(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static MetricsFontInfo GetFontInfo(string key)
        {
            if (!fontCache.TryGetValue(key, out var font))
            {
                font = new MetricsFontInfo
                {
                    Family = GetString($"{key}.fontFamily"),
                    Size = GetDouble($"{key}.fontSize") * GetDouble($"{key}.fontScale", 1),
                    Weight = GetString($"{key}.fontWeight", "normal"),
                    Style = GetString($"{key}.fontStyle", "normal"),
                };
                fontCache[key] = font;
            }

            return new MetricsFontInfo
            {
                Family = font.Family,
                Size = font.Size,
                Weight = font.Weight,
                Style = font.Style,
            };
        }

        public static ElementStyle GetStyle(string key)
        {
            if (!styleCache.TryGetValue(key, out var style))
            {
                style = new ElementStyle
                {
                    FillStyle = Get($"{key}.fillStyle") as string,
                    StrokeStyle = Get($"{key}.strokeStyle") as string,
                    LineWidth = Get($"{key}.lineWidth") as double?,
                    LineDash = Get($"{key}.lineDash") as string,
                    ShadowBlur = Get($"{key}.shadowBlur") as double?,
                    ShadowColor = Get($"{key}.shadowColor") as string,
                };
                styleCache[key] = style;
            }

            return new ElementStyle
            {
                FillStyle = style.FillStyle,
                StrokeStyle = style.StrokeStyle,
                LineWidth = style.LineWidth,
                LineDash = style.LineDash,
                ShadowBlur = style.ShadowBlur,
                ShadowColor = style.ShadowColor,
            };
        }
    }
}

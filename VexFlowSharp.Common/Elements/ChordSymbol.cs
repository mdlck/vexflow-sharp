#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;
using System.Collections.Generic;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp
{
    public enum ChordSymbolHorizontalJustify
    {
        Left = 1,
        Center = 2,
        Right = 3,
        CenterStem = 4,
    }

    public enum ChordSymbolVerticalJustify
    {
        Top = 1,
        Bottom = 2,
    }

    public enum SymbolModifiers
    {
        None = 1,
        Subscript = 2,
        Superscript = 3,
    }

    public class ChordSymbolBlock : Element
    {
        private readonly MetricsFontInfo font;
        private readonly string text;
        private double xShift;
        private double yShift;

        public SymbolModifiers SymbolModifier { get; }
        public bool VAlign { get; set; }

        public ChordSymbolBlock(string text, SymbolModifiers symbolModifier, MetricsFontInfo font)
        {
            this.text = text;
            SymbolModifier = symbolModifier;
            this.font = font;
            boundingBox = new BoundingBox(0, 0, GetWidth(), font.Size);
        }

        public string GetText() => text;
        public double GetXShift() => xShift;
        public double GetYShift() => yShift;
        public ChordSymbolBlock SetXShift(double shift) { xShift = shift; return this; }
        public ChordSymbolBlock SetYShift(double shift) { yShift = shift; return this; }
        public bool IsSuperscript() => SymbolModifier == SymbolModifiers.Superscript;
        public bool IsSubscript() => SymbolModifier == SymbolModifiers.Subscript;

        public double GetWidth()
            => TextFormatter.Create(font.Family, font.Size).GetWidthForTextInPx(text);

        public void Draw(RenderContext ctx, double x, double y)
        {
            double drawX = x + xShift;
            double drawY = y + yShift;
            ctx.SetFont(font.Family, font.Size, font.Weight, font.Style);
            ctx.FillText(text, drawX, drawY);
            boundingBox = new BoundingBox(drawX, drawY - font.Size, GetWidth(), font.Size);
        }
    }

    /// <summary>
    /// Chord symbol modifier made from text/glyph blocks.
    /// Port of VexFlow 5's ChordSymbol class.
    /// </summary>
    public class ChordSymbol : Modifier
    {
        public new const string CATEGORY = "ChordSymbol";
        private const double TextHeightOffsetHack = 1.0;

        private static readonly Dictionary<string, ChordSymbolHorizontalJustify> horizontalStrings =
            new Dictionary<string, ChordSymbolHorizontalJustify>(StringComparer.Ordinal)
            {
                ["left"] = ChordSymbolHorizontalJustify.Left,
                ["right"] = ChordSymbolHorizontalJustify.Right,
                ["center"] = ChordSymbolHorizontalJustify.Center,
                ["centerStem"] = ChordSymbolHorizontalJustify.CenterStem,
            };

        private static readonly Dictionary<string, ChordSymbolVerticalJustify> verticalStrings =
            new Dictionary<string, ChordSymbolVerticalJustify>(StringComparer.Ordinal)
            {
                ["top"] = ChordSymbolVerticalJustify.Top,
                ["above"] = ChordSymbolVerticalJustify.Top,
                ["below"] = ChordSymbolVerticalJustify.Bottom,
                ["bottom"] = ChordSymbolVerticalJustify.Bottom,
            };

        private static readonly Dictionary<string, string> glyphs =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["diminished"] = "\ue870",
                ["dim"] = "\ue870",
                ["halfDiminished"] = "\ue871",
                ["+"] = "\ue872",
                ["augmented"] = "\ue872",
                ["majorSeventh"] = "\ue873",
                ["minor"] = "\ue874",
                ["-"] = "\ue874",
                ["("] = "(",
                ["leftParen"] = "(",
                [")"] = ")",
                ["rightParen"] = ")",
                ["leftBracket"] = "\ue878",
                ["rightBracket"] = "\ue879",
                ["leftParenTall"] = "(",
                ["rightParenTall"] = ")",
                ["/"] = "\ue87b",
                ["over"] = "\ue87b",
                ["#"] = "#",
                ["b"] = "\u266D",
            };

        private readonly List<ChordSymbolBlock> symbolBlocks = new List<ChordSymbolBlock>();
        private MetricsFontInfo font = Metrics.GetFontInfo("ChordSymbol");
        private ChordSymbolHorizontalJustify horizontal = ChordSymbolHorizontalJustify.Left;
        private ChordSymbolVerticalJustify vertical = ChordSymbolVerticalJustify.Top;
        private bool reportWidth = true;

        public override string GetCategory() => CATEGORY;

        public static double SuperSubRatio => Metrics.GetDouble("ChordSymbol.superSubRatio");
        public static double SpacingBetweenBlocks => Metrics.GetDouble("ChordSymbol.spacing");
        public static double SuperscriptOffset => Metrics.GetDouble("ChordSymbol.superscriptOffset");
        public static double SubscriptOffset => Metrics.GetDouble("ChordSymbol.subscriptOffset");
        public static double MinPadding => Metrics.GetDouble("NoteHead.minPadding");

        public IReadOnlyList<ChordSymbolBlock> GetSymbolBlocks() => symbolBlocks;
        public ChordSymbolHorizontalJustify GetHorizontal() => horizontal;
        public ChordSymbolVerticalJustify GetVertical() => vertical;
        public bool GetReportWidth() => reportWidth;

        private static double FontSizeToPixels(double sizeInPoints)
            => sizeInPoints * Metrics.GetDouble("TextFormatter.ptToPx");

        public ChordSymbol SetFontSize(double size)
        {
            font.Size = size;
            return this;
        }

        public ChordSymbol SetFont(string family, double size, string weight = "normal", string style = "normal")
        {
            font = new MetricsFontInfo { Family = family, Size = size, Weight = weight, Style = style };
            return this;
        }

        public ChordSymbol SetReportWidth(bool value)
        {
            reportWidth = value;
            return this;
        }

        public ChordSymbol SetVertical(ChordSymbolVerticalJustify justify)
        {
            vertical = justify;
            return this;
        }

        public ChordSymbol SetVertical(string justify)
        {
            if (verticalStrings.TryGetValue(justify, out var value)) vertical = value;
            return this;
        }

        public ChordSymbol SetHorizontal(ChordSymbolHorizontalJustify justify)
        {
            horizontal = justify;
            return this;
        }

        public ChordSymbol SetHorizontal(string justify)
        {
            if (horizontalStrings.TryGetValue(justify, out var value)) horizontal = value;
            return this;
        }

        public ChordSymbolBlock GetSymbolBlock(string text = "", SymbolModifiers symbolModifier = SymbolModifiers.None)
        {
            var blockFont = new MetricsFontInfo
            {
                Family = font.Family,
                Size = symbolModifier == SymbolModifiers.None ? font.Size : font.Size * SuperSubRatio,
                Weight = font.Weight,
                Style = font.Style,
            };
            var block = new ChordSymbolBlock(text, symbolModifier, blockFont);
            double fontSizeInPixels = FontSizeToPixels(font.Size);
            if (block.IsSubscript()) block.SetYShift(SubscriptOffset * fontSizeInPixels);
            if (block.IsSuperscript()) block.SetYShift(SuperscriptOffset * fontSizeInPixels);
            return block;
        }

        public ChordSymbol AddSymbolBlock(string text = "", SymbolModifiers symbolModifier = SymbolModifiers.None)
        {
            symbolBlocks.Add(GetSymbolBlock(text, symbolModifier));
            return this;
        }

        public ChordSymbol AddText(string text, SymbolModifiers symbolModifier = SymbolModifiers.None)
            => AddSymbolBlock(text, symbolModifier);

        public ChordSymbol AddTextSuperscript(string text)
            => AddSymbolBlock(text, SymbolModifiers.Superscript);

        public ChordSymbol AddTextSubscript(string text)
            => AddSymbolBlock(text, SymbolModifiers.Subscript);

        public ChordSymbol AddGlyph(string glyph, SymbolModifiers symbolModifier = SymbolModifiers.None)
            => AddText(glyphs.TryGetValue(glyph, out var value) ? value : glyph, symbolModifier);

        public ChordSymbol AddGlyphSuperscript(string glyph)
            => AddGlyph(glyph, SymbolModifiers.Superscript);

        public ChordSymbol AddGlyphOrText(string text, SymbolModifiers symbolModifier = SymbolModifiers.None)
        {
            string result = string.Empty;
            foreach (char c in text)
            {
                string key = c.ToString();
                result += glyphs.TryGetValue(key, out var value) ? value : key;
            }
            if (result.Length > 0)
                AddText(result, symbolModifier);
            return this;
        }

        public ChordSymbol AddLine(SymbolModifiers symbolModifier = SymbolModifiers.None)
            => AddText("\ue874\ue874", symbolModifier);

        public static bool Format(List<ChordSymbol> symbols, ModifierContextState state)
        {
            if (symbols == null || symbols.Count == 0) return false;

            double leftWidth = 0;
            double rightWidth = 0;
            double maxLeftGlyphWidth = 0;
            double maxRightGlyphWidth = 0;

            foreach (var symbol in symbols)
            {
                var note = symbol.GetNote() as Note;
                double width = 0;
                double lineSpaces = 1;

                for (int j = 0; j < symbol.symbolBlocks.Count; j++)
                {
                    var block = symbol.symbolBlocks[j];
                    bool sup = block.IsSuperscript();
                    bool sub = block.IsSubscript();
                    block.SetXShift(width);

                    if (sup || sub) lineSpaces = 2;

                    if (sub && j > 0)
                    {
                        var prev = symbol.symbolBlocks[j - 1];
                        if (prev.IsSuperscript())
                        {
                            block.SetXShift(width - prev.GetWidth() - MinPadding);
                            block.VAlign = true;
                            width += -prev.GetWidth() - MinPadding
                                + (prev.GetWidth() > block.GetWidth() ? prev.GetWidth() - block.GetWidth() : 0);
                        }
                    }

                    width += block.GetWidth() + MinPadding;
                }

                if (symbol.vertical == ChordSymbolVerticalJustify.Top)
                {
                    symbol.SetTextLine(state.TopTextLine);
                    state.TopTextLine += lineSpaces;
                }
                else
                {
                    symbol.SetTextLine(state.TextLine + 1);
                    state.TextLine += lineSpaces + 1;
                }

                if (symbol.reportWidth)
                {
                    double glyphWidth = note?.GetNoteMetrics().GlyphWidth ?? 0;
                    if (glyphWidth <= 0) glyphWidth = note?.GetWidth() ?? 0;

                    if (symbol.horizontal == ChordSymbolHorizontalJustify.Right)
                    {
                        maxLeftGlyphWidth = Math.Max(glyphWidth, maxLeftGlyphWidth);
                        leftWidth = Math.Max(leftWidth, width) + MinPadding;
                    }
                    else if (symbol.horizontal == ChordSymbolHorizontalJustify.Left)
                    {
                        maxRightGlyphWidth = Math.Max(glyphWidth, maxRightGlyphWidth);
                        rightWidth = Math.Max(rightWidth, width);
                    }
                    else
                    {
                        leftWidth = Math.Max(leftWidth, width / 2) + MinPadding;
                        rightWidth = Math.Max(rightWidth, width / 2);
                        maxLeftGlyphWidth = Math.Max(glyphWidth / 2, maxLeftGlyphWidth);
                        maxRightGlyphWidth = Math.Max(glyphWidth / 2, maxRightGlyphWidth);
                    }

                    symbol.SetWidth(width);
                }
            }

            double rightOverlap = Math.Min(Math.Max(rightWidth - maxRightGlyphWidth, 0), Math.Max(rightWidth - state.RightShift, 0));
            double leftOverlap = Math.Min(Math.Max(leftWidth - maxLeftGlyphWidth, 0), Math.Max(leftWidth - state.LeftShift, 0));
            state.LeftShift += leftOverlap;
            state.RightShift += rightOverlap;
            return true;
        }

        public override void Draw()
        {
            var ctx = CheckContext();
            var note = (Note)GetNote();
            var stave = note.CheckStave();
            rendered = true;

            var start = note.GetModifierStartXY(ModifierPosition.Above, GetIndex() ?? 0);
            double y;
            if (vertical == ChordSymbolVerticalJustify.Bottom)
            {
                y = stave.GetYForBottomText(textLine + TextHeightOffsetHack);
            }
            else
            {
                double topY = note.GetYs().Length > 0 ? Math.Min(note.GetYs()[0], note.GetYs()[note.GetYs().Length - 1]) : 0;
                y = Math.Min(stave.GetYForTopText(textLine), topY - 10);
            }

            double x = start.X;
            if (horizontal == ChordSymbolHorizontalJustify.Right)
                x = start.X + GetWidth();
            else if (horizontal == ChordSymbolHorizontalJustify.Center)
                x = start.X - GetWidth() / 2;
            else if (horizontal == ChordSymbolHorizontalJustify.CenterStem && note is StemmableNote stemmable)
                x = stemmable.GetStemX() - GetWidth() / 2;

            ctx.OpenGroup("chordsymbol", GetId());
            foreach (var block in symbolBlocks)
                block.Draw(ctx, x, y);
            ctx.CloseGroup();
        }

        public override BoundingBox? GetBoundingBox()
        {
            if (symbolBlocks.Count == 0) return null;
            var first = symbolBlocks[0].GetBoundingBox();
            if (first == null) return null;
            var result = new BoundingBox(first.X, first.Y, first.W, first.H);
            for (int i = 1; i < symbolBlocks.Count; i++)
            {
                var box = symbolBlocks[i].GetBoundingBox();
                if (box != null) result.MergeWith(box);
            }
            return result;
        }
    }
}

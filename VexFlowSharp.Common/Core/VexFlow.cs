#nullable enable annotations

// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System;

namespace VexFlowSharp
{
    /// <summary>
    /// Build metadata exposed by the VexFlow facade. Mirrors VexFlow.BUILD from vexflow.ts.
    /// </summary>
    public class VexFlowBuild
    {
        public string VERSION { get; set; } = VexFlow.VERSION;
        public string ID { get; set; } = VexFlow.ID;
        public string DATE { get; set; } = VexFlow.DATE;
        public string INFO { get; set; } = string.Empty;
    }

    /// <summary>
    /// Portable VexFlow package facade for C# consumers.
    /// Browser renderer/bootstrap exports remain intentionally out of scope for VexFlowSharp.
    /// </summary>
    public static class VexFlow
    {
        public const string VERSION = "5.0.0";
        public const string ID = "";
        public const string DATE = "";

        public static VexFlowBuild BUILD { get; } = new VexFlowBuild();

        public static void SetFonts(params string[] fontNames)
        {
            if (fontNames == null || fontNames.Length == 0)
                Metrics.SetFontFamily("Bravura,Academico");
            else
                Metrics.SetFontFamily(string.Join(",", fontNames));
        }

        public static string[] GetFonts()
            => Metrics.GetFontFamily().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        public static int RENDER_PRECISION_PLACES => Tables.RENDER_PRECISION_PLACES;
        public static double SOFTMAX_FACTOR => Tables.SOFTMAX_FACTOR;
        public static double NOTATION_FONT_SCALE => Tables.NOTATION_FONT_SCALE;
        public static int RESOLUTION => Tables.RESOLUTION;
        public static double SLASH_NOTEHEAD_WIDTH => Tables.SLASH_NOTEHEAD_WIDTH;
        public static double STAVE_LINE_DISTANCE => Tables.STAVE_LINE_DISTANCE;
        public static double STAVE_LINE_THICKNESS => Tables.STAVE_LINE_THICKNESS;
        public static double STEM_HEIGHT => Tables.STEM_HEIGHT;
        public static double STEM_WIDTH => Tables.STEM_WIDTH;

        public static System.Collections.Generic.List<(string Type, double Line)> KeySignature(string spec)
            => Tables.KeySignature(spec);
    }
}

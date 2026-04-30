// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Reflection;

namespace VexFlowSharp
{
    /// <summary>
    /// Category helpers mirroring VexFlow's typeguard.ts without relying on JavaScript instanceof semantics.
    /// </summary>
    public static class TypeGuards
    {
        public static bool IsCategory(object obj, string category, bool checkAncestors = true)
        {
            if (obj == null) return false;

            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField("CATEGORY", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                if (field.GetValue(null) is string value && value == category)
                    return true;

                if (!checkAncestors) return false;
                type = type.BaseType;
            }

            return false;
        }

        public static bool IsAccidental(object obj) => IsCategory(obj, Accidental.CATEGORY);
        public static bool IsAnnotation(object obj) => IsCategory(obj, Annotation.CATEGORY);
        public static bool IsBarline(object obj) => IsCategory(obj, Barline.CATEGORY);
        public static bool IsDot(object obj) => IsCategory(obj, Dot.CATEGORY);
        public static bool IsGraceNote(object obj) => IsCategory(obj, GraceNote.CATEGORY);
        public static bool IsGraceNoteGroup(object obj) => IsCategory(obj, GraceNoteGroup.CATEGORY);
        public static bool IsNote(object obj) => IsCategory(obj, Note.CATEGORY);
        public static bool IsModifier(object obj) => IsCategory(obj, Modifier.CATEGORY);
        public static bool IsRenderContext(object obj) => IsCategory(obj, RenderContext.CATEGORY);
        public static bool IsStaveNote(object obj) => IsCategory(obj, StaveNote.CATEGORY);
        public static bool IsStemmableNote(object obj) => IsCategory(obj, StemmableNote.CATEGORY);
        public static bool IsTabNote(object obj) => IsCategory(obj, TabNote.CATEGORY);
        public static bool IsTickable(object obj) => IsCategory(obj, Tickable.CATEGORY);
    }
}

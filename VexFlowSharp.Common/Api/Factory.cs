// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's Factory class (factory.ts, 704 lines).
// Factory is the primary developer-facing API for constructing scores.
// It wraps all lower-level objects (Stave, Voice, StaveNote, Beam, etc.) into a
// unified builder that handles context injection and draw ordering.
//
// Draw order (must match VexFlow exactly):
//   1. systems.Format()   — sets note x-positions
//   2. staves.Draw()      — staff lines, clef, key, time
//   3. voices.Draw()      — notes, rests
//   4. renderQ.Draw()     — beams, ties, curves, dynamics, etc.
//   5. systems.Draw()     — connectors / debug
//   6. Reset()

using System.Collections.Generic;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Common.Formatting;

namespace VexFlowSharp.Api
{
    /// <summary>
    /// Options for <see cref="Factory.Beam(List{StemmableNote}, FactoryBeamOptions)"/>.
    /// Mirrors VexFlow 5 Factory.Beam options while keeping C# property names.
    /// </summary>
    public class FactoryBeamOptions
    {
        public bool AutoStem { get; set; } = false;
        public List<int> SecondaryBeamBreaks { get; set; }
        public Dictionary<int, PartialBeamDirection> PartialBeamDirections { get; set; }
    }

    /// <summary>
    /// Params object for <see cref="Factory.Beam(FactoryBeamParams)"/>.
    /// Mirrors VexFlow 5 Factory.Beam(params) while keeping C# property names.
    /// </summary>
    public class FactoryBeamParams
    {
        public List<StemmableNote> Notes { get; set; } = new List<StemmableNote>();
        public FactoryBeamOptions Options { get; set; }
    }

    public class FactoryTupletParams
    {
        public List<Note> Notes { get; set; } = new List<Note>();
        public TupletOptions Options { get; set; }
    }

    public class FactoryCurveParams
    {
        public Note From { get; set; }
        public Note To { get; set; }
        public CurveOptions Options { get; set; }
    }

    public class FactoryVoiceOptions
    {
        public VoiceTime? Time { get; set; }
        public string TimeString { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.TextDynamics(FactoryTextDynamicsOptions)"/>.
    /// Mirrors VexFlow 5-style params while keeping C# property names.
    /// </summary>
    public class FactoryTextDynamicsOptions
    {
        public string Text { get; set; }
        public string Dynamics { get; set; } = "p";
        public string Duration { get; set; } = "q";
        public int Dots { get; set; } = 0;
        public double? Line { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.Stave(FactoryStaveOptions)"/>.
    /// Mirrors VexFlow 5 Factory.Stave params while keeping C# property names.
    /// </summary>
    public class FactoryStaveOptions
    {
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double? Width { get; set; }
        public StaveOptions Options { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.TabStave(FactoryTabStaveOptions)"/>.
    /// Mirrors VexFlow 5 Factory.TabStave params while keeping C# property names.
    /// </summary>
    public class FactoryTabStaveOptions : FactoryStaveOptions
    {
    }

    /// <summary>
    /// Options for <see cref="Factory.TextNote(TextNoteStruct)"/>.
    /// Mirrors VexFlow 5 TextNoteStruct while keeping C# property names.
    /// </summary>
    public class FactoryTextNoteOptions : TextNoteStruct
    {
    }

    /// <summary>
    /// Options for <see cref="Factory.StaveLine(FactoryStaveLineOptions)"/>.
    /// Mirrors VexFlow 5 Factory.StaveLine params while keeping C# property names.
    /// </summary>
    public class FactoryStaveLineOptions : StaveLineParams
    {
    }

    /// <summary>
    /// Options for <see cref="Factory.Crescendo(FactoryCrescendoOptions)"/>.
    /// Mirrors VexFlow 5-style params while keeping C# property names.
    /// </summary>
    public class FactoryCrescendoOptions
    {
        public NoteStruct NoteStruct { get; set; } = new NoteStruct { Duration = "q" };
        public bool Decrescendo { get; set; } = false;
        public double? Height { get; set; }
        public double? Line { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.Accidental(FactoryAccidentalOptions)"/>.
    /// Mirrors VexFlow 5-style params while keeping C# property names.
    /// </summary>
    public class FactoryAccidentalOptions
    {
        public string Type { get; set; } = "#";
        public bool Cautionary { get; set; } = false;
    }

    /// <summary>
    /// Options for <see cref="Factory.Vibrato(FactoryVibratoOptions)"/>.
    /// Mirrors VexFlow 5-style params while keeping C# property names.
    /// </summary>
    public class FactoryVibratoOptions
    {
        public int? Code { get; set; }
        public double? VibratoWidth { get; set; }
        public bool? Harsh { get; set; }
        public VibratoRenderOptions RenderOptions { get; set; }
    }

    public class FactoryBarNoteOptions
    {
        public BarlineType? Type { get; set; }
        public string TypeString { get; set; }
    }

    public class FactoryClefNoteNestedOptions
    {
        public string Size { get; set; } = "default";
        public string Annotation { get; set; }
    }

    public class FactoryClefNoteOptions
    {
        public string Type { get; set; } = "treble";
        public FactoryClefNoteNestedOptions Options { get; set; }
    }

    public class FactoryTimeSigNoteOptions
    {
        public string Time { get; set; } = "4/4";
        public double? CustomPadding { get; set; }
    }

    public class FactoryKeySigNoteOptions
    {
        public string Key { get; set; } = "C";
        public string CancelKey { get; set; }
        public string[] AlterKey { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.Annotation(FactoryAnnotationOptions)"/>.
    /// Mirrors VexFlow 5 Factory.Annotation params while keeping C# property names.
    /// </summary>
    public class FactoryAnnotationOptions
    {
        public string Text { get; set; } = "p";
        public AnnotationHorizontalJustify? HJustify { get; set; }
        public AnnotationVerticalJustify? VJustify { get; set; }
        public string HJustifyString { get; set; }
        public string VJustifyString { get; set; }
        public MetricsFontInfo Font { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.Articulation(FactoryArticulationOptions)"/>.
    /// Mirrors VexFlow 5 Factory.Articulation params while keeping C# property names.
    /// </summary>
    public class FactoryArticulationOptions
    {
        public string Type { get; set; } = "a.";
        public ModifierPosition? Position { get; set; }
        public bool? BetweenLines { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.Fingering(FactoryFingeringOptions)"/>.
    /// Mirrors VexFlow 5 Factory.Fingering params while keeping C# property names.
    /// </summary>
    public class FactoryFingeringOptions
    {
        public string Number { get; set; } = "";
        public ModifierPosition? Position { get; set; }
        public double? OffsetX { get; set; }
        public double? OffsetY { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.StringNumber(FactoryStringNumberOptions)"/>.
    /// Mirrors VexFlow 5 Factory.StringNumber params while keeping C# property names.
    /// </summary>
    public class FactoryStringNumberOptions
    {
        public string Number { get; set; } = "";
        public ModifierPosition Position { get; set; } = ModifierPosition.Above;
        public bool DrawCircle { get; set; } = true;
        public double? OffsetX { get; set; }
        public double? OffsetY { get; set; }
        public double? StemOffset { get; set; }
        public bool? Dashed { get; set; }
        public RendererLineEndType? LineEndType { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.Ornament(FactoryOrnamentOptions)"/>.
    /// Mirrors VexFlow 5 Factory.Ornament params while keeping C# property names.
    /// </summary>
    public class FactoryOrnamentOptions
    {
        public string Type { get; set; } = "tr";
        public ModifierPosition? Position { get; set; }
        public string UpperAccidental { get; set; }
        public string LowerAccidental { get; set; }
        public bool? Delayed { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.PedalMarking(FactoryPedalMarkingOptions)"/>.
    /// Mirrors VexFlow 5 Factory.PedalMarking params while keeping C# property names.
    /// </summary>
    public class FactoryPedalMarkingOptions
    {
        public List<StaveNote> Notes { get; set; }
        public string Style { get; set; } = "mixed";
        public string DepressText { get; set; }
        public string ReleaseText { get; set; }
        public int? Line { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.ChordSymbol(FactoryChordSymbolOptions)"/>.
    /// Mirrors VexFlow 5 Factory.ChordSymbol params while keeping C# property names.
    /// </summary>
    public class FactoryChordSymbolOptions
    {
        public double? FontSize { get; set; }
        public string FontFamily { get; set; }
        public string FontWeight { get; set; }
        public string FontStyle { get; set; }
        public ChordSymbolHorizontalJustify? HJustify { get; set; }
        public ChordSymbolVerticalJustify? VJustify { get; set; }
        public string HJustifyString { get; set; }
        public string VJustifyString { get; set; }
        public bool? ReportWidth { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.TextBracket(FactoryTextBracketOptions)"/>.
    /// Mirrors VexFlow 5 Factory.TextBracket params while keeping C# property names.
    /// </summary>
    public class FactoryTextBracketOptions
    {
        public Note Start { get; set; } = null;
        public Note Stop { get; set; } = null;
        public string Text { get; set; } = "";
        public string Superscript { get; set; } = "";
        public TextBracketPosition Position { get; set; } = TextBracketPosition.Top;
        public double? Line { get; set; }
        public bool? Dashed { get; set; }
        public double[] Dash { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.StaveTie(FactoryStaveTieOptions)"/>.
    /// Mirrors VexFlow 5 Factory.StaveTie params while keeping C# property names.
    /// </summary>
    public class FactoryStaveTieOptions
    {
        public TieNotes Notes { get; set; } = null;
        public string Text { get; set; }
        public int? Direction { get; set; }
        public StaveTieRenderOptions RenderOptions { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.VibratoBracket(FactoryVibratoBracketOptions)"/>.
    /// Mirrors VexFlow 5-style params while keeping C# property names.
    /// </summary>
    public class FactoryVibratoBracketOptions
    {
        public Note From { get; set; }
        public Note To { get; set; }
        public int? Line { get; set; }
        public int? Code { get; set; }
        public bool? Harsh { get; set; }
        public double? VibratoWidth { get; set; }
    }

    public class FactoryStaveConnectorTextOptions
    {
        public string Text { get; set; } = "";
        public double ShiftX { get; set; } = 0;
        public double ShiftY { get; set; } = 0;
    }

    /// <summary>
    /// Options for <see cref="Factory.StaveConnector(FactoryStaveConnectorOptions)"/>.
    /// Mirrors VexFlow 5-style params while keeping C# property names.
    /// </summary>
    public class FactoryStaveConnectorOptions
    {
        public Stave TopStave { get; set; } = null;
        public Stave BottomStave { get; set; } = null;
        public StaveConnectorType? Type { get; set; }
        public string TypeString { get; set; }
        public double? XShift { get; set; }
        public List<FactoryStaveConnectorTextOptions> Texts { get; set; }
    }

    /// <summary>
    /// Options for <see cref="Factory.GraceNoteGroup(FactoryGraceNoteGroupOptions)"/>.
    /// Mirrors VexFlow 5-style params while keeping C# property names.
    /// </summary>
    public class FactoryGraceNoteGroupOptions
    {
        public List<GraceNote> Notes { get; set; } = new List<GraceNote>();
        public bool Slur { get; set; } = false;
    }

    /// <summary>
    /// Options for <see cref="Factory.NoteSubGroup(FactoryNoteSubGroupOptions)"/>.
    /// Mirrors VexFlow 5-style params while keeping C# property names.
    /// </summary>
    public class FactoryNoteSubGroupOptions
    {
        public List<Note> Notes { get; set; } = new List<Note>();
    }

    /// <summary>
    /// High-level builder API for VexFlowSharp score construction.
    /// Wraps all lower-level objects into a unified builder that handles
    /// context injection and 5-step draw ordering.
    ///
    /// Port of VexFlow's Factory class from factory.ts.
    /// </summary>
    public class Factory
    {
        private const double DefaultStaveSpace = 10.0;

        // ── Fields ─────────────────────────────────────────────────────────────

        private RenderContext context;
        private readonly double width;
        private readonly double height;

        private List<Stave> staves = new List<Stave>();
        private List<Voice> voices = new List<Voice>();
        private List<Element> renderQ = new List<Element>();
        private List<VexFlowSharp.Common.Formatting.System> systems =
            new List<VexFlowSharp.Common.Formatting.System>();

        private Stave currentStave;

        // ── Constructor ────────────────────────────────────────────────────────

        /// <summary>
        /// Create a Factory with a pre-existing render context.
        /// </summary>
        /// <param name="ctx">The render context to draw into.</param>
        /// <param name="width">Canvas width in pixels.</param>
        /// <param name="height">Canvas height in pixels.</param>
        public Factory(RenderContext ctx, double width, double height)
        {
            this.context = ctx;
            this.width   = width;
            this.height  = height;
        }

        // ── Accessors ─────────────────────────────────────────────────────────

        /// <summary>Get the render context.</summary>
        public RenderContext GetContext() => context;

        /// <summary>Set the render context used for subsequently created and drawn elements.</summary>
        public Factory SetContext(RenderContext ctx)
        {
            context = ctx;
            return this;
        }

        /// <summary>Get the current (most recently created) stave.</summary>
        public Stave GetStave() => currentStave;

        /// <summary>Get all voices in this factory.</summary>
        public List<Voice> GetVoices() => voices;

        // ── Reset ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Clear all builder state: staves, voices, renderQ, systems.
        /// Called automatically at the end of Draw().
        ///
        /// Port of VexFlow's Factory.reset() from factory.ts.
        /// </summary>
        public void Reset()
        {
            staves       = new List<Stave>();
            voices       = new List<Voice>();
            renderQ      = new List<Element>();
            systems      = new List<VexFlowSharp.Common.Formatting.System>();
            currentStave = null;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Render the score in the correct 5-step VexFlow draw order, then Reset().
        ///
        /// Order (must match VexFlow factory.ts draw() exactly):
        ///   1. systems.Format()   — sets note x-positions across all staves
        ///   2. staves.Draw()      — staff lines, clef, key signature, time signature
        ///   3. voices.Draw()      — notes, rests
        ///   4. renderQ.Draw()     — beams, ties, curves, dynamics, etc.
        ///   5. systems.Draw()     — connector rendering
        ///   6. Reset()            — clear builder state for next use
        ///
        /// Port of VexFlow's Factory.draw() from factory.ts.
        /// </summary>
        public void Draw()
        {
            // 1. Format all systems (sets note x-positions)
            foreach (var s in systems)
            {
                s.SetContext(context);
                s.Format();
            }

            // 2. Draw staves (staff lines, clef, key, time)
            // Staves created via Factory.Stave() are in `staves`.
            // Staves created via System.AddStave() live inside the system — draw those too.
            foreach (var s in staves)
            {
                s.SetContext(context);
                s.Draw();
            }
            foreach (var sys in systems)
            {
                foreach (var s in sys.GetStaves())
                {
                    s.SetContext(context);
                    s.Draw();
                }
            }

            // 3. Draw voices (notes, rests)
            foreach (var v in voices)
            {
                v.Draw(context);
            }

            // 4. Draw renderQ elements (beams, ties, curves, dynamics, etc.)
            foreach (var e in renderQ)
            {
                if (!e.IsRendered())
                {
                    e.SetContext(context);
                    e.Draw();
                }
            }

            // 5. Draw systems (connectors)
            foreach (var s in systems)
            {
                s.SetContext(context);
                s.Draw();
            }

            // 6. Reset for next use
            Reset();
        }

        // ── System ────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a System and inject factory reference into it.
        /// System is pushed to the systems list for format/draw.
        ///
        /// Port of VexFlow's Factory.System() from factory.ts.
        /// </summary>
        public VexFlowSharp.Common.Formatting.System System(SystemOptions options = null)
        {
            var system = new VexFlowSharp.Common.Formatting.System(options);
            system.SetContext(context);
            systems.Add(system);
            return system;
        }

        // ── Stave ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a Stave and set it as the current stave.
        /// The stave is added to the staves list for drawing.
        ///
        /// Port of VexFlow's Factory.Stave() from factory.ts.
        /// </summary>
        public Stave Stave(double x = 0, double y = 0, double? w = null, StaveOptions options = null)
        {
            options ??= new StaveOptions { SpacingBetweenLinesPx = DefaultStaveSpace };
            var stave = new Stave(x, y, w ?? width - DefaultStaveSpace, options);
            stave.SetContext(context);
            staves.Add(stave);
            currentStave = stave;
            return stave;
        }

        /// <summary>Create a Stave from a VexFlow 5-style params object.</summary>
        public Stave Stave(FactoryStaveOptions options)
        {
            options ??= new FactoryStaveOptions();
            return Stave(options.X, options.Y, options.Width, options.Options);
        }

        /// <summary>
        /// Create a TabStave and set it as the current stave.
        /// Port of VexFlow's Factory.TabStave() from factory.ts.
        /// </summary>
        public TabStave TabStave(double x = 0, double y = 0, double? w = null, StaveOptions options = null)
        {
            options ??= new StaveOptions { SpacingBetweenLinesPx = DefaultStaveSpace * 1.3 };
            var stave = new TabStave(x, y, w ?? width - DefaultStaveSpace, options);
            stave.SetContext(context);
            staves.Add(stave);
            currentStave = stave;
            return stave;
        }

        /// <summary>Create a TabStave from a VexFlow 5-style params object.</summary>
        public TabStave TabStave(FactoryTabStaveOptions options)
        {
            options ??= new FactoryTabStaveOptions();
            return TabStave(options.X, options.Y, options.Width, options.Options);
        }

        // ── StaveNote ─────────────────────────────────────────────────────────

        /// <summary>
        /// Create a StaveNote, set its stave to the current stave,
        /// inject context, and add to renderQ.
        ///
        /// Port of VexFlow's Factory.StaveNote() from factory.ts.
        /// </summary>
        public StaveNote StaveNote(StaveNoteStruct noteStruct)
        {
            var note = new StaveNote(noteStruct);
            if (currentStave != null) note.SetStave(currentStave);
            note.SetContext(context);
            renderQ.Add(note);
            return note;
        }

        /// <summary>
        /// Create a TabNote, set its stave to the current stave, inject context, and add to renderQ.
        /// Port of VexFlow's Factory.TabNote() from factory.ts.
        /// </summary>
        public TabNote TabNote(TabNoteStruct noteStruct)
        {
            var note = new TabNote(noteStruct);
            if (currentStave != null) note.SetStave(currentStave);
            note.SetContext(context);
            renderQ.Add(note);
            return note;
        }

        /// <summary>Create a GlyphNote, set its stave/context, and add to renderQ.</summary>
        public GlyphNote GlyphNote(string glyph, NoteStruct noteStruct, GlyphNoteOptions options = null)
        {
            var note = new GlyphNote(glyph, noteStruct, options);
            if (currentStave != null) note.SetStave(currentStave);
            note.SetContext(context);
            renderQ.Add(note);
            return note;
        }

        /// <summary>Create a RepeatNote, set its stave/context, and add to renderQ.</summary>
        public RepeatNote RepeatNote(string type, NoteStruct noteStruct = null, GlyphNoteOptions options = null)
        {
            var note = new RepeatNote(type, noteStruct, options);
            if (currentStave != null) note.SetStave(currentStave);
            note.SetContext(context);
            renderQ.Add(note);
            return note;
        }

        /// <summary>Create a BarNote, set its stave/context, and add to renderQ.</summary>
        public BarNote BarNote(BarlineType type = BarlineType.Single)
        {
            var note = new BarNote(type);
            if (currentStave != null) note.SetStave(currentStave);
            note.SetContext(context);
            renderQ.Add(note);
            return note;
        }

        /// <summary>Create a BarNote from a VexFlow 5-style params object and add to renderQ.</summary>
        public BarNote BarNote(FactoryBarNoteOptions options)
        {
            options ??= new FactoryBarNoteOptions();
            var type = options.Type ?? ParseBarlineType(options.TypeString) ?? BarlineType.Single;
            return BarNote(type);
        }

        /// <summary>Create a ClefNote, set its stave/context, and add to renderQ.</summary>
        public ClefNote ClefNote(string type = "treble", string size = "default", string annotation = null)
        {
            var note = new ClefNote(type, size, annotation);
            if (currentStave != null) note.SetStave(currentStave);
            note.SetContext(context);
            renderQ.Add(note);
            return note;
        }

        /// <summary>Create a ClefNote from a VexFlow 5-style params object and add to renderQ.</summary>
        public ClefNote ClefNote(FactoryClefNoteOptions options)
        {
            options ??= new FactoryClefNoteOptions();
            return ClefNote(
                options.Type,
                options.Options.Size ?? "default",
                options.Options.Annotation);
        }

        /// <summary>Create a TimeSigNote, set its stave/context, and add to renderQ.</summary>
        public TimeSigNote TimeSigNote(string time = "4/4")
        {
            var note = new TimeSigNote(time);
            if (currentStave != null) note.SetStave(currentStave);
            note.SetContext(context);
            renderQ.Add(note);
            return note;
        }

        /// <summary>Create a TimeSigNote from a VexFlow 5-style params object and add to renderQ.</summary>
        public TimeSigNote TimeSigNote(FactoryTimeSigNoteOptions options)
        {
            options ??= new FactoryTimeSigNoteOptions();
            var note = new TimeSigNote(options.Time, options.CustomPadding ?? 15);
            if (currentStave != null) note.SetStave(currentStave);
            note.SetContext(context);
            renderQ.Add(note);
            return note;
        }

        /// <summary>Create a KeySigNote, set its stave/context, and add to renderQ.</summary>
        public KeySigNote KeySigNote(string key, string cancelKey = null, string[] alterKey = null)
        {
            var note = new KeySigNote(key, cancelKey, alterKey);
            if (currentStave != null) note.SetStave(currentStave);
            note.SetContext(context);
            renderQ.Add(note);
            return note;
        }

        /// <summary>Create a KeySigNote from a VexFlow 5-style params object and add to renderQ.</summary>
        public KeySigNote KeySigNote(FactoryKeySigNoteOptions options)
        {
            return KeySigNote(options.Key, options.CancelKey, options.AlterKey);
        }

        private static BarlineType? ParseBarlineType(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return null;
            return type.Trim() switch
            {
                "single" => BarlineType.Single,
                "double" => BarlineType.Double,
                "end" => BarlineType.End,
                "repeatBegin" => BarlineType.RepeatBegin,
                "repeatEnd" => BarlineType.RepeatEnd,
                "repeatBoth" => BarlineType.RepeatBoth,
                "none" => BarlineType.None,
                _ => global::System.Enum.TryParse<BarlineType>(type, ignoreCase: true, out var parsed) ? parsed : null,
            };
        }

        // ── GhostNote ─────────────────────────────────────────────────────────

        /// <summary>
        /// Create a GhostNote from a duration string.
        /// Adds to renderQ.
        ///
        /// Port of VexFlow's Factory.GhostNote() from factory.ts.
        /// </summary>
        public GhostNote GhostNote(string duration)
        {
            var ghostNote = new GhostNote(duration);
            if (currentStave != null) ghostNote.SetStave(currentStave);
            ghostNote.SetContext(context);
            renderQ.Add(ghostNote);
            return ghostNote;
        }

        /// <summary>
        /// Create a GhostNote from a NoteStruct.
        /// Adds to renderQ.
        /// </summary>
        public GhostNote GhostNote(NoteStruct noteStruct)
        {
            var ghostNote = new GhostNote(noteStruct);
            if (currentStave != null) ghostNote.SetStave(currentStave);
            ghostNote.SetContext(context);
            renderQ.Add(ghostNote);
            return ghostNote;
        }

        // ── TextDynamics ──────────────────────────────────────────────────────

        /// <summary>
        /// Create a TextDynamics element (p, mf, f, etc.) and add to renderQ.
        ///
        /// Port of VexFlow's Factory.TextDynamics() from factory.ts.
        /// </summary>
        public TextDynamics TextDynamics(string dynamics = "p", string duration = "q")
        {
            var noteStruct = new NoteStruct { Duration = duration };
            var text = new TextDynamics(noteStruct, dynamics);
            if (currentStave != null) text.SetStave(currentStave);
            text.SetContext(context);
            renderQ.Add(text);
            return text;
        }

        /// <summary>Create a TextDynamics note from a VexFlow 5-style params object and add to renderQ.</summary>
        public TextDynamics TextDynamics(FactoryTextDynamicsOptions options)
        {
            options ??= new FactoryTextDynamicsOptions();
            var sequence = options.Text ?? options.Dynamics;
            var text = new TextDynamics(new NoteStruct { Duration = options.Duration, Dots = options.Dots }, sequence);
            if (options.Line.HasValue) text.SetLine(options.Line.Value);
            if (currentStave != null) text.SetStave(currentStave);
            text.SetContext(context);
            renderQ.Add(text);
            return text;
        }

        /// <summary>Create a TextNote, set its stave/context, and add to renderQ.</summary>
        public TextNote TextNote(TextNoteStruct noteStruct)
        {
            var note = new TextNote(noteStruct);
            if (currentStave != null) note.SetStave(currentStave);
            note.SetContext(context);
            renderQ.Add(note);
            return note;
        }

        /// <summary>Create a TextNote from a VexFlow 5-style params object and add to renderQ.</summary>
        public TextNote TextNote(FactoryTextNoteOptions options)
        {
            return TextNote((TextNoteStruct)options);
        }

        // ── Crescendo ─────────────────────────────────────────────────────────

        /// <summary>
        /// Create a Crescendo (hairpin) and add to renderQ.
        ///
        /// Port of VexFlow's Factory's crescendo usage (crescendo.ts).
        /// </summary>
        public Crescendo Crescendo(NoteStruct noteStruct, bool decrescendo = false)
        {
            var crescendo = new Crescendo(noteStruct);
            crescendo.SetDecrescendo(decrescendo);
            if (currentStave != null) crescendo.SetStave(currentStave);
            crescendo.SetContext(context);
            renderQ.Add(crescendo);
            return crescendo;
        }

        /// <summary>Create a Crescendo from a VexFlow 5-style params object and add to renderQ.</summary>
        public Crescendo Crescendo(FactoryCrescendoOptions options)
        {
            options ??= new FactoryCrescendoOptions();
            var crescendo = new Crescendo(options.NoteStruct);
            crescendo.SetDecrescendo(options.Decrescendo);
            if (options.Height.HasValue) crescendo.SetHeight(options.Height.Value);
            if (options.Line.HasValue) crescendo.SetLine(options.Line.Value);
            if (currentStave != null) crescendo.SetStave(currentStave);
            crescendo.SetContext(context);
            renderQ.Add(crescendo);
            return crescendo;
        }

        // ── Beam ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a Beam over the given StemmableNotes and add to renderQ.
        ///
        /// Port of VexFlow's Factory.Beam() from factory.ts.
        /// </summary>
        public Beam Beam(List<StemmableNote> notes, bool autoStem = false)
            => Beam(notes, new FactoryBeamOptions { AutoStem = autoStem });

        /// <summary>
        /// Create a Beam over the given StemmableNotes with VexFlow 5-style factory options and add to renderQ.
        /// </summary>
        public Beam Beam(List<StemmableNote> notes, FactoryBeamOptions options)
        {
            options ??= new FactoryBeamOptions();
            var beam = new Beam(notes, options.AutoStem);
            beam.BreakSecondaryAt(options.SecondaryBeamBreaks ?? new List<int>());
            if (options.PartialBeamDirections != null)
            {
                foreach (var pair in options.PartialBeamDirections)
                    beam.SetPartialBeamSideAt(pair.Key, pair.Value);
            }
            beam.SetContext(context);
            renderQ.Add(beam);
            return beam;
        }

        /// <summary>Create a Beam from a VexFlow 5-style params object and add to renderQ.</summary>
        public Beam Beam(FactoryBeamParams parameters)
        {
            return Beam(parameters.Notes, parameters.Options);
        }

        // ── Tuplet ────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a Tuplet over the given notes and add to renderQ.
        ///
        /// Port of VexFlow's Factory.Tuplet() from factory.ts.
        /// </summary>
        public Tuplet Tuplet(List<Note> notes, TupletOptions options = null)
        {
            var tuplet = new Tuplet(notes, options ?? new TupletOptions());
            tuplet.SetContext(context);
            renderQ.Add(tuplet);
            return tuplet;
        }

        /// <summary>Create a Tuplet from a VexFlow 5-style params object and add to renderQ.</summary>
        public Tuplet Tuplet(FactoryTupletParams parameters)
        {
            return Tuplet(parameters.Notes, parameters.Options);
        }

        // ── Curve ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a Curve (bezier slur) and add to renderQ.
        ///
        /// Port of VexFlow's Factory.Curve() from factory.ts.
        /// </summary>
        public Curve Curve(Note from, Note to, CurveOptions options = null)
        {
            var curve = new Curve(from, to, options ?? new CurveOptions());
            curve.SetContext(context);
            renderQ.Add(curve);
            return curve;
        }

        /// <summary>Create a Curve from a VexFlow 5-style params object and add to renderQ.</summary>
        public Curve Curve(FactoryCurveParams parameters)
        {
            return Curve(parameters.From, parameters.To, parameters.Options);
        }

        /// <summary>Create a StaveLine and add to renderQ.</summary>
        public StaveLine StaveLine(FactoryStaveLineOptions options)
        {
            var line = new StaveLine(options);
            line.SetContext(context);
            renderQ.Add(line);
            return line;
        }

        // ── StaveTie ──────────────────────────────────────────────────────────

        /// <summary>
        /// Create a StaveTie and add to renderQ.
        ///
        /// Port of VexFlow's Factory.StaveTie() from factory.ts.
        /// </summary>
        public StaveTie StaveTie(TieNotes tieNotes, string text = null, int? direction = null)
        {
            var tie = new StaveTie(tieNotes, text);
            if (direction.HasValue) tie.SetDirection(direction.Value);
            tie.SetContext(context);
            renderQ.Add(tie);
            return tie;
        }

        /// <summary>Create a StaveTie from a VexFlow 5-style params object and add to renderQ.</summary>
        public StaveTie StaveTie(FactoryStaveTieOptions options)
        {
            var tie = new StaveTie(options.Notes, options.Text, options.RenderOptions);
            if (options.Direction.HasValue) tie.SetDirection(options.Direction.Value);
            tie.SetContext(context);
            renderQ.Add(tie);
            return tie;
        }

        // ── VibratoBracket ────────────────────────────────────────────────────

        /// <summary>
        /// Create a VibratoBracket and add to renderQ.
        ///
        /// Port of VexFlow's Factory.VibratoBracket() from factory.ts.
        /// </summary>
        public VibratoBracket VibratoBracket(Note from, Note to, int? line = null, int? code = null, bool? harsh = null)
        {
            var bracket = new VibratoBracket(from, to);
            if (line.HasValue)  bracket.SetLine(line.Value);
            if (code.HasValue)  bracket.SetVibratoCode(code.Value);
            if (harsh.HasValue) bracket.SetHarsh(harsh.Value);
            bracket.SetContext(context);
            renderQ.Add(bracket);
            return bracket;
        }

        /// <summary>Create a VibratoBracket from a VexFlow 5-style params object and add to renderQ.</summary>
        public VibratoBracket VibratoBracket(FactoryVibratoBracketOptions options)
        {
            var bracket = new VibratoBracket(options.From, options.To);
            if (options.Line.HasValue) bracket.SetLine(options.Line.Value);
            if (options.Code.HasValue) bracket.SetVibratoCode(options.Code.Value);
            if (options.Harsh.HasValue) bracket.SetHarsh(options.Harsh.Value);
            if (options.VibratoWidth.HasValue) bracket.SetVibratoWidth(options.VibratoWidth.Value);
            bracket.SetContext(context);
            renderQ.Add(bracket);
            return bracket;
        }

        /// <summary>Create a TextBracket spanning two notes and add to renderQ.</summary>
        public TextBracket TextBracket(
            Note from,
            Note to,
            string text,
            string superscript,
            TextBracketPosition position = TextBracketPosition.Top,
            double? line = null)
        {
            var bracket = new TextBracket(new TextBracketParams
            {
                Start = from,
                Stop = to,
                Text = text,
                Superscript = superscript,
                Position = position,
            });
            if (line.HasValue) bracket.SetLine(line.Value);
            bracket.SetContext(context);
            renderQ.Add(bracket);
            return bracket;
        }

        /// <summary>Create a TextBracket from a VexFlow 5-style params object and add to renderQ.</summary>
        public TextBracket TextBracket(FactoryTextBracketOptions options)
        {
            var bracket = new TextBracket(new TextBracketParams
            {
                Start = options.Start,
                Stop = options.Stop,
                Text = options.Text,
                Superscript = options.Superscript,
                Position = options.Position,
            });
            if (options.Line.HasValue) bracket.SetLine(options.Line.Value);
            if (options.Dashed.HasValue) bracket.SetDashed(options.Dashed.Value, options.Dash);
            bracket.SetContext(context);
            renderQ.Add(bracket);
            return bracket;
        }

        // ── StaveConnector ────────────────────────────────────────────────────

        /// <summary>
        /// Create a StaveConnector between two staves and add to renderQ.
        ///
        /// Port of VexFlow's Factory.StaveConnector() from factory.ts.
        /// </summary>
        public StaveConnector StaveConnector(Stave topStave, Stave bottomStave, StaveConnectorType type)
        {
            var connector = new StaveConnector(topStave, bottomStave);
            connector.SetType(type);
            connector.SetContext(context);
            renderQ.Add(connector);
            return connector;
        }

        /// <summary>
        /// Create a StaveConnector between two staves (string type) and add to renderQ.
        /// </summary>
        public StaveConnector StaveConnector(Stave topStave, Stave bottomStave, string type)
        {
            var connector = new StaveConnector(topStave, bottomStave);
            connector.SetType(type);
            connector.SetContext(context);
            renderQ.Add(connector);
            return connector;
        }

        /// <summary>Create a StaveConnector from a VexFlow 5-style params object and add to renderQ.</summary>
        public StaveConnector StaveConnector(FactoryStaveConnectorOptions options)
        {
            var connector = new StaveConnector(options.TopStave, options.BottomStave);
            if (options.TypeString != null)
                connector.SetType(options.TypeString);
            else if (options.Type.HasValue)
                connector.SetType(options.Type.Value);

            if (options.XShift.HasValue)
                connector.SetXShift(options.XShift.Value);

            if (options.Texts != null)
            {
                foreach (var text in options.Texts)
                    connector.SetText(text.Text, text.ShiftX, text.ShiftY);
            }

            connector.SetContext(context);
            renderQ.Add(connector);
            return connector;
        }

        // ── Voice ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a Voice and add to the voices list.
        ///
        /// Port of VexFlow's Factory.Voice() from factory.ts.
        /// </summary>
        public Voice Voice(int? numBeats = null, int? beatValue = null)
        {
            var voiceTime = new VoiceTime
            {
                NumBeats  = numBeats  ?? 4,
                BeatValue = beatValue ?? 4,
                Resolution = Tables.RESOLUTION,
            };
            var voice = new Voice(voiceTime);
            voices.Add(voice);
            return voice;
        }

        /// <summary>Create a Voice from an explicit time descriptor and add to the voices list.</summary>
        public Voice Voice(VoiceTime voiceTime)
        {
            var voice = new Voice(voiceTime);
            voices.Add(voice);
            return voice;
        }

        /// <summary>Create a Voice from a time signature string such as "3/8" and add to the voices list.</summary>
        public Voice Voice(string timeSignature)
        {
            var voice = new Voice(timeSignature);
            voices.Add(voice);
            return voice;
        }

        /// <summary>Create a Voice from a VexFlow 5-style params object and add it to the voices list.</summary>
        public Voice Voice(FactoryVoiceOptions options)
        {
            options ??= new FactoryVoiceOptions();
            if (options.Time.HasValue)
                return Voice(options.Time.Value);

            return Voice(options.TimeString ?? "4/4");
        }

        // ── GraceNote ─────────────────────────────────────────────────────────

        /// <summary>
        /// Create a GraceNote. Does NOT add to renderQ — caller manages rendering.
        ///
        /// Port of VexFlow's Factory.GraceNote() from factory.ts.
        /// </summary>
        public GraceNote GraceNote(GraceNoteStruct noteStruct)
        {
            var note = new GraceNote(noteStruct);
            if (currentStave != null) note.SetStave(currentStave);
            note.SetContext(context);
            return note;
        }

        // ── GraceNoteGroup ────────────────────────────────────────────────────

        /// <summary>
        /// Create a GraceNoteGroup. Does NOT add to renderQ — caller manages attachment.
        ///
        /// Port of VexFlow's Factory.GraceNoteGroup() from factory.ts.
        /// </summary>
        public GraceNoteGroup GraceNoteGroup(List<StemmableNote> notes, bool slur = false)
        {
            var group = new GraceNoteGroup(notes.ConvertAll(n => (GraceNote)n), slur);
            group.SetContext(context);
            return group;
        }

        /// <summary>Create a GraceNoteGroup from typed grace notes. Does NOT add to renderQ.</summary>
        public GraceNoteGroup GraceNoteGroup(List<GraceNote> notes, bool slur = false)
        {
            var group = new GraceNoteGroup(notes, slur);
            group.SetContext(context);
            return group;
        }

        /// <summary>Create a GraceNoteGroup from a VexFlow 5-style params object. Does NOT add to renderQ.</summary>
        public GraceNoteGroup GraceNoteGroup(FactoryGraceNoteGroupOptions options)
        {
            options ??= new FactoryGraceNoteGroupOptions();
            return GraceNoteGroup(options.Notes, options.Slur);
        }

        // ── Accidental ────────────────────────────────────────────────────────

        /// <summary>
        /// Create an Accidental. Does NOT add to renderQ — caller attaches to note.
        ///
        /// Port of VexFlow's Factory.Accidental() from factory.ts.
        /// </summary>
        public Accidental Accidental(string type)
        {
            var accid = new Accidental(type);
            accid.SetContext(context);
            return accid;
        }

        /// <summary>
        /// Create an Accidental from a VexFlow 5-style params object.
        /// Does NOT add to renderQ — caller attaches to note.
        /// </summary>
        public Accidental Accidental(FactoryAccidentalOptions options)
        {
            options ??= new FactoryAccidentalOptions();
            var accid = new Accidental(options.Type);
            if (options.Cautionary) accid.SetAsCautionary();
            accid.SetContext(context);
            return accid;
        }

        // ── Annotation ────────────────────────────────────────────────────────

        /// <summary>
        /// Create an Annotation. Does NOT add to renderQ — caller attaches to note.
        ///
        /// Port of VexFlow's Factory.Annotation() from factory.ts.
        /// </summary>
        public Annotation Annotation(
            string text = "p",
            AnnotationHorizontalJustify hJustify = AnnotationHorizontalJustify.CENTER,
            AnnotationVerticalJustify vJustify   = AnnotationVerticalJustify.BOTTOM)
        {
            var annotation = new Annotation(text);
            annotation.SetJustification(hJustify);
            annotation.SetVerticalJustification(vJustify);
            annotation.SetContext(context);
            return annotation;
        }

        /// <summary>
        /// Create an Annotation from a VexFlow 5-style params object.
        /// Does NOT add to renderQ — caller attaches to note.
        /// </summary>
        public Annotation Annotation(FactoryAnnotationOptions options)
        {
            options ??= new FactoryAnnotationOptions();

            var annotation = new Annotation(options.Text);
            if (options.HJustifyString != null)
                annotation.SetJustification(options.HJustifyString);
            else if (options.HJustify.HasValue)
                annotation.SetJustification(options.HJustify.Value);
            else
                annotation.SetJustification(AnnotationHorizontalJustify.CENTER);

            if (options.VJustifyString != null)
                annotation.SetVerticalJustification(options.VJustifyString);
            else if (options.VJustify.HasValue)
                annotation.SetVerticalJustification(options.VJustify.Value);
            else
                annotation.SetVerticalJustification(AnnotationVerticalJustify.BOTTOM);

            if (options.Font != null)
                annotation.SetFont(options.Font.Family, options.Font.Size, BuildFontStyle(options.Font));

            annotation.SetContext(context);
            return annotation;
        }

        // ── FretHandFinger ───────────────────────────────────────────────────

        /// <summary>
        /// Create a FretHandFinger. Does NOT add to renderQ — caller attaches to note.
        /// Port of VexFlow's Factory.Fingering() from factory.ts.
        /// </summary>
        public FretHandFinger Fingering(string number = "", ModifierPosition? position = null)
        {
            var fingering = new FretHandFinger(number);
            if (position.HasValue) fingering.SetPosition(position.Value);
            fingering.SetContext(context);
            return fingering;
        }

        /// <summary>
        /// Create a FretHandFinger from a VexFlow 5-style params object.
        /// Does NOT add to renderQ — caller attaches to note.
        /// </summary>
        public FretHandFinger Fingering(FactoryFingeringOptions options)
        {
            options ??= new FactoryFingeringOptions();

            var fingering = new FretHandFinger(options.Number);
            if (options.Position.HasValue) fingering.SetPosition(options.Position.Value);
            if (options.OffsetX.HasValue) fingering.SetOffsetX(options.OffsetX.Value);
            if (options.OffsetY.HasValue) fingering.SetOffsetY(options.OffsetY.Value);
            fingering.SetContext(context);
            return fingering;
        }

        // ── StringNumber ─────────────────────────────────────────────────────

        /// <summary>
        /// Create a StringNumber. Does NOT add to renderQ — caller attaches to note.
        /// Port of VexFlow's Factory.StringNumber() from factory.ts.
        /// </summary>
        public StringNumber StringNumber(string number, ModifierPosition position, bool drawCircle = true)
        {
            var stringNumber = new StringNumber(number);
            stringNumber.SetPosition(position);
            stringNumber.SetContext(context);
            stringNumber.SetDrawCircle(drawCircle);
            return stringNumber;
        }

        /// <summary>
        /// Create a StringNumber from a VexFlow 5-style params object.
        /// Does NOT add to renderQ — caller attaches to note.
        /// </summary>
        public StringNumber StringNumber(FactoryStringNumberOptions options)
        {
            options ??= new FactoryStringNumberOptions();

            var stringNumber = new StringNumber(options.Number);
            stringNumber.SetPosition(options.Position);
            stringNumber.SetDrawCircle(options.DrawCircle);
            if (options.OffsetX.HasValue) stringNumber.SetOffsetX(options.OffsetX.Value);
            if (options.OffsetY.HasValue) stringNumber.SetOffsetY(options.OffsetY.Value);
            if (options.StemOffset.HasValue) stringNumber.SetStemOffset(options.StemOffset.Value);
            if (options.Dashed.HasValue) stringNumber.SetDashed(options.Dashed.Value);
            if (options.LineEndType.HasValue) stringNumber.SetLineEndType(options.LineEndType.Value);
            stringNumber.SetContext(context);
            return stringNumber;
        }

        // ── Articulation ──────────────────────────────────────────────────────

        /// <summary>
        /// Create an Articulation. Does NOT add to renderQ — caller attaches to note.
        ///
        /// Port of VexFlow's Factory.Articulation() from factory.ts.
        /// </summary>
        public Articulation Articulation(string type = "a.", ModifierPosition? position = null)
        {
            var articulation = new Articulation(type);
            if (position.HasValue) articulation.SetPosition(position.Value);
            articulation.SetContext(context);
            return articulation;
        }

        /// <summary>
        /// Create an Articulation from a VexFlow 5-style params object.
        /// Does NOT add to renderQ — caller attaches to note.
        /// </summary>
        public Articulation Articulation(FactoryArticulationOptions options)
        {
            options ??= new FactoryArticulationOptions();

            var articulation = new Articulation(options.Type);
            if (options.Position.HasValue) articulation.SetPosition(options.Position.Value);
            if (options.BetweenLines.HasValue) articulation.SetBetweenLines(options.BetweenLines.Value);
            articulation.SetContext(context);
            return articulation;
        }

        private static string BuildFontStyle(MetricsFontInfo font)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(font.Weight) && font.Weight != "normal") parts.Add(font.Weight);
            if (!string.IsNullOrWhiteSpace(font.Style) && font.Style != "normal") parts.Add(font.Style);
            return string.Join(" ", parts);
        }

        // ── Ornament ──────────────────────────────────────────────────────────

        /// <summary>
        /// Create an Ornament. Does NOT add to renderQ — caller attaches to note.
        ///
        /// Port of VexFlow's Factory.Ornament() from factory.ts.
        /// </summary>
        public Ornament Ornament(
            string type,
            ModifierPosition? position = null,
            string upperAccidental     = null,
            string lowerAccidental     = null,
            bool?   delayed             = null)
        {
            var ornament = new Ornament(type);
            if (position.HasValue)          ornament.SetPosition(position.Value);
            if (upperAccidental != null)    ornament.SetUpperAccidental(upperAccidental);
            if (lowerAccidental != null)    ornament.SetLowerAccidental(lowerAccidental);
            if (delayed.HasValue)           ornament.SetDelayed(delayed.Value);
            ornament.SetContext(context);
            return ornament;
        }

        /// <summary>
        /// Create an Ornament from a VexFlow 5-style params object.
        /// Does NOT add to renderQ — caller attaches to note.
        /// </summary>
        public Ornament Ornament(FactoryOrnamentOptions options)
        {
            options ??= new FactoryOrnamentOptions();

            var ornament = new Ornament(options.Type);
            if (options.Position.HasValue) ornament.SetPosition(options.Position.Value);
            if (options.UpperAccidental != null) ornament.SetUpperAccidental(options.UpperAccidental);
            if (options.LowerAccidental != null) ornament.SetLowerAccidental(options.LowerAccidental);
            if (options.Delayed.HasValue) ornament.SetDelayed(options.Delayed.Value);
            ornament.SetContext(context);
            return ornament;
        }

        // ── ChordSymbol ──────────────────────────────────────────────────────

        /// <summary>
        /// Create a ChordSymbol modifier. Does NOT add to renderQ — caller attaches to note.
        /// Port of VexFlow's Factory.ChordSymbol() from factory.ts.
        /// </summary>
        public ChordSymbol ChordSymbol(
            double? fontSize = null,
            string fontFamily = null,
            string fontWeight = null,
            string fontStyle = null,
            string hJustify = null,
            string vJustify = null)
        {
            var chordSymbol = new ChordSymbol();
            chordSymbol.SetHorizontal(hJustify ?? "center");
            chordSymbol.SetVertical(vJustify ?? "top");
            if (fontSize.HasValue || fontFamily != null || fontWeight != null || fontStyle != null)
            {
                var font = Metrics.GetFontInfo("ChordSymbol");
                chordSymbol.SetFont(fontFamily ?? font.Family, fontSize ?? font.Size, fontWeight ?? font.Weight, fontStyle ?? font.Style);
            }
            chordSymbol.SetContext(context);
            return chordSymbol;
        }

        /// <summary>
        /// Create a ChordSymbol from a VexFlow 5-style params object.
        /// Does NOT add to renderQ — caller attaches to note.
        /// </summary>
        public ChordSymbol ChordSymbol(FactoryChordSymbolOptions options)
        {
            options ??= new FactoryChordSymbolOptions();

            var chordSymbol = new ChordSymbol();
            chordSymbol.SetHorizontal("center");
            chordSymbol.SetVertical("top");
            if (options.FontSize.HasValue || options.FontFamily != null || options.FontWeight != null || options.FontStyle != null)
            {
                var font = Metrics.GetFontInfo("ChordSymbol");
                chordSymbol.SetFont(
                    options.FontFamily ?? font.Family,
                    options.FontSize ?? font.Size,
                    options.FontWeight ?? font.Weight,
                    options.FontStyle ?? font.Style);
            }
            if (options.HJustifyString != null)
                chordSymbol.SetHorizontal(options.HJustifyString);
            else if (options.HJustify.HasValue)
                chordSymbol.SetHorizontal(options.HJustify.Value);

            if (options.VJustifyString != null)
                chordSymbol.SetVertical(options.VJustifyString);
            else if (options.VJustify.HasValue)
                chordSymbol.SetVertical(options.VJustify.Value);

            if (options.ReportWidth.HasValue)
                chordSymbol.SetReportWidth(options.ReportWidth.Value);

            chordSymbol.SetContext(context);
            return chordSymbol;
        }

        // ── Vibrato ───────────────────────────────────────────────────────────

        /// <summary>
        /// Create a Vibrato modifier. Does NOT add to renderQ — caller attaches to note.
        ///
        /// Port of VexFlow's Factory's vibrato usage (vibrato.ts).
        /// </summary>
        public Vibrato Vibrato()
        {
            var vibrato = new Vibrato();
            vibrato.SetContext(context);
            return vibrato;
        }

        /// <summary>
        /// Create a Vibrato from a VexFlow 5-style params object.
        /// Does NOT add to renderQ — caller attaches to note.
        /// </summary>
        public Vibrato Vibrato(FactoryVibratoOptions options)
        {
            options ??= new FactoryVibratoOptions();
            var vibrato = new Vibrato();
            if (options.RenderOptions != null)
                vibrato.SetVibratoRenderOptions(options.RenderOptions);
            if (options.Code.HasValue)
                vibrato.SetVibratoCode(options.Code.Value);
            if (options.VibratoWidth.HasValue)
                vibrato.SetVibratoWidth(options.VibratoWidth.Value);
            if (options.Harsh.HasValue)
                vibrato.SetHarsh(options.Harsh.Value);
            vibrato.SetContext(context);
            return vibrato;
        }

        // ── PedalMarking ──────────────────────────────────────────────────────

        /// <summary>
        /// Create a PedalMarking and add to renderQ.
        /// Port of VexFlow's Factory.PedalMarking() from factory.ts.
        /// </summary>
        public PedalMarking PedalMarking(List<StaveNote> notes = null, string style = "mixed")
        {
            var pedal = new PedalMarking(notes ?? new List<StaveNote>());
            pedal.SetType(style);
            pedal.SetContext(context);
            renderQ.Add(pedal);
            return pedal;
        }

        /// <summary>Create a PedalMarking from a VexFlow 5-style params object and add to renderQ.</summary>
        public PedalMarking PedalMarking(FactoryPedalMarkingOptions options)
        {
            options ??= new FactoryPedalMarkingOptions();

            var pedal = new PedalMarking(options.Notes ?? new List<StaveNote>());
            pedal.SetType(options.Style);
            if (options.DepressText != null || options.ReleaseText != null)
                pedal.SetCustomText(options.DepressText ?? string.Empty, options.ReleaseText);
            if (options.Line.HasValue)
                pedal.SetLine(options.Line.Value);
            pedal.SetContext(context);
            renderQ.Add(pedal);
            return pedal;
        }

        /// <summary>Create a MultiMeasureRest and add to renderQ.</summary>
        public MultiMeasureRest MultiMeasureRest(MultiMeasureRestRenderOptions options)
        {
            var rest = new MultiMeasureRest(options.NumberOfMeasures, options);
            if (currentStave != null) rest.SetStave(currentStave);
            rest.SetContext(context);
            renderQ.Add(rest);
            return rest;
        }

        // ── NoteSubGroup ──────────────────────────────────────────────────────

        /// <summary>
        /// Create a NoteSubGroup. Does NOT add to renderQ — caller attaches to note.
        ///
        /// Port of VexFlow's Factory.NoteSubGroup() from factory.ts.
        /// </summary>
        public NoteSubGroup NoteSubGroup(List<Note> notes)
        {
            var group = new NoteSubGroup(notes);
            group.SetContext(context);
            return group;
        }

        /// <summary>Create a NoteSubGroup from a VexFlow 5-style params object. Does NOT add to renderQ.</summary>
        public NoteSubGroup NoteSubGroup(FactoryNoteSubGroupOptions options)
        {
            options ??= new FactoryNoteSubGroupOptions();
            return NoteSubGroup(options.Notes);
        }

        // ── Dot ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Create an augmentation Dot modifier. Does NOT add to renderQ — caller attaches to note.
        ///
        /// Port of VexFlow's usage of Dot from factory.ts / dot.ts.
        /// </summary>
        public Dot Dot()
        {
            var dot = new Dot();
            dot.SetContext(context);
            return dot;
        }

        // ── EasyScore ─────────────────────────────────────────────────────────

        /// <summary>
        /// Create an EasyScore with a bidirectional reference to this factory.
        ///
        /// Port of VexFlow's Factory.EasyScore() from factory.ts.
        /// Note: EasyScore is a stub until Plan 05-04.
        /// </summary>
        public EasyScore EasyScore(EasyScoreOptions options = null)
        {
            return new EasyScore(this, options);
        }

        // ── Formatter ─────────────────────────────────────────────────────────

        /// <summary>
        /// Create a standalone Formatter instance.
        ///
        /// Port of VexFlow's Factory.Formatter() from factory.ts.
        /// </summary>
        public Formatter Formatter(FormatterOptions options = null)
        {
            return new Formatter(options);
        }

        // ── TickContext ────────────────────────────────────────────────────────

        /// <summary>
        /// Create a standalone TickContext instance.
        ///
        /// Port of VexFlow's Factory.TickContext() from factory.ts.
        /// </summary>
        public TickContext TickContext()
        {
            return new TickContext();
        }

        // ── ModifierContext ────────────────────────────────────────────────────

        /// <summary>
        /// Create a standalone ModifierContext instance.
        ///
        /// Port of VexFlow's Factory.ModifierContext() from factory.ts.
        /// </summary>
        public ModifierContext ModifierContext()
        {
            return new ModifierContext();
        }
    }
}

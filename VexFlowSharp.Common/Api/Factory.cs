#nullable enable annotations

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
    /// High-level builder API for VexFlowSharp score construction.
    /// Wraps all lower-level objects into a unified builder that handles
    /// context injection and 5-step draw ordering.
    ///
    /// Port of VexFlow's Factory class from factory.ts.
    /// </summary>
    public class Factory
    {
        // ── Fields ─────────────────────────────────────────────────────────────

        private readonly RenderContext context;
        private readonly double width;
        private readonly double height;

        private List<Stave> staves = new List<Stave>();
        private List<Voice> voices = new List<Voice>();
        private List<Element> renderQ = new List<Element>();
        private List<VexFlowSharp.Common.Formatting.System> systems =
            new List<VexFlowSharp.Common.Formatting.System>();

        private Stave? currentStave;

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

        /// <summary>Get the current (most recently created) stave.</summary>
        public Stave? GetStave() => currentStave;

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
        public VexFlowSharp.Common.Formatting.System System(SystemOptions? options = null)
        {
            var opts = options ?? new SystemOptions { X = 0, Y = 0, Width = width };
            var system = new VexFlowSharp.Common.Formatting.System(opts);
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
        public Stave Stave(double x = 0, double y = 0, double? w = null, StaveOptions? options = null)
        {
            var stave = new Stave(x, y, w ?? width, options);
            staves.Add(stave);
            currentStave = stave;
            return stave;
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
        public GhostNote GhostNote(StaveNoteStruct noteStruct)
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

        // ── Beam ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a Beam over the given StemmableNotes and add to renderQ.
        ///
        /// Port of VexFlow's Factory.Beam() from factory.ts.
        /// </summary>
        public Beam Beam(List<StemmableNote> notes, bool autoStem = false)
        {
            var beam = new Beam(notes, autoStem);
            beam.SetContext(context);
            renderQ.Add(beam);
            return beam;
        }

        // ── Tuplet ────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a Tuplet over the given notes and add to renderQ.
        ///
        /// Port of VexFlow's Factory.Tuplet() from factory.ts.
        /// </summary>
        public Tuplet Tuplet(List<Note> notes, TupletOptions? options = null)
        {
            var tuplet = new Tuplet(notes, options ?? new TupletOptions());
            tuplet.SetContext(context);
            renderQ.Add(tuplet);
            return tuplet;
        }

        // ── Curve ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Create a Curve (bezier slur) and add to renderQ.
        ///
        /// Port of VexFlow's Factory.Curve() from factory.ts.
        /// </summary>
        public Curve Curve(Note from, Note to, CurveOptions? options = null)
        {
            var curve = new Curve(from, to, options ?? new CurveOptions());
            curve.SetContext(context);
            renderQ.Add(curve);
            return curve;
        }

        // ── StaveTie ──────────────────────────────────────────────────────────

        /// <summary>
        /// Create a StaveTie and add to renderQ.
        ///
        /// Port of VexFlow's Factory.StaveTie() from factory.ts.
        /// </summary>
        public StaveTie StaveTie(TieNotes tieNotes, string? text = null, int? direction = null)
        {
            var tie = new StaveTie(tieNotes, text);
            if (direction.HasValue) tie.SetDirection(direction.Value);
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
        public VibratoBracket VibratoBracket(Note? from, Note? to, int? line = null, bool? harsh = null)
        {
            var bracket = new VibratoBracket(from, to);
            if (line.HasValue)  bracket.SetLine(line.Value);
            if (harsh.HasValue) bracket.SetHarsh(harsh.Value);
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

        // ── Ornament ──────────────────────────────────────────────────────────

        /// <summary>
        /// Create an Ornament. Does NOT add to renderQ — caller attaches to note.
        ///
        /// Port of VexFlow's Factory.Ornament() from factory.ts.
        /// </summary>
        public Ornament Ornament(
            string type,
            ModifierPosition? position  = null,
            string? upperAccidental     = null,
            string? lowerAccidental     = null,
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
        public EasyScore EasyScore()
        {
            return new EasyScore(this);
        }

        // ── Formatter ─────────────────────────────────────────────────────────

        /// <summary>
        /// Create a standalone Formatter instance.
        ///
        /// Port of VexFlow's Factory.Formatter() from factory.ts.
        /// </summary>
        public Formatter Formatter(FormatterOptions? options = null)
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

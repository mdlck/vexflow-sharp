// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License
//
// Port of VexFlow's System class (system.ts, 321 lines).
// System is the multi-stave layout container: it runs a unified Formatter pass
// across all staves so note columns align vertically. Required for grand-staff rendering.

using System;
using System.Collections.Generic;
using VexFlowSharp;

namespace VexFlowSharp.Common.Formatting
{
    /// <summary>
    /// Options for configuring a System.
    /// Port of VexFlow's SystemOptions interface from system.ts.
    /// </summary>
    public class SystemOptions
    {
        /// <summary>X origin of the system. Default 10.</summary>
        public double X { get; set; } = 10;

        /// <summary>Y origin of the system. Default 10.</summary>
        public double Y { get; set; } = 10;

        /// <summary>Width of the system. Default 500.</summary>
        public double Width { get; set; } = 500;

        /// <summary>Space between staves in stave-space units. Default 12.</summary>
        public double SpaceBetweenStaves { get; set; } = 12;

        /// <summary>Whether to use auto-width based on minimum total width. Default false.</summary>
        public bool AutoWidth { get; set; } = false;

        /// <summary>Disable justification between voices. Default false.</summary>
        public bool NoJustification { get; set; } = false;

        /// <summary>Omit default padding when calculating justify width. Default false.</summary>
        public bool NoPadding { get; set; } = false;

        /// <summary>Number of formatter tuning iterations. Default 0.</summary>
        public int FormatIterations { get; set; } = 0;

        /// <summary>Learning rate for formatter Tune() calls. Default 0.5.</summary>
        public double Alpha { get; set; } = 0.5;

        /// <summary>Emit formatter debug info. Default false.</summary>
        public bool DebugFormatter { get; set; } = false;

        /// <summary>Additional formatting parameters passed to Formatter.Format().</summary>
        public FormatParams? FormatOptions { get; set; }
    }

    /// <summary>
    /// Represents one stave (staff part) within a System, with its associated voices.
    /// Port of VexFlow's SystemStave interface from system.ts.
    /// </summary>
    public class SystemStave
    {
        /// <summary>Voices to render on this stave.</summary>
        public List<Voice> Voices { get; set; } = new List<Voice>();

        /// <summary>Pre-created Stave, or null to create one from System options.</summary>
        public VexFlowSharp.Stave? Stave { get; set; }

        /// <summary>Skip justification for this stave's voices. Default false.</summary>
        public bool NoJustification { get; set; } = false;

        /// <summary>Optional stave creation options (used when Stave is null).</summary>
        public VexFlowSharp.StaveOptions? Options { get; set; }

        /// <summary>Extra space above this stave in stave-space units.</summary>
        public double SpaceAbove { get; set; } = 0;

        /// <summary>Extra space below this stave in stave-space units.</summary>
        public double SpaceBelow { get; set; } = 0;
    }

    /// <summary>
    /// Internal per-stave metadata used during Format().
    /// </summary>
    internal class StaveInfo
    {
        public double SpaceAbove { get; set; }
        public double SpaceBelow { get; set; }
        public bool NoJustification { get; set; }
    }

    /// <summary>
    /// Musical system: a collection of staves, each with one or more voices,
    /// all formatted together so note columns align vertically.
    ///
    /// Port of VexFlow's System class from system.ts.
    ///
    /// Usage:
    ///   var system = new System(new SystemOptions { X = 10, Y = 10, Width = 570 });
    ///   system.SetContext(ctx);
    ///   var treble = system.AddStave(new SystemStave { Voices = new List&lt;Voice&gt; { voice1 } });
    ///   var bass   = system.AddStave(new SystemStave { Voices = new List&lt;Voice&gt; { voice2 } });
    ///   system.Format();
    /// </summary>
    public class System : VexFlowSharp.Element
    {
        // ── Fields ────────────────────────────────────────────────────────────

        private readonly SystemOptions options;
        private readonly Formatter formatter;
        private readonly List<VexFlowSharp.Stave> partStaves;
        private readonly List<Voice> partVoices;
        private readonly List<StaveInfo> partStaveInfos;
        private readonly List<StaveConnector> connectors;

        private double startX;
        private double lastY;
        private bool formatCalled;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Create a new System with the given layout options.
        /// Port of VexFlow's System constructor from system.ts.
        /// </summary>
        public System(SystemOptions? options = null)
        {
            this.options = options ?? new SystemOptions();

            // If no width specified and noJustification is false, switch to autoWidth
            if (!this.options.NoJustification && this.options.Width == 500 && options == null)
                this.options.AutoWidth = true;

            formatter      = new Formatter();
            partStaves     = new List<VexFlowSharp.Stave>();
            partVoices     = new List<Voice>();
            partStaveInfos = new List<StaveInfo>();
            connectors     = new List<StaveConnector>();
            startX         = 0;
            lastY          = this.options.Y;
            formatCalled   = false;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Get the system's origin X coordinate.</summary>
        public double GetX() => options.X;

        /// <summary>Get the system's origin Y coordinate.</summary>
        public double GetY() => options.Y;

        /// <summary>Get all staves added to this system.</summary>
        public List<VexFlowSharp.Stave> GetStaves() => partStaves;

        /// <summary>Get all voices added to this system.</summary>
        public List<Voice> GetVoices() => partVoices;

        // ── AddStave ─────────────────────────────────────────────────────────

        /// <summary>
        /// Add a stave to the system with associated voices.
        ///
        /// If p.Stave is null, a new Stave is created at the system's (X, Y, Width).
        /// All voices are attached to the stave and stored for the Format() pass.
        ///
        /// Port of VexFlow's System.addStave() from system.ts lines 195-224.
        /// </summary>
        /// <returns>The Stave (pre-existing or newly created).</returns>
        public VexFlowSharp.Stave AddStave(SystemStave p)
        {
            var stave = p.Stave;
            if (stave == null)
            {
                var staveOptions = p.Options ?? new VexFlowSharp.StaveOptions { LeftBar = false };
                stave = new VexFlowSharp.Stave(options.X, options.Y, options.Width, staveOptions);
            }

            var info = new StaveInfo
            {
                SpaceAbove     = p.SpaceAbove,
                SpaceBelow     = p.SpaceBelow,
                NoJustification = p.NoJustification,
            };

            // Attach stave to all voices; also set stave on each tickable (via Note cast)
            foreach (var voice in p.Voices)
            {
                voice.SetStave(stave);
                foreach (var tickable in voice.GetTickables())
                {
                    if (tickable is VexFlowSharp.Note note)
                        note.SetStave(stave);
                }
                partVoices.Add(voice);
            }

            partStaves.Add(stave);
            partStaveInfos.Add(info);
            return stave;
        }

        // ── AddConnector ─────────────────────────────────────────────────────

        /// <summary>
        /// Add a StaveConnector between the first and last stave in this system.
        /// Requires at least 2 staves to have been added.
        ///
        /// Port of VexFlow's System.addConnector() from system.ts lines 225-235.
        /// </summary>
        /// <param name="type">Connector type string (e.g., "brace", "bracket", "double", "single").</param>
        /// <returns>The created StaveConnector (for chaining).</returns>
        public StaveConnector AddConnector(string type = "double")
        {
            if (partStaves.Count < 2)
                throw new VexFlowSharp.VexFlowException("TooFewStaves", "System.AddConnector requires at least 2 staves.");

            var topStave    = partStaves[0];
            var bottomStave = partStaves[partStaves.Count - 1];
            var connector   = new StaveConnector(topStave, bottomStave);
            connector.SetType(type);
            connectors.Add(connector);
            return connector;
        }

        /// <summary>
        /// Add a StaveConnector between the first and last stave in this system.
        /// Overload accepting the StaveConnectorType enum directly.
        /// </summary>
        /// <param name="type">Connector type enum value.</param>
        /// <returns>The created StaveConnector (for chaining).</returns>
        public StaveConnector AddConnector(StaveConnectorType type)
        {
            if (partStaves.Count < 2)
                throw new VexFlowSharp.VexFlowException("TooFewStaves", "System.AddConnector requires at least 2 staves.");

            var topStave    = partStaves[0];
            var bottomStave = partStaves[partStaves.Count - 1];
            var connector   = new StaveConnector(topStave, bottomStave);
            connector.SetType(type);
            connectors.Add(connector);
            return connector;
        }

        // ── Format ───────────────────────────────────────────────────────────

        /// <summary>
        /// Run a unified Formatter pass across all staves, aligning note columns.
        ///
        /// Port of VexFlow's System.format() from system.ts lines 238-298.
        ///
        /// Steps:
        ///   1. Stack staves vertically using Space() and SpaceBetweenStaves.
        ///   2. Find maximum noteStartX across all staves.
        ///   3. Join all voices and set common startX.
        ///   4. Calculate justifyWidth and call Formatter.Format().
        ///   5. PostFormat and optional Tune() iterations.
        ///   6. Align beginning modifiers across staves.
        /// </summary>
        public void Format()
        {
            if (partStaves.Count == 0) return;

            // ── Step 1: Stack staves vertically ──────────────────────────────
            double y = options.Y;
            for (int i = 0; i < partStaves.Count; i++)
            {
                var stave = partStaves[i];
                var info  = partStaveInfos[i];
                y += stave.Space(info.SpaceAbove);
                stave.SetY(y);
                y += stave.Space(info.SpaceBelow);
                y += stave.Space(options.SpaceBetweenStaves);
            }
            lastY = y;

            // ── Step 2: Find maximum noteStartX across all staves ─────────────
            double maxStartX = 0;
            foreach (var stave in partStaves)
                maxStartX = Math.Max(maxStartX, stave.GetNoteStartX());
            startX = maxStartX;

            // Re-assign stave to each tickable now that y positions are set
            // (tickable.GetStave() is only available on Note subclasses)
            foreach (var voice in partVoices)
            {
                foreach (var tickable in voice.GetTickables())
                {
                    if (tickable is VexFlowSharp.Note note)
                    {
                        var tickStave = note.GetStave();
                        if (tickStave != null)
                            note.SetStave(tickStave);
                    }
                }
            }

            // ── Step 3: Join voices and set common startX ─────────────────────
            if (partVoices.Count > 0)
                formatter.JoinVoices(partVoices);

            foreach (var stave in partStaves)
                stave.SetNoteStartX(startX);

            // ── Step 4: Calculate justifyWidth ────────────────────────────────
            double justifyWidth = 0;
            if (options.AutoWidth && partVoices.Count > 0)
            {
                justifyWidth = formatter.PreCalculateMinTotalWidth(partVoices);
                double newWidth = justifyWidth + VexFlowSharp.Stave.RightPadding + (startX - options.X);
                foreach (var stave in partStaves)
                    stave.SetWidth(newWidth);
            }
            else
            {
                justifyWidth = options.NoPadding
                    ? options.Width - (startX - options.X)
                    : options.Width - (startX - options.X) - VexFlowSharp.Stave.DefaultPadding;
            }

            // ── Step 5: Format, PostFormat, optional Tune ─────────────────────
            if (partVoices.Count > 0)
            {
                formatter.Format(
                    partVoices,
                    options.NoJustification ? 0 : justifyWidth,
                    options.FormatOptions);
            }
            formatter.PostFormat();

            for (int i = 0; i < options.FormatIterations; i++)
                formatter.Tune(options.Alpha);

            // ── Step 6: Align beginning modifiers across staves ───────────────
            VexFlowSharp.Stave.FormatBegModifiers(partStaves);

            formatCalled = true;
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Render all StaveConnectors added to this system.
        /// Stave and voice drawing is still performed by the caller after calling Format();
        /// Draw() handles connector rendering which requires both staves to be positioned.
        ///
        /// Port of VexFlow's System.draw() from system.ts lines 300-320.
        /// </summary>
        public override void Draw()
        {
            if (!formatCalled)
                throw new VexFlowSharp.VexFlowException("NoFormatter", "Format() must be called before Draw().");

            var ctx = CheckContext();

            foreach (var connector in connectors)
            {
                connector.SetContext(ctx);
                connector.Draw();
            }

            rendered = true;
        }
    }
}

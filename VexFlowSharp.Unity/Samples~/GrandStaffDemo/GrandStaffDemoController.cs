using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VexFlowSharp.Api;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Unity;

/// <summary>
/// Demo MonoBehaviour that renders a grand staff score using VexFlowSharp.
///
/// Setup:
///   1. Add this script to a GameObject in a scene.
///   2. Add a UIDocument component to the same (or any) GameObject.
///   3. Assign the UIDocument reference in the Inspector.
///
/// What this demo renders:
///   - Treble stave: C5 D5 E5 F5 (quarter notes)
///   - Bass stave:   C3 D3 E3 F3 (quarter notes)
///   - Both staves bracketed with a brace connector
///   - Time signature 4/4 on both staves
///
/// This provides the manual visual verification path for REND-06.
/// </summary>
public class GrandStaffDemoController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VexFlowElement _vfElement;

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogError("GrandStaffDemoController: UIDocument reference is null. Assign in Inspector.");
            return;
        }

        var root = uiDocument.rootVisualElement;

        // ── Create VexFlowElement and add to UI ───────────────────────────────
        _vfElement = new VexFlowElement();
        _vfElement.style.width = 800;
        _vfElement.style.height = 400;
        // Pre-allocate 32 Labels to avoid VisualElement mutations inside
        // generateVisualContent callback (see RESEARCH.md Pitfall 3)
        _vfElement.PreAllocateLabels(32);
        root.Add(_vfElement);

        // ── Build score using Factory + EasyScore ─────────────────────────────
        // VexFlowElement.Context is the long-lived UIElementsRenderContext whose
        // Painter2D is refreshed each frame inside OnGenerateVisualContent.
        var ctx = _vfElement.Context;
        var factory = new Factory(ctx, 800, 400);

        // System handles multi-stave layout and vertical alignment
        var system = factory.System(new SystemOptions { X = 10, Y = 40, Width = 770 });

        // ── Treble stave ──────────────────────────────────────────────────────
        // Parse treble notes via EasyScore DSL
        var es = factory.EasyScore();
        es.DefaultClef = "treble";
        var trebleNotes = es.Notes("C5/q, D5, E5, F5");

        // Create voice and add notes
        var trebleVoice = factory.Voice(4, 4);
        trebleVoice.AddTickables(trebleNotes.ConvertAll(n => (Tickable)n));

        // Add treble stave to system with voice; system creates the Stave internally
        var trebleStave = system.AddStave(new SystemStave
        {
            Voices = new List<Voice> { trebleVoice },
        });
        trebleStave.AddClef("treble").AddTimeSignature("4/4");

        // ── Bass stave ────────────────────────────────────────────────────────
        var esB = factory.EasyScore();
        esB.DefaultClef = "bass";
        var bassNotes = esB.Notes("C3/q, D3, E3, F3", new NoteOptions { Clef = "bass" });

        var bassVoice = factory.Voice(4, 4);
        bassVoice.AddTickables(bassNotes.ConvertAll(n => (Tickable)n));

        var bassStave = system.AddStave(new SystemStave
        {
            Voices = new List<Voice> { bassVoice },
        });
        bassStave.AddClef("bass").AddTimeSignature("4/4");

        // ── System connector (brace) ──────────────────────────────────────────
        system.AddConnector("brace");
        system.AddConnector("singleLeft");

        // ── Render ────────────────────────────────────────────────────────────
        // Render() stores the factory and calls MarkDirtyRepaint().
        // The actual drawing happens in OnGenerateVisualContent when UIElements
        // calls back the generateVisualContent delegate.
        _vfElement.Render(factory);
    }
}

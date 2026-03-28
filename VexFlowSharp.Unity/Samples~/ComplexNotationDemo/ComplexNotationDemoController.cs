using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VexFlowSharp.Api;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Unity;

/// <summary>
/// Demo MonoBehaviour that renders a complex notation score using VexFlowSharp.
///
/// Setup:
///   1. Add this script to a GameObject in a scene.
///   2. Add a UIDocument component to the same (or any) GameObject.
///   3. Assign the UIDocument reference in the Inspector.
///
/// What this demo renders (matches ComplexNotationComparisonTest):
///   - Treble clef, 4/4 time signature
///   - Dotted 8th C4 with ## (double sharp) accidental
///   - 16th D4 with b (flat) accidental
///   - The above two notes auto-beamed together
///   - Quarter E4 (tied to the next note)
///   - Quarter E4 (tied from the previous note)
///   - Quarter G4
///
/// This provides the manual visual verification path for the complex notation pipeline.
/// </summary>
public class ComplexNotationDemoController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VexFlowElement _vfElement;

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogError("ComplexNotationDemoController: UIDocument reference is null. Assign in Inspector.");
            return;
        }

        var root = uiDocument.rootVisualElement;

        // ── Create VexFlowElement and add to UI ───────────────────────────────
        _vfElement = new VexFlowElement();
        _vfElement.style.width = 800;
        _vfElement.style.height = 300;
        // Pre-allocate 32 Labels to avoid VisualElement mutations inside
        // generateVisualContent callback (see RESEARCH.md Pitfall 3)
        _vfElement.PreAllocateLabels(32);
        root.Add(_vfElement);

        // ── Build score using Factory lower-level API ─────────────────────────
        // VexFlowElement.Context is the long-lived UIElementsRenderContext whose
        // Painter2D is refreshed each frame inside OnGenerateVisualContent.
        var factory = new Factory(_vfElement.Context, 800, 300);

        // ── Stave with treble clef and 4/4 time signature ─────────────────────
        var stave = factory.Stave(10, 40, 770);
        stave.AddClef("treble").AddTimeSignature("4/4");

        // ── Notes ─────────────────────────────────────────────────────────────
        // Total duration must equal 4/4:
        //   dotted 8th (3/16) + 16th (1/16) + quarter + quarter + quarter = 4 beats

        // a. Dotted 8th C4 with double sharp (##) accidental
        var dotted8thC4 = factory.StaveNote(new StaveNoteStruct
        {
            Keys     = new[] { "c/4" },
            Duration = "8d",
        });
        dotted8thC4.AddModifier(new Accidental("##"), 0);

        // b. 16th D4 with flat (b) accidental
        var sixteenthD4 = factory.StaveNote(new StaveNoteStruct
        {
            Keys     = new[] { "d/4" },
            Duration = "16",
        });
        sixteenthD4.AddModifier(new Accidental("b"), 0);

        // c. Quarter E4 (first — tied to the next)
        var quarterE4First = factory.StaveNote(new StaveNoteStruct
        {
            Keys     = new[] { "e/4" },
            Duration = "q",
        });

        // d. Quarter E4 (second — tied from the previous)
        var quarterE4Second = factory.StaveNote(new StaveNoteStruct
        {
            Keys     = new[] { "e/4" },
            Duration = "q",
        });

        // e. Quarter G4
        var quarterG4 = factory.StaveNote(new StaveNoteStruct
        {
            Keys     = new[] { "g/4" },
            Duration = "q",
        });

        // ── Voice ─────────────────────────────────────────────────────────────
        var voice = factory.Voice(4, 4);
        voice.AddTickables(new List<Tickable>
        {
            dotted8thC4, sixteenthD4, quarterE4First, quarterE4Second, quarterG4
        });

        // ── Beam (auto-beam the dotted 8th + 16th pair) ───────────────────────
        factory.Beam(new List<StemmableNote> { dotted8thC4, sixteenthD4 });

        // ── Tie (tie the two E4 quarter notes) ────────────────────────────────
        var tie = factory.StaveTie(new TieNotes
        {
            FirstNote  = quarterE4First,
            LastNote   = quarterE4Second,
            FirstIndex = 0,
            LastIndex  = 0,
        });
        // Use VexFlow JS stavetie.ts defaults (cp1=8, cp2=12) for visual parity
        tie.RenderOptions.Cp1 = 8;
        tie.RenderOptions.Cp2 = 12;

        // ── Format voice onto the stave ───────────────────────────────────────
        var voices = new List<Voice> { voice };
        factory.Formatter().JoinVoices(voices).Format(voices, 700);

        // ── Render ────────────────────────────────────────────────────────────
        // Render() stores the factory and calls MarkDirtyRepaint().
        // The actual drawing happens in OnGenerateVisualContent when UIElements
        // calls back the generateVisualContent delegate.
        _vfElement.Render(factory);
    }
}

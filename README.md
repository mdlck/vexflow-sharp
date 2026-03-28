# VexFlowSharp

A (AI-assisted) C# port of [VexFlow](https://github.com/0xfe/vexflow) — the open-source music notation rendering library — designed to mimic the VexFlow API as closely as possible.

## Overview

VexFlowSharp brings VexFlow's music notation rendering to C# environments, including Unity via UI Toolkit. It targets the same class hierarchy and rendering pipeline as VexFlow v4.x, with method names translated to PascalCase and object literals replaced by C# structs. If you already know VexFlow JS, the C# API will feel immediately familiar.

> **Note:** This port is currently based on VexFlow **4.2.6**.

Current status: 546 NUnit tests passing (554 total, 8 skipped), approximately 27,000 lines of C#.

## Project Structure

| Library | Target | Purpose |
|---------|--------|---------|
| `VexFlowSharp.Common` | `netstandard2.1` | Core library — all notation classes, formatting engine, high-level API (`Factory`, `EasyScore`, `System`), and rendering abstractions. No platform-specific dependencies. |
| `VexFlowSharp.Skia` | `net10.0` | SkiaSharp-based `SkiaRenderContext` and `ImageComparison` utility. Shared rendering backend for tests and standalone .NET applications. |
| `VexFlowSharp.Skia.Linux` | `net10.0` | Platform package bundling `SkiaSharp.NativeAssets.Linux` for Linux deployment. |
| `VexFlowSharp.Skia.Windows` | `net10.0` | Platform package bundling `SkiaSharp.NativeAssets.Win32` for Windows deployment. |
| `VexFlowSharp.Skia.macOS` | `net10.0` | Platform package bundling `SkiaSharp.NativeAssets.macOS` for macOS deployment. |
| `VexFlowSharp.Skia.iOS` | `net10.0` | Platform package bundling SkiaSharp native assets for iOS deployment. |
| `VexFlowSharp.Skia.Android` | `net10.0` | Platform package bundling SkiaSharp native assets for Android deployment. |
| `VexFlowSharp.Skia.WebAssembly` | `net10.0` | Platform package bundling SkiaSharp native assets for Blazor/WASM deployment. |
| `VexFlowSharp.Tests` | `net10.0` | NUnit test suite with pixel-comparison testing against VexFlow JS reference images. |
| `VexFlowSharp.Unity` | Unity UPM package | `VexFlowElement` (custom `VisualElement`) and `UIElementsRenderContext` for Unity UI Toolkit / Painter2D rendering. Requires Unity 2022.1+. |

The `VexFlowSharp.Skia.*` platform packages exist because SkiaSharp requires platform-specific native assets. Reference the appropriate platform package for your target OS. For cross-platform builds, reference `VexFlowSharp.Skia` directly and add SkiaSharp native asset packages manually.

## Font Setup

VexFlowSharp uses the Bravura font (SMuFL standard) for all music glyphs. Call `Font.Load` once at startup before constructing any Factory:

```csharp
Font.Load("Bravura", BravuraGlyphs.Data);
```

The Bravura glyph data is compiled into `VexFlowSharp.Common` as a generated C# class — no external font files are needed at runtime.

## Quick Start (.NET / Console)

The following example renders a treble staff with four quarter notes using EasyScore. The `SkiaRenderContext` is defined in `VexFlowSharp.Skia`.

```csharp
using VexFlowSharp.Api;
using VexFlowSharp.Common.Formatting;

// Load the Bravura music font once at startup
Font.Load("Bravura", BravuraGlyphs.Data);

// Create the SkiaSharp render context (width x height in pixels)
var ctx = new SkiaRenderContext(width: 500, height: 200);

// Create a Factory bound to the context
var factory = new Factory(ctx, 500, 200);

// Create a System to manage layout
var system = factory.System(new SystemOptions { X = 10, Y = 40, Width = 480 });

// Use EasyScore DSL for concise note entry
var es = factory.EasyScore();
es.DefaultClef = "treble";
var notes = es.Notes("C5/q, D5, E5, F5");

// Create a 4/4 voice and add notes
var voice = factory.Voice(4, 4);
voice.AddTickables(notes.ConvertAll(n => (Tickable)n));

// Add the stave to the system with clef and time signature
var stave = system.AddStave(new SystemStave
{
    Voices = new List<Voice> { voice }
});
stave.AddClef("treble").AddTimeSignature("4/4");

// Draw everything in the correct order
factory.Draw();

// Save or display the SkiaSharp bitmap
ctx.SavePng("output.png");
```

## Quick Start (Unity)

In Unity, use `VexFlowElement` as a `VisualElement` inside a UI Toolkit hierarchy. The element holds a long-lived `UIElementsRenderContext` whose `Painter2D` reference is refreshed on every repaint by Unity's `generateVisualContent` callback.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VexFlowSharp.Api;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Unity;

public class SheetMusicDemo : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    void Start()
    {
        Font.Load("Bravura", BravuraGlyphs.Data);

        // Create and size the VexFlowElement
        var vfElement = new VexFlowElement();
        vfElement.style.width  = 800;
        vfElement.style.height = 300;

        // Pre-allocate Labels to avoid VisualElement mutations inside the
        // generateVisualContent callback (required if the score has text annotations)
        vfElement.PreAllocateLabels(32);

        uiDocument.rootVisualElement.Add(vfElement);

        // Build the Factory using the element's long-lived Context
        var ctx     = vfElement.Context;
        var factory = new Factory(ctx, 800, 300);

        var system = factory.System(new SystemOptions { X = 10, Y = 40, Width = 770 });

        var es = factory.EasyScore();
        es.DefaultClef = "treble";
        var notes = es.Notes("C5/q, D5, E5, F5");

        var voice = factory.Voice(4, 4);
        voice.AddTickables(notes.ConvertAll(n => (Tickable)n));

        var stave = system.AddStave(new SystemStave
        {
            Voices = new List<Voice> { voice }
        });
        stave.AddClef("treble").AddTimeSignature("4/4");

        // Render() stores the factory and triggers a repaint.
        // factory.Draw() is called inside the generateVisualContent callback.
        vfElement.Render(factory);
    }
}
```

## Grand Staff Example

The following example renders a piano grand staff (treble + bass) with a brace connector. This pattern is used in the `GrandStaffDemoController.cs` sample included in the Unity package.

```csharp
Font.Load("Bravura", BravuraGlyphs.Data);

var ctx     = vfElement.Context;
var factory = new Factory(ctx, 800, 400);

var system = factory.System(new SystemOptions { X = 10, Y = 40, Width = 770 });

// Treble stave
var es = factory.EasyScore();
es.DefaultClef = "treble";
var trebleNotes = es.Notes("C5/q, D5, E5, F5");

var trebleVoice = factory.Voice(4, 4);
trebleVoice.AddTickables(trebleNotes.ConvertAll(n => (Tickable)n));

var trebleStave = system.AddStave(new SystemStave
{
    Voices = new List<Voice> { trebleVoice }
});
trebleStave.AddClef("treble").AddTimeSignature("4/4");

// Bass stave
var esB = factory.EasyScore();
esB.DefaultClef = "bass";
var bassNotes = esB.Notes("C3/q, D3, E3, F3", new NoteOptions { Clef = "bass" });

var bassVoice = factory.Voice(4, 4);
bassVoice.AddTickables(bassNotes.ConvertAll(n => (Tickable)n));

var bassStave = system.AddStave(new SystemStave
{
    Voices = new List<Voice> { bassVoice }
});
bassStave.AddClef("bass").AddTimeSignature("4/4");

// Connect staves with a brace and single left bar line
system.AddConnector("brace");
system.AddConnector("singleLeft");

vfElement.Render(factory);
```

## Unity Samples

The `VexFlowSharp.Unity` package includes two ready-to-use samples in the `Samples~` folder:

| Sample | Description |
|--------|-------------|
| **GrandStaffDemo** | Renders a piano grand staff (treble + bass) with brace connector. Demonstrates `VexFlowElement`, `Factory`, `EasyScore`, and `System` usage. |
| **ComplexNotationDemo** | Renders a multi-measure score with accidentals, articulations, beams, ties, tuplets, dynamics, and other modifiers. Demonstrates the full range of VexFlowSharp notation features. |

Each sample includes a controller script, a Unity scene, and setup instructions. Import them via Unity Package Manager: select the VexFlowSharp.Unity package, expand Samples, and click Import.

## Low-Level API (Without EasyScore)

For full control over note construction, use `Factory.StaveNote` directly instead of EasyScore:

```csharp
// Create a C major chord (C4, E4, G4) quarter note
var note = factory.StaveNote(new StaveNoteStruct
{
    Keys     = new[] { "c/4", "e/4", "g/4" },
    Duration = "q"
});

// Add a sharp accidental to the second key (E4 -> E#4)
note.AddModifier(new Accidental("#"), 1);

// Add a staccato articulation
note.AddModifier(new Articulation("a."), 0);

// Beam a group of eighth notes
var n1 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "8" });
var n2 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "d/4" }, Duration = "8" });
var beam = factory.Beam(new List<StemmableNote> { n1, n2 });

// Tie two notes
var tie = factory.StaveTie(new TieNotes { FirstNote = n1, LastNote = n2 });
```

## Key Differences from VexFlow JS

| VexFlow JS | VexFlowSharp |
|---|---|
| `camelCase` methods (`addClef`, `addModifier`) | `PascalCase` methods (`AddClef`, `AddModifier`) |
| `note.setContext(ctx).draw()` — chainable | `note.SetContext(ctx); note.Draw();` — two separate calls (`SetContext` returns `Element`, not the concrete subtype) |
| `{ keys: ['c/4'], duration: 'q' }` object literal | `new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "q" }` C# object initializer |
| Accidentals inferred from key signature context | Accidentals must be explicitly added: `note.AddModifier(new Accidental("#"), keyIndex)` |
| `new Vex.Flow.Factory({ renderer: { elementId: 'div', width, height } })` | `new Factory(ctx, width, height)` — context is passed directly to the constructor |
| `require('vexflow')` / ESM import | Add `VexFlowSharp.Common` as a NuGet package or precompiled DLL reference |
| `vf.EasyScore().set({ time: '4/4' }).notes(...)` | `var es = factory.EasyScore(); es.DefaultClef = "treble"; es.Notes("C5/q, D5");` |

### SetContext and Draw are not chainable

In VexFlow JS, most elements return `this` from `setContext()`, enabling method chaining:

```js
// VexFlow JS
stave.setContext(ctx).draw();
```

In VexFlowSharp, `SetContext()` returns `Element` (the base class), not the concrete subtype. Chaining does not work without an explicit cast. Use two separate calls:

```csharp
// VexFlowSharp
stave.SetContext(ctx);
stave.Draw();
```

### Accidentals must be added explicitly

VexFlow JS can infer accidentals from the active key signature. VexFlowSharp does not apply accidentals automatically. You must attach them as modifiers:

```csharp
var note = factory.StaveNote(new StaveNoteStruct
{
    Keys     = new[] { "f/4" },
    Duration = "q"
});
note.AddModifier(new Accidental("#"), 0); // F# — index 0 = first key
```

## Draw Order

`factory.Draw()` executes the VexFlow draw pipeline in the required order:

1. `systems.Format()` — calculates note x-positions across all voices and staves
2. `staves.Draw()` — draws staff lines, clef, key signature, time signature
3. `voices.Draw()` — draws notes and rests
4. `renderQ.Draw()` — draws beams, ties, curves, dynamics, and other modifiers
5. `systems.Draw()` — draws connectors (brace, bracket, bar lines)
6. `Reset()` — clears builder state for the next render cycle

Always call `factory.Draw()` rather than drawing elements individually to ensure this order is preserved.

## What Is Implemented

VexFlowSharp.Common includes ports of the following VexFlow classes:

**Notation objects:** `StaveNote`, `GraceNote`, `GhostNote`, `TabNote`, `NoteHead`, `Rest`

**Stave modifiers:** `Clef`, `TimeSignature`, `KeySignature`, `Barline`, `StaveSection`, `StaveTempoSection`, `StaveText`

**Modifiers:** `Accidental`, `Articulation`, `Annotation`, `Dot`, `Ornament`, `Vibrato`, `VibratoBracket`, `NoteSubGroup`, `GraceNoteGroup`

**Connecting elements:** `Beam`, `StaveTie`, `Curve`, `StaveConnector`, `Tuplet`

**Dynamic markings:** `TextDynamics`, `Crescendo`

**Formatting engine:** `Formatter`, `TickContext`, `ModifierContext`, `Voice`, `Fraction`

**High-level API:** `Factory`, `EasyScore` (full DSL parser including chords and dots), `System`, `SystemStave`

**Font system:** `Font`, `Glyph`, `BravuraGlyphs` (SMuFL glyph data compiled into the assembly)

## Known Limitations

- **Cross-stave beaming** is not fully implemented. Beams within a single stave work correctly; beams that span two staves (common in piano music) require additional work.
- **Bravura only.** The font system is wired to the Bravura SMuFL font. Other fonts (Gonville, Petaluma) are not included.
- **Tab stave** notation is partially ported; full guitar tablature rendering has not been validated against reference images.

## Building

```sh
# Build all projects
dotnet build

# Run the test suite (546 passing)
dotnet test
```

### Adding to a Unity Project

1. Add `VexFlowSharp.Unity` as a UPM package (via the package manifest or Package Manager "Add package from disk").
2. Add `VexFlowSharp.Common.dll` to `Assets/Plugins/` as a precompiled assembly.
3. In the `VexFlowSharp.Unity.asmdef`, set `overrideReferences: true` and add `VexFlowSharp.Common.dll` to the `precompiledReferences` list.
4. Requires Unity 2022.1 or later (`Painter2D.BezierCurveTo` was introduced in 2022.1; `Painter2D.Arc` requires 2022.3 LTS or later).

### Adding to a .NET Project

Reference `VexFlowSharp.Common` as a project reference or NuGet package. For SkiaSharp-based rendering, also reference the appropriate `VexFlowSharp.Skia.*` platform package for your target OS (e.g., `VexFlowSharp.Skia.Linux` for Linux). The `SkiaRenderContext` class in `VexFlowSharp.Skia` provides a ready-to-use rendering backend.

## License

MIT (matching VexFlow's license).

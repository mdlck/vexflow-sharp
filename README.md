# VexFlowSharp

VexFlowSharp is a C# port of [VexFlow](https://www.vexflow.com/), the open-source music notation rendering library. Since VexFlow 5, the upstream source lives in the [VexFlow GitHub repository](https://github.com/vexflow/vexflow).

It brings the VexFlow 5 API shape to .NET and Unity while keeping the rendering backend-neutral. If you already know VexFlow, most concepts should feel familiar: `Factory`, `EasyScore`, `System`, voices, tickables, staves, modifiers, beams, ties, and the formatter all map closely to their JavaScript counterparts.

## At a Glance

- C# port targeting VexFlow 5.0.0 behavior.
- Core library targets `netstandard2.1`.
- SkiaSharp renderer for .NET applications and image generation.
- Unity UI Toolkit renderer for Unity 2022.1+.
- Built-in VexFlow font data. No external music font files are required at runtime for the core glyph data.
- NUnit test suite with VexFlow reference image comparisons for visual parity.

The public API is intentionally C#-idiomatic: VexFlow's `camelCase` methods and options become `PascalCase`, while the construction patterns stay close to upstream VexFlow where practical.

## Contents

- [Install](#install)
- [Quick Start: .NET and SkiaSharp](#quick-start-net-and-skiasharp)
- [Quick Start: Unity](#quick-start-unity)
- [Fonts](#fonts)
- [Project Layout](#project-layout)
- [API Notes for VexFlow Users](#api-notes-for-vexflow-users)
- [Implemented Surface](#implemented-surface)
- [Known Limitations](#known-limitations)
- [Build and Test](#build-and-test)
- [License](#license)

## Install

Reference the packages or projects that match your renderer:

| Scenario | Reference |
|---|---|
| Shared notation model and layout engine | `VexFlowSharp.Common` |
| .NET rendering with SkiaSharp | `VexFlowSharp.Skia` plus the right native asset package |
| Linux SkiaSharp deployment | `VexFlowSharp.Skia.Linux` |
| Windows SkiaSharp deployment | `VexFlowSharp.Skia.Windows` |
| macOS SkiaSharp deployment | `VexFlowSharp.Skia.macOS` |
| iOS SkiaSharp deployment | `VexFlowSharp.Skia.iOS` |
| Android SkiaSharp deployment | `VexFlowSharp.Skia.Android` |
| Blazor/WASM SkiaSharp deployment | `VexFlowSharp.Skia.WebAssembly` |
| Unity UI Toolkit rendering | `VexFlowSharp.Unity` |

For cross-platform .NET builds, reference `VexFlowSharp.Skia` directly and add the SkiaSharp native asset packages needed by your target platforms.

## Quick Start: .NET and SkiaSharp

This example renders a treble staff with four quarter notes and writes `output.png`.

```csharp
using System.Collections.Generic;
using VexFlowSharp;
using VexFlowSharp.Api;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Skia;

VexFlow.LoadFonts("Bravura", "Academico");

using var ctx = new SkiaRenderContext(width: 500, height: 200);
var factory = new Factory(ctx, 500, 200);

var system = factory.System(new SystemOptions
{
    X = 10,
    Y = 40,
    Width = 480
});

var easyScore = factory.EasyScore();
easyScore.DefaultClef = "treble";

var notes = easyScore.Notes("C5/q, D5, E5, F5");

var voice = factory.Voice(4, 4);
voice.AddTickables(notes.ConvertAll(note => (Tickable)note));

var stave = system.AddStave(new SystemStave
{
    Voices = new List<Voice> { voice }
});
stave.AddClef("treble").AddTimeSignature("4/4");

factory.Draw();
ctx.SavePng("output.png");
```

Use `EasyScore` for concise note entry, or drop down to `Factory.StaveNote(...)` when you need full control over keys, durations, modifiers, beams, ties, or other notation objects.

## Quick Start: Unity

In Unity, render through `VexFlowElement`, a custom UI Toolkit `VisualElement`. It owns a long-lived `UIElementsRenderContext`; Unity refreshes the underlying `Painter2D` reference during each repaint.

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VexFlowSharp.Api;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Unity;

public class SheetMusicDemo : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private void Start()
    {
        var element = new VexFlowElement
        {
            style =
            {
                width = 800,
                height = 300
            }
        };

        // Allocate labels up front if the score contains text annotations.
        element.PreAllocateLabels(32);
        uiDocument.rootVisualElement.Add(element);

        var factory = new Factory(element.Context, 800, 300);
        var system = factory.System(new SystemOptions { X = 10, Y = 40, Width = 770 });

        var easyScore = factory.EasyScore();
        easyScore.DefaultClef = "treble";

        var notes = easyScore.Notes("C5/q, D5, E5, F5");

        var voice = factory.Voice(4, 4);
        voice.AddTickables(notes.ConvertAll(note => (Tickable)note));

        var stave = system.AddStave(new SystemStave
        {
            Voices = new List<Voice> { voice }
        });
        stave.AddClef("treble").AddTimeSignature("4/4");

        element.Render(factory);
    }
}
```

The Unity package includes two samples in `Samples~`:

| Sample | What it demonstrates |
|---|---|
| `GrandStaffDemo` | Piano grand staff with treble and bass staves, brace connector, `Factory`, `EasyScore`, and `System`. |
| `ComplexNotationDemo` | A richer score with accidentals, articulations, beams, ties, tuplets, dynamics, and modifiers. |

Import samples through Unity Package Manager by selecting the `VexFlowSharp.Unity` package, expanding Samples, and clicking Import.

### Adding VexFlowSharp to Unity

1. Add `VexFlowSharp.Unity` as a UPM package.
2. Add `VexFlowSharp.Common.dll` to `Assets/Plugins/` as a precompiled assembly.
3. In `VexFlowSharp.Unity.asmdef`, enable `overrideReferences` and add `VexFlowSharp.Common.dll` to `precompiledReferences`.
4. Use Unity 2022.1 or later. `Painter2D.BezierCurveTo` requires 2022.1; `Painter2D.Arc` requires 2022.3 LTS or later.

## Fonts

Load fonts once at application startup before constructing a `Factory`.

```csharp
VexFlow.LoadFonts("Bravura", "Academico");
```

To use a different VexFlow font stack, load it and then select it:

```csharp
VexFlow.LoadFonts("Petaluma", "Petaluma Script");
VexFlow.SetFonts("Petaluma", "Petaluma Script");
```

Calling `VexFlow.LoadFonts()` with no arguments registers all built-in VexFlow fonts embedded in `VexFlowSharp.Common`.

Embedded font data and bundled Unity font assets keep their upstream licenses. Most VexFlow font packages use the SIL Open Font License 1.1; Roboto Slab uses Apache-2.0; Gonville has its own permissive font-output notice. Keep the third-party notices with redistributions.

## Project Layout

| Project | Target | Purpose |
|---|---|---|
| `VexFlowSharp.Common` | `netstandard2.1` | Core notation classes, formatter, high-level API, font data, and rendering abstractions. |
| `VexFlowSharp.Skia` | `net10.0` | SkiaSharp render context and image comparison utilities. |
| `VexFlowSharp.Skia.*` | `net10.0` | Platform packages that bundle SkiaSharp native assets. |
| `VexFlowSharp.Unity` | Unity UPM package | UI Toolkit renderer, `VexFlowElement`, and Unity samples. |
| `VexFlowSharp.Tests` | `net10.0` | NUnit unit, layout, renderer, and visual comparison tests. |
| `tools` | Node scripts | VexFlow reference-image and font-data generation helpers. |

## Rendering Scope

VexFlowSharp supports the backend-neutral `RenderContext` abstraction plus concrete SkiaSharp and Unity UI Toolkit renderers.

Browser-specific SVG, Canvas DOM, HTML element, CSS, event, and renderer bootstrap APIs are out of scope for the C# port unless a future renderer package adds them. For web-hosted .NET applications, use the Skia WebAssembly package or another `RenderContext` implementation instead of the JavaScript SVG/Canvas renderer surface.

## API Notes for VexFlow Users

| VexFlow JS | VexFlowSharp |
|---|---|
| `addClef(...)`, `addModifier(...)` | `AddClef(...)`, `AddModifier(...)` |
| `softmaxFactor`, `customPadding` | `SoftmaxFactor`, `CustomPadding` |
| Object literals such as `{ keys, duration }` | C# options objects such as `new StaveNoteStruct { Keys = ..., Duration = ... }` |
| `new Vex.Flow.Factory({ renderer: ... })` | `new Factory(ctx, width, height)` |
| SVG/Canvas DOM renderer setup | `SkiaRenderContext`, `UIElementsRenderContext`, or another C# `RenderContext` |
| `vf.EasyScore().set({ time: "4/4" }).notes(...)` | `var es = factory.EasyScore(); es.Set(new EasyScoreDefaults { Time = "4/4" }); es.Notes(...);` |

### SetContext and Draw Are Not Chainable

VexFlow JS often returns `this` from `setContext()`, which allows chaining:

```js
stave.setContext(ctx).draw();
```

In VexFlowSharp, `SetContext()` returns the base `Element` type. Use two calls:

```csharp
stave.SetContext(ctx);
stave.Draw();
```

For normal score rendering, prefer `factory.Draw()` so the formatter, staves, voices, modifiers, connectors, and render queue are drawn in the correct order.

### Accidentals Are Explicit

VexFlowSharp does not automatically infer accidentals from the active key signature. Add them as modifiers:

```csharp
var note = factory.StaveNote(new StaveNoteStruct
{
    Keys = new[] { "f/4" },
    Duration = "q"
});

note.AddModifier(new Accidental("#"), 0);
```

## Implemented Surface

`VexFlowSharp.Common` includes ports of the main VexFlow notation and layout types:

- Notation objects: `StaveNote`, `GraceNote`, `GhostNote`, `GlyphNote`, `RepeatNote`, `BarNote`, `ClefNote`, `TimeSigNote`, `KeySigNote`, `TextNote`, `TabNote`, `GraceTabNote`, `NoteHead`, `MultiMeasureRest`.
- Stave modifiers: `Clef`, `TimeSignature`, `KeySignature`, `Barline`, `StaveSection`, `StaveTempo`, `StaveText`, `Repetition`, `Volta`.
- Note modifiers: `Accidental`, `Articulation`, `Annotation`, `Dot`, `Ornament`, `Vibrato`, `VibratoBracket`, `NoteSubGroup`, `GraceNoteGroup`, `FretHandFinger`, `StringNumber`, `Stroke`, `Tremolo`, `Parenthesis`.
- Connecting elements: `Beam`, `StaveTie`, `StaveHairpin`, `StaveLine`, `TabTie`, `TabSlide`, `Curve`, `StaveConnector`, `Tuplet`, `TextBracket`.
- Dynamics and text: `TextDynamics`, `Crescendo`, `PedalMarking`, `ChordSymbol`.
- Formatting: `Formatter`, `TickContext`, `ModifierContext`, `Voice`, `Fraction`.
- High-level API: `Factory`, `EasyScore`, `System`, `SystemStave`.
- Fonts: `Font`, `Glyph`, and generated VexFlow-compatible font data.

## Known Limitations

- Cross-stave beaming is not fully implemented. Beams within a single stave work correctly; beams spanning two staves need additional work.
- Browser font loading is not ported. VexFlowSharp uses generated C# font data and `VexFlow.LoadFonts(...)`, not browser `FontFace` or remote `@vexflow-fonts` URLs.
- Visual parity tests compare SkiaSharp output with VexFlow 5 reference PNGs using documented cross-engine tolerances. MusicXML real-score excerpts remain C# output and smoke-test scenes because this repository does not include a local VexFlow 5 MusicXML reference renderer.

## Build and Test

The repository pins the .NET SDK in `global.json`.

```sh
dotnet build
dotnet test
```

Useful focused test runs:

```sh
dotnet test VexFlowSharp.sln --filter "FullyQualifiedName~PixelComparison"
dotnet test VexFlowSharp.sln --filter "Category=Comparison|Category=ImageComparison|Category=ImageCompare"
```

The current VexFlow 5 parity baseline is 966 NUnit tests passing. Maintainer-facing migration details live in:

- [`docs/vexflow-5-migration.md`](docs/vexflow-5-migration.md)
- [`docs/vexflow-5-visual-parity.md`](docs/vexflow-5-visual-parity.md)
- [`docs/vexflow-5-upstream-audit.md`](docs/vexflow-5-upstream-audit.md)

## License

VexFlowSharp source code is licensed under MIT; see [`LICENSE`](LICENSE).

This repository also contains or generates third-party material:

- VexFlow-derived porting and reference material, licensed under MIT.
- Generated VexFlow font data and bundled font binaries, primarily licensed under the SIL Open Font License 1.1.
- Roboto Slab font data, licensed under Apache-2.0.
- Gonville font data, covered by the upstream Gonville license notice.

See [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md) for copyright notices and license text. The Unity package also includes its own third-party notice file so UPM distributions can carry the relevant notices with the package.

# VexFlow 5 Visual Parity Gallery

This document records the visual parity policy, coverage, and verification commands for the VexFlowSharp v5 migration.

## Policy

Visual parity has two tiers:

1. Always-on renderer/layout tests that assert deterministic geometry, renderer calls, grouping, and generated output files.
2. Generated reference PNG comparisons against VexFlow 5, using a cross-engine tolerance because SkiaSharp and browser/node-canvas anti-aliasing differ even when layout is correct.

Default pixel comparison policy:

- Image dimensions must match exactly.
- `ImageComparison.PerChannelTolerance = 5`.
- `ImageComparison.DefaultThresholdPercent = 2.0`.
- Cross-engine tests use `ImageComparison.CrossEngineThresholdPercent = 11.0%` when comparing SkiaSharp output with VexFlow/node-canvas references.
- Any failure must write or preserve both actual and reference PNGs for side-by-side inspection.

## Existing Coverage

| Area | Current coverage | Status |
|---|---|---:|
| Pixel diff utility | `ImageComparisonTests` cover identical images, threshold failures, dimension mismatch, and default threshold. | active |
| Formatter x-position parity | `FormatterRenderingTests` assert freshly generated VexFlow 5 reference x-positions for single voice, two voices, beams, and chromatic accidentals. The generator now registers Bravura and uses v5 `stemDirection` options. | active |
| Formatter PNG comparisons | `SingleVoice_4_4_PixelComparison`, `TwoVoice_PixelComparison`, `BeamEighthNotes_PixelComparison`, and `ChromaticAccidentals_PixelComparison` compare generated VexFlow/C# PNG pairs with the cross-engine tolerance. | active |
| Complex notation PNG comparison | `ComplexNotation_PixelComparison` compares the generated VexFlow/C# PNG pair with the cross-engine tolerance. | active |
| Gallery PNG comparisons | `GalleryTabNotation_PixelComparison`, `GalleryGraceNotes_PixelComparison`, `GallerySmallModifiers_PixelComparison`, and `GalleryStaveModifiers_PixelComparison` compare generated VexFlow/C# PNG pairs with the cross-engine tolerance. | active |
| Percussion PNG comparisons | `PercussionClef_PixelComparison`, `PercussionBasic0_PixelComparison`, `PercussionBasic1_PixelComparison`, `PercussionBasic2_PixelComparison`, `PercussionSnare0_PixelComparison`, and `PercussionSnare1_PixelComparison` compare generated VexFlow/C# PNG pairs with the cross-engine tolerance. | active |
| Comparison output scenes | Beethoven, Schubert, complex notation, and percussion comparison tests render C# PNGs into `VexFlowSharp.Tests/Comparison/Output/` for side-by-side inspection. | active output |
| Grand-staff PNG comparisons | `GrandStaffRenders_PixelComparison`, `FactoryGrandStaffRenders_PixelComparison`, and `EasyScoreGrandStaffRenders` compare generated VexFlow/C# PNG pairs with the cross-engine tolerance. | active |
| Gallery reference generation | `tools/gen-reference-images.mjs` now emits VexFlow 5 node-canvas references for grand-staff, complex notation, percussion scenes, tab notation, grace notes, small modifiers, and stave modifiers in addition to the formatter references. | generated local |
| Gallery C# output scenes | `VisualGalleryComparisonTests` renders C# PNGs for tab notation, grace notes, small modifiers, and stave modifiers into `VexFlowSharp.Tests/Comparison/Output/`. | active output |
| Renderer-call visual behavior | Modifier/note/stave tests assert renderer calls for migrated modules such as dots, articulations, ornaments, bends, pedal markings, text brackets, stave lines, tab notation, hairpins, tuplets, and pointer rectangles. | active |

## Representative Gallery

The visual parity gate uses these representative scenes with current VexFlow 5 reference PNGs and either active generated comparisons or documented side-by-side inspection results:

| Scene | Coverage goal | Reference requirement | Status |
|---|---|---|---:|
| Single voice 4/4 | Basic stave, clef, time signature, note spacing. | VexFlow 5 PNG matching `FormatterRenderingTests.SingleVoice_4_4`. | generated local |
| Two voice measure | Shared tick positions and collision-free stems. | VexFlow 5 PNG matching `FormatterRenderingTests.TwoVoice`. | generated local |
| Beamed eighth notes | Beam grouping, stem directions, beam geometry. | VexFlow 5 PNG matching `FormatterRenderingTests.BeamEighthNotes`. | generated local |
| Chromatic accidentals | Accidental widths, formatting, natural/flat/sharp placement. | VexFlow 5 PNG matching `FormatterRenderingTests.ChromaticAccidentals`. | generated local |
| Grand staff | System, Factory, and EasyScore grand-staff layout with connectors. | `system_grandstaff-vexflow.png`, `factory_grandstaff-vexflow.png`, and `easyscore_grandstaff-vexflow.png`. | active |
| Complex notation | Accidentals, dotted rhythm, beam, tie, formatter spacing. | `complex_notation-vexflow.png` alongside `complex_notation-vfsharp.png`. | generated local |
| Percussion scenes | Percussion clef, two-voice kit patterns, mixed noteheads, sticking annotations, and hi-hat articulations. | `percussion_*-vexflow.png` alongside `percussion_*-vfsharp.png`. | generated + C# output |
| Tab notation | Tab stave, tab notes, muted glyphs, dots, stems, slides/ties. | `gallery_tab_notation-vexflow.png` and `gallery_tab_notation-vfsharp.png`. | generated + C# output |
| Grace notes | Grace-note group, beam, slur, modifier spacing. | `gallery_grace_notes-vexflow.png` and `gallery_grace_notes-vfsharp.png`. | generated + C# output |
| Small modifiers | Tremolo, fret-hand fingering, text bracket, pedal marking, chord symbol, vibrato. | `gallery_small_modifiers-vexflow.png` and `gallery_small_modifiers-vfsharp.png`. | generated + C# output |
| Stave modifiers | Tempo, text, volta, repetition, section, connectors. | `gallery_stave_modifiers-vexflow.png` and `gallery_stave_modifiers-vfsharp.png`. | generated + C# output |
| MusicXML real score excerpts | Beethoven and Schubert scenes already emit C# outputs. | No local VexFlow 5 MusicXML reference renderer exists in this repository; retained as C# output/smoke scenes. | scoped output |

## Commands

Run always-on visual/layout coverage:

```sh
dotnet test VexFlowSharp.sln --filter "Category=Comparison|Category=ImageComparison|Category=ImageCompare"
```

Run generated pixel comparisons:

```sh
dotnet test VexFlowSharp.sln --filter "FullyQualifiedName~PixelComparison"
```

Run full verification:

```sh
dotnet test VexFlowSharp.sln
```

## Scoped Notes

- No generated VexFlow 5 visual comparison scenes remain outside the normal test run.
- MusicXML real-score excerpts remain C# output/smoke scenes because this repository has no local VexFlow 5 MusicXML reference renderer.

## Local Diff Snapshot

After normalizing the C# comparison outputs to a white background, a local per-channel-tolerance diff pass reports:

| Pair | Dimensions | Diff |
|---|---:|---:|
| `complex_notation` | 600x200 | 5.69% |
| `percussion_clef` | 400x120 | 6.24% |
| `percussion_basic0` | 500x200 | 6.92% |
| `percussion_basic1` | 500x200 | 6.04% |
| `percussion_basic2` | 500x200 | 7.09% |
| `percussion_snare0` | 500x200 | 6.00% |
| `percussion_snare1` | 500x200 | 5.81% |
| `gallery_tab_notation` | 700x190 | 8.20% |
| `gallery_grace_notes` | 760x170 | 8.23% |
| `gallery_small_modifiers` | 760x250 | 8.38% |
| `gallery_stave_modifiers` | 760x230 | 6.14% |
| `factory_grandstaff` | 600x250 | 10.61% |
| `easyscore_grandstaff` | 600x250 | 10.20% |

These are below the current 11.0% cross-engine threshold. Bounding-box diagnostics are within a few pixels for the gallery/percussion scenes; the remaining diff is dominated by SkiaSharp vs. node-canvas ink-density differences.

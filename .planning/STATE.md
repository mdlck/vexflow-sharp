---
gsd_state_version: 1.0
milestone: v1.0
milestone_name: milestone
status: Milestone v1.0 archived
stopped_at: Completed quick-260328-cv5 (update README.md to reflect all current project state)
last_updated: "2026-03-28T08:19:59.715Z"
last_activity: "2026-03-28 - Completed quick task 260328-c1d: clean up project for GitHub publishing: remove unnecessary files, commit pending changes"
progress:
  total_phases: 6
  completed_phases: 5
  total_plans: 30
  completed_plans: 29
  percent: 100
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-23 after v1.0 milestone)

**Core value:** Render correct, visually indistinguishable sheet music in Unity using a C# API that mirrors VexFlow closely enough that existing VexFlow documentation serves as the reference.
**Current focus:** v1.0 shipped — planning next milestone

## Current Position

Phase: 6 of 6 (complete)
Status: Milestone v1.0 archived
Last activity: 2026-03-28 - Completed quick task 260328-c1d: clean up project for GitHub publishing: remove unnecessary files, commit pending changes

Progress: [██████████] 100%

## Performance Metrics

**Velocity:**
- Total plans completed: 0
- Average duration: -
- Total execution time: 0 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

**Recent Trend:**
- Last 5 plans: n/a
- Trend: n/a

*Updated after each plan completion*
| Phase 01-foundation P01 | 5 | 1 tasks | 14 files |
| Phase 01-foundation P04 | 6 | 3 tasks | 6 files |
| Phase 01-foundation P02 | 7 | 2 tasks | 11 files |
| Phase 01-foundation P05 | 3 | 2 tasks | 4 files |
| Phase 01-foundation P03 | 4 | 2 tasks | 5 files |
| Phase 02-core-notation-hierarchy P01 | 8 | 2 tasks | 10 files |
| Phase 02-core-notation-hierarchy P02 | 7 | 2 tasks | 14 files |
| Phase 02-core-notation-hierarchy P03 | 22 | 2 tasks | 7 files |
| Phase 02-core-notation-hierarchy P04 | 8 | 2 tasks | 5 files |
| Phase 02-core-notation-hierarchy P05 | 18 | 2 tasks | 8 files |
| Phase 03-formatting-engine P01 | 35 | 2 tasks | 10 files |
| Phase 03-formatting-engine P02 | 25 | 2 tasks | 5 files |
| Phase 03-formatting-engine P03 | 20 | 2 tasks | 4 files |
| Phase 03-formatting-engine P04 | 35 | 2 tasks | 6 files |
| Phase 03-formatting-engine P05 | ~180min | 2 tasks | 14 files |
| Phase 04-standard-modifiers P00 | 8min | 2 tasks | 19 files |
| Phase 04-standard-modifiers P01 | 45 | 2 tasks | 8 files |
| Phase 04-standard-modifiers P03 | 5 | 2 tasks | 8 files |
| Phase 04-standard-modifiers P04-02 | 8 | 2 tasks | 6 files |
| Phase 04-standard-modifiers P04-04 | 30 | 2 tasks | 7 files |
| Phase 04-standard-modifiers P05 | 25 | 2 tasks | 7 files |
| Phase 05-high-level-api-and-system-layout P00 | 15min | 2 tasks | 11 files |
| Phase 05-high-level-api-and-system-layout P01 | 35 | 2 tasks | 3 files |
| Phase 05-high-level-api-and-system-layout P02 | 35 | 3 tasks | 7 files |
| Phase 05-high-level-api-and-system-layout P03 | 15 | 2 tasks | 3 files |
| Phase 05-high-level-api-and-system-layout P04 | 10min | 2 tasks | 5 files |
| Phase 06-unity-uielements-renderer P01 | 2min | 2 tasks | 5 files |
| Phase 06-unity-uielements-renderer P02 | 2min | 2 tasks | 3 files |
| Phase 01-foundation P06 | 5 | 1 tasks | 9 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Pre-Phase 1]: Painter2D availability in Unity 2021.3 LTS vs 2022.1 LTS is unverified — Phase 6 planning must resolve before implementation begins. Research flagged this as scope-altering.
- [Pre-Phase 1]: BravuraGlyphs code generator (Node.js script bravura_glyphs.ts → BravuraGlyphs.cs) is a Phase 1 deliverable; decide whether the script is committed to the repo or run once.
- [Pre-Phase 1]: Typography.OpenFont API method names need verification against current NuGet package before font integration code is written.
- [Phase 01-foundation]: Tests target net10.0 not net8.0: machine only has .NET 10 runtime; net8.0 testhost cannot execute without .NET 8 runtime; plan's intent of concrete runnable TFM is satisfied
- [Phase 01-foundation]: Classic .sln format required: dotnet 10 defaults to .slnx; used --format sln flag to match plan's VexFlowSharp.sln filename requirement
- [Phase 01-foundation]: OpenGroup/CloseGroup are virtual no-ops in RenderContext base — SVG grouping not needed for Canvas/Skia backends
- [Phase 01-foundation]: SkiaSharp.NativeAssets.Linux added to Tests csproj — required for native libSkiaSharp.so to load on Linux test runner
- [Phase 01-foundation]: SkiaRenderContext placed in Tests project, not Common — keeps SkiaSharp dependency isolated from Unity/Common
- [Phase 01-foundation]: Fraction uses LCM-based Add to match VexFlow fraction.ts; Element.context typed as object? placeholder until plan 04 RenderContext; Tickable.modifiers as List<object> until Phase 2 Modifier
- [Phase 01-foundation]: AssertImagesMatch depends on NUnit.Framework.Assert - acceptable because ImageComparison lives in the Tests project
- [Phase 01-foundation]: gen-reference-images.mjs is a Phase 1 placeholder using simple rect; full VexFlow rendering added in Phase 2+
- [Phase 01-foundation]: BravuraGlyphs code generator parses bravura_glyphs.ts as text via regex — no tsx/transpiler needed since all values are literals
- [Phase 01-foundation]: Glyph outline walker pre-parsed int[] arrays — z tokens skipped matching VexFlow behavior; paths filled not stroked-closed
- [Phase 02-core-notation-hierarchy]: Modifier.note typed as Element? until Note.cs exists in plan 02-03; will be narrowed then
- [Phase 02-core-notation-hierarchy]: Test namespaces use .TableTests not .Tables to avoid C# static class name collision
- [Phase 02-core-notation-hierarchy]: Glyph.SetYShift() added to enable StaveModifier.PlaceGlyphOnLine() positioning
- [Phase 02-core-notation-hierarchy]: Tables.AccidentalCodes() added as static method to support KeySignature glyph lookup
- [Phase 02-core-notation-hierarchy]: NoteHead extends Element (not Note) in Phase 2 to avoid forward-reference cycle; will be reparented in Phase 3
- [Phase 02-core-notation-hierarchy]: Note.GetAbsoluteX() returns x when tickContext is null (Phase 2 Pitfall 4 guard); Phase 3 will extend with TickContext
- [Phase 02-core-notation-hierarchy]: StemmableNote.AutoStem uses average keyProps line vs median 3.0: >= 3 = DOWN, < 3 = UP
- [Phase 02-core-notation-hierarchy]: StaveNote.Draw() call order exactly mirrors VexFlow stavenote.ts: ledger lines drawn first (behind noteheads), then stem, noteheads, flag
- [Phase 02-core-notation-hierarchy]: Note.SetStave() guarded with try/catch for context — stave may have no RenderContext in unit tests
- [Phase 02-core-notation-hierarchy]: StaveNote.minNoteheadPadding=12.0 added to satisfy GraceNoteGroup.GetWidth() — matches VexFlow stavenote.ts
- [Phase 02-core-notation-hierarchy]: GraceNoteGroup PreFormat() stubs width without Voice/Formatter; Format() no-op per Pitfall 6; Phase 3 responsibility
- [Phase 02-core-notation-hierarchy]: StaveNote rendering tests are smoke tests — canvas npm unavailable for VexFlow reference PNG generation; pixel-diff tests deferred to Phase 3+
- [Phase 02-core-notation-hierarchy]: SetContext() returns Element not subtype — SetContext() and Draw() must be separate calls, not chained
- [Phase 03-formatting-engine]: Tickable.PreFormat declared void (not Tickable return) to match existing StaveNote/GhostNote signatures
- [Phase 03-formatting-engine]: Note.GetAbsoluteX uses inherited GetTickContext() from Tickable; removed shadowing object? tickContext field
- [Phase 03-formatting-engine]: Voice.Draw casts Tickable to Note for SetStave since Tickable base class does not expose SetStave
- [Phase 03-formatting-engine]: StaveNote.CATEGORY='stavenotes' (plan spec); ModifierContext.members typed as Dictionary<string,List<Element>> (no C# union types); 14 Phase-4 no-op dispatchers in formatters list
- [Phase 03-formatting-engine]: Note.GetAbsoluteX adds stave.GetNoteStartX() + STAVE_PADDING when stave is set — matches VexFlow note.ts getAbsoluteX() which was missing in Phase 2 port
- [Phase 03-formatting-engine]: Formatter.CreateModifierContexts uses int stave-identity index (not Stave? key) to avoid C# Dictionary null-key ArgumentNullException
- [Phase 03-formatting-engine]: Formatter autoBeam path deferred to plan 03-04; AlignRestsToNotes stub deferred to Phase 4 (requires Note.GetLineForRest)
- [Phase 03-formatting-engine]: Beam tier logic: getBeamLines(dur) uses intrinsicTicks < DurationToTicks(dur) strictly; primary tier '4' covers 8ths and shorter
- [Phase 03-formatting-engine]: StaveNote.beam field removed (was object?) — replaced by inherited Beam? from StemmableNote; StaveNote.SetBeam() preserves CalcNoteDisplacements side effect
- [Phase 03-formatting-engine]: TextFormatter uses two-level cache (family+size → FontInfo, text → width-in-em); fontSizeInPixels = sizeInPt * (4/3)
- [Phase 03-formatting-engine]: Glyph.GetWidth() scans bezier outline control points (not XMax-XMin) to match VexFlow bbox.getW(); scale = (point*72)/(resolution*100)
- [Phase 03-formatting-engine]: StaveNote.minNoteheadPadding corrected to 2.0 (was 12.0); PreFormat adds this padding when modifierContext.GetWidth()==0
- [Phase 03-formatting-engine]: Stave.endX = x+width when single end modifier (port of VexFlow stave.ts end_x logic)
- [Phase 03-formatting-engine]: ChromaticAccidentals test uses 4px tolerance for Phase 3 (ModifierContext accidental formatting deferred to Phase 4)
- [Phase 04-standard-modifiers]: renderer.resize() must not be called with node-canvas — Qt.resize() sets canvas.width inside VexFlow strict-mode bundle which fails; pre-size via createCanvas(w,h) instead
- [Phase 04-standard-modifiers]: generate_phase4_refs.js path is ../../../vexflow/ (3 levels up from ReferenceImages/ to project root); Voice.Mode.SOFT needed for non-standard beat counts in tuplet demos
- [Phase 04-standard-modifiers]: ModifierContext.RegisterMember (one-way) used instead of AddMember to avoid circular callback when Tickable.AddToModifierContext registers modifiers
- [Phase 04-standard-modifiers]: NotePx=width preserved in GetNoteMetrics to match VexFlow reference calibration; HeadWidth causes 1px formatter drift
- [Phase 04-standard-modifiers]: Accidental modifiers must be explicitly added to notes in tests (AddModifier) — not auto-applied from keyProps; ChromaticAccidentals test updated accordingly
- [Phase 04-standard-modifiers]: Curve uses cubic bezier (BezierCurveTo); StaveTie uses two quadratic passes (QuadraticCurveTo) matching respective VexFlow sources
- [Phase 04-standard-modifiers]: StaveTieRenderOptions.Cp1/Cp2 default to 36 (plan spec) vs VexFlow 8/12 for larger arcs in C# context
- [Phase 04-standard-modifiers]: ModifierContext Articulation/Ornament at slots 9/10 (0-based) not 6/8 — plan used simplified slot map; actual impl has more stubs between Accidental and Articulation
- [Phase 04-standard-modifiers]: Glyph height approximated as 1.5 line units in Articulation.Format() — pre-render GetMetrics() not available; Ornament.AccidentalLower/Upper width uses GetWidth() with stored code string
- [Phase 04-standard-modifiers]: TextDynamics/Crescendo extend Note (not Modifier): GetWidth uses 'new' keyword; rendered=true directly; preFormatted is local field per class
- [Phase 04-standard-modifiers]: TupletTests use fully qualified VexFlowSharp.Note to avoid clash with VexFlowSharp.Tests.Note namespace
- [Phase 04-standard-modifiers]: AnnotationVerticalJustify enum: ABOVE=1, BELOW=2 (plan spec, not VexFlow TOP=1, BOTTOM=3) — required for test assertions
- [Phase 04-standard-modifiers]: VibratoBracket constructor takes positional Note? start/stop args (not bracket_data struct) — simpler C# API
- [Phase 04-standard-modifiers]: Vibrato.Format() 3-arg signature confirmed: Format(vibratos, state, this) — required when formatter needs sibling modifier context
- [Phase 05-high-level-api-and-system-layout]: namespace VexFlowSharp.Tests.SystemTests (not .System) and StaveConnectorTests (not .Stave) to avoid C# namespace/class collision — same pattern as Phase 2 TableTests
- [Phase 05-high-level-api-and-system-layout]: VexFlow System requires Factory context — new System() throws NoFactory error; must use vf.System() from Factory instance
- [Phase 05-high-level-api-and-system-layout]: System.Format() re-assigns stave to tickables via Note cast after SetY to propagate updated Y positions
- [Phase 05-high-level-api-and-system-layout]: GrandStaffRenders uses structural assertions (noteStartX aligned, Y stacked); pixel comparison in [Explicit] GrandStaffRenders_PixelComparison — cross-engine SkiaSharp/node-canvas diff is always 99%+
- [Phase 05-high-level-api-and-system-layout]: VexFlowSharp.Common.Formatting.System fully qualified in tests to avoid C# System namespace ambiguity
- [Phase 05]: StaveConnectorType enum allows Single = SingleLeft = 1 as duplicate int value — C# permits multiple names to share underlying int
- [Phase 05]: Bracket connector renders via new Glyph('bracketTop', 40).Render(ctx, x, y) — plan's Glyph.RenderGlyph() static method does not exist; instance Render() used instead
- [Phase 05]: NoteSubGroup.AlignSubNotesWithNote uses TickContext.SetXOffset matching VexFlow modifier.ts logic: x = tickContext.GetX() - modLeftPx - modRightPx + spacingFromNextModifier
- [Phase 05]: Factory.Draw() Voice loop calls v.Draw(context) not v.SetContext(ctx).Draw() — Voice does not extend Element; no SetContext on Voice
- [Phase 05]: Factory.EasyScore() returns stub EasyScore with Factory back-reference — full implementation in Plan 05-04
- [Phase 05]: FactoryTests use structural assertions (noteStartX, Y stacking) not pixel comparison — SkiaSharp vs node-canvas diff always 99%+
- [Phase Phase 05]: Parser.FlattenMatches: ParseResult with MatchedString is leaf; with Results recurses; empty returns null — mirrors VexFlow flattenMatches() semantics
- [Phase Phase 05]: EasyScore NOTE() rule has Run trigger same as SINGLENOTE() — needed because CHORD processes notes via NOTE rule; duration normalization: q->4, h->2, w->1 via SanitizeDuration
- [Phase Phase 05]: Dot.BuildAndAttach() added to Dot.cs — was missing from C# port, required by EasyScore.CommitPiece(); auto-fixed as Rule 3 (blocking issue)
- [Phase 06-unity-uielements-renderer]: UIElementsRenderContext uses 'SysNumerics' alias for System.Numerics to resolve Vector2 ambiguity with UnityEngine.Vector2
- [Phase 06-unity-uielements-renderer]: VexFlowElement.cs added alongside UIElementsRenderContext.cs -- required type for constructor injection; plan implicitly required it via interface contract
- [Phase 06-unity-uielements-renderer]: asmdef overrideReferences: true required for precompiledReferences array to take effect (VexFlowSharp.Common.dll)
- [Phase 06]: VexFlowElement.Context exposes long-lived UIElementsRenderContext for Factory construction -- Factory bakes context at construction time so context must outlive the callback
- [Phase 06]: SetPainter(null!) after Draw() clears stale Painter2D reference to prevent silent drawing into a dead painter outside the callback
- [Phase 01-foundation]: Beam.unbeamable field removed (CS0649): field was never assigned, guard was always false dead code; removed with Draw() guard
- [Phase 01-foundation]: Barline.thickness field removed (CS0414): assigned in constructor but never read; dead port artifact removed
- [Phase quick-260326-qvr]: GraceNoteGroup.Draw() uses Voice+Formatter for grace note positioning (fixes Y values bug)
- [Phase quick-260326-qvr]: EnableNUnitRunner=true required for NUnit 6.x + .NET 10 SDK test execution
- [Phase quick-260327-u9h]: CopyLocalLockFileAssemblies=true required for SDK-style .csproj so NuGet dependency DLLs appear in ReferenceCopyLocalPaths and get copied to Unity Plugins/

### Pending Todos

None yet.

### Blockers/Concerns

- Unity minimum version: PROJECT.md states 2021.3 LTS minimum but Painter2D.BezierCurveTo requires 2022.1+. Must be resolved before Phase 6 begins. (Source: research/SUMMARY.md)
- SkiaSharp patch version and Typography.OpenFont exact API surface need NuGet verification before Phase 1 coding begins.

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 260323-rbi | add complex comparison test renderings for VexFlow vs VexFlowSharp | 2026-03-23 | 477451a | [260323-rbi-add-complex-comparison-test-renderings-f](./quick/260323-rbi-add-complex-comparison-test-renderings-f/) |
| 260323-rns | fix rendering differences between VexFlow JS and VexFlowSharp C# | 2026-03-23 | 44921f9 | [260323-rns-fix-rendering-differences-between-vexflo](./quick/260323-rns-fix-rendering-differences-between-vexflo/) |
| 260325-nto | render BeetAnGeSample MusicXML as VexFlow JS + VexFlowSharp comparison PNGs | 2026-03-25 | b1af2f8 | [260325-nto-render-beetangesample-musicxml-as-vexflo](./quick/260325-nto-render-beetangesample-musicxml-as-vexflo/) |
| 260325-o56 | fix time signature numbers rendering one staff line too low | 2026-03-25 | 4b2744b | [260325-o56-fix-time-signature-numbers-rendering-low](./quick/260325-o56-fix-time-signature-numbers-rendering-low/) |
| 260325-opl | fix missing bar lines in BeetAnGeSample comparison PNGs | 2026-03-25 | 666a0d6 | [260325-opl-fix-missing-bar-lines-in-beetangesample-](./quick/260325-opl-fix-missing-bar-lines-in-beetangesample-/) |
| 260325-tw3 | add README.md describing VexFlowSharp project | 2026-03-25 | 1328569 | [260325-tw3-add-readme-md-describing-vexflowsharp-pr](./quick/260325-tw3-add-readme-md-describing-vexflowsharp-pr/) |
| 260325-u3y | divide VexFlowSharp.Tests into Test + Skia library | 2026-03-25 | 473ae62 | [260325-u3y-divide-vexflowsharp-tests-into-the-test-](./quick/260325-u3y-divide-vexflowsharp-tests-into-the-test-/) |
| 260325-ukh | multi-target VexFlowSharp.Skia for all platforms | 2026-03-26 | 32a77fc | [260325-ukh-vexflowsharp-skia-project-seems-to-be-on](./quick/260325-ukh-vexflowsharp-skia-project-seems-to-be-on/) |
| 260327-so8 | add per-OS VexFlowSharp.Skia projects for every platform | 2026-03-27 | 6443a0b | [260327-so8-add-vexflowsharp-skia-projects-for-every](./quick/260327-so8-add-vexflowsharp-skia-projects-for-every/) |
| 260327-u0s | add ComplexNotationDemo Unity sample matching ComplexNotationComparisonTest | 2026-03-27 | e19aa9f | [260327-u0s-take-the-input-from-the-comparison-test-](./quick/260327-u0s-take-the-input-from-the-comparison-test-/) |
| 260327-u9h | copy common dll to unity plugins directory on build | 2026-03-27 | 26373ce | [260327-u9h-copy-common-dll-to-unity-plugins-directo](./quick/260327-u9h-copy-common-dll-to-unity-plugins-directo/) |
| 260328-bo5 | fix dotnet build errors for iOS/macCatalyst/Android on Linux | 2026-03-28 | 500e471 | [260328-bo5-fix-dotnet-build-errors-for-ios-and-macc](./quick/260328-bo5-fix-dotnet-build-errors-for-ios-and-macc/) |
| 260328-c1d | clean up project for GitHub publishing — delete debug scripts, update .gitignore, commit pending changes | 2026-03-28 | 165658d | [260328-c1d-clean-up-project-for-github-publishing-r](./quick/260328-c1d-clean-up-project-for-github-publishing-r/) |

## Session Continuity

Last session: 2026-03-28T08:19:59.707Z
Stopped at: Completed quick-260328-cv5 (update README.md to reflect all current project state)
Resume file: None

# VexFlow 5.0.0 Migration Plan

## Purpose

This document records the completed work that brought the C# port to VexFlow 5.0.0 parity.

The intended reader is a maintainer auditing the migration after completion. The detailed work-area sections below are retained as historical context for what was ported, tested, scoped out, or intentionally mapped differently in C#.

## Completion Status

The scoped VexFlow 5 migration is complete for this repository:

- All generated VexFlow 5 visual comparison scenes run in the normal test suite.
- No NUnit tests are skipped.
- The upstream commit audit has no unexplained `remaining` entries for the local `4.2.2..5.0.0` range.
- Browser/SVG/DOM renderer behavior and alternate upstream music-font runtime assets are documented as out of scope for the current C# rendering model.
- MusicXML real-score excerpts are retained as C# output/smoke scenes because this repository has no local VexFlow 5 MusicXML reference renderer.

## Current State

The migration has already advanced several core behavior areas:

- Key signature compatibility, including expanded `flats_N` / `sharps_N` specs and cancellation behavior.
- Tuplet tick multiplier handling, note attachment, detachment, nesting, and bounding boxes.
- Formatter and tick-context fixes for duration statistics, deviation cost, context movement, tuning, and rest alignment.
- Table updates for key signatures, accidentals, ornaments, notehead stem anchors, and v5 glyph-name passthroughs.
- Element style application with context save/restore and dashed-line support.
- Curve rendering for dashed and non-dashed curves, with default geometry now metric-backed.
- Stave line bounds and default ledger-line styling.
- StaveNote fixes for x-notehead stem anchors, unison/displaced notehead handling, rest alignment support, notehead/stave-note bounding boxes, and ledger-line style inheritance.
- Fraction helpers added for v5 API compatibility.
- Fraction addition now preserves v5-style LCM denominators, including zero-numerator additions used by Voice resolution expansion.
- A first-pass VexFlow 5 `Metrics` equivalent was added with root/parent/specific fallback lookup, font-info lookup, style lookup, and shared stave/accidental/notehead/dot/curve/stave-tie/tuplet/annotation/ornament/stem/fret-hand-fingering/string-number/tremolo/parenthesis/stroke/stave-section/stave-text/volta/repetition/stave-tempo/text-bracket padding and offset call sites.
- Bravura glyph data now includes the VexFlow 5 metronome glyphs needed by stave tempo, and the generator can augment the existing C# table from the vendored Bravura SVG when the legacy VexFlow outline table is absent.
- Stave tempo, stave lines, text brackets, glyph notes, repeat notes, text notes, multi-measure rests, key signature note wrappers, time signature note wrappers, clef note wrappers, pedal markings, chord symbols, bends, vibrato/vibrato brackets, tab staves, tab notes, grace tab notes, tab ties, tab slides, and tablature tunings have first-pass VexFlow 5 ports with focused renderer/API tests. Tab notes now also cover VexFlow 5-style stem-through-stave gaps, muted-string glyph rendering, multi-string tab slide rendering, tab slide labels, stave-context inheritance, grouped class/id attributes, beamed stem suppression, tab-dot stem-base placement, and metric-backed tab stave/grace-tab/tie/slide defaults.
- Annotation now supports VexFlow 5-style justification string aliases, center and center-stem placement, measured text width, stem-aware vertical draw placement, attached-note overlap-aware formatter shifts, and render group calls.
- Articulation now uses glyph height metrics for formatter increments, includes the VexFlow 5 `SetBetweenLines()` override without mutating shared table defaults, uses upstream-style top/bottom Y and initial-offset helpers for stave/tab notes, skips staff snapping for tab notes, and applies v5-style horizontal overlap shifts.
- Accidental now recomputes VexFlow 5 metric font sizes on cautionary and grace-note attachment reset, uses metric-backed cautionary parenthesis padding, has renderer coverage for cautionary parenthesis glyphs, and has alias/direct-glyph-name coverage for the migrated accidental table.
- Ornament now applies VexFlow 5 jazz ornament left/right constructor placement, attachment-time articulation ornament side selection, delayed ornament stave-end/next-context fallback positioning, centered above/below glyph rendering, metric-backed formatter/accidental spacing, glyph-height stacked articulation ornament spacing, render group calls, and renderer coverage for ornament accidentals.
- Stave now exposes VexFlow 5-style category and line-configuration APIs, validates per-line visibility configuration, renders hidden stave lines accordingly, and formats repeat-begin staves with begin modifiers using the upstream temporary single-barline layout behavior.
- Stave now exposes v5-style `GetBottomLineY()`, `GetBottomY()`, modifier X-shift helper behavior, and bounding boxes based on configured staff spacing.
- Stave geometry and measure setters now return the stave for v5-style fluent chaining while preserving existing call sites that ignore the return value.
- Elements, modifiers, notes, and stave modifiers now expose v5-style category names across accidentals, annotations, articulations, bar notes, barlines, clefs, crescendos, curves, dots, flags, fractions, glyphs, fingerings, ghost notes, grace notes/groups, key signatures, noteheads, note subgroups, ornaments, render contexts, stave connectors, stave notes, stemmable notes, stems, stave hairpins, stave lines, stave ties/tab ties, string numbers, text dynamics, text notes, time signatures, tremolos, tuplets, vibratos/vibrato brackets, voices, repetitions, sections, tempo, stave text, and voltas.
- Stave line bottom bounds and rendered line rectangles now respect the stave element `LineWidth` style like v5.
- Repeat barline dot radius and repeat-bar offsets now come from v5-style metrics with renderer-call coverage.
- StaveNote now uses glyph text metrics for rest collision bounds in multi-voice formatting, exposes notehead glyph text metrics needed by v5 layout code, uses glyph metrics for notehead width, applies v5-style displaced-notehead absolute X shifts, applies v5-style rest Y shifts and flag-aware right-side positioning in `GetModifierStartXY`, preserves v5 unison overlap behavior for same-line two-voice notes while still shifting style/dot/head differences, preserves per-key notehead styles across reset/reline/stem-direction rebuilds, and positions flagged notes from flag glyph ascent/descent metrics with bounding-box coverage.
- StaveNote and Tuplet now use v5-style pointer-rectangle metrics and emit `RenderContext.PointerRect()` calls for interaction bounds when drawn.
- Tuplet bracket padding, line width, and leg length now come from v5-style metrics instead of local draw constants.
- Dot radius/width/spacing/grace sizing, Curve geometry defaults, Crescendo default hairpin geometry, StaveTie geometry defaults and close-note control points, Stroke formatter spacing, Tremolo glyph font size/spacing, repeat barline dot geometry, KeySignature accidental spacing/fallback advances, Stave/System/Formatter/TickContext defaults, TextFormatter fallback sizing, TextDynamics glyph/placement defaults, StaveTempo spacing/shifts, cautionary accidental parenthesis padding, Ornament formatter/accidental spacing, MultiMeasureRest layout defaults and internal draw ratios, TextBracket line/bracket defaults, PedalMarking bracket defaults, FretHandFinger/StringNumber modifier spacing and draw offsets, Volta bracket/label geometry, StaveConnector line/text geometry, tab stave/grace-tab/tie/slide defaults, and Vibrato/VibratoBracket width/offset defaults now come from explicit v5-style metrics instead of local constants/root fallbacks.
- Beam generation now applies the VexFlow 5 tuplet reformat pass after beam attachment, updating tuplets to the beam side and bracket state based on whether every tuplet note was beamed, covers v5 rest-breaking / `beamRests` / `beamMiddleOnly` / stem-direction-maintenance / unbeamable-duration grouping behavior, keeps auto-stemmed beam direction in sync with the recomputed note directions, verifies stemlet configuration, uses v5-style flat slopes for tab-note beams, and beam rendering now includes the supported 256th-note sixth beam level.
- Bend now implements the v5 tablature bend modifier surface with phrase width calculation, modifier-context formatting, line style metrics, tap text, and renderer-call coverage.
- Flag now wraps the existing flag glyph drawing with the v5 `Flag` category and render group surface, and StaveHairpin now has a first-pass note-to-note v5 port with render options, above/below placement, tick-to-pixel shift helper, and renderer-call coverage.
- Voice now accepts v5-style time signature strings, exposes actual resolution, preserves total-tick denominators when adding tuplets/fractional durations, propagates its stave during pre-format, exposes merged tickable bounding boxes, rolls back failed strict additions, and has ignored-tickable coverage for bar notes.
- Formatter `FormatAndDraw()` now returns the formatted voice bounding box like v5 instead of dropping the result.
- Formatter `FormatAndDraw()` now also accepts the v5 boolean `autoBeam` convenience overload.
- System now exposes v5-style category and bounding-box behavior based on system origin, width, and formatted vertical span.
- Factory now supports v5-style `SetContext()`, sets context on created systems/staves, preserves v5 default `System()` origin options, widens `GhostNote()` creation to generic note structs, accepts EasyScore construction options, accepts options objects for staves, tab staves, voices, beams, tuplets, curves, text dynamics, text notes, crescendos, accidentals, annotations, articulations, fingerings, string numbers, ornaments, chord symbols, vibratos, stave ties, stave lines, vibrato brackets, stave connectors, grace note groups, note subgroups, text brackets, pedal markings, and bar/clef/time/key signature note wrappers, and has smoke coverage for v5-style wrapper constructors: staves, tab staves, voices, beams, tuplets, curves, glyph notes, repeat notes, ghost notes, bar notes, clef/time/key signature note wrappers, text notes, stave lines, multi-measure rests, and text brackets.
- GraceNoteGroup now exposes the v5 category, owns the v5-style soft grace-note voice and formatter minimum-width preformat path, participates in modifier-context formatting with group shift and spacing-from-next-modifier behavior, supports `BeamNotes()` with v5 grace-beam render options, and renders slurs through `StaveTie` / `TabTie` with `SlurYShift` render-option coverage.
- EasyScore now supports v5-style defaults via `Set()` and construction-time or post-construction `SetOptions()`, render-context reassignment via `SetContext()`, voice creation with time-string and flat or nested softmax options, non-throwing `Parse()` results with opt-in `ThrowOnError`, public commit hooks, fluent `Beam()` / `Tuplet()` helpers that return the input note list for composition, v5 `x` / `X` note type parsing, ghost notes in returned note lists, and per-note `id` / comma-separated `class` / `articulations` / `fingerings` / `ornaments` / `annotation` / `annotations` parser options through Element attribute compatibility helpers and migrated modifier classes.
- RenderContext now exposes v5-compatible `PointerRect()`, `Fill(attributes)`, `OpenGroup()`, `CloseGroup()`, `Add()`, `FillStyle`, and `StrokeStyle` surface methods/properties, with recording-context coverage; Skia save/restore now preserves font, line-cap, dash, stroke/fill, fill/stroke style properties, and stored shadow state; the README now documents Skia/Unity/custom `RenderContext` as the supported rendering model and browser SVG/Canvas DOM APIs as out of scope for the C# port.
- Registry now ports the backend-neutral v5 element registry surface with auto element ids, id/type/class attribute indexing, default registry auto-registration, and expanded type/class-list attribute-update coverage.
- TypeGuards now ports the backend-neutral v5 category helper surface with exact and ancestor-aware category checks plus focused helper coverage for common notation types.
- The portable `VexFlow` facade now exposes v5-style build metadata, font-family setters/getters backed by Metrics cache invalidation, key-signature delegation, and table constants including render precision.
- Element now includes v5-style group style propagation, `DrawWithStyle()` save/style/draw/restore behavior, parent ownership checks for children, dynamic attributes, `GetContext()`, `SetRendered()`, and base `type`-attribute category behavior with focused coverage.
- Base `Modifier` and `Tickable` now expose v5 category strings, and TypeGuards covers their ancestor-aware checks.
- StaveHairpin now uses the shared `DrawWithStyle()` path for tick-formatted drawing and has focused coverage for above/below placement, invalid-position ignores, missing-note draw failures, and invalid tick formatter input.
- TextFormatter registered-font matching now covers v5-style requested-family prefixes and quoted CSS fallback lists, and registered font metric overwrites / parameter updates invalidate cached widths.

The test suite is green at the time this plan was last updated:

```text
Passed: 938, Failed: 0, Skipped: 0, Total: 938
```

## Historical Migration Rule

During the migration, each area followed this rule:

1. Compare the final VexFlow 5.0.0 source, not only intermediate commits.
2. Port behavior in small, testable slices.
3. Add focused unit tests for layout/math/API behavior.
4. Add image or renderer-call tests for visual behavior where possible.
5. Run the full test suite after each slice.

Avoid a broad mechanical rewrite unless it directly reduces parity risk. The current C# port has its own architecture, and not every TypeScript refactor maps cleanly to C#.

## Historical Work Areas

The following sections are the closed migration checklist. Their "Remaining work" subsections describe what each slice originally tracked; items that are in scope for this port have since been implemented, verified, or explicitly scoped.

### 1. Font And Metrics System

VexFlow 5 moved many hard-coded engraving values into font metrics and a common metrics layer. The C# port still contains several local constants and approximations.

Remaining work:

- Complete adoption of the new `Metrics` equivalent for remaining common values such as font sizes, modifier spacing, and module-specific styles; Dot radius/width/spacing/grace sizing, Curve defaults, Crescendo default hairpin geometry, StaveTie defaults/close-note control points, Stroke formatter spacing, Tremolo font size/spacing, repeat barline dot geometry, KeySignature accidental spacing/fallback advances, Stave/System/Formatter/TickContext defaults, TextFormatter fallback sizing, TextDynamics glyph/placement defaults, StaveTempo spacing/shifts, cautionary accidental parenthesis padding, Ornament formatter/accidental spacing and stacked articulation ornament y-offsets, MultiMeasureRest layout defaults and internal draw ratios, TextBracket line/bracket defaults, PedalMarking bracket defaults, FretHandFinger/StringNumber modifier spacing and draw offsets, Volta bracket/label geometry, StaveConnector line/text geometry, tab stave/grace-tab/tie/slide defaults, and Vibrato/VibratoBracket width/offset defaults are now explicit metrics.
- Replace the remaining hard-coded layout constants with metric lookups where v5 uses metrics.
- Audit glyph width, glyph height, ascent, descent, pointer-rectangle defaults, and text measurement paths; StaveNote/Tuplet pointer rectangles, Tuplet bracket metrics, Dot metrics, Curve defaults, StaveTie defaults/close-note control points, Stroke spacing, Tremolo font size/spacing, repeat barline dots, KeySignature spacing, Annotation measured width, TextFormatter fallback sizing, v5-style family-prefix/CSS-list matching, and registry/update cache invalidation, StaveTempo spacing/shifts, cautionary accidental parenthesis padding, and Ornament stacked articulation y-offsets are now covered.
- Ensure Bravura glyph data is complete enough for v5 glyph names used by migrated features.
- Decide how multi-font support maps to C#, especially Bravura, Gonville, Petaluma, Leland, text fonts, and custom glyphs.

Verification:

- Metric lookup tests for known v5 values.
- Rendering tests for spacing-sensitive objects: stave padding, annotations, articulations, rests, tuplets, curves, tremolos, and text modifiers.
- Image comparisons for representative measures before and after metric replacement.

### 2. First-Pass V5 Module Hardening

The previously tracked missing V5 module surface now has first-pass C# equivalents. The remaining module work is parity hardening rather than creating the initial classes.

Remaining work:

- Expand chord symbol image parity, especially dense super/subscript stacks, center-stem placement, and SMuFL chord glyph metrics.
- Expand pedal marking image parity for same-note release/depress pairs and mixed/text/bracket styles across formatted voices.
- Expand tab notation parity for remaining flags/dots image behavior, grace-tab scaling, tab ties, and tuning integration; tab stave, grace-tab, tab-tie, and tab-slide defaults are now metric-backed, tab notes inherit stave context and respect v5 group/stem/dot placement behavior, and the v5 tablature `Tuning` helper now has focused API coverage.
- Expand vibrato image parity for alternate SMuFL vibrato codes and bracket spans across stave boundaries.

Verification:

- Constructor/API tests matching v5 options.
- Formatter integration tests for each modifier category.
- Renderer-call tests for expected draw order and glyphs.
- Image comparisons for at least one realistic example per module.

### 3. Annotation Parity

The C# annotation implementation exists, but it is simplified compared with VexFlow 5.

Remaining work:

- Complete image-level validation of horizontal justification semantics after the v5 overlap behavior port.
- Complete image-level validation of vertical justification after v5 string aliases and center/center-stem placement support.
- Use v5-style text measurement and font sizing rather than local approximations; Annotation now reports measured width, TextFormatter fallback sizing is now metric-backed, registered font matching now covers v5-style requested-family prefixes and quoted CSS fallback lists, and metric overwrites / parameter updates invalidate cached widths, but full registered text-font data remains open.
- Continue auditing stem, tab-note, stave-line-count, and notehead-width cases against rendered v5 examples.
- Preserve old C# enum/API behavior only where needed for compatibility.

Verification:

- Tests for left, right, center, and center-stem justification.
- Tests for above, below, center, and center-stem vertical placement.
- Multi-annotation formatting tests for text-line stacking and left/right shifts.
- Image comparison with annotations on stem-up, stem-down, rest, chord, and tab-note cases.

### 4. Articulation Parity

The C# articulation implementation has v5-style lookup fallback, but layout is still approximate.

Remaining work:

- Complete image-level validation after the glyph-height formatter increment port.
- Complete image-level validation after the upstream top/bottom Y and initial-offset helper port.
- Expand snap-to-staff parity coverage for articulations that can and cannot sit between staff lines.
- Confirm above/below glyph selection for every articulation code.
- Verify grace-note handling once that path is complete; tab-note draw behavior now has renderer-call coverage.

Verification:

- Tests for staccato, tenuto, accent, marcato, fermata, bow marks, pizzicato, and choke.
- Tests for above and below placement on stem-up/stem-down notes.
- Snap-to-staff tests for staff-line and staff-space results.
- Image comparisons for stacked articulations and articulations near ledger lines.

### 5. Accidental Layout Parity

The table lookup behavior is closer to v5, but full layout parity still needs review.

Remaining work:

- Audit accidental column layout against final v5.
- Expand accidental alias/direct glyph coverage only if additional upstream aliases are found.
- Complete image-level validation of cautionary accidental parentheses spacing after the v5 metric reset and metric-backed parenthesis padding ports.
- Complete image-level validation for natural collision spacing in key signatures and note accidentals; key signature spacing constants are now metric-backed with focused width coverage.
- Complete image-level validation of microtonal accidental widths and padding after alias/direct glyph-name coverage.
- Verify accidental formatting for chords with dense vertical collisions.

Verification:

- Unit tests for all v5 accidental aliases.
- Formatter tests for 1 through 6 accidental columns.
- Renderer tests for cautionary accidentals.
- Image comparisons for dense chromatic chords and microtonal examples.

### 6. Ornament Layout Parity

Ornament lookup compatibility is improved, but full v5 behavior is not yet proven.

Remaining work:

- Complete image-level validation of ornament positioning, delayed behavior, and jazz ornament placement after the v5 placement port.
- Complete image-level validation of upper/lower accidental positioning and scaling after renderer-call coverage.
- Confirm attack, release, transition, and articulation ornament groups.
- Expand direct glyph-name passthrough rendering only if additional upstream glyph names are missing.

Verification:

- Tests for classical ornaments, jazz ornaments, delayed ornaments, and ornament accidentals.
- Image comparisons for ornaments above and below notes, including stacked ornament cases.

## Core Rendering And Layout Work

### 7. Stave Parity

Some v5 stave changes are already ported, but the stave still needs a fuller options and metrics audit.

Remaining work:

- Continue auditing option names and defaults with v5 while preserving C# API compatibility; line configuration, metric-backed default stave options, and fluent geometry/measure setter APIs are now ported.
- Replace remaining stave padding approximations with metrics; stave line width and repeat barline dot geometry now respect metrics/style.
- Expand start/end modifier sorting and alignment coverage after the repeat-begin layout special-case port.
- Expand begin/end barline behavior coverage with all supported modifier combinations.
- Audit multi-stave alignment and system layout; system bounding boxes now have focused coverage after formatting.
- Continue auditing v5 pointer/interaction rectangle behavior beyond the now-covered StaveNote and Tuplet defaults.

Verification:

- Tests for stave options, line visibility, spacing, padding, bottom-line/bottom-Y helpers, modifier X shifts, bounding boxes, note start/end X, and multi-stave begin modifier alignment.
- Image comparisons for staves with clefs, key signatures, time signatures, repeats, voltas, sections, and tempo text.

### 8. StaveNote Parity

Several important v5 StaveNote fixes are already ported, but remaining differences are likely in edge-case layout and visual metrics.

Remaining work:

- Audit `Format` behavior for two and three voices against final v5.
- Continue auditing `Format` behavior for three voices against final v5; two-voice unison/style differences and rest glyph-metric bounds now have focused coverage.
- Expand flag positioning and bounding-box validation beyond the newly covered stem-up eighth-note glyph-metric path.
- Expand modifier start XY coverage beyond the newly ported rest shifts and flag-aware right positioning, especially all positions and stem directions.
- Verify notehead displacement for dot counts, whole notes, and broader unison cases; glyph-metric notehead width, v5-style displaced-notehead absolute X shifts, mixed-style reset preservation, and two-voice style-sensitive shifts now have focused coverage.
- Confirm stem, flag, notehead, ledger-line, and modifier draw order.

Verification:

- Tests for same-line chords, seconds, unisons, rests in multi-voice contexts, and mixed notehead styles.
- Bounding-box tests for stems, flags, modifiers, rests, chords, and pointer rectangles.
- Image comparisons for dense multi-voice measures.

### 9. Beam Parity

Beam behavior exists in C#, but v5 changed note, tuplet, and rest interactions that should be audited.

Remaining work:

- Expand image coverage for beamed rests and stemlets; focused unit coverage now verifies v5 grouping behavior and stemlet configuration.
- Expand tuplets-inside-beams coverage beyond the newly ported post-generation tuplet location/bracket reformat pass.
- Verify beam slope, stem extension, and formatting under dense rhythms; auto-stem direction, tab-note flat slope, and 256th-note beam-level rendering now have focused coverage.

Verification:

- Unit tests for beam grouping and rest options.
- Image comparisons for beamed rests, tuplets, stemlets, and mixed durations.

### 10. Tickable, Note, Voice, Formatter Completeness

Major formatter fixes are ported, but the broader v5 layout engine still needs parity review.

Remaining work:

- Audit voice strict/soft mode behavior against v5.
- Expand formatter image coverage for mixed tuplets; basic triplet, nested-tuplet denominator expansion, actual-resolution behavior, pointer rectangles, and bracket metrics now have focused coverage.
- Confirm modifier-context grouping by stave; FormatterOptions and TickContext padding defaults are now metric-backed, and GraceNoteGroup now participates in modifier-context formatting with grace-beam and tie-backed slur coverage.
- Confirm formatter tuning convergence and cost functions under multi-voice layouts.
- Verify ghost notes and unaligned notes in formatter spacing; bar-note ignored-tick behavior, voice bounding-box merging, `FormatAndDraw()` bounding-box return, and the v5 boolean auto-beam overload now have focused coverage.

Verification:

- Multi-voice formatter tests with shared and unshared tick positions.
- Mixed tuplets and nested tuplets across voices.
- Tests for ghost notes and bar notes in formatter spacing.
- Image comparisons for formatter-heavy scores.

## API And Factory Work

### 11. Factory API

VexFlow 5 changed naming, options, and object construction patterns.

Remaining work:

- Audit factory methods against v5.
- Add missing constructors for modules that are ported; `Factory.SetContext()` now mirrors v5 context reassignment, `Factory.System()` now sets context immediately and preserves v5 default origin options, `Factory.Stave()` / `Factory.TabStave()` now accept v5-style params objects, `Factory.Voice()` now accepts `VoiceTime`, v5-style time strings, and params objects, `Factory.Beam()` now supports auto-stem, secondary beam breaks, forced partial-beam directions, and combined params objects, `Factory.Tuplet()` / `Factory.Curve()` now accept v5-style params objects, `Factory.GhostNote()` now accepts generic note structs, `Factory.EasyScore()` now accepts construction options, `Factory.TextDynamics()` now accepts text/duration/dots/line options while preserving the C# `Dynamics` alias, `Factory.TextNote()` now accepts v5-style text note structs, `Factory.StaveLine()` now accepts v5-style indexed notehead line params, `Factory.Crescendo()` now accepts note/direction/height/line options, `Factory.Accidental()` now accepts type/cautionary options, `Factory.Annotation()` now accepts text/justification/font options, `Factory.Articulation()` now accepts type/position/between-lines options, `Factory.Fingering()` now accepts number/position/offset options, `Factory.StringNumber()` now accepts position/circle/offset/stem/line options, `Factory.Ornament()` now accepts type/position/accidental/delayed options, `Factory.ChordSymbol()` now accepts font/justification/report-width options, `Factory.Vibrato()` now accepts code/width/harsh/render-options options, `Factory.StaveTie()` now accepts notes/text/direction/render-options options, `Factory.VibratoBracket()` now accepts from/to/line/code/harsh/width options, `Factory.StaveConnector()` now accepts stave/type/x-shift/text options, `Factory.GraceNoteGroup()` now accepts typed grace-note and slur options, `Factory.NoteSubGroup()` now accepts notes options, `Factory.BarNote()` / `Factory.ClefNote()` / `Factory.TimeSigNote()` / `Factory.KeySigNote()` now accept v5-style params objects, `Factory.TextBracket()` now accepts start/stop/text/superscript/position/line/dash options, `Factory.PedalMarking()` now accepts notes/style/custom-text/line options, and factory smoke coverage now includes stave context injection plus staves/tab staves, voices, beams, tuplets, curves, glyph/repeat/ghost/bar/clef/time/key wrapper notes, text notes, stave lines, multi-measure rests, and text brackets.
- Continue auditing remaining option names while keeping the documented C# convention: VexFlow 5 `camelCase` options map to PascalCase C# properties.
- Verify default options match v5 where possible.

Verification:

- API smoke tests that construct representative scores through the factory only.
- Tests for option defaults and option overrides.

### 12. EasyScore And Parser

The C# EasyScore/parser path needs a v5 compatibility audit once core modules are complete.

Remaining work:

- Compare grammar and parser behavior with v5.
- Add support for any missing v5 syntax; EasyScore construction options, post-construction `SetOptions()`, defaults, render-context reassignment, voice time strings, flat and nested softmax options, non-throwing parse results, public commit hooks, fluent beam/tuplet helpers, v5 `x` / `X` note type parsing, ghost notes returned from `Notes()`, and `id` / `class` / `articulations` / `fingerings` / `ornaments` / `annotation` / `annotations` note options now have focused coverage.
- Verify modifiers produced by EasyScore use migrated classes and options; id/class element attributes plus articulation, fret-hand fingering, ornament, and annotation options now have focused coverage.
- Ensure errors match useful C# exceptions while retaining v5 semantics; `Parse()` now preserves the v5 result-returning path while `Notes()` keeps the existing C# throwing behavior.

Verification:

- Parser tests for notes, chords, rests, tuplets, accidentals, articulations, ornaments, beams, ties, and annotations.
- End-to-end EasyScore rendering tests.

### 13. Public Naming And Compatibility Layer

The TypeScript migration moved from older snake-case names to camelCase. C# should not mirror TypeScript naming blindly, but it needs a clear compatibility story.

Remaining work:

- Decide which old C# API names remain supported; element ids and registry-indexed attributes now follow v5-style auto-id and id/type/class behavior.
- Decide which v5 option names need additional aliases beyond the documented PascalCase mapping.
- Document remaining intentional deviations from upstream; the README now documents PascalCase option names, factory context construction, non-chainable `SetContext()` / `Draw()`, explicit accidental attachment, EasyScore `Set()` usage, renderer scope differences, and the portable subset of the `VexFlow` facade.
- Add obsolete markers only if the project is ready for them.

Verification:

- API compatibility tests for common old names.
- Tests for new v5-compatible option aliases.

## Rendering Backend Work

### 14. RenderContext Parity

The Skia render context supports the paths needed by current tests, but v5 rendering has a wider surface.

Remaining work:

- Audit all v5 render context methods against the C# abstraction.
- Add missing methods needed by migrated modules; the v5 `pointerRect()`, `fill(attributes)`, group, and `add(child)` surface is now available as C# `PointerRect()` / `Fill(attributes)` / `OpenGroup()` / `CloseGroup()` / `Add()` with backend-neutral defaults and recording coverage, and annotations/ornaments now emit render group calls.
- Verify save/restore behavior around styles, shadows, line dashes, and fonts; Skia save/restore now preserves font, line-cap, dash, stroke/fill, fill/stroke style properties, and stored shadow state with focused coverage.
- Decide how to map v5 group/pointer rectangle behavior to Skia.

Verification:

- Recording render context tests for draw order and style state.
- Skia image tests for dashed lines, text, shadows, glyphs, curves, and filled/stroked shapes.

### 15. SVG / Canvas / Web Behavior

VexFlow 5 includes browser and SVG-specific behavior. For this C# port, the supported model is backend-neutral `RenderContext` plus concrete SkiaSharp and Unity UI Toolkit renderers. Browser-specific SVG, Canvas DOM, HTML element, CSS, event, and renderer bootstrap APIs are documented as out of scope unless a future renderer package explicitly adds them.

Remaining work:

- Keep the README rendering-scope section in sync as renderer packages evolve.
- If a future SVG or browser renderer becomes in scope, design C# abstractions before porting implementation details.

Verification:

- Scope decision documented in the README.
- Tests only for supported rendering targets.

## Testing And Verification Work

### 16. Image Comparison Coverage

The current suite is strong on unit behavior but light on visual parity.

Visual parity scaffold:

- [`vexflow-5-visual-parity.md`](vexflow-5-visual-parity.md) records the tolerance policy, existing always-on renderer/layout coverage, explicit pixel comparison commands, and the representative gallery used for visual parity.
- `tools/gen-reference-images.mjs` now generates local VexFlow 5 node-canvas references for the formatter scenarios plus system/factory/EasyScore grand staff, complex notation, percussion scenes, tab notation, grace notes, small modifiers, and stave modifiers.
- `FormatterRenderingTests`, `SystemTests`, `FactoryTests`, `EasyScoreTests`, `ComplexNotationComparisonTest`, `PercussionComparisonTest`, and `VisualGalleryComparisonTests` now run generated VexFlow/C# PNG comparisons in the normal test suite with `ImageComparison.CrossEngineThresholdPercent`.
- `ComplexNotationComparisonTest` now emits a white-backed C# PNG output for the generated VexFlow/C# PNG pair.
- `PercussionComparisonTest` now emits white-backed C# PNG outputs for the VexFlow-native percussion scenes.
- `VisualGalleryComparisonTests` now emits C# PNG outputs for tab notation, grace notes, small modifiers, and stave modifiers into `VexFlowSharp.Tests/Comparison/Output/`.

Remaining work:

- No generated VexFlow 5 visual comparison scenes remain outside the normal test run.
- MusicXML real-score excerpts remain C# output/smoke scenes because this repository has no local VexFlow 5 MusicXML reference renderer.
- Cover basic notation, dense chords, tuplets, beams, key signatures, rests, modifiers, text, tab notation, and systems.
- Store or generate reference images in a reproducible way.
- Make image tests easy to run locally and in CI.

Verification:

- Image comparison tests with clear diff output.
- A documented tolerance policy for anti-aliasing and backend differences.

### 17. Upstream Commit Audit

The migration has ported many targeted v5 commits, and the local upstream audit is now captured.

Audit scaffold:

- [`vexflow-5-upstream-audit.md`](vexflow-5-upstream-audit.md) records the local upstream range, audit commands, module-surface classifications, all 230 upstream `src/` commits classified through `9cbac77c`, and command-defined classifications for the remaining non-source release/build/demo/test/docs churn in the 467-commit full range.

Remaining work:

- Re-open this audit only if a fuller upstream baseline, such as a real `4.2.6` tag, is added to `vexflow/`.
- Continue closing the visual parity/image-comparison gate for commits marked `deferred`.

Suggested classifications:

- `ported`: implemented and tested in C#.
- `not applicable`: browser/build/TypeScript-only, or irrelevant to the C# architecture.
- `deferred`: intentionally left for a later module.
- `remaining`: behavior should still be ported.

Verification:

- A commit audit table exists.
- Every `ported` entry has a test or a clear reason why testing is not practical.
- Every `not applicable` entry has a short reason.

## Suggested Execution Order

1. Finish the metrics/font layer.
2. Bring annotation and articulation to exact v5 layout behavior.
3. Finish accidental and ornament layout parity.
4. Harden first-pass small modifier modules with image parity: tremolo, fret-hand fingering, text bracket, pedal marking, chord symbol, and vibrato.
5. Harden first-pass stave modifier modules with image parity: tempo, text, volta, repetition, and section.
6. Complete StaveNote, Beam, Voice, and Formatter edge-case audits.
7. Harden larger first-pass note families: glyph notes, repeat notes, tab notes, tab staves, grace tab notes, tab ties, tab slides, and tuning integration.
8. Audit Factory, EasyScore, and Parser.
9. Expand image comparison coverage.
10. Complete the upstream commit audit table. Completed for the local `4.2.2..5.0.0` range; re-open only if a fuller upstream baseline is added.

## Definition Of Done For V5 Migration

The migration can be considered complete when:

- Every VexFlow 5.0.0 module is either ported or explicitly documented as out of scope.
- Core notation behavior has unit coverage and visual coverage.
- Public API differences are documented.
- The upstream commit audit has no unexplained `remaining` entries.
- The full test suite passes.
- Representative generated image comparisons pass for migrated rendering features.

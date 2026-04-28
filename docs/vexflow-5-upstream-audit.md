# VexFlow 5 Upstream Audit

This document records the final upstream audit gate for the VexFlowSharp v5 migration.

## Scope

The vendored upstream checkout in `vexflow/` has a `5.0.0` tag but no `4.2.6` tag. Use `4.2.2..5.0.0` as the local audit range unless a fuller upstream history is added later.

Audit commands:

```sh
git -C vexflow rev-list --count 4.2.2..5.0.0
git -C vexflow log --oneline --reverse 4.2.2..5.0.0 -- src
git -C vexflow diff --name-only 4.2.2..5.0.0 -- src
```

Current local range size: 467 commits.

Classifications:

- `ported`: implemented in C# with unit, renderer-call, or documented focused coverage.
- `not applicable`: browser/build/TypeScript-only or outside the backend-neutral C# architecture.
- `deferred`: intentionally left for a later visual/backend pass.
- `remaining`: behavior still needs C# work or explicit documentation.

## Source Module Surface

| Upstream area | C# status | Notes |
|---|---:|---|
| Core element/style/registry/type guards | ported | `Element`, `Registry`, `TypeGuards`, and focused tests cover ids, classes, style wrapping, group style, rendered state, context, and category helpers. |
| Metrics/font lookup | partial | Metrics layer and Bravura glyph augmentation are ported; multi-font/browser font loading remains deferred or out of scope. |
| Browser renderer/bootstrap (`canvascontext`, `svgcontext`, `renderer`, `web`) | not applicable | README documents `RenderContext`, Skia, Unity, and custom backends as the supported model. |
| Factory/EasyScore/parser surface | partial | Factory and EasyScore have broad v5 option coverage; parser and remaining option-name audits continue. |
| Notes and modifiers | partial | Many first-pass modules are ported with renderer-call tests; remaining work is visual parity and edge-case layout audits. |
| Tablature | partial | Tab notes/staves/ties/slides/grace-tab/tuning are ported; remaining work is image parity and integration hardening. |
| Upstream generated fonts | partial | Bravura coverage is extended; Gonville, Petaluma, Leland, and browser font loading are not yet first-class C# runtime assets. |

## Commit Audit Table

This table is intentionally filled in small tranches. Add exact test references when a commit is marked `ported`; add a reason when it is `not applicable`.

| Hash | Upstream subject | Status | C# evidence / reason |
|---|---|---:|---|
| `a0e0eb42` | Update copyright notices to include VexFlow contributors. | not applicable | Header/legal text churn; no runtime behavior. |
| `c67255d8` | Use the `@author` tag consistently. | not applicable | TypeScript documentation/style only. |
| `325f5ae2` | Use camelCase where possible (avoid snake_case). | not applicable | C# intentionally uses PascalCase API naming; README documents this deviation. |
| `4985f381` | Address review comments. | not applicable | Review cleanup commit; covered by neighboring feature commits where behavior exists. |
| `ba8dbe2e` | Use `#` to specify private class members instead of the `private` keyword. | not applicable | TypeScript syntax/style only. |
| `ca7b3d80` | element (v5) | ported | `ElementTests`, `RegistryTests`, and `TypeGuardsTests` cover v5 ids, attributes/classes, style wrapping, group style, registry updates, rendered state, and context. |
| `2ee3ed0c` | review comments | ported | Folded into the element/registry/typeguard coverage above. |
| `b569e2a2` | review comments | ported | Folded into the element/registry/typeguard coverage above. |
| `c8242be8` | fix element comments | not applicable | Documentation comments only. |
| `26de0fc3` | vibrato (v5) | ported | `VibratoTests` and `VibratoBracketTests` cover v5 categories, metrics, and renderer calls. |
| `578ce012` | Merge pull request #22 from rvilarl/vf5/vibrato | ported | Merge commit for `26de0fc3`; evidence above. |
| `cb8e016e` | chordsymbol (v5) | ported | `ChordSymbolTests` cover constructor/API surface, formatting, metrics, and renderer calls; image parity remains tracked separately. |
| `a724f674` | loadWebFont updated | not applicable | Browser font loading is outside the supported C# rendering model. |
| `94b7d317` | review comments | ported | Folded into chord symbol/font-loading classifications above. |
| `51e752be` | revert rename of roboto slab | not applicable | Browser/text-font asset naming; C# runtime currently ships Bravura-focused metrics. |
| `2b9d24b7` | chordsymbol with text parenthesis | ported | `ChordSymbolTests` cover text parenthesis handling and block rendering. |
| `036bda9e` | review comments | ported | Folded into chord symbol coverage. |
| `bfae30cb` | review comments | ported | Folded into chord symbol coverage. |
| `95bd1742` | Clean up unused imports and formatting issues. | not applicable | TypeScript formatting/import cleanup only. |
| `ab35657a` | articulation (v5) | ported | `ArticulationTests` cover v5 placement helpers, tab/stave behavior, between-lines override, formatting, and renderer calls. |
| `bb36b1f7` | use `\uXXXX` escape codes | not applicable | TypeScript source encoding cleanup. |
| `8ca69586` | review comments | ported | Folded into articulation coverage. |
| `e459090e` | stavetempo (v5) | ported | `StaveTempoTests` cover category, metric-backed spacing/shifts, glyph rendering, and stave integration. |
| `f48b97a7` | use `\uXXXX` escape codes | not applicable | TypeScript source encoding cleanup. |
| `0f59a8cb` | review comments | ported | Folded into stave tempo coverage. |
| `953673b0` | Add Academico. | deferred | Multi-font asset support is still tracked under font/metrics remaining work. |
| `4e97c64d` | Simplify `Tables.lookupMetric()` and add more comments. | ported | `MetricsTests` and metrics call-site coverage verify C# lookup fallback behavior. |
| `c3a0a455` | Merge pull request #39 from ronyeh/fonts | partial | Bravura/metrics pieces are ported; full multi-font support remains deferred. |
| `33f735af` | clef (v5) | ported | `StaveTests`, `FactoryTests`, and signature-wrapper tests cover clef category, wrappers, and rendering integration. |
| `7593c679` | musejazz hack to show progress | deferred | Related to alternate font progress; covered by multi-font deferred item. |
| `76047989` | frethandfinger (v5) | ported | `FretHandFingerTests` cover category, metrics, formatting, placement, and draw calls. |
| `e279bf50` | glyphnote (v5) | ported | `GlyphNoteTests` and factory wrapper tests cover category, construction, modifiers, and rendering. |
| `70d58ec2` | stavesection (v5) | ported | `StaveSectionTests` cover category, positioning, metrics, and draw behavior. |
| `1456668f` | Change for backwards compatibility | ported | Covered by factory/API compatibility tests around wrapper creation and PascalCase option mappings. |
| `943b76f5` | tremolo (v5) | ported | `TremoloTests` cover v5 category, metrics, and renderer calls. |
| `8d347588` | Add `getReportWidth` to chordsymbol | ported | `ChordSymbolTests` cover report-width formatting behavior. |
| `dad4c19b` | Merge pull request #54 from vexflow/53-error-in-ornament | ported | Ornament error/layout coverage is tracked in `OrnamentTests`. |
| `c3511d50` | Merge pull request #47 from rvilarl/vf5/frethandfinger | ported | Merge commit for fret-hand fingering; evidence above. |
| `4875d868` | review comments | ported | Folded into nearby module coverage. |
| `bf30cf26` | Merge pull request #41 from rvilarl/vf5/clef | ported | Merge commit for clef; evidence above. |
| `ced42ce9` | keysignature (v5) | ported | `KeySignatureTests`, `FactoryTests`, and signature-note wrapper tests cover v5 key specs, cancellation, wrappers, metrics, and rendering integration. |
| `d92b2444` | tuplet (v5) | ported | `TupletTests`, formatter tests, and beam tests cover tick multipliers, nesting, bracket metrics, pointer rects, and beam reformat behavior. |
| `b8ab2d51` | review comments | ported | Folded into key signature / tuplet coverage. |
| `2fc973a1` | bend (v5) | ported | `BendTests` cover v5 category, phrase widths, tap text, formatting, render options, and draw calls. |
| `8d975e30` | Merge pull request #48 from rvilarl/vf5/glyphnote | ported | Merge commit for glyph notes; `GlyphNoteTests` and factory tests cover the C# surface. |
| `a92c360a` | Merge pull request #55 from rvilarl/vf5/tuplet | ported | Merge commit for tuplet; evidence above. |
| `cf549777` | review comments | ported | Folded into nearby module coverage. |
| `b4c5b8d8` | Merge pull request #42 from rvilarl/vf5/keysignature | ported | Merge commit for key signature; evidence above. |
| `fe64939d` | Merge pull request #50 from rvilarl/vf5/stavesection | ported | Merge commit for stave section; `StaveSectionTests` cover the behavior. |
| `29ecba4f` | timesignature (v5) | ported | `StaveTests`, `FactoryTests`, and signature-note wrapper tests cover v5 time signature category/wrapper integration. |
| `570312aa` | review comments | ported | Folded into time signature coverage. |
| `3159b480` | Merge pull request #56 from rvilarl/vf5/tremolo | ported | Merge commit for tremolo; `TremoloTests` cover v5 behavior. |
| `96138076` | stavemodifier (v5) | ported | `StaveTests` and stave modifier subclasses cover v5 categories and layout integration. |
| `a442dd8a` | Use `loadWebFonts(fontNames)` to load only a few fonts. | not applicable | Browser web-font loading is outside the supported C# rendering model. |
| `96ebf68d` | Merge pull request #65 from ronyeh/fonts | partial | Bravura/metrics support is ported; browser and alternate font loading remain deferred/out of scope. |
| `d7eca9e6` | pedalmarking (v5) | ported | `PedalMarkingTests` cover v5 category, bracket/text styles, render options, and draw behavior. |
| `d6c87898` | annotation (v5) | ported | `AnnotationTests` cover justification aliases, placement, stem-aware draw placement, measured width, formatting shifts, and render groups. |
| `834b872e` | review comments | ported | Folded into pedal marking / annotation coverage. |
| `38e87b8d` | element x y integration | ported | `ElementTests`, note/stave tests, and bounding-box tests cover element coordinate integration used by migrated modules. |
| `203caaca` | notes refactoring preparation | ported | Covered by `NoteTests`, `StaveNoteTests`, `TickableTests`, and migrated note-family tests. |
| `d95726db` | Merge pull request #68 from rvilarl/vf5/noteextract1 | ported | Merge commit for note refactor preparation; evidence above. |
| `22513140` | Merge pull request #51 from rvilarl/vf5/annotation | ported | Merge commit for annotation; evidence above. |
| `a027c5c3` | review comments | ported | Folded into note/annotation coverage. |
| `b2bd6470` | Merge pull request #67 from rvilarl/vf5/element-x-y | ported | Merge commit for element x/y integration; evidence above. |
| `91cbc6df` | Merge pull request #52 from rvilarl/vf5/bend | ported | Merge commit for bend; evidence above. |
| `870c8dc5` | flag (v5) | ported | `StaveNoteRenderingTests` verify `Flag` category and rendering integration; `Flag` wraps glyph group behavior. |
| `91ed9adf` | review comments | ported | Folded into flag coverage. |
| `0638a527` | textnote (v5) | ported | `TextNoteTests` and factory tests cover category, construction, metrics, stave attachment, and rendering. |
| `756bbab9` | review comments | ported | Folded into text note coverage. |
| `4f44423f` | Merge pull request #70 from rvilarl/vf5/flag | ported | Merge commit for flag; evidence above. |
| `8669d074` | notehead (v5) | ported | `NoteHeadTests` and `StaveNoteRenderingTests` cover v5 category, metrics, glyph widths, style preservation, and rendering. |
| `58ce0662` | review comments | ported | Folded into notehead coverage. |
| `dbd2c527` | note (v5) | ported | `NoteTests`, `StaveNoteTests`, tab/glyph/repeat/text note tests cover v5 note API/category/layout behavior. |
| `ae0eb4ec` | review comments | ported | Folded into note coverage. |
| `9dd9dd0f` | operators fix | ported | `FractionTests`, `VoiceTests`, and tick-context/formatter tests cover arithmetic and duration behavior. |
| `cb6ffb87` | staverepetition (v5) | ported | `RepetitionTests` and stave tests cover category, positioning, and rendering integration. |
| `2fc61565` | parenthesis (v5) | ported | `ParenthesisTests` cover category, layout metrics, and renderer calls. |
| `c08a215e` | review comments | ported | Folded into repetition / parenthesis coverage. |
| `9ccef3b6` | review comments | ported | Folded into repetition / parenthesis coverage. |
| `16e72876` | review comments | ported | Folded into repetition / parenthesis coverage. |
| `64e844f5` | Merge pull request #81 from rvilarl/vf5/staverepetition | ported | Merge commit for stave repetition; evidence above. |
| `119f242d` | Merge branch `main` into vf5/parenthesis | ported | Merge bookkeeping for parenthesis branch; behavior covered above. |
| `52d1cfbd` | remove glyph in tests | not applicable | Upstream test cleanup only. |
| `6966688e` | new fonts | deferred | Alternate font assets remain tracked under multi-font support. |
| `416e2353` | remove textformatter | partial | C# keeps a backend-neutral `TextFormatter`; v5 font matching/cache behavior has focused `TextFormatterTests`. |
| `7f8ec9f1` | Merge pull request #82 from rvilarl/newfonts | deferred | Alternate font package support remains deferred. |
| `48b15e57` | staveconnector (v5) | ported | `StaveConnectorTests` cover category, type variants, text/line metrics, and draw calls. |
| `b1746690` | stavevolta (v5) | ported | `VoltaTests` cover category, label/bracket geometry, placement, and draw behavior. |
| `cd7c90b5` | accidental (v5) | ported | `AccidentalTests`, key signature tests, and renderer tests cover v5 glyph aliases, cautionary parentheses, metrics, and formatting. |
| `470dff34` | review comments | ported | Folded into accidental coverage. |
| `51f2b887` | Merge pull request #35 from rvilarl/vf5/accidental | ported | Merge commit for accidental; evidence above. |
| `b8dbbec3` | Merge pull request #87 from rvilarl/removetextformatter | partial | C# intentionally preserves `TextFormatter`; migrated cache/font matching behavior is tested. |
| `8049033c` | Merge pull request #88 from rvilarl/vf5/staveconnector | ported | Merge commit for stave connector; evidence above. |
| `d0fff046` | stroke (v5) | ported | `StrokeTests` cover category, metrics, formatting, tab/stave placement, and draw calls. |
| `78f66e11` | review comments | ported | Folded into stroke coverage. |
| `f6a78cd9` | Add a `Glyphs` enum. | partial | C# uses generated Bravura glyph data and table constants rather than a direct TS enum; v5 glyph-name coverage is tested where used. |
| `ca8292a2` | ornament (v5) | ported | `OrnamentTests` cover category, jazz ornaments, accidentals, stacked articulation spacing, metrics, and renderer calls. |
| `06b76324` | Merge pull request #92 from rvilarl/vf5/ornament | ported | Merge commit for ornament; evidence above. |
| `9ea650f4` | refactor currentMusicFont | partial | Current music font is represented through C# `Metrics`/font-family facade; full alternate music-font loading remains deferred. |
| `efb033d9` | review comments | partial | Folded into glyph/font classifications above. |
| `afadd9b5` | font glyphs removed | partial | C# keeps generated Bravura glyph data; upstream font asset reshaping is only partially applicable. |
| `04032100` | accidental removed from keyprops | ported | Accidental and key-signature tests cover v5 accidental table/glyph lookup behavior without legacy key-prop accidental coupling. |
| `284a4b1d` | Merge pull request #96 from rvilarl/cleanup2 | partial | Cleanup merge spanning font/glyph and accidental changes; classifications above apply. |
| `27588771` | textbracket (v5) | ported | `TextBracketTests` cover constructor/API, metric-backed defaults, dashed/solid rendering, and placement. |
| `21bb98c2` | common metrics fonts | partial | C# `MetricsTests` cover common metrics lookup; full upstream font-family asset stack remains deferred. |
| `2c53e2e6` | review comments | partial | Folded into text bracket / metrics classifications. |
| `7318a8f6` | Merge pull request #100 from rvilarl/cleanup4 | partial | Cleanup merge; covered by neighboring classifications. |
| `6e06a4f9` | measure text reworked | ported | `TextFormatterTests`, annotation/chord/text note tests cover measured text width and cache invalidation paths. |
| `bb7f3de2` | review comments | ported | Folded into text measurement coverage. |
| `6c8bc95a` | measure text with element | partial | Text measurement behavior is ported via `TextFormatter`/element call sites; browser canvas text measurement is not applicable. |
| `093e53f8` | call to measure text removed | ported | Covered by text formatter and measured-width call-site tests. |
| `8f96ccea` | Merge pull request #104 from rvilarl/profile3 | partial | Metrics/profile merge; backend-neutral metrics are covered, browser profiling is not applicable. |
| `2d4fce0f` | Run eslint. | not applicable | TypeScript lint formatting only. |
| `5cd3156b` | tables access internal | ported | Table constant/API tests and call-site coverage verify C# table access behavior. |
| `f97989d3` | vexflow4 #1590 | ported | Covered by key signature/table compatibility tests for carried-over v4 issue behavior. |
| `1300be35` | vexflow4 #1596 | ported | Covered by note/formatter compatibility tests for carried-over v4 issue behavior. |
| `0921706f` | review comments | ported | Folded into table/metrics coverage. |
| `08e62dc2` | Merge pull request #106 from rvilarl/metrics | partial | Backend-neutral `Metrics` is ported; full upstream font payload remains deferred. |
| `d63fc269` | Skip alternate code points / avoid NBSP descriptions. | ported | Bravura glyph generation/tests cover duplicate/alternate glyph handling in the generated C# table. |
| `c4397795` | Merge pull request #110 from ronyeh/duplicate-enum | ported | Merge commit for glyph duplicate handling; evidence above. |
| `860a67d5` | font scale | ported | Metrics/table tests cover v5 notation font scale constants and call sites. |
| `1c471d21` | fix jmusejazz time signature | deferred | Alternate music font handling remains deferred; time signature wrappers are otherwise ported. |
| `01e5dec7` | Merge pull request #112 from rvilarl/scale | ported | Merge commit for font scale; evidence above. |
| `20e5cabe` | Add `Element.setTextMeasurementCanvas()` / OffscreenCanvas. | not applicable | Browser/OffscreenCanvas text measurement injection does not map to backend-neutral C# rendering. |
| `106bdace` | Ported similar to 0xfe/vexflow#1598. | ported | Covered by key-signature width/formatting tests and signature wrapper behavior. |
| `a0aa7ccb` | calculate width without stave | ported | Key signature and signature note wrapper tests cover width calculation without full stave draw dependency. |
| `b9f7bb58` | Merge pull request #120 from rvilarl/keysignaturewidth | ported | Merge commit for key-signature width; evidence above. |
| `e59e4136` | staveline (v5) | ported | `StaveLineTests` cover category, metric defaults, placement, and renderer calls. |
| `4ab869d2` | support single digit time signatures | ported | Time signature/stave tests cover v5 time-signature construction and rendering smoke paths. |
| `32faa23f` | adjust big time signatures | ported | Time-signature metric/glyph behavior covered by stave and wrapper tests. |
| `47b93945` | Merge pull request #125 from rvilarl/timesignaturesizes | ported | Merge commit for time signature sizing; evidence above. |
| `4b7ca61f` | Merge pull request #123 from rvilarl/staveline | ported | Merge commit for stave line; evidence above. |
| `fb55c97e` | New fonts. Use `@vexflow-fonts` URLs. | not applicable | Browser package URL/font hosting does not apply to C# assemblies. |
| `802fb743` | Fix vexflow issue #91. | ported | Covered by carried-over key/table compatibility tests in C#. |
| `c77fcfce` | `setMusicFont()` => `setFonts()` API rename. | ported | `VexFlowTests` cover `VexFlow.SetFonts()` / `GetFonts()` and metrics cache invalidation. |
| `81a05f3f` | Merge pull request #132 from ronyeh/font-api-naming | ported | Merge commit for facade font API naming; evidence above. |
| `d8e84ae4` | dot v5 | ported | `DotTests` cover metric-backed radius/width/spacing, grace scaling, formatting, rendering, and tab-note stem-base placement. |
| `e322a2f0` | Base64 encode fonts for bundling in `vexflow.js`. | not applicable | JS bundling concern; C# packages embed/generated assets differently. |
| `288fe6d7` | Improve comments / add Academico default family. | partial | Default facade font family includes `Bravura,Academico`; actual Academico font asset support remains deferred. |
| `1250aecd` | metronome musicxml features | ported | Bravura glyph augmentation and `StaveTempoTests` cover metronome glyphs/rendering. |
| `27cc82d6` | review comments | ported | Folded into stave tempo/metronome coverage. |
| `15db4db1` | getElementWidth added | ported | Chord symbol and text measurement tests cover element/block width behavior. |
| `bd794143` | review comments | ported | Folded into element-width/text measurement coverage. |
| `8b292b83` | glyphs constants | partial | C# uses generated glyph data/tables rather than a direct TS `Glyphs` enum; usage coverage exists for migrated glyph names. |
| `c452a6ad` | `Vex.Flow` => `VexFlow` | ported | Portable `VexFlow` facade exists with build/font/key-signature/table constant tests. |
| `de2dc400` | Rename `flow.ts` to `vexflow.ts`. | not applicable | TypeScript entrypoint rename; C# exports by assembly. |
| `d7409da4` | Moved utility methods to `util.ts`. | not applicable | TypeScript source organization only. |
| `269ad64d` | Have webpack export the library as `VexFlow`. | not applicable | JS bundling/export behavior; C# facade is tested separately. |
| `5c10f0bd` | Demos for font loading and entry files. | not applicable | Upstream demo/browser packaging only. |
| `2b9aa124` | More demos / eslint fixes. | not applicable | Upstream demo/lint automation only. |
| `7cda9a61` | redesign pointer events | ported | `RenderContextTests`, `StaveNoteRenderingTests`, and `TupletTests` cover pointer-rectangle render surface; browser events remain out of scope. |
| `91ea2bdc` | linedash support | ported | `ElementTests`, curve/text bracket rendering tests, and Skia state tests cover line dash styling. |
| `f21d8958` | curve supporting linedash | ported | `CurveTests` cover dashed and solid curves with renderer-call assertions. |
| `5a745303` | fix stavenote bounding box | ported | `StaveNoteTests` / rendering tests cover notehead, rest, and stave-note bounding boxes. |
| `5a91dc4d` | refactor elementstyle | ported | `ElementTests` cover style application, group style, and draw-with-style save/restore behavior. |
| `bd2d535a` | elementstyle reworked | ported | Folded into element style coverage above. |
| `5c25f49d` | ornaments above stave | ported | `OrnamentTests` cover above/below placement and delayed stave-end/next-context positioning. |
| `332de0d5` | Remove GonvilleSmufl | not applicable | Upstream font asset packaging cleanup; C# does not ship that asset path. |
| `94fa0322` | Generate Base64 Gonville font. | deferred | Alternate Gonville font support remains deferred. |
| `a5895253` | Merge pull request #137 from ronyeh/gonville | deferred | Alternate font packaging remains deferred. |
| `fdd27c06` | Merge pull request #163 from rvilarl/style/bend | ported | Bend style/render behavior is covered by `BendTests` and base element style tests. |
| `59ee2fad` | Merge pull request #162 from rvilarl/fixnotebbox | ported | Merge commit for note bounding-box fixes; evidence in note/stave-note/glyph-note bounding-box tests. |
| `b34eb7ad` | bounding box fixes | ported | `StaveNoteTests`, `GlyphNoteTests`, and formatter/system bounding-box tests cover migrated bounding boxes. |
| `fc4b732a` | fix bounding box | ported | Folded into bounding-box coverage above. |
| `6797fdc6` | glyphnote bounding box | ported | `GlyphNoteTests` cover rendered state and bounding-box behavior. |
| `25f3dad2` | clefnote bounding box | ported | Signature wrapper tests cover clef-note category/wrapper behavior; bounding-box integration is covered through formatter/system tests. |
| `ee5ddd6f` | eslint auto fixes. | not applicable | TypeScript lint formatting only. |
| `a78184a7` | Merge pull request #156 from rvilarl/pointer-events | ported | Pointer-rectangle render surface is covered by `RenderContextTests`, `StaveNoteRenderingTests`, and `TupletTests`; browser events remain out of scope. |
| `73b783e9` | Glyph Inspector debug glyph origins. | not applicable | Upstream debugging/demo tooling only. |
| `8f88860d` | fix justify | ported | `AnnotationTests` cover v5 justification aliases and center/center-stem placement. |
| `32dced4e` | Merge pull request #168 from ronyeh/gonville | deferred | Alternate Gonville font support remains deferred. |
| `a3ad37dc` | StaveTie: move rendering logic out of draw | ported | `StaveTieTests` cover category, geometry defaults, close-note control points, and draw behavior. |
| `4d4ad410` | Upgrade TypeScript and add missing `TextMetrics` fields. | not applicable | TypeScript build/type compatibility; C# text metrics are represented through `TextMeasure`/`TextFormatter`. |
| `0ec2f2b3` | Fix mean calculation in formatter | ported | `FormatterTests` cover duration statistics, average deviation cost, and formatting results. |
| `9dc09ce8` | add `getAverageDeviationCost` | ported | `FormatterTests` cover average deviation cost. |
| `5437d40b` | lint | not applicable | TypeScript lint formatting only. |
| `acab4415` | Annotation: remove outdated comment | not applicable | Documentation/comment cleanup only. |
| `59634541` | fix param typing. | not applicable | TypeScript typing cleanup only. |
| `7e4d760b` | Remove unneeded test on StaveModifier | not applicable | Upstream test cleanup only. |
| `be28adaf` | Merge pull request #170 from rvilarl/fixBoundingBox | ported | Merge commit for bounding-box fixes; evidence above. |
| `5d4c1428` | eslint autofix | not applicable | TypeScript lint formatting only. |
| `e440dc05` | Remove hash from private properties | not applicable | TypeScript private-field syntax cleanup only. |
| `fde9d217` | Merge pull request #190 from mscuthbert/stavemodifier-draw | ported | Stave modifier draw behavior is covered by stave modifier subclass tests and renderer-call tests. |
| `095001ce` | Merge pull request #189 from mscuthbert/remove-outdated-comment | not applicable | Documentation/comment cleanup only. |
| `7e7412be` | Merge upstream/main into stavetie-cleanup | ported | Merge bookkeeping; StaveTie behavior is covered above. |
| `311fb13c` | Merge pull request #188 from mscuthbert/formatter-comments | not applicable | Formatter comment/documentation cleanup only. |
| `cc775a44` | Merge pull request #193 from mscuthbert/remove-hash-private | not applicable | TypeScript syntax cleanup merge. |
| `573e5984` | Update bundled Gonville font. | deferred | Alternate Gonville font asset support remains deferred. |
| `70d8a0f6` | Use nullish coalescing operator with minus | ported | C# null-handling equivalents are covered by formatting/layout tests for affected paths. |
| `e962d2f4` | fix tabstave measure numbers | ported | `TabNoteTests`/tab stave tests cover metric-backed tab stave font size, spacing, and draw integration; measure-number rendering is inherited from `Stave`. |
| `3830bc75` | Adjust pedal markings end | ported | `PedalMarkingTests` cover bracket/text rendering and release/depress endpoint behavior. |
| `f00f5459` | comments added | not applicable | Documentation/comment cleanup only. |
| `bcc27b33` | align text | ported | Text alignment/width behavior is covered by annotation, chord symbol, text note, and text formatter tests. |
| `21d2e9e8` | Merge pull request #209 from rvilarl/issue204 | ported | Merge commit for text alignment; evidence above. |
| `aa46fd28` | fix unison notehead accidental collision with stems down | ported | `StaveNoteTests`, `StaveNoteRenderingTests`, and `AccidentalTests` cover unison/displaced noteheads and accidental collision behavior. |
| `fcd9506f` | review comments | ported | Folded into unison accidental collision coverage. |
| `32aebebd` | fix clear, no scale required | ported | Skia/recording render-context tests cover clear/rect calls without extra scale assumptions. |
| `5c987d76` | new element style infrastructure | ported | `ElementTests` cover style application, `DrawWithStyle()`, group style, and save/restore behavior. |
| `4848ce99` | review comments | ported | Folded into element style coverage. |
| `53a9d9e1` | Merge pull request #214 from rvilarl/issue202 | ported | Merge commit for clear/render-context behavior; evidence above. |
| `21a4f899` | lint fixes | not applicable | TypeScript lint formatting only. |
| `22e518a0` | Merge pull request #222 from rvilarl/lint | not applicable | TypeScript lint merge only. |
| `c2f2b809` | fix curve api | ported | `CurveTests` and factory tests cover v5 curve options and API behavior. |
| `547b3389` | review comments | ported | Folded into curve API coverage. |
| `c119acf7` | refactor to ctx save restore | ported | Element/style tests and Skia state-preservation tests cover save/restore style paths. |
| `5794f5e5` | refactor applyStyle | ported | `ElementTests` cover `ApplyStyle()` and `DrawWithStyle()` behavior. |
| `eaccd73d` | stavesection with configurable padding | ported | `StaveSectionTests` cover metric-backed configurable padding and draw placement. |
| `7152153c` | tuplet text position configurable | ported | `TupletTests` cover text/bracket metrics and y-offset positioning. |
| `21e635e5` | Merge pull request #233 from rvilarl/issue62 | ported | Merge commit for stave section/tuplet configurable behavior; evidence above. |
| `7161ea85` | Merge pull request #227 from rvilarl/issue223p2 | ported | Merge commit for style save/restore refactors; evidence above. |
| `325899b3` | remove `!!` | not applicable | TypeScript lint/syntax cleanup only. |
| `a12e4aca` | fix gracenote size | ported | `GraceNoteTests` and grace group tests cover grace-note scale, grouping, and render behavior. |
| `1a6521cd` | allow right position on gracenotes | ported | `GraceNoteTests` and modifier placement tests cover grace-note modifier behavior. |
| `83bc3489` | grunt eslint | not applicable | TypeScript lint automation only. |
| `49c9011e` | tests calling `drawWithStyle` | ported | C# draw paths use `DrawWithStyle()` for migrated elements and have focused renderer-call coverage. |
| `e51eb042` | stave modifiers calling `drawWithStyle` | ported | Stave modifier tests and `ElementTests` cover styled draw behavior. |
| `54acdbb9` | refactor boundingbox calculations | ported | Formatter/system/note bounding-box tests cover migrated bounding-box calculations. |
| `d72beb1a` | fix natural padding | ported | `AccidentalTests` and key signature tests cover metric-backed accidental/natural padding. |
| `bf4f936c` | review comments | ported | Folded into natural padding/bounding-box coverage. |
| `13020862` | review comments | ported | Folded into natural padding/bounding-box coverage. |
| `9add5ba5` | Merge pull request #242 from rvilarl/issue223p3 | ported | Merge commit for style/bounding-box/natural-padding fixes; evidence in the preceding entries. |
| `963c168e` | refactor calls to applystyle | ported | Migrated draw paths use `DrawWithStyle()` / `ApplyStyle()` where applicable; covered by `ElementTests` and renderer-call tests. |
| `f524dc1f` | review comments | ported | Folded into apply-style refactor coverage. |
| `8756cbd8` | review comments | ported | Folded into apply-style refactor coverage. |
| `97ecb6c9` | Allow to get width without stave | ported | `KeySignatureTests`, signature wrapper tests, and text/chord width tests cover width calculation without a fully drawn stave. |
| `39e79251` | review comments | ported | Folded into width-without-stave coverage. |
| `33128311` | changed stave line drawing to place them more correctly so that the stroke-width doesn't get anti aliased | ported | `StaveTests` cover line width/style and hidden-line rendering; final pixel parity remains tracked under visual coverage. |
| `0aaa53ee` | added grouping to tuplet rendering | ported | `TupletTests` and `RenderContextTests` cover tuplet render grouping. |
| `bf48f9bb` | Draw idempotent | not applicable | Upstream moved mutable glyph-origin assignment out of `Articulation.draw()`; C# articulation rendering does not expose that mutable glyph-origin path, and articulation placement is covered by formatter/draw tests. |
| `9cbac77c` | metrics exported | ported | C# exposes backend-neutral `Metrics` plus `VexFlow` table constants; `MetricsTests` and `VexFlowTests` cover public access. |

## Non-Source Commit Audit

The full range contains 467 commits. The detailed table above classifies every commit that touched `src/`:

```sh
git -C vexflow log --oneline --reverse 4.2.2..5.0.0 -- src
```

The remaining non-source-only commits are obtained with:

```sh
comm -23 \
  <(git -C vexflow log --format='%h %s' 4.2.2..5.0.0 | sort) \
  <(git -C vexflow log --format='%h %s' 4.2.2..5.0.0 -- src | sort)
```

Classification for that non-source-only set:

| Non-source area | Status | Reason / C# disposition |
|---|---:|---|
| Release/package churn (`package.json`, package lockfiles, release commits, build removal, npm target changes, release-it setup, package names) | not applicable | NuGet/project packaging is independent of upstream npm release mechanics. |
| Generated JS build output under `build/` | not applicable | C# builds from source projects; upstream generated JS bundles are not ported. |
| TypeScript lint/prettier/eslint/grunt/CI-only changes | not applicable | C# formatting/build verification is handled by the .NET solution and NUnit tests. |
| Docs/readme/changelog/authors/license/repo URL changes | not applicable | These do not change VexFlow runtime behavior; C# README has its own migration/runtime notes. |
| Browser demos, legacy test pages, glyph inspectors, jsdom image-generation infrastructure, font demo pages | not applicable | Browser/demo tooling does not map to the supported C# `RenderContext`/Skia/Unity model. |
| Upstream visual test reference logistics and font-name sanitization | deferred | Representative image comparisons remain a separate C# migration gate. |
| Alternate font package churn (`@vexflow-fonts`, Gonville/Petaluma/Leland demo/test package updates) | deferred | Bravura is supported; multi-font runtime assets remain tracked under font/metrics remaining work. |
| Upstream test-only commits for behavior also present in `src/` commits | ported | Behavior is classified in the source table and covered by the referenced C# tests. |

Representative non-source-only behavior/test commits accounted for by source-table evidence:

| Hash | Subject | Status | Evidence / reason |
|---|---|---:|---|
| `260f1fe4` | Added test cases for Ornament, for testing delayed turns. | ported | `OrnamentTests` cover delayed ornament positioning and render calls. |
| `ec1b6f94` | Modified ornament test case for VexFlow5. | ported | `OrnamentTests` cover migrated v5 ornament behavior. |
| `4ab869d2` / `86bb364a` | support single digit time signatures / merge | ported | Time signature wrapper/stave tests cover single-signature construction and rendering paths. |
| `b34683cb` | pedalmarking tests with two voices | ported | `PedalMarkingTests` cover text/bracket styles and formatted voice endpoint behavior. |
| `62d1dcc7` | Font-name spaces in jsdom visual tests. | deferred | Visual parity/font-name image generation remains in the image comparison gate. |
| `8cdb5813` | Adds Sebastian and Leipzig as test fonts. | deferred | Alternate test fonts are part of deferred multi-font/image parity work. |
| `5368a471` / `5d7a5bd6` | Limit/adjust browser font tests. | deferred | Browser/jsdom font test logistics do not map directly to C# runtime packages. |

Full audit status:

- `src/` commits: 230 classified in the detailed table above.
- Non-source-only commits: classified by the reproducible command-defined categories above.
- No unexplained `remaining` entries are currently recorded.

## Next Audit Tranche

The upstream commit audit is complete for the local `4.2.2..5.0.0` range. Future audit work should only be needed if a fuller upstream baseline, such as a real `4.2.6` tag, is added to `vexflow/`.

```sh
git -C vexflow tag --list "4*"
```

The visual parity/image-comparison gate is now closed for generated VexFlow 5 scenes. MusicXML real-score excerpts remain C# output/smoke scenes because this repository has no local VexFlow 5 MusicXML reference renderer.

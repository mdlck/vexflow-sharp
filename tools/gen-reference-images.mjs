#!/usr/bin/env node
// gen-reference-images.mjs
// Generates VexFlow reference PNGs for image comparison tests.
// Usage: node tools/gen-reference-images.mjs
//
// Prerequisites:
//   npm install canvas           (in project root)
//   vexflow must be built:       cd vexflow && npx grunt
//
// Output: VexFlowSharp.Tests/reference-images/*.png
//
// Phase 1: simple_rect.png — baseline image comparison test
// Phase 3 (03-05): Four formatter scenarios matching Phase 3 C# test cases
//   - formatter_single_voice_4_4.png  — 4 quarter notes in 4/4, single voice
//   - formatter_two_voice.png         — two voices on same stave
//   - beam_eighth_notes.png           — 8 eighth notes with autoBeam
//   - formatter_chromatic_accidentals.png — chromatic scale with sharps
// Visual parity gallery:
//   - complex_notation-vexflow.png
//   - percussion_*-vexflow.png
//   - gallery_tab_notation-vexflow.png
//   - gallery_grace_notes-vexflow.png
//   - gallery_small_modifiers-vexflow.png
//   - gallery_stave_modifiers-vexflow.png
//
// Canvas size and stave position must match exactly what FormatterRenderingTests.cs uses.
// (500x200 px, Stave at x=10, y=40, width=400)

'use strict';

import { createCanvas, registerFont } from 'canvas';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { createRequire } from 'module';

const require = createRequire(import.meta.url);
const __dirname = path.dirname(fileURLToPath(import.meta.url));

if (typeof globalThis.document === 'undefined') {
  globalThis.document = {
    createElement(tagName) {
      if (tagName === 'canvas') return createCanvas(300, 150);
      const style = {};
      Object.defineProperty(style, 'font', {
        set(value) {
          this._font = value;
          const familyMatch = /(?:^|\s)([^,\s][^,]*?)\s*$/.exec(value);
          const sizeMatch = /(\d+(?:\.\d+)?(?:px|pt|em|%))/.exec(value);
          this.fontFamily = familyMatch ? familyMatch[1].replace(/^['"]|['"]$/g, '') : 'Arial';
          this.fontSize = sizeMatch ? sizeMatch[1] : '10pt';
          this.fontWeight = /\bbold\b|[6-9]00/.test(value) ? 'bold' : 'normal';
          this.fontStyle = /\bitalic\b/.test(value) ? 'italic' : 'normal';
        },
        get() {
          return this._font ?? '';
        },
      });
      return { style };
    },
    getElementById() {
      return null;
    },
  };
}

registerFont(path.join(__dirname, '../vexflow/node_modules/@vexflow-fonts/bravura/bravura.otf'), { family: 'Bravura' });

const VF = require('../vexflow/build/cjs/vexflow.js');
const {
  Stave,
  StaveNote,
  Formatter,
  Voice,
  Renderer,
  Beam,
  Accidental,
  Stem,
  Element,
  Factory,
  Dot,
  GraceNote,
  GraceNoteGroup,
  TabStave,
  TabNote,
  TabTie,
  TabSlide,
  PedalMarking,
  TextBracket,
  Tremolo,
  FretHandFinger,
  StringNumber,
  ChordSymbol,
  Vibrato,
  VibratoBracket,
  Repetition,
  Volta,
  StaveModifierPosition,
  TextNote,
  Modifier,
} = VF;

// VexFlow 5 measures Element text through a process-global offscreen canvas.
// Node does not provide document/OffscreenCanvas, so wire node-canvas in explicitly.
Element.setTextMeasurementCanvas(createCanvas(300, 150));

// Output directories
const LEGACY_OUTPUT_DIR = path.join(__dirname, '../VexFlowSharp.Tests/Infrastructure/ReferenceImages');
const FORMATTER_OUTPUT_DIR = path.join(__dirname, '../VexFlowSharp.Tests/reference-images');

fs.mkdirSync(LEGACY_OUTPUT_DIR, { recursive: true });
fs.mkdirSync(FORMATTER_OUTPUT_DIR, { recursive: true });

// Canvas dimensions and stave position shared between JS reference and C# tests
const CANVAS_WIDTH = 500;
const CANVAS_HEIGHT = 200;
const STAVE_X = 10;
const STAVE_Y = 40;
const STAVE_WIDTH = 400;

// ── Helper: create a canvas with white background ──────────────────────────

function makeCanvas() {
  return makeSizedCanvas(CANVAS_WIDTH, CANVAS_HEIGHT);
}

function makeSizedCanvas(width, height) {
  const canvas = createCanvas(width, height);
  const renderer = new Renderer(canvas, Renderer.Backends.CANVAS);
  const context = renderer.getContext();
  // White background
  context.save();
  context.fillStyle = '#FFFFFF';
  context.fillRect(0, 0, width, height);
  context.restore();
  return { canvas, context };
}

function writeReference(canvas, filename) {
  const buffer = canvas.toBuffer('image/png');
  fs.writeFileSync(path.join(FORMATTER_OUTPUT_DIR, filename), buffer);
  console.log(`Generated: ${filename}`);
}

function makeFactory(context, width, height) {
  return new Factory({ renderer: { elementId: null, width, height } }).setContext(context);
}

// ── Legacy Phase 1: Simple rect (baseline) ────────────────────────────────

function generateSimpleRect() {
  const canvas = createCanvas(100, 100);
  const ctx = canvas.getContext('2d');
  ctx.fillStyle = '#FFFFFF';
  ctx.fillRect(0, 0, 100, 100);
  ctx.fillStyle = '#000000';
  ctx.fillRect(10, 10, 30, 30);
  const buffer = canvas.toBuffer('image/png');
  fs.writeFileSync(path.join(LEGACY_OUTPUT_DIR, 'simple_rect.png'), buffer);
  console.log('Generated: simple_rect.png (legacy)');
}

// ── Scenario 1: Single voice 4/4 — 4 quarter notes ────────────────────────
// Matches FormatterRenderingTests.cs::SingleVoice_4_4_MatchesReference
//   new Stave(10, 40, 400).addClef("treble").addTimeSignature("4/4")
//   notes: C4, D4, E4, F4 quarter
//   Formatter.FormatAndDraw(ctx, stave, notes)

function generateSingleVoice4_4() {
  const { canvas, context } = makeCanvas();

  const stave = new Stave(STAVE_X, STAVE_Y, STAVE_WIDTH);
  stave.addClef('treble').addTimeSignature('4/4').setContext(context).draw();

  const notes = [
    new StaveNote({ keys: ['c/4'], duration: '4', clef: 'treble' }),
    new StaveNote({ keys: ['d/4'], duration: '4', clef: 'treble' }),
    new StaveNote({ keys: ['e/4'], duration: '4', clef: 'treble' }),
    new StaveNote({ keys: ['f/4'], duration: '4', clef: 'treble' }),
  ];

  const voice = new Voice({ num_beats: 4, beat_value: 4 });
  voice.addTickables(notes);
  new Formatter().joinVoices([voice]).formatToStave([voice], stave);
  voice.draw(context, stave);

  const noteXPositions = notes.map(n => n.getAbsoluteX());
  console.log('  SingleVoice note x-positions:', noteXPositions);

  const buffer = canvas.toBuffer('image/png');
  fs.writeFileSync(path.join(FORMATTER_OUTPUT_DIR, 'formatter_single_voice_4_4.png'), buffer);
  console.log('Generated: formatter_single_voice_4_4.png');
  return noteXPositions;
}

// ── Scenario 2: Two-voice measure ─────────────────────────────────────────
// Matches FormatterRenderingTests.cs::TwoVoice_MatchesReference
//   Voice1: C5, D5, E5, F5 quarter notes (stem up)
//   Voice2: C4, E4 half notes (stem down)
//   formatter.JoinVoices().FormatToStave()

function generateTwoVoice() {
  const { canvas, context } = makeCanvas();

  const stave = new Stave(STAVE_X, STAVE_Y, STAVE_WIDTH);
  stave.addClef('treble').addTimeSignature('4/4').setContext(context).draw();

  const voice1Notes = [
    new StaveNote({ keys: ['c/5'], duration: '4', stemDirection: Stem.UP }),
    new StaveNote({ keys: ['d/5'], duration: '4', stemDirection: Stem.UP }),
    new StaveNote({ keys: ['e/5'], duration: '4', stemDirection: Stem.UP }),
    new StaveNote({ keys: ['f/5'], duration: '4', stemDirection: Stem.UP }),
  ];
  const voice2Notes = [
    new StaveNote({ keys: ['c/4'], duration: '2', stemDirection: Stem.DOWN }),
    new StaveNote({ keys: ['e/4'], duration: '2', stemDirection: Stem.DOWN }),
  ];

  const voice1 = new Voice({ num_beats: 4, beat_value: 4 });
  const voice2 = new Voice({ num_beats: 4, beat_value: 4 });
  voice1.addTickables(voice1Notes);
  voice2.addTickables(voice2Notes);

  new Formatter().joinVoices([voice1, voice2]).formatToStave([voice1, voice2], stave);
  voice1.draw(context, stave);
  voice2.draw(context, stave);

  const v1x = voice1Notes.map(n => n.getAbsoluteX());
  const v2x = voice2Notes.map(n => n.getAbsoluteX());
  console.log('  TwoVoice V1 x-positions:', v1x);
  console.log('  TwoVoice V2 x-positions:', v2x);

  const buffer = canvas.toBuffer('image/png');
  fs.writeFileSync(path.join(FORMATTER_OUTPUT_DIR, 'formatter_two_voice.png'), buffer);
  console.log('Generated: formatter_two_voice.png');
  return { v1x, v2x };
}

// ── Scenario 3: Beamed eighth notes ───────────────────────────────────────
// Matches FormatterRenderingTests.cs::BeamEighthNotes_MatchesReference
//   8 eighth notes: C4, D4, E4, F4, G4, A4, B4, C5
//   AutoBeam via Beam.generateBeams()

function generateBeamEighthNotes() {
  const { canvas, context } = makeCanvas();

  const stave = new Stave(STAVE_X, STAVE_Y, STAVE_WIDTH);
  stave.addClef('treble').addTimeSignature('4/4').setContext(context).draw();

  const eighthNotes = [
    new StaveNote({ keys: ['c/4'], duration: '8' }),
    new StaveNote({ keys: ['d/4'], duration: '8' }),
    new StaveNote({ keys: ['e/4'], duration: '8' }),
    new StaveNote({ keys: ['f/4'], duration: '8' }),
    new StaveNote({ keys: ['g/4'], duration: '8' }),
    new StaveNote({ keys: ['a/4'], duration: '8' }),
    new StaveNote({ keys: ['b/4'], duration: '8' }),
    new StaveNote({ keys: ['c/5'], duration: '8' }),
  ];

  const voice = new Voice({ num_beats: 4, beat_value: 4 });
  voice.addTickables(eighthNotes);

  // AutoBeam: generateBeams groups notes by beat
  const beams = Beam.generateBeams(eighthNotes);

  new Formatter().joinVoices([voice]).formatToStave([voice], stave);
  voice.draw(context, stave);
  beams.forEach(beam => beam.setContext(context).draw());

  const xPositions = eighthNotes.map(n => n.getAbsoluteX());
  console.log('  Beam8ths x-positions:', xPositions);

  const buffer = canvas.toBuffer('image/png');
  fs.writeFileSync(path.join(FORMATTER_OUTPUT_DIR, 'beam_eighth_notes.png'), buffer);
  console.log('Generated: beam_eighth_notes.png');
  return xPositions;
}

// ── Scenario 4: Chromatic scale with accidentals ──────────────────────────
// Matches FormatterRenderingTests.cs::ChromaticAccidentals_MatchesReference
//   8 eighth notes: C4, C#4, D4, D#4, E4, F4, F#4, G4
//   Accidentals added via note.addModifier(new Accidental('#'))

function generateChromaticAccidentals() {
  const { canvas, context } = makeCanvas();

  const stave = new Stave(STAVE_X, STAVE_Y, STAVE_WIDTH);
  stave.addClef('treble').addTimeSignature('4/4').setContext(context).draw();

  // Chromatic scale: C4, C#4, D4, D#4, E4, F4, F#4, G4
  const noteDefinitions = [
    { key: 'c/4', accidental: null },
    { key: 'c#/4', accidental: '#' },
    { key: 'd/4', accidental: null },
    { key: 'd#/4', accidental: '#' },
    { key: 'e/4', accidental: null },
    { key: 'f/4', accidental: null },
    { key: 'f#/4', accidental: '#' },
    { key: 'g/4', accidental: null },
  ];

  const chromaNotes = noteDefinitions.map(def => {
    const note = new StaveNote({ keys: [def.key], duration: '8' });
    if (def.accidental) {
      note.addModifier(new Accidental(def.accidental));
    }
    return note;
  });

  const voice = new Voice({ num_beats: 4, beat_value: 4 });
  voice.addTickables(chromaNotes);

  new Formatter().joinVoices([voice]).formatToStave([voice], stave);
  voice.draw(context, stave);

  const xPositions = chromaNotes.map(n => n.getAbsoluteX());
  console.log('  Chromatic x-positions:', xPositions);

  const buffer = canvas.toBuffer('image/png');
  fs.writeFileSync(path.join(FORMATTER_OUTPUT_DIR, 'formatter_chromatic_accidentals.png'), buffer);
  console.log('Generated: formatter_chromatic_accidentals.png');
  return xPositions;
}

// -- Visual parity gallery references ---------------------------------------

function generateComplexNotationReference() {
  const { canvas, context } = makeSizedCanvas(600, 200);

  const stave = new Stave(10, 40, 570);
  stave.addClef('treble').addTimeSignature('4/4').setContext(context).draw();

  const dotted8thC4 = new StaveNote({ keys: ['c/4'], duration: '8d' });
  dotted8thC4.addModifier(new Accidental('##'), 0);

  const sixteenthD4 = new StaveNote({ keys: ['d/4'], duration: '16' });
  sixteenthD4.addModifier(new Accidental('b'), 0);

  const quarterE4First = new StaveNote({ keys: ['e/4'], duration: 'q' });
  const quarterE4Second = new StaveNote({ keys: ['e/4'], duration: 'q' });
  const quarterG4 = new StaveNote({ keys: ['g/4'], duration: 'q' });

  const notes = [dotted8thC4, sixteenthD4, quarterE4First, quarterE4Second, quarterG4];
  const voice = new Voice({ num_beats: 4, beat_value: 4 });
  voice.addTickables(notes);
  const beams = Beam.generateBeams([dotted8thC4, sixteenthD4]);
  const tie = new VF.StaveTie({
    firstNote: quarterE4First,
    lastNote: quarterE4Second,
    firstIndexes: [0],
    lastIndexes: [0],
  });

  new Formatter().joinVoices([voice]).format([voice], 500);
  voice.draw(context, stave);
  beams.forEach(beam => beam.setContext(context).draw());
  tie.setContext(context).draw();

  writeReference(canvas, 'complex_notation-vexflow.png');
}

function generateTabNotationReference() {
  const { canvas, context } = makeSizedCanvas(700, 190);
  const stave = new TabStave(10, 25, 660);
  stave.addTabGlyph().setContext(context).draw();

  const notes = [
    new TabNote({ positions: [{ str: 6, fret: 3 }], duration: '8' }, true),
    new TabNote({ positions: [{ str: 5, fret: 5 }, { str: 4, fret: 5 }], duration: '8' }, true),
    new TabNote({ positions: [{ str: 2, fret: 'x' }, { str: 3, fret: 7 }], duration: '4d' }, true),
    new TabNote({ positions: [{ str: 2, fret: 8 }, { str: 4, fret: 9 }], duration: '8' }, true),
    new TabNote({ positions: [{ str: 1, fret: 10 }, { str: 3, fret: 9 }], duration: '4' }, true),
  ];

  Dot.buildAndAttach([notes[2]], { all: true });

  const voice = new Voice({ num_beats: 4, beat_value: 4 });
  voice.addTickables(notes);
  new Formatter().joinVoices([voice]).formatToStave([voice], stave);
  voice.draw(context, stave);

  new Beam([notes[0], notes[1]]).setContext(context).draw();
  new TabSlide({ firstNote: notes[1], lastNote: notes[3], firstIndexes: [0] }).setContext(context).draw();
  new TabTie({ firstNote: notes[3], lastNote: notes[4], firstIndexes: [1], lastIndexes: [1] }).setContext(context).draw();

  writeReference(canvas, 'gallery_tab_notation-vexflow.png');
}

function generateGraceNotesReference() {
  const { canvas, context } = makeSizedCanvas(760, 170);
  const f = makeFactory(context, 760, 170);
  const stave = f.Stave({ x: 10, y: 25, width: 730 }).addClef('treble').addTimeSignature('4/4');

  const graceA = ['e/4', 'f/4', 'g/4'].map(key => new GraceNote({ keys: [key], duration: '32' }));
  graceA[1].addModifier(new Accidental('#'), 0);
  const graceB = [new GraceNote({ keys: ['b/4'], duration: '8', slash: true })];
  const graceC = [
    new GraceNote({ keys: ['e/4'], duration: '8' }),
    new GraceNote({ keys: ['f/4'], duration: '16' }),
    new GraceNote({ keys: ['e/4', 'g/4'], duration: '8' }),
  ];
  graceC[2].addModifier(new Accidental('n'), 0);

  const notes = [
    new StaveNote({ keys: ['b/4'], duration: '4', autoStem: true }).addModifier(new GraceNoteGroup(graceA, true).beamNotes(), 0),
    new StaveNote({ keys: ['c/5'], duration: '4', autoStem: true }).addModifier(new GraceNoteGroup(graceB, true).beamNotes(), 0),
    new StaveNote({ keys: ['c/5', 'd/5'], duration: '4', autoStem: true }).addModifier(new GraceNoteGroup(graceC, true).beamNotes(), 0),
    new StaveNote({ keys: ['a/4'], duration: '4', autoStem: true }),
  ];

  const voice = f.Voice().setStrict(false).addTickables(notes);
  f.Formatter().joinVoices([voice]).formatToStave([voice], stave);
  stave.setContext(context).draw();
  voice.draw(context, stave);

  writeReference(canvas, 'gallery_grace_notes-vexflow.png');
}

function generateSmallModifiersReference() {
  const { canvas, context } = makeSizedCanvas(760, 250);
  const f = makeFactory(context, 760, 250);
  const stave = f.Stave({ x: 10, y: 60, width: 730 }).addClef('treble').addTimeSignature('4/4');

  const superscript = { symbolModifier: ChordSymbol.symbolModifiers.SUPERSCRIPT };
  const subscript = { symbolModifier: ChordSymbol.symbolModifiers.SUBSCRIPT };
  const notes = [
    f.StaveNote({ keys: ['c/4'], duration: 'q' })
      .addModifier(new Tremolo(3))
      .addModifier(new FretHandFinger('1').setPosition(Modifier.Position.LEFT), 0),
    f.StaveNote({ keys: ['e/4'], duration: 'q' })
      .addModifier(new StringNumber('3').setPosition(Modifier.Position.ABOVE), 0)
      .addModifier(f.ChordSymbol({ fontSize: 14 }).addText('F7').addGlyphOrText('b9', superscript).addGlyphOrText('#11', subscript), 0),
    f.StaveNote({ keys: ['g/4'], duration: 'q' }).addModifier(new Vibrato(), 0),
    f.StaveNote({ keys: ['c/5'], duration: 'q' }),
  ];

  const voice = f.Voice().addTickables(notes);
  f.TextBracket({ from: notes[0], to: notes[3], text: '8', options: { superscript: 'va', position: 'top' } });
  f.PedalMarking({ notes: [notes[0], notes[2], notes[2], notes[3]], options: { style: 'mixed' } });
  f.Formatter().joinVoices([voice]).formatToStave([voice], stave);
  f.draw();
  new VibratoBracket({ start: notes[1], stop: notes[3] }).setContext(context).draw();

  writeReference(canvas, 'gallery_small_modifiers-vexflow.png');
}

function generateStaveModifiersReference() {
  const { canvas, context } = makeSizedCanvas(760, 230);
  const stave = new Stave(20, 75, 700);
  stave
    .setTempo({ name: 'Allegro', duration: 'q', bpm: 120 }, -10)
    .setSection('A', -5)
    .setVoltaType(Volta.type.BEGIN_END, '1.', -20)
    .setRepetitionType(Repetition.type.DC_AL_CODA, 10)
    .setStaveText('cantabile', StaveModifierPosition.BELOW, {
      justification: TextNote.Justification.CENTER,
    })
    .addClef('treble')
    .addTimeSignature('4/4')
    .setContext(context)
    .draw();

  const notes = [
    new StaveNote({ keys: ['c/4'], duration: 'q' }),
    new StaveNote({ keys: ['d/4'], duration: 'q' }),
    new StaveNote({ keys: ['e/4'], duration: 'q' }),
    new StaveNote({ keys: ['f/4'], duration: 'q' }),
  ];
  const voice = new Voice({ num_beats: 4, beat_value: 4 }).addTickables(notes);
  new Formatter().joinVoices([voice]).formatToStave([voice], stave);
  voice.draw(context, stave);

  writeReference(canvas, 'gallery_stave_modifiers-vexflow.png');
}

function generatePercussionClefReference() {
  const { canvas, context } = makeSizedCanvas(400, 120);
  new Stave(10, 10, 300).addClef('percussion').setContext(context).draw();
  writeReference(canvas, 'percussion_clef-vexflow.png');
}

function generatePercussionBasic0Reference() {
  const { canvas, context } = makeSizedCanvas(500, 200);
  const f = makeFactory(context, 500, 200);
  const stave = f.Stave().addClef('percussion').addTimeSignature('4/4');

  const voice0 = f.Voice().addTickables([
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
  ]);
  const voice1 = f.Voice().addTickables([
    f.StaveNote({ keys: ['f/4'], duration: '8', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['f/4'], duration: '8', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['d/4/x2', 'c/5'], duration: '4', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['f/4'], duration: '8', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['f/4'], duration: '8', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['d/4/x2', 'c/5'], duration: '4', stemDirection: Stem.DOWN }),
  ]);

  f.Beam({ notes: voice0.getTickables() });
  f.Beam({ notes: voice1.getTickables().slice(0, 2) });
  f.Beam({ notes: voice1.getTickables().slice(3, 5) });
  f.Formatter().joinVoices(f.getVoices()).formatToStave(f.getVoices(), stave);
  f.draw();
  writeReference(canvas, 'percussion_basic0-vexflow.png');
}

function generatePercussionBasic1Reference() {
  const { canvas, context } = makeSizedCanvas(500, 200);
  const f = makeFactory(context, 500, 200);
  const stave = f.Stave().addClef('percussion').addTimeSignature('4/4');

  f.Voice().addTickables([
    f.StaveNote({ keys: ['f/5/x2'], duration: '4' }),
    f.StaveNote({ keys: ['f/5/x2'], duration: '4' }),
    f.StaveNote({ keys: ['f/5/x2'], duration: '4' }),
    f.StaveNote({ keys: ['f/5/x2'], duration: '4' }),
  ]);
  f.Voice().addTickables([
    f.StaveNote({ keys: ['f/4'], duration: '4', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['d/4/x2', 'c/5'], duration: '4', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['f/4'], duration: '4', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['d/4/x2', 'c/5'], duration: '4', stemDirection: Stem.DOWN }),
  ]);

  f.Formatter().joinVoices(f.getVoices()).formatToStave(f.getVoices(), stave);
  f.draw();
  writeReference(canvas, 'percussion_basic1-vexflow.png');
}

function generatePercussionBasic2Reference() {
  const { canvas, context } = makeSizedCanvas(500, 200);
  const f = makeFactory(context, 500, 200);
  const stave = f.Stave().addClef('percussion').addTimeSignature('4/4');

  const voice0 = f.Voice().addTickables([
    f.StaveNote({ keys: ['a/5/x3'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5'], duration: '8' }),
    f.StaveNote({ keys: ['g/4/n', 'g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '8' }),
  ]);
  f.Beam({ notes: voice0.getTickables().slice(1) });

  const note4 = f.StaveNote({ keys: ['d/4/x2', 'c/5'], duration: '8d', stemDirection: Stem.DOWN });
  const note5 = f.StaveNote({ keys: ['c/5'], duration: '16', stemDirection: Stem.DOWN });
  const voice1 = f.Voice().addTickables([
    f.StaveNote({ keys: ['f/4'], duration: '8', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['f/4'], duration: '8', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['d/4/x2', 'c/5'], duration: '4', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['f/4'], duration: '4', stemDirection: Stem.DOWN }),
    note4,
    note5,
  ]);
  Dot.buildAndAttach([note4], { all: true });
  f.Beam({ notes: voice1.getTickables().slice(0, 2) });
  f.Beam({ notes: [note4, note5] });

  f.Formatter().joinVoices(f.getVoices()).formatToStave(f.getVoices(), stave);
  f.draw();
  writeReference(canvas, 'percussion_basic2-vexflow.png');
}

function generatePercussionSnare0Reference() {
  const { canvas, context } = makeSizedCanvas(500, 200);
  const f = makeFactory(context, 500, 200);
  const stave = f.Stave().addClef('percussion').addTimeSignature('4/4');

  f.Voice().addTickables([
    f.StaveNote({ keys: ['c/5'], duration: '4', stemDirection: Stem.DOWN })
      .addModifier(f.Articulation({ type: 'a>' }), 0)
      .addModifier(f.Annotation({ text: 'L' }).setFont('Arial', 14, 'bold italic'), 0),
    f.StaveNote({ keys: ['c/5'], duration: '4', stemDirection: Stem.DOWN })
      .addModifier(f.Annotation({ text: 'R' }).setFont('Arial', 14, 'bold italic'), 0),
    f.StaveNote({ keys: ['c/5'], duration: '4', stemDirection: Stem.DOWN })
      .addModifier(f.Annotation({ text: 'L' }).setFont('Arial', 14, 'bold italic'), 0),
    f.StaveNote({ keys: ['c/5'], duration: '4', stemDirection: Stem.DOWN })
      .addModifier(f.Annotation({ text: 'L' }).setFont('Arial', 14, 'bold italic'), 0),
  ]);

  f.Formatter().joinVoices(f.getVoices()).formatToStave(f.getVoices(), stave);
  f.draw();
  writeReference(canvas, 'percussion_snare0-vexflow.png');
}

function generatePercussionSnare1Reference() {
  const { canvas, context } = makeSizedCanvas(500, 200);
  const f = makeFactory(context, 500, 200);
  const stave = f.Stave().addClef('percussion').addTimeSignature('4/4');

  f.Voice().addTickables([
    f.StaveNote({ keys: ['g/5/x2'], duration: '4', stemDirection: Stem.DOWN })
      .addModifier(f.Articulation({ type: 'ah' }), 0),
    f.StaveNote({ keys: ['g/5/x2'], duration: '4', stemDirection: Stem.DOWN }),
    f.StaveNote({ keys: ['g/5/x2'], duration: '4', stemDirection: Stem.DOWN })
      .addModifier(f.Articulation({ type: 'ah' }), 0),
    f.StaveNote({ keys: ['a/5/x3'], duration: '4', stemDirection: Stem.DOWN })
      .addModifier(f.Articulation({ type: 'a,' }), 0),
  ]);

  f.Formatter().joinVoices(f.getVoices()).formatToStave(f.getVoices(), stave);
  f.draw();
  writeReference(canvas, 'percussion_snare1-vexflow.png');
}

function generatePercussionReferences() {
  generatePercussionClefReference();
  generatePercussionBasic0Reference();
  generatePercussionBasic1Reference();
  generatePercussionBasic2Reference();
  generatePercussionSnare0Reference();
  generatePercussionSnare1Reference();
}

function makeGrandStaffParts(f) {
  const n1 = f.StaveNote({ keys: ['c/4'], duration: 'q' });
  const n2 = f.StaveNote({ keys: ['d/4'], duration: 'q' });
  const n3 = f.StaveNote({ keys: ['e/4'], duration: 'q' });
  const n4 = f.StaveNote({ keys: ['f/4'], duration: 'q' });
  const n5 = f.StaveNote({ keys: ['c/3'], duration: 'q', clef: 'bass' });
  const n6 = f.StaveNote({ keys: ['d/3'], duration: 'q', clef: 'bass' });
  const n7 = f.StaveNote({ keys: ['e/3'], duration: 'q', clef: 'bass' });
  const n8 = f.StaveNote({ keys: ['f/3'], duration: 'q', clef: 'bass' });
  const trebleVoice = f.Voice({ time: '4/4' }).addTickables([n1, n2, n3, n4]);
  const bassVoice = f.Voice({ time: '4/4' }).addTickables([n5, n6, n7, n8]);
  return { trebleVoice, bassVoice };
}

function generateSystemGrandStaffReference() {
  const { canvas, context } = makeSizedCanvas(600, 250);
  const f = makeFactory(context, 600, 250);
  const { trebleVoice, bassVoice } = makeGrandStaffParts(f);
  const system = f.System({ x: 10, y: 10, width: 570 });
  system.addStave({ voices: [trebleVoice] }).addClef('treble').addTimeSignature('4/4');
  system.addStave({ voices: [bassVoice] }).addClef('bass').addTimeSignature('4/4');
  f.draw();
  writeReference(canvas, 'system_grandstaff-vexflow.png');
}

function generateFactoryGrandStaffReference() {
  const { canvas, context } = makeSizedCanvas(600, 250);
  const f = makeFactory(context, 600, 250);
  const { trebleVoice, bassVoice } = makeGrandStaffParts(f);
  const system = f.System({ x: 10, y: 10, width: 570 });
  system.addStave({ voices: [trebleVoice] }).addClef('treble').addTimeSignature('4/4');
  system.addStave({ voices: [bassVoice] }).addClef('bass').addTimeSignature('4/4');
  system.addConnector('singleLeft');
  system.addConnector('brace');
  f.draw();
  writeReference(canvas, 'factory_grandstaff-vexflow.png');
}

function generateEasyScoreGrandStaffReference() {
  const { canvas, context } = makeSizedCanvas(600, 250);
  const f = makeFactory(context, 600, 250);
  const score = f.EasyScore();
  const trebleNotes = score.notes('C4/q, D4, E4, F4');
  const bassNotes = score.set({ clef: 'bass' }).notes('C3/q, D3, E3, F3', { clef: 'bass' });
  const trebleVoice = score.voice(trebleNotes);
  const bassVoice = score.voice(bassNotes);
  const system = f.System({ x: 10, y: 10, width: 570 });
  system.addStave({ voices: [trebleVoice] }).addClef('treble').addTimeSignature('4/4');
  system.addStave({ voices: [bassVoice] }).addClef('bass').addTimeSignature('4/4');
  system.addConnector('singleLeft');
  system.addConnector('brace');
  f.draw();
  writeReference(canvas, 'easyscore_grandstaff-vexflow.png');
}

function generateGrandStaffReferences() {
  generateSystemGrandStaffReference();
  generateFactoryGrandStaffReference();
  generateEasyScoreGrandStaffReference();
}

function generateRealScoreReferencePlaceholders() {
  // The C# comparison suite currently emits real-score outputs from imported
  // MusicXML. VexFlow 5 has no matching local MusicXML renderer in this repo,
  // so keep a small reference page that documents the required manual pairing.
  const { canvas, context } = makeSizedCanvas(760, 180);
  context.fillStyle = '#000000';
  context.font = '16px Arial';
  context.fillText('Real score excerpts: compare C# outputs manually against current VexFlow 5 renderings.', 20, 60);
  context.font = '13px Arial';
  context.fillText('Scenes: beethoven_op98, schubert_avemaria, percussion_*.', 20, 90);
  context.fillText('No VexFlow 5 MusicXML reference generator is available in this repository yet.', 20, 115);
  writeReference(canvas, 'gallery_real_score_manual-note.png');
}

// ── Main ───────────────────────────────────────────────────────────────────

console.log('Generating VexFlow reference images...');
console.log(`Canvas size: ${CANVAS_WIDTH}x${CANVAS_HEIGHT} px`);
console.log(`Stave: x=${STAVE_X}, y=${STAVE_Y}, width=${STAVE_WIDTH}`);
console.log('');

generateSimpleRect();

console.log('');
console.log('Phase 3 reference images:');

const singleVoiceXPositions = generateSingleVoice4_4();
const twoVoiceXPositions = generateTwoVoice();
const beamXPositions = generateBeamEighthNotes();
const chromaticXPositions = generateChromaticAccidentals();

console.log('');
console.log('Reference image generation complete.');
console.log(`Output directory: ${FORMATTER_OUTPUT_DIR}`);
console.log('');
console.log('These x-positions must match the C# rendering output to within ±0.5px:');
console.log('  SingleVoice notes:', JSON.stringify(singleVoiceXPositions.map(x => +x.toFixed(4))));
console.log('  TwoVoice V1 notes:', JSON.stringify(twoVoiceXPositions.v1x.map(x => +x.toFixed(4))));
console.log('  TwoVoice V2 notes:', JSON.stringify(twoVoiceXPositions.v2x.map(x => +x.toFixed(4))));
console.log('  Beam8ths notes:', JSON.stringify(beamXPositions.map(x => +x.toFixed(4))));
console.log('  Chromatic notes:', JSON.stringify(chromaticXPositions.map(x => +x.toFixed(4))));

console.log('');
console.log('Visual parity gallery references:');
generateComplexNotationReference();
generatePercussionReferences();
generateGrandStaffReferences();
generateTabNotationReference();
generateGraceNotesReference();
generateSmallModifiersReference();
generateStaveModifiersReference();
generateRealScoreReferencePlaceholders();

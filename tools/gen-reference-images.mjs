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
//
// Canvas size and stave position must match exactly what FormatterRenderingTests.cs uses.
// (500x200 px, Stave at x=10, y=40, width=400)

'use strict';

import { createCanvas } from 'canvas';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { createRequire } from 'module';

const require = createRequire(import.meta.url);
const __dirname = path.dirname(fileURLToPath(import.meta.url));

const VF = require('../vexflow/build/cjs/vexflow.js');
const { Stave, StaveNote, Formatter, Voice, Renderer, Beam, Accidental, Stem } = VF;

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
  const canvas = createCanvas(CANVAS_WIDTH, CANVAS_HEIGHT);
  const renderer = new Renderer(canvas, Renderer.Backends.CANVAS);
  const context = renderer.getContext();
  // White background
  context.save();
  context.fillStyle = '#FFFFFF';
  context.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
  context.restore();
  return { canvas, context };
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
    new StaveNote({ keys: ['c/5'], duration: '4', stem_direction: Stem.UP }),
    new StaveNote({ keys: ['d/5'], duration: '4', stem_direction: Stem.UP }),
    new StaveNote({ keys: ['e/5'], duration: '4', stem_direction: Stem.UP }),
    new StaveNote({ keys: ['f/5'], duration: '4', stem_direction: Stem.UP }),
  ];
  const voice2Notes = [
    new StaveNote({ keys: ['c/4'], duration: '2', stem_direction: Stem.DOWN }),
    new StaveNote({ keys: ['e/4'], duration: '2', stem_direction: Stem.DOWN }),
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

// ── Main ───────────────────────────────────────────────────────────────────

console.log('Generating VexFlow reference images...');
console.log(`Canvas size: ${CANVAS_WIDTH}x${CANVAS_HEIGHT} px`);
console.log(`Stave: x=${STAVE_X}, y=${STAVE_Y}, width=${STAVE_WIDTH}`);
console.log('');

generateSimpleRect();

console.log('');
console.log('Phase 3 reference images:');

const singleVoiceXPositions = generateSingleVoice4_4();
generateTwoVoice();
generateBeamEighthNotes();
generateChromaticAccidentals();

console.log('');
console.log('Reference image generation complete.');
console.log(`Output directory: ${FORMATTER_OUTPUT_DIR}`);
console.log('');
console.log('These x-positions must match the C# rendering output to within ±0.5px:');
console.log('  SingleVoice notes:', JSON.stringify(singleVoiceXPositions.map(x => +x.toFixed(4))));

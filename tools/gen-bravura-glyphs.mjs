#!/usr/bin/env node
// tools/gen-bravura-glyphs.mjs
// Node.js ESM code generator: reads vexflow/src/fonts/bravura_glyphs.ts
// and emits VexFlowSharp.Common/Fonts/Generated/BravuraGlyphs.cs
// Regenerate with: node tools/gen-bravura-glyphs.mjs

import { readFileSync, writeFileSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');

const inputPath = resolve(repoRoot, 'vexflow/src/fonts/bravura_glyphs.ts');
const outputPath = resolve(repoRoot, 'VexFlowSharp.Common/Fonts/Generated/BravuraGlyphs.cs');

// Read the TypeScript source as text
const source = readFileSync(inputPath, 'utf8');

// Parse glyph entries from the source text using a regex approach.
// Each glyph entry has the shape:
//   glyphName: {
//     x_min: N,
//     x_max: N,
//     y_min: N,   (optional — may be absent or present)
//     y_max: N,   (optional)
//     ha: N,
//     o: 'outline string',
//   },
//
// We parse them sequentially by finding named property blocks inside the glyphs: { } block.

function parseGlyphs(source) {
  // Find the glyphs: { ... } block
  const glyphsStart = source.indexOf('glyphs: {');
  if (glyphsStart === -1) throw new Error('Could not find glyphs: { in source');

  // We'll walk character-by-character to extract each glyph block
  // Find the opening brace of the glyphs object
  const glyphsOpenBrace = source.indexOf('{', glyphsStart + 8);

  // Extract all glyph entries by regex: look for "  glyphName: {" patterns
  // Glyph names can contain letters, digits, underscore
  const glyphEntryRegex = /\n    (\w+):\s*\{([^}]+)\}/g;

  const glyphs = [];
  let match;
  while ((match = glyphEntryRegex.exec(source)) !== null) {
    const name = match[1];
    const body = match[2];

    // Parse fields from body
    const xMinM = body.match(/x_min:\s*(-?\d+)/);
    const xMaxM = body.match(/x_max:\s*(-?\d+)/);
    const yMinM = body.match(/y_min:\s*(-?\d+)/);
    const yMaxM = body.match(/y_max:\s*(-?\d+)/);
    const haM = body.match(/ha:\s*(-?\d+)/);
    const oM = body.match(/o:\s*'([^']+)'/);

    if (!xMinM || !xMaxM || !haM || !oM) {
      // Skip entries that don't have required fields (shouldn't happen)
      continue;
    }

    glyphs.push({
      name,
      xMin: parseInt(xMinM[1], 10),
      xMax: parseInt(xMaxM[1], 10),
      yMin: yMinM ? parseInt(yMinM[1], 10) : null,
      yMax: yMaxM ? parseInt(yMaxM[1], 10) : null,
      ha: parseInt(haM[1], 10),
      outline: parseOutline(oM[1]),
    });
  }

  return glyphs;
}

// Parse an outline string into a pre-parsed int[] array.
// Format:
//   m x y       -> [0, x, y]               MOVE
//   l x y       -> [1, x, y]               LINE
//   q ex ey cx cy -> [2, ex, ey, cx, cy]   QUADRATIC (endpoint first, then control)
//   b ex ey c1x c1y c2x c2y -> [3, ex, ey, c1x, c1y, c2x, c2y]  BEZIER (endpoint first)
//   z           -> skip (ignored by VexFlow outline walker)
function parseOutline(oStr) {
  const tokens = oStr.trim().split(/\s+/);
  const result = [];
  let i = 0;

  while (i < tokens.length) {
    const cmd = tokens[i++];
    switch (cmd) {
      case 'm': {
        const x = parseInt(tokens[i++], 10);
        const y = parseInt(tokens[i++], 10);
        result.push(0, x, y);
        break;
      }
      case 'l': {
        const x = parseInt(tokens[i++], 10);
        const y = parseInt(tokens[i++], 10);
        result.push(1, x, y);
        break;
      }
      case 'q': {
        // q ex ey cx cy — endpoint first, then control point
        const ex = parseInt(tokens[i++], 10);
        const ey = parseInt(tokens[i++], 10);
        const cx = parseInt(tokens[i++], 10);
        const cy = parseInt(tokens[i++], 10);
        result.push(2, ex, ey, cx, cy);
        break;
      }
      case 'b': {
        // b ex ey c1x c1y c2x c2y — endpoint first, then two control points
        const ex = parseInt(tokens[i++], 10);
        const ey = parseInt(tokens[i++], 10);
        const c1x = parseInt(tokens[i++], 10);
        const c1y = parseInt(tokens[i++], 10);
        const c2x = parseInt(tokens[i++], 10);
        const c2y = parseInt(tokens[i++], 10);
        result.push(3, ex, ey, c1x, c1y, c2x, c2y);
        break;
      }
      case 'z':
        // Skip — VexFlow ignores z in outline parsing; path is filled, not stroked closed
        break;
      default:
        // Unknown token — skip
        break;
    }
  }

  return result;
}

// Format an int[] as a C# int[] literal
function formatIntArray(arr) {
  if (arr.length === 0) return 'new int[] { }';
  return `new int[] { ${arr.join(', ')} }`;
}

// Format an optional int? value
function formatNullableInt(val) {
  if (val === null || val === undefined) return 'null';
  return val.toString();
}

function generateCSharp(glyphs, generatedOn) {
  const lines = [];

  lines.push('// AUTO-GENERATED from vexflow/src/fonts/bravura_glyphs.ts — do not edit manually.');
  lines.push('// Regenerate with: node tools/gen-bravura-glyphs.mjs');
  lines.push('');
  lines.push('using System.Collections.Generic;');
  lines.push('');
  lines.push('namespace VexFlowSharp');
  lines.push('{');
  lines.push('    public static class BravuraGlyphs');
  lines.push('    {');
  lines.push('        public static readonly FontData Data = new FontData');
  lines.push('        {');
  lines.push('            Resolution = 1000,');
  lines.push('            FontFamily = "Bravura",');
  lines.push(`            GeneratedOn = "${generatedOn}",`);
  lines.push('            Glyphs = new Dictionary<string, FontGlyph>');
  lines.push('            {');

  for (const glyph of glyphs) {
    const yMinStr = glyph.yMin !== null ? glyph.yMin.toString() : 'null';
    const yMaxStr = glyph.yMax !== null ? glyph.yMax.toString() : 'null';
    const outlineStr = formatIntArray(glyph.outline);

    lines.push(`                ["${glyph.name}"] = new FontGlyph { XMin = ${glyph.xMin}, XMax = ${glyph.xMax}, YMin = ${yMinStr}, YMax = ${yMaxStr}, Ha = ${glyph.ha}, CachedOutline = ${outlineStr} },`);
  }

  lines.push('            }');
  lines.push('        };');
  lines.push('    }');
  lines.push('}');
  lines.push('');

  return lines.join('\n');
}

// Extract generatedOn from source
function extractGeneratedOn(source) {
  const m = source.match(/generatedOn:\s*'([^']+)'/);
  return m ? m[1] : new Date().toISOString();
}

// Main
console.log(`Reading: ${inputPath}`);
const glyphs = parseGlyphs(source);
console.log(`Parsed ${glyphs.length} glyphs`);

if (glyphs.length < 400) {
  throw new Error(`Expected at least 400 glyphs, got ${glyphs.length}. Check parsing logic.`);
}

const generatedOn = extractGeneratedOn(source);
const csSource = generateCSharp(glyphs, generatedOn);

writeFileSync(outputPath, csSource, 'utf8');
console.log(`Written: ${outputPath}`);
console.log(`Done. ${glyphs.length} glyphs generated.`);

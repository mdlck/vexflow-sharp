#!/usr/bin/env node
// tools/gen-bravura-glyphs.mjs
// Node.js ESM code generator: reads vexflow/src/fonts/bravura_glyphs.ts
// and emits VexFlowSharp.Common/Fonts/Generated/BravuraGlyphs.cs.
//
// VexFlow 5 no longer ships the old outline table, so when that source is
// missing this augments the existing generated C# table with locally vendored
// Bravura SVG outlines for glyphs that the V5 migration needs.
// Regenerate with: node tools/gen-bravura-glyphs.mjs

import { existsSync, readFileSync, writeFileSync } from 'fs';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');

const inputPath = resolve(repoRoot, 'vexflow/src/fonts/bravura_glyphs.ts');
const outputPath = resolve(repoRoot, 'VexFlowSharp.Common/Fonts/Generated/BravuraGlyphs.cs');
const svgInputPath = resolve(repoRoot, 'fonts/bravura-redist/svg/Bravura.svg');
const glyphNamesPath = resolve(repoRoot, 'vexflow/tools/fonts/config/glyphnames.json');

const augmentationGlyphs = [
  '6stringTabClef',
  'metAugmentationDot',
  'metNote1024thDown',
  'metNote1024thUp',
  'metNote128thDown',
  'metNote128thUp',
  'metNote16thDown',
  'metNote16thUp',
  'metNote256thDown',
  'metNote256thUp',
  'metNote32ndDown',
  'metNote32ndUp',
  'metNote512thDown',
  'metNote512thUp',
  'metNote64thDown',
  'metNote64thUp',
  'metNote8thDown',
  'metNote8thUp',
  'metNoteDoubleWhole',
  'metNoteDoubleWholeSquare',
  'metNoteHalfDown',
  'metNoteHalfUp',
  'metNoteQuarterDown',
  'metNoteQuarterUp',
  'metNoteWhole',
  'restHBarLeft',
  'restHBarMiddle',
  'restHBarRight',
  'restLonga',
];

// Read the TypeScript source as text
const source = existsSync(inputPath) ? readFileSync(inputPath, 'utf8') : null;

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

function parseSvgGlyphAttributes(tag) {
  const attrs = {};
  const attrRegex = /([\w:-]+)="([^"]*)"/g;
  let match;
  while ((match = attrRegex.exec(tag)) !== null) {
    attrs[match[1]] = match[2];
  }
  return attrs;
}

function parseCodepoint(codepoint) {
  return codepoint.replace(/^U\+/i, '').toLowerCase();
}

function svgGlyphsByCodepoint(svgSource) {
  const glyphs = new Map();
  const glyphRegex = /<glyph\b[\s\S]*?\/>/g;
  let match;
  while ((match = glyphRegex.exec(svgSource)) !== null) {
    const attrs = parseSvgGlyphAttributes(match[0]);
    const unicode = attrs.unicode?.match(/^&#x([0-9a-f]+);$/i);
    if (!unicode || !attrs.d) continue;

    glyphs.set(unicode[1].toLowerCase(), {
      horizAdvX: parseInt(attrs['horiz-adv-x'] ?? '0', 10),
      d: attrs.d,
    });
  }

  return glyphs;
}

function tokenizeSvgPath(path) {
  return path.match(/[a-zA-Z]|[-+]?(?:\d*\.\d+|\d+)(?:e[-+]?\d+)?/g) ?? [];
}

function isCommand(token) {
  return /^[a-zA-Z]$/.test(token);
}

function roundCoord(value) {
  return Math.round(Number(value));
}

function parseSvgPath(path) {
  const tokens = tokenizeSvgPath(path);
  const result = [];
  let i = 0;
  let cmd = null;
  let x = 0;
  let y = 0;
  let startX = 0;
  let startY = 0;
  let prevCubicControl = null;

  const read = () => roundCoord(tokens[i++]);
  const hasNumber = () => i < tokens.length && !isCommand(tokens[i]);
  const point = (relative) => {
    const px = read();
    const py = read();
    return relative ? [x + px, y + py] : [px, py];
  };

  while (i < tokens.length) {
    if (isCommand(tokens[i])) cmd = tokens[i++];
    if (!cmd) throw new Error(`SVG path data starts without command: ${path}`);

    const relative = cmd === cmd.toLowerCase();
    switch (cmd.toLowerCase()) {
      case 'm': {
        const [nx, ny] = point(relative);
        result.push(0, nx, ny);
        x = startX = nx;
        y = startY = ny;
        cmd = relative ? 'l' : 'L';
        while (hasNumber()) {
          const [lx, ly] = point(relative);
          result.push(1, lx, ly);
          x = lx;
          y = ly;
        }
        break;
      }
      case 'l':
        while (hasNumber()) {
          const [lx, ly] = point(relative);
          result.push(1, lx, ly);
          x = lx;
          y = ly;
          prevCubicControl = null;
        }
        break;
      case 'h':
        while (hasNumber()) {
          const nx = relative ? x + read() : read();
          result.push(1, nx, y);
          x = nx;
          prevCubicControl = null;
        }
        break;
      case 'v':
        while (hasNumber()) {
          const ny = relative ? y + read() : read();
          result.push(1, x, ny);
          y = ny;
          prevCubicControl = null;
        }
        break;
      case 'q':
        while (hasNumber()) {
          const [cx, cy] = point(relative);
          const [ex, ey] = point(relative);
          result.push(2, ex, ey, cx, cy);
          x = ex;
          y = ey;
          prevCubicControl = null;
        }
        break;
      case 'c':
        while (hasNumber()) {
          const [c1x, c1y] = point(relative);
          const [c2x, c2y] = point(relative);
          const [ex, ey] = point(relative);
          result.push(3, ex, ey, c1x, c1y, c2x, c2y);
          x = ex;
          y = ey;
          prevCubicControl = [c2x, c2y];
        }
        break;
      case 's':
        while (hasNumber()) {
          const [c1x, c1y] = prevCubicControl ? [2 * x - prevCubicControl[0], 2 * y - prevCubicControl[1]] : [x, y];
          const [c2x, c2y] = point(relative);
          const [ex, ey] = point(relative);
          result.push(3, ex, ey, c1x, c1y, c2x, c2y);
          x = ex;
          y = ey;
          prevCubicControl = [c2x, c2y];
        }
        break;
      case 'z':
        x = startX;
        y = startY;
        prevCubicControl = null;
        break;
      default:
        throw new Error(`Unsupported SVG path command '${cmd}' in: ${path}`);
    }
  }

  return result;
}

function bboxFromOutline(outline) {
  let minX = Infinity;
  let maxX = -Infinity;
  let minY = Infinity;
  let maxY = -Infinity;

  for (let i = 0; i < outline.length;) {
    const cmd = outline[i++];
    const pointCount = cmd === 0 || cmd === 1 ? 1 : cmd === 2 ? 2 : cmd === 3 ? 3 : 0;
    for (let p = 0; p < pointCount; p++) {
      const x = outline[i++];
      const y = outline[i++];
      minX = Math.min(minX, x);
      maxX = Math.max(maxX, x);
      minY = Math.min(minY, y);
      maxY = Math.max(maxY, y);
    }
  }

  return {
    xMin: Number.isFinite(minX) ? minX : 0,
    xMax: Number.isFinite(maxX) ? maxX : 0,
    yMin: Number.isFinite(minY) ? minY : 0,
    yMax: Number.isFinite(maxY) ? maxY : 0,
  };
}

function svgGlyphToCSharp(name, svgGlyph) {
  const outline = parseSvgPath(svgGlyph.d);
  const bbox = bboxFromOutline(outline);
  const outlineStr = formatIntArray(outline);

  return `                ["${name}"] = new FontGlyph { XMin = ${bbox.xMin}, XMax = ${bbox.xMax}, YMin = ${bbox.yMin}, YMax = ${bbox.yMax}, Ha = ${svgGlyph.horizAdvX}, CachedOutline = ${outlineStr} },`;
}

function augmentGeneratedCSharp() {
  console.log(`Reading existing generated file: ${outputPath}`);
  let csSource = readFileSync(outputPath, 'utf8');
  const glyphNames = JSON.parse(readFileSync(glyphNamesPath, 'utf8'));
  const svgGlyphs = svgGlyphsByCodepoint(readFileSync(svgInputPath, 'utf8'));

  const lines = [];
  for (const name of augmentationGlyphs) {
    if (csSource.includes(`["${name}"] =`)) continue;

    const codepoint = augmentationCodepoints[name] ?? glyphNames[name]?.codepoint;
    if (!codepoint) throw new Error(`No codepoint found for ${name}`);

    const svgGlyph = svgGlyphs.get(parseCodepoint(codepoint));
    if (!svgGlyph) throw new Error(`No SVG outline found for ${name} (${codepoint})`);

    lines.push(svgGlyphToCSharp(name, svgGlyph));
  }

  if (lines.length === 0) {
    console.log('No missing augmentation glyphs.');
    return;
  }

  const insertion = `${lines.join('\n')}\n`;
  const marker = '            }\n        };';
  if (!csSource.includes(marker)) throw new Error('Could not find BravuraGlyphs dictionary close marker.');

  csSource = csSource.replace(marker, `${insertion}${marker}`);
  writeFileSync(outputPath, csSource, 'utf8');
  console.log(`Added ${lines.length} Bravura glyphs from SVG font data.`);
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
if (source) {
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
} else {
  console.log(`Legacy outline source not found: ${inputPath}`);
  augmentGeneratedCSharp();
}

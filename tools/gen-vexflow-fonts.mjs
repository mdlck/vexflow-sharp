#!/usr/bin/env node
// Generate C# font data from the vendored @vexflow-fonts packages.
//
// This intentionally keeps Bravura's existing generated file as-is because it
// includes a small augmentation path for legacy migration coverage.

import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'fs';
import { dirname, resolve } from 'path';
import { fileURLToPath } from 'url';
import { createRequire } from 'module';

const require = createRequire(import.meta.url);
const opentype = require('../vexflow/node_modules/opentype.js');

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');
const fontsRoot = resolve(repoRoot, 'vexflow/node_modules/@vexflow-fonts');
const outputDir = resolve(repoRoot, 'VexFlowSharp.Common/Fonts/Generated');
const glyphNamesPath = resolve(repoRoot, 'vexflow/tools/fonts/config/glyphnames.json');

const generatedOn = '2026-04-28T00:00:00.000Z';

const supportedFonts = [
  { family: 'Academico', path: 'academico/academico.otf', kind: 'text' },
  { family: 'Bravura Text', path: 'bravuratext/bravuratext.otf', kind: 'text' },
  { family: 'Edwin', path: 'edwin/edwin-roman.otf', kind: 'text' },
  { family: 'Finale Ash', path: 'finaleash/finaleash.otf', kind: 'music' },
  { family: 'Finale Ash Text', path: 'finaleashtext/finaleashtext.otf', kind: 'text' },
  { family: 'Finale Broadway', path: 'finalebroadway/finalebroadway.otf', kind: 'music' },
  { family: 'Finale Broadway Text', path: 'finalebroadwaytext/finalebroadwaytext.otf', kind: 'text' },
  { family: 'Finale Jazz', path: 'finalejazz/finalejazz.otf', kind: 'music' },
  { family: 'Finale Jazz Text', path: 'finalejazztext/finalejazztext.otf', kind: 'text' },
  { family: 'Finale Maestro', path: 'finalemaestro/finalemaestro.otf', kind: 'music' },
  { family: 'Finale Maestro Text', path: 'finalemaestrotext/finalemaestrotext-regular.otf', kind: 'text' },
  { family: 'Gonville', path: 'gonville/gonville.otf', kind: 'music' },
  { family: 'Gootville', path: 'gootville/gootville.otf', kind: 'music' },
  { family: 'Gootville Text', path: 'gootvilletext/gootvilletext.otf', kind: 'text' },
  { family: 'Leipzig', path: 'leipzig/leipzig.otf', kind: 'music' },
  { family: 'Leland', path: 'leland/leland.otf', kind: 'music' },
  { family: 'Leland Text', path: 'lelandtext/lelandtext.otf', kind: 'text' },
  { family: 'MuseJazz', path: 'musejazz/musejazz.otf', kind: 'music' },
  { family: 'MuseJazz Text', path: 'musejazztext/musejazztext.otf', kind: 'text' },
  { family: 'Nepomuk', path: 'nepomuk/nepomuk-regular.otf', kind: 'text' },
  { family: 'Petaluma', path: 'petaluma/petaluma.otf', kind: 'music' },
  { family: 'Petaluma Script', path: 'petalumascript/petalumascript.otf', kind: 'text' },
  { family: 'Petaluma Text', path: 'petalumatext/petalumatext.otf', kind: 'text' },
  { family: 'Roboto Slab', path: 'robotoslab/robotoslab-regular-400.otf', kind: 'text' },
  { family: 'Sebastian', path: 'sebastian/sebastian.otf', kind: 'music' },
  { family: 'Sebastian Text', path: 'sebastiantext/sebastiantext.otf', kind: 'text' },
];

function className(family) {
  return family.replace(/[^A-Za-z0-9]+/g, '') + 'Glyphs';
}

function csString(value) {
  return value.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
}

function formatInt(value) {
  return Math.round(value).toString();
}

function formatNullableInt(value) {
  return Number.isFinite(value) ? formatInt(value) : 'null';
}

function formatIntArray(arr) {
  return arr.length === 0 ? 'System.Array.Empty<int>()' : `new int[] { ${arr.join(', ')} }`;
}

function outlineFromPath(path) {
  const outline = [];
  for (const command of path.commands) {
    switch (command.type) {
      case 'M':
        outline.push(0, Math.round(command.x), Math.round(command.y));
        break;
      case 'L':
        outline.push(1, Math.round(command.x), Math.round(command.y));
        break;
      case 'Q':
        outline.push(2, Math.round(command.x), Math.round(command.y), Math.round(command.x1), Math.round(command.y1));
        break;
      case 'C':
        outline.push(
          3,
          Math.round(command.x),
          Math.round(command.y),
          Math.round(command.x1),
          Math.round(command.y1),
          Math.round(command.x2),
          Math.round(command.y2)
        );
        break;
      case 'Z':
        break;
      default:
        throw new Error(`Unsupported OpenType path command: ${command.type}`);
    }
  }
  return outline;
}

function bboxFromOutline(outline) {
  let minX = Infinity;
  let maxX = -Infinity;
  let minY = Infinity;
  let maxY = -Infinity;

  for (let i = 0; i < outline.length;) {
    const command = outline[i++];
    const pointCount = command === 0 || command === 1 ? 1 : command === 2 ? 2 : command === 3 ? 3 : 0;
    for (let p = 0; p < pointCount; p++) {
      const x = outline[i++];
      const y = outline[i++];
      minX = Math.min(minX, x);
      maxX = Math.max(maxX, x);
      minY = Math.min(minY, y);
      maxY = Math.max(maxY, y);
    }
  }

  return { minX, maxX, minY, maxY };
}

function parseCodepoint(value) {
  return parseInt(value.replace(/^U\+/i, ''), 16);
}

function isMissingGlyph(glyph) {
  return !glyph || glyph.index === 0 || !glyph.path || glyph.path.commands.length === 0;
}

function generateMusicFont(fontConfig, glyphNames) {
  const fontPath = resolve(fontsRoot, fontConfig.path);
  if (!existsSync(fontPath)) throw new Error(`Missing font file: ${fontPath}`);

  const font = opentype.loadSync(fontPath);
  const lines = [];

  for (const [name, info] of Object.entries(glyphNames)) {
    const codepoint = parseCodepoint(info.codepoint);
    const glyph = font.charToGlyph(String.fromCodePoint(codepoint));
    if (isMissingGlyph(glyph)) continue;

    const outline = outlineFromPath(glyph.path);
    if (outline.length === 0) continue;

    const bbox = bboxFromOutline(outline);
    const height = Number.isFinite(bbox.maxY) && Number.isFinite(bbox.minY)
      ? bbox.maxY - bbox.minY
      : glyph.advanceWidth ?? 0;
    lines.push(
      `                ["${csString(name)}"] = new FontGlyph { XMin = ${formatNullableInt(bbox.minX)}, XMax = ${formatNullableInt(bbox.maxX)}, YMin = ${formatNullableInt(bbox.minY)}, YMax = ${formatNullableInt(bbox.maxY)}, Ha = ${formatInt(height)}, CachedOutline = ${formatIntArray(outline)} },`
    );
  }

  if (lines.length === 0) throw new Error(`No SMuFL glyphs found for ${fontConfig.family}`);

  return renderFontClass(fontConfig, font.unitsPerEm, lines, 1.0);
}

function generateTextFont(fontConfig) {
  const fontPath = resolve(fontsRoot, fontConfig.path);
  if (!existsSync(fontPath)) throw new Error(`Missing font file: ${fontPath}`);

  const font = opentype.loadSync(fontPath);
  const lines = [];
  for (let codepoint = 32; codepoint <= 126; codepoint++) {
    const character = String.fromCodePoint(codepoint);
    const glyph = font.charToGlyph(character);
    if (!glyph || glyph.index === 0) continue;
    lines.push(`                ["${csString(character)}"] = new FontGlyph { XMin = 0, XMax = ${formatInt(glyph.advanceWidth ?? 0)}, Ha = ${formatInt(glyph.advanceWidth ?? 0)} },`);
  }

  return renderFontClass(fontConfig, font.unitsPerEm, lines, 0.72);
}

function renderFontClass(fontConfig, resolution, glyphLines, glyphScale) {
  return `// AUTO-GENERATED from ${fontConfig.path} - do not edit manually.
// Regenerate with: node tools/gen-vexflow-fonts.mjs

using System.Collections.Generic;

namespace VexFlowSharp
{
    public static class ${className(fontConfig.family)}
    {
        public static readonly FontData Data = new FontData
        {
            Resolution = ${resolution},
            GlyphScale = ${glyphScale.toFixed(2)},
            FontFamily = "${csString(fontConfig.family)}",
            GeneratedOn = "${generatedOn}",
            Glyphs = new Dictionary<string, FontGlyph>
            {
${glyphLines.join('\n')}
            }
        };
    }
}
`;
}

function renderRegistry() {
  const entries = supportedFonts.map((font) =>
    `                ["${csString(font.family)}"] = new BuiltInFont("${csString(font.family)}", "${csString(font.path.replace(/\\/g, '/'))}", ${className(font.family)}.Data, ${font.kind === 'music' ? 'true' : 'false'}),`
  );

  return `// AUTO-GENERATED from tools/gen-vexflow-fonts.mjs - do not edit manually.
// Regenerate with: node tools/gen-vexflow-fonts.mjs

using System;
using System.Collections.Generic;

namespace VexFlowSharp
{
    public sealed class BuiltInFont
    {
        public BuiltInFont(string family, string path, FontData data, bool isMusicFont)
        {
            Family = family;
            Path = path;
            Data = data;
            IsMusicFont = isMusicFont;
        }

        public string Family { get; }
        public string Path { get; }
        public FontData Data { get; }
        public bool IsMusicFont { get; }
    }

    public static class BuiltInFonts
    {
        public static readonly IReadOnlyDictionary<string, BuiltInFont> All =
            new Dictionary<string, BuiltInFont>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bravura"] = new BuiltInFont("Bravura", "bravura/bravura.otf", BravuraGlyphs.Data, true),
${entries.join('\n')}
            };
    }
}
`;
}

mkdirSync(outputDir, { recursive: true });
const glyphNames = JSON.parse(readFileSync(glyphNamesPath, 'utf8'));

for (const fontConfig of supportedFonts) {
  const source = fontConfig.kind === 'music' ? generateMusicFont(fontConfig, glyphNames) : generateTextFont(fontConfig);
  const outPath = resolve(outputDir, `${className(fontConfig.family)}.cs`);
  writeFileSync(outPath, source, 'utf8');
  console.log(`Generated ${outPath}`);
}

writeFileSync(resolve(outputDir, 'BuiltInFonts.cs'), renderRegistry(), 'utf8');
console.log('Generated built-in font registry.');

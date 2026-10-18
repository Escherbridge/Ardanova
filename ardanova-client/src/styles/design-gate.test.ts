import fs from "node:fs";
import path from "node:path";

import { describe, expect, it } from "vitest";

/** Design-system gate: token classes must resolve, and raw-palette / raw-error debt may only shrink. See `src/styles/AGENTS.md` §design-gate. */

const SRC = path.resolve(__dirname, "..");
const GLOBALS_CSS = path.join(SRC, "styles", "globals.css");

/** Known misses owned by other lanes; each is a deferred item. Remove the entry when that lane fixes it. */
const KNOWN_UNDEFINED_TOKENS = new Set([
  "app/tasks/create/page.tsx:system-purple",
]);

/** Ceilings measured at loop2/design. Lower them when debt is paid; never raise them. */
const RAW_PALETTE_CEILING = 156;
const RAW_ERROR_MESSAGE_CEILING = 52;
const RAW_ERROR_NOTIFIER_CEILING = 1;

const TOKEN_FAMILIES =
  "neon|system|primary|brand|success|warning|destructive|muted|chart|sidebar|card|popover|secondary";
const COLOR_PREFIXES =
  "text|bg|border(?:-[trblxy])?|ring(?:-offset)?|fill|stroke|from|via|to|outline|decoration|divide|placeholder|caret";
const PALETTE =
  "slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose";

const tokenClass = new RegExp(
  `(?<![\\w-])(?:[\\w-]+:)*(?:${COLOR_PREFIXES})-((?:${TOKEN_FAMILIES})(?:-[a-z0-9]+)*)(?:/\\d+)?(?![\\w-])`,
  "g",
);
const rawPaletteClass = new RegExp(
  `(?<![\\w-])(?:[\\w-]+:)*(?:${COLOR_PREFIXES})-(?:${PALETTE})-\\d{2,3}(?![\\w-])`,
  "g",
);
/** A JSX expression that renders a raw error message, e.g. `{mutation.error.message}`. */
const rawErrorMessageExpression = /\{[^{}]*\berror\??\.message\b[^{}]*\}/g;
/** A toast/alert call (args may span lines, two paren levels deep) that shows any `x.message`. */
const notifierCall =
  /\b(?:toast(?:\.\w+)?|alert)\(((?:[^()]|\((?:[^()]|\([^()]*\))*\))*)\)/g;
const rawMessageArgument = /\b\w+\??\.message\b/;

function sourceFiles(directory: string): string[] {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) return sourceFiles(full);
    return /\.tsx?$/.test(entry.name) &&
      !/\.(test|spec)\.tsx?$/.test(entry.name)
      ? [full]
      : [];
  });
}

const files = sourceFiles(SRC).map((file) => ({
  relative: path.relative(SRC, file).split(path.sep).join("/"),
  text: fs.readFileSync(file, "utf8"),
}));

const themeColors = new Set(
  [
    ...fs
      .readFileSync(GLOBALS_CSS, "utf8")
      .matchAll(/--color-([a-z0-9-]+)\s*:/g),
  ].map((match) => match[1]),
);

describe("design tokens", () => {
  it("every semantic color class resolves to a --color-* token in globals.css", () => {
    const undefinedTokens = files.flatMap(({ relative, text }) =>
      [...text.matchAll(tokenClass)]
        .map((match) => match[1])
        .filter((name) => !themeColors.has(name))
        .map((name) => `${relative}:${name}`)
        .filter((key) => !KNOWN_UNDEFINED_TOKENS.has(key)),
    );
    expect([...new Set(undefinedTokens)]).toEqual([]);
  });

  it("raw Tailwind palette classes do not grow past the ceiling", () => {
    const count = files.reduce(
      (total, { text }) => total + (text.match(rawPaletteClass)?.length ?? 0),
      0,
    );
    expect(count).toBeLessThanOrEqual(RAW_PALETTE_CEILING);
  });

  it("JSX that renders a raw error.message does not grow past the ceiling", () => {
    const count = files
      .filter(({ relative }) => relative.endsWith(".tsx"))
      .reduce(
        (total, { text }) =>
          total + (text.match(rawErrorMessageExpression)?.length ?? 0),
        0,
      );
    expect(count).toBeLessThanOrEqual(RAW_ERROR_MESSAGE_CEILING);
  });

  it("toast/alert calls that show a raw error message do not grow past the ceiling", () => {
    const leaks = files.flatMap(({ relative, text }) =>
      [...text.matchAll(notifierCall)]
        .filter((match) => rawMessageArgument.test(match[1] ?? ""))
        .map(
          (match) =>
            `${relative}:${text.slice(0, match.index).split("\n").length}`,
        ),
    );
    expect(leaks.length, leaks.join("\n")).toBeLessThanOrEqual(
      RAW_ERROR_NOTIFIER_CEILING,
    );
  });
});

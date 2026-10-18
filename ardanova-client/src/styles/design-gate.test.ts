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

/** Ceilings re-measured at loop3/design. Lower them when debt is paid; never raise them. */
const RAW_PALETTE_CEILING = 144;
const RAW_ERROR_MESSAGE_CEILING = 52;
const RAW_ERROR_NOTIFIER_CEILING = 1;
const WHITE_ALPHA_BORDER_CEILING = 30;
const UNTITLED_PAGE_CEILING = 48;

/** WCAG 2.x AA for normal-size text. */
const AA_TEXT = 4.5;
const THEME_BLOCKS = [":root", ".dark", ".public-shell"] as const;
/** Accents used as `text-X` on page/card/muted surfaces and in the `bg-X/20 text-X` tinted-badge recipe. */
const TEXT_ACCENTS = [
  "primary",
  "destructive",
  "system",
  "success",
  "warning",
  "neon",
  "neon-cyan",
  "neon-pink",
  "neon-green",
  "neon-yellow",
  "neon-purple",
] as const;
/** Solid-fill pairs (`bg-X text-X-foreground`) used by Badge and Button. */
const FILL_PAIRS = [
  "primary",
  "destructive",
  "system",
  "success",
  "warning",
] as const;

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
/** Dark-theme leftovers such as `border-white/10`: invisible on the light paper background. */
const whiteAlphaBorder =
  /(?<![\w-])(?:[\w-]+:)*(?:border(?:-[trblxy])?|divide(?:-[xy])?)-white\/\d+(?![\w-])/g;
/** A page or layout that sets a document title (static `metadata.title` or `generateMetadata`). */
const declaresTitle =
  /export\s+(?:async\s+)?function\s+generateMetadata\b|export\s+const\s+metadata\b[^;]*?\btitle\s*:/s;

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

const globalsCss = fs.readFileSync(GLOBALS_CSS, "utf8");

/** `--name: value` declarations between `start` and the next column-0 `}`. */
function declarationsFrom(start: number): Map<string, string> {
  if (start < 0) throw new Error("globals.css block not found");
  const body = globalsCss.slice(start, globalsCss.indexOf("\n}", start));
  return new Map(
    [...body.matchAll(/(--[a-z0-9-]+)\s*:\s*([^;]+);/g)].map((match) => [
      match[1],
      match[2].trim(),
    ]),
  );
}

/** Custom properties of one top-level theme block (`:root`, `.dark`, `.public-shell`). */
function cssBlock(selector: string): Map<string, string> {
  const escaped = selector.replace(/[.]/g, "\\.");
  return declarationsFrom(
    globalsCss.search(new RegExp(`^${escaped}\\s*\\{`, "m")),
  );
}

const themeInline = declarationsFrom(globalsCss.indexOf("@theme inline"));
const rootTheme = cssBlock(":root");

/** Follows `var()` chains to a hex colour; theme blocks fall back to `:root` like the cascade does. */
function resolveColor(value: string, theme: Map<string, string>): string {
  let current = value;
  for (let depth = 0; depth < 10; depth++) {
    const reference = /^var\((--[a-z0-9-]+)\)$/.exec(current);
    if (!reference) break;
    const name = reference[1];
    current =
      theme.get(name) ?? rootTheme.get(name) ?? themeInline.get(name) ?? "";
  }
  if (!/^#[0-9a-f]{6}$/i.test(current)) {
    throw new Error(
      `${value} did not resolve to a hex colour (got "${current}")`,
    );
  }
  return current;
}

/** WCAG 2.x relative luminance. */
function luminance(hex: string): number {
  const [red, green, blue] = [1, 3, 5].map((i) => {
    const channel = parseInt(hex.slice(i, i + 2), 16) / 255;
    return channel <= 0.03928
      ? channel / 12.92
      : ((channel + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
}

function contrast(first: string, second: string): number {
  const [lighter, darker] = [luminance(first), luminance(second)].sort(
    (a, b) => b - a,
  );
  return (lighter + 0.05) / (darker + 0.05);
}

/** `fill` at `alpha` composited over `base`, as `bg-X/20` renders. */
function tint(fill: string, base: string, alpha: number): string {
  const channels = [1, 3, 5].map((i) => {
    const fillChannel = parseInt(fill.slice(i, i + 2), 16);
    const baseChannel = parseInt(base.slice(i, i + 2), 16);
    return Math.round(fillChannel * alpha + baseChannel * (1 - alpha))
      .toString(16)
      .padStart(2, "0");
  });
  return `#${channels.join("")}`;
}

/** Colour Tailwind v4 emits for `text-X`: `--text-color-X` wins over `--color-X`. */
function textToken(name: string): string {
  return (
    themeInline.get(`--text-color-${name}`) ??
    themeInline.get(`--color-${name}`) ??
    `var(--${name})`
  );
}

const themeColors = new Set(
  [...globalsCss.matchAll(/--color-([a-z0-9-]+)\s*:/g)].map(
    (match) => match[1],
  ),
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

  it("dark-theme white-alpha borders do not grow past the ceiling", () => {
    const count = files.reduce(
      (total, { text }) => total + (text.match(whiteAlphaBorder)?.length ?? 0),
      0,
    );
    expect(count).toBeLessThanOrEqual(WHITE_ALPHA_BORDER_CEILING);
  });

  it("pages without a document title do not grow past the ceiling", () => {
    const app = path.join(SRC, "app");
    const hasTitle = (file: string) =>
      fs.existsSync(file) && declaresTitle.test(fs.readFileSync(file, "utf8"));
    const untitled = files
      .filter(({ relative }) => /^app\/.+\/page\.tsx$/.test(relative))
      .filter(({ relative }) => {
        const page = path.join(SRC, relative);
        if (hasTitle(page)) return false;
        for (
          let dir = path.dirname(page);
          dir !== app;
          dir = path.dirname(dir)
        ) {
          if (hasTitle(path.join(dir, "layout.tsx"))) return false;
        }
        return true;
      })
      .map(({ relative }) => relative);
    expect(untitled.length, untitled.join("\n")).toBeLessThanOrEqual(
      UNTITLED_PAGE_CEILING,
    );
  });
});

describe("token contrast (WCAG AA)", () => {
  /** Every text/surface pair that fails 4.5:1 in one theme block. */
  function contrastFailures(selector: (typeof THEME_BLOCKS)[number]): string[] {
    const theme = cssBlock(selector);
    const color = (value: string) => resolveColor(value, theme);
    const surfaces = ["background", "card", "muted"].map((name) =>
      color(`var(--${name})`),
    );
    const failures: string[] = [];
    const check = (label: string, foreground: string, background: string) => {
      const ratio = contrast(foreground, background);
      if (ratio < AA_TEXT) failures.push(`${label}: ${ratio.toFixed(2)}`);
    };

    for (const text of ["foreground", "muted-foreground"]) {
      surfaces.forEach((surface, i) =>
        check(`${text} on surface ${i}`, color(`var(--${text})`), surface),
      );
    }
    for (const accent of TEXT_ACCENTS) {
      const text = color(textToken(accent));
      const fill = color(themeInline.get(`--color-${accent}`)!);
      surfaces.forEach((surface, i) =>
        check(`text-${accent} on surface ${i}`, text, surface),
      );
      for (const alpha of [0.1, 0.2]) {
        surfaces.forEach((base, i) =>
          check(
            `text-${accent} on bg-${accent}/${alpha * 100} over surface ${i}`,
            text,
            tint(fill, base, alpha),
          ),
        );
      }
    }
    for (const accent of FILL_PAIRS) {
      check(
        `${accent}-foreground on bg-${accent}`,
        color(`var(--${accent}-foreground)`),
        color(`var(--${accent})`),
      );
    }
    return failures;
  }

  it.each(THEME_BLOCKS)(
    "%s text tokens meet 4.5:1 on every surface and tinted badge",
    (selector) => {
      expect(contrastFailures(selector)).toEqual([]);
    },
  );
});

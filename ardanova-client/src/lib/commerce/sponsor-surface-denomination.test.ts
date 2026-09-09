/**
 * The sponsor's spend surface is denominated in DOLLARS. This is the mechanical half of that
 * claim, because a commit message saying the inversion happened is not a control.
 *
 * What it caught, and what it exists to stop coming back: the founder's ask was inverted to a
 * dollar field first, and the sponsor's half was left ARDA-denominated — "Amount to spend, in
 * ARDA", quick chips reading "500 ARDA", a font-mono ARDA affix chip of exactly the shape the
 * founder's field had just lost, and "Enter a positive ARDA amount with no more than two decimal
 * places". The two halves of the same money story disagreed on the primary spend surface.
 *
 * The rule: on these files, the ticker may appear only where a person is being told what their
 * dollars SETTLE as — never in a label, a placeholder, a quick-amount chip, an affix, or a
 * refusal. The permitted captions are pinned individually, so a new one has to be added here
 * deliberately rather than arriving with a reword.
 */

import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";

/** The files a person passes through to sponsor a project, in order. */
const SPONSOR_SURFACE = [
  "src/app/projects/[slug]/invest/page.tsx",
  "src/app/projects/[slug]/invest/success/page.tsx",
  "src/components/equity/equity-preview.tsx",
  "src/components/equity/conversion-chain.tsx",
] as const;

/**
 * Ticker mentions in RENDERED copy that are permitted, each because it tells a person what
 * their dollars settle as. A comment is not rendered copy and is filtered out before this.
 */
const PERMITTED_SETTLEMENT_CAPTIONS = [
  "Settles as {formatPlatformUnits(ardaAmount)} ARDA from your balance.",
  '`${new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 }).format(value)} ARDA`',
] as const;

/** Copy shapes that ASK a person to think in the ticker. Any of these is the defect itself. */
const ASK_SHAPES = [
  /in ARDA/i,
  /ARDA amount/i,
  /amount to spend, in ARDA/i,
  /\{amount\.toLocaleString\(\)\} ARDA/,
  /Quick ARDA amounts/i,
];

function readSurface(file: string): string {
  return readFileSync(new URL(`../../../${file}`, import.meta.url), "utf8");
}

/** Lines that are not `//` comments and not inside a `/** ... *\/` block. */
function renderedLines(source: string): string[] {
  const kept: string[] = [];
  let inBlockComment = false;

  for (const raw of source.split("\n")) {
    const line = raw.trim();
    if (inBlockComment) {
      if (line.includes("*/")) inBlockComment = false;
      continue;
    }
    if (line.startsWith("/*")) {
      if (!line.includes("*/")) inBlockComment = true;
      continue;
    }
    if (line.startsWith("//") || line.startsWith("*")) continue;
    kept.push(raw);
  }

  return kept;
}

describe("the sponsor spends in dollars", () => {
  it("reads every file on the surface", () => {
    // Without this, every assertion below is vacuously true on a renamed route.
    for (const file of SPONSOR_SURFACE) {
      expect(readSurface(file).length, file).toBeGreaterThan(500);
    }
  });

  it("names the ticker only in a pinned settlement caption", () => {
    const unexpected: string[] = [];

    for (const file of SPONSOR_SURFACE) {
      for (const line of renderedLines(readSurface(file))) {
        if (!line.includes("ARDA")) continue;
        const permitted = PERMITTED_SETTLEMENT_CAPTIONS.some((caption) =>
          line.includes(caption),
        );
        if (!permitted) unexpected.push(`${file}: ${line.trim()}`);
      }
    }

    expect(
      unexpected,
      unexpected.length > 0
        ? `The ticker reached sponsor copy outside a settlement caption:\n\n${unexpected.join("\n")}` +
            `\n\nA sponsor names dollars; the platform unit is at most a derived caption (D52).`
        : "",
    ).toHaveLength(0);
  });

  it("asks for an amount in dollars, never in the ticker", () => {
    for (const file of SPONSOR_SURFACE) {
      const rendered = renderedLines(readSurface(file)).join("\n");
      for (const shape of ASK_SHAPES) {
        expect(shape.test(rendered), `${file} matched ${shape}`).toBe(false);
      }
    }
  });

  it("still carries a dollar affix on the amount field", () => {
    // The other side of the rule: deleting the ticker without putting a currency in its place
    // would leave a bare number, which is not a dollar field either.
    const invest = readSurface(SPONSOR_SURFACE[0]);
    expect(invest).toContain("parseSponsorDollars");
    expect(invest).toContain("formatSponsorDollars");
    expect(invest).toMatch(/\{"?\$"?\}|>\s*\$\s*</);
  });
});

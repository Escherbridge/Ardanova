import { existsSync, readFileSync } from "node:fs";
import { join } from "node:path";

import { describe, expect, it } from "vitest";

/** Layout that owns each money route's document title; the root template appends " — ArdaNova". */
const routeLayouts = [
  "portfolio/layout.tsx",
  "portfolio/add-funds/layout.tsx",
  "portfolio/withdraw/layout.tsx",
  "swap/layout.tsx",
  "projects/layout.tsx",
  "settings/payouts/layout.tsx",
  "settings/verification/layout.tsx",
];

function declaredTitle(relative: string): string | undefined {
  const path = join(__dirname, relative);
  if (!existsSync(path)) return undefined;
  const source = readFileSync(path, "utf8");
  if (/^\s*["']use client["']/.test(source)) return undefined;
  return /export const metadata: Metadata = \{ title: "([^"]+)" \}/.exec(
    source,
  )?.[1];
}

describe("money route document titles (WCAG 2.4.2)", () => {
  it("gives every money route its own title from a server layout", () => {
    const titles = routeLayouts.map(declaredTitle);

    expect(titles).not.toContain(undefined);
    expect(new Set(titles).size).toBe(routeLayouts.length);
  });
});

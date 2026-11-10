// @vitest-environment jsdom
import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { patterns } from "~/lib/writing";

import { PatternBody } from "./bodies";

describe("pattern pages", () => {
  it("keeps the simulation visible and avoids investment language", () => {
    for (const pattern of patterns) {
      const { container, unmount } = render(
        <PatternBody slug={pattern.slug} />,
      );
      expect(container.textContent).toContain("simulated");
      expect(container.textContent).toMatch(
        /do not automatically create legal ownership/i,
      );
      expect(container.textContent).not.toMatch(
        /\b(investment|token|tokens|returns?)\b/i,
      );
      unmount();
    }
  });
});

// @vitest-environment jsdom
import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { patterns } from "~/lib/writing";

import { PatternBody } from "./bodies";
import PatternsPage from "./page";

const researchedSlugs = [
  "a-record-is-not-a-right",
  "two-records-one-agreement",
  "decision-logs",
] as const;

describe("pattern pages", () => {
  it("keeps the simulation visible and avoids investment language", () => {
    for (const pattern of patterns) {
      const { container, unmount } = render(
        <PatternBody slug={pattern.slug} />,
      );
      const text = container.textContent ?? "";
      expect(text).toContain("ECONOMY: SIMULATED");
      expect(text).toContain("simulated");
      expect(text).toMatch(/do not automatically create legal ownership/i);
      expect(text).toContain("Sources");
      expect(text).not.toMatch(/\b(investment|token|tokens|returns?)\b/i);
      unmount();
    }
  });

  it("publishes an illustrative scenario on each researched child", () => {
    for (const slug of researchedSlugs) {
      const { container, unmount } = render(<PatternBody slug={slug} />);
      const text = container.textContent ?? "";
      expect(text).toContain("An illustrative scenario");
      expect(text).toContain("This is a worked example, not a customer.");
      expect(text).toContain("Every stat on this page is cited.");
      expect(container.innerHTML).toContain("https://azoa.ardanova.com");
      expect(container.innerHTML).not.toContain("azoa.ardanova.com/workflows");
      unmount();
    }
  });
});

describe("patterns index", () => {
  it("names the three research children and the simulation sentence", () => {
    const { container } = render(<PatternsPage />);
    const text = container.textContent ?? "";
    expect(text).toContain("The shape of a shared record.");
    expect(text).toContain("ECONOMY: SIMULATED");
    expect(text).toMatch(/do not automatically create legal ownership/i);
    expect(text).not.toMatch(/\b(investment|token|tokens|returns?)\b/i);
    for (const slug of researchedSlugs) {
      expect(container.innerHTML).toContain(`/patterns/${slug}`);
    }
    expect(container.innerHTML).not.toContain("/patterns/problem-intake");
  });
});

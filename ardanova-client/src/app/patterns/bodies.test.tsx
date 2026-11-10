// @vitest-environment jsdom
import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { patterns, researchedPatterns } from "~/lib/writing";

import { PatternBody } from "./bodies";
import PatternsPage from "./page";

const researchedSlugs = researchedPatterns.map((pattern) => pattern.slug);

const fieldGuideSlugs = [
  "directors-minutes",
  "records-of-resolutions",
  "what-belongs-in-the-minutes",
  "readable-record",
  "informal-channels",
] as const;

const withAzoa = new Set([
  "a-record-is-not-a-right",
  "two-records-one-agreement",
  "decision-logs",
  "directors-minutes",
  "records-of-resolutions",
]);

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
      expect(text).toContain("This is a worked example, not a customer");
      expect(text).toContain("Every stat on this page is cited.");
      if (withAzoa.has(slug)) {
        expect(container.innerHTML).toContain("https://azoa.ardanova.com");
      }
      expect(container.innerHTML).not.toContain("azoa.ardanova.com/workflows");
      unmount();
    }
  });

  it("keeps the field notes from reading as legal advice", () => {
    for (const slug of fieldGuideSlugs) {
      const { container, unmount } = render(<PatternBody slug={slug} />);
      expect(container.textContent ?? "").toMatch(/not legal advice/i);
      unmount();
    }
  });

  it("keeps the opened figures and refuses a sterling conversion of level 3", () => {
    const directors = render(<PatternBody slug="directors-minutes" />);
    const directorsText = directors.container.textContent ?? "";
    expect(directorsText).toContain(
      "Every company must cause minutes of all proceedings at meetings of its directors to be recorded.",
    );
    expect(directorsText).toContain(
      "The records must be kept for at least ten years from the date of the meeting.",
    );
    expect(directorsText).toContain("level 3 on the standard scale");
    expect(directorsText).toContain("one-tenth of level 3");
    expect(directorsText).not.toMatch(/£|\bGBP\b|sterling/i);
    expect(directors.container.innerHTML).toContain(
      "https://www.legislation.gov.uk/ukpga/2006/46/section/248",
    );
    directors.unmount();

    const resolutions = render(<PatternBody slug="records-of-resolutions" />);
    const resolutionsText = resolutions.container.textContent ?? "";
    expect(resolutionsText).toContain(
      "copies of all resolutions of members passed otherwise than at general meetings",
    );
    expect(resolutionsText).toContain(
      "minutes of all proceedings of general meetings",
    );
    expect(resolutionsText).toContain(
      "at least ten years from the date of the resolution, meeting or decision (as appropriate)",
    );
    expect(resolutionsText).toContain("one-tenth of level 3");
    expect(resolutionsText).toContain("does not invent a pound figure");
    expect(resolutionsText).not.toMatch(/£|\bGBP\b|sterling/i);
    expect(resolutions.container.innerHTML).toContain(
      "https://www.legislation.gov.uk/ukpga/2006/46/section/355",
    );
    resolutions.unmount();
  });

  it("keeps informal channels a records habit", () => {
    const { container } = render(<PatternBody slug="informal-channels" />);
    const text = container.textContent ?? "";
    expect(text).toContain("not a surveillance recommendation");
    expect(text).toContain("not a recommendation to search private devices");
    expect(text).toContain("not as a reason to install a monitor");
    expect(text).toContain(
      "Record decisions arising from telephone or online meetings or via non-corporate communication channels, such as WhatsApp.",
    );
    expect(text).toContain(
      "Neither section is stretched into a chat-app offence.",
    );
    expect(container.innerHTML).toContain(
      "https://www.gov.uk/government/publications/guidance-management-of-private-office-information-and-records/guidance-for-the-management-of-private-office-information-and-records-html",
    );
    expect(container.innerHTML).toContain(
      "https://www.legislation.gov.uk/ukpga/2006/46/section/248",
    );
    expect(container.innerHTML).toContain(
      "https://www.legislation.gov.uk/ukpga/2006/46/section/355",
    );
  });
});

describe("patterns index", () => {
  it("names the research children and the simulation sentence", () => {
    const { container } = render(<PatternsPage />);
    const text = container.textContent ?? "";
    expect(text).toContain("The shape of a shared record.");
    expect(text).toContain("Eight patterns");
    expect(text).toContain("ECONOMY: SIMULATED");
    expect(text).toMatch(/do not automatically create legal ownership/i);
    expect(text).toContain("not legal advice");
    expect(text).not.toMatch(/\b(investment|token|tokens|returns?)\b/i);
    for (const slug of researchedSlugs) {
      expect(container.innerHTML).toContain(`/patterns/${slug}`);
    }
    for (const slug of fieldGuideSlugs) {
      expect(container.innerHTML).toContain(`/patterns/${slug}`);
    }
    expect(container.innerHTML).not.toContain("/patterns/problem-intake");
  });
});

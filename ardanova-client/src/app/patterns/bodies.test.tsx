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
  "what-happened-what-was-decided",
  "tna-section-46",
  "articles-of-incorporation",
  "company-registers",
  "sole-member-decisions",
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
      expect(text).toContain("A situation like this");
      expect(text).toContain("Sources");
      expect(text).not.toMatch(
        /Hero figures|Hero points|An illustrative scenario|Every stat on this page is cited/,
      );
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

  it("keeps the second batch on the opened figures and off forbidden links", () => {
    const record = render(
      <PatternBody slug="what-happened-what-was-decided" />,
    );
    const recordText = record.container.textContent ?? "";
    expect(recordText).toContain("systemic");
    expect(recordText).toContain("19 May 2026");
    expect(recordText).toContain(
      "what happened, what was decided, and how to do things",
    );
    expect(recordText).not.toMatch(/ISO 15489-1:2016,\s*clause/i);
    expect(record.container.innerHTML).toContain(
      "https://www.gov.uk/government/publications/records-and-information-created-by-hmrc/hmrc-records-and-information-collection-policy",
    );
    record.unmount();

    const code = render(<PatternBody slug="tna-section-46" />);
    const codeText = code.container.textContent ?? "";
    expect(codeText).toContain("ISBN 978-1-5286-2517-3");
    expect(codeText).toContain("32-page PDF");
    expect(codeText).toContain("15 July 2021");
    expect(codeText).toContain("not all information is a record");
    expect(codeText).toContain("does not form part of the Code itself");
    expect(codeText).toContain(
      "not a claim that ArdaNova is a relevant authority",
    );
    expect(codeText).toContain("in pursuit of legal obligations");
    expect(code.container.innerHTML).toContain(
      "https://www.gov.uk/government/publications/code-of-practice-on-the-management-of-records-issued-under-section-46-the-freedom-of-information-act-2000",
    );
    expect(code.container.innerHTML).toContain(
      "https://assets.publishing.service.gov.uk/media/6114c3278fa8f506c4d84100/Freedom_Information_Code_Practice_Web_Accessible.pdf",
    );
    code.unmount();

    const articles = render(<PatternBody slug="articles-of-incorporation" />);
    const articlesText = articles.container.textContent ?? "";
    expect(articlesText).toContain(
      "A company must have articles of association prescribing regulations for the company.",
    );
    expect(articlesText).toContain("to take at least one share each");
    expect(articlesText).toContain(
      "HTTP 404 on 2026-11-10 and is not linked here",
    );
    expect(articles.container.innerHTML).not.toMatch(
      /href="[^"]*(companieshouse|model-articles)/i,
    );
    expect(articles.container.innerHTML).toContain(
      "https://www.legislation.gov.uk/ukpga/2006/46/section/18",
    );
    articles.unmount();

    const register = render(<PatternBody slug="company-registers" />);
    const registerText = register.container.textContent ?? "";
    expect(registerText).toContain(
      "Every company must keep a register of its members.",
    );
    expect(registerText).toContain("26.10.2023");
    expect(registerText).toContain("otherwise prosp.");
    expect(registerText).toContain(
      "does not write as if 113A and 113B are already the everyday duty",
    );
    expect(registerText).toContain("10 November 2026");
    expect(register.container.innerHTML).toContain(
      "https://www.legislation.gov.uk/ukpga/2006/46/section/113",
    );
    expect(register.container.innerHTML).toContain(
      "https://www.legislation.gov.uk/ukpga/2006/46/section/1134",
    );
    register.unmount();

    const sole = render(<PatternBody slug="sole-member-decisions" />);
    const soleText = sole.container.textContent ?? "";
    expect(soleText).toContain("level 2 on the standard scale");
    expect(soleText).not.toMatch(/£|\bGBP\b|sterling/i);
    expect(soleText).toContain("does not affect the validity");
    expect(sole.container.innerHTML).toContain(
      "https://www.legislation.gov.uk/ukpga/2006/46/section/357",
    );
    expect(sole.container.innerHTML).toContain(
      "https://www.legislation.gov.uk/ukpga/2006/46/section/288",
    );
    sole.unmount();

    for (const slug of [
      "what-happened-what-was-decided",
      "tna-section-46",
      "articles-of-incorporation",
      "company-registers",
      "sole-member-decisions",
    ] as const) {
      const { container, unmount } = render(<PatternBody slug={slug} />);
      const text = container.textContent ?? "";
      expect(text).not.toMatch(
        /\b(investment|token|tokens|kyc|cap-table|cap table)\b/i,
      );
      expect(container.innerHTML).not.toMatch(
        /href="[^"]*(companieshouse|model-articles)/i,
      );
      unmount();
    }
  });
});

describe("patterns index", () => {
  it("names the research children and the simulation sentence", () => {
    const { container } = render(<PatternsPage />);
    const text = container.textContent ?? "";
    expect(text).toContain("The shape of a shared record.");
    expect(text).toContain("Thirteen patterns");
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

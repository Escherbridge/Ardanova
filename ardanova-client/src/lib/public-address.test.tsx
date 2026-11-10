// @vitest-environment jsdom
import { render, type RenderResult } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  authForPage: async () => null,
}));
vi.mock("~/trpc/server", () => ({
  HydrateClient: (props: { children?: ReactNode }) => props.children ?? null,
}));

const { default: Home } = await import("~/app/page");
const { default: TermsPage } = await import("~/app/terms/page");
const { default: PrivacyPage } = await import("~/app/privacy/page");
const { default: NotesPage } = await import("~/app/notes/page");
const { default: NotePage } = await import("~/app/notes/[slug]/page");
const { default: PatternsPage } = await import("~/app/patterns/page");
const { default: PatternPage } = await import("~/app/patterns/[slug]/page");
const { default: NewsPage } = await import("~/app/news/page");
const { siteGraph } = await import("~/lib/schema");
const { notes, patterns } = await import("~/lib/writing");

/** House number, then a street name, then a USPS street suffix. */
const STREET_ADDRESS =
  /\b\d{1,6}\s+(?:[NSEW]\.?\s+)?(?:[\w.'#-]+\s+){0,6}(?:alley|aly|avenue|ave|boulevard|blvd|circle|cir|court|ct|drive|dr|highway|hwy|lane|ln|parkway|pkwy|place|pl|road|rd|street|st|terrace|ter|trail|trl|way)\b\.?/i;

/** A 5-digit ZIP, or ZIP+4. A hyphen with any other tail is not one. */
const ZIP_CODE = /\b\d{5}(?:-\d{4})?(?![\d-])/;

const PO_BOX = /\bP\.?\s*O\.?\s*Box\b/i;

function visibleText(view: RenderResult): string {
  return view.container.textContent ?? "";
}

describe("public pages", () => {
  it("keeps the operator contact on the city and the public email", () => {
    for (const view of [render(<TermsPage />), render(<PrivacyPage />)]) {
      const mail = view.container.querySelector("a[href^='mailto:']");
      const location = view.container.querySelector("address > p:last-of-type");
      expect(`${location?.textContent ?? ""} ${mail?.textContent ?? ""}`).toBe(
        "Boise, Idaho contact@escherbridge.com",
      );
      expect(mail?.getAttribute("href")).toBe(
        "mailto:contact@escherbridge.com",
      );
    }
  });

  it("does not publish a street, a ZIP code, or a PO Box", async () => {
    const pages: { name: string; text: string }[] = [
      { name: "/", text: visibleText(render(await Home())) },
      { name: "/terms", text: visibleText(render(<TermsPage />)) },
      { name: "/privacy", text: visibleText(render(<PrivacyPage />)) },
      { name: "/notes", text: visibleText(render(<NotesPage />)) },
      { name: "/patterns", text: visibleText(render(<PatternsPage />)) },
      { name: "/news", text: visibleText(render(<NewsPage />)) },
      { name: "json-ld", text: JSON.stringify(siteGraph()) },
    ];

    for (const note of notes) {
      pages.push({
        name: `/notes/${note.slug}`,
        text: visibleText(
          render(
            await NotePage({ params: Promise.resolve({ slug: note.slug }) }),
          ),
        ),
      });
    }

    for (const pattern of patterns) {
      pages.push({
        name: `/patterns/${pattern.slug}`,
        text: visibleText(
          render(
            await PatternPage({
              params: Promise.resolve({ slug: pattern.slug }),
            }),
          ),
        ),
      });
    }

    const hits = pages.flatMap((page) => {
      const postal =
        STREET_ADDRESS.test(page.text) ||
        ZIP_CODE.test(page.text) ||
        PO_BOX.test(page.text);
      return postal ? [page.name] : [];
    });

    expect(hits).toEqual([]);
  });
});

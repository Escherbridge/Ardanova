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
const { default: ContactPage } = await import("~/app/contact/page");
const { notes, patterns } = await import("~/lib/writing");

/** Raw markdown that would mean a page dumped source instead of HTML. */
const MARKDOWN = /\*\*|`|# /;

function visibleText(view: RenderResult): string {
  return view.container.textContent ?? "";
}

describe("public pages render HTML, not markdown", () => {
  it("has one heading and no raw markdown or double-escaped ampersands", async () => {
    const pages: { name: string; view: RenderResult }[] = [
      { name: "/", view: render(await Home()) },
      { name: "/terms", view: render(<TermsPage />) },
      { name: "/privacy", view: render(<PrivacyPage />) },
      { name: "/notes", view: render(<NotesPage />) },
      { name: "/patterns", view: render(<PatternsPage />) },
      { name: "/news", view: render(<NewsPage />) },
      { name: "/contact", view: render(<ContactPage />) },
    ];

    for (const note of notes) {
      pages.push({
        name: `/notes/${note.slug}`,
        view: render(
          await NotePage({ params: Promise.resolve({ slug: note.slug }) }),
        ),
      });
    }

    for (const pattern of patterns) {
      pages.push({
        name: `/patterns/${pattern.slug}`,
        view: render(
          await PatternPage({
            params: Promise.resolve({ slug: pattern.slug }),
          }),
        ),
      });
    }

    const hits = pages.flatMap((page) => {
      const text = visibleText(page.view);
      const problems: string[] = [];
      if (page.view.container.querySelectorAll("h1").length !== 1) {
        problems.push("h1");
      }
      if (MARKDOWN.test(text)) problems.push("markdown");
      if (text.includes("&amp;")) problems.push("amp");
      page.view.unmount();
      return problems.length > 0 ? [`${page.name}: ${problems.join(",")}`] : [];
    });

    expect(hits).toEqual([]);
  });
});

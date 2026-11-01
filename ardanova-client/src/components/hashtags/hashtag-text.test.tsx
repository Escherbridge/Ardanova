// @vitest-environment jsdom
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { HashtagText } from "./hashtag-text";

/** Mirrors the extraction rules in ArdaNova.Application/Feed/Hashtags.cs (Feed/AGENTS.md §Hashtags). */
const ROWS: { row: string; text: string; links: string[] }[] = [
  {
    row: "a tag in running copy",
    text: "Framing the east wall Saturday, need two hands #carpentry",
    links: ["/tag/carpentry"],
  },
  { row: "a URL fragment", text: "Plans at example.com/#anchor", links: [] },
  { row: "a markdown heading", text: "# Heading\nMaterials list", links: [] },
  {
    row: "mid-word, doubled and entity hashes",
    text: "page#frag and ##twice and &#39;",
    links: [],
  },
  {
    row: "case folds and edge dashes trim; the copy keeps what was typed",
    text: "Roof day #Roofing-",
    links: ["/tag/roofing"],
  },
  { row: "one character is not a tag", text: "Plan #a", links: [] },
  {
    row: "more than 40 characters is not a tag",
    text: `#${"x".repeat(41)}`,
    links: [],
  },
  {
    row: "combining marks stay inside the tag",
    text: "चलो #साझा करें",
    links: [`/tag/${encodeURIComponent("साझा")}`],
  },
];

describe("HashtagText", () => {
  it.each(ROWS)("$row", ({ text, links }) => {
    const { container } = render(
      <p>
        <HashtagText text={text} />
      </p>,
    );

    expect(
      screen.queryAllByRole("link").map((link) => link.getAttribute("href")),
    ).toEqual(links);
    // Linking never drops or rewrites a character of the person's words.
    expect(container.textContent).toBe(text);
  });
});

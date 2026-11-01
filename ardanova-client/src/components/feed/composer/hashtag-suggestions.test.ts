import { describe, expect, it } from "vitest";

import type { TaxonomyTermDto } from "~/lib/contracts/taxonomy-contract";

import {
  hashtagFragmentAt,
  hashtagSuggestions,
  insertHashtag,
} from "./hashtag-suggestions";

/** A .NET TaxonomyTermDto as `GET /api/taxonomy` serializes it. */
const term = (
  slug: string,
  kind: string,
  label: string,
  sortOrder = 0,
  parentSlug: string | null = null,
): TaxonomyTermDto => ({
  slug,
  kind,
  label,
  description: null,
  parentSlug,
  sortOrder,
});

const terms = [
  term("CONSTRUCTION", "SECTOR", "Construction", 3),
  term("CARPENTRY", "SKILL", "Carpentry", 0, "CONSTRUCTION"),
  term("CARE", "SECTOR", "Care", 1),
  term("FOOD_BEVERAGE", "SECTOR", "Food and drink", 2),
  term("CATERING", "PROJECT_TYPE", "Catering", 0),
  term("COMMUNITY_GARDEN", "MOBILE_UNIT", "Community garden", 0),
];

describe("hashtagFragmentAt: the same look-behind the server's Hashtags.cs uses", () => {
  it.each([
    [
      "typing a tag at the end",
      "Framing #carp",
      13,
      { start: 8, query: "carp" },
    ],
    ["at the very start", "#Ca", 3, { start: 0, query: "ca" }],
    ["after a line break", "Saturday\n#co", 12, { start: 9, query: "co" }],
    ["one character is enough", "need #c", 7, { start: 5, query: "c" }],
    ["a bare #", "need #", 6, null],
    ["mid-word", "barn#ca", 7, null],
    ["a URL anchor", "see /docs/#setup", 16, null],
    ["an HTML entity", "it&#39", 6, null],
    ["a doubled #", "##ca", 4, null],
    ["the caret inside a tag", "#carpentry", 4, null],
    ["the caret after a finished tag", "#carp done", 10, null],
  ])("%s", (_case, text, caret, expected) => {
    expect(hashtagFragmentAt(text, caret)).toEqual(expected);
  });
});

describe("hashtagSuggestions", () => {
  it.each([
    [
      "tag prefixes beat label words, sectors before skills, then the API's sort order",
      "ca",
      ["care", "carpentry"],
    ],
    ["a skill is offered as its own tag", "carp", ["carpentry"]],
    [
      "a slug's separators read as the API's canon writes them",
      "food",
      ["food-beverage"],
    ],
    [
      "a label word that is not the slug still finds it",
      "drink",
      ["food-beverage"],
    ],
    ["project types and unknown kinds are not post tags", "cat", []],
    ["a tag typed in full is not offered again", "carpentry", []],
  ])("%s", (_case, query, expected) => {
    expect(hashtagSuggestions(terms, query).map(({ tag }) => tag)).toEqual(
      expected,
    );
  });

  it("caps the list", () => {
    const many = Array.from({ length: 12 }, (_, index) =>
      term(`CRAFT_${index}`, "SKILL", `Craft ${index}`, index),
    );
    expect(hashtagSuggestions(many, "craft", 6)).toHaveLength(6);
  });
});

describe("insertHashtag", () => {
  it.each([
    [
      "at the end it adds a space and lands after it",
      "Framing #carp",
      13,
      { text: "Framing #carpentry ", caret: 19 },
    ],
    [
      "before existing words it keeps the one space",
      "Framing #carp Saturday",
      13,
      { text: "Framing #carpentry Saturday", caret: 19 },
    ],
  ])("%s", (_case, text, caret, expected) => {
    const fragment = hashtagFragmentAt(text, caret);
    if (!fragment) throw new Error(`no tag at the caret in "${text}"`);
    expect(insertHashtag(text, fragment, caret, "carpentry")).toEqual(expected);
  });
});

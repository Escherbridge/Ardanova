import { canonicalHashtag } from "~/components/hashtags/hashtag-tokens";
import type { TaxonomyTermDto } from "~/lib/contracts/taxonomy-contract";
import { sectorTag } from "~/lib/feed/stream";

/** Inline `#` suggestions over canonical TaxonomyTerms. See ./AGENTS.md §Hashtag suggestions. */

/** The `#fragment` ending at the caret: the same look-behind as hashtag-tokens.ts (no mid-word, `/#`, `&#`, `##`). */
const FRAGMENT_BEFORE_CARET =
  /(?<![\p{L}\p{N}\p{M}_/&#-])#([\p{L}\p{N}\p{M}_-]{1,40})$/u;
const TAG_CHARACTER = /[\p{L}\p{N}\p{M}_-]/u;

export interface HashtagFragment {
  /** Index of the `#`. */
  start: number;
  /** What follows the `#`, lowercased. */
  query: string;
}

/** The tag being typed at `caret`, or null when the caret is not at the end of one. */
export function hashtagFragmentAt(
  text: string,
  caret: number,
): HashtagFragment | null {
  // Mid-tag ("#carp|entry") is editing an existing tag, not typing a new one.
  if (TAG_CHARACTER.test(text.charAt(caret))) return null;
  const match = FRAGMENT_BEFORE_CARET.exec(text.slice(0, caret));
  if (!match?.[1]) return null;
  return { start: match.index, query: match[1].toLowerCase() };
}

export interface HashtagSuggestion {
  /** Stored form, as the API's canon resolves it: FOOD_BEVERAGE -> "food-beverage". */
  tag: string;
  label: string;
  kind: "SECTOR" | "SKILL";
}

const SUGGESTED_KINDS = new Set(["SECTOR", "SKILL"]);
const compact = (value: string) => value.replace(/[-_\s]/g, "");

/**
 * Sectors and skills whose tag or label starts with `query`, best first: a tag prefix beats a label-word prefix,
 * then sectors before skills, then the API's sort order. A tag already typed in full is not suggested again.
 */
export function hashtagSuggestions(
  terms: readonly TaxonomyTermDto[],
  query: string,
  limit = 6,
): HashtagSuggestion[] {
  const wanted = query.toLowerCase();
  if (!wanted) return [];

  const ranked: { suggestion: HashtagSuggestion; score: number[] }[] = [];
  const seen = new Set<string>();
  for (const term of terms) {
    if (!SUGGESTED_KINDS.has(term.kind)) continue;
    const tag = canonicalHashtag(sectorTag(term.slug));
    if (!tag || tag === wanted || seen.has(tag)) continue;

    const label = term.label.toLowerCase();
    const tagMatch =
      tag.startsWith(wanted) || compact(tag).startsWith(compact(wanted));
    const labelMatch =
      compact(label).startsWith(compact(wanted)) ||
      label.split(/[\s/&-]+/).some((word) => word.startsWith(wanted));
    if (!tagMatch && !labelMatch) continue;

    seen.add(tag);
    ranked.push({
      suggestion: {
        tag,
        label: term.label,
        kind: term.kind === "SECTOR" ? "SECTOR" : "SKILL",
      },
      score: [tagMatch ? 0 : 1, term.kind === "SECTOR" ? 0 : 1, term.sortOrder],
    });
  }

  return ranked
    .sort(
      (left, right) =>
        compareScores(left.score, right.score) ||
        left.suggestion.tag.localeCompare(right.suggestion.tag),
    )
    .slice(0, limit)
    .map(({ suggestion }) => suggestion);
}

function compareScores(left: number[], right: number[]): number {
  for (let index = 0; index < left.length; index++) {
    const difference = (left[index] ?? 0) - (right[index] ?? 0);
    if (difference !== 0) return difference;
  }
  return 0;
}

/** Replaces the fragment with `#tag ` and returns where the caret lands (after the space). */
export function insertHashtag(
  text: string,
  fragment: HashtagFragment,
  caret: number,
  tag: string,
): { text: string; caret: number } {
  const before = text.slice(0, fragment.start);
  const after = text.slice(caret);
  const separator = /^\s/.test(after) ? "" : " ";
  const inserted = `#${tag}${separator}`;
  // Land after the whitespace either way, so typing continues as a new word.
  return {
    text: before + inserted + after,
    caret: before.length + inserted.length + (separator ? 0 : 1),
  };
}

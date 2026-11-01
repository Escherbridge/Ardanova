/** #tag parsing that mirrors ArdaNova.Application/Feed/Hashtags.cs; see components/feed/AGENTS.md §Hashtags. */

/** Same cap as Hashtags.MaxPerPost: a tag past the tenth is not stored, so it is not linked. */
export const MAX_HASHTAGS_PER_POST = 10;

// Hashtags.TagInCopy: '#' not mid-word, not after '/', '&', '#' or '-', then 2-40 tag characters and no more.
const TAG_IN_COPY = new RegExp(
  String.raw`(?<![\p{L}\p{N}\p{M}_/&#-])#([\p{L}\p{N}\p{M}_-]{2,40})(?![\p{L}\p{N}\p{M}_-])`,
  "gu",
);
const WHOLE_TAG = /^[\p{L}\p{N}\p{M}_-]{2,40}$/u;

export type HashtagToken =
  | { kind: "text"; text: string }
  | { kind: "tag"; text: string; tag: string };

/** One tag as typed ("#Carpentry", "roofing-") in stored form ("carpentry", "roofing"); null if not a tag. */
export function canonicalHashtag(raw: string): string | null {
  let text = raw.normalize("NFKC").trim();
  if (text.startsWith("#")) text = text.slice(1);
  if (!WHOLE_TAG.test(text)) return null;
  const trimmed = text.replace(/^[-_]+|[-_]+$/g, "").toLowerCase();
  return trimmed.length >= 2 ? trimmed : null;
}

/** Splits copy into text runs and #tags, keeping the writer's characters exactly as typed. */
export function tokenizeHashtags(text: string): HashtagToken[] {
  const tokens: HashtagToken[] = [];
  const linked = new Set<string>();
  let cursor = 0;
  for (const match of text.matchAll(TAG_IN_COPY)) {
    const tag = canonicalHashtag(match[1] ?? "");
    if (!tag) continue;
    if (!linked.has(tag) && linked.size >= MAX_HASHTAGS_PER_POST) continue;
    linked.add(tag);
    const start = match.index;
    if (start > cursor) {
      tokens.push({ kind: "text", text: text.slice(cursor, start) });
    }
    tokens.push({ kind: "tag", text: match[0], tag });
    cursor = start + match[0].length;
  }
  if (cursor < text.length) {
    tokens.push({ kind: "text", text: text.slice(cursor) });
  }
  return tokens;
}

/** The stored-form tags a piece of copy links inline. */
export function hashtagsIn(text: string): Set<string> {
  const tags = new Set<string>();
  for (const token of tokenizeHashtags(text)) {
    if (token.kind === "tag") tags.add(token.tag);
  }
  return tags;
}

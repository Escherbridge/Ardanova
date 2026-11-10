import { publicCanonical } from "~/lib/site";

import { noteCanonical, notes, patternCanonical, patterns } from "./writing";

export type IndexableUrl = {
  url: string;
  lastModified: string;
};

/**
 * Public indexable URLs. News is an empty shelf (noindex) and session-gated
 * app routes stay out.
 */
export function indexableUrls(): IndexableUrl[] {
  return [
    { url: publicCanonical("/"), lastModified: "2026-11-10" },
    { url: publicCanonical("/terms"), lastModified: "2026-11-10" },
    { url: publicCanonical("/privacy"), lastModified: "2026-11-10" },
    { url: publicCanonical("/notes"), lastModified: "2026-11-09" },
    ...notes.map((note) => ({
      url: noteCanonical(note.slug),
      lastModified: note.publishedAt,
    })),
    { url: publicCanonical("/patterns"), lastModified: "2026-11-10" },
    ...patterns.map((pattern) => ({
      url: patternCanonical(pattern.slug),
      lastModified: pattern.publishedAt,
    })),
  ];
}

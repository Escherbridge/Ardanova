import { ESCHERBRIDGE_PROJECTS } from "~/components/brand/escherbridge-projects";

import {
  CONTACT_DESCRIPTION,
  HOME_DESCRIPTION,
  NEWS_DESCRIPTION,
  NOTES_DESCRIPTION,
  PATTERNS_DESCRIPTION,
  PRIVACY_DESCRIPTION,
  TERMS_DESCRIPTION,
} from "./public-copy";
import { publicCanonical } from "./site";
import { noteCanonical, notes, patternCanonical, patterns } from "./writing";

function link(label: string, url: string, detail?: string): string {
  return detail ? `- [${label}](${url}): ${detail}` : `- [${label}](${url})`;
}

/** Curated llms.txt map. URLs come from the same public content as the sitemap. */
export function buildLlmsTxt(): string {
  const lines = [
    "# ArdaNova",
    "",
    `> ${HOME_DESCRIPTION} Operated by Escherbridge, LLC, Boise, Idaho. The economy on the deployed service is simulated. A record is not a promise of payment. Author: Escherbridge.`,
    "",
    "## Contact",
    link("Contact", publicCanonical("/contact"), CONTACT_DESCRIPTION),
    "",
    "## Notes",
    link("Notes", publicCanonical("/notes"), NOTES_DESCRIPTION),
    ...notes.map((note) =>
      link(note.title, noteCanonical(note.slug), note.description),
    ),
    "",
    "## Patterns",
    link("Patterns", publicCanonical("/patterns"), PATTERNS_DESCRIPTION),
    ...patterns.map((pattern) =>
      link(pattern.title, patternCanonical(pattern.slug), pattern.description),
    ),
    "",
    "## News",
    link("News", publicCanonical("/news"), NEWS_DESCRIPTION),
    "",
    "## Legal",
    link("Terms of use", publicCanonical("/terms"), TERMS_DESCRIPTION),
    link("Privacy notice", publicCanonical("/privacy"), PRIVACY_DESCRIPTION),
    "",
    "## An Escherbridge project",
    ...ESCHERBRIDGE_PROJECTS.map((site) => link(site.label, site.href)),
    "",
    "## Machine-readable",
    link(
      "RSS",
      publicCanonical("/feed.xml"),
      "Notes and pattern pages. News is omitted until a dated item exists.",
    ),
    link("Sitemap", publicCanonical("/sitemap.xml"), "Indexable public URLs."),
    link(
      "Robots",
      publicCanonical("/robots.txt"),
      "Allows the public site and disallows account, admin, and workspace routes.",
    ),
    "",
    "## Workspace",
    "The workspace at /dashboard needs a Google sign-in. There is no public API for agents yet.",
    "",
    "## Crawler notes",
    "- This file follows the llms.txt convention (https://llmstxt.org/).",
    "- Titles and URLs come from the same public content as the sitemap and the RSS feed.",
    "- No street address is published. The operator line is Escherbridge, LLC, Boise, Idaho.",
    "- Workspace records are not payment, equity, or a promise that anyone will be paid.",
    "",
  ];
  return lines.join("\n");
}

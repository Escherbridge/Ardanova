import { SITE_ORIGIN } from "~/lib/site";

import { feedEntries } from "./writing";

function escapeXml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function rfc822(isoDate: string): string {
  return new Date(`${isoDate}T00:00:00.000Z`).toUTCString();
}

/** RSS 2.0 for notes and pattern pages. News is omitted until a dated item exists. */
export function buildFeedXml(): string {
  const items = feedEntries()
    .map(
      (entry) => `    <item>
      <title>${escapeXml(entry.title)}</title>
      <link>${escapeXml(entry.url)}</link>
      <guid isPermaLink="true">${escapeXml(entry.url)}</guid>
      <pubDate>${rfc822(entry.publishedAt)}</pubDate>
      <description>${escapeXml(entry.description)}</description>
      <dc:creator>Escherbridge</dc:creator>
    </item>`,
    )
    .join("\n");

  return `<?xml version="1.0" encoding="UTF-8"?>
<rss version="2.0" xmlns:dc="http://purl.org/dc/elements/1.1/">
  <channel>
    <title>ArdaNova</title>
    <link>${SITE_ORIGIN}/</link>
    <description>Notes and patterns from the ArdaNova workspace. The economy is simulated.</description>
    <language>en-us</language>
${items}
  </channel>
</rss>
`;
}

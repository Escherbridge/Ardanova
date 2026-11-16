import { describe, expect, it } from "vitest";

import { GET as wellKnownLlms } from "~/app/.well-known/llms.txt/route";
import { GET as publicLlms } from "~/app/llms.txt/route";

import { buildFeedXml } from "./feed-xml";
import { buildLlmsTxt } from "./llms-txt";
import {
  CONTACT_DESCRIPTION,
  HOME_DESCRIPTION,
  HOME_TITLE,
  NEWS_DESCRIPTION,
  NOTES_DESCRIPTION,
  PATTERNS_DESCRIPTION,
  PRIVACY_DESCRIPTION,
  PUBLIC_INDEX_DESCRIPTIONS,
  TERMS_DESCRIPTION,
} from "./public-copy";
import { indexableUrls } from "./public-urls";
import { siteGraph } from "./schema";
import {
  entryTitle,
  feedEntries,
  notes,
  patternCanonical,
  patterns,
} from "./writing";

describe("public sitemap URLs", () => {
  const urls = indexableUrls().map((entry) => entry.url);

  it("lists the homepage, legal pages, notes, and patterns on ardanova.com", () => {
    expect(urls).toContain("https://ardanova.com/");
    expect(urls).toContain("https://ardanova.com/terms");
    expect(urls).toContain("https://ardanova.com/privacy");
    expect(urls).toContain("https://ardanova.com/contact");
    expect(urls).toContain("https://ardanova.com/notes");
    expect(urls).toContain(
      "https://ardanova.com/notes/solutionary-thinking-in-practice",
    );
    expect(urls).toContain(
      "https://ardanova.com/notes/social-ecology-and-shared-stewardship",
    );
    expect(urls).toContain(
      "https://ardanova.com/notes/ownership-without-surveillance",
    );
    expect(urls).toContain(
      "https://ardanova.com/notes/why-shared-work-breaks-between-orgs",
    );
    expect(urls).toContain(
      "https://ardanova.com/notes/separating-contribution-ledgers-from-governance-votes",
    );
    expect(urls).toContain(
      "https://ardanova.com/notes/a-record-is-not-a-right",
    );
    expect(urls).toContain(
      "https://ardanova.com/notes/two-records-one-agreement",
    );
    expect(urls).toContain("https://ardanova.com/patterns");
    expect(urls).toContain(
      "https://ardanova.com/patterns/reconciling-two-ledgers-without-a-central-custodian",
    );
    expect(urls).toContain(
      "https://ardanova.com/patterns/append-only-amendments-and-retractions",
    );
    expect(urls).toContain(
      "https://ardanova.com/patterns/a-record-is-not-a-right",
    );
    expect(urls).toContain(
      "https://ardanova.com/patterns/two-records-one-agreement",
    );
    expect(urls).toContain("https://ardanova.com/patterns/decision-logs");
    expect(urls).toContain("https://ardanova.com/patterns/problem-intake");
    expect(urls).toContain("https://ardanova.com/patterns/directors-minutes");
    expect(urls).toContain(
      "https://ardanova.com/patterns/records-of-resolutions",
    );
    expect(urls).toContain(
      "https://ardanova.com/patterns/what-belongs-in-the-minutes",
    );
    expect(urls).toContain("https://ardanova.com/patterns/readable-record");
    expect(urls).toContain("https://ardanova.com/patterns/informal-channels");
    expect(urls).toContain(
      "https://ardanova.com/patterns/what-happened-what-was-decided",
    );
    expect(urls).toContain("https://ardanova.com/patterns/tna-section-46");
    expect(urls).toContain(
      "https://ardanova.com/patterns/articles-of-incorporation",
    );
    expect(urls).toContain("https://ardanova.com/patterns/company-registers");
    expect(urls).toContain(
      "https://ardanova.com/patterns/sole-member-decisions",
    );
  });

  it("keeps pattern titles, descriptions, and canonicals unique", () => {
    const titles = patterns.map((pattern) => pattern.title);
    const descriptions = patterns.map((pattern) => pattern.description);
    const canonicals = patterns.map((pattern) =>
      patternCanonical(pattern.slug),
    );
    expect(new Set(titles).size).toBe(titles.length);
    expect(new Set(descriptions).size).toBe(descriptions.length);
    expect(new Set(canonicals).size).toBe(canonicals.length);
    expect(urls).not.toContain("https://ardanova.com/guides");
  });

  it("omits the empty news shelf and session-gated app routes", () => {
    const joined = urls.join("\n");
    expect(joined).not.toContain("/news");
    expect(joined).not.toContain("/projects");
    expect(joined).not.toContain("/opportunities");
    expect(joined).not.toContain("/governance");
    expect(joined).not.toContain("/dashboard");
    expect(joined).not.toContain("/auth");
    expect(joined).not.toContain("railway.app");
  });

  it("keeps document titles unique and within 60 characters with the brand", () => {
    const branded = [
      HOME_TITLE,
      "Notes — ArdaNova",
      "Patterns — ArdaNova",
      "News — ArdaNova",
      "Terms of use — ArdaNova",
      "Privacy notice — ArdaNova",
      "Contact — ArdaNova",
      ...notes.map((note) => `${entryTitle(note)} — ArdaNova`),
      ...patterns.map((pattern) => `${entryTitle(pattern)} — ArdaNova`),
    ];
    expect(branded.every((title) => title.length <= 60)).toBe(true);
    expect(new Set(branded).size).toBe(branded.length);
  });

  it("keeps public descriptions in the snippet range", () => {
    const descriptions = [
      ...PUBLIC_INDEX_DESCRIPTIONS,
      ...notes.map((note) => note.description),
      ...patterns.map((pattern) => pattern.description),
    ];
    const lengths = descriptions.map((description) => description.length);
    expect(Math.min(...lengths)).toBeGreaterThanOrEqual(150);
    expect(Math.max(...lengths)).toBeLessThanOrEqual(165);
    expect(HOME_DESCRIPTION.length).toBeGreaterThanOrEqual(150);
    expect(NOTES_DESCRIPTION.length).toBeLessThanOrEqual(165);
    expect(PATTERNS_DESCRIPTION.length).toBeLessThanOrEqual(165);
    expect(NEWS_DESCRIPTION.length).toBeGreaterThanOrEqual(150);
    expect(TERMS_DESCRIPTION.length).toBeGreaterThanOrEqual(150);
    expect(PRIVACY_DESCRIPTION.length).toBeGreaterThanOrEqual(150);
    expect(CONTACT_DESCRIPTION).toContain("Escherbridge, LLC, Boise, Idaho");
    expect(CONTACT_DESCRIPTION).toContain("contact@escherbridge.com");
  });
});

describe("RSS feed", () => {
  const xml = buildFeedXml();

  it("is RSS and lists the notes and pattern pages", () => {
    expect(xml.startsWith("<?xml")).toBe(true);
    expect(xml).toContain("<rss");
    for (const entry of feedEntries()) {
      expect(xml).toContain(entry.url);
    }
    expect(xml).not.toContain("/news");
    expect(xml).not.toMatch(/\b(investment|token|tokens|returns?)\b/i);
  });
});

describe("sitewide JSON-LD", () => {
  it("names the operator and the public host", () => {
    const graph = siteGraph()["@graph"];
    const organization = graph.find((node) => node["@type"] === "Organization");
    const website = graph.find((node) => node["@type"] === "WebSite");
    expect(organization).toMatchObject({
      name: "ArdaNova",
      url: "https://ardanova.com",
      email: "contact@escherbridge.com",
      parentOrganization: { name: "Escherbridge, LLC" },
    });
    expect(website).toMatchObject({
      url: "https://ardanova.com",
      inLanguage: "en-US",
    });
    expect(JSON.stringify(graph)).not.toMatch(
      /instagram|facebook|twitter\.com/i,
    );
    expect(postalAddressKeys(graph)).toEqual([]);
  });
});

describe("llms.txt", () => {
  const text = buildLlmsTxt();

  it("summarizes the site and lists real public URLs", () => {
    expect(text.startsWith("# ArdaNova\n")).toBe(true);
    expect(text).toContain("Escherbridge, LLC, Boise, Idaho");
    expect(text).toContain("contact@escherbridge.com");
    expect(text).not.toContain("<!--");
    expect(text).toContain("https://ardanova.com/contact");
    expect(text).toContain("https://ardanova.com/feed.xml");
    expect(text).toContain("https://ardanova.com/sitemap.xml");
    expect(text).toContain("https://llmstxt.org/");
    const indexable = new Set(indexableUrls().map((entry) => entry.url));
    const allowed = new Set([
      ...indexable,
      "https://ardanova.com/news",
      "https://ardanova.com/feed.xml",
      "https://ardanova.com/sitemap.xml",
      "https://ardanova.com/robots.txt",
    ]);
    const siteLinks = [
      ...text.matchAll(/\]\((https:\/\/ardanova\.com[^)]*)\)/g),
    ].map((match) => match[1]);
    expect(siteLinks.length).toBeGreaterThan(10);
    expect(siteLinks.filter((url) => !allowed.has(url ?? ""))).toEqual([]);
    for (const note of notes) {
      expect(text).toContain(`https://ardanova.com/notes/${note.slug}`);
    }
    for (const pattern of patterns) {
      expect(text).toContain(`https://ardanova.com/patterns/${pattern.slug}`);
    }
    expect(text).not.toContain("railway.app");
    expect(text).not.toMatch(/\b(investment|token|tokens|returns?)\b/i);
    expect(text).not.toMatch(/\b\d{5}(?:-\d{4})?\b/);
    expect(text).toContain(
      "## Workspace\nThe workspace at /dashboard needs a Google sign-in. There is no public API for agents yet.",
    );
  });

  it("is the same document at /.well-known/llms.txt", async () => {
    const [page, wellKnown] = await Promise.all([
      publicLlms(),
      wellKnownLlms(),
    ]);
    expect(wellKnown.headers.get("content-type")).toBe(
      page.headers.get("content-type"),
    );
    expect(await wellKnown.text()).toBe(await page.text());
  });
});

const postalField = new Set(["streetAddress", "postalCode"]);

function postalAddressKeys(value: unknown): string[] {
  if (Array.isArray(value)) return value.flatMap(postalAddressKeys);
  if (typeof value !== "object" || value === null) return [];
  return Object.entries(value).flatMap(([key, child]) => {
    const nested = postalAddressKeys(child);
    return postalField.has(key) ? [key, ...nested] : nested;
  });
}

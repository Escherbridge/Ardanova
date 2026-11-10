import { describe, expect, it } from "vitest";

import { buildFeedXml } from "./feed-xml";
import { indexableUrls } from "./public-urls";
import { siteGraph } from "./schema";
import { feedEntries, patternCanonical, patterns } from "./writing";

describe("public sitemap URLs", () => {
  const urls = indexableUrls().map((entry) => entry.url);

  it("lists the homepage, legal pages, notes, and patterns on ardanova.com", () => {
    expect(urls).toContain("https://ardanova.com/");
    expect(urls).toContain("https://ardanova.com/terms");
    expect(urls).toContain("https://ardanova.com/privacy");
    expect(urls).toContain("https://ardanova.com/notes");
    expect(urls).toContain(
      "https://ardanova.com/notes/a-record-is-not-a-right",
    );
    expect(urls).toContain(
      "https://ardanova.com/notes/two-records-one-agreement",
    );
    expect(urls).toContain("https://ardanova.com/patterns");
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
  });
});

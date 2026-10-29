import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";

import {
  categoryLabel,
  PROJECT_TYPE_TERMS,
  projectTypeDescription,
  projectTypeLabel,
  SECTOR_LABELS,
  SKILL_LABELS,
  skillLabel,
} from "./labels";

describe("taxonomy labels", () => {
  it.each([
    [
      "known project type",
      projectTypeLabel("PHYSICAL_BUILD"),
      "Physical build",
    ],
    [
      "project type a newer API added",
      projectTypeLabel("MOBILE_KITCHEN"),
      "Mobile kitchen",
    ],
    [
      "curated sector, not its humanized code",
      categoryLabel("FOOD_BEVERAGE"),
      "Food and drink",
    ],
    ["legacy category code", categoryLabel("ARTS_CULTURE"), "Arts and culture"],
    ["unknown sector code", categoryLabel("SHIP_REPAIR"), "Ship repair"],
    [
      "free-text custom category, kept as typed",
      categoryLabel(" 3D printing "),
      "3D printing",
    ],
    ["curated skill acronym", skillLabel("IT_SUPPORT"), "IT support"],
    ["unknown skill code", skillLabel("FORKLIFT_DRIVING"), "Forklift driving"],
  ])("labels a %s", (_case, actual, expected) => {
    expect(actual).toBe(expected);
  });

  it("describes known project types and stays silent on unknown ones", () => {
    expect(projectTypeDescription("COOPERATIVE")).toBe(
      "A business owned and run by its members",
    );
    expect(projectTypeDescription("MOBILE_KITCHEN")).toBeUndefined();
  });
});

/**
 * The client mirrors the seeded vocabulary until the API serves TaxonomyTerm, so a label edited in
 * one place and not the other would show two names for one thing. This reads the migration that
 * seeds the database and compares every term, both ways.
 */
describe("the client labels match the seeded TaxonomyTerm rows", () => {
  const seedSql = readFileSync(
    "prisma/migrations/20261027003000_taxonomy_term/migration.sql",
    "utf8",
  ).replace(/^--.*$/gm, "");

  function seededTerms(kind: string) {
    const statement = seedSql
      .split(";")
      .find((sql) => sql.includes(`'${kind}'::"TaxonomyKind"`));
    if (!statement)
      throw new Error(`No ${kind} seed statement in the migration`);
    const rows = [
      ...statement.matchAll(/\('([A-Z0-9_]+)', '([^']*)', '([^']*)'/g),
    ];
    return rows.map(([, slug, label, third]) => ({ slug, label, third }));
  }

  const asLabels = (terms: { slug: string; label: string }[]) =>
    Object.fromEntries(terms.map(({ slug, label }) => [slug, label]));

  it("for sectors, including all ten legacy category codes", () => {
    const sectors = seededTerms("SECTOR");

    expect(asLabels(sectors)).toEqual(SECTOR_LABELS);
    expect(Object.keys(SECTOR_LABELS)).toEqual(
      expect.arrayContaining([
        "AGRICULTURE",
        "ARTS_CULTURE",
        "BUSINESS",
        "EDUCATION",
        "ENVIRONMENT",
        "FINANCE",
        "HEALTHCARE",
        "OTHER",
        "SOCIAL_IMPACT",
        "TECHNOLOGY",
      ]),
    );
  });

  it("for project types, label and description", () => {
    const seeded = Object.fromEntries(
      seededTerms("PROJECT_TYPE").map(({ slug, label, third }) => [
        slug,
        { label, description: third },
      ]),
    );

    expect(seeded).toEqual(PROJECT_TYPE_TERMS);
  });

  it("for skills, each under a seeded sector", () => {
    const skills = seededTerms("SKILL");

    expect(asLabels(skills)).toEqual(SKILL_LABELS);
    expect(
      skills.filter(({ third: sector }) => !(sector in SECTOR_LABELS)),
    ).toEqual([]);
  });
});

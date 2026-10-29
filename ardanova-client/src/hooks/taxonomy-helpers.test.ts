import { describe, expect, it } from "vitest";

import {
  labelForTerm,
  skillsBySector,
  type TaxonomyTermLike,
} from "./taxonomy-helpers";

const SECTOR_AGRICULTURE: TaxonomyTermLike = {
  slug: "AGRICULTURE",
  kind: "SECTOR",
  label: "Agriculture",
  parentSlug: null,
};

const SKILL_FARMING: TaxonomyTermLike = {
  slug: "FARMING",
  kind: "SKILL",
  label: "Farming",
  parentSlug: "AGRICULTURE",
};

const SKILL_GARDENING: TaxonomyTermLike = {
  slug: "GARDENING_LANDSCAPING",
  kind: "SKILL",
  label: "Gardening and landscaping",
  parentSlug: "AGRICULTURE",
};

const SKILL_CARPENTRY: TaxonomyTermLike = {
  slug: "CARPENTRY",
  kind: "SKILL",
  label: "Carpentry",
  parentSlug: "CONSTRUCTION",
};

const terms = [
  SECTOR_AGRICULTURE,
  SKILL_FARMING,
  SKILL_GARDENING,
  SKILL_CARPENTRY,
];

describe("labelForTerm", () => {
  it("uses the loaded term's own label when the slug is known", () => {
    expect(labelForTerm(terms, "AGRICULTURE", "SECTOR")).toBe("Agriculture");
  });

  it("falls back to the labels.ts sector table for a slug not yet loaded", () => {
    expect(labelForTerm([], "FOOD_BEVERAGE", "SECTOR")).toBe("Food and drink");
  });

  it("falls back to the labels.ts skill table for a slug not yet loaded", () => {
    expect(labelForTerm([], "IT_SUPPORT", "SKILL")).toBe("IT support");
  });

  it("falls back to the labels.ts project type table for a slug not yet loaded", () => {
    expect(labelForTerm([], "PHYSICAL_BUILD", "PROJECT_TYPE")).toBe(
      "Physical build",
    );
  });

  it("humanizes a code this client has never seen, instead of rendering blank", () => {
    expect(labelForTerm([], "FORKLIFT_DRIVING", "SKILL")).toBe(
      "Forklift driving",
    );
    expect(labelForTerm([], "", undefined)).toBe("");
  });
});

describe("skillsBySector", () => {
  it("groups SKILL terms by their parent SECTOR slug, preserving order", () => {
    expect(skillsBySector(terms)).toEqual({
      AGRICULTURE: [SKILL_FARMING, SKILL_GARDENING],
      CONSTRUCTION: [SKILL_CARPENTRY],
    });
  });

  it("excludes SECTOR and PROJECT_TYPE terms, and skills with no parent", () => {
    const orphanSkill: TaxonomyTermLike = {
      slug: "ORPHAN",
      kind: "SKILL",
      label: "Orphan",
      parentSlug: null,
    };
    const result = skillsBySector([SECTOR_AGRICULTURE, orphanSkill]);
    expect(result).toEqual({});
  });
});

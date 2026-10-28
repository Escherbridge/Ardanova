"use client";

import { api, type RouterOutputs } from "~/trpc/react";
import { labelForTerm, skillsBySector as groupSkillsBySector } from "./taxonomy-helpers";
import type { TaxonomyKindCode } from "~/lib/contracts/taxonomy-contract";

export type TaxonomyTerm = RouterOutputs["taxonomy"]["getAll"][number];

/**
 * Taxonomy terms for one kind (or all kinds when omitted), with a label lookup that
 * falls back to the Phase 0 labels.ts tables while the API hasn't answered yet or
 * doesn't recognize a slug, so a label never renders blank. See taxonomy-helpers.ts
 * for the pure, directly-tested logic this wraps.
 */
export function useTaxonomy(kind?: TaxonomyKindCode) {
  const query = api.taxonomy.getAll.useQuery(kind ? { kind } : undefined, {
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });

  const terms = query.data ?? [];

  return {
    terms,
    isLoading: query.isLoading,
    error: query.error,
    labelFor: (slug: string, termKind: TaxonomyKindCode | undefined = kind) =>
      labelForTerm(terms, slug, termKind),
    skillsBySector: () => groupSkillsBySector(terms),
  };
}

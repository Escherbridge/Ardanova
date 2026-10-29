// Pure taxonomy term helpers; use-taxonomy.ts wires these to the tRPC query.
import {
  categoryLabel,
  humanizeCode,
  projectTypeLabel,
  skillLabel,
} from "~/lib/taxonomy/labels";
import type { TaxonomyKindCode } from "~/lib/contracts/taxonomy-contract";

export interface TaxonomyTermLike {
  slug: string;
  kind: TaxonomyKindCode;
  label: string;
  parentSlug: string | null;
}

/** Label for a slug this client has no loaded term for yet. Never blank. */
export function fallbackLabel(
  kind: TaxonomyKindCode | undefined,
  slug: string,
): string {
  switch (kind) {
    case "SECTOR":
      return categoryLabel(slug);
    case "SKILL":
      return skillLabel(slug);
    case "PROJECT_TYPE":
      return projectTypeLabel(slug);
    default:
      return humanizeCode(slug);
  }
}

/** Label for `slug`: the loaded term's own label, or the Phase 0 fallback table for `termKind`. */
export function labelForTerm<T extends TaxonomyTermLike>(
  terms: readonly T[],
  slug: string,
  termKind: TaxonomyKindCode | undefined,
): string {
  const term = terms.find((t) => t.slug === slug);
  if (term) return term.label;
  return fallbackLabel(termKind, slug);
}

/** SKILL terms grouped by their parent SECTOR slug; order within each group is preserved. */
export function skillsBySector<T extends TaxonomyTermLike>(
  terms: readonly T[],
): Record<string, T[]> {
  const groups: Record<string, T[]> = {};
  for (const term of terms) {
    if (term.kind !== "SKILL" || !term.parentSlug) continue;
    (groups[term.parentSlug] ??= []).push(term);
  }
  return groups;
}

import { z } from "zod";

/** TaxonomyTerm kinds this client knows; the API may add more (see `taxonomyKindCodeSchema`). */
export const TAXONOMY_KINDS = ["SECTOR", "SKILL", "PROJECT_TYPE"] as const;

export const taxonomyKindSchema = z.enum(TAXONOMY_KINDS);

export type TaxonomyKind = z.infer<typeof taxonomyKindSchema>;
/** A TaxonomyTerm kind as read from the API: a known value, or one a newer API added since. */
export type TaxonomyKindCode = TaxonomyKind | (string & {});
/** Read-side kind: any non-empty code. See src/lib/contracts/project-contract.ts for the same tolerance shape. */
export const taxonomyKindCodeSchema: z.ZodType<TaxonomyKindCode> = z
  .string()
  .min(1);

/** A .NET TaxonomyTermDto. Unknown `kind` values and extra fields parse rather than fail. */
export const taxonomyTermDtoSchema = z.object({
  slug: z.string().min(1),
  kind: taxonomyKindCodeSchema,
  label: z.string(),
  description: z.string().nullable(),
  parentSlug: z.string().nullable(),
  sortOrder: z.number().int(),
});

export const taxonomyTermListDtoSchema = z.array(taxonomyTermDtoSchema);

export type TaxonomyTermDto = z.infer<typeof taxonomyTermDtoSchema>;

export function isTaxonomyKind(value: string): value is TaxonomyKind {
  return taxonomyKindSchema.safeParse(value).success;
}

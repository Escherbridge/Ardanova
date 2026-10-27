import { z } from 'zod';
import { TaxonomyKindSchema } from '../inputTypeSchemas/TaxonomyKindSchema'

/////////////////////////////////////////
// TAXONOMY TERM SCHEMA
/////////////////////////////////////////

export const TaxonomyTermSchema = z.object({
  kind: TaxonomyKindSchema,
  id: z.string().cuid(),
  slug: z.string(),
  label: z.string(),
  description: z.string().nullable(),
  parentId: z.string().nullable(),
  sortOrder: z.number().int(),
  isActive: z.boolean(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type TaxonomyTerm = z.infer<typeof TaxonomyTermSchema>

export default TaxonomyTermSchema;

import { z } from 'zod';
import { PBITypeSchema } from '../inputTypeSchemas/PBITypeSchema'
import { PBIStatusSchema } from '../inputTypeSchemas/PBIStatusSchema'
import { PrioritySchema } from '../inputTypeSchemas/PrioritySchema'

/////////////////////////////////////////
// PRODUCT BACKLOG ITEM SCHEMA
/////////////////////////////////////////

export const ProductBacklogItemSchema = z.object({
  type: PBITypeSchema,
  status: PBIStatusSchema,
  priority: PrioritySchema,
  id: z.string().cuid(),
  featureId: z.string(),
  title: z.string(),
  description: z.string().nullable(),
  storyPoints: z.number().int().nullable(),
  acceptanceCriteria: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assigneeId: z.string().nullable(),
})

export type ProductBacklogItem = z.infer<typeof ProductBacklogItemSchema>

export default ProductBacklogItemSchema;

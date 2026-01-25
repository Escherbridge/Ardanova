import { z } from 'zod';
import { BacklogItemTypeSchema } from '../inputTypeSchemas/BacklogItemTypeSchema'
import { BacklogStatusSchema } from '../inputTypeSchemas/BacklogStatusSchema'

/////////////////////////////////////////
// BACKLOG ITEM SCHEMA
/////////////////////////////////////////

export const BacklogItemSchema = z.object({
  type: BacklogItemTypeSchema,
  status: BacklogStatusSchema,
  id: z.string().cuid(),
  pbiId: z.string(),
  title: z.string(),
  description: z.string().nullable(),
  estimate: z.number().int().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type BacklogItem = z.infer<typeof BacklogItemSchema>

export default BacklogItemSchema;

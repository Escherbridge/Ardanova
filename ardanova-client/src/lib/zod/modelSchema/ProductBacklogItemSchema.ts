import { z } from 'zod';
import { Prisma } from '@prisma/client'
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
  projectId: z.string(),
  featureId: z.string().nullable(),
  sprintId: z.string().nullable(),
  epicId: z.string().nullable(),
  milestoneId: z.string().nullable(),
  guildId: z.string().nullable(),
  title: z.string(),
  description: z.string().nullable(),
  storyPoints: z.number().int().nullable(),
  acceptanceCriteria: z.string().nullable(),
  equityReward: z.instanceof(Prisma.Decimal, { message: "Field 'equityReward' must be a Decimal. Location: ['Models', 'ProductBacklogItem']"}).nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assigneeId: z.string().nullable(),
})

export type ProductBacklogItem = z.infer<typeof ProductBacklogItemSchema>

export default ProductBacklogItemSchema;

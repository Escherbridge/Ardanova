import { z } from 'zod';
import { FeatureStatusSchema } from '../inputTypeSchemas/FeatureStatusSchema'
import { PrioritySchema } from '../inputTypeSchemas/PrioritySchema'

/////////////////////////////////////////
// FEATURE SCHEMA
/////////////////////////////////////////

export const FeatureSchema = z.object({
  status: FeatureStatusSchema,
  priority: PrioritySchema,
  id: z.string().cuid(),
  sprintId: z.string(),
  title: z.string(),
  description: z.string().nullable(),
  order: z.number().int(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  assigneeId: z.string().nullable(),
})

export type Feature = z.infer<typeof FeatureSchema>

export default FeatureSchema;

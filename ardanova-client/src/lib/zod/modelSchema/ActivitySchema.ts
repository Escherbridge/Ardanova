import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { ActivityTypeSchema } from '../inputTypeSchemas/ActivityTypeSchema'

/////////////////////////////////////////
// ACTIVITY SCHEMA
/////////////////////////////////////////

export const ActivitySchema = z.object({
  type: ActivityTypeSchema,
  id: z.string().cuid(),
  userId: z.string(),
  projectId: z.string().nullable(),
  entityType: z.string(),
  entityId: z.string(),
  action: z.string(),
  metadata: JsonValueSchema.nullable(),
  createdAt: z.coerce.date(),
})

export type Activity = z.infer<typeof ActivitySchema>

export default ActivitySchema;

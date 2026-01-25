import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { XPEventTypeSchema } from '../inputTypeSchemas/XPEventTypeSchema'

/////////////////////////////////////////
// XP EVENT SCHEMA
/////////////////////////////////////////

export const XPEventSchema = z.object({
  eventType: XPEventTypeSchema,
  id: z.string().cuid(),
  userId: z.string(),
  amount: z.number().int(),
  source: z.string(),
  sourceId: z.string().nullable(),
  metadata: JsonValueSchema.nullable(),
  createdAt: z.coerce.date(),
})

export type XPEvent = z.infer<typeof XPEventSchema>

export default XPEventSchema;

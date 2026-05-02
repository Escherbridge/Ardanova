import { z } from 'zod';

/////////////////////////////////////////
// EVENT CO HOST SCHEMA
/////////////////////////////////////////

export const EventCoHostSchema = z.object({
  id: z.string().cuid(),
  eventId: z.string(),
  userId: z.string(),
  addedAt: z.coerce.date(),
})

export type EventCoHost = z.infer<typeof EventCoHostSchema>

export default EventCoHostSchema;

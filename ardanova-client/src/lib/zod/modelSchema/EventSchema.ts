import { z } from 'zod';
import { EventTypeSchema } from '../inputTypeSchemas/EventTypeSchema'
import { EventVisibilitySchema } from '../inputTypeSchemas/EventVisibilitySchema'
import { EventStatusSchema } from '../inputTypeSchemas/EventStatusSchema'

/////////////////////////////////////////
// EVENT SCHEMA
/////////////////////////////////////////

export const EventSchema = z.object({
  type: EventTypeSchema,
  visibility: EventVisibilitySchema,
  status: EventStatusSchema,
  id: z.string().cuid(),
  title: z.string(),
  slug: z.string(),
  description: z.string().nullable(),
  location: z.string().nullable(),
  locationUrl: z.string().nullable(),
  isOnline: z.boolean(),
  meetingUrl: z.string().nullable(),
  timezone: z.string(),
  startDate: z.coerce.date(),
  endDate: z.coerce.date(),
  maxAttendees: z.number().int().nullable(),
  attendeesCount: z.number().int(),
  coverImage: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  organizerId: z.string(),
  projectId: z.string().nullable(),
  guildId: z.string().nullable(),
})

export type Event = z.infer<typeof EventSchema>

export default EventSchema;

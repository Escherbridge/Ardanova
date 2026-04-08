import { z } from 'zod';
import { AttendeeStatusSchema } from '../inputTypeSchemas/AttendeeStatusSchema'

/////////////////////////////////////////
// EVENT ATTENDEE SCHEMA
/////////////////////////////////////////

export const EventAttendeeSchema = z.object({
  status: AttendeeStatusSchema,
  id: z.string().cuid(),
  eventId: z.string(),
  userId: z.string(),
  rsvpAt: z.coerce.date(),
  attendedAt: z.coerce.date().nullable(),
  notes: z.string().nullable(),
})

export type EventAttendee = z.infer<typeof EventAttendeeSchema>

export default EventAttendeeSchema;

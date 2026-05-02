import { z } from 'zod';

/////////////////////////////////////////
// EVENT REMINDER SCHEMA
/////////////////////////////////////////

export const EventReminderSchema = z.object({
  id: z.string().cuid(),
  eventId: z.string(),
  userId: z.string(),
  remindAt: z.coerce.date(),
  isSent: z.boolean(),
  sentAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
})

export type EventReminder = z.infer<typeof EventReminderSchema>

export default EventReminderSchema;

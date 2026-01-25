import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { NotificationTypeSchema } from '../inputTypeSchemas/NotificationTypeSchema'

/////////////////////////////////////////
// NOTIFICATION SCHEMA
/////////////////////////////////////////

export const NotificationSchema = z.object({
  type: NotificationTypeSchema,
  id: z.string().cuid(),
  userId: z.string(),
  title: z.string(),
  message: z.string(),
  data: JsonValueSchema.nullable(),
  isRead: z.boolean(),
  readAt: z.coerce.date().nullable(),
  actionUrl: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type Notification = z.infer<typeof NotificationSchema>

export default NotificationSchema;

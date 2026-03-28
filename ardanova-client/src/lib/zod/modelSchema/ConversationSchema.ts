import { z } from 'zod';
import { ConversationTypeSchema } from '../inputTypeSchemas/ConversationTypeSchema'

/////////////////////////////////////////
// CONVERSATION SCHEMA
/////////////////////////////////////////

export const ConversationSchema = z.object({
  type: ConversationTypeSchema,
  id: z.string().cuid(),
  name: z.string().nullable(),
  avatarUrl: z.string().nullable(),
  createdById: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  lastMessageAt: z.coerce.date().nullable(),
})

export type Conversation = z.infer<typeof ConversationSchema>

export default ConversationSchema;

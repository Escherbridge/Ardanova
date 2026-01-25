import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { MessageStatusSchema } from '../inputTypeSchemas/MessageStatusSchema'

/////////////////////////////////////////
// CHAT MESSAGE SCHEMA
/////////////////////////////////////////

export const ChatMessageSchema = z.object({
  status: MessageStatusSchema,
  id: z.string().cuid(),
  userToId: z.string(),
  userFromId: z.string(),
  message: z.string().nullable(),
  ChatReaction: JsonValueSchema.nullable(),
  chatAttachmentId: z.string().nullable(),
  sentAt: z.coerce.date(),
  seenAt: z.coerce.date().nullable(),
})

export type ChatMessage = z.infer<typeof ChatMessageSchema>

export default ChatMessageSchema;

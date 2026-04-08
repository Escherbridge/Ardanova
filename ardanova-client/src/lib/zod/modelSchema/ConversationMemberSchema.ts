import { z } from 'zod';
import { ConversationRoleSchema } from '../inputTypeSchemas/ConversationRoleSchema'

/////////////////////////////////////////
// CONVERSATION MEMBER SCHEMA
/////////////////////////////////////////

export const ConversationMemberSchema = z.object({
  role: ConversationRoleSchema,
  id: z.string().cuid(),
  conversationId: z.string(),
  userId: z.string(),
  lastReadAt: z.coerce.date().nullable(),
  joinedAt: z.coerce.date(),
  lastActiveAt: z.coerce.date().nullable(),
})

export type ConversationMember = z.infer<typeof ConversationMemberSchema>

export default ConversationMemberSchema;

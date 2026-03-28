import { z } from 'zod';

export const ConversationRoleSchema = z.enum(['MEMBER','ADMIN','OWNER']);

export type ConversationRoleType = `${z.infer<typeof ConversationRoleSchema>}`

export default ConversationRoleSchema;

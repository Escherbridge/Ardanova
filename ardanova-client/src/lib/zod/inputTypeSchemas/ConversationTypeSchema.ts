import { z } from 'zod';

export const ConversationTypeSchema = z.enum(['DIRECT','GROUP']);

export type ConversationTypeType = `${z.infer<typeof ConversationTypeSchema>}`

export default ConversationTypeSchema;

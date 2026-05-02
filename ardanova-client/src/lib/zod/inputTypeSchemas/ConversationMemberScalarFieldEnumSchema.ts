import { z } from 'zod';

export const ConversationMemberScalarFieldEnumSchema = z.enum(['id','conversationId','userId','role','lastReadAt','joinedAt','lastActiveAt']);

export default ConversationMemberScalarFieldEnumSchema;

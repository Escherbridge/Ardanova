import { z } from 'zod';

export const ConversationScalarFieldEnumSchema = z.enum(['id','type','name','avatarUrl','createdById','createdAt','updatedAt','lastMessageAt']);

export default ConversationScalarFieldEnumSchema;

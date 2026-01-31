import { z } from 'zod';

export const ChatMessageScalarFieldEnumSchema = z.enum(['id','userToId','userFromId','message','status','ChatReaction','chatAttachmentId','sentAt','seenAt','conversationId','replyToId','deliveredAt','isDeleted','editedAt']);

export default ChatMessageScalarFieldEnumSchema;

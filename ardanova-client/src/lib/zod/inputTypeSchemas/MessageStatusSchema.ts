import { z } from 'zod';

export const MessageStatusSchema = z.enum(['SENT','DELIVERED','READ','FAILED','NOTSEEN']);

export type MessageStatusType = `${z.infer<typeof MessageStatusSchema>}`

export default MessageStatusSchema;

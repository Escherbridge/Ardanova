import { z } from 'zod';

export const EscrowStatusSchema = z.enum(['NONE','FUNDED','RELEASED','DISPUTED','REFUNDED']);

export type EscrowStatusType = `${z.infer<typeof EscrowStatusSchema>}`

export default EscrowStatusSchema;

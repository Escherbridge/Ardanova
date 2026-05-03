import { z } from 'zod';

export const PayoutStatusSchema = z.enum(['PENDING','PROCESSING','COMPLETED','FAILED','CANCELLED']);

export type PayoutStatusType = `${z.infer<typeof PayoutStatusSchema>}`

export default PayoutStatusSchema;

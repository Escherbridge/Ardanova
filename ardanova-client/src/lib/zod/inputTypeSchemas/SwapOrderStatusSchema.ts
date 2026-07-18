import { z } from 'zod';

export const SwapOrderStatusSchema = z.enum(['DRAFT','AUTHORIZED','PENDING_SETTLEMENT','CONFIRMED','REJECTED','CANCELLED','FAILED']);

export type SwapOrderStatusType = `${z.infer<typeof SwapOrderStatusSchema>}`

export default SwapOrderStatusSchema;

import { z } from 'zod';

export const SwapStatusSchema = z.enum(['PENDING','PROCESSING','COMPLETED','FAILED','CANCELLED']);

export type SwapStatusType = `${z.infer<typeof SwapStatusSchema>}`

export default SwapStatusSchema;

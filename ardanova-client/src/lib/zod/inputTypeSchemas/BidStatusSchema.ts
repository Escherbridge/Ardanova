import { z } from 'zod';

export const BidStatusSchema = z.enum(['SUBMITTED','UNDER_REVIEW','ACCEPTED','REJECTED','WITHDRAWN','COMPLETED']);

export type BidStatusType = `${z.infer<typeof BidStatusSchema>}`

export default BidStatusSchema;

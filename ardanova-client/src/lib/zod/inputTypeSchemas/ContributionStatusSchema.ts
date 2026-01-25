import { z } from 'zod';

export const ContributionStatusSchema = z.enum(['PENDING','CONFIRMED','REFUNDED','FAILED']);

export type ContributionStatusType = `${z.infer<typeof ContributionStatusSchema>}`

export default ContributionStatusSchema;

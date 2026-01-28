import { z } from 'zod';

export const CompensationModelSchema = z.enum(['FIXED_SHARES','HOURLY_SHARES','EQUITY_PERCENT','HYBRID','BOUNTY','MILESTONE']);

export type CompensationModelType = `${z.infer<typeof CompensationModelSchema>}`

export default CompensationModelSchema;

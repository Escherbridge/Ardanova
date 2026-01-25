import { z } from 'zod';

export const CompensationModelSchema = z.enum(['FIXED_TOKEN','HOURLY_TOKEN','EQUITY_PERCENT','HYBRID','BOUNTY','MILESTONE']);

export type CompensationModelType = `${z.infer<typeof CompensationModelSchema>}`

export default CompensationModelSchema;

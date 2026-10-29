import { z } from 'zod';

export const FundingIntentVoidScalarFieldEnumSchema = z.enum(['id','funderUserId','idempotencyKey','createdAt']);

export default FundingIntentVoidScalarFieldEnumSchema;

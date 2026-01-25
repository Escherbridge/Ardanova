import { z } from 'zod';

export const TreasuryScalarFieldEnumSchema = z.enum(['id','projectId','balance','tokenAssetId','createdAt','updatedAt']);

export default TreasuryScalarFieldEnumSchema;

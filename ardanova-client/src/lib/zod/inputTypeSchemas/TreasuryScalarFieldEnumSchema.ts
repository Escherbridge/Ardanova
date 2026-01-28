import { z } from 'zod';

export const TreasuryScalarFieldEnumSchema = z.enum(['id','projectId','balance','shareAssetId','createdAt','updatedAt']);

export default TreasuryScalarFieldEnumSchema;

import { z } from 'zod';

export const DelegatedVoteScalarFieldEnumSchema = z.enum(['id','projectId','delegatorId','delegateeId','tokenId','amount','isActive','createdAt','expiresAt','revokedAt']);

export default DelegatedVoteScalarFieldEnumSchema;

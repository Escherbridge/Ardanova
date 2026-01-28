import { z } from 'zod';

export const DelegatedVoteScalarFieldEnumSchema = z.enum(['id','projectId','delegatorId','delegateeId','shareId','amount','isActive','createdAt','expiresAt','revokedAt']);

export default DelegatedVoteScalarFieldEnumSchema;

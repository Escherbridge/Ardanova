import { z } from 'zod';

export const VoteScalarFieldEnumSchema = z.enum(['id','proposalId','voterId','choice','weight','reason','txHash','createdAt']);

export default VoteScalarFieldEnumSchema;

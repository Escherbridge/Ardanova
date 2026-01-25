import { z } from 'zod';

export const TreasuryTransactionScalarFieldEnumSchema = z.enum(['id','treasuryId','type','amount','description','txHash','proposalId','createdAt']);

export default TreasuryTransactionScalarFieldEnumSchema;

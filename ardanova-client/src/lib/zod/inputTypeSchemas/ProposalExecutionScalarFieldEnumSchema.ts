import { z } from 'zod';

export const ProposalExecutionScalarFieldEnumSchema = z.enum(['id','proposalId','executedAt','txHash','result']);

export default ProposalExecutionScalarFieldEnumSchema;

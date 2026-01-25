import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'

/////////////////////////////////////////
// PROPOSAL EXECUTION SCHEMA
/////////////////////////////////////////

export const ProposalExecutionSchema = z.object({
  id: z.string().cuid(),
  proposalId: z.string(),
  executedAt: z.coerce.date(),
  txHash: z.string().nullable(),
  result: JsonValueSchema.nullable(),
})

export type ProposalExecution = z.infer<typeof ProposalExecutionSchema>

export default ProposalExecutionSchema;

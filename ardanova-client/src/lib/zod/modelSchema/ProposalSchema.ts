import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { ProposalTypeSchema } from '../inputTypeSchemas/ProposalTypeSchema'
import { ProposalStatusSchema } from '../inputTypeSchemas/ProposalStatusSchema'

/////////////////////////////////////////
// PROPOSAL SCHEMA
/////////////////////////////////////////

export const ProposalSchema = z.object({
  type: ProposalTypeSchema,
  status: ProposalStatusSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  creatorId: z.string(),
  title: z.string(),
  description: z.string(),
  options: JsonValueSchema,
  quorum: z.number().int(),
  threshold: z.number().int(),
  votingStart: z.coerce.date().nullable(),
  votingEnd: z.coerce.date().nullable(),
  executionDelay: z.number().int().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type Proposal = z.infer<typeof ProposalSchema>

export default ProposalSchema;

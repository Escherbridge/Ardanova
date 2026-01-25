import { z } from 'zod';

export const ProposalStatusSchema = z.enum(['DRAFT','ACTIVE','PASSED','REJECTED','EXECUTED','CANCELLED','EXPIRED']);

export type ProposalStatusType = `${z.infer<typeof ProposalStatusSchema>}`

export default ProposalStatusSchema;

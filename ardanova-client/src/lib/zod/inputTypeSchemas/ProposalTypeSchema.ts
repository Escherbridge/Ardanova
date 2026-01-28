import { z } from 'zod';

export const ProposalTypeSchema = z.enum(['TREASURY','GOVERNANCE','STRATEGIC','OPERATIONAL','EMERGENCY','CONSTITUTIONAL','SHARES']);

export type ProposalTypeType = `${z.infer<typeof ProposalTypeSchema>}`

export default ProposalTypeSchema;

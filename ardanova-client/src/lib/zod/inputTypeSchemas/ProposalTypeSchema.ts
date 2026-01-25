import { z } from 'zod';

export const ProposalTypeSchema = z.enum(['TREASURY','GOVERNANCE','STRATEGIC','OPERATIONAL','EMERGENCY','CONSTITUTIONAL','TOKEN']);

export type ProposalTypeType = `${z.infer<typeof ProposalTypeSchema>}`

export default ProposalTypeSchema;

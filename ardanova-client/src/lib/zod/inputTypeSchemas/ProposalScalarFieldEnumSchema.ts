import { z } from 'zod';

export const ProposalScalarFieldEnumSchema = z.enum(['id','projectId','creatorId','type','title','description','options','quorum','threshold','status','votingStart','votingEnd','executionDelay','createdAt','updatedAt']);

export default ProposalScalarFieldEnumSchema;

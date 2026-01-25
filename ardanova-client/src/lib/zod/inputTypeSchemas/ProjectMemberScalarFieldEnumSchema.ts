import { z } from 'zod';

export const ProjectMemberScalarFieldEnumSchema = z.enum(['id','projectId','userId','role','tokenBalance','votingPower','joinedAt','invitedById']);

export default ProjectMemberScalarFieldEnumSchema;

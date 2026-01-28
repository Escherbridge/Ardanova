import { z } from 'zod';

export const ProjectMemberScalarFieldEnumSchema = z.enum(['id','projectId','userId','role','shareBalance','votingPower','joinedAt','invitedById']);

export default ProjectMemberScalarFieldEnumSchema;

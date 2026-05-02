import { z } from 'zod';

export const ProjectFollowScalarFieldEnumSchema = z.enum(['id','userId','projectId','notifyUpdates','notifyMilestones','notifyProposals','createdAt']);

export default ProjectFollowScalarFieldEnumSchema;

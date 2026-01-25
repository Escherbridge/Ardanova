import { z } from 'zod';

export const ProjectBidScalarFieldEnumSchema = z.enum(['id','projectId','guildId','userId','proposal','timeline','budget','deliverables','status','submittedAt','reviewedAt']);

export default ProjectBidScalarFieldEnumSchema;

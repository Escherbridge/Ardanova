import { z } from 'zod';

export const ProjectGateStatusSchema = z.enum(['FUNDING','ACTIVE','SUCCEEDED','FAILED']);

export type ProjectGateStatusType = `${z.infer<typeof ProjectGateStatusSchema>}`

export default ProjectGateStatusSchema;

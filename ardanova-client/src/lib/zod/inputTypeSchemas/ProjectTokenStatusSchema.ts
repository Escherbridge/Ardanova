import { z } from 'zod';

export const ProjectTokenStatusSchema = z.enum(['PENDING', 'ACTIVE', 'FROZEN', 'DISSOLVED']);

export type ProjectTokenStatusType = `${z.infer<typeof ProjectTokenStatusSchema>}`;

export default ProjectTokenStatusSchema;

import { z } from 'zod';

export const ProjectStatusSchema = z.enum(['DRAFT','PUBLISHED','SEEKING_SUPPORT','FUNDED','IN_PROGRESS','COMPLETED','CANCELLED']);

export type ProjectStatusType = `${z.infer<typeof ProjectStatusSchema>}`

export default ProjectStatusSchema;

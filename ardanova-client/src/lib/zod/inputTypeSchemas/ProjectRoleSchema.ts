import { z } from 'zod';

export const ProjectRoleSchema = z.enum(['FOUNDER','LEADER','CORE_CONTRIBUTOR','CONTRIBUTOR','OBSERVER']);

export type ProjectRoleType = `${z.infer<typeof ProjectRoleSchema>}`

export default ProjectRoleSchema;

import { z } from 'zod';

export const ProjectTypeSchema = z.enum(['TEMPORARY','LONG_TERM','FOUNDATION','BUSINESS','PRODUCT','OPEN_SOURCE','COMMUNITY']);

export type ProjectTypeType = `${z.infer<typeof ProjectTypeSchema>}`

export default ProjectTypeSchema;

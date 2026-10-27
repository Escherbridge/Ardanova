import { z } from 'zod';

export const ProjectTypeSchema = z.enum(['TEMPORARY','LONG_TERM','FOUNDATION','BUSINESS','PRODUCT','OPEN_SOURCE','COMMUNITY','SERVICE','PHYSICAL_BUILD','VENUE','PROGRAM','RESEARCH','COOPERATIVE']);

export type ProjectTypeType = `${z.infer<typeof ProjectTypeSchema>}`

export default ProjectTypeSchema;

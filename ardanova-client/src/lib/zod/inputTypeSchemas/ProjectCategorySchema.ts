import { z } from 'zod';

export const ProjectCategorySchema = z.enum(['TECHNOLOGY','HEALTHCARE','EDUCATION','ENVIRONMENT','SOCIAL_IMPACT','BUSINESS','ARTS_CULTURE','AGRICULTURE','FINANCE','OTHER']);

export type ProjectCategoryType = `${z.infer<typeof ProjectCategorySchema>}`

export default ProjectCategorySchema;

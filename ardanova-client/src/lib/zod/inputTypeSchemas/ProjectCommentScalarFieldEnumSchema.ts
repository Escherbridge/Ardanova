import { z } from 'zod';

export const ProjectCommentScalarFieldEnumSchema = z.enum(['id','projectId','userId','content','parentId','createdAt','updatedAt']);

export default ProjectCommentScalarFieldEnumSchema;

import { z } from 'zod';

export const ProjectUpdateScalarFieldEnumSchema = z.enum(['id','projectId','userId','title','content','images','createdAt']);

export default ProjectUpdateScalarFieldEnumSchema;

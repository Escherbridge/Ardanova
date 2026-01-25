import { z } from 'zod';

export const ProjectSupportScalarFieldEnumSchema = z.enum(['id','projectId','userId','supportType','monthlyAmount','message','isActive','createdAt','updatedAt']);

export default ProjectSupportScalarFieldEnumSchema;

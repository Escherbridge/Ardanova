import { z } from 'zod';

export const ProjectApplicationScalarFieldEnumSchema = z.enum(['id','projectId','userId','roleTitle','message','skills','experience','availability','status','appliedAt','reviewedAt','reviewMessage']);

export default ProjectApplicationScalarFieldEnumSchema;

import { z } from 'zod';

export const ProjectResourceScalarFieldEnumSchema = z.enum(['id','projectId','name','description','quantity','estimatedCost','isRequired','isObtained','createdAt']);

export default ProjectResourceScalarFieldEnumSchema;

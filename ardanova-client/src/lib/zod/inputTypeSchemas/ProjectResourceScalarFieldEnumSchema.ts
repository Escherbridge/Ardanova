import { z } from 'zod';

export const ProjectResourceScalarFieldEnumSchema = z.enum(['id','projectId','name','description','quantity','estimatedCost','recurringCost','recurringIntervalDays','isRequired','isObtained','createdAt']);

export default ProjectResourceScalarFieldEnumSchema;

import { z } from 'zod';

export const ProjectTokenPolicyScalarFieldEnumSchema = z.enum(['id','projectId','assetDefinitionId','version','termsHash','allocationRules','effectiveFrom','retiredAt','createdAt']);

export default ProjectTokenPolicyScalarFieldEnumSchema;

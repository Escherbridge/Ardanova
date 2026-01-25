import { z } from 'zod';

export const ProjectTokenScalarFieldEnumSchema = z.enum(['id','projectId','assetId','name','symbol','totalSupply','decimals','allocation','vestingConfig','isDeployed','deployedAt','createdAt','updatedAt']);

export default ProjectTokenScalarFieldEnumSchema;

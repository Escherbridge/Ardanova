import { z } from 'zod';

export const ProjectShareScalarFieldEnumSchema = z.enum(['id','projectId','assetId','name','symbol','totalSupply','decimals','allocation','vestingConfig','isDeployed','deployedAt','createdAt','updatedAt']);

export default ProjectShareScalarFieldEnumSchema;

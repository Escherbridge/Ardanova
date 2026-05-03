import { z } from 'zod';

export const ProjectWalletScalarFieldEnumSchema = z.enum(['id','projectId','address','provider','label','balance','reservedBalance','isVerified','createdAt','updatedAt']);

export default ProjectWalletScalarFieldEnumSchema;

import { z } from 'zod';

export const ProjectMembershipRequestScalarFieldEnumSchema = z.enum(['id','projectId','userId','requestedRole','message','skills','motivation','portfolio','status','reviewedById','reviewMessage','createdAt','reviewedAt']);

export default ProjectMembershipRequestScalarFieldEnumSchema;

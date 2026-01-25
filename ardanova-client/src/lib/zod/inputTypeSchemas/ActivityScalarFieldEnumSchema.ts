import { z } from 'zod';

export const ActivityScalarFieldEnumSchema = z.enum(['id','userId','projectId','type','entityType','entityId','action','metadata','createdAt']);

export default ActivityScalarFieldEnumSchema;

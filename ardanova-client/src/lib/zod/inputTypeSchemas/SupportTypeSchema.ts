import { z } from 'zod';

export const SupportTypeSchema = z.enum(['VOTE','SUBSCRIPTION','VOLUNTEER','RESOURCE']);

export type SupportTypeType = `${z.infer<typeof SupportTypeSchema>}`

export default SupportTypeSchema;

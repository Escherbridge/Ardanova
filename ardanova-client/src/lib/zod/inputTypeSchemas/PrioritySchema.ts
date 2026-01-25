import { z } from 'zod';

export const PrioritySchema = z.enum(['LOW','MEDIUM','HIGH','CRITICAL']);

export type PriorityType = `${z.infer<typeof PrioritySchema>}`

export default PrioritySchema;

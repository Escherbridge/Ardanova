import { z } from 'zod';

export const ExperienceLevelSchema = z.enum(['ENTRY','JUNIOR','MID','SENIOR','LEAD','EXPERT']);

export type ExperienceLevelType = `${z.infer<typeof ExperienceLevelSchema>}`

export default ExperienceLevelSchema;

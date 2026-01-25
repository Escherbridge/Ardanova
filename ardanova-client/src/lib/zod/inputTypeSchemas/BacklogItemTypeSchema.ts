import { z } from 'zod';

export const BacklogItemTypeSchema = z.enum(['TASK','BUG','IMPROVEMENT','RESEARCH']);

export type BacklogItemTypeType = `${z.infer<typeof BacklogItemTypeSchema>}`

export default BacklogItemTypeSchema;

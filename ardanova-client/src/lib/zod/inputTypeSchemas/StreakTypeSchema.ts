import { z } from 'zod';

export const StreakTypeSchema = z.enum(['DAILY_LOGIN','DAILY_CONTRIBUTION','WEEKLY_TASK']);

export type StreakTypeType = `${z.infer<typeof StreakTypeSchema>}`

export default StreakTypeSchema;

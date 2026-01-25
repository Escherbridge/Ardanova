import { z } from 'zod';

export const LeaderboardPeriodSchema = z.enum(['DAILY','WEEKLY','MONTHLY','QUARTERLY','ALL_TIME']);

export type LeaderboardPeriodType = `${z.infer<typeof LeaderboardPeriodSchema>}`

export default LeaderboardPeriodSchema;

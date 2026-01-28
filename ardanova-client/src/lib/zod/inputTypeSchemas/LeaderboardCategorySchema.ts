import { z } from 'zod';

export const LeaderboardCategorySchema = z.enum(['XP','TASKS_COMPLETED','SHARES_EARNED','PROPOSALS_CREATED','VOTES_CAST','PROJECTS_FUNDED']);

export type LeaderboardCategoryType = `${z.infer<typeof LeaderboardCategorySchema>}`

export default LeaderboardCategorySchema;

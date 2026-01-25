import { z } from 'zod';

export const AchievementCategorySchema = z.enum(['CONTRIBUTOR','COLLABORATOR','INVESTOR','GOVERNANCE','COMMUNITY','STREAK','MILESTONE','GAMING']);

export type AchievementCategoryType = `${z.infer<typeof AchievementCategorySchema>}`

export default AchievementCategorySchema;

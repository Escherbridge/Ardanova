import { z } from 'zod';

export const AchievementRaritySchema = z.enum(['COMMON','UNCOMMON','RARE','EPIC','LEGENDARY']);

export type AchievementRarityType = `${z.infer<typeof AchievementRaritySchema>}`

export default AchievementRaritySchema;

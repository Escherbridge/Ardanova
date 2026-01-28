import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { Prisma } from '@prisma/client'
import { AchievementCategorySchema } from '../inputTypeSchemas/AchievementCategorySchema'
import { AchievementRaritySchema } from '../inputTypeSchemas/AchievementRaritySchema'

/////////////////////////////////////////
// ACHIEVEMENT SCHEMA
/////////////////////////////////////////

export const AchievementSchema = z.object({
  category: AchievementCategorySchema,
  rarity: AchievementRaritySchema,
  id: z.string().cuid(),
  name: z.string(),
  description: z.string(),
  criteria: JsonValueSchema,
  xpReward: z.number().int(),
  equityReward: z.instanceof(Prisma.Decimal, { message: "Field 'equityReward' must be a Decimal. Location: ['Models', 'Achievement']"}).nullable(),
  icon: z.string().nullable(),
  isActive: z.boolean(),
  createdAt: z.coerce.date(),
})

export type Achievement = z.infer<typeof AchievementSchema>

export default AchievementSchema;

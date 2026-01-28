import { z } from 'zod';

export const AchievementScalarFieldEnumSchema = z.enum(['id','name','description','category','criteria','xpReward','equityReward','rarity','icon','isActive','createdAt']);

export default AchievementScalarFieldEnumSchema;

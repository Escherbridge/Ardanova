import { z } from 'zod';

export const AchievementScalarFieldEnumSchema = z.enum(['id','name','description','category','criteria','xpReward','tokenReward','rarity','icon','isActive','createdAt']);

export default AchievementScalarFieldEnumSchema;

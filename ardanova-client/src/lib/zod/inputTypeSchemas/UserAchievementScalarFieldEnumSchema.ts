import { z } from 'zod';

export const UserAchievementScalarFieldEnumSchema = z.enum(['id','userId','achievementId','progress','earnedAt']);

export default UserAchievementScalarFieldEnumSchema;

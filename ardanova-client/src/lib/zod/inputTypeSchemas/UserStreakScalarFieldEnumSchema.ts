import { z } from 'zod';

export const UserStreakScalarFieldEnumSchema = z.enum(['id','userId','currentStreak','longestStreak','lastActivityDate','streakType','createdAt','updatedAt']);

export default UserStreakScalarFieldEnumSchema;

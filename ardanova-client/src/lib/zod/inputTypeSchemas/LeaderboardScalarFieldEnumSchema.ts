import { z } from 'zod';

export const LeaderboardScalarFieldEnumSchema = z.enum(['id','period','category','startDate','endDate','createdAt']);

export default LeaderboardScalarFieldEnumSchema;

import { z } from 'zod';

export const LeaderboardEntryScalarFieldEnumSchema = z.enum(['id','leaderboardId','userId','rank','score','metadata']);

export default LeaderboardEntryScalarFieldEnumSchema;

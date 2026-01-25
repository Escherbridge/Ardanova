import { z } from 'zod';

export const ReferralScalarFieldEnumSchema = z.enum(['id','referrerId','referredId','referralCode','status','rewardClaimed','xpRewarded','tokenRewarded','createdAt','completedAt']);

export default ReferralScalarFieldEnumSchema;

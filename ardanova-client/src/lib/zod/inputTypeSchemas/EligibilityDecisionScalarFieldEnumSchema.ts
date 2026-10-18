import { z } from 'zod';

export const EligibilityDecisionScalarFieldEnumSchema = z.enum(['id','userId','equityOrRedemptionRightPolicyId','status','evidenceDigest','reasonCode','decidedByUserId','expiresAt','reviewedAt','createdAt']);

export default EligibilityDecisionScalarFieldEnumSchema;

import { z } from 'zod';

export const FundingIntentScalarFieldEnumSchema = z.enum(['id','semanticKey','idempotencyKey','status','funderUserId','projectId','projectTokenConfigId','assetDefinitionId','projectTokenPolicyId','equityOrRedemptionRightPolicyId','eligibilityDecisionId','currencyCode','amount','scale','disclosureVersion','eligibilitySnapshot','termsSnapshot','termsHash','paymentProvider','providerCheckoutSessionId','providerPaymentIntentId','verifiedProviderEventId','settlementId','expiresAt','paymentVerifiedAt','settledAt','cancelledAt','createdAt','updatedAt']);

export default FundingIntentScalarFieldEnumSchema;

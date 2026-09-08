import { z } from 'zod';

export const FundingIntentScalarFieldEnumSchema = z.enum(['id','semanticKey','idempotencyKey','status','funderUserId','projectId','projectTokenConfigId','disclosureVersion','platformAssetCode','platformAmountBaseUnits','platformScale','requestedPlatformBaseUnits','projectUnitAssetCode','projectUnitQuantity','projectUnitScale','projectAssetId','usdReferenceMinorUnits','declaredRateArdaPerUnit','declaredRateUsdPerUnit','declaredRateSource','declaredRateDeclaredAt','declaredRateVersion','eligibilitySnapshot','termsSnapshot','termsHash','exchangeIdempotencyKey','azoaOperationId','holdReference','releaseNotBefore','settlementId','settlementAttemptCount','holdingReasonCode','holdingReasonDetail','heldAt','expiresAt','settledAt','cancelledAt','createdAt','updatedAt']);

export default FundingIntentScalarFieldEnumSchema;

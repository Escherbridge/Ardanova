import { z } from 'zod';

export const SwapQuoteScalarFieldEnumSchema = z.enum(['id','semanticKey','actorUserId','sourceAssetDefinitionId','ardaAssetDefinitionId','targetAssetDefinitionId','sourceProjectTokenPolicyId','targetProjectTokenPolicyId','sourceAmountAtoms','ardaAmountAtoms','targetAmountAtoms','liquiditySnapshot','gateDecisionSnapshot','termsHash','expiresAt','createdAt']);

export default SwapQuoteScalarFieldEnumSchema;

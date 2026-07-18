import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'

/////////////////////////////////////////
// SWAP QUOTE SCHEMA
/////////////////////////////////////////

export const SwapQuoteSchema = z.object({
  id: z.string().cuid(),
  semanticKey: z.string(),
  actorUserId: z.string(),
  sourceAssetDefinitionId: z.string(),
  ardaAssetDefinitionId: z.string(),
  targetAssetDefinitionId: z.string(),
  sourceProjectTokenPolicyId: z.string(),
  targetProjectTokenPolicyId: z.string(),
  sourceAmountAtoms: z.string(),
  ardaAmountAtoms: z.string(),
  targetAmountAtoms: z.string(),
  liquiditySnapshot: JsonValueSchema,
  gateDecisionSnapshot: JsonValueSchema,
  termsHash: z.string(),
  expiresAt: z.coerce.date(),
  createdAt: z.coerce.date(),
})

export type SwapQuote = z.infer<typeof SwapQuoteSchema>

export default SwapQuoteSchema;

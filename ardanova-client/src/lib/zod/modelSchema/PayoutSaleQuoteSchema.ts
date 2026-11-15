import { z } from 'zod';
import { TokenHolderClassSchema } from '../inputTypeSchemas/TokenHolderClassSchema'

/////////////////////////////////////////
// PAYOUT SALE QUOTE SCHEMA
/////////////////////////////////////////

export const PayoutSaleQuoteSchema = z.object({
  holderClass: TokenHolderClassSchema,
  id: z.string(),
  currencyCode: z.string(),
  currencyDecimals: z.number().int(),
  userId: z.string(),
  projectTokenConfigId: z.string(),
  sourceUnitAmount: z.number().int(),
  platformMinorUnits: z.bigint(),
  usdReferenceMinorUnits: z.bigint(),
  commandJson: z.string(),
  createdAt: z.coerce.date(),
  expiresAt: z.coerce.date(),
})

export type PayoutSaleQuote = z.infer<typeof PayoutSaleQuoteSchema>

export default PayoutSaleQuoteSchema;

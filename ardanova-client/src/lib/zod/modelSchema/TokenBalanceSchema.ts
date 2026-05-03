import { z } from 'zod';
import { TokenHolderClassSchema } from '../inputTypeSchemas/TokenHolderClassSchema'

/////////////////////////////////////////
// TOKEN BALANCE SCHEMA
/////////////////////////////////////////

export const TokenBalanceSchema = z.object({
  holderClass: TokenHolderClassSchema.nullable(),
  id: z.string().cuid(),
  userId: z.string(),
  projectTokenConfigId: z.string().nullable(),
  isPlatformToken: z.boolean(),
  isLiquid: z.boolean(),
  balance: z.number().int(),
  lockedBalance: z.number().int(),
  updatedAt: z.coerce.date(),
})

export type TokenBalance = z.infer<typeof TokenBalanceSchema>

export default TokenBalanceSchema;

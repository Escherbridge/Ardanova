import { z } from 'zod';
import { TokenHolderClassSchema } from '../inputTypeSchemas/TokenHolderClassSchema';

/////////////////////////////////////////
// TOKEN BALANCE SCHEMA
/////////////////////////////////////////

export const TokenBalanceSchema = z.object({
  id: z.string().cuid(),
  userId: z.string(),
  projectTokenConfigId: z.string().nullable(),
  isPlatformToken: z.boolean(),
  holderClass: TokenHolderClassSchema.nullable(),
  isLiquid: z.boolean(),
  balance: z.number().int(),
  lockedBalance: z.number().int(),
  updatedAt: z.coerce.date(),
});

export type TokenBalance = z.infer<typeof TokenBalanceSchema>;

export default TokenBalanceSchema;

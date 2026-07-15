import { z } from 'zod';
import { WalletProviderSchema } from '../inputTypeSchemas/WalletProviderSchema'

/////////////////////////////////////////
// WALLET SCHEMA
/////////////////////////////////////////

export const WalletSchema = z.object({
  provider: WalletProviderSchema,
  id: z.string().cuid(),
  userId: z.string(),
  address: z.string(),
  label: z.string().nullable(),
  isVerified: z.boolean(),
  verifiedAt: z.coerce.date().nullable(),
  verificationChain: z.string().nullable(),
  verificationNetwork: z.string().nullable(),
  verificationChallengeId: z.string().nullable(),
  isPrimary: z.boolean(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type Wallet = z.infer<typeof WalletSchema>

export default WalletSchema;

import { z } from 'zod';

/////////////////////////////////////////
// WALLET VERIFICATION CHALLENGE SCHEMA
/////////////////////////////////////////

export const WalletVerificationChallengeSchema = z.object({
  id: z.string().cuid(),
  userId: z.string(),
  walletId: z.string(),
  address: z.string(),
  chain: z.string(),
  network: z.string(),
  nonceHash: z.string(),
  issuedAt: z.coerce.date(),
  expiresAt: z.coerce.date(),
  consumedAt: z.coerce.date().nullable(),
  proofVerified: z.boolean().nullable(),
  signatureHash: z.string().nullable(),
  failureCode: z.string().nullable(),
})

export type WalletVerificationChallenge = z.infer<typeof WalletVerificationChallengeSchema>

export default WalletVerificationChallengeSchema;

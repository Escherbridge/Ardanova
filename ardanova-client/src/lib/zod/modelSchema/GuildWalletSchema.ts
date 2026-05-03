import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { WalletProviderSchema } from '../inputTypeSchemas/WalletProviderSchema'

/////////////////////////////////////////
// GUILD WALLET SCHEMA
/////////////////////////////////////////

export const GuildWalletSchema = z.object({
  provider: WalletProviderSchema,
  id: z.string().cuid(),
  guildId: z.string(),
  address: z.string().nullable(),
  label: z.string().nullable(),
  balance: z.instanceof(Prisma.Decimal, { message: "Field 'balance' must be a Decimal. Location: ['Models', 'GuildWallet']"}),
  reservedBalance: z.instanceof(Prisma.Decimal, { message: "Field 'reservedBalance' must be a Decimal. Location: ['Models', 'GuildWallet']"}),
  isVerified: z.boolean(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type GuildWallet = z.infer<typeof GuildWalletSchema>

export default GuildWalletSchema;

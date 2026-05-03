import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { WalletProviderSchema } from '../inputTypeSchemas/WalletProviderSchema'

/////////////////////////////////////////
// PROJECT WALLET SCHEMA
/////////////////////////////////////////

export const ProjectWalletSchema = z.object({
  provider: WalletProviderSchema,
  id: z.string().cuid(),
  projectId: z.string(),
  address: z.string().nullable(),
  label: z.string().nullable(),
  balance: z.instanceof(Prisma.Decimal, { message: "Field 'balance' must be a Decimal. Location: ['Models', 'ProjectWallet']"}),
  reservedBalance: z.instanceof(Prisma.Decimal, { message: "Field 'reservedBalance' must be a Decimal. Location: ['Models', 'ProjectWallet']"}),
  isVerified: z.boolean(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type ProjectWallet = z.infer<typeof ProjectWalletSchema>

export default ProjectWalletSchema;

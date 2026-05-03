import { z } from 'zod';
import { MembershipCredentialStatusSchema } from '../inputTypeSchemas/MembershipCredentialStatusSchema'
import { UserTierSchema } from '../inputTypeSchemas/UserTierSchema'
import { MembershipGrantTypeSchema } from '../inputTypeSchemas/MembershipGrantTypeSchema'

/////////////////////////////////////////
// MEMBERSHIP CREDENTIAL SCHEMA
/////////////////////////////////////////

export const MembershipCredentialSchema = z.object({
  status: MembershipCredentialStatusSchema,
  tier: UserTierSchema.nullable(),
  grantedVia: MembershipGrantTypeSchema,
  id: z.string().cuid(),
  projectId: z.string().nullable(),
  guildId: z.string().nullable(),
  userId: z.string(),
  assetId: z.string().nullable(),
  isTransferable: z.boolean(),
  grantedByProposalId: z.string().nullable(),
  metadataUri: z.string().nullable(),
  mintTxHash: z.string().nullable(),
  revokeTxHash: z.string().nullable(),
  mintedAt: z.coerce.date().nullable(),
  revokedAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type MembershipCredential = z.infer<typeof MembershipCredentialSchema>

export default MembershipCredentialSchema;

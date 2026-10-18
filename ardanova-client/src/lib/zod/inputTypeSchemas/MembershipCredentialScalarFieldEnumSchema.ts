import { z } from 'zod';

export const MembershipCredentialScalarFieldEnumSchema = z.enum(['id','projectId','guildId','userId','assetId','status','isTransferable','tier','grantedVia','grantedByProposalId','metadataUri','mintTxHash','issuedThroughNodeAt','revokeTxHash','mintedAt','revokedAt','createdAt','updatedAt']);

export default MembershipCredentialScalarFieldEnumSchema;

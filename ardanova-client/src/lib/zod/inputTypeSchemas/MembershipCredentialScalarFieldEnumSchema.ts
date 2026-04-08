import { z } from 'zod';

export const MembershipCredentialScalarFieldEnumSchema = z.enum(['id','projectId','userId','assetId','status','isTransferable','grantedVia','grantedByProposalId','mintTxHash','revokeTxHash','mintedAt','revokedAt','createdAt','updatedAt']);

export default MembershipCredentialScalarFieldEnumSchema;

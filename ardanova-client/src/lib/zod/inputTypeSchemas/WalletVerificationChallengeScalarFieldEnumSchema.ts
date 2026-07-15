import { z } from 'zod';

export const WalletVerificationChallengeScalarFieldEnumSchema = z.enum(['id','userId','walletId','address','chain','network','nonceHash','issuedAt','expiresAt','consumedAt','proofVerified','signatureHash','failureCode']);

export default WalletVerificationChallengeScalarFieldEnumSchema;

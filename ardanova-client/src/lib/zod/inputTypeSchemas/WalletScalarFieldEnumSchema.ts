import { z } from 'zod';

export const WalletScalarFieldEnumSchema = z.enum(['id','userId','address','provider','label','isVerified','verifiedAt','verificationChain','verificationNetwork','verificationChallengeId','isPrimary','createdAt','updatedAt']);

export default WalletScalarFieldEnumSchema;

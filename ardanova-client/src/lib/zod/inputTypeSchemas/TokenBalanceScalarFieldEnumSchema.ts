import { z } from 'zod';

export const TokenBalanceScalarFieldEnumSchema = z.enum(['id','userId','projectTokenConfigId','isPlatformToken','holderClass','isLiquid','balance','lockedBalance','assetId','walletAddress','observedAt','updatedAt']);

export default TokenBalanceScalarFieldEnumSchema;

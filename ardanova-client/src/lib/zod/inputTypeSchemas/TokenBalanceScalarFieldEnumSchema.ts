import { z } from 'zod';

export const TokenBalanceScalarFieldEnumSchema = z.enum([
  'id',
  'userId',
  'projectTokenConfigId',
  'isPlatformToken',
  'holderClass',
  'isLiquid',
  'balance',
  'lockedBalance',
  'updatedAt',
]);

export default TokenBalanceScalarFieldEnumSchema;

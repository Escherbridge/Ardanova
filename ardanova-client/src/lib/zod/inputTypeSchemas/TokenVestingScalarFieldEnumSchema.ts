import { z } from 'zod';

export const TokenVestingScalarFieldEnumSchema = z.enum(['id','holderId','totalAmount','releasedAmount','startDate','cliffEnd','vestingEnd','releaseFrequency','createdAt']);

export default TokenVestingScalarFieldEnumSchema;

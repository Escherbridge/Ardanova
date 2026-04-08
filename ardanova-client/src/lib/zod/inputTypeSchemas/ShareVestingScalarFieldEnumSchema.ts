import { z } from 'zod';

export const ShareVestingScalarFieldEnumSchema = z.enum(['id','holderId','totalAmount','releasedAmount','startDate','cliffEnd','vestingEnd','releaseFrequency','createdAt']);

export default ShareVestingScalarFieldEnumSchema;

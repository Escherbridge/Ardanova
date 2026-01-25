import { z } from 'zod';

export const VestingFrequencySchema = z.enum(['DAILY','WEEKLY','MONTHLY','QUARTERLY']);

export type VestingFrequencyType = `${z.infer<typeof VestingFrequencySchema>}`

export default VestingFrequencySchema;

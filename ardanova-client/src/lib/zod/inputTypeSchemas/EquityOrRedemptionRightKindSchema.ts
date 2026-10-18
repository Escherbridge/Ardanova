import { z } from 'zod';

export const EquityOrRedemptionRightKindSchema = z.enum(['EQUITY','REDEMPTION']);

export type EquityOrRedemptionRightKindType = `${z.infer<typeof EquityOrRedemptionRightKindSchema>}`

export default EquityOrRedemptionRightKindSchema;

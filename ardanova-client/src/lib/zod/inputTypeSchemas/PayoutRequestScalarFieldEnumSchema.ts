import { z } from 'zod';

export const PayoutRequestScalarFieldEnumSchema = z.enum(['id','currencyCode','currencyDecimals','userId','sourceProjectTokenConfigId','sourceTokenAmount','ardaTokenAmount','usdAmount','ardaMinorUnits','usdReferenceMinorUnits','status','holderClass','gateStatusAtRequest','conversionTxHash','payoutTxHash','stripePayoutId','failureReason','requestedAt','processedAt','completedAt','requestKey','quoteId','exchangeAdmitBefore','exchangeCommandJson']);

export default PayoutRequestScalarFieldEnumSchema;

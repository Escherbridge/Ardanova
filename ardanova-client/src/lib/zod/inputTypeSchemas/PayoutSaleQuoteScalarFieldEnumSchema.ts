import { z } from 'zod';

export const PayoutSaleQuoteScalarFieldEnumSchema = z.enum(['id','currencyCode','currencyDecimals','userId','projectTokenConfigId','holderClass','sourceUnitAmount','platformMinorUnits','usdReferenceMinorUnits','commandJson','createdAt','expiresAt']);

export default PayoutSaleQuoteScalarFieldEnumSchema;

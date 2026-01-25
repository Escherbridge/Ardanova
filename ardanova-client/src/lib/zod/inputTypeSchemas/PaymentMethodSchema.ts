import { z } from 'zod';

export const PaymentMethodSchema = z.enum(['BANK_TRANSFER','USSD','CARD','WALLET']);

export type PaymentMethodType = `${z.infer<typeof PaymentMethodSchema>}`

export default PaymentMethodSchema;

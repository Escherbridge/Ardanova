import { z } from 'zod';

export const SaleScalarFieldEnumSchema = z.enum(['id','shopId','buyerId','total','tax','discount','paymentMethod','notes','createdAt','userId']);

export default SaleScalarFieldEnumSchema;

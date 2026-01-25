import { z } from 'zod';

export const SaleItemScalarFieldEnumSchema = z.enum(['id','saleId','productId','quantity','price','total']);

export default SaleItemScalarFieldEnumSchema;

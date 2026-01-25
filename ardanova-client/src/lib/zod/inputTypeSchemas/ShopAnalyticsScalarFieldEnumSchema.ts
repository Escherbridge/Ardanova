import { z } from 'zod';

export const ShopAnalyticsScalarFieldEnumSchema = z.enum(['id','shopId','date','revenue','expenses','profit','salesCount','newCustomers','createdAt']);

export default ShopAnalyticsScalarFieldEnumSchema;

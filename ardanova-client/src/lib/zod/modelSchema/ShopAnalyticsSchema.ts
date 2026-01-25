import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// SHOP ANALYTICS SCHEMA
/////////////////////////////////////////

export const ShopAnalyticsSchema = z.object({
  id: z.string().cuid(),
  shopId: z.string(),
  date: z.coerce.date(),
  revenue: z.instanceof(Prisma.Decimal, { message: "Field 'revenue' must be a Decimal. Location: ['Models', 'ShopAnalytics']"}),
  expenses: z.instanceof(Prisma.Decimal, { message: "Field 'expenses' must be a Decimal. Location: ['Models', 'ShopAnalytics']"}),
  profit: z.instanceof(Prisma.Decimal, { message: "Field 'profit' must be a Decimal. Location: ['Models', 'ShopAnalytics']"}),
  salesCount: z.number().int(),
  newCustomers: z.number().int(),
  createdAt: z.coerce.date(),
})

export type ShopAnalytics = z.infer<typeof ShopAnalyticsSchema>

export default ShopAnalyticsSchema;

import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// SALE ITEM SCHEMA
/////////////////////////////////////////

export const SaleItemSchema = z.object({
  id: z.string().cuid(),
  saleId: z.string(),
  productId: z.string(),
  quantity: z.number().int(),
  price: z.instanceof(Prisma.Decimal, { message: "Field 'price' must be a Decimal. Location: ['Models', 'SaleItem']"}),
  total: z.instanceof(Prisma.Decimal, { message: "Field 'total' must be a Decimal. Location: ['Models', 'SaleItem']"}),
})

export type SaleItem = z.infer<typeof SaleItemSchema>

export default SaleItemSchema;

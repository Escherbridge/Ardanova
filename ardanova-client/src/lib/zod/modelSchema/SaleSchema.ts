import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { PaymentMethodSchema } from '../inputTypeSchemas/PaymentMethodSchema'

/////////////////////////////////////////
// SALE SCHEMA
/////////////////////////////////////////

export const SaleSchema = z.object({
  paymentMethod: PaymentMethodSchema,
  id: z.string().cuid(),
  shopId: z.string(),
  buyerId: z.string().nullable(),
  total: z.instanceof(Prisma.Decimal, { message: "Field 'total' must be a Decimal. Location: ['Models', 'Sale']"}),
  tax: z.instanceof(Prisma.Decimal, { message: "Field 'tax' must be a Decimal. Location: ['Models', 'Sale']"}).nullable(),
  discount: z.instanceof(Prisma.Decimal, { message: "Field 'discount' must be a Decimal. Location: ['Models', 'Sale']"}).nullable(),
  notes: z.string().nullable(),
  createdAt: z.coerce.date(),
  userId: z.string(),
})

export type Sale = z.infer<typeof SaleSchema>

export default SaleSchema;

import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// PRODUCT SCHEMA
/////////////////////////////////////////

export const ProductSchema = z.object({
  id: z.string().cuid(),
  projectId: z.string(),
  name: z.string(),
  description: z.string().nullable(),
  sku: z.string().nullable(),
  price: z.instanceof(Prisma.Decimal, { message: "Field 'price' must be a Decimal. Location: ['Models', 'Product']"}),
  cost: z.instanceof(Prisma.Decimal, { message: "Field 'cost' must be a Decimal. Location: ['Models', 'Product']"}).nullable(),
  category: z.string().nullable(),
  isActive: z.boolean(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  userId: z.string(),
})

export type Product = z.infer<typeof ProductSchema>

export default ProductSchema;

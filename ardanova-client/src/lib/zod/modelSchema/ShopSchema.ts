import { z } from 'zod';

/////////////////////////////////////////
// SHOP SCHEMA
/////////////////////////////////////////

export const ShopSchema = z.object({
  id: z.string().cuid(),
  name: z.string(),
  description: z.string().nullable(),
  address: z.string().nullable(),
  phone: z.string().nullable(),
  email: z.string().nullable(),
  website: z.string().nullable(),
  logo: z.string().nullable(),
  isActive: z.boolean(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  ownerId: z.string(),
})

export type Shop = z.infer<typeof ShopSchema>

export default ShopSchema;

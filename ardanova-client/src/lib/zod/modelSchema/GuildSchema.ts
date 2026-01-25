import { z } from 'zod';
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// GUILD SCHEMA
/////////////////////////////////////////

export const GuildSchema = z.object({
  id: z.string().cuid(),
  name: z.string(),
  slug: z.string(),
  description: z.string(),
  website: z.string().nullable(),
  email: z.string(),
  phone: z.string().nullable(),
  address: z.string().nullable(),
  logo: z.string().nullable(),
  portfolio: z.string().nullable(),
  specialties: z.string().nullable(),
  isVerified: z.boolean(),
  rating: z.instanceof(Prisma.Decimal, { message: "Field 'rating' must be a Decimal. Location: ['Models', 'Guild']"}).nullable(),
  reviewsCount: z.number().int(),
  projectsCount: z.number().int(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  ownerId: z.string(),
})

export type Guild = z.infer<typeof GuildSchema>

export default GuildSchema;

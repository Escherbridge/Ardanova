import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { UserRoleSchema } from '../inputTypeSchemas/UserRoleSchema'
import { UserTypeSchema } from '../inputTypeSchemas/UserTypeSchema'
import { UserTierSchema } from '../inputTypeSchemas/UserTierSchema'
import { VerificationLevelSchema } from '../inputTypeSchemas/VerificationLevelSchema'

/////////////////////////////////////////
// USER SCHEMA
/////////////////////////////////////////

export const UserSchema = z.object({
  role: UserRoleSchema,
  userType: UserTypeSchema,
  tier: UserTierSchema,
  verificationLevel: VerificationLevelSchema,
  id: z.string().cuid(),
  email: z.string(),
  emailVerified: z.coerce.date().nullable(),
  name: z.string().nullable(),
  image: z.string().nullable(),
  bio: z.string().nullable(),
  location: z.string().nullable(),
  phone: z.string().nullable(),
  website: z.string().nullable(),
  linkedIn: z.string().nullable(),
  twitter: z.string().nullable(),
  isVerified: z.boolean(),
  totalXP: z.number().int(),
  level: z.number().int(),
  trustScore: z.instanceof(Prisma.Decimal, { message: "Field 'trustScore' must be a Decimal. Location: ['Models', 'User']"}),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type User = z.infer<typeof UserSchema>

export default UserSchema;

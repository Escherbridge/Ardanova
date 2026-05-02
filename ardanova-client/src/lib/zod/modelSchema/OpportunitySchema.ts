import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { OpportunityTypeSchema } from '../inputTypeSchemas/OpportunityTypeSchema'
import { OpportunityOriginSchema } from '../inputTypeSchemas/OpportunityOriginSchema'
import { OpportunityStatusSchema } from '../inputTypeSchemas/OpportunityStatusSchema'
import { ExperienceLevelSchema } from '../inputTypeSchemas/ExperienceLevelSchema'

/////////////////////////////////////////
// OPPORTUNITY SCHEMA
/////////////////////////////////////////

export const OpportunitySchema = z.object({
  type: OpportunityTypeSchema,
  origin: OpportunityOriginSchema,
  status: OpportunityStatusSchema,
  experienceLevel: ExperienceLevelSchema,
  id: z.string().cuid(),
  title: z.string(),
  slug: z.string(),
  description: z.string(),
  requirements: z.string().nullable(),
  skills: z.string().nullable(),
  benefits: z.string().nullable(),
  location: z.string().nullable(),
  isRemote: z.boolean(),
  compensation: z.instanceof(Prisma.Decimal, { message: "Field 'compensation' must be a Decimal. Location: ['Models', 'Opportunity']"}).nullable(),
  compensationDetails: z.string().nullable(),
  deadline: z.coerce.date().nullable(),
  maxApplications: z.number().int().nullable(),
  applicationsCount: z.number().int(),
  bidsCount: z.number().int(),
  coverImage: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  closedAt: z.coerce.date().nullable(),
  posterId: z.string(),
  guildId: z.string().nullable(),
  projectId: z.string().nullable(),
  taskId: z.string().nullable(),
})

export type Opportunity = z.infer<typeof OpportunitySchema>

export default OpportunitySchema;

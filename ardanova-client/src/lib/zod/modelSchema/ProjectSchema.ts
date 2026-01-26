import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { ProjectCategorySchema } from '../inputTypeSchemas/ProjectCategorySchema'
import { ProjectStatusSchema } from '../inputTypeSchemas/ProjectStatusSchema'

/////////////////////////////////////////
// PROJECT SCHEMA
/////////////////////////////////////////

export const ProjectSchema = z.object({
  category: ProjectCategorySchema,
  status: ProjectStatusSchema,
  id: z.string().cuid(),
  title: z.string(),
  slug: z.string(),
  description: z.string(),
  problemStatement: z.string(),
  solution: z.string(),
  fundingGoal: z.instanceof(Prisma.Decimal, { message: "Field 'fundingGoal' must be a Decimal. Location: ['Models', 'Project']"}).nullable(),
  currentFunding: z.instanceof(Prisma.Decimal, { message: "Field 'currentFunding' must be a Decimal. Location: ['Models', 'Project']"}),
  supportersCount: z.number().int(),
  votesCount: z.number().int(),
  viewsCount: z.number().int(),
  featured: z.boolean(),
  isTrending: z.boolean(),
  trendingScore: z.instanceof(Prisma.Decimal, { message: "Field 'trendingScore' must be a Decimal. Location: ['Models', 'Project']"}),
  trendingRank: z.number().int().nullable(),
  trendingAt: z.coerce.date().nullable(),
  tags: z.string().nullable(),
  images: z.string().nullable(),
  videos: z.string().nullable(),
  documents: z.string().nullable(),
  targetAudience: z.string().nullable(),
  expectedImpact: z.string().nullable(),
  timeline: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  publishedAt: z.coerce.date().nullable(),
  fundedAt: z.coerce.date().nullable(),
  completedAt: z.coerce.date().nullable(),
  createdById: z.string(),
  assignedGuildId: z.string().nullable(),
})

export type Project = z.infer<typeof ProjectSchema>

export default ProjectSchema;

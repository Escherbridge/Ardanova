import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { Prisma } from '@prisma/client'
import { PostTypeSchema } from '../inputTypeSchemas/PostTypeSchema'
import { PostVisibilitySchema } from '../inputTypeSchemas/PostVisibilitySchema'

/////////////////////////////////////////
// POST SCHEMA
/////////////////////////////////////////

export const PostSchema = z.object({
  type: PostTypeSchema,
  visibility: PostVisibilitySchema,
  id: z.string().cuid(),
  authorId: z.string(),
  projectId: z.string().nullable(),
  guildId: z.string().nullable(),
  title: z.string().nullable(),
  content: z.string(),
  metadata: JsonValueSchema.nullable(),
  likesCount: z.number().int(),
  commentsCount: z.number().int(),
  sharesCount: z.number().int(),
  viewsCount: z.number().int(),
  isPinned: z.boolean(),
  isTrending: z.boolean(),
  trendingScore: z.instanceof(Prisma.Decimal, { message: "Field 'trendingScore' must be a Decimal. Location: ['Models', 'Post']"}),
  trendingRank: z.number().int().nullable(),
  trendingAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type Post = z.infer<typeof PostSchema>

export default PostSchema;

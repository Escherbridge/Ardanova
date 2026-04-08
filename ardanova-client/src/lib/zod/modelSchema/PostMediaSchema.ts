import { z } from 'zod';
import { MimeTypeSchema } from '../inputTypeSchemas/MimeTypeSchema'

/////////////////////////////////////////
// POST MEDIA SCHEMA
/////////////////////////////////////////

export const PostMediaSchema = z.object({
  type: MimeTypeSchema,
  id: z.string().cuid(),
  postId: z.string(),
  url: z.string(),
  thumbnailUrl: z.string().nullable(),
  altText: z.string().nullable(),
  order: z.number().int(),
  createdAt: z.coerce.date(),
})

export type PostMedia = z.infer<typeof PostMediaSchema>

export default PostMediaSchema;

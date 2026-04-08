import { z } from 'zod';

export const PostVisibilitySchema = z.enum(['PUBLIC','PROJECT_MEMBERS','GUILD_MEMBERS','PRIVATE']);

export type PostVisibilityType = `${z.infer<typeof PostVisibilitySchema>}`

export default PostVisibilitySchema;

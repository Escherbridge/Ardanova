import { z } from 'zod';

export const PostBookmarkScalarFieldEnumSchema = z.enum(['id','postId','userId','createdAt']);

export default PostBookmarkScalarFieldEnumSchema;

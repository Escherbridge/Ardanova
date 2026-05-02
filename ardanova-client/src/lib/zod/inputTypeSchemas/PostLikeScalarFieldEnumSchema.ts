import { z } from 'zod';

export const PostLikeScalarFieldEnumSchema = z.enum(['id','postId','userId','createdAt']);

export default PostLikeScalarFieldEnumSchema;

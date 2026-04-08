import { z } from 'zod';

export const PostCommentScalarFieldEnumSchema = z.enum(['id','postId','authorId','parentId','content','likesCount','createdAt','updatedAt']);

export default PostCommentScalarFieldEnumSchema;

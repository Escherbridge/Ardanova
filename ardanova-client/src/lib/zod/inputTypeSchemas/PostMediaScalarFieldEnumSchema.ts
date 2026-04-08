import { z } from 'zod';

export const PostMediaScalarFieldEnumSchema = z.enum(['id','postId','type','url','thumbnailUrl','altText','order','createdAt']);

export default PostMediaScalarFieldEnumSchema;

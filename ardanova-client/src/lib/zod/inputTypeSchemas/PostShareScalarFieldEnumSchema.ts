import { z } from 'zod';

export const PostShareScalarFieldEnumSchema = z.enum(['id','postId','userId','sharedToProjectId','sharedToGuildId','comment','createdAt']);

export default PostShareScalarFieldEnumSchema;

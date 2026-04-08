import { z } from 'zod';

export const UserFollowScalarFieldEnumSchema = z.enum(['id','followerId','followingId','createdAt']);

export default UserFollowScalarFieldEnumSchema;

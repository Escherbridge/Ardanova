import { z } from 'zod';

export const PostScalarFieldEnumSchema = z.enum(['id','authorId','projectId','guildId','type','visibility','title','content','metadata','likesCount','commentsCount','sharesCount','viewsCount','isPinned','isTrending','trendingScore','trendingRank','trendingAt','createdAt','updatedAt']);

export default PostScalarFieldEnumSchema;

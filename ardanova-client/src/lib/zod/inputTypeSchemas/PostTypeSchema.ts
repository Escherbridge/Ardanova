import { z } from 'zod';

export const PostTypeSchema = z.enum(['POST','PROJECT_UPDATE','GUILD_ACTIVITY','TASK_COMPLETED','MILESTONE','PROPOSAL','SHOP_ITEM']);

export type PostTypeType = `${z.infer<typeof PostTypeSchema>}`

export default PostTypeSchema;

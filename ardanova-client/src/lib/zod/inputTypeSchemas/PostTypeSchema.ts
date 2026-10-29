import { z } from 'zod';

export const PostTypeSchema = z.enum(['POST','PROJECT_UPDATE','GUILD_ACTIVITY','TASK_COMPLETED','MILESTONE','PROPOSAL','SHOP_ITEM','PROJECT_LAUNCHED','FUNDING_PROGRESS','OPPORTUNITY_OPENED','OPPORTUNITY_UPDATE','GUILD_FOUNDED','GUILD_UPDATE','EVENT_SCHEDULED','DISCUSSION','MEMBER_JOINED']);

export type PostTypeType = `${z.infer<typeof PostTypeSchema>}`

export default PostTypeSchema;

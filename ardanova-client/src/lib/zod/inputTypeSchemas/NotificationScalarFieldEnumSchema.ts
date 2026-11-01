import { z } from 'zod';

export const NotificationScalarFieldEnumSchema = z.enum(['id','userId','type','title','message','data','isRead','readAt','actionUrl','createdAt','postId']);

export default NotificationScalarFieldEnumSchema;

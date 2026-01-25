import { z } from 'zod';

export const XPEventScalarFieldEnumSchema = z.enum(['id','userId','eventType','amount','source','sourceId','metadata','createdAt']);

export default XPEventScalarFieldEnumSchema;

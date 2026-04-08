import { z } from 'zod';

export const TokenHolderClassSchema = z.enum(['CONTRIBUTOR', 'INVESTOR', 'FOUNDER']);

export type TokenHolderClassType = `${z.infer<typeof TokenHolderClassSchema>}`;

export default TokenHolderClassSchema;

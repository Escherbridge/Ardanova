import { z } from 'zod';
import type { Prisma } from '@prisma/client';

export const DecimalJsLikeSchema = z.object({
  d: z.array(z.number()),
  e: z.number(),
  s: z.number(),
  toFixed: z.any(),
}) as z.ZodType<Prisma.DecimalJsLike>;

export default DecimalJsLikeSchema;

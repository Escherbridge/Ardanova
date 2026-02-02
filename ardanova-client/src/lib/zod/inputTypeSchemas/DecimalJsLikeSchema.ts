import { z } from 'zod';
import type { Prisma } from '@prisma/client';

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const DecimalJsLikeSchema = z.object({
  d: z.array(z.number()),
  e: z.number(),
  s: z.number(),
  toFixed: z.function(),
}) as any as z.ZodType<Prisma.DecimalJsLike>

export default DecimalJsLikeSchema;

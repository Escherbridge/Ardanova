import { z } from 'zod';
import { Prisma } from '@prisma/client';

export const JsonNullValueFilterSchema = z.enum(['DbNull','JsonNull','AnyNull',]).transform((value) => value === 'JsonNull' ? Prisma.JsonNull : value === 'DbNull' ? Prisma.DbNull : value === 'AnyNull' ? Prisma.AnyNull : value) as unknown as z.ZodType<Prisma.JsonNullValueFilter>;
import { z } from 'zod';
import { JsonValueSchema } from '../inputTypeSchemas/JsonValueSchema'
import { Prisma } from '@prisma/client'

/////////////////////////////////////////
// PROJECT TOKEN SCHEMA
/////////////////////////////////////////

export const ProjectTokenSchema = z.object({
  id: z.string().cuid(),
  projectId: z.string(),
  assetId: z.string().nullable(),
  name: z.string(),
  symbol: z.string(),
  totalSupply: z.instanceof(Prisma.Decimal, { message: "Field 'totalSupply' must be a Decimal. Location: ['Models', 'ProjectToken']"}),
  decimals: z.number().int(),
  allocation: JsonValueSchema,
  vestingConfig: JsonValueSchema.nullable(),
  isDeployed: z.boolean(),
  deployedAt: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
})

export type ProjectToken = z.infer<typeof ProjectTokenSchema>

export default ProjectTokenSchema;

import { z } from 'zod';
import { AssetDefinitionKindSchema } from '../inputTypeSchemas/AssetDefinitionKindSchema'

/////////////////////////////////////////
// ASSET DEFINITION SCHEMA
/////////////////////////////////////////

export const AssetDefinitionSchema = z.object({
  kind: AssetDefinitionKindSchema,
  id: z.string().cuid(),
  chainType: z.string(),
  chainNetwork: z.string(),
  canonicalAssetId: z.string(),
  symbol: z.string(),
  displayName: z.string(),
  scale: z.number().int(),
  supersedesAssetDefinitionId: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type AssetDefinition = z.infer<typeof AssetDefinitionSchema>

export default AssetDefinitionSchema;

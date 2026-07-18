import { z } from 'zod';

export const AssetDefinitionScalarFieldEnumSchema = z.enum(['id','kind','chainType','chainNetwork','canonicalAssetId','symbol','displayName','scale','supersedesAssetDefinitionId','createdAt']);

export default AssetDefinitionScalarFieldEnumSchema;

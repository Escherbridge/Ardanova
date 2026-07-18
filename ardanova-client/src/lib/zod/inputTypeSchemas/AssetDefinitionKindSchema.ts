import { z } from 'zod';

export const AssetDefinitionKindSchema = z.enum(['PROJECT_UTILITY','ECOSYSTEM_UTILITY','EQUITY_RIGHT','REDEMPTION_RIGHT']);

export type AssetDefinitionKindType = `${z.infer<typeof AssetDefinitionKindSchema>}`

export default AssetDefinitionKindSchema;

import { z } from 'zod';

export const KycProviderSchema = z.enum(['MANUAL','VERIFF']);

export type KycProviderType = `${z.infer<typeof KycProviderSchema>}`

export default KycProviderSchema;

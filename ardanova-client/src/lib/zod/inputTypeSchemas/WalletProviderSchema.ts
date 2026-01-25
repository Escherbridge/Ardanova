import { z } from 'zod';

export const WalletProviderSchema = z.enum(['PERA','DEFLY','ALGOSIGNER','WALLETCONNECT','OTHER']);

export type WalletProviderType = `${z.infer<typeof WalletProviderSchema>}`

export default WalletProviderSchema;

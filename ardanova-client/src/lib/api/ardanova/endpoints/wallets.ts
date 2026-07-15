import { type BaseApiClient, type ApiResponse } from "../../base-client";

export interface Wallet {
  id: string;
  userId: string;
  address: string;
  provider: string;
  label?: string | null;
  isVerified: boolean;
  isPrimary: boolean;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface CreateWalletDto {
  address: string;
  provider?: WalletProvider;
  label?: string;
  isPrimary?: boolean;
  [key: string]: unknown;
}

export interface UpdateWalletDto {
  label?: string;
  isPrimary?: boolean;
  [key: string]: unknown;
}

export type WalletProvider =
  | "PERA"
  | "DEFLY"
  | "ALGOSIGNER"
  | "WALLETCONNECT"
  | "OTHER";

export interface WalletVerificationChallenge {
  challengeId: string;
  message: string;
  chain: string;
  network: string;
  expiresAt: string;
}

export interface CompleteWalletVerificationDto {
  challengeId: string;
  nonce: string;
  signature: string;
}

export class WalletsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Wallet>> {
    return this.client.get<Wallet>(`/api/wallets/${encodeURIComponent(id)}`);
  }

  getMine(): Promise<ApiResponse<Wallet[]>> {
    return this.client.get<Wallet[]>("/api/wallets/me");
  }

  getByAddress(address: string): Promise<ApiResponse<Wallet>> {
    return this.client.get<Wallet>(
      `/api/wallets/address/${encodeURIComponent(address)}`,
    );
  }

  getMyPrimary(): Promise<ApiResponse<Wallet>> {
    return this.client.get<Wallet>("/api/wallets/me/primary");
  }

  create(data: CreateWalletDto): Promise<ApiResponse<Wallet>> {
    return this.client.post<Wallet>("/api/wallets", data);
  }

  update(id: string, data: UpdateWalletDto): Promise<ApiResponse<Wallet>> {
    return this.client.put<Wallet>(
      `/api/wallets/${encodeURIComponent(id)}`,
      data,
    );
  }

  issueVerificationChallenge(
    id: string,
  ): Promise<ApiResponse<WalletVerificationChallenge>> {
    return this.client.post<WalletVerificationChallenge>(
      `/api/wallets/${encodeURIComponent(id)}/verification-challenge`,
      {},
    );
  }

  completeVerificationChallenge(
    id: string,
    data: CompleteWalletVerificationDto,
  ): Promise<ApiResponse<Wallet>> {
    return this.client.post<Wallet>(
      `/api/wallets/${encodeURIComponent(id)}/verification-challenge/complete`,
      data,
    );
  }

  setPrimary(id: string): Promise<ApiResponse<Wallet>> {
    return this.client.post<Wallet>(
      `/api/wallets/${encodeURIComponent(id)}/set-primary`,
      {},
    );
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/wallets/${encodeURIComponent(id)}`);
  }
}

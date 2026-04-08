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
  userId: string;
  address: string;
  provider?: string;
  label?: string;
  isPrimary?: boolean;
  [key: string]: unknown;
}

export interface UpdateWalletDto {
  label?: string;
  isPrimary?: boolean;
  [key: string]: unknown;
}

export class WalletsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Wallet>> {
    return this.client.get<Wallet>(`/api/wallets/${encodeURIComponent(id)}`);
  }

  getByUserId(userId: string): Promise<ApiResponse<Wallet[]>> {
    return this.client.get<Wallet[]>(`/api/wallets/user/${encodeURIComponent(userId)}`);
  }

  getByAddress(address: string): Promise<ApiResponse<Wallet>> {
    return this.client.get<Wallet>(`/api/wallets/address/${encodeURIComponent(address)}`);
  }

  getPrimary(userId: string): Promise<ApiResponse<Wallet>> {
    return this.client.get<Wallet>(`/api/wallets/user/${encodeURIComponent(userId)}/primary`);
  }

  create(data: CreateWalletDto): Promise<ApiResponse<Wallet>> {
    return this.client.post<Wallet>("/api/wallets", data);
  }

  update(id: string, data: UpdateWalletDto): Promise<ApiResponse<Wallet>> {
    return this.client.put<Wallet>(`/api/wallets/${encodeURIComponent(id)}`, data);
  }

  verify(id: string): Promise<ApiResponse<Wallet>> {
    return this.client.post<Wallet>(`/api/wallets/${encodeURIComponent(id)}/verify`, {});
  }

  setPrimary(id: string): Promise<ApiResponse<Wallet>> {
    return this.client.post<Wallet>(`/api/wallets/${encodeURIComponent(id)}/set-primary`, {});
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/wallets/${encodeURIComponent(id)}`);
  }
}

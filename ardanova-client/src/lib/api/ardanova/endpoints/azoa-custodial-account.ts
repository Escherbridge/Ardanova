import { type BaseApiClient, type ApiResponse } from "../../base-client";

export type AzoaKycStatus = "Unknown" | "Pending" | "Approved" | "Rejected";

export interface AzoaCustodialAccountStatusDto {
  avatarId?: string | null;
  walletId?: string | null;
  walletAddress?: string | null;
  kycStatus: AzoaKycStatus;
  identityReady: boolean;
  kycReady: boolean;
  walletReady: boolean;
  ready: boolean;
  unavailableReason?: string | null;
}

export interface AzoaCustodialAccountCapabilitiesDto {
  enabled: boolean;
  walletChain: string;
  custodyMode: string;
  custodyAvailable: boolean;
  blockchainProviderAvailable: boolean;
  kycProvider: string;
  kycAvailable: boolean;
  hostedVerification: boolean;
  acceptsDocumentReferences: boolean;
  developmentSimulation: boolean;
  identityReady: boolean;
  kycReady: boolean;
  walletProvisioningReady: boolean;
  ready: boolean;
  unavailableReason?: string | null;
}

export interface AzoaKycSessionDto {
  provider: string;
  hostedVerification: boolean;
  acceptsDocumentReferences: boolean;
  developmentSimulation: boolean;
  verificationUrl?: string | null;
  expiresAt?: string | null;
  instructions?: string | null;
}

export class AzoaCustodialAccountEndpoint {
  constructor(private client: BaseApiClient) {}

  getCapabilities(): Promise<ApiResponse<AzoaCustodialAccountCapabilitiesDto>> {
    return this.client.get<AzoaCustodialAccountCapabilitiesDto>(
      "/api/azoa/custodial-account/capabilities",
    );
  }

  ensure(): Promise<ApiResponse<AzoaCustodialAccountStatusDto>> {
    return this.client.post<AzoaCustodialAccountStatusDto>(
      "/api/azoa/custodial-account/ensure",
    );
  }

  getStatus(): Promise<ApiResponse<AzoaCustodialAccountStatusDto>> {
    return this.client.get<AzoaCustodialAccountStatusDto>(
      "/api/azoa/custodial-account/status",
    );
  }

  beginKyc(): Promise<ApiResponse<AzoaKycSessionDto>> {
    return this.client.post<AzoaKycSessionDto>(
      "/api/azoa/custodial-account/kyc/session",
    );
  }
}

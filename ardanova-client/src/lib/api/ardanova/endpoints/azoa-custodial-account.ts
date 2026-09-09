import { type BaseApiClient, type ApiResponse } from "../../base-client";

/** Mirrors the node's TenantKycStatus. "UnderReview" means setup is FINISHED and a
 *  reviewer owes a decision - it is not another kind of unfinished setup. */
export type AzoaKycStatus =
  | "Unknown"
  | "Pending"
  | "Approved"
  | "Rejected"
  | "UnderReview";

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
  /** When an open review is due a decision; null unless `kycStatus` is "UnderReview". */
  reviewDueAt?: string | null;
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

/**
 * The outcome of withdrawing the standing permission for ArdaNova to act for this person.
 * The server half of this chain is L2.7's - an ArdaNova controller action over the node's
 * `DELETE /api/tenant/consent/provisioned`; this is only the client's call into it.
 */
export interface AzoaConsentRevocationDto {
  revoked: boolean;
  revokedAt?: string | null;
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

  /** Withdraws ArdaNova's standing permission to act for this person; re-granting it restores the account. */
  revokeConsent(): Promise<ApiResponse<AzoaConsentRevocationDto>> {
    return this.client.delete<AzoaConsentRevocationDto>(
      "/api/azoa/custodial-account/consent",
    );
  }
}

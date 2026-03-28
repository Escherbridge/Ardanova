import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type { MembershipCredential } from "./membership-credentials";

/** Matches backend `GrantMembershipCredentialDto` (camelCase JSON). */
export interface GrantMembershipCredentialDto {
  projectId?: string;
  guildId?: string;
  userId: string;
  grantedVia: string;
  grantedByProposalId?: string;
}

/** Backend `CheckAutoGrantRequestDto` — camelCase JSON */
export interface CheckAutoGrantRequest {
  userId: string;
  projectId?: string;
  guildId?: string;
}

export interface UpdateTierDto {
  tier: string;
}

export interface AsaInfo {
  assetId?: string;
  [key: string]: unknown;
}

export interface CredentialWithChainData extends MembershipCredential {
  asaInfo?: AsaInfo;
  [key: string]: unknown;
}

export class CredentialUtilityEndpoint {
  constructor(private client: BaseApiClient) {}

  grantAndMint(dto: GrantMembershipCredentialDto): Promise<ApiResponse<MembershipCredential>> {
    return this.client.post<MembershipCredential>("/api/CredentialUtility/grant-and-mint", dto);
  }

  revokeAndBurn(id: string): Promise<ApiResponse<MembershipCredential>> {
    return this.client.post<MembershipCredential>(`/api/CredentialUtility/${encodeURIComponent(id)}/revoke-and-burn`, {});
  }

  upgradeTier(id: string, body: UpdateTierDto): Promise<ApiResponse<MembershipCredential>> {
    return this.client.patch<MembershipCredential>(
      `/api/CredentialUtility/${encodeURIComponent(id)}/upgrade-tier`,
      body,
    );
  }

  checkAutoGrant(dto: CheckAutoGrantRequest): Promise<ApiResponse<MembershipCredential | null>> {
    return this.client.post<MembershipCredential | null>("/api/CredentialUtility/check-auto-grant", dto);
  }

  retryMint(id: string): Promise<ApiResponse<MembershipCredential>> {
    return this.client.post<MembershipCredential>(`/api/CredentialUtility/${encodeURIComponent(id)}/retry-mint`, {});
  }

  getChainData(id: string): Promise<ApiResponse<CredentialWithChainData>> {
    return this.client.get<CredentialWithChainData>(`/api/CredentialUtility/${encodeURIComponent(id)}/chain-data`);
  }
}

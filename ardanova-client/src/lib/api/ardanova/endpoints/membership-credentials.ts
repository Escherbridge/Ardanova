import { type BaseApiClient, type ApiResponse } from "../../base-client";

export type MembershipCredentialStatus = "ACTIVE" | "REVOKED" | "SUSPENDED";
/** Matches ArdaNova.Domain.Models.Enums.MembershipGrantType */
export type MembershipGrantType =
  | "FOUNDER"
  | "DAO_VOTE"
  | "CONTRIBUTION_THRESHOLD"
  | "APPLICATION_APPROVED"
  | "GAME_SDK_THRESHOLD";

export interface MembershipCredential {
  id: string;
  projectId?: string | null;
  guildId?: string | null;
  userId: string;
  status: MembershipCredentialStatus;
  grantedVia: MembershipGrantType;
  tier?: string | null;
  grantedByProposalId?: string | null;
  isTransferable?: boolean;
  mintTxHash?: string | null;
  revokeTxHash?: string | null;
  createdAt?: string;
  updatedAt?: string;
  revokedAt?: string | null;
  mintAddress?: string | null;
  [key: string]: unknown;
}

export interface GrantMembershipCredentialDto {
  projectId?: string;
  guildId?: string;
  userId: string;
  grantedVia: MembershipGrantType;
  grantedByProposalId?: string;
}

export interface RevokeMembershipCredentialDto {
  reason?: string;
  [key: string]: unknown;
}

export interface UpdateMembershipCredentialMintDto {
  mintAddress?: string | null;
  [key: string]: unknown;
}

export interface UpdateCredentialTierDto {
  tier: string;
}

export interface CredentialEligibilityDto {
  isEligible: boolean;
  reason?: string;
  requiredTier?: string;
}

export class MembershipCredentialsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<MembershipCredential>> {
    return this.client.get<MembershipCredential>(`/api/membershipcredentials/${id}`);
  }

  getByProjectId(projectId: string): Promise<ApiResponse<MembershipCredential[]>> {
    return this.client.get<MembershipCredential[]>(`/api/membershipcredentials/project/${projectId}`);
  }

  getActiveByProjectId(projectId: string): Promise<ApiResponse<MembershipCredential[]>> {
    return this.client.get<MembershipCredential[]>(`/api/membershipcredentials/project/${projectId}/active`);
  }

  getByUserId(userId: string): Promise<ApiResponse<MembershipCredential[]>> {
    return this.client.get<MembershipCredential[]>(`/api/membershipcredentials/user/${userId}`);
  }

  getByProjectAndUser(projectId: string, userId: string): Promise<ApiResponse<MembershipCredential | null>> {
    return this.client.get<MembershipCredential | null>(`/api/membershipcredentials/project/${projectId}/user/${userId}`);
  }

  getByGuildId(guildId: string): Promise<ApiResponse<MembershipCredential[]>> {
    return this.client.get<MembershipCredential[]>(`/api/membershipcredentials/guild/${guildId}`);
  }

  getActiveByGuildId(guildId: string): Promise<ApiResponse<MembershipCredential[]>> {
    return this.client.get<MembershipCredential[]>(`/api/membershipcredentials/guild/${guildId}/active`);
  }

  getByGuildAndUser(guildId: string, userId: string): Promise<ApiResponse<MembershipCredential | null>> {
    return this.client.get<MembershipCredential | null>(`/api/membershipcredentials/guild/${guildId}/user/${userId}`);
  }

  grant(data: GrantMembershipCredentialDto): Promise<ApiResponse<MembershipCredential>> {
    return this.client.post<MembershipCredential>("/api/membershipcredentials", data);
  }

  revoke(id: string, data?: RevokeMembershipCredentialDto): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/membershipcredentials/${id}/revoke`, data ?? {});
  }

  suspend(id: string): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/membershipcredentials/${id}/suspend`, {});
  }

  reactivate(id: string): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/membershipcredentials/${id}/reactivate`, {});
  }

  updateTier(id: string, data: UpdateCredentialTierDto): Promise<ApiResponse<MembershipCredential>> {
    return this.client.patch<MembershipCredential>(`/api/membershipcredentials/${id}/tier`, data);
  }
}

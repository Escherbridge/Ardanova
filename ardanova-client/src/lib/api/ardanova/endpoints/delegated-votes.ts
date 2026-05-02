import { type BaseApiClient, type ApiResponse } from "../../base-client";

export interface DelegatedVote {
  id: string;
  projectId: string;
  delegatorId: string;
  delegateeId: string;
  shareId: string;
  amount: number;
  isActive: boolean;
  createdAt: string;
  expiresAt?: string | null;
  revokedAt?: string | null;
  [key: string]: unknown;
}

export interface CreateDelegatedVoteDto {
  projectId: string;
  delegatorId: string;
  delegateeId: string;
  shareId: string;
  amount: number;
  expiresAt?: string;
  [key: string]: unknown;
}

export interface UpdateDelegatedVoteDto {
  amount?: number;
  expiresAt?: string;
  [key: string]: unknown;
}

export class DelegatedVotesEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<DelegatedVote>> {
    return this.client.get<DelegatedVote>(`/api/delegatedvotes/${encodeURIComponent(id)}`);
  }

  getByDelegator(delegatorId: string): Promise<ApiResponse<DelegatedVote[]>> {
    return this.client.get<DelegatedVote[]>(`/api/delegatedvotes/delegator/${encodeURIComponent(delegatorId)}`);
  }

  getByDelegatee(delegateeId: string): Promise<ApiResponse<DelegatedVote[]>> {
    return this.client.get<DelegatedVote[]>(`/api/delegatedvotes/delegatee/${encodeURIComponent(delegateeId)}`);
  }

  getByProject(projectId: string): Promise<ApiResponse<DelegatedVote[]>> {
    return this.client.get<DelegatedVote[]>(`/api/delegatedvotes/project/${encodeURIComponent(projectId)}`);
  }

  getActiveByProject(projectId: string): Promise<ApiResponse<DelegatedVote[]>> {
    return this.client.get<DelegatedVote[]>(
      `/api/delegatedvotes/project/${encodeURIComponent(projectId)}/active`
    );
  }

  getTotalPower(delegateeId: string, projectId: string): Promise<ApiResponse<number>> {
    return this.client.get<number>(
      `/api/delegatedvotes/delegatee/${encodeURIComponent(delegateeId)}/project/${encodeURIComponent(projectId)}/power`
    );
  }

  create(data: CreateDelegatedVoteDto): Promise<ApiResponse<DelegatedVote>> {
    return this.client.post<DelegatedVote>("/api/delegatedvotes", data);
  }

  update(id: string, data: UpdateDelegatedVoteDto): Promise<ApiResponse<DelegatedVote>> {
    return this.client.put<DelegatedVote>(`/api/delegatedvotes/${encodeURIComponent(id)}`, data);
  }

  revoke(id: string): Promise<ApiResponse<DelegatedVote>> {
    return this.client.post<DelegatedVote>(`/api/delegatedvotes/${encodeURIComponent(id)}/revoke`, {});
  }
}

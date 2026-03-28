import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

export interface Proposal {
  id: string;
  projectId: string;
  creatorId: string;
  type: string;
  title: string;
  description: string;
  options: string;
  quorum: number;
  threshold: number;
  status: string;
  votingStart?: string | null;
  votingEnd?: string | null;
  createdAt: string;
  [key: string]: unknown;
}

export interface Vote {
  id: string;
  proposalId: string;
  voterId: string;
  choice: number;
  weight: number;
  reason?: string | null;
  txHash?: string | null;
  createdAt: string;
  voter?: { id: string; name?: string | null; image?: string | null };
}

export interface ProposalVoteSummary {
  proposalId: string;
  totalVotes: number;
  options?: unknown[];
  [key: string]: unknown;
}

export interface CreateProposalDto {
  creatorId?: string;
  createdById?: string;
  projectId: string;
  type: string;
  title: string;
  description: string;
  /** JSON string — matches .NET `CreateProposalDto.Options`. */
  options: string;
  quorum: number;
  threshold: number;
  votingEnd?: string;
  votingDays?: number;
  [key: string]: unknown;
}

export interface UpdateProposalDto {
  title?: string;
  description?: string;
  options?: string[];
  quorum?: number;
  threshold?: number;
  votingEnd?: string;
  [key: string]: unknown;
}

export interface CastVoteDto {
  voterId?: string;
  userId?: string;
  choice: number;
  weight?: number;
  reason?: string;
  [key: string]: unknown;
}

export interface SearchProposalsParams {
  searchTerm?: string;
  type?: string;
  status?: string;
  projectId?: string;
  page?: number;
  pageSize?: number;
}

export class GovernanceEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Proposal>> {
    return this.client.get<Proposal>(`/api/governance/proposals/${id}`);
  }

  getActive(): Promise<ApiResponse<Proposal[]>> {
    return this.client.get<Proposal[]>("/api/governance/proposals/active");
  }

  getByCreatorId(creatorId: string): Promise<ApiResponse<Proposal[]>> {
    return this.client.get<Proposal[]>(`/api/governance/proposals/proposer/${creatorId}`);
  }

  search(params: SearchProposalsParams = {}): Promise<ApiResponse<PagedResult<Proposal>>> {
    const sp = new URLSearchParams();
    if (params.searchTerm) sp.set("searchTerm", params.searchTerm);
    if (params.type) sp.set("type", params.type);
    if (params.status) sp.set("status", params.status);
    if (params.projectId) sp.set("projectId", params.projectId ?? "");
    sp.set("page", String(params.page ?? 1));
    sp.set("pageSize", String(params.pageSize ?? 10));
    return this.client.get<PagedResult<Proposal>>(`/api/governance/proposals/search?${sp.toString()}`);
  }

  create(data: CreateProposalDto): Promise<ApiResponse<Proposal>> {
    return this.client.post<Proposal>("/api/governance/proposals", data);
  }

  update(id: string, data: UpdateProposalDto): Promise<ApiResponse<Proposal>> {
    return this.client.put<Proposal>(`/api/governance/proposals/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/governance/proposals/${id}`);
  }

  vote(proposalId: string, dto: CastVoteDto): Promise<ApiResponse<Vote>> {
    return this.client.post<Vote>(`/api/governance/proposals/${proposalId}/vote`, dto);
  }

  getMyVote(proposalId: string, userId: string): Promise<ApiResponse<Vote | null>> {
    return this.client.get<Vote | null>(`/api/governance/proposals/${proposalId}/my-vote?userId=${encodeURIComponent(userId)}`);
  }

  getVotes(proposalId: string): Promise<ApiResponse<Vote[]>> {
    return this.client.get<Vote[]>(`/api/governance/proposals/${proposalId}/votes`);
  }

  getVoteSummary(proposalId: string): Promise<ApiResponse<ProposalVoteSummary>> {
    return this.client.get<ProposalVoteSummary>(`/api/governance/proposals/${proposalId}/summary`);
  }

  execute(id: string): Promise<ApiResponse<Proposal>> {
    return this.client.patch<Proposal>(`/api/governance/proposals/${id}/execute`, {});
  }

  cancel(id: string): Promise<ApiResponse<Proposal>> {
    return this.client.patch<Proposal>(`/api/governance/proposals/${id}/cancel`, {});
  }

  publishProposal(id: string): Promise<ApiResponse<Proposal>> {
    return this.client.patch<Proposal>(`/api/governance/proposals/${id}/publish`, {});
  }
}

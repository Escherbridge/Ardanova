import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

export interface Opportunity {
  id: string;
  posterId: string;
  title: string;
  description: string;
  type?: string | null;
  experienceLevel?: string | null;
  skills?: string | null;
  requirements?: string | null;
  compensation?: number | null;
  compensationDetails?: string | null;
  location?: string | null;
  isRemote?: boolean;
  deadline?: string | null;
  maxApplications?: number | null;
  projectId?: string | null;
  guildId?: string | null;
  taskId?: string | null;
  status?: string | null;
  slug?: string | null;
  origin?: string | null;
  applicationsCount?: number;
  bidsCount?: number;
  poster?: { id: string; name?: string | null; image?: string | null };
  createdAt: string;
  updatedAt: string;
}

export interface OpportunityApplication {
  id: string;
  opportunityId: string;
  userId: string;
  applicantId?: string;
  coverLetter?: string;
  portfolio?: string | null;
  additionalInfo?: string | null;
  status?: string | null;
  message?: string | null;
  appliedAt: string;
  applicant?: { id: string; name?: string | null; image?: string | null; email?: string | null };
}

export interface CreateOpportunityDto {
  posterId: string;
  title: string;
  description: string;
  type?: string;
  experienceLevel?: string;
  skills?: string;
  requirements?: string;
  compensation?: number;
  compensationDetails?: string;
  location?: string;
  isRemote?: boolean;
  deadline?: string;
  maxApplications?: number;
  projectId?: string | null;
  guildId?: string | null;
  taskId?: string | null;
  [key: string]: unknown;
}

export interface UpdateOpportunityDto {
  title?: string;
  description?: string;
  type?: string;
  experienceLevel?: string;
  skills?: string;
  requirements?: string;
  compensation?: number;
  compensationDetails?: string;
  location?: string;
  isRemote?: boolean;
  deadline?: string;
  maxApplications?: number;
  status?: string;
  [key: string]: unknown;
}

export interface ApplyToOpportunityDto {
  applicantId: string;
  coverLetter: string;
  portfolio?: string;
  additionalInfo?: string;
  [key: string]: unknown;
}

export interface UpdateApplicationStatusDto {
  status: string;
  reviewNotes?: string;
}

export interface CreateOpportunityUpdateDto {
  opportunityId: string;
  userId: string;
  title: string;
  content: string;
  images?: string;
}

export interface OpportunityCommentDto {
  id: string;
  opportunityId: string;
  userId: string;
  content: string;
  parentId?: string | null;
  createdAt: string;
  updatedAt: string;
  author?: { id: string; name?: string | null; image?: string | null; email?: string | null };
}

export interface CreateOpportunityCommentDto {
  opportunityId: string;
  userId: string;
  content: string;
  parentId?: string;
}

export interface OpportunityUpdate {
  id: string;
  opportunityId: string;
  userId: string;
  title: string;
  content: string;
  images?: string | null;
  createdAt: string;
  user?: { id: string; name?: string | null; image?: string | null };
}

export interface SearchOpportunitiesParams {
  searchTerm?: string;
  type?: string;
  experienceLevel?: string;
  skills?: string;
  sourceType?: string;
  page?: number;
  pageSize?: number;
}

export class OpportunitiesEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Opportunity>> {
    return this.client.get<Opportunity>(`/api/opportunities/${id}`);
  }

  getBySlug(slug: string): Promise<ApiResponse<Opportunity>> {
    return this.client.get<Opportunity>(`/api/opportunities/slug/${encodeURIComponent(slug)}`);
  }

  getByPosterId(posterId: string): Promise<ApiResponse<Opportunity[]>> {
    return this.client.get<Opportunity[]>(`/api/opportunities/poster/${posterId}`);
  }

  getByGuildId(guildId: string): Promise<ApiResponse<Opportunity[]>> {
    return this.client.get<Opportunity[]>(`/api/opportunities/guild/${guildId}`);
  }

  getByProjectId(projectId: string): Promise<ApiResponse<Opportunity[]>> {
    return this.client.get<Opportunity[]>(`/api/opportunities/project/${projectId}`);
  }

  search(params: SearchOpportunitiesParams = {}): Promise<ApiResponse<PagedResult<Opportunity> & { totalCount?: number; totalPages?: number; hasNextPage?: boolean }>> {
    const sp = new URLSearchParams();
    if (params.searchTerm) sp.set("searchTerm", params.searchTerm);
    if (params.type) sp.set("type", params.type);
    if (params.experienceLevel) sp.set("experienceLevel", params.experienceLevel);
    if (params.skills) sp.set("skills", params.skills);
    if (params.sourceType) sp.set("sourceType", params.sourceType ?? "");
    sp.set("page", String(params.page ?? 1));
    sp.set("pageSize", String(params.pageSize ?? 10));
    return this.client.get<PagedResult<Opportunity> & { totalCount?: number; totalPages?: number; hasNextPage?: boolean }>(
      `/api/opportunities/search?${sp.toString()}`
    );
  }

  create(data: CreateOpportunityDto): Promise<ApiResponse<Opportunity>> {
    return this.client.post<Opportunity>("/api/opportunities", data);
  }

  update(id: string, data: UpdateOpportunityDto): Promise<ApiResponse<Opportunity>> {
    return this.client.put<Opportunity>(`/api/opportunities/${id}`, data);
  }

  close(id: string): Promise<ApiResponse<Opportunity>> {
    return this.client.patch<Opportunity>(`/api/opportunities/${id}/close`, {});
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/opportunities/${id}`);
  }

  apply(opportunityId: string, dto: ApplyToOpportunityDto): Promise<ApiResponse<OpportunityApplication>> {
    return this.client.post<OpportunityApplication>(`/api/opportunities/${opportunityId}/apply`, dto);
  }

  getApplications(opportunityId: string): Promise<ApiResponse<OpportunityApplication[]>> {
    return this.client.get<OpportunityApplication[]>(`/api/opportunities/${opportunityId}/applications`);
  }

  updateApplicationStatus(
    applicationId: string,
    data: UpdateApplicationStatusDto
  ): Promise<ApiResponse<OpportunityApplication>> {
    return this.client.patch<OpportunityApplication>(`/api/opportunities/applications/${applicationId}/status`, data);
  }

  getUpdates(opportunityId: string): Promise<ApiResponse<OpportunityUpdate[]>> {
    return this.client.get<OpportunityUpdate[]>(`/api/opportunities/${opportunityId}/updates`);
  }

  createUpdate(
    opportunityId: string,
    data: CreateOpportunityUpdateDto
  ): Promise<ApiResponse<OpportunityUpdate>> {
    return this.client.post<OpportunityUpdate>(`/api/opportunities/${opportunityId}/updates`, data);
  }

  deleteUpdate(updateId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/opportunities/updates/${updateId}`);
  }

  getComments(opportunityId: string): Promise<ApiResponse<OpportunityCommentDto[]>> {
    return this.client.get<OpportunityCommentDto[]>(`/api/opportunities/${opportunityId}/comments`);
  }

  addComment(
    opportunityId: string,
    data: CreateOpportunityCommentDto
  ): Promise<ApiResponse<OpportunityCommentDto>> {
    return this.client.post<OpportunityCommentDto>(`/api/opportunities/${opportunityId}/comments`, data);
  }

  deleteComment(commentId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/opportunities/comments/${commentId}`);
  }
}

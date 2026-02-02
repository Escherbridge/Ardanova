import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

// ============ Type Definitions ============

export interface Project {
  id: string;
  title: string;
  slug: string;
  description: string;
  problemStatement: string;
  solution: string;
  categories: string[];
  status: string;
  fundingGoal?: number;
  currentFunding: number;
  supportersCount: number;
  votesCount: number;
  viewsCount: number;
  featured: boolean;
  tags?: string;
  images?: string;
  videos?: string;
  documents?: string;
  targetAudience?: string;
  expectedImpact?: string;
  timeline?: string;
  createdById: string;
  assignedAgencyId?: string;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string;
  fundedAt?: string;
  completedAt?: string;
}

export interface CreateProjectDto {
  title: string;
  description: string;
  problemStatement: string;
  solution: string;
  categories: string[];
  createdById: string;
  targetAudience?: string;
  expectedImpact?: string;
  timeline?: string;
  tags?: string;
  images?: string;
  videos?: string;
  documents?: string;
  fundingGoal?: number;
}

export interface UpdateProjectDto {
  title?: string;
  description?: string;
  problemStatement?: string;
  solution?: string;
  categories?: string[];
  status?: string;
  targetAudience?: string;
  expectedImpact?: string;
  timeline?: string;
  tags?: string;
  images?: string;
  videos?: string;
  documents?: string;
  fundingGoal?: number;
}

export interface SearchProjectsParams {
  searchTerm?: string;
  status?: string;
  category?: string;
  page?: number;
  pageSize?: number;
}

export interface ProjectResource {
  id: string;
  projectId: string;
  name: string;
  description?: string;
  quantity: number;
  estimatedCost?: number;
  recurringCost?: number;
  recurringIntervalDays?: number;
  isRequired: boolean;
  isObtained: boolean;
  createdAt: string;
}

export interface CreateResourceDto {
  name: string;
  description?: string;
  quantity?: number;
  estimatedCost?: number;
  recurringCost?: number;
  recurringIntervalDays?: number;
  isRequired?: boolean;
}

export interface UpdateResourceDto {
  name?: string;
  description?: string;
  quantity?: number;
  estimatedCost?: number;
  recurringCost?: number;
  recurringIntervalDays?: number;
  isRequired?: boolean;
  isObtained?: boolean;
}

export interface ProjectMilestone {
  id: string;
  projectId: string;
  title: string;
  description?: string;
  targetDate: string;
  completedAt?: string;
  isCompleted: boolean;
  createdAt: string;
}

export interface CreateMilestoneDto {
  title: string;
  description?: string;
  targetDate: string;
}

export interface UpdateMilestoneDto {
  title?: string;
  description?: string;
  targetDate?: string;
}

export interface ProjectMember {
  id: string;
  projectId: string;
  userId: string;
  role: string;
  tokenBalance: number;
  votingPower: number;
  joinedAt: string;
  user?: { id: string; name?: string; email: string; image?: string };
}

export interface AddMemberDto {
  userId: string;
  role: string;
}

export interface UpdateMemberDto {
  role?: string;
}

export interface ProjectApplication {
  id: string;
  projectId: string;
  userId: string;
  roleTitle: string;
  message: string;
  skills?: string;
  experience?: string;
  availability?: string;
  status: string;
  appliedAt: string;
  reviewedAt?: string;
  reviewMessage?: string;
  user?: { id: string; name?: string; email: string; image?: string };
}

export interface CreateApplicationDto {
  roleTitle: string;
  message: string;
  skills?: string;
  experience?: string;
  availability?: string;
}

export interface ReviewApplicationDto {
  status: string;
  reviewMessage?: string;
}

export interface Proposal {
  id: string;
  projectId: string;
  creatorId: string;
  type: string;
  title: string;
  description: string;
  options: string; // JSON string of options array
  quorum: number;
  threshold: number;
  status: string;
  votingStart?: string;
  votingEnd?: string;
  createdAt: string;
  creator?: { id: string; name?: string; email: string; image?: string };
  votes?: Vote[];
}

export interface CreateProposalDto {
  creatorId?: string;
  createdById?: string;
  type: string;
  title: string;
  description: string;
  options: string[];
  quorum: number;
  threshold: number;
  votingEnd?: string;
  votingDays?: number;
}

export interface Vote {
  id: string;
  proposalId: string;
  voterId: string;
  choice: number;
  weight: number;
  reason?: string;
  createdAt: string;
  voter?: { id: string; name?: string; email: string; image?: string };
}

export interface CastVoteDto {
  voterId?: string;
  userId?: string;
  choice: number;
  weight?: number;
  reason?: string;
}

export interface ProjectUpdate {
  id: string;
  projectId: string;
  userId: string;
  title: string;
  content: string;
  images?: string;
  createdAt: string;
  user?: { id: string; name?: string; email: string; image?: string };
}

export interface CreateUpdateDto {
  userId?: string;
  createdById?: string;
  title: string;
  content: string;
  images?: string;
}

export interface ProjectComment {
  id: string;
  projectId: string;
  userId: string;
  content: string;
  parentId?: string;
  createdAt: string;
  updatedAt: string;
  user?: { id: string; name?: string; email: string; image?: string };
  replies?: ProjectComment[];
}

export interface CreateCommentDto {
  projectId?: string;
  userId: string;
  content: string;
  parentId?: string;
}

export interface ProjectSupport {
  id: string;
  projectId: string;
  userId: string;
  supportType: string;
  monthlyAmount?: number;
  message?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  project?: Project;
  user?: { id: string; name?: string; email: string; image?: string };
}

export interface CreateSupportDto {
  userId: string;
  supportType: string;
  monthlyAmount?: number;
  message?: string;
}

// ============ Projects Endpoint ============

export class ProjectsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Project>> {
    return this.client.get<Project>(`/api/projects/${id}`);
  }

  getBySlug(slug: string): Promise<ApiResponse<Project>> {
    return this.client.get<Project>(`/api/projects/slug/${slug}`);
  }

  getAll(): Promise<ApiResponse<Project[]>> {
    return this.client.get<Project[]>(`/api/projects`);
  }

  getPaged(page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<Project>>> {
    return this.client.get<PagedResult<Project>>(`/api/projects/paged?page=${page}&pageSize=${pageSize}`);
  }

  search(params: SearchProjectsParams = {}): Promise<ApiResponse<PagedResult<Project>>> {
    const queryParts: string[] = [];
    if (params.searchTerm) queryParts.push(`searchTerm=${encodeURIComponent(params.searchTerm)}`);
    if (params.status) queryParts.push(`status=${encodeURIComponent(params.status)}`);
    if (params.category) queryParts.push(`category=${encodeURIComponent(params.category)}`);
    queryParts.push(`page=${params.page ?? 1}`);
    queryParts.push(`pageSize=${params.pageSize ?? 10}`);
    const queryString = queryParts.join('&');
    return this.client.get<PagedResult<Project>>(`/api/projects/search?${queryString}`);
  }

  getByUserId(userId: string): Promise<ApiResponse<Project[]>> {
    return this.client.get<Project[]>(`/api/projects/user/${userId}`);
  }

  getByStatus(status: string): Promise<ApiResponse<Project[]>> {
    return this.client.get<Project[]>(`/api/projects/status/${status}`);
  }

  getByCategory(category: string): Promise<ApiResponse<Project[]>> {
    return this.client.get<Project[]>(`/api/projects/category/${category}`);
  }

  getFeatured(): Promise<ApiResponse<Project[]>> {
    return this.client.get<Project[]>(`/api/projects/featured`);
  }

  create(data: CreateProjectDto): Promise<ApiResponse<Project>> {
    return this.client.post<Project>("/api/projects", data);
  }

  update(id: string, data: UpdateProjectDto): Promise<ApiResponse<Project>> {
    return this.client.put<Project>(`/api/projects/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/projects/${id}`);
  }

  publish(id: string): Promise<ApiResponse<Project>> {
    return this.client.post<Project>(`/api/projects/${id}/publish`);
  }

  setFeatured(id: string, featured: boolean): Promise<ApiResponse<Project>> {
    return this.client.post<Project>(`/api/projects/${id}/featured?featured=${featured}`);
  }

  // ============ Resource Methods ============

  addResource(projectId: string, data: CreateResourceDto): Promise<ApiResponse<ProjectResource>> {
    return this.client.post<ProjectResource>(`/api/projects/${projectId}/resources`, data);
  }

  updateResource(resourceId: string, data: UpdateResourceDto): Promise<ApiResponse<ProjectResource>> {
    return this.client.put<ProjectResource>(`/api/project-resources/${resourceId}`, data);
  }

  deleteResource(resourceId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/project-resources/${resourceId}`);
  }

  getResources(projectId: string): Promise<ApiResponse<ProjectResource[]>> {
    return this.client.get<ProjectResource[]>(`/api/projects/${projectId}/resources`);
  }

  getResourceById(resourceId: string): Promise<ApiResponse<ProjectResource>> {
    return this.client.get<ProjectResource>(`/api/project-resources/${resourceId}`);
  }

  // ============ Milestone Methods ============

  addMilestone(projectId: string, data: CreateMilestoneDto): Promise<ApiResponse<ProjectMilestone>> {
    return this.client.post<ProjectMilestone>(`/api/projects/${projectId}/milestones`, data);
  }

  updateMilestone(projectId: string, milestoneId: string, data: UpdateMilestoneDto): Promise<ApiResponse<ProjectMilestone>> {
    return this.client.put<ProjectMilestone>(`/api/projects/${projectId}/milestones/${milestoneId}`, data);
  }

  deleteMilestone(projectId: string, milestoneId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/projects/${projectId}/milestones/${milestoneId}`);
  }

  getMilestones(projectId: string): Promise<ApiResponse<ProjectMilestone[]>> {
    return this.client.get<ProjectMilestone[]>(`/api/projects/${projectId}/milestones`);
  }

  getMilestoneById(projectId: string, milestoneId: string): Promise<ApiResponse<ProjectMilestone>> {
    return this.client.get<ProjectMilestone>(`/api/projects/${projectId}/milestones/${milestoneId}`);
  }

  completeMilestone(projectId: string, milestoneId: string): Promise<ApiResponse<ProjectMilestone>> {
    return this.client.post<ProjectMilestone>(`/api/projects/${projectId}/milestones/${milestoneId}/complete`);
  }

  // ============ Member Methods ============

  addMember(projectId: string, data: AddMemberDto): Promise<ApiResponse<ProjectMember>> {
    return this.client.post<ProjectMember>(`/api/projects/${projectId}/members`, data);
  }

  updateMember(memberId: string, data: UpdateMemberDto): Promise<ApiResponse<ProjectMember>> {
    return this.client.put<ProjectMember>(`/api/project-members/${memberId}`, data);
  }

  removeMember(memberId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/project-members/${memberId}`);
  }

  getMembers(projectId: string): Promise<ApiResponse<ProjectMember[]>> {
    return this.client.get<ProjectMember[]>(`/api/projects/${projectId}/members`);
  }

  getMemberById(memberId: string): Promise<ApiResponse<ProjectMember>> {
    return this.client.get<ProjectMember>(`/api/project-members/${memberId}`);
  }

  updateMemberRole(memberId: string, data: UpdateMemberDto): Promise<ApiResponse<ProjectMember>> {
    return this.updateMember(memberId, data);
  }

  // ============ Application Methods ============

  submitApplication(projectId: string, data: CreateApplicationDto): Promise<ApiResponse<ProjectApplication>> {
    return this.client.post<ProjectApplication>(`/api/projects/${projectId}/applications`, data);
  }

  getApplications(projectId: string): Promise<ApiResponse<ProjectApplication[]>> {
    return this.client.get<ProjectApplication[]>(`/api/projects/${projectId}/applications`);
  }

  reviewApplication(applicationId: string, data: ReviewApplicationDto): Promise<ApiResponse<ProjectApplication>> {
    return this.client.put<ProjectApplication>(`/api/project-applications/${applicationId}/review`, data);
  }

  getApplicationById(applicationId: string): Promise<ApiResponse<ProjectApplication>> {
    return this.client.get<ProjectApplication>(`/api/project-applications/${applicationId}`);
  }

  applyToProject(projectId: string, data: CreateApplicationDto & { userId: string }): Promise<ApiResponse<ProjectApplication>> {
    return this.submitApplication(projectId, data);
  }

  // ============ Proposal & Vote Methods ============

  createProposal(projectId: string, data: CreateProposalDto): Promise<ApiResponse<Proposal>> {
    // Normalize field names for backend compatibility
    const normalizedData = {
      ...data,
      creatorId: data.creatorId ?? data.createdById,
      votingEnd: data.votingEnd ?? (data.votingDays
        ? new Date(Date.now() + data.votingDays * 24 * 60 * 60 * 1000).toISOString()
        : undefined),
    };
    return this.client.post<Proposal>(`/api/projects/${projectId}/proposals`, normalizedData);
  }

  getProposals(projectId: string): Promise<ApiResponse<Proposal[]>> {
    return this.client.get<Proposal[]>(`/api/projects/${projectId}/proposals`);
  }

  getProposalById(proposalId: string): Promise<ApiResponse<Proposal>> {
    return this.client.get<Proposal>(`/api/proposals/${proposalId}`);
  }

  castVote(proposalId: string, data: CastVoteDto): Promise<ApiResponse<Vote>> {
    // Normalize field names for backend compatibility
    const normalizedData = {
      ...data,
      voterId: data.voterId ?? data.userId,
    };
    return this.client.post<Vote>(`/api/proposals/${proposalId}/votes`, normalizedData);
  }

  getVotes(proposalId: string): Promise<ApiResponse<Vote[]>> {
    return this.client.get<Vote[]>(`/api/proposals/${proposalId}/votes`);
  }

  closeProposal(proposalId: string): Promise<ApiResponse<Proposal>> {
    return this.client.post<Proposal>(`/api/proposals/${proposalId}/close`);
  }

  // ============ Update Methods ============

  createUpdate(projectId: string, data: CreateUpdateDto): Promise<ApiResponse<ProjectUpdate>> {
    // Normalize field names for backend compatibility
    const normalizedData = {
      ...data,
      userId: data.userId ?? data.createdById,
    };
    return this.client.post<ProjectUpdate>(`/api/projects/${projectId}/updates`, normalizedData);
  }

  getUpdates(projectId: string): Promise<ApiResponse<ProjectUpdate[]>> {
    return this.client.get<ProjectUpdate[]>(`/api/projects/${projectId}/updates`);
  }

  deleteUpdate(updateId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/project-updates/${updateId}`);
  }

  getUpdateById(updateId: string): Promise<ApiResponse<ProjectUpdate>> {
    return this.client.get<ProjectUpdate>(`/api/project-updates/${updateId}`);
  }

  // ============ Comment Methods ============

  addComment(projectId: string, data: CreateCommentDto): Promise<ApiResponse<ProjectComment>> {
    return this.client.post<ProjectComment>(`/api/projects/${projectId}/comments`, data);
  }

  getComments(projectId: string): Promise<ApiResponse<ProjectComment[]>> {
    return this.client.get<ProjectComment[]>(`/api/projects/${projectId}/comments`);
  }

  deleteComment(commentId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/project-comments/${commentId}`);
  }

  getCommentById(commentId: string): Promise<ApiResponse<ProjectComment>> {
    return this.client.get<ProjectComment>(`/api/project-comments/${commentId}`);
  }

  // ============ Support Methods ============

  supportProject(projectId: string, data: CreateSupportDto): Promise<ApiResponse<ProjectSupport>> {
    return this.client.post<ProjectSupport>(`/api/projects/${projectId}/support`, data);
  }

  cancelSupport(supportId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/project-support/${supportId}`);
  }

  getUserSupports(userId: string): Promise<ApiResponse<ProjectSupport[]>> {
    return this.client.get<ProjectSupport[]>(`/api/project-support/user/${userId}`);
  }

  getSupporters(projectId: string): Promise<ApiResponse<ProjectSupport[]>> {
    return this.client.get<ProjectSupport[]>(`/api/projects/${projectId}/supporters`);
  }

  getSupportById(supportId: string): Promise<ApiResponse<ProjectSupport>> {
    return this.client.get<ProjectSupport>(`/api/project-support/${supportId}`);
  }
}

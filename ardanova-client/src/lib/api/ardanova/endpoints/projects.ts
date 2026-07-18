import {
  type BaseApiClient,
  type ApiResponse,
  type PagedResult,
} from "../../base-client";
import type {
  CreateProjectDto as ProjectCreateContract,
  CreateProjectMilestoneDto as MilestoneCreateContract,
  ProjectCreatorDto,
  ProjectDto,
  ProjectDuration,
  ProjectMilestoneDto,
  ProjectRole,
  ProjectStatus,
  ProjectType,
  UpdateProjectDto as ProjectUpdateContract,
  UpdateProjectMilestoneDto as MilestoneUpdateContract,
} from "~/lib/contracts/project-contract";

// ============ Type Definitions ============

export type Project = ProjectDto;
export type CreateProjectDto = ProjectCreateContract;
export type UpdateProjectDto = ProjectUpdateContract;
export type ProjectMilestone = ProjectMilestoneDto;
export type CreateMilestoneDto = MilestoneCreateContract;
export type UpdateMilestoneDto = MilestoneUpdateContract;
export type {
  ProjectCreatorDto,
  ProjectDuration,
  ProjectRole,
  ProjectStatus,
  ProjectType,
  ProjectMilestoneDto,
};

export interface SearchProjectsParams {
  searchTerm?: string;
  status?: ProjectStatus;
  category?: string;
  projectType?: ProjectType;
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

export interface ProjectMember {
  id: string;
  projectId: string;
  userId: string;
  role: ProjectRole;
  shareBalance: number;
  votingPower: number;
  joinedAt: string;
  invitedById: string | null;
  user: {
    id: string;
    name: string | null;
    email: string | null;
    image: string | null;
  } | null;
}

export interface AddMemberDto {
  userId: string;
  role: ProjectRole;
  invitedById?: string | null;
}

export interface UpdateMemberDto {
  role?: ProjectRole;
  shareBalance?: number | null;
  votingPower?: number | null;
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

export type ProposalType =
  | "TREASURY"
  | "GOVERNANCE"
  | "STRATEGIC"
  | "OPERATIONAL"
  | "EMERGENCY"
  | "CONSTITUTIONAL"
  | "SHARES";

export type ProposalStatus =
  | "DRAFT"
  | "ACTIVE"
  | "PASSED"
  | "REJECTED"
  | "EXECUTED"
  | "CANCELLED"
  | "EXPIRED";

export interface Proposal {
  id: string;
  projectId: string;
  creatorId: string;
  type: ProposalType;
  title: string;
  description: string;
  options: string; // JSON string of options array
  quorum: number;
  threshold: number;
  status: ProposalStatus;
  votingStart: string | null;
  votingEnd: string | null;
  executionDelay: number | null;
  createdAt: string;
  updatedAt: string;
  creator: { id: string; name: string | null; image: string | null } | null;
  project: { id: string; title: string; slug: string } | null;
  votesCount: number;
  totalVotingPower: number;
}

export interface CreateProposalDto {
  projectId: string;
  creatorId: string;
  type: ProposalType;
  title: string;
  description: string;
  options: string;
  quorum: number;
  threshold: number;
  votingStart?: string | null;
  votingEnd?: string | null;
  executionDelay?: number | null;
}

export interface UpdateProposalDto {
  title?: string;
  description?: string;
  options?: string;
  quorum?: number;
  threshold?: number;
  votingStart?: string | null;
  votingEnd?: string | null;
  executionDelay?: number | null;
}

export interface ProposalComment {
  id: string;
  proposalId: string;
  userId: string;
  content: string;
  parentId: string | null;
  createdAt: string;
  updatedAt: string;
  user: { id: string; name: string | null; image: string | null } | null;
  replies: ProposalComment[] | null;
}

export interface CreateProposalCommentDto {
  proposalId: string;
  userId: string;
  content: string;
  parentId?: string | null;
}

export interface Vote {
  id: string;
  proposalId: string;
  voterId: string;
  choice: number;
  weight: number;
  reason: string | null;
  txHash: string | null;
  createdAt: string;
  voter: { id: string; name: string | null; image: string | null } | null;
}

export interface CastVoteDto {
  voterId: string;
  choice: number;
  weight?: number;
  reason?: string | null;
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
  parentId: string | null;
  targetType:
    | "PROJECT"
    | "MILESTONE"
    | "EPIC"
    | "SPRINT"
    | "FEATURE"
    | "PBI"
    | "TASK";
  targetId: string;
  createdAt: string;
  updatedAt: string;
  author: { id: string; name: string | null; image: string | null } | null;
}

export interface CreateCommentDto {
  projectId?: string;
  content: string;
  parentId?: string;
  targetType?: string;
  targetId?: string;
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

export interface ProjectInvitation {
  id: string;
  projectId: string;
  invitedById: string;
  invitedUserId: string;
  role: string;
  status: string;
  message?: string;
  createdAt: string;
  respondedAt?: string;
}

export interface CreateProjectInvitationDto {
  invitedById: string;
  invitedUserId: string;
  role: string;
  message?: string;
}

// ============ Projects Endpoint ============

export class ProjectsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Project>> {
    return this.client.get<Project>(`/api/projects/${encodeURIComponent(id)}`);
  }

  getBySlug(slug: string): Promise<ApiResponse<Project>> {
    return this.client.get<Project>(
      `/api/projects/slug/${encodeURIComponent(slug)}`,
    );
  }

  getAll(): Promise<ApiResponse<Project[]>> {
    return this.client.get<Project[]>(`/api/projects`);
  }

  getPaged(
    page = 1,
    pageSize = 10,
  ): Promise<ApiResponse<PagedResult<Project>>> {
    return this.client.get<PagedResult<Project>>(
      `/api/projects/paged?page=${page}&pageSize=${pageSize}`,
    );
  }

  search(
    params: SearchProjectsParams = {},
  ): Promise<ApiResponse<PagedResult<Project>>> {
    const queryParts: string[] = [];
    if (params.searchTerm)
      queryParts.push(`searchTerm=${encodeURIComponent(params.searchTerm)}`);
    if (params.status)
      queryParts.push(`status=${encodeURIComponent(params.status)}`);
    if (params.category)
      queryParts.push(`category=${encodeURIComponent(params.category)}`);
    if (params.projectType)
      queryParts.push(`projectType=${encodeURIComponent(params.projectType)}`);
    queryParts.push(`page=${params.page ?? 1}`);
    queryParts.push(`pageSize=${params.pageSize ?? 10}`);
    const queryString = queryParts.join("&");
    return this.client.get<PagedResult<Project>>(
      `/api/projects/search?${queryString}`,
    );
  }

  getByUserId(userId: string): Promise<ApiResponse<Project[]>> {
    return this.client.get<Project[]>(`/api/projects/user/${userId}`);
  }

  getByStatus(status: ProjectStatus): Promise<ApiResponse<Project[]>> {
    return this.client.get<Project[]>(
      `/api/projects/status/${encodeURIComponent(status)}`,
    );
  }

  getByCategory(category: string): Promise<ApiResponse<Project[]>> {
    return this.client.get<Project[]>(
      `/api/projects/category/${encodeURIComponent(category)}`,
    );
  }

  getByProjectType(projectType: ProjectType): Promise<ApiResponse<Project[]>> {
    return this.client.get<Project[]>(
      `/api/projects/type/${encodeURIComponent(projectType)}`,
    );
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
    return this.client.post<Project>(
      `/api/projects/${id}/featured?featured=${featured}`,
    );
  }

  // ============ Resource Methods ============

  addResource(
    projectId: string,
    data: CreateResourceDto,
  ): Promise<ApiResponse<ProjectResource>> {
    return this.client.post<ProjectResource>(
      `/api/projects/${projectId}/resources`,
      data,
    );
  }

  updateResource(
    projectId: string,
    resourceId: string,
    data: UpdateResourceDto,
  ): Promise<ApiResponse<ProjectResource>> {
    return this.client.put<ProjectResource>(
      `/api/projects/${projectId}/resources/${resourceId}`,
      data,
    );
  }

  deleteResource(
    projectId: string,
    resourceId: string,
  ): Promise<ApiResponse<void>> {
    return this.client.delete(
      `/api/projects/${projectId}/resources/${resourceId}`,
    );
  }

  getResources(projectId: string): Promise<ApiResponse<ProjectResource[]>> {
    return this.client.get<ProjectResource[]>(
      `/api/projects/${projectId}/resources`,
    );
  }

  getResourceById(
    projectId: string,
    resourceId: string,
  ): Promise<ApiResponse<ProjectResource>> {
    return this.client.get<ProjectResource>(
      `/api/projects/${projectId}/resources/${resourceId}`,
    );
  }

  // ============ Milestone Methods ============

  addMilestone(
    projectId: string,
    data: CreateMilestoneDto,
  ): Promise<ApiResponse<ProjectMilestone>> {
    return this.client.post<ProjectMilestone>(
      `/api/projects/${projectId}/milestones`,
      data,
    );
  }

  updateMilestone(
    projectId: string,
    milestoneId: string,
    data: UpdateMilestoneDto,
  ): Promise<ApiResponse<ProjectMilestone>> {
    return this.client.put<ProjectMilestone>(
      `/api/projects/${projectId}/milestones/${milestoneId}`,
      data,
    );
  }

  deleteMilestone(
    projectId: string,
    milestoneId: string,
  ): Promise<ApiResponse<void>> {
    return this.client.delete(
      `/api/projects/${projectId}/milestones/${milestoneId}`,
    );
  }

  getMilestones(projectId: string): Promise<ApiResponse<ProjectMilestone[]>> {
    return this.client.get<ProjectMilestone[]>(
      `/api/projects/${projectId}/milestones`,
    );
  }

  getMilestoneById(
    projectId: string,
    milestoneId: string,
  ): Promise<ApiResponse<ProjectMilestone>> {
    return this.client.get<ProjectMilestone>(
      `/api/projects/${projectId}/milestones/${milestoneId}`,
    );
  }

  completeMilestone(
    projectId: string,
    milestoneId: string,
  ): Promise<ApiResponse<ProjectMilestone>> {
    return this.client.post<ProjectMilestone>(
      `/api/projects/${projectId}/milestones/${milestoneId}/complete`,
    );
  }

  // ============ Member Methods ============

  addMember(
    projectId: string,
    data: AddMemberDto,
  ): Promise<ApiResponse<ProjectMember>> {
    return this.client.post<ProjectMember>(
      `/api/projects/${projectId}/members`,
      data,
    );
  }

  updateMember(
    projectId: string,
    memberId: string,
    data: UpdateMemberDto,
  ): Promise<ApiResponse<ProjectMember>> {
    return this.client.put<ProjectMember>(
      `/api/projects/${projectId}/members/${memberId}`,
      data,
    );
  }

  removeMember(
    projectId: string,
    memberId: string,
  ): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/projects/${projectId}/members/${memberId}`);
  }

  getMembers(projectId: string): Promise<ApiResponse<ProjectMember[]>> {
    return this.client.get<ProjectMember[]>(
      `/api/projects/${projectId}/members`,
    );
  }

  getMemberById(
    projectId: string,
    memberId: string,
  ): Promise<ApiResponse<ProjectMember>> {
    return this.client.get<ProjectMember>(
      `/api/projects/${projectId}/members/${memberId}`,
    );
  }

  updateMemberRole(
    projectId: string,
    memberId: string,
    data: UpdateMemberDto,
  ): Promise<ApiResponse<ProjectMember>> {
    return this.updateMember(projectId, memberId, data);
  }

  // ============ Application Methods ============

  submitApplication(
    projectId: string,
    data: CreateApplicationDto,
  ): Promise<ApiResponse<ProjectApplication>> {
    return this.client.post<ProjectApplication>(
      `/api/projects/${projectId}/applications`,
      data,
    );
  }

  getApplications(
    projectId: string,
  ): Promise<ApiResponse<ProjectApplication[]>> {
    return this.client.get<ProjectApplication[]>(
      `/api/projects/${projectId}/applications`,
    );
  }

  reviewApplication(
    projectId: string,
    applicationId: string,
    data: ReviewApplicationDto,
  ): Promise<ApiResponse<ProjectApplication>> {
    const body =
      data.reviewMessage !== undefined
        ? { reviewMessage: data.reviewMessage }
        : {};
    if (data.status === "ACCEPTED") {
      return this.client.post<ProjectApplication>(
        `/api/projects/${projectId}/applications/${applicationId}/accept`,
        body,
      );
    }
    if (data.status === "REJECTED") {
      return this.client.post<ProjectApplication>(
        `/api/projects/${projectId}/applications/${applicationId}/reject`,
        body,
      );
    }
    if (data.status === "WITHDRAWN") {
      return this.client.post<ProjectApplication>(
        `/api/projects/${projectId}/applications/${applicationId}/withdraw`,
      );
    }
    return Promise.resolve({
      data: undefined,
      error: `Unsupported application status for review: ${data.status}`,
      status: 400,
    });
  }

  getApplicationById(
    projectId: string,
    applicationId: string,
  ): Promise<ApiResponse<ProjectApplication>> {
    return this.client.get<ProjectApplication>(
      `/api/projects/${projectId}/applications/${applicationId}`,
    );
  }

  applyToProject(
    projectId: string,
    data: CreateApplicationDto & { userId: string },
  ): Promise<ApiResponse<ProjectApplication>> {
    return this.submitApplication(projectId, data);
  }

  // ============ Proposal & Vote Methods ============

  createProposal(
    projectId: string,
    data: CreateProposalDto,
  ): Promise<ApiResponse<Proposal>> {
    return this.client.post<Proposal>(
      `/api/projects/${projectId}/proposals`,
      data,
    );
  }

  getProposals(projectId: string): Promise<ApiResponse<PagedResult<Proposal>>> {
    return this.client.get<PagedResult<Proposal>>(
      `/api/projects/${projectId}/proposals`,
    );
  }

  getProposalById(
    projectId: string,
    proposalId: string,
  ): Promise<ApiResponse<Proposal>> {
    return this.client.get<Proposal>(
      `/api/projects/${projectId}/proposals/${proposalId}`,
    );
  }

  castVote(
    projectId: string,
    proposalId: string,
    data: CastVoteDto,
  ): Promise<ApiResponse<Vote>> {
    return this.client.post<Vote>(
      `/api/projects/${projectId}/proposals/${proposalId}/votes`,
      data,
    );
  }

  getVotes(
    projectId: string,
    proposalId: string,
  ): Promise<ApiResponse<Vote[]>> {
    return this.client.get<Vote[]>(
      `/api/projects/${projectId}/proposals/${proposalId}/votes`,
    );
  }

  closeProposal(
    projectId: string,
    proposalId: string,
  ): Promise<ApiResponse<Proposal>> {
    return this.client.post<Proposal>(
      `/api/projects/${projectId}/proposals/${proposalId}/cancel`,
    );
  }

  updateProposal(
    projectId: string,
    proposalId: string,
    data: UpdateProposalDto,
  ): Promise<ApiResponse<Proposal>> {
    return this.client.put<Proposal>(
      `/api/projects/${projectId}/proposals/${proposalId}`,
      data,
    );
  }

  publishProposal(
    projectId: string,
    proposalId: string,
  ): Promise<ApiResponse<Proposal>> {
    return this.client.post<Proposal>(
      `/api/projects/${projectId}/proposals/${proposalId}/publish`,
    );
  }

  getProposalComments(
    projectId: string,
    proposalId: string,
  ): Promise<ApiResponse<ProposalComment[]>> {
    return this.client.get<ProposalComment[]>(
      `/api/projects/${projectId}/proposals/${proposalId}/comments`,
    );
  }

  createProposalComment(
    projectId: string,
    proposalId: string,
    data: Omit<CreateProposalCommentDto, "proposalId">,
  ): Promise<ApiResponse<ProposalComment>> {
    return this.client.post<ProposalComment>(
      `/api/projects/${projectId}/proposals/${proposalId}/comments`,
      data,
    );
  }

  // ============ Update Methods ============

  createUpdate(
    projectId: string,
    data: CreateUpdateDto,
  ): Promise<ApiResponse<ProjectUpdate>> {
    // Normalize field names for backend compatibility
    const normalizedData = {
      ...data,
      userId: data.userId ?? data.createdById,
    };
    return this.client.post<ProjectUpdate>(
      `/api/projects/${projectId}/updates`,
      normalizedData,
    );
  }

  getUpdates(projectId: string): Promise<ApiResponse<ProjectUpdate[]>> {
    return this.client.get<ProjectUpdate[]>(
      `/api/projects/${projectId}/updates`,
    );
  }

  deleteUpdate(
    projectId: string,
    updateId: string,
  ): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/projects/${projectId}/updates/${updateId}`);
  }

  getUpdateById(
    projectId: string,
    updateId: string,
  ): Promise<ApiResponse<ProjectUpdate>> {
    return this.client.get<ProjectUpdate>(
      `/api/projects/${projectId}/updates/${updateId}`,
    );
  }

  // ============ Comment Methods ============

  addComment(
    projectId: string,
    data: CreateCommentDto,
  ): Promise<ApiResponse<ProjectComment>> {
    return this.client.post<ProjectComment>(
      `/api/projects/${projectId}/comments`,
      data,
    );
  }

  getComments(projectId: string): Promise<ApiResponse<ProjectComment[]>> {
    return this.client.get<ProjectComment[]>(
      `/api/projects/${projectId}/comments`,
    );
  }

  getCommentsByTarget(
    targetType: string,
    targetId: string,
  ): Promise<ApiResponse<ProjectComment[]>> {
    return this.client.get<ProjectComment[]>(
      `/api/projects/comments/target/${targetType}/${targetId}`,
    );
  }

  deleteComment(
    projectId: string,
    commentId: string,
  ): Promise<ApiResponse<void>> {
    return this.client.delete(
      `/api/projects/${projectId}/comments/${commentId}`,
    );
  }

  getCommentById(
    projectId: string,
    commentId: string,
  ): Promise<ApiResponse<ProjectComment>> {
    return this.client.get<ProjectComment>(
      `/api/projects/${projectId}/comments/${commentId}`,
    );
  }

  // ============ Support Methods ============

  supportProject(
    projectId: string,
    data: CreateSupportDto,
  ): Promise<ApiResponse<ProjectSupport>> {
    return this.client.post<ProjectSupport>(
      `/api/projects/${projectId}/support`,
      data,
    );
  }

  cancelSupport(
    projectId: string,
    supportId: string,
  ): Promise<ApiResponse<void>> {
    return this.client.delete(
      `/api/projects/${projectId}/support/${supportId}`,
    );
  }

  /** No dedicated UsersController route found; keep until backend exposes user support list. */
  getUserSupports(userId: string): Promise<ApiResponse<ProjectSupport[]>> {
    return this.client.get<ProjectSupport[]>(
      `/api/project-support/user/${userId}`,
    );
  }

  getSupporters(projectId: string): Promise<ApiResponse<ProjectSupport[]>> {
    return this.client.get<ProjectSupport[]>(
      `/api/projects/${projectId}/support`,
    );
  }

  getSupportById(
    projectId: string,
    supportId: string,
  ): Promise<ApiResponse<ProjectSupport>> {
    return this.client.get<ProjectSupport>(
      `/api/projects/${projectId}/support/${supportId}`,
    );
  }

  // ============ Invitation Methods ============

  getInvitations(projectId: string): Promise<ApiResponse<ProjectInvitation[]>> {
    return this.client.get<ProjectInvitation[]>(
      `/api/projects/${projectId}/invitations`,
    );
  }

  getInvitationsByUserId(
    userId: string,
  ): Promise<ApiResponse<ProjectInvitation[]>> {
    return this.client.get<ProjectInvitation[]>(
      `/api/project-invitations/user/${userId}`,
    );
  }

  createInvitation(
    projectId: string,
    data: CreateProjectInvitationDto,
  ): Promise<ApiResponse<ProjectInvitation>> {
    return this.client.post<ProjectInvitation>(
      `/api/projects/${projectId}/invitations`,
      data,
    );
  }

  acceptInvitation(
    projectId: string,
    invitationId: string,
  ): Promise<ApiResponse<ProjectInvitation>> {
    return this.client.post<ProjectInvitation>(
      `/api/projects/${projectId}/invitations/${invitationId}/accept`,
    );
  }

  rejectInvitation(
    projectId: string,
    invitationId: string,
  ): Promise<ApiResponse<ProjectInvitation>> {
    return this.client.post<ProjectInvitation>(
      `/api/projects/${projectId}/invitations/${invitationId}/reject`,
    );
  }
}

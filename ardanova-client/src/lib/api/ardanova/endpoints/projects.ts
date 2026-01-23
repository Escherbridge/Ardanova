import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

// ============ Type Definitions ============

export interface Project {
  id: string;
  title: string;
  slug: string;
  description: string;
  problemStatement: string;
  solution: string;
  category: string;
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
  category: string;
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
  category?: string;
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
}

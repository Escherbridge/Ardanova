import { type BaseApiClient, type ApiResponse } from "../../base-client";

export type FeatureStatus = "PLANNED" | "IN_PROGRESS" | "COMPLETED" | "CANCELLED";
export type FeaturePriority = "CRITICAL" | "HIGH" | "MEDIUM" | "LOW";

export interface Feature {
  id: string;
  projectId: string;
  sprintId?: string | null;
  epicId?: string | null;
  milestoneId?: string | null;
  guildId?: string | null;
  title: string;
  description?: string | null;
  status: FeatureStatus;
  priority: FeaturePriority;
  order?: number | null;
  assigneeId?: string | null;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface CreateFeature {
  sprintId: string;
  title: string;
  description?: string;
  priority?: FeaturePriority;
  [key: string]: unknown;
}

export interface UpdateFeature {
  title?: string;
  description?: string;
  priority?: FeaturePriority;
  status?: FeatureStatus;
  assigneeId?: string | null;
  [key: string]: unknown;
}

export class FeaturesEndpoint {
  constructor(private client: BaseApiClient) {}

  getBySprintId(sprintId: string): Promise<ApiResponse<Feature[]>> {
    return this.client.get<Feature[]>(`/api/sprints/${sprintId}/features`);
  }

  getById(id: string): Promise<ApiResponse<Feature>> {
    return this.client.get<Feature>(`/api/features/${id}`);
  }

  create(data: CreateFeature): Promise<ApiResponse<Feature>> {
    return this.client.post<Feature>("/api/features", data);
  }

  update(id: string, data: UpdateFeature): Promise<ApiResponse<Feature>> {
    return this.client.put<Feature>(`/api/features/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/features/${id}`);
  }

  assign(id: string, userId: string | null): Promise<ApiResponse<Feature>> {
    return this.client.put<Feature>(`/api/features/${id}/assign`, { userId });
  }

  updateStatus(id: string, status: FeatureStatus): Promise<ApiResponse<Feature>> {
    return this.client.put<Feature>(`/api/features/${id}/status`, { status });
  }

  updatePriority(id: string, priority: FeaturePriority): Promise<ApiResponse<Feature>> {
    return this.client.put<Feature>(`/api/features/${id}/priority`, { priority });
  }
}

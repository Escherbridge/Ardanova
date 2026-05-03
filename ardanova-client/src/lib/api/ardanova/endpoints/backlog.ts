import { type BaseApiClient, type ApiResponse } from "../../base-client";

// ============ Type Definitions ============

// These string unions mirror the backend enums:
// - PBIType
// - PBIStatus
// - TaskPriority (exposed here as PbiPriority)

export type PbiType = "FEATURE" | "ENHANCEMENT" | "BUG" | "TECHNICAL_DEBT" | "SPIKE";

export type PbiStatus =
  | "NEW"
  | "READY"
  | "IN_PROGRESS"
  | "DONE"
  | "CANCELLED";

export type PbiPriority = "CRITICAL" | "HIGH" | "MEDIUM" | "LOW";

export interface ProductBacklogItem {
  id: string;
  projectId: string;
  featureId?: string | null;
  sprintId?: string | null;
  epicId?: string | null;
  milestoneId?: string | null;
  guildId?: string | null;
  title: string;
  description?: string | null;
  type: PbiType;
  storyPoints?: number | null;
  status: PbiStatus;
  acceptanceCriteria?: string | null;
  priority: PbiPriority;
  assigneeId?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductBacklogItem {
  projectId: string;
  title: string;
  featureId?: string;
  sprintId?: string;
  epicId?: string;
  milestoneId?: string;
  guildId?: string;
  description?: string;
  type?: PbiType;
  storyPoints?: number;
  acceptanceCriteria?: string;
  priority?: PbiPriority;
  assigneeId?: string | null;
}

export interface UpdateProductBacklogItem {
  title?: string;
  description?: string;
  type?: PbiType;
  storyPoints?: number;
  status?: PbiStatus;
  acceptanceCriteria?: string;
  priority?: PbiPriority;
  assigneeId?: string | null;
}

// ============ Backlog Endpoint ============

export class BacklogEndpoint {
  constructor(private client: BaseApiClient) {}

  /**
   * Get all PBIs for a given feature.
   * Maps to GET /api/features/{featureId}/product-backlog-items
   */
  getPbisByFeatureId(
    featureId: string,
  ): Promise<ApiResponse<ProductBacklogItem[]>> {
    return this.client.get<ProductBacklogItem[]>(
      `/api/features/${featureId}/product-backlog-items`,
    );
  }

  /**
   * Get all PBIs for a given project.
   * Maps to GET /api/projects/{projectId}/product-backlog-items
   */
  getPbisByProjectId(
    projectId: string,
  ): Promise<ApiResponse<ProductBacklogItem[]>> {
    return this.client.get<ProductBacklogItem[]>(
      `/api/projects/${projectId}/product-backlog-items`,
    );
  }

  /**
   * Get a single PBI by id.
   * Maps to GET /api/product-backlog-items/{id}
   */
  getPbiById(id: string): Promise<ApiResponse<ProductBacklogItem>> {
    return this.client.get<ProductBacklogItem>(`/api/product-backlog-items/${id}`);
  }

  /**
   * Create a new PBI.
   * Maps to POST /api/product-backlog-items
   */
  createPbi(
    data: CreateProductBacklogItem,
  ): Promise<ApiResponse<ProductBacklogItem>> {
    return this.client.post<ProductBacklogItem>(
      "/api/product-backlog-items",
      data,
    );
  }

  /**
   * Update an existing PBI.
   * Maps to PUT /api/product-backlog-items/{id}
   */
  updatePbi(
    id: string,
    data: UpdateProductBacklogItem,
  ): Promise<ApiResponse<ProductBacklogItem>> {
    return this.client.put<ProductBacklogItem>(
      `/api/product-backlog-items/${id}`,
      data,
    );
  }

  /**
   * Delete a PBI.
   * Maps to DELETE /api/product-backlog-items/{id}
   */
  deletePbi(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/product-backlog-items/${id}`);
  }

  /**
   * Assign or unassign a PBI to a user.
   * Maps to PUT /api/product-backlog-items/{id}/assign
   */
  assignPbi(
    id: string,
    userId: string | null,
  ): Promise<ApiResponse<ProductBacklogItem>> {
    return this.client.put<ProductBacklogItem>(
      `/api/product-backlog-items/${id}/assign`,
      { userId },
    );
  }

  /**
   * Update the status of a PBI.
   * Maps to PUT /api/product-backlog-items/{id}/status
   */
  updatePbiStatus(
    id: string,
    status: PbiStatus,
  ): Promise<ApiResponse<ProductBacklogItem>> {
    return this.client.put<ProductBacklogItem>(
      `/api/product-backlog-items/${id}/status`,
      { status },
    );
  }
}


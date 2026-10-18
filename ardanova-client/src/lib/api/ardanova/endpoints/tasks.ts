import { apiPath } from "../../path";
import {
  type BaseApiClient,
  type ApiResponse,
  type PagedResult,
} from "../../base-client";

export type TaskStatus =
  | "TODO"
  | "IN_PROGRESS"
  | "REVIEW"
  | "COMPLETED"
  | "BLOCKED";
export type TaskPriority = "LOW" | "MEDIUM" | "HIGH" | "URGENT";
export type TaskType =
  | "FEATURE"
  | "BUG"
  | "ENHANCEMENT"
  | "DOCUMENTATION"
  | "RESEARCH"
  | "DESIGN"
  | "TESTING"
  | "REVIEW"
  | "MAINTENANCE"
  | "OTHER";
export type EscrowStatus =
  | "NONE"
  | "FUNDED"
  | "RELEASED"
  | "DISPUTED"
  | "REFUNDED";

export interface TaskUser {
  id: string;
  name?: string | null;
  image?: string | null;
}

export interface TaskProject {
  id: string;
  title: string;
  slug: string;
}

export interface Task {
  id: string;
  projectId: string;
  pbiId?: string | null;
  featureId?: string | null;
  sprintId?: string | null;
  epicId?: string | null;
  milestoneId?: string | null;
  guildId?: string | null;
  title: string;
  description?: string | null;
  opportunityId?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  taskType: TaskType;
  estimatedHours?: number | null;
  actualHours?: number | null;
  equityReward?: number | null;
  escrowStatus: EscrowStatus;
  dueDate?: string | null;
  completedAt?: string | null;
  createdAt: string;
  updatedAt: string;
  assignedToId?: string | null;
  assignedTo?: TaskUser | null;
  project?: TaskProject | null;
}

export interface TaskCommerceView {
  taskId: string;
  agreementId: string;
  title: string;
  description?: string | null;
  assetCode: string;
  awardAmount: number;
  scale: number;
  agreementStatus: string;
  escrowStatus: string;
  /**
   * When the worker's own window ends and the award can be moved. Null until the owner has
   * approved the completed work. This is a tax-deferral window - the award is theirs, and it
   * is restricted until this date so they are not taxed before they can realise it. It is
   * never a refund window: reward units come from the platform reserve, not from a card.
   */
  workerHoldReleasesAt?: string | null;
  /** True while the award is held and cannot yet be moved. */
  isRestricted?: boolean;
}

export interface CreateTaskDto {
  projectId: string;
  title: string;
  description?: string;
  priority?: TaskPriority;
  taskType?: TaskType;
  assignedToId?: string | null;
  pbiId?: string;
  featureId?: string;
  sprintId?: string;
  epicId?: string;
  milestoneId?: string;
  guildId?: string;
  estimatedHours?: number;
  dueDate?: string;
  equityReward?: number;
}

export interface UpdateTaskDto {
  title?: string;
  description?: string;
  priority?: TaskPriority;
  taskType?: TaskType;
  status?: TaskStatus;
  estimatedHours?: number;
  actualHours?: number;
  dueDate?: string;
  equityReward?: number;
}

export interface SearchTasksParams {
  searchTerm?: string;
  status?: TaskStatus;
  priority?: TaskPriority;
  taskType?: TaskType;
  projectId?: string;
  page?: number;
  pageSize?: number;
}

export class TasksEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Task>> {
    return this.client.get<Task>(apiPath`/api/tasks/${id}`);
  }

  getCommerce(id: string): Promise<ApiResponse<TaskCommerceView>> {
    return this.client.get<TaskCommerceView>(apiPath`/api/task-commerce/${id}`);
  }

  getMine(): Promise<ApiResponse<Task[]>> {
    return this.client.get<Task[]>("/api/tasks/me");
  }

  search(
    params: SearchTasksParams = {},
  ): Promise<ApiResponse<PagedResult<Task>>> {
    const sp = new URLSearchParams();
    if (params.searchTerm) sp.set("searchTerm", params.searchTerm);
    if (params.status) sp.set("status", params.status);
    if (params.priority) sp.set("priority", params.priority);
    if (params.taskType) sp.set("taskType", params.taskType);
    if (params.projectId) sp.set("projectId", params.projectId);
    sp.set("page", String(params.page ?? 1));
    sp.set("pageSize", String(params.pageSize ?? 10));
    return this.client.get<PagedResult<Task>>(apiPath`/api/tasks/search?${sp}`);
  }

  create(data: CreateTaskDto): Promise<ApiResponse<Task>> {
    return this.client.post<Task>("/api/tasks", data);
  }

  update(id: string, data: UpdateTaskDto): Promise<ApiResponse<Task>> {
    return this.client.put<Task>(apiPath`/api/tasks/${id}`, data);
  }

  updateStatus(id: string, status: TaskStatus): Promise<ApiResponse<Task>> {
    return this.client.patch<Task>(apiPath`/api/tasks/${id}/status`, {
      status,
    });
  }

  /** The only assignment boundary: a member's user id assigns, null unassigns. */
  assign(id: string, userId: string | null): Promise<ApiResponse<Task>> {
    return this.client.put<Task>(apiPath`/api/tasks/${id}/assign`, {
      userId,
    });
  }

  getByPbiId(pbiId: string): Promise<ApiResponse<Task[]>> {
    return this.client.get<Task[]>(apiPath`/api/tasks/pbi/${pbiId}`);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(apiPath`/api/tasks/${id}`);
  }
}

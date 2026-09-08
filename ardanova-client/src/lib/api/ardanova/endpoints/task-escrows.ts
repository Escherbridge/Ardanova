import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type { TaskEscrowDto } from "~/lib/contracts/task-escrow-contract";

export type TaskEscrow = TaskEscrowDto;

export interface CreateTaskEscrowDto {
  taskId: string;
  shareId: string;
  amount: number;
}

export interface RefundEscrowDto {
  reason?: string;
}

export interface DisputeEscrowDto {
  reason: "SCOPE_DISPUTE" | "QUALITY_ISSUE" | "NON_DELIVERY" | "OTHER";
  description: string;
}

export interface ResolveDisputeDto {
  outcome: "REFUND" | "REJECT";
  resolvedByUserId: string;
}

export class TaskEscrowsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<TaskEscrow>> {
    return this.client.get<TaskEscrow>(
      `/api/taskescrows/${encodeURIComponent(id)}`,
    );
  }

  getByTaskId(taskId: string): Promise<ApiResponse<TaskEscrow>> {
    return this.client.get<TaskEscrow>(
      `/api/taskescrows/task/${encodeURIComponent(taskId)}`,
    );
  }

  getMine(): Promise<ApiResponse<TaskEscrow[]>> {
    return this.client.get<TaskEscrow[]>("/api/taskescrows/me");
  }

  /** Admin-credentialed: every disputed escrow, across every funder. */
  getAllDisputed(): Promise<ApiResponse<TaskEscrow[]>> {
    return this.client.get<TaskEscrow[]>("/api/taskescrows/disputed");
  }

  create(data: CreateTaskEscrowDto): Promise<ApiResponse<TaskEscrow>> {
    return this.client.post<TaskEscrow>("/api/taskescrows", data);
  }

  /**
   * Authorizes release. Takes no body: release evidence is the hold's own confirmed state on
   * the node, read back after the call, never a chain hash a form asked someone to transcribe.
   */
  release(id: string): Promise<ApiResponse<TaskEscrow>> {
    return this.client.post<TaskEscrow>(
      `/api/taskescrows/${encodeURIComponent(id)}/release`,
      {},
    );
  }

  dispute(
    id: string,
    data: DisputeEscrowDto,
  ): Promise<ApiResponse<TaskEscrow>> {
    return this.client.post<TaskEscrow>(
      `/api/taskescrows/${encodeURIComponent(id)}/dispute`,
      data,
    );
  }

  refund(id: string, data: RefundEscrowDto): Promise<ApiResponse<TaskEscrow>> {
    return this.client.post<TaskEscrow>(
      `/api/taskescrows/${encodeURIComponent(id)}/refund`,
      data,
    );
  }

  /** Admin-credentialed dispute adjudication. See TaskEscrowsController.Resolve. */
  resolve(
    id: string,
    data: ResolveDisputeDto,
  ): Promise<ApiResponse<TaskEscrow>> {
    return this.client.post<TaskEscrow>(
      `/api/taskescrows/${encodeURIComponent(id)}/resolve`,
      data,
    );
  }
}

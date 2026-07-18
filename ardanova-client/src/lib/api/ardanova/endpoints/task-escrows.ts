import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type { TaskEscrowDto } from "~/lib/contracts/task-escrow-contract";

export type TaskEscrow = TaskEscrowDto;

export interface CreateTaskEscrowDto {
  taskId: string;
  shareId: string;
  amount: number;
  txHashFund?: string;
}

export interface ReleaseEscrowDto {
  txHash?: string;
}

export interface RefundEscrowDto {
  txHash?: string;
}

export interface DisputeEscrowDto {
  reason: "SCOPE_DISPUTE" | "QUALITY_ISSUE" | "NON_DELIVERY" | "OTHER";
  description: string;
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

  create(data: CreateTaskEscrowDto): Promise<ApiResponse<TaskEscrow>> {
    return this.client.post<TaskEscrow>("/api/taskescrows", data);
  }

  release(
    id: string,
    data: ReleaseEscrowDto,
  ): Promise<ApiResponse<TaskEscrow>> {
    return this.client.post<TaskEscrow>(
      `/api/taskescrows/${encodeURIComponent(id)}/release`,
      data,
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
}

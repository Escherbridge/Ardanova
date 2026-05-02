import { type BaseApiClient, type ApiResponse } from "../../base-client";

export type ProjectTokenStatus = "PENDING" | "ACTIVE" | "FROZEN" | "DISSOLVED";
export type ProjectGateStatus = "FUNDING" | "ACTIVE" | "SUCCEEDED" | "FAILED";
export type TokenHolderClass = "CONTRIBUTOR" | "INVESTOR" | "FOUNDER";
export type AllocationStatus = "RESERVED" | "DISTRIBUTED" | "REVOKED" | "BURNED";

export interface ProjectTokenConfigDto {
  id: string;
  projectId: string;
  [key: string]: unknown;
}

export interface CreateProjectTokenConfigDto {
  projectId: string;
  totalSupply: number;
  fundingGoal: number;
  unitName: string;
  assetName?: string;
  successCriteria?: string;
}

export interface TokenAllocationDto {
  id: string;
  [key: string]: unknown;
}

export interface CreateTokenAllocationDto {
  taskId: string;
  equityPercentage: number;
}

export interface CreateInvestorAllocationDto {
  userId: string;
  usdAmount: number;
}

export interface CreateFounderAllocationDto {
  userId: string;
  equityPercentage: number;
}

export interface ProjectInvestmentDto {
  id: string;
  [key: string]: unknown;
}

export interface ProjectGateStatusDto {
  configId: string;
  status: ProjectGateStatus;
  [key: string]: unknown;
}

export interface GateTransitionResultDto {
  [key: string]: unknown;
}

export interface FailProjectDto {
  reason: string;
}

export class ProjectTokensEndpoint {
  constructor(private client: BaseApiClient) {}

  createConfig(dto: CreateProjectTokenConfigDto): Promise<ApiResponse<ProjectTokenConfigDto>> {
    return this.client.post<ProjectTokenConfigDto>("/api/ProjectTokens/config", dto);
  }

  getConfig(id: string): Promise<ApiResponse<ProjectTokenConfigDto>> {
    return this.client.get<ProjectTokenConfigDto>(`/api/ProjectTokens/config/${encodeURIComponent(id)}`);
  }

  getConfigByProject(projectId: string): Promise<ApiResponse<ProjectTokenConfigDto>> {
    return this.client.get<ProjectTokenConfigDto>(
      `/api/ProjectTokens/config/by-project/${encodeURIComponent(projectId)}`,
    );
  }

  getSupply(id: string): Promise<ApiResponse<unknown>> {
    return this.client.get<unknown>(`/api/ProjectTokens/config/${encodeURIComponent(id)}/supply`);
  }

  allocateToTask(configId: string, dto: CreateTokenAllocationDto): Promise<ApiResponse<TokenAllocationDto>> {
    return this.client.post<TokenAllocationDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/allocate/task`,
      dto,
    );
  }

  allocateToInvestor(configId: string, dto: CreateInvestorAllocationDto): Promise<ApiResponse<TokenAllocationDto>> {
    return this.client.post<TokenAllocationDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/allocate/investor`,
      dto,
    );
  }

  allocateToFounder(configId: string, dto: CreateFounderAllocationDto): Promise<ApiResponse<TokenAllocationDto>> {
    return this.client.post<TokenAllocationDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/allocate/founder`,
      dto,
    );
  }

  distribute(allocationId: string, recipientUserId: string): Promise<ApiResponse<unknown>> {
    const q = `?recipientUserId=${encodeURIComponent(recipientUserId)}`;
    return this.client.post<unknown>(`/api/ProjectTokens/allocations/${encodeURIComponent(allocationId)}/distribute${q}`);
  }

  revoke(allocationId: string): Promise<ApiResponse<unknown>> {
    return this.client.post<unknown>(`/api/ProjectTokens/allocations/${encodeURIComponent(allocationId)}/revoke`, {});
  }

  getAllocations(configId: string): Promise<ApiResponse<TokenAllocationDto[]>> {
    return this.client.get<TokenAllocationDto[]>(`/api/ProjectTokens/${encodeURIComponent(configId)}/allocations`);
  }

  getAllocationsByTask(taskId: string): Promise<ApiResponse<TokenAllocationDto[]>> {
    return this.client.get<TokenAllocationDto[]>(
      `/api/ProjectTokens/allocations/by-task/${encodeURIComponent(taskId)}`,
    );
  }

  getInvestors(configId: string): Promise<ApiResponse<ProjectInvestmentDto[]>> {
    return this.client.get<ProjectInvestmentDto[]>(`/api/ProjectTokens/${encodeURIComponent(configId)}/investors`);
  }

  getGateStatus(configId: string): Promise<ApiResponse<ProjectGateStatusDto>> {
    return this.client.get<ProjectGateStatusDto>(`/api/ProjectTokens/${encodeURIComponent(configId)}/gate`);
  }

  evaluateGate(configId: string): Promise<ApiResponse<GateTransitionResultDto>> {
    return this.client.post<GateTransitionResultDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/gate/evaluate`,
      {},
    );
  }

  clearGate(configId: string, verifiedByUserId: string): Promise<ApiResponse<GateTransitionResultDto>> {
    const q = `?verifiedByUserId=${encodeURIComponent(verifiedByUserId)}`;
    return this.client.post<GateTransitionResultDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/gate/clear${q}`,
      {},
    );
  }

  failProject(configId: string, dto: FailProjectDto): Promise<ApiResponse<unknown>> {
    return this.client.post<unknown>(`/api/ProjectTokens/${encodeURIComponent(configId)}/gate/fail`, dto);
  }

  burnFounder(configId: string): Promise<ApiResponse<unknown>> {
    return this.client.post<unknown>(`/api/ProjectTokens/${encodeURIComponent(configId)}/burn-founder`, {});
  }

  trustProtection(configId: string): Promise<ApiResponse<unknown>> {
    return this.client.post<unknown>(`/api/ProjectTokens/${encodeURIComponent(configId)}/trust-protection`, {});
  }
}

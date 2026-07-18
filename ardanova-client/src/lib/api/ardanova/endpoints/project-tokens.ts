import type {
  CreateFounderAllocationDto,
  CreateInvestorAllocationDto,
  CreateProjectTokenConfigDto,
  CreateTokenAllocationDto,
  FailProjectDto,
  GateTransitionResultDto,
  ProjectGateStatusDto,
  ProjectInvestmentDto,
  ProjectTokenMetadataBatchDto,
  ProjectTokenConfigDto,
  TokenAllocationDto,
} from "~/lib/contracts/tokenomics-contract";
import { type ApiResponse, type BaseApiClient } from "../../base-client";

export type {
  AllocationStatus,
  CreateFounderAllocationDto,
  CreateInvestorAllocationDto,
  CreateProjectTokenConfigDto,
  CreateTokenAllocationDto,
  FailProjectDto,
  GateTransitionResultDto,
  ProjectGateStatus,
  ProjectGateStatusDto,
  ProjectInvestmentDto,
  ProjectTokenMetadataBatchDto,
  ProjectTokenConfigDto,
  ProjectTokenStatus,
  TokenAllocationDto,
  TokenHolderClass,
} from "~/lib/contracts/tokenomics-contract";

export class ProjectTokensEndpoint {
  constructor(private client: BaseApiClient) {}

  createConfig(
    dto: CreateProjectTokenConfigDto,
  ): Promise<ApiResponse<ProjectTokenConfigDto>> {
    return this.client.post<ProjectTokenConfigDto>(
      "/api/ProjectTokens/config",
      dto,
    );
  }

  getConfig(id: string): Promise<ApiResponse<ProjectTokenConfigDto>> {
    return this.client.get<ProjectTokenConfigDto>(
      `/api/ProjectTokens/config/${encodeURIComponent(id)}`,
    );
  }

  getMetadata(
    ids: readonly string[],
  ): Promise<ApiResponse<ProjectTokenMetadataBatchDto>> {
    return this.client.post<ProjectTokenMetadataBatchDto>(
      "/api/ProjectTokens/config/metadata/batch",
      { ids },
    );
  }

  getConfigByProject(
    projectId: string,
  ): Promise<ApiResponse<ProjectTokenConfigDto>> {
    return this.client.get<ProjectTokenConfigDto>(
      `/api/ProjectTokens/config/by-project/${encodeURIComponent(projectId)}`,
    );
  }

  getSupply(id: string): Promise<ApiResponse<ProjectTokenConfigDto>> {
    return this.client.get<ProjectTokenConfigDto>(
      `/api/ProjectTokens/config/${encodeURIComponent(id)}/supply`,
    );
  }

  allocateToPbi(
    configId: string,
    dto: CreateTokenAllocationDto,
  ): Promise<ApiResponse<TokenAllocationDto>> {
    return this.client.post<TokenAllocationDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/allocate/pbi`,
      dto,
    );
  }

  allocateToInvestor(
    configId: string,
    dto: CreateInvestorAllocationDto,
  ): Promise<ApiResponse<TokenAllocationDto>> {
    return this.client.post<TokenAllocationDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/allocate/investor`,
      dto,
    );
  }

  allocateToFounder(
    configId: string,
    dto: CreateFounderAllocationDto,
  ): Promise<ApiResponse<TokenAllocationDto>> {
    return this.client.post<TokenAllocationDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/allocate/founder`,
      dto,
    );
  }

  distribute(
    allocationId: string,
    recipientUserId: string,
  ): Promise<ApiResponse<TokenAllocationDto>> {
    const query = `?recipientUserId=${encodeURIComponent(recipientUserId)}`;
    return this.client.post<TokenAllocationDto>(
      `/api/ProjectTokens/allocations/${encodeURIComponent(allocationId)}/distribute${query}`,
    );
  }

  revoke(allocationId: string): Promise<ApiResponse<TokenAllocationDto>> {
    return this.client.post<TokenAllocationDto>(
      `/api/ProjectTokens/allocations/${encodeURIComponent(allocationId)}/revoke`,
      {},
    );
  }

  getAllocations(configId: string): Promise<ApiResponse<TokenAllocationDto[]>> {
    return this.client.get<TokenAllocationDto[]>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/allocations`,
    );
  }

  getAllocationsByPbi(
    pbiId: string,
  ): Promise<ApiResponse<TokenAllocationDto[]>> {
    return this.client.get<TokenAllocationDto[]>(
      `/api/ProjectTokens/allocations/by-pbi/${encodeURIComponent(pbiId)}`,
    );
  }

  getInvestors(configId: string): Promise<ApiResponse<ProjectInvestmentDto[]>> {
    return this.client.get<ProjectInvestmentDto[]>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/investors`,
    );
  }

  getGateStatus(configId: string): Promise<ApiResponse<ProjectGateStatusDto>> {
    return this.client.get<ProjectGateStatusDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/gate`,
    );
  }

  evaluateGate(
    configId: string,
  ): Promise<ApiResponse<GateTransitionResultDto>> {
    return this.client.post<GateTransitionResultDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/gate/evaluate`,
      {},
    );
  }

  clearGate(
    configId: string,
    verifiedByUserId: string,
  ): Promise<ApiResponse<GateTransitionResultDto>> {
    const query = `?verifiedByUserId=${encodeURIComponent(verifiedByUserId)}`;
    return this.client.post<GateTransitionResultDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/gate/clear${query}`,
      {},
    );
  }

  failProject(
    configId: string,
    dto: FailProjectDto,
  ): Promise<ApiResponse<GateTransitionResultDto>> {
    return this.client.post<GateTransitionResultDto>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/gate/fail`,
      dto,
    );
  }

  burnFounder(configId: string): Promise<ApiResponse<boolean>> {
    return this.client.post<boolean>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/burn-founder`,
      {},
    );
  }

  trustProtection(configId: string): Promise<ApiResponse<boolean>> {
    return this.client.post<boolean>(
      `/api/ProjectTokens/${encodeURIComponent(configId)}/trust-protection`,
      {},
    );
  }
}

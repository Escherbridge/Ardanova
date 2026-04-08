import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type { FailProjectDto, GateTransitionResultDto, ProjectGateStatusDto } from "./project-tokens";

/** Thin client for gate-only flows; routes map to `ProjectTokensController` gate actions. */
export class ProjectGatesEndpoint {
  constructor(private client: BaseApiClient) {}

  getStatus(configId: string): Promise<ApiResponse<ProjectGateStatusDto>> {
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
}

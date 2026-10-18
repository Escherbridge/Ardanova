import { apiPath, query } from "../../path";
import { type BaseApiClient, type ApiResponse } from "../../base-client";
import type {
  FailProjectDto,
  GateTransitionResultDto,
  ProjectGateStatusDto,
} from "./project-tokens";

/** Thin client for gate-only flows; routes map to `ProjectTokensController` gate actions. */
export class ProjectGatesEndpoint {
  constructor(private client: BaseApiClient) {}

  getStatus(configId: string): Promise<ApiResponse<ProjectGateStatusDto>> {
    return this.client.get<ProjectGateStatusDto>(
      apiPath`/api/ProjectTokens/${configId}/gate`,
    );
  }

  evaluateGate(
    configId: string,
  ): Promise<ApiResponse<GateTransitionResultDto>> {
    return this.client.post<GateTransitionResultDto>(
      apiPath`/api/ProjectTokens/${configId}/gate/evaluate`,
      {},
    );
  }

  clearGate(
    configId: string,
    verifiedByUserId: string,
  ): Promise<ApiResponse<GateTransitionResultDto>> {
    const q = query({ verifiedByUserId });
    return this.client.post<GateTransitionResultDto>(
      apiPath`/api/ProjectTokens/${configId}/gate/clear${q}`,
      {},
    );
  }

  failProject(
    configId: string,
    dto: FailProjectDto,
  ): Promise<ApiResponse<GateTransitionResultDto>> {
    return this.client.post<GateTransitionResultDto>(
      apiPath`/api/ProjectTokens/${configId}/gate/fail`,
      dto,
    );
  }
}

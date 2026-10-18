import { type ApiResponse, type BaseApiClient } from "../../base-client";

export interface NovaTurn {
  question: string;
  answer: string;
}

export interface NovaRefineRequest {
  idea: string;
  turns: NovaTurn[];
  state?: string;
  sessionId?: string;
}

export interface NovaSource {
  title: string;
  url: string;
}

export interface NovaVerdict {
  /** go | adjust | not_yet, or "preview" from the local preview (nothing assessed). */
  outcome: string;
  headline: string;
  summary: string;
  strengths: string[];
  risks: string[];
  nextStep: string;
  sources: NovaSource[];
}

export interface NovaBrief {
  workingName: string;
  oneLiner: string;
  customer: string;
  problem: string;
  offer: string;
  pricing: string;
  channels: string[];
  milestones: string[];
  metrics: string[];
}

export interface NovaRefineResponse {
  type: "question" | "verdict";
  assistant: string;
  /** "live" answered by Nova AI; "local-preview" fixed practice questions, nothing analysed. */
  mode: "live" | "local-preview";
  question?: string | null;
  number?: number | null;
  maxQuestions?: number | null;
  state?: string | null;
  sessionId?: string | null;
  verdict?: NovaVerdict | null;
  vertical?: string | null;
  brief?: NovaBrief | null;
  /** Live refinements this member can still start today; null when nothing is counted (local preview). */
  remainingToday?: number | null;
}

/** A refinement turn may take the upstream's retry-and-fallback path (~88 s); .NET gives up at 95 s, so wait for its answer. */
export const NOVA_TURN_TIMEOUT_MS = 100_000;

export interface NovaStatus {
  assistant: string;
  /** "live", "local-preview" (fixed practice questions) or "off" (not connected on this deployment). */
  refinement: "live" | "local-preview" | "off";
  /** New live refinements a member may start per UTC day; null unless live. */
  dailyLimit?: number | null;
  /** How many of those this member has left today; null unless live. */
  remainingToday?: number | null;
}

/** Actor-signed Nova AI routes; .NET is the only caller of the upstream service. */
export class NovaEndpoint {
  constructor(private readonly client: BaseApiClient) {}

  status(): Promise<ApiResponse<NovaStatus>> {
    return this.client.get<NovaStatus>("/api/nova/status");
  }

  refineProject(
    request: NovaRefineRequest,
  ): Promise<ApiResponse<NovaRefineResponse>> {
    return this.client.post<NovaRefineResponse>(
      "/api/nova/project-refinement",
      request,
    );
  }
}

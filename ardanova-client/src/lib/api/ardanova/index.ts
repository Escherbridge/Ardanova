import { env } from "~/env";
import { BaseApiClient, type BaseApiClientConfig } from "../base-client";
import { UsersEndpoint } from "./endpoints/users";
import { ProjectsEndpoint } from "./endpoints/projects";
import { GuildsEndpoint } from "./endpoints/guilds";

// Re-export types from endpoints
export type { User, CreateUserDto, UpdateUserDto } from "./endpoints/users";
export type { Project, CreateProjectDto, UpdateProjectDto, SearchProjectsParams } from "./endpoints/projects";
export type {
  Guild,
  CreateGuildDto,
  UpdateGuildDto,
  GuildMember,
  CreateGuildMemberDto,
  UpdateGuildMemberDto,
  ProjectBid,
  CreateProjectBidDto,
  UpdateProjectBidDto,
  GuildReview,
  CreateGuildReviewDto,
  UpdateGuildReviewDto,
  BidStatus,
} from "./endpoints/guilds";

// Re-export schemas for validation
export {
  GuildApiSchema,
  CreateGuildSchema,
  UpdateGuildSchema,
  GuildMemberApiSchema,
  CreateGuildMemberSchema,
  UpdateGuildMemberSchema,
  ProjectBidApiSchema,
  CreateProjectBidSchema,
  UpdateProjectBidSchema,
  GuildReviewApiSchema,
  CreateGuildReviewSchema,
  UpdateGuildReviewSchema,
} from "./endpoints/guilds";

/**
 * ArdaNova API Client
 * Composes all ArdaNova endpoint modules into a single client instance
 */
export class ArdaNovaApiClient extends BaseApiClient {
  readonly users: UsersEndpoint;
  readonly projects: ProjectsEndpoint;
  readonly guilds: GuildsEndpoint;

  constructor(config: BaseApiClientConfig) {
    super(config);
    this.users = new UsersEndpoint(this);
    this.projects = new ProjectsEndpoint(this);
    this.guilds = new GuildsEndpoint(this);
  }

  health() {
    return this.get<{ status: string; timestamp: string }>("/health");
  }

  /**
   * Create an ArdaNova client with API Key authentication
   */
  static withApiKey(baseUrl: string, apiKey: string) {
    return new ArdaNovaApiClient({
      baseUrl,
      headers: { "X-Api-Key": apiKey },
    });
  }
}

// Export singleton instance configured from environment
export const apiClient = ArdaNovaApiClient.withApiKey(env.API_URL, env.API_KEY);

import { env } from "~/env";
import { BaseApiClient, type BaseApiClientConfig } from "../base-client";
import { UsersEndpoint } from "./endpoints/users";
import { ProjectsEndpoint } from "./endpoints/projects";
import { GuildsEndpoint } from "./endpoints/guilds";
import { ShopsEndpoint } from "./endpoints/shops";
import { TasksEndpoint } from "./endpoints/tasks";
import { EventsEndpoint } from "./endpoints/events";
import { OpportunitiesEndpoint } from "./endpoints/opportunities";
import { GovernanceEndpoint } from "./endpoints/governance";

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
export type { Shop, CreateShopDto, UpdateShopDto, SearchShopsParams } from "./endpoints/shops";
export type { Task, TaskUser, TaskProject, CreateTaskDto, UpdateTaskDto, SearchTasksParams } from "./endpoints/tasks";
export type { Event, EventOrganizer, EventAttendee, CreateEventDto, UpdateEventDto, RegisterEventDto, SearchEventsParams } from "./endpoints/events";
export type { Opportunity, OpportunityApplication, CreateOpportunityDto, UpdateOpportunityDto, ApplyToOpportunityDto, SearchOpportunitiesParams } from "./endpoints/opportunities";
export type { Proposal, Vote, ProposalVoteSummary, CreateProposalDto, UpdateProposalDto, CastVoteDto, SearchProposalsParams } from "./endpoints/governance";

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
  readonly shops: ShopsEndpoint;
  readonly tasks: TasksEndpoint;
  readonly events: EventsEndpoint;
  readonly opportunities: OpportunitiesEndpoint;
  readonly governance: GovernanceEndpoint;

  constructor(config: BaseApiClientConfig) {
    super(config);
    this.users = new UsersEndpoint(this);
    this.projects = new ProjectsEndpoint(this);
    this.guilds = new GuildsEndpoint(this);
    this.shops = new ShopsEndpoint(this);
    this.tasks = new TasksEndpoint(this);
    this.events = new EventsEndpoint(this);
    this.opportunities = new OpportunitiesEndpoint(this);
    this.governance = new GovernanceEndpoint(this);
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

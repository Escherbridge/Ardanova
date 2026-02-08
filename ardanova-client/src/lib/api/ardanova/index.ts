import { env } from "~/env";
import { BaseApiClient, type BaseApiClientConfig } from "../base-client";
import { UsersEndpoint } from "./endpoints/users";
import { ProjectsEndpoint } from "./endpoints/projects";
import { GuildsEndpoint } from "./endpoints/guilds";
import { TasksEndpoint } from "./endpoints/tasks";
import { EventsEndpoint } from "./endpoints/events";
import { OpportunitiesEndpoint } from "./endpoints/opportunities";
import { GovernanceEndpoint } from "./endpoints/governance";
import { SprintsEndpoint } from "./endpoints/sprints";
import { EpicsEndpoint } from "./endpoints/epics";
import { BacklogEndpoint } from "./endpoints/backlog";
import { FeaturesEndpoint } from "./endpoints/features";
import { OpportunityBidsEndpoint } from "./endpoints/opportunity-bids";
import { MembershipCredentialsEndpoint } from "./endpoints/membership-credentials";
import { CredentialUtilityEndpoint } from "./endpoints/credential-utility";
import { ProductsEndpoint } from "./endpoints/products";
import { ChatEndpoint } from "./endpoints/chat";
import { EnumsEndpoint } from "./endpoints/enums";
import { StreaksEndpoint } from "./endpoints/streaks";
import { XPEventsEndpoint } from "./endpoints/xp-events";
import { AchievementsEndpoint } from "./endpoints/achievements";
import { LeaderboardsEndpoint } from "./endpoints/leaderboards";
import { ReferralsEndpoint } from "./endpoints/referrals";
import { KycEndpoint } from "./endpoints/kyc";

// Re-export types from endpoints
export type {
  User,
  CreateUserDto,
  UpdateUserDto,
  AdminUpdateUserRoleDto,
  AdminUpdateUserTypeDto,
  AdminUpdateVerificationLevelDto,
  UserSkill,
  CreateUserSkillDto,
  UpdateUserSkillDto,
  UserExperience,
  CreateUserExperienceDto,
  UpdateUserExperienceDto,
} from "./endpoints/users";
export type {
  Project,
  CreateProjectDto,
  UpdateProjectDto,
  SearchProjectsParams,
  ProjectResource,
  CreateResourceDto,
  UpdateResourceDto,
  ProjectMilestone,
  CreateMilestoneDto,
  UpdateMilestoneDto,
  ProjectMember,
  AddMemberDto,
  UpdateMemberDto,
  ProjectApplication,
  CreateApplicationDto,
  ReviewApplicationDto,
  Proposal as ProjectProposal,
  CreateProposalDto as CreateProjectProposalDto,
  Vote as ProjectVote,
  CastVoteDto as CastProjectVoteDto,
  ProjectUpdate,
  CreateUpdateDto,
  ProjectComment,
  CreateCommentDto,
  ProjectSupport,
  CreateSupportDto,
} from "./endpoints/projects";
export type {
  Guild,
  CreateGuildDto,
  UpdateGuildDto,
  GuildMember,
  CreateGuildMemberDto,
  UpdateGuildMemberDto,
  GuildReview,
  CreateGuildReviewDto,
  UpdateGuildReviewDto,
} from "./endpoints/guilds";
export type { Task, TaskUser, TaskProject, CreateTaskDto, UpdateTaskDto, SearchTasksParams } from "./endpoints/tasks";
export type { Event, EventOrganizer, EventAttendee, CreateEventDto, UpdateEventDto, RegisterEventDto, SearchEventsParams } from "./endpoints/events";
export type { Opportunity, OpportunityApplication, CreateOpportunityDto, UpdateOpportunityDto, ApplyToOpportunityDto, SearchOpportunitiesParams } from "./endpoints/opportunities";
export type { Proposal, Vote, ProposalVoteSummary, CreateProposalDto, UpdateProposalDto, CastVoteDto, SearchProposalsParams } from "./endpoints/governance";
export type {
  Sprint,
  SprintStatus,
  CreateSprint,
  UpdateSprint,
} from "./endpoints/sprints";
export type {
  Epic,
  EpicPriority,
  EpicStatus,
  CreateEpic,
  UpdateEpic,
} from "./endpoints/epics";
export type {
  ProductBacklogItem,
  PbiType,
  PbiPriority,
  PbiStatus,
  CreateProductBacklogItem,
  UpdateProductBacklogItem,
} from "./endpoints/backlog";
export type {
  Feature,
  FeatureStatus,
  FeaturePriority,
  CreateFeature,
  UpdateFeature,
} from "./endpoints/features";
export type {
  Product,
  CreateProduct,
  UpdateProduct,
} from "./endpoints/products";
export type {
  OpportunityBid,
  OpportunityBidStatus,
  CreateOpportunityBid,
  UpdateOpportunityBid,
} from "./endpoints/opportunity-bids";
export type {
  MembershipCredential,
  MembershipCredentialStatus,
  MembershipGrantType,
  GrantMembershipCredentialDto,
  RevokeMembershipCredentialDto,
  UpdateMembershipCredentialMintDto,
  UpdateCredentialTierDto,
  CredentialEligibilityDto,
} from "./endpoints/membership-credentials";
export type {
  AsaInfo,
  CredentialWithChainData,
  CheckAutoGrantRequest,
  UpdateTierDto,
} from "./endpoints/credential-utility";
export type {
  ChatConversation,
  ChatParticipant,
  ChatMessage,
  GetConversationsParams,
  GetOrCreateDirectDto,
  CreateGroupDto,
  UpdateGroupDto,
  AddMemberDto as ChatAddMemberDto,
  SendMessageDto,
  UpdateMessageDto,
  MarkAsReadDto,
  TypingIndicatorDto,
  CursorPaginatedResult,
} from "./endpoints/chat";
export type {
  UserStreak,
  CreateUserStreakDto,
} from "./endpoints/streaks";
export type {
  XPEventDto,
  AwardXPDto,
  XPSummaryDto,
  LevelInfoDto,
  XPRewardsConfigDto,
} from "./endpoints/xp-events";
export type {
  AchievementDto,
  CreateAchievementDto,
  UpdateAchievementDto,
  UserAchievementDto,
  UpdateProgressDto,
} from "./endpoints/achievements";
export type {
  LeaderboardDto,
  CreateLeaderboardDto,
  LeaderboardEntryDto,
  CreateLeaderboardEntryDto,
  UpdateLeaderboardEntryDto,
} from "./endpoints/leaderboards";
export type {
  Referral,
  CreateReferralDto,
  ClaimReferralRewardDto,
} from "./endpoints/referrals";
export type {
  KycSubmission,
  KycDocument,
  KycStatus,
  KycDocumentType,
  KycProvider,
  SubmitKycDto,
  SubmitKycDocumentDto,
  ReviewKycDto,
} from "./endpoints/kyc";

// Re-export schemas for validation
export {
  GuildApiSchema,
  CreateGuildSchema,
  UpdateGuildSchema,
  GuildMemberApiSchema,
  CreateGuildMemberSchema,
  UpdateGuildMemberSchema,
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
  readonly tasks: TasksEndpoint;
  readonly events: EventsEndpoint;
  readonly opportunities: OpportunitiesEndpoint;
  readonly governance: GovernanceEndpoint;
  readonly sprints: SprintsEndpoint;
  readonly epics: EpicsEndpoint;
  readonly backlog: BacklogEndpoint;
  readonly features: FeaturesEndpoint;
  readonly products: ProductsEndpoint;
  readonly opportunityBids: OpportunityBidsEndpoint;
  readonly membershipCredentials: MembershipCredentialsEndpoint;
  readonly credentialUtility: CredentialUtilityEndpoint;
  readonly chat: ChatEndpoint;
  readonly enums: EnumsEndpoint;
  readonly streaks: StreaksEndpoint;
  readonly xpEvents: XPEventsEndpoint;
  readonly achievements: AchievementsEndpoint;
  readonly leaderboards: LeaderboardsEndpoint;
  readonly referrals: ReferralsEndpoint;
  readonly kyc: KycEndpoint;

  constructor(config: BaseApiClientConfig) {
    super(config);
    this.users = new UsersEndpoint(this);
    this.projects = new ProjectsEndpoint(this);
    this.guilds = new GuildsEndpoint(this);
    this.tasks = new TasksEndpoint(this);
    this.events = new EventsEndpoint(this);
    this.opportunities = new OpportunitiesEndpoint(this);
    this.governance = new GovernanceEndpoint(this);
    this.sprints = new SprintsEndpoint(this);
    this.epics = new EpicsEndpoint(this);
    this.backlog = new BacklogEndpoint(this);
    this.features = new FeaturesEndpoint(this);
    this.products = new ProductsEndpoint(this);
    this.opportunityBids = new OpportunityBidsEndpoint(this);
    this.membershipCredentials = new MembershipCredentialsEndpoint(this);
    this.credentialUtility = new CredentialUtilityEndpoint(this);
    this.chat = new ChatEndpoint(this);
    this.enums = new EnumsEndpoint(this);
    this.streaks = new StreaksEndpoint(this);
    this.xpEvents = new XPEventsEndpoint(this);
    this.achievements = new AchievementsEndpoint(this);
    this.leaderboards = new LeaderboardsEndpoint(this);
    this.referrals = new ReferralsEndpoint(this);
    this.kyc = new KycEndpoint(this);
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

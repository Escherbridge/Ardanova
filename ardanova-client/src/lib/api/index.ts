// Base client exports - use these to create clients for other APIs
export {
  BaseApiClient,
  createApiKeyClient,
  createBearerClient,
  type ApiResponse,
  type PagedResult,
  type BaseApiClientConfig,
} from "./base-client";

// ArdaNova API client exports
export {
  ArdaNovaApiClient,
  apiClient,
  // User types
  type User,
  type CreateUserDto,
  type UpdateUserDto,
  // Project types
  type Project,
  type CreateProjectDto,
  type UpdateProjectDto,
  // Guild types
  type Guild,
  type CreateGuildDto,
  type UpdateGuildDto,
  type GuildMember,
  type CreateGuildMemberDto,
  type GuildReview,
  type CreateGuildReviewDto,
  type ProjectBid,
  // Guild schemas for validation
  GuildApiSchema,
  CreateGuildSchema,
  UpdateGuildSchema,
  GuildMemberApiSchema,
  CreateGuildMemberSchema,
  GuildReviewApiSchema,
  CreateGuildReviewSchema,
} from "./ardanova";

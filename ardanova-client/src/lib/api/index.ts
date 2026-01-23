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
  type User,
  type CreateUserDto,
  type UpdateUserDto,
  type Project,
  type CreateProjectDto,
  type UpdateProjectDto,
  type Agency,
  type CreateAgencyDto,
  type UpdateAgencyDto,
  type Business,
  type CreateBusinessDto,
  type UpdateBusinessDto,
} from "./ardanova";

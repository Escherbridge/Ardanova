/**
 * @deprecated Import from '~/lib/api' instead
 * This file is kept for backwards compatibility
 */
export {
  // Base client
  BaseApiClient,
  createApiKeyClient,
  createBearerClient,
  type ApiResponse,
  type PagedResult,
  type BaseApiClientConfig,
  // ArdaNova client
  ArdaNovaApiClient,
  apiClient,
  type User,
  type CreateUserDto,
  type UpdateUserDto,
  type Project,
  type CreateProjectDto,
  type UpdateProjectDto,
} from "./api";

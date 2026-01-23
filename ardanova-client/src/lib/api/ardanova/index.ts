import { env } from "~/env";
import { BaseApiClient, type BaseApiClientConfig } from "../base-client";
import { UsersEndpoint } from "./endpoints/users";
import { ProjectsEndpoint } from "./endpoints/projects";
import { AgenciesEndpoint } from "./endpoints/agencies";
import { BusinessesEndpoint } from "./endpoints/businesses";

// Re-export types from endpoints
export type { User, CreateUserDto, UpdateUserDto } from "./endpoints/users";
export type { Project, CreateProjectDto, UpdateProjectDto } from "./endpoints/projects";
export type { Agency, CreateAgencyDto, UpdateAgencyDto } from "./endpoints/agencies";
export type { Business, CreateBusinessDto, UpdateBusinessDto } from "./endpoints/businesses";

/**
 * ArdaNova API Client
 * Composes all ArdaNova endpoint modules into a single client instance
 */
export class ArdaNovaApiClient extends BaseApiClient {
  readonly users: UsersEndpoint;
  readonly projects: ProjectsEndpoint;
  readonly agencies: AgenciesEndpoint;
  readonly businesses: BusinessesEndpoint;

  constructor(config: BaseApiClientConfig) {
    super(config);
    this.users = new UsersEndpoint(this);
    this.projects = new ProjectsEndpoint(this);
    this.agencies = new AgenciesEndpoint(this);
    this.businesses = new BusinessesEndpoint(this);
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

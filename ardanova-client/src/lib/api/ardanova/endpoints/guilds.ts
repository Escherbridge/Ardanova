import { z } from "zod";
import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

/** Request bodies here match the .NET DTOs exactly; identity (owner, reviewer, inviter) always comes from the signed actor. See endpoints/AGENTS.md §Guilds. */

export interface Guild {
  id: string;
  name: string;
  slug?: string | null;
  description?: string | null;
  logoUrl?: string | null;
  /** Backend `Logo` (camelCase `logo`); may mirror `logoUrl` depending on API version */
  logo?: string | null;
  website?: string | null;
  email?: string;
  phone?: string | null;
  address?: string | null;
  portfolio?: string | null;
  specialties?: string | null;
  isVerified: boolean;
  rating?: number | null;
  reviewsCount?: number;
  projectsCount?: number;
  membersCount?: number;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
}

export interface GuildMember {
  id: string;
  guildId: string;
  userId: string;
  role?: string | null;
  joinedAt: string;
  /** Public profile only; the API never returns a member's email. */
  user?: { id: string; name?: string | null; image?: string | null } | null;
}

/** Matches `GuildReviewDto`: the reviewer is `userId`, the text is `comment`. */
export interface GuildReview {
  id: string;
  guildId: string;
  userId: string;
  projectId?: string | null;
  rating: number;
  comment?: string | null;
  createdAt: string;
}

/** Matches `GuildInvitationDto`. */
export interface GuildInvitation {
  id: string;
  guildId: string;
  guildName?: string | null;
  guildSlug?: string | null;
  invitedById: string;
  invitedUserId?: string | null;
  invitedEmail?: string | null;
  role: string;
  message?: string | null;
  status: string;
  createdAt: string;
  expiresAt?: string | null;
  respondedAt?: string | null;
}

/** Matches `CreateGuildDto`; the owner is the signed actor, so no owner id is sent. */
export interface CreateGuildDto {
  name: string;
  description: string;
  email: string;
  website?: string;
  phone?: string;
  address?: string;
  logo?: string;
  portfolio?: string;
  specialties?: string;
}

export interface UpdateGuildDto {
  name?: string;
  slug?: string;
  description?: string;
  logoUrl?: string;
  logo?: string | null;
  website?: string;
  email?: string;
  phone?: string | null;
  address?: string | null;
  portfolio?: string | null;
  specialties?: string | null;
}

/** Matches `CreateGuildMemberDto`; the guild is the route. */
export interface CreateGuildMemberDto {
  userId: string;
  role: string;
}

export interface UpdateGuildMemberDto {
  role?: string;
}

/** Matches `CreateGuildReviewDto`; the reviewer is the signed actor. */
export interface CreateGuildReviewDto {
  rating: number;
  comment?: string;
  projectId?: string;
}

export interface UpdateGuildReviewDto {
  rating?: number;
  comment?: string;
}

export interface GuildUpdate {
  id: string;
  guildId?: string;
  title: string;
  content: string;
  createdAt: string;
  createdById?: string;
  updatedAt?: string;
}

/** Roles a guild can hand out; OWNER is never grantable through members, invitations or applications. */
export const GrantableGuildRoleSchema = z.enum([
  "ADMIN",
  "MANAGER",
  "MEMBER",
  "APPRENTICE",
]);

// Zod schemas for validation (re-exported from index)
export const GuildApiSchema = z.object({
  id: z.string(),
  name: z.string(),
  slug: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  logoUrl: z.string().nullable().optional(),
  website: z.string().nullable().optional(),
  isVerified: z.boolean(),
  ownerId: z.string(),
  createdAt: z.string(),
  updatedAt: z.string(),
});

export const CreateGuildSchema = z.object({
  name: z.string().min(1),
  description: z.string().min(1),
  email: z.string().min(1).email(),
  phone: z.string().optional(),
  address: z.string().optional(),
  logo: z.string().optional(),
  portfolio: z.string().optional(),
  specialties: z.string().optional(),
  // Backend accepts optional URL; form may omit or send a full URL (not `z.string().url()` on empty)
  website: z
    .string()
    .optional()
    .transform((s) =>
      s === undefined || s.trim() === "" ? undefined : s.trim(),
    )
    .refine((s) => s === undefined || /^https?:\/\/.+/.test(s), {
      message: "Website must start with http:// or https://",
    }),
});

export const UpdateGuildSchema = z.object({
  name: z.string().min(1).optional(),
  slug: z.string().optional(),
  description: z.string().optional(),
  logoUrl: z.string().url().optional(),
  website: z.string().url().optional(),
  email: z.string().email().optional(),
  phone: z.string().optional(),
  address: z.string().optional(),
  logo: z.string().optional(),
  portfolio: z.string().optional(),
  specialties: z.string().optional(),
});

export const GuildMemberApiSchema = z.object({
  id: z.string(),
  guildId: z.string(),
  userId: z.string(),
  role: z.string().nullable().optional(),
  joinedAt: z.string(),
});

export const CreateGuildMemberSchema = z.object({
  userId: z.string().min(1),
  role: GrantableGuildRoleSchema,
});

export const UpdateGuildMemberSchema = z.object({
  role: GrantableGuildRoleSchema.optional(),
});

export const GuildReviewApiSchema = z.object({
  id: z.string(),
  guildId: z.string(),
  userId: z.string(),
  rating: z.number(),
  comment: z.string().nullable().optional(),
  createdAt: z.string(),
});

export const CreateGuildReviewSchema = z.object({
  rating: z.number().int().min(1).max(5),
  comment: z.string().optional(),
  projectId: z.string().optional(),
});

export const UpdateGuildReviewSchema = z.object({
  rating: z.number().int().min(1).max(5).optional(),
  comment: z.string().optional(),
});

export class GuildsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Guild>> {
    return this.client.get<Guild>(`/api/guilds/${id}`);
  }

  getBySlug(slug: string): Promise<ApiResponse<Guild>> {
    return this.client.get<Guild>(`/api/guilds/slug/${encodeURIComponent(slug)}`);
  }

  getByOwnerId(ownerId: string): Promise<ApiResponse<Guild | Guild[]>> {
    return this.client.get<Guild | Guild[]>(`/api/guilds/owner/${ownerId}`);
  }

  getPaged(page = 1, limit = 10): Promise<ApiResponse<PagedResult<Guild>>> {
    return this.client.get<PagedResult<Guild>>(`/api/guilds/paged?page=${page}&pageSize=${limit}`);
  }

  getVerified(): Promise<ApiResponse<Guild[]>> {
    return this.client.get<Guild[]>("/api/guilds/verified");
  }

  create(data: CreateGuildDto): Promise<ApiResponse<Guild>> {
    return this.client.post<Guild>("/api/guilds", data);
  }

  update(id: string, data: UpdateGuildDto): Promise<ApiResponse<Guild>> {
    return this.client.put<Guild>(`/api/guilds/${id}`, data);
  }

  /** The backend derives the requester from the signed actor; no query parameter is sent. */
  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/guilds/${id}`);
  }

  getMembers(guildId: string): Promise<ApiResponse<GuildMember[]>> {
    return this.client.get<GuildMember[]>(`/api/guilds/${guildId}/members`);
  }

  addMember(guildId: string, data: CreateGuildMemberDto): Promise<ApiResponse<GuildMember>> {
    return this.client.post<GuildMember>(`/api/guilds/${guildId}/members`, data);
  }

  updateMember(guildId: string, memberId: string, data: UpdateGuildMemberDto): Promise<ApiResponse<GuildMember>> {
    return this.client.put<GuildMember>(`/api/guilds/${guildId}/members/${memberId}`, data);
  }

  removeMember(guildId: string, memberId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/guilds/${guildId}/members/${memberId}`);
  }

  getReviews(guildId: string): Promise<ApiResponse<GuildReview[]>> {
    return this.client.get<GuildReview[]>(`/api/guilds/${guildId}/reviews`);
  }

  createReview(guildId: string, data: CreateGuildReviewDto): Promise<ApiResponse<GuildReview>> {
    return this.client.post<GuildReview>(`/api/guilds/${guildId}/reviews`, data);
  }

  updateReview(guildId: string, reviewId: string, data: UpdateGuildReviewDto): Promise<ApiResponse<GuildReview>> {
    return this.client.put<GuildReview>(`/api/guilds/${guildId}/reviews/${reviewId}`, data);
  }

  deleteReview(guildId: string, reviewId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/guilds/${guildId}/reviews/${reviewId}`);
  }

  getUpdates(guildId: string): Promise<ApiResponse<GuildUpdate[]>> {
    return this.client.get<GuildUpdate[]>(`/api/guilds/${guildId}/updates`);
  }

  /** Matches `CreateGuildUpdateDto`; the author is the signed actor. */
  createUpdate(guildId: string, data: { title: string; content: string; images?: string }): Promise<ApiResponse<GuildUpdate>> {
    return this.client.post<GuildUpdate>(`/api/guilds/${guildId}/updates`, data);
  }

  deleteUpdate(guildId: string, updateId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/guilds/${guildId}/updates/${updateId}`);
  }

  getApplications(guildId: string): Promise<ApiResponse<unknown[]>> {
    return this.client.get<unknown[]>(`/api/guilds/${guildId}/applications`);
  }

  /** Matches `CreateGuildApplicationDto`; the guild is the route and the applicant is the signed actor. */
  submitApplication(
    guildId: string,
    data: {
      requestedRole: string;
      message: string;
      skills?: string;
      experience?: string;
      portfolio?: string;
      availability?: string;
    }
  ): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/guilds/${guildId}/applications`, data);
  }

  getApplicationById(guildId: string, applicationId: string): Promise<ApiResponse<{ id: string; guildId: string; userId: string; [key: string]: unknown }>> {
    return this.client.get(`/api/guilds/${guildId}/applications/${applicationId}`);
  }

  acceptApplication(guildId: string, applicationId: string, data?: { reviewMessage?: string }): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/guilds/${guildId}/applications/${applicationId}/accept`, data ?? {});
  }

  rejectApplication(guildId: string, applicationId: string, data?: { reviewMessage?: string }): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/guilds/${guildId}/applications/${applicationId}/reject`, data ?? {});
  }

  getInvitations(guildId: string): Promise<ApiResponse<GuildInvitation[]>> {
    return this.client.get<GuildInvitation[]>(`/api/guilds/${guildId}/invitations`);
  }

  /** The signed actor's own invitations that can still be answered. */
  getMyInvitations(): Promise<ApiResponse<GuildInvitation[]>> {
    return this.client.get<GuildInvitation[]>("/api/guilds/invitations/mine");
  }

  /** Matches `CreateGuildInvitationDto`; the inviter is the signed actor and the invitee must already have an account. */
  createInvitation(
    guildId: string,
    data: {
      invitedUserId?: string;
      invitedEmail?: string;
      role: string;
      message?: string;
    }
  ): Promise<ApiResponse<GuildInvitation>> {
    return this.client.post<GuildInvitation>(`/api/guilds/${guildId}/invitations`, data);
  }

  /** The backend accepts or declines only for the bound invitee. */
  respondToInvitation(guildId: string, invitationId: string, data: { accept: boolean }): Promise<ApiResponse<GuildInvitation>> {
    return data.accept
      ? this.client.post<GuildInvitation>(`/api/guilds/${guildId}/invitations/${invitationId}/accept`, {})
      : this.client.post<GuildInvitation>(`/api/guilds/${guildId}/invitations/${invitationId}/reject`, {});
  }

  getFollowers(guildId: string): Promise<ApiResponse<unknown[]>> {
    return this.client.get<unknown[]>(`/api/guilds/${guildId}/followers`);
  }

  /** `POST api/guilds/{guildId}/follow`; the follower is the signed actor. */
  followGuild(guildId: string): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/guilds/${guildId}/follow`, {
      notifyUpdates: true,
      notifyEvents: true,
      notifyProjects: true,
    });
  }

  /** `DELETE api/guilds/{guildId}/follow` for the signed actor. */
  unfollowGuild(guildId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/guilds/${guildId}/follow`);
  }

  /** `GET api/guilds/{guildId}/follow/check?userId=` */
  isFollowing(guildId: string, userId: string): Promise<ApiResponse<boolean>> {
    return this.client.get<boolean>(
      `/api/guilds/${guildId}/follow/check?userId=${encodeURIComponent(userId)}`
    );
  }
}

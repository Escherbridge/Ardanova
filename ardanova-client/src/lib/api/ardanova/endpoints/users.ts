import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

// ============ Type Definitions ============

export interface User {
  id: string;
  email: string;
  emailVerified?: string;
  name?: string;
  image?: string;
  bio?: string;
  location?: string;
  phone?: string;
  website?: string;
  linkedIn?: string;
  twitter?: string;
  role: string;
  userType: string;
  isVerified: boolean;
  totalXP: number;
  level: number;
  tier: string;
  trustScore: number;
  verificationLevel: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateUserDto {
  email: string;
  name?: string;
  image?: string;
  role?: string;
  userType?: string;
}

export interface UpdateUserDto {
  name?: string;
  bio?: string;
  location?: string;
  phone?: string;
  website?: string;
  linkedIn?: string;
  twitter?: string;
  image?: string;
}

export interface AdminUpdateUserRoleDto {
  role: string;
}

export interface AdminUpdateUserTypeDto {
  userType: string;
}

export interface AdminUpdateVerificationLevelDto {
  verificationLevel: string;
}

// ============ UserSkill Types ============

export interface UserSkill {
  id: string;
  userId: string;
  skill: string;
  level: number;
}

export interface CreateUserSkillDto {
  skill: string;
  level?: number;
}

export interface UpdateUserSkillDto {
  level?: number;
}

// ============ UserExperience Types ============

export interface UserExperience {
  id: string;
  userId: string;
  title: string;
  company: string;
  description?: string;
  startDate: string;
  endDate?: string;
  isCurrent: boolean;
}

export interface CreateUserExperienceDto {
  title: string;
  company: string;
  description?: string;
  startDate: string;
  endDate?: string;
  isCurrent?: boolean;
}

export interface UpdateUserExperienceDto {
  title?: string;
  company?: string;
  description?: string;
  startDate?: string;
  endDate?: string;
  isCurrent?: boolean;
}

// ============ Users Endpoint ============

export class UsersEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<User>> {
    return this.client.get<User>(`/api/users/${id}`);
  }

  getByEmail(email: string): Promise<ApiResponse<User>> {
    return this.client.get<User>(`/api/users/email/${encodeURIComponent(email)}`);
  }

  getAll(page = 1, pageSize = 10): Promise<ApiResponse<PagedResult<User>>> {
    return this.client.get<PagedResult<User>>(`/api/users?page=${page}&pageSize=${pageSize}`);
  }

  search(query: string, page = 1, pageSize = 20): Promise<ApiResponse<PagedResult<User>>> {
    return this.client.get<PagedResult<User>>(
      `/api/users/search?query=${encodeURIComponent(query)}&page=${page}&pageSize=${pageSize}`
    );
  }

  create(data: CreateUserDto): Promise<ApiResponse<User>> {
    return this.client.post<User>("/api/users", data);
  }

  update(id: string, data: UpdateUserDto): Promise<ApiResponse<User>> {
    return this.client.put<User>(`/api/users/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/users/${id}`);
  }

  updateRole(id: string, dto: AdminUpdateUserRoleDto): Promise<ApiResponse<User>> {
    return this.client.put<User>(`/api/users/${id}/role`, dto);
  }

  updateUserType(id: string, dto: AdminUpdateUserTypeDto): Promise<ApiResponse<User>> {
    return this.client.put<User>(`/api/users/${id}/user-type`, dto);
  }

  updateVerificationLevel(id: string, dto: AdminUpdateVerificationLevelDto): Promise<ApiResponse<User>> {
    return this.client.put<User>(`/api/users/${id}/verification-level`, dto);
  }

  // ---- Skills ----

  getSkills(userId: string): Promise<ApiResponse<UserSkill[]>> {
    return this.client.get<UserSkill[]>(`/api/users/${userId}/skills`);
  }

  addSkill(userId: string, data: CreateUserSkillDto): Promise<ApiResponse<UserSkill>> {
    return this.client.post<UserSkill>(`/api/users/${userId}/skills`, data);
  }

  updateSkill(userId: string, skillId: string, data: UpdateUserSkillDto): Promise<ApiResponse<UserSkill>> {
    return this.client.put<UserSkill>(`/api/users/${userId}/skills/${skillId}`, data);
  }

  deleteSkill(userId: string, skillId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/users/${userId}/skills/${skillId}`);
  }

  // ---- Experience ----

  getExperience(userId: string): Promise<ApiResponse<UserExperience[]>> {
    return this.client.get<UserExperience[]>(`/api/users/${userId}/experience`);
  }

  addExperience(userId: string, data: CreateUserExperienceDto): Promise<ApiResponse<UserExperience>> {
    return this.client.post<UserExperience>(`/api/users/${userId}/experience`, data);
  }

  updateExperience(userId: string, experienceId: string, data: UpdateUserExperienceDto): Promise<ApiResponse<UserExperience>> {
    return this.client.put<UserExperience>(`/api/users/${userId}/experience/${experienceId}`, data);
  }

  deleteExperience(userId: string, experienceId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/users/${userId}/experience/${experienceId}`);
  }
}

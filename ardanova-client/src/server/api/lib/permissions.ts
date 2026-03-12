import { apiClient } from "~/lib/api";

interface PermissionResult {
  allowed: boolean;
  reason?: string;
}

export async function canCreateGuildOpportunity(
  userId: string,
  guildId: string
): Promise<PermissionResult> {
  try {
    const guildResponse = await apiClient.guilds.getById(guildId);

    if (guildResponse.error || !guildResponse.data) {
      return { allowed: false, reason: "Guild not found" };
    }

    if (guildResponse.data.ownerId === userId) {
      return { allowed: true };
    }

    const membersResponse = await apiClient.guilds.getMembers(guildId);

    if (membersResponse.error || !membersResponse.data) {
      return { allowed: false, reason: "You don't have permission to create opportunities for this guild" };
    }

    const membership = membersResponse.data.find(
      (m: { userId: string; role: string }) =>
        m.userId === userId && ["OWNER", "ADMIN", "MANAGER"].includes(m.role)
    );

    if (membership) {
      return { allowed: true };
    }

    return { allowed: false, reason: "You don't have permission to create opportunities for this guild" };
  } catch {
    return { allowed: false, reason: "Failed to verify guild permissions" };
  }
}

export async function canCreateProjectOpportunity(
  userId: string,
  projectId: string
): Promise<PermissionResult> {
  try {
    const projectResponse = await apiClient.projects.getById(projectId);

    if (projectResponse.error || !projectResponse.data) {
      return { allowed: false, reason: "Project not found" };
    }

    if (projectResponse.data.createdById === userId) {
      return { allowed: true };
    }

    const membersResponse = await apiClient.projects.getMembers(projectId);

    if (membersResponse.error || !membersResponse.data) {
      return { allowed: false, reason: "You don't have permission to create opportunities for this project" };
    }

    const membership = membersResponse.data.find(
      (m: { userId: string; role: string }) =>
        m.userId === userId && ["FOUNDER", "LEADER", "CORE_CONTRIBUTOR"].includes(m.role)
    );

    if (membership) {
      return { allowed: true };
    }

    return { allowed: false, reason: "You don't have permission to create opportunities for this project" };
  } catch {
    return { allowed: false, reason: "Failed to verify project permissions" };
  }
}

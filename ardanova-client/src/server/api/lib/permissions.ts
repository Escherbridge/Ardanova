import { PrismaClient } from "@prisma/client";

interface PermissionResult {
  allowed: boolean;
  reason?: string;
}

export async function canCreateGuildOpportunity(
  db: PrismaClient,
  userId: string,
  guildId: string
): Promise<PermissionResult> {
  // Check if user is guild owner
  const guild = await db.guild.findUnique({
    where: { id: guildId },
    select: { ownerId: true },
  });

  if (!guild) {
    return { allowed: false, reason: "Guild not found" };
  }

  if (guild.ownerId === userId) {
    return { allowed: true };
  }

  // Check if user has appropriate role in guild
  const membership = await db.guildMember.findFirst({
    where: {
      guildId,
      userId,
      role: { in: ["OWNER", "ADMIN", "MANAGER"] },
    },
  });

  if (membership) {
    return { allowed: true };
  }

  return { allowed: false, reason: "You don't have permission to create opportunities for this guild" };
}

export async function canCreateProjectOpportunity(
  db: PrismaClient,
  userId: string,
  projectId: string
): Promise<PermissionResult> {
  // Check if user is project owner
  const project = await db.project.findUnique({
    where: { id: projectId },
    select: { createdById: true },
  });

  if (!project) {
    return { allowed: false, reason: "Project not found" };
  }

  if (project.createdById === userId) {
    return { allowed: true };
  }

  // Check if user has appropriate role in project
  const membership = await db.projectMember.findFirst({
    where: {
      projectId,
      userId,
      role: { in: ["FOUNDER", "LEADER", "CORE_CONTRIBUTOR"] },
    },
  });

  if (membership) {
    return { allowed: true };
  }

  return { allowed: false, reason: "You don't have permission to create opportunities for this project" };
}


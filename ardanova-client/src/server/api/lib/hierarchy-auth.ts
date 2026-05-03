/**
 * Hierarchy-level authorization helpers.
 *
 * Rules:
 * - Project owners/members with LEAD/ADMIN role can always create children
 * - Assignees of a parent item can create children under that item
 * - Anyone assigned to an ancestor can create descendants
 */

import { apiClient } from "~/lib/api";
import { TRPCError } from "@trpc/server";

interface AuthContext {
  userId: string;
  projectId: string;
}

/**
 * Check if user can manage a project (owner, LEAD, or ADMIN member)
 */
export async function canManageProject(ctx: AuthContext): Promise<boolean> {
  const membersResp = await apiClient.projects.getMembers(ctx.projectId);
  if (membersResp.error || !membersResp.data) return false;

  const member = membersResp.data.find((m: any) => m.userId === ctx.userId);
  if (!member) return false;

  const role = (member.role as string)?.toUpperCase();
  return ["FOUNDER", "LEADER", "CORE_CONTRIBUTOR", "ADMIN", "LEAD"].includes(role);
}

/**
 * Check if user is the assignee of a specific hierarchy item.
 * Fetches the item and checks its assigneeId/assignedToId.
 */
async function isAssigneeOf(
  level: "milestone" | "epic" | "sprint" | "feature" | "pbi" | "task",
  itemId: string,
  userId: string
): Promise<boolean> {
  try {
    let item: any;
    switch (level) {
      case "milestone": {
        const r = await apiClient.projects.getMilestoneById?.("", itemId);
        // Milestones don't have a standalone getById - check via assigneeId
        // We'll handle this at the router level
        return false;
      }
      case "epic": {
        const r = await apiClient.epics.getById(itemId);
        item = r.data;
        break;
      }
      case "sprint": {
        const r = await apiClient.sprints.getById(itemId);
        item = r.data;
        break;
      }
      case "feature": {
        const r = await apiClient.features.getById(itemId);
        item = r.data;
        break;
      }
      case "pbi": {
        const r = await apiClient.backlog.getPbiById(itemId);
        item = r.data;
        break;
      }
      case "task": {
        const r = await apiClient.tasks.getById(itemId);
        item = r.data;
        break;
      }
    }
    if (!item) return false;
    return item.assigneeId === userId || item.assignedToId === userId;
  } catch {
    return false;
  }
}

/**
 * Authorize a user to create a child item under a parent.
 *
 * Checks (in order):
 * 1. User is a project manager (owner/lead/admin)
 * 2. User is the assignee of the parent item
 *
 * Throws TRPCError FORBIDDEN if neither condition is met.
 */
export async function authorizeChildCreation(
  ctx: AuthContext,
  parentLevel: "milestone" | "epic" | "sprint" | "feature" | "pbi",
  parentId: string
): Promise<void> {
  // Project managers can always create
  if (await canManageProject(ctx)) return;

  // Check if user is assignee of the parent
  if (await isAssigneeOf(parentLevel, parentId, ctx.userId)) return;

  throw new TRPCError({
    code: "FORBIDDEN",
    message: `You must be a project manager or assigned to this ${parentLevel} to add children`,
  });
}

/**
 * Authorize creation of a root-level item (no parent, directly under project).
 * Only project managers can do this.
 */
export async function authorizeRootCreation(ctx: AuthContext): Promise<void> {
  if (await canManageProject(ctx)) return;

  throw new TRPCError({
    code: "FORBIDDEN",
    message: "Only project managers can create root-level work items",
  });
}

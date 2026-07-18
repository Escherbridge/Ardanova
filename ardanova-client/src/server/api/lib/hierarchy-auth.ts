import { TRPCError } from "@trpc/server";
import { apiClient } from "~/lib/api";
import type { ProjectRole } from "~/lib/contracts/project-contract";

const MANAGER_ROLES = new Set<ProjectRole>([
  "FOUNDER",
  "LEADER",
  "CORE_CONTRIBUTOR",
]);

export type HierarchyLevel =
  | "milestone"
  | "epic"
  | "sprint"
  | "feature"
  | "pbi"
  | "task";

export type HierarchyMutationKind = "work" | "structure";

export interface HierarchyAuthContext {
  userId: string;
  projectId: string;
  isAdmin?: boolean;
}

export interface HierarchyParent {
  id: string;
  level: HierarchyLevel;
}

export interface ResolvedHierarchyItem extends HierarchyParent {
  projectId: string;
  assigneeId: string | null;
  ancestry: Partial<Record<HierarchyLevel, string>>;
  chainAssigneeIds: readonly string[];
}

interface HierarchyResponse<T> {
  data?: T;
  error?: string;
  status: number;
}

interface ProjectReference {
  id: string;
  createdById: string;
  assignedGuildId: string | null;
}

interface MemberReference {
  userId: string;
  role: ProjectRole;
}

interface MilestoneReference {
  projectId: string;
}

interface WorkReference {
  projectId: string;
  assigneeId?: string | null;
}

interface EpicReference extends WorkReference {
  milestoneId: string;
}

interface SprintReference extends WorkReference {
  epicId: string;
}

interface FeatureReference extends WorkReference {
  sprintId: string;
}

interface PbiReference extends WorkReference {
  milestoneId?: string | null;
  epicId?: string | null;
  sprintId?: string | null;
  featureId?: string | null;
}

interface TaskReference {
  projectId: string;
  assignedToId?: string | null;
  milestoneId?: string | null;
  epicId?: string | null;
  sprintId?: string | null;
  featureId?: string | null;
  pbiId?: string | null;
}

export interface HierarchyClient {
  projects: {
    getById(id: string): Promise<HierarchyResponse<ProjectReference>>;
    getMembers(
      projectId: string,
    ): Promise<HierarchyResponse<MemberReference[]>>;
    getMilestoneById(
      projectId: string,
      milestoneId: string,
    ): Promise<HierarchyResponse<MilestoneReference>>;
  };
  epics: {
    getById(id: string): Promise<HierarchyResponse<EpicReference>>;
  };
  sprints: {
    getById(id: string): Promise<HierarchyResponse<SprintReference>>;
  };
  features: {
    getById(id: string): Promise<HierarchyResponse<FeatureReference>>;
  };
  backlog: {
    getPbiById(id: string): Promise<HierarchyResponse<PbiReference>>;
  };
  tasks: {
    getById(id: string): Promise<HierarchyResponse<TaskReference>>;
  };
}

function notFound(level: HierarchyLevel, id: string): never {
  throw new TRPCError({
    code: "NOT_FOUND",
    message: `${level} ${id} was not found`,
  });
}

function unverifiable(message: string): never {
  throw new TRPCError({ code: "FORBIDDEN", message });
}

function assertProjectMatch(
  level: HierarchyLevel,
  id: string,
  actualProjectId: string,
  expectedProjectId?: string,
): void {
  if (!actualProjectId) {
    unverifiable(
      `The ${level} ${id} project association could not be verified`,
    );
  }

  if (expectedProjectId && actualProjectId !== expectedProjectId) {
    unverifiable(`The ${level} ${id} does not belong to the expected project`);
  }
}

const HIERARCHY_DEPTH: Record<HierarchyLevel, number> = {
  milestone: 0,
  epic: 1,
  sprint: 2,
  feature: 3,
  pbi: 4,
  task: 5,
};

function assertSingleChain(items: readonly ResolvedHierarchyItem[]): void {
  if (items.length < 2) return;

  const deepest = items.reduce((current, candidate) =>
    HIERARCHY_DEPTH[candidate.level] > HIERARCHY_DEPTH[current.level]
      ? candidate
      : current,
  );

  for (const item of items) {
    if (deepest.ancestry[item.level] !== item.id) {
      unverifiable("Hierarchy parents do not form one ancestry chain");
    }
  }
}

function collectAssignees(
  assigneeId: string | null | undefined,
  parents: readonly ResolvedHierarchyItem[],
): string[] {
  const assignees = new Set<string>();
  if (assigneeId) assignees.add(assigneeId);
  for (const parent of parents) {
    for (const parentAssigneeId of parent.chainAssigneeIds) {
      assignees.add(parentAssigneeId);
    }
  }
  return [...assignees];
}

function collectAncestry(
  level: HierarchyLevel,
  id: string,
  parents: readonly ResolvedHierarchyItem[],
): Partial<Record<HierarchyLevel, string>> {
  const ancestry: Partial<Record<HierarchyLevel, string>> = { [level]: id };
  for (const parent of parents) {
    Object.assign(ancestry, parent.ancestry);
  }
  return ancestry;
}

export class HierarchyAuthorization {
  constructor(private readonly client: HierarchyClient) {}

  async canManageProject(ctx: HierarchyAuthContext): Promise<boolean> {
    if (ctx.isAdmin) return true;

    const project = await this.client.projects.getById(ctx.projectId);
    if (project.error || !project.data) return false;
    if (project.data.createdById === ctx.userId) return true;

    const members = await this.client.projects.getMembers(ctx.projectId);
    if (members.error || !members.data) return false;

    return members.data.some(
      (member) =>
        member.userId === ctx.userId && MANAGER_ROLES.has(member.role),
    );
  }

  async requireProjectManager(ctx: HierarchyAuthContext): Promise<void> {
    if (await this.canManageProject(ctx)) return;

    throw new TRPCError({
      code: "FORBIDDEN",
      message: "Project management rights are required for this change",
    });
  }

  async requireProjectMember(projectId: string, userId: string): Promise<void> {
    const project = await this.client.projects.getById(projectId);
    if (project.error || !project.data) {
      throw new TRPCError({ code: "NOT_FOUND", message: "Project not found" });
    }
    if (project.data.createdById === userId) return;

    const members = await this.client.projects.getMembers(projectId);
    if (
      members.error ||
      !members.data?.some((member) => member.userId === userId)
    ) {
      throw new TRPCError({
        code: "BAD_REQUEST",
        message: "The assignee must be a member of this project",
      });
    }
  }

  async requireAssignedGuild(
    projectId: string,
    guildId: string,
  ): Promise<void> {
    const project = await this.client.projects.getById(projectId);
    if (project.error || !project.data) {
      throw new TRPCError({ code: "NOT_FOUND", message: "Project not found" });
    }
    if (project.data.assignedGuildId !== guildId) {
      throw new TRPCError({
        code: "BAD_REQUEST",
        message: "The guild is not assigned to this project",
      });
    }
  }

  async resolve(
    level: HierarchyLevel,
    id: string,
    expectedProjectId?: string,
  ): Promise<ResolvedHierarchyItem> {
    switch (level) {
      case "milestone":
        return this.resolveMilestone(id, expectedProjectId);
      case "epic":
        return this.resolveEpic(id, expectedProjectId);
      case "sprint":
        return this.resolveSprint(id, expectedProjectId);
      case "feature":
        return this.resolveFeature(id, expectedProjectId);
      case "pbi":
        return this.resolvePbi(id, expectedProjectId);
      case "task":
        return this.resolveTask(id, expectedProjectId);
    }
  }

  async authorizeCreation(
    ctx: HierarchyAuthContext,
    parents: readonly HierarchyParent[],
  ): Promise<void> {
    const resolvedParents = await Promise.all(
      parents.map((parent) =>
        this.resolve(parent.level, parent.id, ctx.projectId),
      ),
    );
    assertSingleChain(resolvedParents);

    if (await this.canManageProject(ctx)) return;

    if (
      resolvedParents.some((parent) =>
        parent.chainAssigneeIds.includes(ctx.userId),
      )
    ) {
      return;
    }

    throw new TRPCError({
      code: "FORBIDDEN",
      message:
        "Project management rights or an explicit parent assignment are required",
    });
  }

  async authorizeMutation(
    userId: string,
    level: HierarchyLevel,
    id: string,
    kind: HierarchyMutationKind,
    isAdmin = false,
  ): Promise<ResolvedHierarchyItem> {
    const item = await this.resolve(level, id);
    const ctx = { userId, projectId: item.projectId, isAdmin };

    if (await this.canManageProject(ctx)) return item;
    if (kind === "work" && item.assigneeId === userId) return item;

    throw new TRPCError({
      code: "FORBIDDEN",
      message:
        kind === "structure"
          ? "Project management rights are required for this structural change"
          : "Project management rights or an explicit item assignment are required",
    });
  }

  private async resolveMilestone(
    id: string,
    expectedProjectId?: string,
  ): Promise<ResolvedHierarchyItem> {
    if (!expectedProjectId) {
      unverifiable("A milestone project association is required");
    }

    const response = await this.client.projects.getMilestoneById(
      expectedProjectId,
      id,
    );
    if (response.error || !response.data) notFound("milestone", id);

    assertProjectMatch(
      "milestone",
      id,
      response.data.projectId,
      expectedProjectId,
    );
    return {
      id,
      level: "milestone",
      projectId: response.data.projectId,
      assigneeId: null,
      ancestry: { milestone: id },
      chainAssigneeIds: [],
    };
  }

  private async resolveEpic(
    id: string,
    expectedProjectId?: string,
  ): Promise<ResolvedHierarchyItem> {
    const response = await this.client.epics.getById(id);
    if (response.error || !response.data) notFound("epic", id);

    const epic = response.data;
    assertProjectMatch("epic", id, epic.projectId, expectedProjectId);
    const milestone = await this.resolveMilestone(
      epic.milestoneId,
      epic.projectId,
    );

    return {
      id,
      level: "epic",
      projectId: epic.projectId,
      assigneeId: epic.assigneeId ?? null,
      ancestry: collectAncestry("epic", id, [milestone]),
      chainAssigneeIds: collectAssignees(epic.assigneeId, [milestone]),
    };
  }

  private async resolveSprint(
    id: string,
    expectedProjectId?: string,
  ): Promise<ResolvedHierarchyItem> {
    const response = await this.client.sprints.getById(id);
    if (response.error || !response.data) notFound("sprint", id);

    const sprint = response.data;
    assertProjectMatch("sprint", id, sprint.projectId, expectedProjectId);
    const epic = await this.resolveEpic(sprint.epicId, sprint.projectId);

    return {
      id,
      level: "sprint",
      projectId: sprint.projectId,
      assigneeId: sprint.assigneeId ?? null,
      ancestry: collectAncestry("sprint", id, [epic]),
      chainAssigneeIds: collectAssignees(sprint.assigneeId, [epic]),
    };
  }

  private async resolveFeature(
    id: string,
    expectedProjectId?: string,
  ): Promise<ResolvedHierarchyItem> {
    const response = await this.client.features.getById(id);
    if (response.error || !response.data) notFound("feature", id);

    const feature = response.data;
    assertProjectMatch("feature", id, feature.projectId, expectedProjectId);
    const sprint = await this.resolveSprint(
      feature.sprintId,
      feature.projectId,
    );

    return {
      id,
      level: "feature",
      projectId: feature.projectId,
      assigneeId: feature.assigneeId ?? null,
      ancestry: collectAncestry("feature", id, [sprint]),
      chainAssigneeIds: collectAssignees(feature.assigneeId, [sprint]),
    };
  }

  private async resolvePbi(
    id: string,
    expectedProjectId?: string,
  ): Promise<ResolvedHierarchyItem> {
    const response = await this.client.backlog.getPbiById(id);
    if (response.error || !response.data) notFound("pbi", id);

    const item = response.data;
    assertProjectMatch("pbi", id, item.projectId, expectedProjectId);

    const parents: Promise<ResolvedHierarchyItem>[] = [];
    if (item.milestoneId) {
      parents.push(this.resolveMilestone(item.milestoneId, item.projectId));
    }
    if (item.epicId) {
      parents.push(this.resolveEpic(item.epicId, item.projectId));
    }
    if (item.sprintId) {
      parents.push(this.resolveSprint(item.sprintId, item.projectId));
    }
    if (item.featureId) {
      parents.push(this.resolveFeature(item.featureId, item.projectId));
    }
    const resolvedParents = await Promise.all(parents);
    assertSingleChain(resolvedParents);

    return {
      id,
      level: "pbi",
      projectId: item.projectId,
      assigneeId: item.assigneeId ?? null,
      ancestry: collectAncestry("pbi", id, resolvedParents),
      chainAssigneeIds: collectAssignees(item.assigneeId, resolvedParents),
    };
  }

  private async resolveTask(
    id: string,
    expectedProjectId?: string,
  ): Promise<ResolvedHierarchyItem> {
    const response = await this.client.tasks.getById(id);
    if (response.error || !response.data) notFound("task", id);

    const task = response.data;
    assertProjectMatch("task", id, task.projectId, expectedProjectId);

    const parents: Promise<ResolvedHierarchyItem>[] = [];
    if (task.milestoneId) {
      parents.push(this.resolveMilestone(task.milestoneId, task.projectId));
    }
    if (task.epicId) {
      parents.push(this.resolveEpic(task.epicId, task.projectId));
    }
    if (task.sprintId) {
      parents.push(this.resolveSprint(task.sprintId, task.projectId));
    }
    if (task.featureId) {
      parents.push(this.resolveFeature(task.featureId, task.projectId));
    }
    if (task.pbiId) {
      parents.push(this.resolvePbi(task.pbiId, task.projectId));
    }
    const resolvedParents = await Promise.all(parents);
    assertSingleChain(resolvedParents);

    return {
      id,
      level: "task",
      projectId: task.projectId,
      assigneeId: task.assignedToId ?? null,
      ancestry: collectAncestry("task", id, resolvedParents),
      chainAssigneeIds: collectAssignees(task.assignedToId, resolvedParents),
    };
  }
}

export const hierarchyAuthorization = new HierarchyAuthorization(apiClient);

export async function canManageProject(
  ctx: HierarchyAuthContext,
): Promise<boolean> {
  return hierarchyAuthorization.canManageProject(ctx);
}

export async function authorizeChildCreation(
  ctx: HierarchyAuthContext,
  parentLevel: HierarchyLevel,
  parentId: string,
): Promise<void> {
  return hierarchyAuthorization.authorizeCreation(ctx, [
    { level: parentLevel, id: parentId },
  ]);
}

export async function authorizeRootCreation(
  ctx: HierarchyAuthContext,
): Promise<void> {
  return hierarchyAuthorization.requireProjectManager(ctx);
}

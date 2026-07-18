import { describe, expect, it, vi } from "vitest";
import { HierarchyAuthorization, type HierarchyClient } from "./hierarchy-auth";

vi.mock("~/lib/api", () => ({ apiClient: {} }));

function ok<T>(data: T) {
  return Promise.resolve({ data, status: 200 });
}

function createClient(): HierarchyClient {
  return {
    projects: {
      getById: (id) =>
        ok({ id, createdById: "owner", assignedGuildId: "guild-1" }),
      getMembers: () =>
        ok([
          { userId: "leader", role: "LEADER" },
          { userId: "contributor", role: "CONTRIBUTOR" },
          { userId: "feature-assignee", role: "CONTRIBUTOR" },
        ]),
      getMilestoneById: (projectId) => ok({ projectId }),
    },
    epics: {
      getById: () =>
        ok({
          projectId: "project-1",
          milestoneId: "milestone-1",
          assigneeId: "epic-assignee",
        }),
    },
    sprints: {
      getById: () =>
        ok({
          projectId: "project-1",
          epicId: "epic-1",
          assigneeId: "sprint-assignee",
        }),
    },
    features: {
      getById: () =>
        ok({
          projectId: "project-1",
          sprintId: "sprint-1",
          assigneeId: "feature-assignee",
        }),
    },
    backlog: {
      getPbiById: () =>
        ok({
          projectId: "project-1",
          featureId: "feature-1",
          assigneeId: "pbi-assignee",
        }),
    },
    tasks: {
      getById: () =>
        ok({
          projectId: "project-1",
          pbiId: "pbi-1",
          assignedToId: "task-assignee",
        }),
    },
  };
}

describe("HierarchyAuthorization", () => {
  it("recognizes only the creator and explicit management roles", async () => {
    const authorization = new HierarchyAuthorization(createClient());

    await expect(
      authorization.canManageProject({
        userId: "owner",
        projectId: "project-1",
      }),
    ).resolves.toBe(true);
    await expect(
      authorization.canManageProject({
        userId: "leader",
        projectId: "project-1",
      }),
    ).resolves.toBe(true);
    await expect(
      authorization.canManageProject({
        userId: "contributor",
        projectId: "project-1",
      }),
    ).resolves.toBe(false);
  });

  it("allows an assignee to change work state but not structure", async () => {
    const authorization = new HierarchyAuthorization(createClient());

    await expect(
      authorization.authorizeMutation(
        "feature-assignee",
        "feature",
        "feature-1",
        "work",
      ),
    ).resolves.toMatchObject({ projectId: "project-1" });
    await expect(
      authorization.authorizeMutation(
        "feature-assignee",
        "feature",
        "feature-1",
        "structure",
      ),
    ).rejects.toMatchObject({ code: "FORBIDDEN" });
  });

  it("allows explicit parent assignment to create a child", async () => {
    const authorization = new HierarchyAuthorization(createClient());

    await expect(
      authorization.authorizeCreation(
        { userId: "epic-assignee", projectId: "project-1" },
        [{ level: "epic", id: "epic-1" }],
      ),
    ).resolves.toBeUndefined();
  });

  it("allows an assigned ancestor to create work deeper in the same chain", async () => {
    const authorization = new HierarchyAuthorization(createClient());

    await expect(
      authorization.authorizeCreation(
        { userId: "epic-assignee", projectId: "project-1" },
        [{ level: "feature", id: "feature-1" }],
      ),
    ).resolves.toBeUndefined();
  });

  it("rejects same-project parents that do not form one ancestry chain", async () => {
    const authorization = new HierarchyAuthorization(createClient());

    await expect(
      authorization.authorizeCreation(
        { userId: "owner", projectId: "project-1" },
        [
          { level: "feature", id: "feature-1" },
          { level: "sprint", id: "sprint-2" },
        ],
      ),
    ).rejects.toMatchObject({ code: "FORBIDDEN" });
  });

  it("requires management rights for a root item", async () => {
    const authorization = new HierarchyAuthorization(createClient());

    await expect(
      authorization.authorizeCreation(
        { userId: "contributor", projectId: "project-1" },
        [],
      ),
    ).rejects.toMatchObject({ code: "FORBIDDEN" });
  });

  it("rejects a caller project that differs from persisted ancestry", async () => {
    const authorization = new HierarchyAuthorization(createClient());

    await expect(
      authorization.resolve("epic", "epic-1", "project-2"),
    ).rejects.toMatchObject({ code: "FORBIDDEN" });
  });

  it("rejects a parent whose nested project association is inconsistent", async () => {
    const client = createClient();
    client.projects.getMilestoneById = () => ok({ projectId: "project-2" });
    const authorization = new HierarchyAuthorization(client);

    await expect(authorization.resolve("epic", "epic-1")).rejects.toMatchObject(
      { code: "FORBIDDEN" },
    );
  });

  it("requires an explicit project member for assignment", async () => {
    const authorization = new HierarchyAuthorization(createClient());

    await expect(
      authorization.requireProjectMember("project-1", "feature-assignee"),
    ).resolves.toBeUndefined();
    await expect(
      authorization.requireProjectMember("project-1", "outsider"),
    ).rejects.toMatchObject({ code: "BAD_REQUEST" });
  });
});

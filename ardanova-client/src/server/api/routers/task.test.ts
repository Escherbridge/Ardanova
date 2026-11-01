import { afterAll, afterEach, describe, expect, it, vi } from "vitest";

import {
  browserOverTheBff,
  dotNetAnswer,
  dotNetBaseUrl,
  fakeDotNetApi,
  refusalOf,
} from "~/server/api/dotnet-test-edge";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "member-ada", role: "INDIVIDUAL" },
  }),
}));

const browser = await browserOverTheBff();

afterEach(() => {
  vi.unstubAllGlobals();
});

afterAll(() => {
  vi.unstubAllEnvs();
});

/** .NET `TaskDto` once the claim landed, as TasksController.Assign answers 200. */
const claimedTask = {
  id: "task-frame",
  projectId: "project-barn",
  title: "Frame the east wall",
  status: "TODO",
  priority: "MEDIUM",
  taskType: "FEATURE",
  escrowStatus: "NONE",
  assignedToId: "member-ada",
  hasAgreement: false,
  createdAt: "2026-10-01T09:00:00Z",
  updatedAt: "2026-11-01T09:00:00Z",
};

describe("task.claim (Pick up task, plan §2)", () => {
  it("asks .NET for the conditional claim as the session user: claim: true, so even a manager's stale card never takes over", async () => {
    const requests = fakeDotNetApi(() => dotNetAnswer(200, claimedTask));

    await browser.task.claim.mutate({ id: "task-frame" });

    expect(requests).toEqual([
      {
        method: "PUT",
        url: `${dotNetBaseUrl}/api/tasks/task-frame/assign`,
        body: { userId: "member-ada", claim: true },
        signedActor: "member-ada",
      },
    ]);
  });

  it.each([
    // TasksController.Assign's membership refusal: "you can't", not "you sent garbage".
    [400, "Assignee must be a member of the project", "FORBIDDEN"],
    // A hidden draft's task and a missing one answer the same.
    [404, "Task with id task-frame not found", "NOT_FOUND"],
  ] as const)(
    "a .NET %i (%s) reaches the browser as %s with the API's words",
    async (status, error, code) => {
      fakeDotNetApi(() =>
        dotNetAnswer(status, {
          error,
          statusCode: status,
          traceId: "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
        }),
      );

      const failure = await refusalOf(
        browser.task.claim.mutate({ id: "task-frame" }),
      );

      expect(failure.data?.code).toBe(code);
      expect(failure.message).toBe(error);
    },
  );
});

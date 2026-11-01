import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const { currentSession, tasks } = vi.hoisted(() => ({
  currentSession: vi.fn<() => Promise<unknown>>(),
  tasks: { assign: vi.fn() },
}));

vi.mock("~/server/auth", () => ({ auth: currentSession }));
vi.mock("~/lib/api", () => ({ apiClient: { tasks } }));

import { createCallerFactory, createTRPCContext } from "~/server/api/trpc";
import { taskRouter } from "./task";

async function callerAs(userId: string) {
  currentSession.mockResolvedValue({
    expires: "2099-01-01T00:00:00Z",
    user: { id: userId, role: "INDIVIDUAL" },
  });
  return createCallerFactory(taskRouter)(
    await createTRPCContext({ headers: new Headers() }),
  );
}

describe("task.claim (self-claim / Pick up task, plan §2)", () => {
  beforeEach(() => vi.clearAllMocks());

  it("claims as the signed-in session user, not a value a caller could supply", async () => {
    tasks.assign.mockResolvedValue({
      status: 200,
      data: { id: "task-1", status: "IN_PROGRESS", assignedToId: "user-ada" },
    });

    const caller = await callerAs("user-ada");
    const claimed = await caller.claim({ id: "task-1" });

    expect(tasks.assign).toHaveBeenCalledWith("task-1", "user-ada");
    expect(claimed.assignedToId).toBe("user-ada");
  });

  it("a conditional-write 409 (already assigned) surfaces as CONFLICT, not a generic failure", async () => {
    tasks.assign.mockResolvedValue({ status: 409, error: "Already picked up" });

    const caller = await callerAs("user-ada");
    await expect(caller.claim({ id: "task-1" })).rejects.toMatchObject({
      code: "CONFLICT",
    });
  });

  it("a 400 (not a project member) surfaces as FORBIDDEN rather than BAD_REQUEST", async () => {
    tasks.assign.mockResolvedValue({
      status: 400,
      error: "You must be a member of this project to pick up its tasks.",
    });

    const caller = await callerAs("user-visitor");
    await expect(caller.claim({ id: "task-1" })).rejects.toMatchObject({
      code: "FORBIDDEN",
    });
  });

  it("a 404 (no such task) surfaces as NOT_FOUND", async () => {
    tasks.assign.mockResolvedValue({ status: 404, error: "Task not found" });

    const caller = await callerAs("user-ada");
    await expect(caller.claim({ id: "task-missing" })).rejects.toMatchObject({
      code: "NOT_FOUND",
    });
  });
});
